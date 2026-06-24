using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.Enums;
using HospitalQualityDashboardDemo.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Services
{
    public partial class IndicatorService : DbServiceBase
    {
        private readonly ExcelImportExportService _excel = new ExcelImportExportService();

        // Truy vấn danh mục chỉ số chất lượng theo điều kiện được cung cấp.
        public IList<ChiSoViewModel> GetAll(bool includeInactive = true, int? filterKhoaPhongId = null)
        {
            string sql;
            if (filterKhoaPhongId.HasValue)
            {
                sql = @"
SELECT cs.ChiSoChatLuongId, cs.MaChiSo, cs.SoThuTu, cs.TenChiSo, cs.DinhNghia, cs.LinhVucApDung, cs.KhiaCanhChatLuong, cs.ThanhToChatLuong,
       cs.LyDoLuaChon, cs.PhuongPhapTinh, cs.TuSoMoTa, cs.MauSoMoTa, cs.NguonSoLieu, cs.ThuThapTongHop,
       cs.KhoaPhongThuThapId, cs.KhoaPhongTongHopId, cs.GiaTriSoLieu, cs.LoaiCongThuc, cs.DonViTinh, cs.DangHoatDong
FROM dbo.ChiSoChatLuong cs
INNER JOIN dbo.PhanCongChiSo pc ON pc.ChiSoChatLuongId = cs.ChiSoChatLuongId
WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1
  AND (@IncludeInactive = 1 OR cs.DangHoatDong = 1)
ORDER BY ISNULL(cs.SoThuTu, 9999), cs.MaChiSo";
            }
            else
            {
                sql = @"
SELECT ChiSoChatLuongId, MaChiSo, SoThuTu, TenChiSo, DinhNghia, LinhVucApDung, KhiaCanhChatLuong, ThanhToChatLuong,
       LyDoLuaChon, PhuongPhapTinh, TuSoMoTa, MauSoMoTa, NguonSoLieu, ThuThapTongHop,
       KhoaPhongThuThapId, KhoaPhongTongHopId, GiaTriSoLieu, LoaiCongThuc, DonViTinh, DangHoatDong
FROM dbo.ChiSoChatLuong
WHERE (@IncludeInactive = 1 OR DangHoatDong = 1)
ORDER BY ISNULL(SoThuTu, 9999), MaChiSo";
            }

            var items = Query(sql, MapIndicator,
                Param("@IncludeInactive", includeInactive),
                Param("@KhoaPhongId", filterKhoaPhongId));
            PopulateIndicatorFrequencies(items);
            return items;
        }

        // Truy vấn danh mục chỉ số chất lượng theo điều kiện được cung cấp.
        public IList<ChiSoViewModel> GetAll(bool includeInactive, int? filterKhoaPhongId, int page, int pageSize, out int totalItems)
        {
            page = NormalizePage(page);
            pageSize = NormalizePageSize(pageSize);

            string countSql;
            string sql;
            if (filterKhoaPhongId.HasValue)
            {
                countSql = @"
SELECT COUNT(*)
FROM dbo.ChiSoChatLuong cs
INNER JOIN dbo.PhanCongChiSo pc ON pc.ChiSoChatLuongId = cs.ChiSoChatLuongId
WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1
  AND (@IncludeInactive = 1 OR cs.DangHoatDong = 1)";

                sql = @"
SELECT cs.ChiSoChatLuongId, cs.MaChiSo, cs.SoThuTu, cs.TenChiSo, cs.DinhNghia, cs.LinhVucApDung, cs.KhiaCanhChatLuong, cs.ThanhToChatLuong,
       cs.LyDoLuaChon, cs.PhuongPhapTinh, cs.TuSoMoTa, cs.MauSoMoTa, cs.NguonSoLieu, cs.ThuThapTongHop,
       cs.KhoaPhongThuThapId, cs.KhoaPhongTongHopId, cs.GiaTriSoLieu, cs.LoaiCongThuc, cs.DonViTinh, cs.DangHoatDong
FROM dbo.ChiSoChatLuong cs
INNER JOIN dbo.PhanCongChiSo pc ON pc.ChiSoChatLuongId = cs.ChiSoChatLuongId
WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1
  AND (@IncludeInactive = 1 OR cs.DangHoatDong = 1)
ORDER BY ISNULL(cs.SoThuTu, 9999), cs.MaChiSo
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            }
            else
            {
                countSql = @"
SELECT COUNT(*)
FROM dbo.ChiSoChatLuong
WHERE (@IncludeInactive = 1 OR DangHoatDong = 1)";

                sql = @"
SELECT ChiSoChatLuongId, MaChiSo, SoThuTu, TenChiSo, DinhNghia, LinhVucApDung, KhiaCanhChatLuong, ThanhToChatLuong,
       LyDoLuaChon, PhuongPhapTinh, TuSoMoTa, MauSoMoTa, NguonSoLieu, ThuThapTongHop,
       KhoaPhongThuThapId, KhoaPhongTongHopId, GiaTriSoLieu, LoaiCongThuc, DonViTinh, DangHoatDong
FROM dbo.ChiSoChatLuong
WHERE (@IncludeInactive = 1 OR DangHoatDong = 1)
ORDER BY ISNULL(SoThuTu, 9999), MaChiSo
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            }

            var countParameters = new[]
            {
                Param("@IncludeInactive", includeInactive),
                Param("@KhoaPhongId", filterKhoaPhongId)
            };
            totalItems = Convert.ToInt32(Scalar(countSql, countParameters));

            var items = Query(sql, MapIndicator,
                Param("@IncludeInactive", includeInactive),
                Param("@KhoaPhongId", filterKhoaPhongId),
                Param("@Offset", (page - 1) * pageSize),
                Param("@PageSize", pageSize));
            PopulateIndicatorFrequencies(items);
            return items;
        }

        // Xác định dữ liệu có thỏa điều kiện nghiệp vụ của danh mục chỉ số chất lượng hay không.
        public bool IsAssigned(int indicatorId, int khoaPhongId)
        {
            var count = Convert.ToInt32(Scalar(@"
SELECT COUNT(*) FROM dbo.PhanCongChiSo
WHERE ChiSoChatLuongId = @IndicatorId AND KhoaPhongId = @KhoaPhongId AND DangHoatDong = 1",
                Param("@IndicatorId", indicatorId),
                Param("@KhoaPhongId", khoaPhongId)));
            return count > 0;
        }

        // Chuẩn hóa dữ liệu đầu vào trước khi dùng cho danh mục chỉ số chất lượng.
        private static int NormalizePage(int page)
        {
            return page < 1 ? 1 : page;
        }

        // Chuẩn hóa dữ liệu đầu vào trước khi dùng cho danh mục chỉ số chất lượng.
        private static int NormalizePageSize(int pageSize)
        {
            if (pageSize < 1) return 20;
            return pageSize > 100 ? 100 : pageSize;
        }

        // Truy vấn danh mục chỉ số chất lượng theo điều kiện được cung cấp.
        public IList<SelectListItem> GetOptions()
        {
            return DropdownCache.GetOrAdd("dropdown:indicators", () => Query(@"
SELECT ChiSoChatLuongId, MaChiSo, TenChiSo
FROM dbo.ChiSoChatLuong
WHERE DangHoatDong = 1
ORDER BY ISNULL(SoThuTu, 9999), MaChiSo",
                r => new SelectListItem
                {
                    Value = Int(r, "ChiSoChatLuongId").ToString(),
                    Text = String(r, "MaChiSo") + " - " + String(r, "TenChiSo")
                }).ToList());
        }

        // Truy vấn danh mục chỉ số chất lượng theo điều kiện được cung cấp.
        public ChiSoViewModel Get(int id)
        {
            return Get(id, null);
        }

        // Truy vấn danh mục chỉ số chất lượng theo điều kiện được cung cấp.
        public ChiSoViewModel Get(int id, int? targetYear)
        {
            var model = QuerySingle(@"SELECT ChiSoChatLuongId, MaChiSo, SoThuTu, TenChiSo, DinhNghia, LinhVucApDung, KhiaCanhChatLuong, ThanhToChatLuong,
LyDoLuaChon, PhuongPhapTinh, TuSoMoTa, MauSoMoTa, NguonSoLieu, ThuThapTongHop, KhoaPhongThuThapId, KhoaPhongTongHopId, GiaTriSoLieu, LoaiCongThuc, DonViTinh, DangHoatDong
FROM dbo.ChiSoChatLuong WHERE ChiSoChatLuongId = @Id", MapIndicator, Param("@Id", id));
            if (model == null)
            {
                return null;
            }

            PopulateIndicatorFrequencies(new[] { model });

            var targetSql = targetYear.HasValue
                ? @"SELECT TOP 1 Nam, ToanTuSoSanh, GiaTriMucTieu, MoTaMucTieu
FROM dbo.ChiSoMucTieu
WHERE ChiSoChatLuongId = @Id
ORDER BY
    CASE WHEN Nam = @TargetYear THEN 0 ELSE 1 END,
    CASE WHEN Nam <= @TargetYear THEN 0 ELSE 1 END,
    CASE WHEN Nam <= @TargetYear THEN Nam END DESC,
    Nam DESC"
                : "SELECT TOP 1 Nam, ToanTuSoSanh, GiaTriMucTieu, MoTaMucTieu FROM dbo.ChiSoMucTieu WHERE ChiSoChatLuongId = @Id ORDER BY Nam DESC";
            var targetParameters = targetYear.HasValue
                ? new[] { Param("@Id", id), Param("@TargetYear", targetYear.Value) }
                : new[] { Param("@Id", id) };
            var target = QuerySingle(targetSql,
                r => new ChiSoViewModel
                {
                    NamMucTieu = Int(r, "Nam"),
                    ToanTuSoSanh = String(r, "ToanTuSoSanh"),
                    GiaTriMucTieu = NullableDecimal(r, "GiaTriMucTieu"),
                    MoTaMucTieu = String(r, "MoTaMucTieu")
                },
                targetParameters);
            if (target != null)
            {
                model.NamMucTieu = target.NamMucTieu;
                model.ToanTuSoSanh = target.ToanTuSoSanh;
                model.GiaTriMucTieu = target.GiaTriMucTieu;
                model.MoTaMucTieu = target.MoTaMucTieu;
            }

            return model;
        }

        // Kiểm tra và cập nhật dữ liệu của danh mục chỉ số chất lượng.
        public void Save(IndicatorSaveDto dto)
        {
            Save(new ChiSoViewModel
            {
                ChiSoChatLuongId = dto.ChiSoChatLuongId,
                MaChiSo = dto.MaChiSo,
                SoThuTu = dto.SoThuTu,
                TenChiSo = dto.TenChiSo,
                DinhNghia = dto.DinhNghia,
                LinhVucApDung = dto.LinhVucApDung,
                KhiaCanhChatLuong = dto.KhiaCanhChatLuong,
                ThanhToChatLuong = dto.ThanhToChatLuong,
                LyDoLuaChon = dto.LyDoLuaChon,
                PhuongPhapTinh = dto.PhuongPhapTinh,
                TuSoMoTa = dto.TuSoMoTa,
                MauSoMoTa = dto.MauSoMoTa,
                NguonSoLieu = dto.NguonSoLieu,
                ThuThapTongHop = dto.ThuThapTongHop,
                KhoaPhongThuThapId = dto.KhoaPhongThuThapId,
                KhoaPhongTongHopId = dto.KhoaPhongTongHopId,
                GiaTriSoLieu = dto.GiaTriSoLieu,
                TanSuatBaoCao = dto.TanSuatBaoCao,
                SelectedTanSuatBaoCaoValues = dto.SelectedTanSuatBaoCaoValues,
                TanSuatBaoCaos = dto.TanSuatBaoCaos,
                LoaiCongThuc = dto.LoaiCongThuc,
                DonViTinh = dto.DonViTinh,
                DangHoatDong = dto.DangHoatDong,
                NamMucTieu = dto.NamMucTieu,
                ToanTuSoSanh = dto.ToanTuSoSanh,
                GiaTriMucTieu = dto.GiaTriMucTieu,
                MoTaMucTieu = dto.MoTaMucTieu
            });
        }

        // Kiểm tra và cập nhật dữ liệu của danh mục chỉ số chất lượng.
        public void Save(ChiSoViewModel model)
        {
            ExecuteInTransaction((conn, trans) => Save(conn, trans, model));
            DropdownCache.Remove("dropdown:indicators");
        }

        // Kiểm tra và cập nhật dữ liệu của danh mục chỉ số chất lượng.
        public void Save(SqlConnection connection, SqlTransaction transaction, ChiSoViewModel model)
        {
            ApplySelectedFrequencies(model);
            if (model.ChiSoChatLuongId == 0)
            {
                var id = Convert.ToInt32(Scalar(connection, transaction, @"INSERT INTO dbo.ChiSoChatLuong(MaChiSo, SoThuTu, TenChiSo, DinhNghia, LinhVucApDung, KhiaCanhChatLuong,
ThanhToChatLuong, LyDoLuaChon, PhuongPhapTinh, TuSoMoTa, MauSoMoTa, NguonSoLieu, ThuThapTongHop, KhoaPhongThuThapId, KhoaPhongTongHopId, GiaTriSoLieu, LoaiCongThuc, DonViTinh, DangHoatDong)
OUTPUT INSERTED.ChiSoChatLuongId
VALUES(@MaChiSo, @SoThuTu, @TenChiSo, @DinhNghia, @LinhVucApDung, @KhiaCanhChatLuong, @ThanhToChatLuong, @LyDoLuaChon,
@PhuongPhapTinh, @TuSoMoTa, @MauSoMoTa, @NguonSoLieu, @ThuThapTongHop, @KhoaPhongThuThapId, @KhoaPhongTongHopId, @GiaTriSoLieu, @LoaiCongThuc, @DonViTinh, @DangHoatDong)",
                    IndicatorParams(model)));
                model.ChiSoChatLuongId = id;
            }
            else
            {
                var parameters = IndicatorParams(model).Concat(new[] { Param("@ChiSoChatLuongId", model.ChiSoChatLuongId) }).ToArray();
                Execute(connection, transaction, @"UPDATE dbo.ChiSoChatLuong SET MaChiSo=@MaChiSo, SoThuTu=@SoThuTu, TenChiSo=@TenChiSo, DinhNghia=@DinhNghia,
LinhVucApDung=@LinhVucApDung, KhiaCanhChatLuong=@KhiaCanhChatLuong, ThanhToChatLuong=@ThanhToChatLuong, LyDoLuaChon=@LyDoLuaChon,
PhuongPhapTinh=@PhuongPhapTinh, TuSoMoTa=@TuSoMoTa, MauSoMoTa=@MauSoMoTa, NguonSoLieu=@NguonSoLieu, ThuThapTongHop=@ThuThapTongHop,
KhoaPhongThuThapId=@KhoaPhongThuThapId, KhoaPhongTongHopId=@KhoaPhongTongHopId, GiaTriSoLieu=@GiaTriSoLieu, LoaiCongThuc=@LoaiCongThuc, DonViTinh=@DonViTinh, DangHoatDong=@DangHoatDong, NgayCapNhat=GETDATE()
WHERE ChiSoChatLuongId=@ChiSoChatLuongId", parameters);
            }

            SaveTarget(connection, transaction, model);
            SaveFrequencies(connection, transaction, model.ChiSoChatLuongId, model.TanSuatBaoCaos);
            ReconcileDeploymentHistory(connection, transaction, model.ChiSoChatLuongId, model.TanSuatBaoCaos, model.DangHoatDong);
        }

        // Kiểm tra và cập nhật dữ liệu của danh mục chỉ số chất lượng.
        public void SetActive(int id, bool active)
        {
            Execute("UPDATE dbo.ChiSoChatLuong SET DangHoatDong = @Active, NgayCapNhat = GETDATE() WHERE ChiSoChatLuongId = @Id", Param("@Active", active), Param("@Id", id));
            DropdownCache.Remove("dropdown:indicators");
        }

        // Xóa bản ghi được chọn sau khi áp dụng các ràng buộc của danh mục chỉ số chất lượng.
        public void Delete(int id)
        {
            ExecuteInTransaction((conn, trans) =>
            {
                var reportCount = Convert.ToInt32(Scalar(conn, trans, "SELECT COUNT(*) FROM dbo.BaoCao WHERE ChiSoChatLuongId=@Id", Param("@Id", id)));
                if (reportCount > 0)
                {
                    throw new InvalidOperationException("Chỉ số đã có báo cáo, vui lòng khóa thay vì xóa.");
                }

                Execute(conn, trans, "DELETE FROM dbo.ChiSoTanSuatBaoCao WHERE ChiSoChatLuongId=@Id", Param("@Id", id));
                Execute(conn, trans, "DELETE FROM dbo.PhanCongChiSo WHERE ChiSoChatLuongId=@Id", Param("@Id", id));
                Execute(conn, trans, "DELETE FROM dbo.ChiSoMucTieu WHERE ChiSoChatLuongId=@Id", Param("@Id", id));
                Execute(conn, trans, "DELETE FROM dbo.ChiSoChatLuong WHERE ChiSoChatLuongId=@Id", Param("@Id", id));
            });
            DropdownCache.Remove("dropdown:indicators");
        }

        // Đọc, kiểm tra và nhập dữ liệu từ tệp tải lên.
    }
}
