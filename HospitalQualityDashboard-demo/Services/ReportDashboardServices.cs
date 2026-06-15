// Mục đích: xử lý tính toán báo cáo, lưu quy trình báo cáo và dữ liệu dashboard.
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.Enums;
using HospitalQualityDashboardDemo.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Services
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
                throw new InvalidOperationException("Mẫu số phải lớn hơn 0.");
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

        public IList<ReportEntryViewModel> GetAll(ReportListQueryDto dto, int page, int pageSize, out int totalItems)
        {
            page = NormalizePage(page);
            pageSize = NormalizePageSize(pageSize);

            const string countSql = @"
SELECT COUNT(*)
FROM dbo.BaoCao bc
INNER JOIN dbo.KyBaoCao ky ON ky.KyBaoCaoId = bc.KyBaoCaoId
WHERE (@KyBaoCaoId IS NULL OR bc.KyBaoCaoId = @KyBaoCaoId)
  AND (@KhoaPhongId IS NULL OR bc.KhoaPhongId = @KhoaPhongId)
  AND (@ChiSoChatLuongId IS NULL OR bc.ChiSoChatLuongId = @ChiSoChatLuongId)
  AND (@IsAdmin = 1 OR ky.TrangThai <> @DraftPeriodStatus)
  AND ((@IsAdmin = 1 AND bc.TrangThai IN (@DaGuiStatus, @QuaHanStatus, @DaKhoaStatus))
       OR (@IsAdmin = 0 AND bc.KhoaPhongId = @CurrentKhoaPhongId))";

            var parameters = BuildReportListParameters(dto);
            totalItems = Convert.ToInt32(Scalar(countSql, parameters));

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
ORDER BY ky.TuNgay DESC, kp.TenKhoaPhong, cs.MaChiSo
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var pagedParameters = BuildReportListParameters(dto)
                .Concat(new[] { Param("@Offset", (page - 1) * pageSize), Param("@PageSize", pageSize) })
                .ToArray();

            return Query(sql, MapReport, pagedParameters);
        }

        private static SqlParameter[] BuildReportListParameters(ReportListQueryDto dto)
        {
            return new[]
            {
                Param("@KyBaoCaoId", dto.PeriodId),
                Param("@KhoaPhongId", dto.DepartmentId),
                Param("@ChiSoChatLuongId", dto.IndicatorId),
                Param("@IsAdmin", dto.IsAdmin),
                Param("@CurrentKhoaPhongId", dto.CurrentDepartmentId),
                Param("@DraftPeriodStatus", (byte)TrangThaiKyBaoCao.Nhap),
                Param("@DaGuiStatus", (byte)TrangThaiBaoCao.DaGui),
                Param("@QuaHanStatus", (byte)TrangThaiBaoCao.QuaHan),
                Param("@DaKhoaStatus", (byte)TrangThaiBaoCao.DaKhoa)
            };
        }

        private static int NormalizePage(int page)
        {
            return page < 1 ? 1 : page;
        }

        private static int NormalizePageSize(int pageSize)
        {
            if (pageSize < 1) return 20;
            return pageSize > 100 ? 100 : pageSize;
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
                    throw new InvalidOperationException("Không tìm thấy báo cáo cần sửa.");
                }

                if (existingReport.TrangThai != TrangThaiBaoCao.Nhap)
                {
                    throw new InvalidOperationException("Chỉ được sửa báo cáo ở trạng thái Nháp.");
                }

                model.KyBaoCaoId = existingReport.KyBaoCaoId;
                model.KhoaPhongId = existingReport.KhoaPhongId;
                model.ChiSoChatLuongId = existingReport.ChiSoChatLuongId;
                model.PhanCongChiSoId = existingReport.PhanCongChiSoId;
            }

            var indicator = _indicators.Get(model.ChiSoChatLuongId);
            _calculator.Calculate(model, indicator);
            var beforeSnapshot = isNewReport ? null : GetReportDetailSnapshot(model.BaoCaoId);

            ExecuteInTransaction((conn, trans) =>
            {
                if (isNewReport)
                {
                    var assignmentId = model.PhanCongChiSoId > 0
                        ? model.PhanCongChiSoId
                        : Convert.ToInt32(Scalar(conn, trans, @"SELECT TOP 1 PhanCongChiSoId FROM dbo.PhanCongChiSo
WHERE KhoaPhongId=@KhoaPhongId AND ChiSoChatLuongId=@ChiSoChatLuongId
ORDER BY DangHoatDong DESC, PhanCongChiSoId",
                            Param("@KhoaPhongId", model.KhoaPhongId),
                            Param("@ChiSoChatLuongId", model.ChiSoChatLuongId)));

                    model.BaoCaoId = Convert.ToInt32(Scalar(conn, trans, @"INSERT INTO dbo.BaoCao(KyBaoCaoId, KhoaPhongId, ChiSoChatLuongId, PhanCongChiSoId, TrangThai, NguoiTaoId)
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
                    Execute(conn, trans, "UPDATE dbo.BaoCao SET NgayCapNhat=GETDATE() WHERE BaoCaoId=@Id AND TrangThai=@Nhap",
                        Param("@Id", model.BaoCaoId),
                        Param("@Nhap", (byte)TrangThaiBaoCao.Nhap));
                }

                Execute(conn, trans, @"
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
                LogSystemAction(conn, trans,
                    userId,
                    "BaoCao",
                    isNewReport ? "TaoNhap" : "SuaNhap",
                    "BaoCao",
                    model.BaoCaoId,
                    isNewReport
                        ? "Tạo báo cáo nháp. Sau: " + afterSnapshot
                        : "Sửa báo cáo nháp. Trước: " + beforeSnapshot + " | Sau: " + afterSnapshot);
            });

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

        public void Delete(int id, int userId)
        {
            var beforeSnapshot = GetReportDetailSnapshot(id);
            ExecuteInTransaction((conn, trans) =>
            {
                Execute(conn, trans, "DELETE FROM dbo.BaoCaoChiTiet WHERE BaoCaoId=@Id", Param("@Id", id));
                Execute(conn, trans, "DELETE FROM dbo.BaoCao WHERE BaoCaoId=@Id", Param("@Id", id));

                LogSystemAction(
                    conn,
                    trans,
                    userId,
                    "BaoCao",
                    "XoaBaoCao",
                    "BaoCao",
                    id,
                    "Xóa báo cáo. Dữ liệu trước khi xóa: " + beforeSnapshot);
            });
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

        private void LogSystemAction(SqlConnection connection, SqlTransaction transaction, int userId, string feature, string action, string entityName, int? entityId, string content)
        {
            Execute(connection, transaction, @"INSERT INTO dbo.NhatKyHeThong(TaiKhoanId, ChucNang, HanhDong, DoiTuong, DoiTuongId, NoiDung)
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
        public DashboardService()
            : base(DatabaseConfiguration.GetConnectionString(), 60)
        {
        }

        private static string CalculateXepLoai(int daGui, int tong)
        {
            if (tong == 0) return "N/A";
            decimal rate = (decimal)daGui * 100 / tong;
            if (rate >= 90) return "Xuất sắc";
            if (rate >= 70) return "Khá";
            if (rate >= 50) return "Trung bình";
            return "Yếu";
        }

        private DashboardViewModel GetDashboardOptimized(bool admin, int? departmentId, int? tanSuatFilter)
        {
            var model = new DashboardViewModel
            {
                IsAdmin = admin,
                DepartmentProgress = new List<DepartmentProgressViewModel>(),
                MissingReports = new List<MissingReportAlertViewModel>(),
                SelectedTanSuat = tanSuatFilter,
                TanSuatOptions = BuildDashboardFrequencyOptions(tanSuatFilter)
            };

            ApplyOptimizedDashboardSummary(model, admin, departmentId, tanSuatFilter);
            model.DepartmentProgress = GetOptimizedDepartmentProgress(admin ? null : departmentId, tanSuatFilter);

            if (!admin && departmentId.HasValue)
            {
                model.MissingReports = GetMissingReportsForDepartment(departmentId.Value);
                model.BaoCaoThieu = model.MissingReports.Count;
                model.DueSoonReportCount = model.MissingReports.Count(x => x.IsDueSoon);
                model.OverdueMissingReportCount = model.MissingReports.Count(x => x.IsOverdue);
            }

            return model;
        }

        private static IList<SelectListItem> BuildDashboardFrequencyOptions(int? tanSuatFilter)
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Tất cả tần suất", Selected = !tanSuatFilter.HasValue },
                new SelectListItem { Value = "1", Text = "Hàng ngày", Selected = tanSuatFilter == 1 },
                new SelectListItem { Value = "2", Text = "Hàng tuần", Selected = tanSuatFilter == 2 },
                new SelectListItem { Value = "3", Text = "Hàng tháng", Selected = tanSuatFilter == 3 },
                new SelectListItem { Value = "4", Text = "Hàng quý", Selected = tanSuatFilter == 4 },
                new SelectListItem { Value = "5", Text = "6 tháng", Selected = tanSuatFilter == 5 },
                new SelectListItem { Value = "9", Text = "9 tháng", Selected = tanSuatFilter == 9 },
                new SelectListItem { Value = "6", Text = "Hàng năm", Selected = tanSuatFilter == 6 },
                new SelectListItem { Value = "7", Text = "Khi phát sinh", Selected = tanSuatFilter == 7 },
                new SelectListItem { Value = "8", Text = "Trước và sau khi thực hiện", Selected = tanSuatFilter == 8 }
            };
        }

        private void ApplyOptimizedDashboardSummary(DashboardViewModel model, bool admin, int? departmentId, int? tanSuatFilter)
        {
            var sql = admin ? @"
SELECT
    (SELECT COUNT(DISTINCT bc.BaoCaoId)
     FROM dbo.BaoCao bc
     LEFT JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = bc.ChiSoChatLuongId
     WHERE bc.TrangThai IN (2,3,4)
       AND (@TanSuat IS NULL OR ts.TanSuatBaoCao = @TanSuat)) AS BaoCaoDaGui,
    (SELECT COUNT(DISTINCT pc.PhanCongChiSoId)
     FROM dbo.PhanCongChiSo pc
     LEFT JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId
     WHERE pc.DangHoatDong = 1
       AND (@TanSuat IS NULL OR ts.TanSuatBaoCao = @TanSuat)) AS ChiSoDuocPhanCong,
    (SELECT COUNT(DISTINCT cs.ChiSoChatLuongId)
     FROM dbo.ChiSoChatLuong cs
     LEFT JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = cs.ChiSoChatLuongId
     WHERE cs.DangHoatDong = 1
       AND (@TanSuat IS NULL OR ts.TanSuatBaoCao = @TanSuat)) AS TongChiSo,
    (SELECT COUNT(DISTINCT bc.BaoCaoId)
     FROM dbo.BaoCao bc
     LEFT JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = bc.ChiSoChatLuongId
     WHERE bc.TrangThai = @QuaHanStatus
       AND (@TanSuat IS NULL OR ts.TanSuatBaoCao = @TanSuat)) AS BaoCaoQuaHan"
            : @"
SELECT
    (SELECT COUNT(DISTINCT bc.BaoCaoId)
     FROM dbo.BaoCao bc
     LEFT JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = bc.ChiSoChatLuongId
     WHERE bc.TrangThai IN (2,3,4)
       AND bc.KhoaPhongId = @KhoaPhongId
       AND (@TanSuat IS NULL OR ts.TanSuatBaoCao = @TanSuat)) AS BaoCaoDaGui,
    (SELECT COUNT(DISTINCT pc.PhanCongChiSoId)
     FROM dbo.PhanCongChiSo pc
     LEFT JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId
     WHERE pc.DangHoatDong = 1
       AND pc.KhoaPhongId = @KhoaPhongId
       AND (@TanSuat IS NULL OR ts.TanSuatBaoCao = @TanSuat)) AS ChiSoDuocPhanCong,
    (SELECT COUNT(DISTINCT pc.PhanCongChiSoId)
     FROM dbo.PhanCongChiSo pc
     LEFT JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId
     WHERE pc.DangHoatDong = 1
       AND pc.KhoaPhongId = @KhoaPhongId
       AND (@TanSuat IS NULL OR ts.TanSuatBaoCao = @TanSuat)) AS TongChiSo,
    (SELECT COUNT(DISTINCT bc.BaoCaoId)
     FROM dbo.BaoCao bc
     LEFT JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = bc.ChiSoChatLuongId
     WHERE bc.TrangThai = @QuaHanStatus
       AND bc.KhoaPhongId = @KhoaPhongId
       AND (@TanSuat IS NULL OR ts.TanSuatBaoCao = @TanSuat)) AS BaoCaoQuaHan";

            var summary = QuerySingle(sql, r => new DashboardSummaryRow
            {
                BaoCaoDaGui = Int(r, "BaoCaoDaGui"),
                ChiSoDuocPhanCong = Int(r, "ChiSoDuocPhanCong"),
                TongChiSo = Int(r, "TongChiSo"),
                BaoCaoQuaHan = Int(r, "BaoCaoQuaHan")
            },
                Param("@KhoaPhongId", departmentId),
                Param("@TanSuat", tanSuatFilter),
                Param("@QuaHanStatus", (byte)TrangThaiBaoCao.QuaHan));

            if (summary == null)
            {
                return;
            }

            model.BaoCaoDaGui = summary.BaoCaoDaGui;
            model.ChiSoDuocPhanCong = summary.ChiSoDuocPhanCong;
            model.TongChiSo = summary.TongChiSo;
            model.BaoCaoQuaHan = summary.BaoCaoQuaHan;
            model.BaoCaoThieu = model.ChiSoDuocPhanCong - model.BaoCaoDaGui;
        }

        private IList<DepartmentProgressViewModel> GetOptimizedDepartmentProgress(int? departmentId, int? tanSuatFilter)
        {
            const string sql = @"
WITH AssignmentFrequency AS
(
    SELECT pc.KhoaPhongId, pc.ChiSoChatLuongId, cst.TanSuatBaoCao
    FROM dbo.PhanCongChiSo pc
    LEFT JOIN dbo.ChiSoTanSuatBaoCao cst ON cst.ChiSoChatLuongId = pc.ChiSoChatLuongId
    WHERE pc.DangHoatDong = 1
),
ReportByIndicator AS
(
    SELECT bc.KhoaPhongId, bc.ChiSoChatLuongId,
           MAX(CASE WHEN bc.TrangThai IN (2,3,4) THEN 1 ELSE 0 END) AS HasSubmitted,
           MAX(CASE WHEN bc.TrangThai = 1 THEN 1 ELSE 0 END) AS HasDraft
    FROM dbo.BaoCao bc
    GROUP BY bc.KhoaPhongId, bc.ChiSoChatLuongId
)
SELECT
    kp.TenKhoaPhong,
    COUNT(DISTINCT CASE WHEN @TanSuat IS NULL OR af.TanSuatBaoCao = @TanSuat THEN af.ChiSoChatLuongId END) AS Tong,
    COUNT(DISTINCT CASE WHEN (@TanSuat IS NULL OR af.TanSuatBaoCao = @TanSuat) AND rb.HasSubmitted = 1 THEN af.ChiSoChatLuongId END) AS DaGui,
    COUNT(DISTINCT CASE WHEN (@TanSuat IS NULL OR af.TanSuatBaoCao = @TanSuat) AND rb.HasDraft = 1 THEN af.ChiSoChatLuongId END) AS LuuNhap,
    COUNT(DISTINCT CASE WHEN af.TanSuatBaoCao = 3 THEN af.ChiSoChatLuongId END) AS TongThang,
    COUNT(DISTINCT CASE WHEN af.TanSuatBaoCao = 3 AND rb.HasSubmitted = 1 THEN af.ChiSoChatLuongId END) AS DaGuiThang,
    COUNT(DISTINCT CASE WHEN af.TanSuatBaoCao = 4 THEN af.ChiSoChatLuongId END) AS TongQuy,
    COUNT(DISTINCT CASE WHEN af.TanSuatBaoCao = 4 AND rb.HasSubmitted = 1 THEN af.ChiSoChatLuongId END) AS DaGuiQuy,
    COUNT(DISTINCT CASE WHEN af.TanSuatBaoCao = 6 THEN af.ChiSoChatLuongId END) AS TongNam,
    COUNT(DISTINCT CASE WHEN af.TanSuatBaoCao = 6 AND rb.HasSubmitted = 1 THEN af.ChiSoChatLuongId END) AS DaGuiNam
FROM dbo.KhoaPhong kp
LEFT JOIN AssignmentFrequency af ON af.KhoaPhongId = kp.KhoaPhongId
LEFT JOIN ReportByIndicator rb ON rb.KhoaPhongId = af.KhoaPhongId AND rb.ChiSoChatLuongId = af.ChiSoChatLuongId
WHERE @KhoaPhongId IS NULL OR kp.KhoaPhongId = @KhoaPhongId
GROUP BY kp.TenKhoaPhong
ORDER BY kp.TenKhoaPhong";

            var progressItems = Query(sql, r => new DepartmentProgressViewModel
            {
                TenKhoaPhong = String(r, "TenKhoaPhong"),
                Tong = Int(r, "Tong"),
                DaGui = Int(r, "DaGui"),
                LuuNhap = Int(r, "LuuNhap"),
                TongThang = Int(r, "TongThang"),
                DaGuiThang = Int(r, "DaGuiThang"),
                TongQuy = Int(r, "TongQuy"),
                DaGuiQuy = Int(r, "DaGuiQuy"),
                TongNam = Int(r, "TongNam"),
                DaGuiNam = Int(r, "DaGuiNam")
            },
                Param("@KhoaPhongId", departmentId),
                Param("@TanSuat", tanSuatFilter));

            foreach (var progress in progressItems)
            {
                progress.XepLoai = CalculateXepLoai(progress.DaGui, progress.Tong);
                progress.XepLoaiThang = CalculateXepLoai(progress.DaGuiThang, progress.TongThang);
                progress.XepLoaiQuy = CalculateXepLoai(progress.DaGuiQuy, progress.TongQuy);
                progress.XepLoaiNam = CalculateXepLoai(progress.DaGuiNam, progress.TongNam);
            }

            return progressItems;
        }

        private class DashboardSummaryRow
        {
            public int BaoCaoDaGui { get; set; }
            public int ChiSoDuocPhanCong { get; set; }
            public int TongChiSo { get; set; }
            public int BaoCaoQuaHan { get; set; }
        }

        #pragma warning disable 0162
        public DashboardViewModel GetDashboard(bool admin, int? departmentId, int? tanSuatFilter = null)
        {
            return GetDashboardOptimized(admin, departmentId, tanSuatFilter);

            var model = new DashboardViewModel
            {
                IsAdmin = admin,
                DepartmentProgress = new List<DepartmentProgressViewModel>(),
                MissingReports = new List<MissingReportAlertViewModel>(),
                SelectedTanSuat = tanSuatFilter
            };

            model.TanSuatOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Tất cả tần suất", Selected = !tanSuatFilter.HasValue },
                new SelectListItem { Value = "1", Text = "Hàng ngày", Selected = tanSuatFilter == 1 },
                new SelectListItem { Value = "2", Text = "Hàng tuần", Selected = tanSuatFilter == 2 },
                new SelectListItem { Value = "3", Text = "Hàng tháng", Selected = tanSuatFilter == 3 },
                new SelectListItem { Value = "4", Text = "Hàng quý", Selected = tanSuatFilter == 4 },
                new SelectListItem { Value = "5", Text = "6 tháng", Selected = tanSuatFilter == 5 },
                new SelectListItem { Value = "9", Text = "9 tháng", Selected = tanSuatFilter == 9 },
                new SelectListItem { Value = "6", Text = "Hàng năm", Selected = tanSuatFilter == 6 },
                new SelectListItem { Value = "7", Text = "Khi phát sinh", Selected = tanSuatFilter == 7 },
                new SelectListItem { Value = "8", Text = "Trước và sau khi thực hiện", Selected = tanSuatFilter == 8 }
            };

            if (admin)
            {
                if (tanSuatFilter.HasValue)
                {
                    model.BaoCaoDaGui = Convert.ToInt32(Scalar(@"
                        SELECT COUNT(DISTINCT bc.BaoCaoId) 
                        FROM dbo.BaoCao bc 
                        JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = bc.ChiSoChatLuongId
                        WHERE bc.TrangThai IN (2,3,4) AND ts.TanSuatBaoCao = @TanSuat", Param("@TanSuat", tanSuatFilter.Value)));

                    model.ChiSoDuocPhanCong = Convert.ToInt32(Scalar(@"
                        SELECT COUNT(DISTINCT pc.PhanCongChiSoId) 
                        FROM dbo.PhanCongChiSo pc 
                        JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId
                        WHERE pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = @TanSuat", Param("@TanSuat", tanSuatFilter.Value)));

                    model.TongChiSo = Convert.ToInt32(Scalar(@"
                        SELECT COUNT(DISTINCT cs.ChiSoChatLuongId) 
                        FROM dbo.ChiSoChatLuong cs 
                        JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = cs.ChiSoChatLuongId
                        WHERE cs.DangHoatDong = 1 AND ts.TanSuatBaoCao = @TanSuat", Param("@TanSuat", tanSuatFilter.Value)));

                    model.BaoCaoQuaHan = Convert.ToInt32(Scalar(@"
                        SELECT COUNT(DISTINCT bc.BaoCaoId) 
                        FROM dbo.BaoCao bc 
                        JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = bc.ChiSoChatLuongId
                        WHERE bc.TrangThai = @QuaHanStatus AND ts.TanSuatBaoCao = @TanSuat", 
                        Param("@QuaHanStatus", (byte)TrangThaiBaoCao.QuaHan), Param("@TanSuat", tanSuatFilter.Value)));
                }
                else
                {
                    model.BaoCaoDaGui = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.BaoCao WHERE TrangThai IN (2,3,4)"));
                    model.ChiSoDuocPhanCong = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.PhanCongChiSo WHERE DangHoatDong=1"));
                    model.TongChiSo = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.ChiSoChatLuong WHERE DangHoatDong=1"));
                    model.BaoCaoQuaHan = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.BaoCao bc WHERE bc.TrangThai=@QuaHanStatus", Param("@QuaHanStatus", (byte)TrangThaiBaoCao.QuaHan)));
                }
            }
            else
            {
                if (tanSuatFilter.HasValue)
                {
                    model.BaoCaoDaGui = Convert.ToInt32(Scalar(@"
                        SELECT COUNT(DISTINCT bc.BaoCaoId) 
                        FROM dbo.BaoCao bc 
                        JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = bc.ChiSoChatLuongId
                        WHERE bc.TrangThai IN (2,3,4) AND bc.KhoaPhongId=@KhoaPhongId AND ts.TanSuatBaoCao = @TanSuat", 
                        Param("@KhoaPhongId", departmentId), Param("@TanSuat", tanSuatFilter.Value)));

                    model.ChiSoDuocPhanCong = Convert.ToInt32(Scalar(@"
                        SELECT COUNT(DISTINCT pc.PhanCongChiSoId) 
                        FROM dbo.PhanCongChiSo pc 
                        JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId
                        WHERE pc.DangHoatDong = 1 AND pc.KhoaPhongId=@KhoaPhongId AND ts.TanSuatBaoCao = @TanSuat", 
                        Param("@KhoaPhongId", departmentId), Param("@TanSuat", tanSuatFilter.Value)));

                    model.TongChiSo = model.ChiSoDuocPhanCong;

                    model.BaoCaoQuaHan = Convert.ToInt32(Scalar(@"
                        SELECT COUNT(DISTINCT bc.BaoCaoId) 
                        FROM dbo.BaoCao bc 
                        JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = bc.ChiSoChatLuongId
                        WHERE bc.TrangThai = @QuaHanStatus AND bc.KhoaPhongId=@KhoaPhongId AND ts.TanSuatBaoCao = @TanSuat", 
                        Param("@QuaHanStatus", (byte)TrangThaiBaoCao.QuaHan), Param("@KhoaPhongId", departmentId), Param("@TanSuat", tanSuatFilter.Value)));
                }
                else
                {
                    model.BaoCaoDaGui = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.BaoCao WHERE TrangThai IN (2,3,4) AND KhoaPhongId=@KhoaPhongId", Param("@KhoaPhongId", departmentId)));
                    model.ChiSoDuocPhanCong = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.PhanCongChiSo WHERE DangHoatDong=1 AND KhoaPhongId=@KhoaPhongId", Param("@KhoaPhongId", departmentId)));
                    model.TongChiSo = model.ChiSoDuocPhanCong;
                    model.BaoCaoQuaHan = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.BaoCao bc WHERE bc.TrangThai=@QuaHanStatus AND bc.KhoaPhongId=@KhoaPhongId", Param("@QuaHanStatus", (byte)TrangThaiBaoCao.QuaHan), Param("@KhoaPhongId", departmentId)));
                }
            }

            model.BaoCaoThieu = model.ChiSoDuocPhanCong - model.BaoCaoDaGui;

            if (admin)
            {
                string query;
                List<SqlParameter> sqlParams = new List<SqlParameter>();
                
                if (tanSuatFilter.HasValue)
                {
                    query = @"
SELECT 
    kp.TenKhoaPhong,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = @TanSuat) AS Tong,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = @TanSuat AND bc.TrangThai IN (2,3,4)) AS DaGui,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = @TanSuat AND bc.TrangThai = 1) AS LuuNhap,
    
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 3) AS TongThang,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 3 AND bc.TrangThai IN (2,3,4)) AS DaGuiThang,

    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 4) AS TongQuy,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 4 AND bc.TrangThai IN (2,3,4)) AS DaGuiQuy,

    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 6) AS TongNam,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 6 AND bc.TrangThai IN (2,3,4)) AS DaGuiNam
FROM dbo.KhoaPhong kp
ORDER BY kp.TenKhoaPhong";
                    sqlParams.Add(Param("@TanSuat", tanSuatFilter.Value));
                }
                else
                {
                    query = @"
SELECT 
    kp.TenKhoaPhong,
    (SELECT COUNT(*) FROM dbo.PhanCongChiSo pc WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1) AS Tong,
    (SELECT COUNT(*) FROM dbo.PhanCongChiSo pc JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND bc.TrangThai IN (2,3,4)) AS DaGui,
    (SELECT COUNT(*) FROM dbo.PhanCongChiSo pc JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND bc.TrangThai = 1) AS LuuNhap,
    
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 3) AS TongThang,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 3 AND bc.TrangThai IN (2,3,4)) AS DaGuiThang,

    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 4) AS TongQuy,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 4 AND bc.TrangThai IN (2,3,4)) AS DaGuiQuy,

    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 6) AS TongNam,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 6 AND bc.TrangThai IN (2,3,4)) AS DaGuiNam
