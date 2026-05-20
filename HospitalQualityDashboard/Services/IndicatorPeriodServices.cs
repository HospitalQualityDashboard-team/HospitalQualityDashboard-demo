using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using HospitalQualityDashboard.Models.Enums;
using HospitalQualityDashboard.Models.ViewModels;

namespace HospitalQualityDashboard.Services
{
    public class IndicatorService : DbServiceBase
    {
        public IList<ChiSoViewModel> GetAll(bool includeInactive = true)
        {
            const string sql = @"
SELECT ChiSoChatLuongId, MaChiSo, SoThuTu, TenChiSo, DinhNghia, LinhVucApDung, KhiaCanhChatLuong, ThanhToChatLuong,
       LyDoLuaChon, PhuongPhapTinh, TuSoMoTa, MauSoMoTa, NguonSoLieu, ThuThapTongHop, GiaTriSoLieu,
       TanSuatBaoCao, LoaiCongThuc, DonViTinh, DangHoatDong
FROM dbo.ChiSoChatLuong
WHERE (@IncludeInactive = 1 OR DangHoatDong = 1)
ORDER BY ISNULL(SoThuTu, 9999), MaChiSo";
            return Query(sql, MapIndicator, Param("@IncludeInactive", includeInactive));
        }

        public IList<SelectListItem> GetOptions()
        {
            return GetAll(false).Select(x => new SelectListItem
            {
                Value = x.ChiSoChatLuongId.ToString(),
                Text = x.MaChiSo + " - " + x.TenChiSo
            }).ToList();
        }

