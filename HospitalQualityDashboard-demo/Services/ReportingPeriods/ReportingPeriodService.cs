using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.Enums;
using HospitalQualityDashboardDemo.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Services
{
    public class ReportingPeriodService : DbServiceBase
    {
        // Truy vấn kỳ báo cáo theo điều kiện được cung cấp.
        public IList<KyBaoCaoViewModel> GetAll()
        {
            const string sql = @"
SELECT ky.KyBaoCaoId, ky.TenKyBaoCao, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay, ky.HanNop, ky.TrangThai,
       COUNT(bc.BaoCaoId) AS TongBaoCao,
       ISNULL(SUM(CASE WHEN bc.TrangThai IN (2,3,4) THEN 1 ELSE 0 END), 0) AS DaGui
FROM dbo.KyBaoCao ky
LEFT JOIN dbo.BaoCao bc ON bc.KyBaoCaoId = ky.KyBaoCaoId
GROUP BY ky.KyBaoCaoId, ky.TenKyBaoCao, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay, ky.HanNop, ky.TrangThai
ORDER BY ky.TuNgay DESC";
            return Query(sql, MapPeriod);
        }

        // Truy vấn kỳ báo cáo theo điều kiện được cung cấp.
        public IList<KyBaoCaoViewModel> GetAll(int page, int pageSize, out int totalItems)
        {
            page = NormalizePage(page);
            pageSize = NormalizePageSize(pageSize);
            totalItems = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.KyBaoCao"));

            const string sql = @"
SELECT ky.KyBaoCaoId, ky.TenKyBaoCao, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay, ky.HanNop, ky.TrangThai,
       COUNT(bc.BaoCaoId) AS TongBaoCao,
       ISNULL(SUM(CASE WHEN bc.TrangThai IN (2,3,4) THEN 1 ELSE 0 END), 0) AS DaGui
FROM dbo.KyBaoCao ky
LEFT JOIN dbo.BaoCao bc ON bc.KyBaoCaoId = ky.KyBaoCaoId
GROUP BY ky.KyBaoCaoId, ky.TenKyBaoCao, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay, ky.HanNop, ky.TrangThai
ORDER BY ky.TuNgay DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            return Query(sql, MapPeriod,
                Param("@Offset", (page - 1) * pageSize),
                Param("@PageSize", pageSize));
        }

        // Truy vấn kỳ báo cáo theo điều kiện được cung cấp.
        public IList<TanSuatBaoCao> GetFrequenciesForDepartment(int departmentId)
        {
            const string sql = @"
SELECT DISTINCT tsb.TanSuatBaoCao
FROM dbo.PhanCongChiSo pc
INNER JOIN dbo.ChiSoTanSuatBaoCao tsb ON tsb.ChiSoChatLuongId = pc.ChiSoChatLuongId
WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1";

            return Query(sql, r => (TanSuatBaoCao)r.GetByte(0), Param("@KhoaPhongId", departmentId));
        }

        // Truy vấn kỳ báo cáo theo điều kiện được cung cấp.
        public IList<SelectListItem> GetOptions()
        {
            return DropdownCache.GetOrAdd("dropdown:periods", () => Query(@"
SELECT KyBaoCaoId, TenKyBaoCao
FROM dbo.KyBaoCao
ORDER BY TuNgay DESC",
                r => new SelectListItem
                {
                    Value = Int(r, "KyBaoCaoId").ToString(),
                    Text = String(r, "TenKyBaoCao")
                }).ToList());
        }

        // Xác định dữ liệu có thỏa điều kiện nghiệp vụ của kỳ báo cáo hay không.
        public bool IsOpenForDepartment(int periodId, int departmentId)
        {
            var count = Convert.ToInt32(Scalar(@"
SELECT COUNT(DISTINCT ky.KyBaoCaoId)
FROM dbo.KyBaoCao ky
INNER JOIN dbo.PhanCongChiSo pc ON pc.KhoaPhongId=@KhoaPhongId AND pc.DangHoatDong=1
INNER JOIN dbo.ChiSoTanSuatBaoCao cst ON cst.ChiSoChatLuongId = pc.ChiSoChatLuongId AND cst.TanSuatBaoCao = ky.LoaiKyBaoCao
WHERE ky.KyBaoCaoId=@KyBaoCaoId AND ky.TrangThai=@Mo",
                Param("@KyBaoCaoId", periodId),
                Param("@KhoaPhongId", departmentId),
                Param("@Mo", (byte)TrangThaiKyBaoCao.Mo)));

            return count > 0;
        }

        // Truy vấn kỳ báo cáo theo điều kiện được cung cấp.
        public KyBaoCaoViewModel Get(int id)
        {
            return QuerySingle(@"SELECT KyBaoCaoId, TenKyBaoCao, LoaiKyBaoCao, TuNgay, DenNgay, HanNop, TrangThai, 0 AS TongBaoCao, 0 AS DaGui FROM dbo.KyBaoCao WHERE KyBaoCaoId=@Id",
                MapPeriod, Param("@Id", id));
        }

        // Kiểm tra và cập nhật dữ liệu của kỳ báo cáo.
        public void Save(ReportingPeriodSaveDto dto)
        {
            Save(new KyBaoCaoViewModel
            {
                KyBaoCaoId = dto.KyBaoCaoId,
                TenKyBaoCao = dto.TenKyBaoCao,
                LoaiKyBaoCao = dto.LoaiKyBaoCao,
                TuNgay = dto.TuNgay,
                DenNgay = dto.DenNgay,
                HanNop = dto.HanNop,
                TrangThai = dto.TrangThai
            });
        }

        // Kiểm tra và cập nhật dữ liệu của kỳ báo cáo.
        public void Save(KyBaoCaoViewModel model)
        {
            if (model.TuNgay > model.DenNgay || model.HanNop < model.DenNgay)
            {
                throw new InvalidOperationException("Ngày báo cáo và hạn nộp không hợp lệ.");
            }

            if (model.KyBaoCaoId == 0)
            {
                Execute(@"INSERT INTO dbo.KyBaoCao(TenKyBaoCao, LoaiKyBaoCao, TuNgay, DenNgay, HanNop, TrangThai)
VALUES(@TenKyBaoCao, @LoaiKyBaoCao, @TuNgay, @DenNgay, @HanNop, @TrangThai)",
                    PeriodParams(model));
                DropdownCache.Remove("dropdown:periods");
                return;
            }

            var parameters = PeriodParams(model).Concat(new[] { Param("@KyBaoCaoId", model.KyBaoCaoId) }).ToArray();
            Execute(@"UPDATE dbo.KyBaoCao SET TenKyBaoCao=@TenKyBaoCao, LoaiKyBaoCao=@LoaiKyBaoCao, TuNgay=@TuNgay, DenNgay=@DenNgay,
HanNop=@HanNop, TrangThai=@TrangThai, NgayCapNhat=GETDATE() WHERE KyBaoCaoId=@KyBaoCaoId", parameters);
            DropdownCache.Remove("dropdown:periods");
        }

        // Kiểm tra và cập nhật dữ liệu của kỳ báo cáo.
        public void SetStatus(int id, TrangThaiKyBaoCao status)
        {
            Execute("UPDATE dbo.KyBaoCao SET TrangThai=@TrangThai, NgayCapNhat=GETDATE() WHERE KyBaoCaoId=@Id", Param("@TrangThai", (byte)status), Param("@Id", id));
            DropdownCache.Remove("dropdown:periods");
        }

        // Xóa bản ghi được chọn sau khi áp dụng các ràng buộc của kỳ báo cáo.
        public void Delete(int id)
        {
            var dependentCount = Convert.ToInt32(Scalar(@"
SELECT
    (SELECT COUNT(*) FROM dbo.BaoCao WHERE KyBaoCaoId=@Id) +
    (SELECT COUNT(*) FROM dbo.ThongBao WHERE KyBaoCaoId=@Id)",
                Param("@Id", id)));
            if (dependentCount > 0)
            {
                throw new InvalidOperationException("Kỳ báo cáo đã có dữ liệu liên quan, vui lòng khóa thay vì xóa.");
            }

            Execute("DELETE FROM dbo.KyBaoCao WHERE KyBaoCaoId=@Id", Param("@Id", id));
            DropdownCache.Remove("dropdown:periods");
        }

        // Tạo tập tham số SQL từ model để dùng cho thao tác ghi dữ liệu.
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

        // Chuyển dữ liệu nguồn sang cấu trúc dùng cho kỳ báo cáo.
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

        // Chuẩn hóa dữ liệu đầu vào trước khi dùng cho kỳ báo cáo.
        private static int NormalizePage(int page)
        {
            return page < 1 ? 1 : page;
        }

        // Chuẩn hóa dữ liệu đầu vào trước khi dùng cho kỳ báo cáo.
        private static int NormalizePageSize(int pageSize)
        {
            if (pageSize < 1) return 20;
            return pageSize > 100 ? 100 : pageSize;
        }
    }
}
