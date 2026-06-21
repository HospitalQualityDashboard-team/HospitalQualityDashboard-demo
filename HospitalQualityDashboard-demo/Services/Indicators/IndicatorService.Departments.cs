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
    public partial class IndicatorService
    {
        private int? FindIndicatorId(string code, string name)
        {
            if (!string.IsNullOrWhiteSpace(code))
            {
                var idByCode = Scalar("SELECT ChiSoChatLuongId FROM dbo.ChiSoChatLuong WHERE MaChiSo=@MaChiSo", Param("@MaChiSo", code.Trim()));
                if (idByCode != null)
                {
                    return Convert.ToInt32(idByCode);
                }
            }

            var idByName = Scalar("SELECT ChiSoChatLuongId FROM dbo.ChiSoChatLuong WHERE TenChiSo=@TenChiSo", Param("@TenChiSo", name.Trim()));
            return idByName == null ? (int?)null : Convert.ToInt32(idByName);
        }

        // Truy vấn danh mục chỉ số chất lượng theo điều kiện được cung cấp.
        private int? FindIndicatorId(SqlConnection connection, SqlTransaction transaction, string code, string name)
        {
            if (!string.IsNullOrWhiteSpace(code))
            {
                var idByCode = Scalar(connection, transaction, "SELECT ChiSoChatLuongId FROM dbo.ChiSoChatLuong WHERE MaChiSo=@MaChiSo", Param("@MaChiSo", code.Trim()));
                if (idByCode != null)
                {
                    return Convert.ToInt32(idByCode);
                }
            }

            var idByName = Scalar(connection, transaction, "SELECT ChiSoChatLuongId FROM dbo.ChiSoChatLuong WHERE TenChiSo=@TenChiSo", Param("@TenChiSo", name.Trim()));
            return idByName == null ? (int?)null : Convert.ToInt32(idByName);
        }

        // Truy vấn danh mục chỉ số chất lượng theo điều kiện được cung cấp.
        private IList<DepartmentLookup> GetDepartmentLookups()
        {
            return Query("SELECT KhoaPhongId, IdKhoaPhongNguon, TenKhoaPhong FROM dbo.KhoaPhong WHERE Used=1",
                r => new DepartmentLookup
                {
                    KhoaPhongId = Int(r, "KhoaPhongId"),
                    IdKhoaPhongNguon = Int(r, "IdKhoaPhongNguon"),
                    TenKhoaPhong = String(r, "TenKhoaPhong"),
                    NormalizedName = NormalizeKey(String(r, "TenKhoaPhong"))
                });
        }

        // Xác định giá trị phù hợp từ các nguồn dữ liệu của danh mục chỉ số chất lượng.
        internal static IList<int> ResolveDepartmentIds(string value, IList<DepartmentLookup> departments)
        {
            var ids = new List<int>();
            if (string.IsNullOrWhiteSpace(value))
            {
                return ids;
            }

            var tokens = value.Split(new[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                var normalizedToken = NormalizeKey(token);
                int sourceId;
                DepartmentLookup department = null;
                if (int.TryParse(token.Trim(), out sourceId))
                {
                    department = departments.FirstOrDefault(x => x.IdKhoaPhongNguon == sourceId);
                }

                if (department == null)
                {
                    var alias = ResolveDepartmentAlias(normalizedToken);
                    if (!string.IsNullOrWhiteSpace(alias))
                    {
                        department = departments.FirstOrDefault(x => x.NormalizedName == alias);
                    }
                }

                if (department == null)
                {
                    department = departments.FirstOrDefault(x => x.NormalizedName == normalizedToken)
                        ?? departments.FirstOrDefault(x => normalizedToken.Contains(x.NormalizedName) || x.NormalizedName.Contains(normalizedToken));
                }

                if (department != null && !ids.Contains(department.KhoaPhongId))
                {
                    ids.Add(department.KhoaPhongId);
                }
            }

            if (ids.Count == 0)
            {
                var normalizedValue = NormalizeKey(value);
                var alias = ResolveDepartmentAlias(normalizedValue);
                if (!string.IsNullOrWhiteSpace(alias))
                {
                    foreach (var department in departments.Where(x => x.NormalizedName == alias))
                    {
                        if (!ids.Contains(department.KhoaPhongId))
                        {
                            ids.Add(department.KhoaPhongId);
                        }
                    }
                }

                foreach (var department in departments.Where(x => normalizedValue.Contains(x.NormalizedName)))
                {
                    if (!ids.Contains(department.KhoaPhongId))
                    {
                        ids.Add(department.KhoaPhongId);
                    }
                }
            }

            AddAllMatchingDepartments(ids, NormalizeKey(value), departments);
            return ids;
        }

        // Bổ sung dữ liệu mới phục vụ danh mục chỉ số chất lượng.
        private static void AddAllMatchingDepartments(IList<int> ids, string normalizedValue, IList<DepartmentLookup> departments)
        {
            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                return;
            }

            foreach (var alias in ResolveDepartmentAliases(normalizedValue))
            {
                foreach (var department in departments.Where(x => x.NormalizedName == alias))
                {
                    AddDepartmentId(ids, department);
                }
            }

            foreach (var department in departments.Where(x => normalizedValue.Contains(x.NormalizedName)))
            {
                AddDepartmentId(ids, department);
            }
        }

        // Bổ sung dữ liệu mới phục vụ danh mục chỉ số chất lượng.
        private static void AddDepartmentId(IList<int> ids, DepartmentLookup department)
        {
            if (department != null && !ids.Contains(department.KhoaPhongId))
            {
                ids.Add(department.KhoaPhongId);
            }
        }

        // Xác định giá trị phù hợp từ các nguồn dữ liệu của danh mục chỉ số chất lượng.
        private static IEnumerable<string> ResolveDepartmentAliases(string normalizedValue)
        {
            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                yield break;
            }

            if (normalizedValue.Contains("ksnk") || normalizedValue.Contains("kiem soat nhiem khuan"))
            {
                yield return "kiem soat nhiem khuan";
            }

            if (normalizedValue.Contains("kht h") || normalizedValue.Contains("khth") || normalizedValue.Contains("ke hoach tong hop"))
            {
                yield return "ke hoach tong hop";
            }

            if (normalizedValue.Contains("tccb") || normalizedValue.Contains("to chuc can bo"))
            {
                yield return "to chuc can bo";
            }

            if (normalizedValue.Contains("hanh chanh quan tri") || normalizedValue.Contains("hcqt") || normalizedValue.Contains("hanh chinh quan tri"))
            {
                yield return "hanh chinh quan tri";
            }
        }

        // Xác định giá trị phù hợp từ các nguồn dữ liệu của danh mục chỉ số chất lượng.
        private static string ResolveDepartmentAlias(string normalizedValue)
        {
            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                return null;
            }

            if (normalizedValue.Contains("ksnk") || normalizedValue.Contains("kiem soat nhiem khuan"))
            {
                return NormalizeKey("Kiểm soát nhiễm khuẩn");
            }

            if (normalizedValue.Contains("kht h") || normalizedValue.Contains("khth") || normalizedValue.Contains("ke hoach tong hop"))
            {
                return NormalizeKey("Kế hoạch tổng hợp");
            }

            if (normalizedValue.Contains("tccb") || normalizedValue.Contains("to chuc can bo"))
            {
                return NormalizeKey("Tổ chức cán bộ");
            }

            if (normalizedValue.Contains("hanh chanh quan tri") || normalizedValue.Contains("hcqt") || normalizedValue.Contains("hanh chinh quan tri"))
            {
                return NormalizeKey("Hành chính quản trị");
            }

            return null;
        }

        // Truy vấn danh mục chỉ số chất lượng theo điều kiện được cung cấp.
        private static string GetDepartmentSource(IDictionary<string, string> row, string collectionText)
        {
            var explicitValue = GetValue(row, "KhoaPhongQuanLy", "KHOAPHONGQUANLY", "Khoa phong quan ly", "Khoa/Phong quan ly", "Đơn vị thu thập", "Don vi thu thap");
            return string.IsNullOrWhiteSpace(explicitValue) ? collectionText : explicitValue;
        }

        // Phân tích giá trị đầu vào và chuyển sang kiểu dữ liệu cần dùng.
        internal class DepartmentLookup
        {
            public int KhoaPhongId { get; set; }
            public int IdKhoaPhongNguon { get; set; }
            public string TenKhoaPhong { get; set; }
            public string NormalizedName { get; set; }
        }
    }
}