        public ChiSoViewModel Get(int id)
        {
            var model = QuerySingle(@"SELECT ChiSoChatLuongId, MaChiSo, SoThuTu, TenChiSo, DinhNghia, LinhVucApDung, KhiaCanhChatLuong, ThanhToChatLuong,
LyDoLuaChon, PhuongPhapTinh, TuSoMoTa, MauSoMoTa, NguonSoLieu, ThuThapTongHop, GiaTriSoLieu, TanSuatBaoCao, LoaiCongThuc, DonViTinh, DangHoatDong
FROM dbo.ChiSoChatLuong WHERE ChiSoChatLuongId = @Id", MapIndicator, Param("@Id", id));
            if (model == null)
            {
                return null;
            }

            var target = QuerySingle("SELECT TOP 1 Nam, ToanTuSoSanh, GiaTriMucTieu, MoTaMucTieu FROM dbo.ChiSoMucTieu WHERE ChiSoChatLuongId = @Id ORDER BY Nam DESC",
                r => new ChiSoViewModel
                {
                    NamMucTieu = Int(r, "Nam"),
                    ToanTuSoSanh = String(r, "ToanTuSoSanh"),
                    GiaTriMucTieu = NullableDecimal(r, "GiaTriMucTieu"),
                    MoTaMucTieu = String(r, "MoTaMucTieu")
                },
                Param("@Id", id));
            if (target != null)
            {
                model.NamMucTieu = target.NamMucTieu;
                model.ToanTuSoSanh = target.ToanTuSoSanh;
                model.GiaTriMucTieu = target.GiaTriMucTieu;
                model.MoTaMucTieu = target.MoTaMucTieu;
            }

            return model;
        }

        public void Save(ChiSoViewModel model)
        {
            if (model.ChiSoChatLuongId == 0)
            {
                var id = Convert.ToInt32(Scalar(@"INSERT INTO dbo.ChiSoChatLuong(MaChiSo, SoThuTu, TenChiSo, DinhNghia, LinhVucApDung, KhiaCanhChatLuong,
ThanhToChatLuong, LyDoLuaChon, PhuongPhapTinh, TuSoMoTa, MauSoMoTa, NguonSoLieu, ThuThapTongHop, GiaTriSoLieu, TanSuatBaoCao, LoaiCongThuc, DonViTinh, DangHoatDong)
OUTPUT INSERTED.ChiSoChatLuongId
VALUES(@MaChiSo, @SoThuTu, @TenChiSo, @DinhNghia, @LinhVucApDung, @KhiaCanhChatLuong, @ThanhToChatLuong, @LyDoLuaChon,
@PhuongPhapTinh, @TuSoMoTa, @MauSoMoTa, @NguonSoLieu, @ThuThapTongHop, @GiaTriSoLieu, @TanSuatBaoCao, @LoaiCongThuc, @DonViTinh, @DangHoatDong)",
                    IndicatorParams(model)));
                model.ChiSoChatLuongId = id;
            }
            else
            {
                var parameters = IndicatorParams(model).Concat(new[] { Param("@ChiSoChatLuongId", model.ChiSoChatLuongId) }).ToArray();
                Execute(@"UPDATE dbo.ChiSoChatLuong SET MaChiSo=@MaChiSo, SoThuTu=@SoThuTu, TenChiSo=@TenChiSo, DinhNghia=@DinhNghia,
LinhVucApDung=@LinhVucApDung, KhiaCanhChatLuong=@KhiaCanhChatLuong, ThanhToChatLuong=@ThanhToChatLuong, LyDoLuaChon=@LyDoLuaChon,
PhuongPhapTinh=@PhuongPhapTinh, TuSoMoTa=@TuSoMoTa, MauSoMoTa=@MauSoMoTa, NguonSoLieu=@NguonSoLieu, ThuThapTongHop=@ThuThapTongHop,
GiaTriSoLieu=@GiaTriSoLieu, TanSuatBaoCao=@TanSuatBaoCao, LoaiCongThuc=@LoaiCongThuc, DonViTinh=@DonViTinh, DangHoatDong=@DangHoatDong, NgayCapNhat=GETDATE()
WHERE ChiSoChatLuongId=@ChiSoChatLuongId", parameters);
            }

            SaveTarget(model);
        }

        public void SetActive(int id, bool active)
        {
            Execute("UPDATE dbo.ChiSoChatLuong SET DangHoatDong = @Active, NgayCapNhat = GETDATE() WHERE ChiSoChatLuongId = @Id", Param("@Active", active), Param("@Id", id));
        }

        private void SaveTarget(ChiSoViewModel model)
        {
            if (!model.NamMucTieu.HasValue || string.IsNullOrWhiteSpace(model.ToanTuSoSanh))
            {
                return;
            }

            Execute(@"
IF EXISTS (SELECT 1 FROM dbo.ChiSoMucTieu WHERE ChiSoChatLuongId=@ChiSoChatLuongId AND Nam=@Nam)
    UPDATE dbo.ChiSoMucTieu SET ToanTuSoSanh=@ToanTuSoSanh, GiaTriMucTieu=@GiaTriMucTieu, MoTaMucTieu=@MoTaMucTieu WHERE ChiSoChatLuongId=@ChiSoChatLuongId AND Nam=@Nam
ELSE
    INSERT INTO dbo.ChiSoMucTieu(ChiSoChatLuongId, Nam, ToanTuSoSanh, GiaTriMucTieu, MoTaMucTieu) VALUES(@ChiSoChatLuongId, @Nam, @ToanTuSoSanh, @GiaTriMucTieu, @MoTaMucTieu)",
                Param("@ChiSoChatLuongId", model.ChiSoChatLuongId),
                Param("@Nam", model.NamMucTieu.Value),
                Param("@ToanTuSoSanh", model.ToanTuSoSanh),
                Param("@GiaTriMucTieu", model.GiaTriMucTieu),
                Param("@MoTaMucTieu", model.MoTaMucTieu));
        }

        private static SqlParameter[] IndicatorParams(ChiSoViewModel model)
        {
            return new[]
            {
                Param("@MaChiSo", model.MaChiSo),
                Param("@SoThuTu", model.SoThuTu),
                Param("@TenChiSo", model.TenChiSo),
                Param("@DinhNghia", model.DinhNghia),
                Param("@LinhVucApDung", model.LinhVucApDung),
                Param("@KhiaCanhChatLuong", model.KhiaCanhChatLuong),
                Param("@ThanhToChatLuong", model.ThanhToChatLuong),
                Param("@LyDoLuaChon", model.LyDoLuaChon),
                Param("@PhuongPhapTinh", model.PhuongPhapTinh),
                Param("@TuSoMoTa", model.TuSoMoTa),
                Param("@MauSoMoTa", model.MauSoMoTa),
                Param("@NguonSoLieu", model.NguonSoLieu),
                Param("@ThuThapTongHop", model.ThuThapTongHop),
                Param("@GiaTriSoLieu", model.GiaTriSoLieu),
                Param("@TanSuatBaoCao", (byte)model.TanSuatBaoCao),
                Param("@LoaiCongThuc", (byte)model.LoaiCongThuc),
                Param("@DonViTinh", model.DonViTinh),
                Param("@DangHoatDong", model.DangHoatDong)
            };
        }

        private static ChiSoViewModel MapIndicator(SqlDataReader reader)
        {
            return new ChiSoViewModel
            {
                ChiSoChatLuongId = Int(reader, "ChiSoChatLuongId"),
                MaChiSo = String(reader, "MaChiSo"),
                SoThuTu = NullableInt(reader, "SoThuTu"),
                TenChiSo = String(reader, "TenChiSo"),
                DinhNghia = String(reader, "DinhNghia"),
                LinhVucApDung = String(reader, "LinhVucApDung"),
                KhiaCanhChatLuong = String(reader, "KhiaCanhChatLuong"),
                ThanhToChatLuong = String(reader, "ThanhToChatLuong"),
                LyDoLuaChon = String(reader, "LyDoLuaChon"),
                PhuongPhapTinh = String(reader, "PhuongPhapTinh"),
                TuSoMoTa = String(reader, "TuSoMoTa"),
                MauSoMoTa = String(reader, "MauSoMoTa"),
                NguonSoLieu = String(reader, "NguonSoLieu"),
                ThuThapTongHop = String(reader, "ThuThapTongHop"),
                GiaTriSoLieu = String(reader, "GiaTriSoLieu"),
                TanSuatBaoCao = (TanSuatBaoCao)reader.GetByte(reader.GetOrdinal("TanSuatBaoCao")),
                LoaiCongThuc = (LoaiCongThuc)reader.GetByte(reader.GetOrdinal("LoaiCongThuc")),
                DonViTinh = String(reader, "DonViTinh"),
                DangHoatDong = reader.GetBoolean(reader.GetOrdinal("DangHoatDong"))
            };
        }
    }