FROM dbo.KhoaPhong kp
ORDER BY kp.TenKhoaPhong";
                }

                model.DepartmentProgress = Query(query, r => new DepartmentProgressViewModel
                {
                    TenKhoaPhong = String(r, "TenKhoaPhong"),
                    Tong = Int(r, "Tong"),
                    DaGui = Int(r, "DaGui"),
                    LuuNhap = Int(r, "LuuNhap"),
                    
                    TongThang = Int(r, "TongThang"),
                    DaGuiThang = Int(r, "DaGuiThang"),
                    
                    TongQuy = Int(r, "TongQuy"),
                    DaGuiQuy = Int(r, "DaGuiQuy"),
                    
                    TongNam = Int(r, "TongNam"),
                    DaGuiNam = Int(r, "DaGuiNam")
                }, sqlParams.ToArray());

                foreach (var progress in model.DepartmentProgress)
                {
                    progress.XepLoai = CalculateXepLoai(progress.DaGui, progress.Tong);
                    progress.XepLoaiThang = CalculateXepLoai(progress.DaGuiThang, progress.TongThang);
                    progress.XepLoaiQuy = CalculateXepLoai(progress.DaGuiQuy, progress.TongQuy);
                    progress.XepLoaiNam = CalculateXepLoai(progress.DaGuiNam, progress.TongNam);
                }
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
                    string query;
                    List<SqlParameter> sqlParams = new List<SqlParameter>();
                    sqlParams.Add(Param("@KhoaPhongId", departmentId.Value));
                    
                    if (tanSuatFilter.HasValue)
                    {
                        query = @"
SELECT 
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = @TanSuat) AS Tong,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = @KhoaPhongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = @TanSuat AND bc.TrangThai IN (2,3,4)) AS DaGui,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = @KhoaPhongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = @TanSuat AND bc.TrangThai = 1) AS LuuNhap,
    
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 3) AS TongThang,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = @KhoaPhongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 3 AND bc.TrangThai IN (2,3,4)) AS DaGuiThang,

    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 4) AS TongQuy,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = @KhoaPhongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 4 AND bc.TrangThai IN (2,3,4)) AS DaGuiQuy,

    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 6) AS TongNam,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = @KhoaPhongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 6 AND bc.TrangThai IN (2,3,4)) AS DaGuiNam";
                        sqlParams.Add(Param("@TanSuat", tanSuatFilter.Value));
                    }
                    else
                    {
                        query = @"
SELECT 
    (SELECT COUNT(*) FROM dbo.PhanCongChiSo pc WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1) AS Tong,
    (SELECT COUNT(*) FROM dbo.PhanCongChiSo pc JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = @KhoaPhongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND bc.TrangThai IN (2,3,4)) AS DaGui,
    (SELECT COUNT(*) FROM dbo.PhanCongChiSo pc JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = @KhoaPhongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND bc.TrangThai = 1) AS LuuNhap,
    
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 3) AS TongThang,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = @KhoaPhongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 3 AND bc.TrangThai IN (2,3,4)) AS DaGuiThang,

    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 4) AS TongQuy,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = @KhoaPhongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 4 AND bc.TrangThai IN (2,3,4)) AS DaGuiQuy,

    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 6) AS TongNam,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = @KhoaPhongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 6 AND bc.TrangThai IN (2,3,4)) AS DaGuiNam";
                    }

                    var progress = Query(query, r => new DepartmentProgressViewModel
                    {
                        TenKhoaPhong = departmentName,
                        Tong = Int(r, "Tong"),
                        DaGui = Int(r, "DaGui"),
                        LuuNhap = Int(r, "LuuNhap"),
                        
                        TongThang = Int(r, "TongThang"),
                        DaGuiThang = Int(r, "DaGuiThang"),
                        
                        TongQuy = Int(r, "TongQuy"),
                        DaGuiQuy = Int(r, "DaGuiQuy"),
                        
                        TongNam = Int(r, "TongNam"),
                        DaGuiNam = Int(r, "DaGuiNam")
                    }, sqlParams.ToArray()).FirstOrDefault();

                    if (progress != null)
                    {
                        progress.XepLoai = CalculateXepLoai(progress.DaGui, progress.Tong);
                        progress.XepLoaiThang = CalculateXepLoai(progress.DaGuiThang, progress.TongThang);
                        progress.XepLoaiQuy = CalculateXepLoai(progress.DaGuiQuy, progress.TongQuy);
                        progress.XepLoaiNam = CalculateXepLoai(progress.DaGuiNam, progress.TongNam);
                        model.DepartmentProgress.Add(progress);
                    }
                }
            }

            return model;
        }

        #pragma warning restore 0162
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
