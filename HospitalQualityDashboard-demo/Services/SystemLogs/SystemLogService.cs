// Mục đích: đọc nhật ký thao tác hệ thống cho màn hình quản trị.
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Services
{
    public class SystemLogService : DbServiceBase
    {
        public IList<SystemLogRowViewModel> GetAll(SystemLogQueryDto query, int page, int pageSize, out int totalItems)
        {
            query = query ?? new SystemLogQueryDto();
            page = NormalizePage(page);
            pageSize = NormalizePageSize(pageSize);

            totalItems = Convert.ToInt32(Scalar(@"
SELECT COUNT(*)
FROM dbo.NhatKyHeThong logs
LEFT JOIN dbo.TaiKhoan accounts ON logs.TaiKhoanId = accounts.TaiKhoanId
LEFT JOIN dbo.NhanVien employees ON accounts.NhanVienId = employees.NhanVienId
WHERE (@AccountKeyword IS NULL OR accounts.TenDangNhap LIKE @AccountKeywordLike OR employees.HoTen LIKE @AccountKeywordLike)
  AND (@Module IS NULL OR logs.ChucNang = @Module)
  AND (@LogAction IS NULL OR logs.HanhDong = @LogAction)
  AND (@FromDate IS NULL OR logs.ThoiGian >= @FromDate)
  AND (@ToDate IS NULL OR logs.ThoiGian < DATEADD(day, 1, @ToDate))",
                BuildFilterParameters(query).ToArray()));

            var pagedParameters = BuildFilterParameters(query)
                .Concat(new[]
                {
                    Param("@Offset", (page - 1) * pageSize),
                    Param("@PageSize", pageSize)
                })
                .ToArray();

            return Query(@"
SELECT logs.NhatKyHeThongId,
       logs.TaiKhoanId,
       accounts.TenDangNhap,
       employees.HoTen,
       logs.ChucNang,
       logs.HanhDong,
       logs.DoiTuong,
       logs.DoiTuongId,
       logs.NoiDung,
       logs.ThoiGian
FROM dbo.NhatKyHeThong logs
LEFT JOIN dbo.TaiKhoan accounts ON logs.TaiKhoanId = accounts.TaiKhoanId
LEFT JOIN dbo.NhanVien employees ON accounts.NhanVienId = employees.NhanVienId
WHERE (@AccountKeyword IS NULL OR accounts.TenDangNhap LIKE @AccountKeywordLike OR employees.HoTen LIKE @AccountKeywordLike)
  AND (@Module IS NULL OR logs.ChucNang = @Module)
  AND (@LogAction IS NULL OR logs.HanhDong = @LogAction)
  AND (@FromDate IS NULL OR logs.ThoiGian >= @FromDate)
  AND (@ToDate IS NULL OR logs.ThoiGian < DATEADD(day, 1, @ToDate))
ORDER BY logs.ThoiGian DESC, logs.NhatKyHeThongId DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
                MapSystemLog,
                pagedParameters);
        }

        public IList<SelectListItem> GetModuleOptions()
        {
            return GetDistinctOptions("ChucNang");
        }

        public IList<SelectListItem> GetActionOptions()
        {
            return GetDistinctOptions("HanhDong");
        }

        private IList<SelectListItem> GetDistinctOptions(string columnName)
        {
            var sql = string.Format(@"
SELECT DISTINCT {0} AS Value
FROM dbo.NhatKyHeThong
WHERE {0} IS NOT NULL AND LTRIM(RTRIM({0})) <> N''
ORDER BY {0}", columnName);

            return Query(sql, reader =>
            {
                var value = String(reader, "Value");
                return new SelectListItem { Value = value, Text = value };
            });
        }

        private static IEnumerable<SqlParameter> BuildFilterParameters(SystemLogQueryDto query)
        {
            var accountKeyword = string.IsNullOrWhiteSpace(query.AccountKeyword) ? null : query.AccountKeyword.Trim();
            var module = string.IsNullOrWhiteSpace(query.Module) ? null : query.Module.Trim();
            var action = string.IsNullOrWhiteSpace(query.LogAction) ? null : query.LogAction.Trim();

            yield return Param("@AccountKeyword", accountKeyword);
            yield return Param("@AccountKeywordLike", accountKeyword == null ? null : "%" + accountKeyword + "%");
            yield return Param("@Module", module);
            yield return Param("@LogAction", action);
            yield return Param("@FromDate", query.FromDate);
            yield return Param("@ToDate", query.ToDate);
        }

        private static SystemLogRowViewModel MapSystemLog(SqlDataReader reader)
        {
            return new SystemLogRowViewModel
            {
                NhatKyHeThongId = Int(reader, "NhatKyHeThongId"),
                TaiKhoanId = NullableInt(reader, "TaiKhoanId"),
                TenDangNhap = String(reader, "TenDangNhap"),
                HoTen = String(reader, "HoTen"),
                Module = String(reader, "ChucNang"),
                Action = String(reader, "HanhDong"),
                DoiTuong = String(reader, "DoiTuong"),
                DoiTuongId = NullableInt(reader, "DoiTuongId"),
                NoiDung = String(reader, "NoiDung"),
                ThoiGian = reader.GetDateTime(reader.GetOrdinal("ThoiGian"))
            };
        }

        private static int NormalizePage(int page)
        {
            return page < 1 ? 1 : page;
        }

        private static int NormalizePageSize(int pageSize)
        {
            if (pageSize < 1) return 10;
            return pageSize > 100 ? 100 : pageSize;
        }
    }
}