    public class AssignmentService : DbServiceBase
    {
        public IList<AssignmentItemViewModel> GetAll(int? khoaPhongId = null)
        {
            const string sql = @"
SELECT pc.PhanCongChiSoId, kp.TenKhoaPhong, cs.MaChiSo, cs.TenChiSo, pc.DangHoatDong
FROM dbo.PhanCongChiSo pc
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = pc.KhoaPhongId
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = pc.ChiSoChatLuongId
WHERE (@KhoaPhongId IS NULL OR pc.KhoaPhongId = @KhoaPhongId)
ORDER BY kp.TenKhoaPhong, cs.MaChiSo";
            return Query(sql, r => new AssignmentItemViewModel
            {
                PhanCongChiSoId = Int(r, "PhanCongChiSoId"),
                TenKhoaPhong = String(r, "TenKhoaPhong"),
                MaChiSo = String(r, "MaChiSo"),
                TenChiSo = String(r, "TenChiSo"),
                DangHoatDong = r.GetBoolean(r.GetOrdinal("DangHoatDong"))
            }, Param("@KhoaPhongId", khoaPhongId));
        }

        public void Assign(int khoaPhongId, IEnumerable<int> indicatorIds, int currentUserId)
        {
            foreach (var indicatorId in indicatorIds ?? new int[0])
            {
                Execute(@"
IF EXISTS (SELECT 1 FROM dbo.PhanCongChiSo WHERE KhoaPhongId=@KhoaPhongId AND ChiSoChatLuongId=@ChiSoChatLuongId)
    UPDATE dbo.PhanCongChiSo SET DangHoatDong=1 WHERE KhoaPhongId=@KhoaPhongId AND ChiSoChatLuongId=@ChiSoChatLuongId
ELSE
    INSERT INTO dbo.PhanCongChiSo(KhoaPhongId, ChiSoChatLuongId, DangHoatDong, NguoiTaoId) VALUES(@KhoaPhongId, @ChiSoChatLuongId, 1, @NguoiTaoId)",
                    Param("@KhoaPhongId", khoaPhongId),
                    Param("@ChiSoChatLuongId", indicatorId),
                    Param("@NguoiTaoId", currentUserId));
            }
        }

        public void Deactivate(int id)
        {
            Execute("UPDATE dbo.PhanCongChiSo SET DangHoatDong = 0 WHERE PhanCongChiSoId = @Id", Param("@Id", id));
        }
    }

