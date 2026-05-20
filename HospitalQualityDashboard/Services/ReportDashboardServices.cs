using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using HospitalQualityDashboard.Models.Enums;
using HospitalQualityDashboard.Models.ViewModels;

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

        public IList<ReportEntryViewModel> GetAll(int? periodId, int? departmentId, int? indicatorId, bool admin, int? currentDepartmentId)
        {
            const string sql = @"
SELECT bc.BaoCaoId, bc.KyBaoCaoId, bc.KhoaPhongId, bc.ChiSoChatLuongId, ky.TenKyBaoCao, kp.TenKhoaPhong,
       cs.MaChiSo, cs.TenChiSo, cs.LoaiCongThuc, bc.TrangThai,
       ct.TuSo, ct.MauSo, ct.GiaTriNhap, ct.KetQua, ct.DatMucTieu, ct.GhiChu
FROM dbo.BaoCao bc
INNER JOIN dbo.KyBaoCao ky ON ky.KyBaoCaoId = bc.KyBaoCaoId
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = bc.KhoaPhongId
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = bc.ChiSoChatLuongId
LEFT JOIN dbo.BaoCaoChiTiet ct ON ct.BaoCaoId = bc.BaoCaoId
WHERE (@KyBaoCaoId IS NULL OR bc.KyBaoCaoId = @KyBaoCaoId)
  AND (@KhoaPhongId IS NULL OR bc.KhoaPhongId = @KhoaPhongId)
  AND (@ChiSoChatLuongId IS NULL OR bc.ChiSoChatLuongId = @ChiSoChatLuongId)
  AND (@IsAdmin = 1 OR bc.KhoaPhongId = @CurrentKhoaPhongId)
ORDER BY ky.TuNgay DESC, kp.TenKhoaPhong, cs.MaChiSo";
            return Query(sql, MapReport,
                Param("@KyBaoCaoId", periodId),
                Param("@KhoaPhongId", departmentId),
                Param("@ChiSoChatLuongId", indicatorId),
                Param("@IsAdmin", admin),
                Param("@CurrentKhoaPhongId", currentDepartmentId));
        }

        public IList<ReportEntryViewModel> GetAssignedForUser(int periodId, int departmentId)
        {
            const string sql = @"
SELECT ISNULL(bc.BaoCaoId, 0) AS BaoCaoId, @KyBaoCaoId AS KyBaoCaoId, pc.KhoaPhongId, pc.ChiSoChatLuongId,
       ky.TenKyBaoCao, kp.TenKhoaPhong, cs.MaChiSo, cs.TenChiSo, cs.LoaiCongThuc,
       ISNULL(bc.TrangThai, CASE WHEN GETDATE() > ky.HanNop THEN 3 ELSE 1 END) AS TrangThai,
       ct.TuSo, ct.MauSo, ct.GiaTriNhap, ct.KetQua, ct.DatMucTieu, ct.GhiChu
FROM dbo.PhanCongChiSo pc
INNER JOIN dbo.KyBaoCao ky ON ky.KyBaoCaoId = @KyBaoCaoId
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = pc.KhoaPhongId
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = pc.ChiSoChatLuongId
LEFT JOIN dbo.BaoCao bc ON bc.KyBaoCaoId = ky.KyBaoCaoId AND bc.KhoaPhongId = pc.KhoaPhongId AND bc.ChiSoChatLuongId = pc.ChiSoChatLuongId
LEFT JOIN dbo.BaoCaoChiTiet ct ON ct.BaoCaoId = bc.BaoCaoId
WHERE pc.DangHoatDong = 1 AND pc.KhoaPhongId = @KhoaPhongId
ORDER BY cs.MaChiSo";
            return Query(sql, MapReport, Param("@KyBaoCaoId", periodId), Param("@KhoaPhongId", departmentId));
        }

        public ReportEntryViewModel Get(int id)
        {
            return GetAll(null, null, null, true, null).FirstOrDefault(x => x.BaoCaoId == id);
        }

        public int SaveDraft(ReportEntryViewModel model, int userId)
        {
            var indicator = _indicators.Get(model.ChiSoChatLuongId);
            _calculator.Calculate(model, indicator);

            if (model.BaoCaoId == 0)
            {
                model.BaoCaoId = Convert.ToInt32(Scalar(@"INSERT INTO dbo.BaoCao(KyBaoCaoId, KhoaPhongId, ChiSoChatLuongId, TrangThai, NguoiTaoId)
OUTPUT INSERTED.BaoCaoId VALUES(@KyBaoCaoId, @KhoaPhongId, @ChiSoChatLuongId, @TrangThai, @NguoiTaoId)",
                    Param("@KyBaoCaoId", model.KyBaoCaoId),
                    Param("@KhoaPhongId", model.KhoaPhongId),
                    Param("@ChiSoChatLuongId", model.ChiSoChatLuongId),
                    Param("@TrangThai", (byte)TrangThaiBaoCao.Nhap),
                    Param("@NguoiTaoId", userId)));
            }
            else
            {
                var status = Convert.ToInt32(Scalar("SELECT TrangThai FROM dbo.BaoCao WHERE BaoCaoId=@Id", Param("@Id", model.BaoCaoId)));
                if (status != (byte)TrangThaiBaoCao.Nhap)
                {
                    throw new InvalidOperationException("Chi duoc sua bao cao o trang thai Nhap.");
                }
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

            return model.BaoCaoId;
        }

        public void Submit(int id, int userId)
        {
            Execute(@"UPDATE dbo.BaoCao SET TrangThai=@TrangThai, NguoiGuiId=@NguoiGuiId, NgayGui=GETDATE(), NgayCapNhat=GETDATE()
WHERE BaoCaoId=@Id AND TrangThai=@Nhap",
                Param("@TrangThai", (byte)TrangThaiBaoCao.DaGui),
                Param("@NguoiGuiId", userId),
                Param("@Id", id),
                Param("@Nhap", (byte)TrangThaiBaoCao.Nhap));
        }

        public void Lock(int id)
        {
            Execute("UPDATE dbo.BaoCao SET TrangThai=@TrangThai, NgayCapNhat=GETDATE() WHERE BaoCaoId=@Id AND TrangThai=@DaGui",
                Param("@TrangThai", (byte)TrangThaiBaoCao.DaKhoa),
                Param("@Id", id),
                Param("@DaGui", (byte)TrangThaiBaoCao.DaGui));
        }

        private static ReportEntryViewModel MapReport(SqlDataReader reader)
        {
            return new ReportEntryViewModel
            {
                BaoCaoId = Int(reader, "BaoCaoId"),
                KyBaoCaoId = Int(reader, "KyBaoCaoId"),
                KhoaPhongId = Int(reader, "KhoaPhongId"),
                ChiSoChatLuongId = Int(reader, "ChiSoChatLuongId"),
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
                GhiChu = String(reader, "GhiChu")
            };
        }
    }

    public class DashboardService : DbServiceBase
    {
        public DashboardViewModel GetDashboard(bool admin, int? departmentId)
        {
            var model = new DashboardViewModel { IsAdmin = admin, DepartmentProgress = new List<DepartmentProgressViewModel>() };
            model.TongChiSo = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.ChiSoChatLuong WHERE DangHoatDong=1"));
            model.BaoCaoDaGui = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.BaoCao WHERE TrangThai IN (2,4) AND (@IsAdmin=1 OR KhoaPhongId=@KhoaPhongId)", Param("@IsAdmin", admin), Param("@KhoaPhongId", departmentId)));
            model.ChiSoDuocPhanCong = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.PhanCongChiSo WHERE DangHoatDong=1 AND (@IsAdmin=1 OR KhoaPhongId=@KhoaPhongId)", Param("@IsAdmin", admin), Param("@KhoaPhongId", departmentId)));
            model.BaoCaoQuaHan = Convert.ToInt32(Scalar(@"SELECT COUNT(*) FROM dbo.KyBaoCao ky CROSS JOIN dbo.PhanCongChiSo pc
WHERE pc.DangHoatDong=1 AND GETDATE()>ky.HanNop AND NOT EXISTS(SELECT 1 FROM dbo.BaoCao bc WHERE bc.KyBaoCaoId=ky.KyBaoCaoId AND bc.KhoaPhongId=pc.KhoaPhongId AND bc.ChiSoChatLuongId=pc.ChiSoChatLuongId AND bc.TrangThai IN (2,4))
AND (@IsAdmin=1 OR pc.KhoaPhongId=@KhoaPhongId)", Param("@IsAdmin", admin), Param("@KhoaPhongId", departmentId)));
            model.BaoCaoThieu = model.ChiSoDuocPhanCong - model.BaoCaoDaGui;

            if (admin)
            {
                model.DepartmentProgress = Query(@"SELECT kp.TenKhoaPhong, COUNT(pc.PhanCongChiSoId) AS Tong,
ISNULL(SUM(CASE WHEN bc.TrangThai IN (2,4) THEN 1 ELSE 0 END), 0) AS DaGui
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

            return model;
        }
    }
}
