// Mục đích: xử lý tính toán báo cáo, lưu quy trình báo cáo và dữ liệu dashboard.
using HospitalQualityDashboard.Models.DTOs;
using HospitalQualityDashboard.Models.Enums;
using HospitalQualityDashboard.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace HospitalQualityDashboard.Services
{
    public class IndicatorCalculationService
    {
        public void Calculate(ReportEntryViewModel report, ChiSoViewModel indicator)
        {
            if (indicator.LoaiCongThuc == LoaiCongThuc.TyLe)
            {
                EnsureDenominator(report.MauSo);
                report.KetQua = report.TuSo.GetValueOrDefault() / report.MauSo.Value * 100;
            }
            else if (indicator.LoaiCongThuc == LoaiCongThuc.SoLuong || indicator.LoaiCongThuc == LoaiCongThuc.GiaTriTrucTiep || indicator.LoaiCongThuc == LoaiCongThuc.DiemTrungBinh)
            {
                report.KetQua = report.GiaTriNhap;
            }
            else
            {
                EnsureDenominator(report.MauSo);
                report.KetQua = report.TuSo.GetValueOrDefault() / report.MauSo.Value;
            }

            report.DatMucTieu = CompareTarget(report.KetQua, indicator.ToanTuSoSanh, indicator.GiaTriMucTieu);
        }

        private static void EnsureDenominator(decimal? denominator)
        {
            if (!denominator.HasValue || denominator.Value == 0)
            {
                throw new InvalidOperationException("Mau so phai lon hon 0.");
            }
        }

        private static bool? CompareTarget(decimal? result, string op, decimal? target)
        {
            if (!result.HasValue || !target.HasValue || string.IsNullOrWhiteSpace(op))
            {
                return null;
            }

            switch (op.Trim())
            {
                case ">": return result.Value > target.Value;
                case ">=": return result.Value >= target.Value;
                case "<": return result.Value < target.Value;
                case "<=": return result.Value <= target.Value;
                case "=":
                case "==": return result.Value == target.Value;
                default: return null;
            }
        }
    }

    public class ReportService : DbServiceBase
    {
        private readonly IndicatorService _indicators = new IndicatorService();
        private readonly IndicatorCalculationService _calculator = new IndicatorCalculationService();

        public IList<ReportEntryViewModel> GetAll(ReportListQueryDto dto)
        {
            return GetAll(dto.PeriodId, dto.DepartmentId, dto.IndicatorId, dto.IsAdmin, dto.CurrentDepartmentId);
        }

        public IList<ReportEntryViewModel> GetAll(int? periodId, int? departmentId, int? indicatorId, bool admin, int? currentDepartmentId)
        {
            const string sql = @"
SELECT bc.BaoCaoId, bc.KyBaoCaoId, bc.KhoaPhongId, bc.ChiSoChatLuongId, ISNULL(bc.PhanCongChiSoId, 0) AS PhanCongChiSoId, ky.TenKyBaoCao, kp.TenKhoaPhong,
       cs.MaChiSo, cs.TenChiSo, cs.LoaiCongThuc, bc.TrangThai, bc.YKienPhanHoi,
       ct.TuSo, ct.MauSo, ct.GiaTriNhap, ct.KetQua, ct.DatMucTieu, ct.GhiChu
FROM dbo.BaoCao bc
INNER JOIN dbo.KyBaoCao ky ON ky.KyBaoCaoId = bc.KyBaoCaoId
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = bc.KhoaPhongId
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = bc.ChiSoChatLuongId
LEFT JOIN dbo.BaoCaoChiTiet ct ON ct.BaoCaoId = bc.BaoCaoId
WHERE (@KyBaoCaoId IS NULL OR bc.KyBaoCaoId = @KyBaoCaoId)
  AND (@KhoaPhongId IS NULL OR bc.KhoaPhongId = @KhoaPhongId)
  AND (@ChiSoChatLuongId IS NULL OR bc.ChiSoChatLuongId = @ChiSoChatLuongId)
  AND (@IsAdmin = 1 OR ky.TrangThai <> @DraftPeriodStatus)
  AND ((@IsAdmin = 1 AND bc.TrangThai IN (@DaGuiStatus, @QuaHanStatus, @DaKhoaStatus))
       OR (@IsAdmin = 0 AND bc.KhoaPhongId = @CurrentKhoaPhongId))
ORDER BY ky.TuNgay DESC, kp.TenKhoaPhong, cs.MaChiSo";
            return Query(sql, MapReport,
                Param("@KyBaoCaoId", periodId),
                Param("@KhoaPhongId", departmentId),
                Param("@ChiSoChatLuongId", indicatorId),
                Param("@IsAdmin", admin),
                Param("@CurrentKhoaPhongId", currentDepartmentId),
                Param("@DraftPeriodStatus", (byte)TrangThaiKyBaoCao.Nhap),
                Param("@DaGuiStatus", (byte)TrangThaiBaoCao.DaGui),
                Param("@QuaHanStatus", (byte)TrangThaiBaoCao.QuaHan),
                Param("@DaKhoaStatus", (byte)TrangThaiBaoCao.DaKhoa));
        }

        public IList<ReportEntryViewModel> GetAssignedForUser(int periodId, int departmentId)
        {
            _indicators.EnsureIndicatorFrequencyTable();
            const string sql = @"
SELECT ISNULL(bc.BaoCaoId, 0) AS BaoCaoId, @KyBaoCaoId AS KyBaoCaoId, pc.KhoaPhongId, pc.ChiSoChatLuongId, pc.PhanCongChiSoId,
       ky.TenKyBaoCao, kp.TenKhoaPhong, cs.MaChiSo, cs.TenChiSo, cs.LoaiCongThuc,
       ISNULL(bc.TrangThai, 1) AS TrangThai, bc.YKienPhanHoi,
       ct.TuSo, ct.MauSo, ct.GiaTriNhap, ct.KetQua, ct.DatMucTieu, ct.GhiChu
FROM dbo.PhanCongChiSo pc
INNER JOIN dbo.KyBaoCao ky ON ky.KyBaoCaoId = @KyBaoCaoId
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = pc.KhoaPhongId
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = pc.ChiSoChatLuongId
INNER JOIN dbo.ChiSoTanSuatBaoCao cst ON cst.ChiSoChatLuongId = pc.ChiSoChatLuongId AND cst.TanSuatBaoCao = ky.LoaiKyBaoCao
LEFT JOIN dbo.BaoCao bc ON bc.KyBaoCaoId = ky.KyBaoCaoId AND bc.KhoaPhongId = pc.KhoaPhongId AND bc.ChiSoChatLuongId = pc.ChiSoChatLuongId
LEFT JOIN dbo.BaoCaoChiTiet ct ON ct.BaoCaoId = bc.BaoCaoId
WHERE pc.DangHoatDong = 1 AND pc.KhoaPhongId = @KhoaPhongId
AND ky.TrangThai = @Mo
ORDER BY cs.MaChiSo";
            return Query(sql, MapReport,
                Param("@KyBaoCaoId", periodId),
                Param("@KhoaPhongId", departmentId),
                Param("@Mo", (byte)TrangThaiKyBaoCao.Mo));
        }

        public ReportEntryViewModel Get(int id)
        {
            const string sql = @"
SELECT bc.BaoCaoId, bc.KyBaoCaoId, bc.KhoaPhongId, bc.ChiSoChatLuongId, ISNULL(bc.PhanCongChiSoId, 0) AS PhanCongChiSoId, ky.TenKyBaoCao, kp.TenKhoaPhong,
       cs.MaChiSo, cs.TenChiSo, cs.LoaiCongThuc, bc.TrangThai, bc.YKienPhanHoi,
       ct.TuSo, ct.MauSo, ct.GiaTriNhap, ct.KetQua, ct.DatMucTieu, ct.GhiChu
FROM dbo.BaoCao bc
INNER JOIN dbo.KyBaoCao ky ON ky.KyBaoCaoId = bc.KyBaoCaoId
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = bc.KhoaPhongId
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = bc.ChiSoChatLuongId
LEFT JOIN dbo.BaoCaoChiTiet ct ON ct.BaoCaoId = bc.BaoCaoId
WHERE bc.BaoCaoId=@Id";
            return Query(sql, MapReport, Param("@Id", id)).FirstOrDefault();
        }

        public int SaveDraft(ReportDraftDto dto, int userId)
        {
            return SaveDraft(new ReportEntryViewModel
            {
                BaoCaoId = dto.BaoCaoId,
                KyBaoCaoId = dto.KyBaoCaoId,
                KhoaPhongId = dto.KhoaPhongId,
                ChiSoChatLuongId = dto.ChiSoChatLuongId,
                PhanCongChiSoId = dto.PhanCongChiSoId,
                TrangThai = dto.TrangThai,
                TuSo = dto.TuSo,
                MauSo = dto.MauSo,
                GiaTriNhap = dto.GiaTriNhap,
                KetQua = dto.KetQua,
                DatMucTieu = dto.DatMucTieu,
                GhiChu = dto.GhiChu,
                YKienPhanHoi = dto.YKienPhanHoi
            }, userId);
        }

        public int SaveDraft(ReportEntryViewModel model, int userId)
        {
            var isNewReport = model.BaoCaoId == 0;
            ReportEntryViewModel existingReport = null;
            if (!isNewReport)
            {
                existingReport = Get(model.BaoCaoId);
                if (existingReport == null)
                {
                    throw new InvalidOperationException("Khong tim thay bao cao can sua.");
                }

                if (existingReport.TrangThai != TrangThaiBaoCao.Nhap)
                {
                    throw new InvalidOperationException("Chi duoc sua bao cao o trang thai Nhap.");
                }

                model.KyBaoCaoId = existingReport.KyBaoCaoId;
                model.KhoaPhongId = existingReport.KhoaPhongId;
                model.ChiSoChatLuongId = existingReport.ChiSoChatLuongId;
                model.PhanCongChiSoId = existingReport.PhanCongChiSoId;
            }

            var indicator = _indicators.Get(model.ChiSoChatLuongId);
            _calculator.Calculate(model, indicator);
            var beforeSnapshot = isNewReport ? null : GetReportDetailSnapshot(model.BaoCaoId);

            if (isNewReport)
            {
                var assignmentId = model.PhanCongChiSoId > 0
                    ? model.PhanCongChiSoId
                    : Convert.ToInt32(Scalar(@"SELECT TOP 1 PhanCongChiSoId FROM dbo.PhanCongChiSo
WHERE KhoaPhongId=@KhoaPhongId AND ChiSoChatLuongId=@ChiSoChatLuongId
ORDER BY DangHoatDong DESC, PhanCongChiSoId",
                        Param("@KhoaPhongId", model.KhoaPhongId),
                        Param("@ChiSoChatLuongId", model.ChiSoChatLuongId)));

                model.BaoCaoId = Convert.ToInt32(Scalar(@"INSERT INTO dbo.BaoCao(KyBaoCaoId, KhoaPhongId, ChiSoChatLuongId, PhanCongChiSoId, TrangThai, NguoiTaoId)
OUTPUT INSERTED.BaoCaoId VALUES(@KyBaoCaoId, @KhoaPhongId, @ChiSoChatLuongId, @PhanCongChiSoId, @TrangThai, @NguoiTaoId)",
                    Param("@KyBaoCaoId", model.KyBaoCaoId),
                    Param("@KhoaPhongId", model.KhoaPhongId),
                    Param("@ChiSoChatLuongId", model.ChiSoChatLuongId),
                    Param("@PhanCongChiSoId", assignmentId),
                    Param("@TrangThai", (byte)TrangThaiBaoCao.Nhap),
                    Param("@NguoiTaoId", userId)));
            }
            else
            {
                Execute("UPDATE dbo.BaoCao SET NgayCapNhat=GETDATE() WHERE BaoCaoId=@Id AND TrangThai=@Nhap",
                    Param("@Id", model.BaoCaoId),
                    Param("@Nhap", (byte)TrangThaiBaoCao.Nhap));
            }

            Execute(@"
IF EXISTS (SELECT 1 FROM dbo.BaoCaoChiTiet WHERE BaoCaoId=@BaoCaoId)
    UPDATE dbo.BaoCaoChiTiet SET TuSo=@TuSo, MauSo=@MauSo, GiaTriNhap=@GiaTriNhap, KetQua=@KetQua, DatMucTieu=@DatMucTieu, GhiChu=@GhiChu, NgayCapNhat=GETDATE() WHERE BaoCaoId=@BaoCaoId
ELSE
    INSERT INTO dbo.BaoCaoChiTiet(BaoCaoId, TuSo, MauSo, GiaTriNhap, KetQua, DatMucTieu, GhiChu) VALUES(@BaoCaoId, @TuSo, @MauSo, @GiaTriNhap, @KetQua, @DatMucTieu, @GhiChu)",
                Param("@BaoCaoId", model.BaoCaoId),
                Param("@TuSo", model.TuSo),
                Param("@MauSo", model.MauSo),
                Param("@GiaTriNhap", model.GiaTriNhap),
                Param("@KetQua", model.KetQua),
                Param("@DatMucTieu", model.DatMucTieu),
                Param("@GhiChu", model.GhiChu));

            var afterSnapshot = BuildReportDetailSnapshot(model);
            LogSystemAction(
                userId,
                "BaoCao",
                isNewReport ? "TaoNhap" : "SuaNhap",
                "BaoCao",
                model.BaoCaoId,
                isNewReport
                    ? "Tạo báo cáo nháp. Sau: " + afterSnapshot
                    : "Sửa báo cáo nháp. Trước: " + beforeSnapshot + " | Sau: " + afterSnapshot);

            return model.BaoCaoId;
        }

        public void Submit(int id, int userId)
        {
            var affectedRows = Execute(@"UPDATE bc
SET TrangThai=CASE WHEN CAST(GETDATE() AS date) > ky.HanNop THEN @QuaHan ELSE @DaGui END,
    NguoiGuiId=@NguoiGuiId,
    NgayGui=GETDATE(),
    NgayCapNhat=GETDATE()
FROM dbo.BaoCao bc
INNER JOIN dbo.KyBaoCao ky ON ky.KyBaoCaoId = bc.KyBaoCaoId
WHERE bc.BaoCaoId=@Id AND bc.TrangThai=@Nhap",
                Param("@DaGui", (byte)TrangThaiBaoCao.DaGui),
                Param("@QuaHan", (byte)TrangThaiBaoCao.QuaHan),
                Param("@NguoiGuiId", userId),
                Param("@Id", id),
                Param("@Nhap", (byte)TrangThaiBaoCao.Nhap));

            if (affectedRows > 0)
            {
                LogSystemAction(
                    userId,
                    "BaoCao",
                    "GuiBaoCao",
                    "BaoCao",
                    id,
                    "Gửi báo cáo. Dữ liệu tại thời điểm gửi: " + GetReportDetailSnapshot(id));
            }
        }

        public void Lock(int id)
        {
            Execute("UPDATE dbo.BaoCao SET TrangThai=@TrangThai, NgayCapNhat=GETDATE() WHERE BaoCaoId=@Id AND TrangThai IN (@DaGui, @QuaHan)",
                Param("@TrangThai", (byte)TrangThaiBaoCao.DaKhoa),
                Param("@Id", id),
                Param("@DaGui", (byte)TrangThaiBaoCao.DaGui),
                Param("@QuaHan", (byte)TrangThaiBaoCao.QuaHan));
        }

        public void Delete(int id)
        {
            Execute("DELETE FROM dbo.BaoCaoChiTiet WHERE BaoCaoId=@Id", Param("@Id", id));
            Execute("DELETE FROM dbo.BaoCao WHERE BaoCaoId=@Id", Param("@Id", id));
        }

        private static ReportEntryViewModel MapReport(SqlDataReader reader)
        {
            return new ReportEntryViewModel
            {
                BaoCaoId = Int(reader, "BaoCaoId"),
                KyBaoCaoId = Int(reader, "KyBaoCaoId"),
                KhoaPhongId = Int(reader, "KhoaPhongId"),
                ChiSoChatLuongId = Int(reader, "ChiSoChatLuongId"),
                PhanCongChiSoId = Int(reader, "PhanCongChiSoId"),
                TenKyBaoCao = String(reader, "TenKyBaoCao"),
                TenKhoaPhong = String(reader, "TenKhoaPhong"),
                MaChiSo = String(reader, "MaChiSo"),
                TenChiSo = String(reader, "TenChiSo"),
                LoaiCongThuc = (LoaiCongThuc)reader.GetByte(reader.GetOrdinal("LoaiCongThuc")),
                TrangThai = (TrangThaiBaoCao)Convert.ToByte(reader["TrangThai"]),
                TuSo = NullableDecimal(reader, "TuSo"),
                MauSo = NullableDecimal(reader, "MauSo"),
                GiaTriNhap = NullableDecimal(reader, "GiaTriNhap"),
                KetQua = NullableDecimal(reader, "KetQua"),
                DatMucTieu = reader.IsDBNull(reader.GetOrdinal("DatMucTieu")) ? (bool?)null : reader.GetBoolean(reader.GetOrdinal("DatMucTieu")),
                GhiChu = String(reader, "GhiChu"),
                YKienPhanHoi = String(reader, "YKienPhanHoi")
            };
        }

        private string GetReportDetailSnapshot(int reportId)
        {
            var snapshot = QuerySingle(@"
SELECT bc.BaoCaoId, bc.KyBaoCaoId, bc.KhoaPhongId, bc.ChiSoChatLuongId, ISNULL(bc.PhanCongChiSoId, 0) AS PhanCongChiSoId,
       ct.TuSo, ct.MauSo, ct.GiaTriNhap, ct.KetQua, ct.DatMucTieu, ct.GhiChu
FROM dbo.BaoCao bc
LEFT JOIN dbo.BaoCaoChiTiet ct ON ct.BaoCaoId = bc.BaoCaoId
WHERE bc.BaoCaoId = @BaoCaoId",
                reader => new ReportEntryViewModel
                {
                    BaoCaoId = Int(reader, "BaoCaoId"),
                    KyBaoCaoId = Int(reader, "KyBaoCaoId"),
                    KhoaPhongId = Int(reader, "KhoaPhongId"),
                    ChiSoChatLuongId = Int(reader, "ChiSoChatLuongId"),
                    PhanCongChiSoId = Int(reader, "PhanCongChiSoId"),
                    TuSo = NullableDecimal(reader, "TuSo"),
                    MauSo = NullableDecimal(reader, "MauSo"),
                    GiaTriNhap = NullableDecimal(reader, "GiaTriNhap"),
                    KetQua = NullableDecimal(reader, "KetQua"),
                    DatMucTieu = reader.IsDBNull(reader.GetOrdinal("DatMucTieu")) ? (bool?)null : reader.GetBoolean(reader.GetOrdinal("DatMucTieu")),
                    GhiChu = String(reader, "GhiChu")
                },
                Param("@BaoCaoId", reportId));

            return snapshot == null ? "Không tìm thấy dữ liệu báo cáo." : BuildReportDetailSnapshot(snapshot);
        }

        private static string BuildReportDetailSnapshot(ReportEntryViewModel model)
        {
            return string.Format(
                "BaoCaoId={0}; KyBaoCaoId={1}; KhoaPhongId={2}; ChiSoChatLuongId={3}; PhanCongChiSoId={4}; TuSo={5}; MauSo={6}; GiaTriNhap={7}; KetQua={8}; DatMucTieu={9}; GhiChu={10}",
                model.BaoCaoId,
                model.KyBaoCaoId,
                model.KhoaPhongId,
                model.ChiSoChatLuongId,
                model.PhanCongChiSoId,
                FormatValue(model.TuSo),
                FormatValue(model.MauSo),
                FormatValue(model.GiaTriNhap),
                FormatValue(model.KetQua),
                model.DatMucTieu.HasValue ? model.DatMucTieu.Value.ToString() : "NULL",
                string.IsNullOrWhiteSpace(model.GhiChu) ? "NULL" : model.GhiChu);
        }

        private static string FormatValue(decimal? value)
        {
            return value.HasValue ? value.Value.ToString("0.####") : "NULL";
        }

        private void LogSystemAction(int userId, string feature, string action, string entityName, int? entityId, string content)
        {
            Execute(@"INSERT INTO dbo.NhatKyHeThong(TaiKhoanId, ChucNang, HanhDong, DoiTuong, DoiTuongId, NoiDung)
VALUES(@TaiKhoanId, @ChucNang, @HanhDong, @DoiTuong, @DoiTuongId, @NoiDung)",
                Param("@TaiKhoanId", userId),
                Param("@ChucNang", feature),
                Param("@HanhDong", action),
                Param("@DoiTuong", entityName),
                Param("@DoiTuongId", entityId),
                Param("@NoiDung", content));
        }
    }

    public class DashboardService : DbServiceBase
    {
        public DashboardViewModel GetDashboard(bool admin, int? departmentId)
        {
            var indicators = new IndicatorService();
            indicators.EnsureIndicatorFrequencyTable();
            var model = new DashboardViewModel
            {
                IsAdmin = admin,
                DepartmentProgress = new List<DepartmentProgressViewModel>(),
                MissingReports = new List<MissingReportAlertViewModel>()
            };
            model.BaoCaoDaGui = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.BaoCao WHERE TrangThai IN (2,3,4) AND (@IsAdmin=1 OR KhoaPhongId=@KhoaPhongId)", Param("@IsAdmin", admin), Param("@KhoaPhongId", departmentId)));
            model.ChiSoDuocPhanCong = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.PhanCongChiSo WHERE DangHoatDong=1 AND (@IsAdmin=1 OR KhoaPhongId=@KhoaPhongId)", Param("@IsAdmin", admin), Param("@KhoaPhongId", departmentId)));
            model.TongChiSo = admin
                ? Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.ChiSoChatLuong WHERE DangHoatDong=1"))
                : model.ChiSoDuocPhanCong;
            model.BaoCaoQuaHan = Convert.ToInt32(Scalar(@"SELECT COUNT(*) FROM dbo.BaoCao bc WHERE bc.TrangThai=@QuaHanStatus
AND (@IsAdmin=1 OR bc.KhoaPhongId=@KhoaPhongId)", Param("@QuaHanStatus", (byte)TrangThaiBaoCao.QuaHan), Param("@IsAdmin", admin), Param("@KhoaPhongId", departmentId)));
            model.BaoCaoThieu = model.ChiSoDuocPhanCong - model.BaoCaoDaGui;

            if (admin)
            {
                model.DepartmentProgress = Query(@"SELECT kp.TenKhoaPhong, COUNT(pc.PhanCongChiSoId) AS Tong,
ISNULL(SUM(CASE WHEN bc.TrangThai IN (2,3,4) THEN 1 ELSE 0 END), 0) AS DaGui
FROM dbo.KhoaPhong kp
LEFT JOIN dbo.PhanCongChiSo pc ON pc.KhoaPhongId=kp.KhoaPhongId AND pc.DangHoatDong=1
LEFT JOIN dbo.BaoCao bc ON bc.KhoaPhongId=kp.KhoaPhongId AND bc.ChiSoChatLuongId=pc.ChiSoChatLuongId
GROUP BY kp.TenKhoaPhong ORDER BY kp.TenKhoaPhong", r => new DepartmentProgressViewModel
                {
                    TenKhoaPhong = String(r, "TenKhoaPhong"),
                    Tong = Int(r, "Tong"),
                    DaGui = Int(r, "DaGui")
                });
            }
            else if (departmentId.HasValue)
            {
                model.MissingReports = GetMissingReportsForDepartment(departmentId.Value);
                model.BaoCaoThieu = model.MissingReports.Count;
                model.DueSoonReportCount = model.MissingReports.Count(x => x.IsDueSoon);
                model.OverdueMissingReportCount = model.MissingReports.Count(x => x.IsOverdue);

                var departmentName = Convert.ToString(Scalar("SELECT TenKhoaPhong FROM dbo.KhoaPhong WHERE KhoaPhongId=@KhoaPhongId", Param("@KhoaPhongId", departmentId)));
                if (!string.IsNullOrWhiteSpace(departmentName))
                {
                    model.DepartmentProgress.Add(new DepartmentProgressViewModel
                    {
                        TenKhoaPhong = departmentName,
                        Tong = model.ChiSoDuocPhanCong,
                        DaGui = model.BaoCaoDaGui
                    });
                }
            }

            return model;
        }

        public IList<MissingReportAlertViewModel> GetMissingReportsForDepartment(int departmentId)
        {
            return GetMissingReportsForDepartment(departmentId, null, false);
        }

        public IList<MissingReportAlertViewModel> GetMissingReportsForDepartment(int departmentId, int? periodId, bool overdueOnly)
        {
            const string sql = @"
SELECT ky.KyBaoCaoId, ky.TenKyBaoCao, ky.HanNop, cs.ChiSoChatLuongId, pc.PhanCongChiSoId,
       cs.MaChiSo, cs.TenChiSo, DATEDIFF(day, CAST(GETDATE() AS date), ky.HanNop) AS DaysUntilDue
FROM dbo.KyBaoCao ky
INNER JOIN dbo.PhanCongChiSo pc ON pc.DangHoatDong = 1
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = pc.ChiSoChatLuongId AND cs.DangHoatDong = 1
INNER JOIN dbo.ChiSoTanSuatBaoCao cst ON cst.ChiSoChatLuongId = pc.ChiSoChatLuongId AND cst.TanSuatBaoCao = ky.LoaiKyBaoCao
LEFT JOIN dbo.BaoCao bc ON bc.KyBaoCaoId = ky.KyBaoCaoId
    AND bc.KhoaPhongId = pc.KhoaPhongId
    AND bc.ChiSoChatLuongId = pc.ChiSoChatLuongId
    AND bc.TrangThai IN (@DaGui, @QuaHan, @DaKhoa)
WHERE ky.TrangThai = @Mo
  AND pc.KhoaPhongId = @KhoaPhongId
  AND (@KyBaoCaoId IS NULL OR ky.KyBaoCaoId = @KyBaoCaoId)
  AND (@OverdueOnly = 0 OR DATEDIFF(day, CAST(GETDATE() AS date), ky.HanNop) < 0)
  AND bc.BaoCaoId IS NULL
ORDER BY ky.HanNop, cs.MaChiSo";

            return Query(sql, reader =>
            {
                var daysUntilDue = Int(reader, "DaysUntilDue");
                return new MissingReportAlertViewModel
                {
                    KyBaoCaoId = Int(reader, "KyBaoCaoId"),
                    TenKyBaoCao = String(reader, "TenKyBaoCao"),
                    HanNop = reader.GetDateTime(reader.GetOrdinal("HanNop")),
                    ChiSoChatLuongId = Int(reader, "ChiSoChatLuongId"),
                    PhanCongChiSoId = Int(reader, "PhanCongChiSoId"),
                    MaChiSo = String(reader, "MaChiSo"),
                    TenChiSo = String(reader, "TenChiSo"),
                    DaysUntilDue = daysUntilDue,
                    IsOverdue = daysUntilDue < 0,
                    IsDueSoon = daysUntilDue >= 0 && daysUntilDue <= 7
                };
            },
                Param("@DaGui", (byte)TrangThaiBaoCao.DaGui),
                Param("@QuaHan", (byte)TrangThaiBaoCao.QuaHan),
                Param("@DaKhoa", (byte)TrangThaiBaoCao.DaKhoa),
                Param("@Mo", (byte)TrangThaiKyBaoCao.Mo),
                Param("@KhoaPhongId", departmentId),
                Param("@KyBaoCaoId", periodId),
                Param("@OverdueOnly", overdueOnly ? 1 : 0));
        }
    }
}