    public class ReportingPeriodService : DbServiceBase
    {
        public IList<KyBaoCaoViewModel> GetAll()
        {
            const string sql = @"
SELECT ky.KyBaoCaoId, ky.TenKyBaoCao, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay, ky.HanNop, ky.TrangThai,
       COUNT(bc.BaoCaoId) AS TongBaoCao,
       ISNULL(SUM(CASE WHEN bc.TrangThai IN (2,4) THEN 1 ELSE 0 END), 0) AS DaGui
FROM dbo.KyBaoCao ky
LEFT JOIN dbo.BaoCao bc ON bc.KyBaoCaoId = ky.KyBaoCaoId
GROUP BY ky.KyBaoCaoId, ky.TenKyBaoCao, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay, ky.HanNop, ky.TrangThai
ORDER BY ky.TuNgay DESC";
            return Query(sql, MapPeriod);
        }

        public IList<SelectListItem> GetOptions()
        {
            return GetAll().Select(x => new SelectListItem { Value = x.KyBaoCaoId.ToString(), Text = x.TenKyBaoCao }).ToList();
        }

        public KyBaoCaoViewModel Get(int id)
        {
            return QuerySingle(@"SELECT KyBaoCaoId, TenKyBaoCao, LoaiKyBaoCao, TuNgay, DenNgay, HanNop, TrangThai, 0 AS TongBaoCao, 0 AS DaGui FROM dbo.KyBaoCao WHERE KyBaoCaoId=@Id",
                MapPeriod, Param("@Id", id));
        }

        public void Save(KyBaoCaoViewModel model)
        {
            if (model.TuNgay > model.DenNgay || model.HanNop < model.DenNgay)
            {
                throw new InvalidOperationException("Ngay bao cao va han nop khong hop le.");
            }

            if (model.KyBaoCaoId == 0)
            {
                Execute(@"INSERT INTO dbo.KyBaoCao(TenKyBaoCao, LoaiKyBaoCao, TuNgay, DenNgay, HanNop, TrangThai)
VALUES(@TenKyBaoCao, @LoaiKyBaoCao, @TuNgay, @DenNgay, @HanNop, @TrangThai)",
                    PeriodParams(model));
                return;
            }

            var parameters = PeriodParams(model).Concat(new[] { Param("@KyBaoCaoId", model.KyBaoCaoId) }).ToArray();
            Execute(@"UPDATE dbo.KyBaoCao SET TenKyBaoCao=@TenKyBaoCao, LoaiKyBaoCao=@LoaiKyBaoCao, TuNgay=@TuNgay, DenNgay=@DenNgay,
HanNop=@HanNop, TrangThai=@TrangThai, NgayCapNhat=GETDATE() WHERE KyBaoCaoId=@KyBaoCaoId", parameters);
        }

        public void SetStatus(int id, TrangThaiKyBaoCao status)
        {
            Execute("UPDATE dbo.KyBaoCao SET TrangThai=@TrangThai, NgayCapNhat=GETDATE() WHERE KyBaoCaoId=@Id", Param("@TrangThai", (byte)status), Param("@Id", id));
        }

        private static SqlParameter[] PeriodParams(KyBaoCaoViewModel model)
        {
            return new[]
            {
                Param("@TenKyBaoCao", model.TenKyBaoCao),
                Param("@LoaiKyBaoCao", (byte)model.LoaiKyBaoCao),
                Param("@TuNgay", model.TuNgay),
                Param("@DenNgay", model.DenNgay),
                Param("@HanNop", model.HanNop),
                Param("@TrangThai", (byte)model.TrangThai)
            };
        }

        private static KyBaoCaoViewModel MapPeriod(SqlDataReader reader)
        {
            return new KyBaoCaoViewModel
            {
                KyBaoCaoId = Int(reader, "KyBaoCaoId"),
                TenKyBaoCao = String(reader, "TenKyBaoCao"),
                LoaiKyBaoCao = (TanSuatBaoCao)reader.GetByte(reader.GetOrdinal("LoaiKyBaoCao")),
                TuNgay = reader.GetDateTime(reader.GetOrdinal("TuNgay")),
                DenNgay = reader.GetDateTime(reader.GetOrdinal("DenNgay")),
                HanNop = reader.GetDateTime(reader.GetOrdinal("HanNop")),
                TrangThai = (TrangThaiKyBaoCao)reader.GetByte(reader.GetOrdinal("TrangThai")),
                TongBaoCao = Int(reader, "TongBaoCao"),
                DaGui = Int(reader, "DaGui")
            };
        }
    }
}
