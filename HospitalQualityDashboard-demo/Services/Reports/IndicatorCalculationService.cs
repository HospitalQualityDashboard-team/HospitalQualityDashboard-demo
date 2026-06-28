// Mục đích: tính kết quả chỉ số và áp dụng ngoại lệ nghiệp vụ giữa tử số, mẫu số, mục tiêu.
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.Enums;
using HospitalQualityDashboardDemo.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Services
{
    public class IndicatorCalculationService
    {
        public const string NumeratorCannotExceedDenominatorMessage = "Tử số không được lớn hơn mẫu số";

        private static readonly ISet<string> NumeratorWithinDenominatorIndicatorCodes =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "CS01",
                "CS02",
                "CS05",
                "CS13",
                "CS15",
                "CS16",
                "CS22",
                "CS24",
                "CS25",
                "CS26",
                "CS27",
                "CS28",
                "CS29",
                "CS30",
                "CS31",
                "CS32",
                "CS33",
                "CS38",
                "CS39",
                "CS40",
                "CS41",
                "CS46",
                "CS47"
            };

        private static readonly string[] NumeratorWithinDenominatorIndicatorNameTokens =
        {
            "ty le nguoi benh duoc cung cap hoa don",
            "ty le nguoi benh thanh toan vien phi truc tuyen",
            "ty le ho so bhyt chuyen cong giam dinh",
            "dieu duong co kien thuc danh gia phan loai",
            "dinh nhom mau tai giuong",
            "ghi chep dung va du",
            "ty le tuan thu ve sinh tay",
            "ty le hien mac viem phoi benh vien",
            "nhiem khuan tiet nieu",
            "nhiem khuan vet mo",
            "nhiem khuan huyet",
            "nhiem khuan da mo mem",
            "chi thi hoa hoc nhom 5",
            "tiem an toan",
            "loet do ti de",
            "su co y khoa do dung thuoc",
            "viem phoi do u dong",
            "cong tac di buong",
            "tu van giao duc suc khoe",
            "ky thuat chuyen mon theo phan tuyen",
            "phau thuat tu loai ii",
            "tu vong va tien luong tu vong",
            "chuyen sang benh vien khac"
        };

        // Tính toán giá trị nghiệp vụ phục vụ tính kết quả chỉ số.
        public void Calculate(ReportEntryViewModel report, ChiSoViewModel indicator)
        {
            if (indicator.LoaiCongThuc == LoaiCongThuc.TyLe)
            {
                EnsureDenominator(report.MauSo);
                EnsureNumeratorWithinDenominator(report.TuSo, report.MauSo, indicator);
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

            report.KetQua = RoundResult(report.KetQua);
            report.DatMucTieu = CompareTarget(report.KetQua, indicator.ToanTuSoSanh, indicator.GiaTriMucTieu);
        }

        // Xác định điều kiện nghiệp vụ của tính kết quả chỉ số để controller/service chọn nhánh xử lý an toàn.
        public static bool RequiresNumeratorWithinDenominator(ChiSoViewModel indicator)
        {
            return indicator != null && RequiresNumeratorWithinDenominator(indicator.MaChiSo, indicator.TenChiSo);
        }

        // Xác định điều kiện nghiệp vụ của tính kết quả chỉ số để controller/service chọn nhánh xử lý an toàn.
        public static bool RequiresNumeratorWithinDenominator(string indicatorCode)
        {
            return RequiresNumeratorWithinDenominator(indicatorCode, null);
        }

        // Xác định điều kiện nghiệp vụ của tính kết quả chỉ số để controller/service chọn nhánh xử lý an toàn.
        public static bool RequiresNumeratorWithinDenominator(string indicatorCode, string indicatorName)
        {
            var normalizedCode = NormalizeIndicatorCode(indicatorCode);
            if (!string.IsNullOrWhiteSpace(normalizedCode) &&
                NumeratorWithinDenominatorIndicatorCodes.Contains(normalizedCode))
            {
                return true;
            }

            var normalizedName = NormalizeText(indicatorName);
            return !string.IsNullOrWhiteSpace(normalizedName) &&
                NumeratorWithinDenominatorIndicatorNameTokens.Any(normalizedName.Contains);
        }

        // Với chỉ số tỷ lệ thông thường, tử số không được vượt mẫu số để tránh kết quả nghiệp vụ vô lý.
        private static void EnsureNumeratorWithinDenominator(decimal? numerator, decimal? denominator, ChiSoViewModel indicator)
        {
            if (!RequiresNumeratorWithinDenominator(indicator) || !numerator.HasValue || !denominator.HasValue)
            {
                return;
            }

            if (numerator.Value > denominator.Value)
            {
                throw new InvalidOperationException(NumeratorCannotExceedDenominatorMessage);
            }
        }

        // Chuẩn hóa dữ liệu tính kết quả chỉ số trước khi so sánh, lọc hoặc lưu để giảm lỗi do khoảng trắng/định dạng.
        private static string NormalizeIndicatorCode(string indicatorCode)
        {
            if (string.IsNullOrWhiteSpace(indicatorCode))
            {
                return string.Empty;
            }

            var trimmed = indicatorCode.Trim().ToUpperInvariant();
            if (trimmed.StartsWith("CS", StringComparison.OrdinalIgnoreCase))
            {
                int number;
                if (int.TryParse(trimmed.Substring(2), out number))
                {
                    return "CS" + number.ToString("00");
                }
            }

            return trimmed;
        }

        // Chuẩn hóa dữ liệu tính kết quả chỉ số trước khi so sánh, lọc hoặc lưu để giảm lỗi do khoảng trắng/định dạng.
        private static string NormalizeText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            foreach (var character in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(character == 'đ' ? 'd' : character);
                }
            }

            var text = builder.ToString()
                .Replace(",", " ")
                .Replace("/", " ")
                .Replace("-", " ")
                .Replace("`", " ")
                .Normalize(NormalizationForm.FormC);

            while (text.Contains("  "))
            {
                text = text.Replace("  ", " ");
            }

            return text;
        }

        // Mẫu số bắt buộc phải có và khác 0 trước khi tính tỷ lệ hoặc trung bình.
        private static void EnsureDenominator(decimal? denominator)
        {
            if (!denominator.HasValue || denominator.Value == 0)
            {
                throw new InvalidOperationException("Mẫu số phải lớn hơn 0.");
            }
        }

        // So sánh giá trị theo quy tắc nghiệp vụ của tính kết quả chỉ số.
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

        // Làm tròn kết quả theo độ chính xác quy định của tính kết quả chỉ số.
        private static decimal? RoundResult(decimal? value)
        {
            return value.HasValue ? Math.Round(value.Value, 2, MidpointRounding.AwayFromZero) : (decimal?)null;
        }
    }
}
