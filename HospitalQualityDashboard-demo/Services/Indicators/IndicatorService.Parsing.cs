// Mục đích: phân tích dữ liệu thô từ tài liệu/Excel thành trường chỉ số có cấu trúc.
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
        // Xử lý chức năng chỉ số chất lượng của method GetTargetText, giữ logic nghiệp vụ tập trung trong tầng phù hợp.
        private static string GetTargetText(IDictionary<string, string> row)
        {
            var explicitValue = GetValue(row, "MucTieuDatDuoc", "MUCTIEUDATDUOC", "Muc tieu dat duoc", "Mục tiêu đạt được");
            if (!string.IsNullOrWhiteSpace(explicitValue))
            {
                return explicitValue;
            }

            foreach (var item in row)
            {
                var key = NormalizeKey(item.Key);
                if (key.Contains("muc tieu dat duoc") || key.Contains("muc tieu"))
                {
                    return item.Value;
                }
            }

            return null;
        }

        // Xử lý chức năng chỉ số chất lượng của method GetTargetYear, giữ logic nghiệp vụ tập trung trong tầng phù hợp.
        private static int? GetTargetYear(IDictionary<string, string> row)
        {
            foreach (var item in row)
            {
                var match = Regex.Match(item.Key ?? string.Empty, @"(19|20)\d{2}");
                int year;
                if (match.Success && int.TryParse(match.Value, out year))
                {
                    return year;
                }
            }

            return null;
        }

        // Xử lý chức năng chỉ số chất lượng của method GetValue, giữ logic nghiệp vụ tập trung trong tầng phù hợp.
        private static string GetValue(IDictionary<string, string> row, params string[] names)
        {
            string value;
            return TryGetValue(row, out value, names) ? value : null;
        }

        // Tìm và đọc giá trị theo nhiều tên cột có thể xuất hiện.
        private static bool TryGetValue(IDictionary<string, string> row, out string value, params string[] names)
        {
            foreach (var name in names)
            {
                if (row.TryGetValue(name, out value))
                {
                    return true;
                }
            }

            foreach (var item in row)
            {
                foreach (var name in names)
                {
                    if (NormalizeKey(item.Key) == NormalizeKey(name))
                    {
                        value = item.Value;
                        return true;
                    }
                }
            }

            value = null;
            return false;
        }

        // Phân tích giá trị đầu vào và chuyển sang kiểu dữ liệu cần dùng.
        private static int? ParseNullableInt(string value)
        {
            int number;
            return int.TryParse(value, out number) ? number : (int?)null;
        }

        // Phân tích giá trị đầu vào và chuyển sang kiểu dữ liệu cần dùng.
        private static decimal? ParseNullableDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            decimal number;
            return decimal.TryParse(value.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out number) ? number : (decimal?)null;
        }

        // Phân tích giá trị đầu vào và chuyển sang kiểu dữ liệu cần dùng.
        private static bool ParseBool(string value, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            if (value == "1") return true;
            if (value == "0") return false;

            bool result;
            return bool.TryParse(value, out result) ? result : defaultValue;
        }

        // Chuyển chuỗi rỗng hoặc chỉ có khoảng trắng thành null.
        private static string NullIfWhiteSpace(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        // Chuẩn hóa dữ liệu chỉ số chất lượng trước khi so sánh, lọc hoặc lưu để giảm lỗi do khoảng trắng/định dạng.
        internal static string NormalizeKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();
            foreach (var ch in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (ch == 'đ')
                {
                    builder.Append('d');
                    continue;
                }

                builder.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
            }

            var text = Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
            if (text.StartsWith("phong "))
            {
                text = text.Substring(6);
            }

            if (text.StartsWith("khoa "))
            {
                text = text.Substring(5);
            }

            return text;
        }

    }
}
