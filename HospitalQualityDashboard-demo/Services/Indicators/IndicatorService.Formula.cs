// Mục đích: chuẩn hóa công thức tính và quy tắc hiển thị kết quả của chỉ số.
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
        private static LoaiCongThuc ParseFormulaType(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            LoaiCongThuc formula;
            if (Enum.TryParse(value, true, out formula))
            {
                return formula;
            }

            var text = NormalizeKey(value);
            if (text.Contains("ty le") || text.Contains("phan tram")) return LoaiCongThuc.TyLe;
            if (text.Contains("so luong")) return LoaiCongThuc.SoLuong;
            if (text.Contains("thoi gian")) return LoaiCongThuc.ThoiGianTrungBinh;
            if (text.Contains("diem")) return LoaiCongThuc.DiemTrungBinh;
            if (text.Contains("ty so")) return LoaiCongThuc.TySo;
            return LoaiCongThuc.GiaTriTrucTiep;
        }

        // Suy ra giá trị phù hợp từ nội dung hiện có của danh mục chỉ số chất lượng.
        private static LoaiCongThuc InferFormulaType(ChiSoViewModel model)
        {
            var name = NormalizeIndicatorName(model == null ? null : model.TenChiSo);
            var method = NormalizeKey(model == null ? null : model.PhuongPhapTinh);
            var numerator = NormalizeKey(model == null ? null : model.TuSoMoTa);
            var denominator = NormalizeKey(model == null ? null : model.MauSoMoTa);

            if (StartsWithWord(name, "ty le") ||
                StartsWithWord(name, "ty suat") ||
                StartsWithWord(name, "cong suat") ||
                StartsWithWord(name, "hieu suat") ||
                ContainsFormulaPhrase(method, "ty le") ||
                ContainsFormulaPhrase(method, "phan tram") ||
                method.Contains("100") ||
                (!string.IsNullOrWhiteSpace(numerator) && !string.IsNullOrWhiteSpace(denominator) && method.Contains("%")))
            {
                return LoaiCongThuc.TyLe;
            }

            if (StartsWithWord(name, "ty so") || ContainsFormulaPhrase(method, "ty so"))
            {
                return LoaiCongThuc.TySo;
            }

            if (StartsWithWord(name, "thoi gian") || ContainsFormulaPhrase(method, "thoi gian trung binh"))
            {
                return LoaiCongThuc.ThoiGianTrungBinh;
            }

            if (StartsWithAnyWord(name, "so luong", "so ca", "so cuoc", "so nguoi", "so lan", "so luot", "so ngay", "so khoa", "so bac si"))
            {
                return LoaiCongThuc.SoLuong;
            }

            if (ContainsFormulaPhrase(name, "diem trung binh") ||
                ContainsFormulaPhrase(method, "diem trung binh") ||
                ContainsFormulaPhrase(name, "diem") ||
                ContainsFormulaPhrase(method, "diem"))
            {
                return LoaiCongThuc.DiemTrungBinh;
            }

            if (string.IsNullOrWhiteSpace(denominator))
            {
                return LoaiCongThuc.GiaTriTrucTiep;
            }

            return LoaiCongThuc.TyLe;
        }

        // Suy ra giá trị phù hợp từ nội dung hiện có của danh mục chỉ số chất lượng.
        private static string InferUnit(ChiSoViewModel model)
        {
            if (model == null)
            {
                return null;
            }

            var name = NormalizeIndicatorName(model == null ? null : model.TenChiSo);

            if (name.Contains("bac si co chung chi phau thuat noi soi"))
            {
                return "bác sĩ có chứng chỉ/phẫu thuật viên";
            }

            if (name.Contains("bac si dieu duong"))
            {
                return "bác sĩ/điều dưỡng";
            }

            if (name.Contains("dieu duong giuong benh"))
            {
                return "điều dưỡng/giường bệnh";
            }

            if (name.Contains("duoc si giuong benh"))
            {
                return "dược sĩ/giường bệnh";
            }

            if (name.Contains("nhan vien dinh duong giuong benh"))
            {
                return "nhân viên dinh dưỡng/giường bệnh";
            }

            if (name.Contains("bac si giuong benh"))
            {
                return "bác sĩ/giường bệnh";
            }

            if (name.Contains("thoi gian xu ly su co he thong mang"))
            {
                return "giờ";
            }

            if (name.Contains("thoi gian kham benh trung binh"))
            {
                return "phút";
            }

            if (name.Contains("thoi gian nam vien trung binh"))
            {
                return "ngày";
            }

            if (name.Contains("bao cao phan ung co hai"))
            {
                return "báo cáo";
            }

            if (StartsWithWord(name, "so ca"))
            {
                return "ca";
            }

            if (StartsWithWord(name, "so luot"))
            {
                return "lượt";
            }

            if (name.Contains("buong ve sinh"))
            {
                return "buồng";
            }

            if (name.Contains("diem tiep noi"))
            {
                return "điểm tiếp nối";
            }

            if (name.Contains("cau thang"))
            {
                return "cầu thang";
            }

            if (name.Contains("vi tinh hoa quan ly trang thiet bi"))
            {
                return "mức độ";
            }

            if (name.Contains("bac si tham du") ||
                name.Contains("can bo y te") ||
                name.Contains("nguoi benh noi tru duoc danh gia") ||
                name.Contains("nguoi benh noi tru an suat an") ||
                name.Contains("nguoi benh duoc truyen thong dinh duong"))
            {
                return "người";
            }

            switch (model.LoaiCongThuc)
            {
                case LoaiCongThuc.TyLe:
                    return "%";
                case LoaiCongThuc.TySo:
                    return "tỷ số";
                case LoaiCongThuc.SoLuong:
                    return "số lượng";
                case LoaiCongThuc.ThoiGianTrungBinh:
                    return "thời gian";
                case LoaiCongThuc.DiemTrungBinh:
                    return "điểm";
                case LoaiCongThuc.GiaTriTrucTiep:
                    return "giá trị";
                default:
                    return null;
            }
        }

        // Chuẩn hóa dữ liệu chỉ số chất lượng trước khi so sánh, lọc hoặc lưu để giảm lỗi do khoảng trắng/định dạng.
        private static string NormalizeIndicatorName(string value)
        {
            return Regex.Replace(NormalizeKey(value), @"^\d+\s+", string.Empty).Trim();
        }

        // Kiểm tra nội dung có chứa mẫu cần nhận diện hay không.
        private static bool StartsWithAnyWord(string text, params string[] phrases)
        {
            return phrases.Any(phrase => StartsWithWord(text, phrase));
        }

        // Kiểm tra nội dung có chứa mẫu cần nhận diện hay không.
        private static bool StartsWithWord(string text, string phrase)
        {
            return !string.IsNullOrWhiteSpace(text) &&
                (text == phrase || text.StartsWith(phrase + " ", StringComparison.OrdinalIgnoreCase));
        }

        // Kiểm tra nội dung có chứa mẫu cần nhận diện hay không.
        private static bool ContainsFormulaPhrase(string text, string phrase)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            return text == phrase ||
                text.StartsWith(phrase + " ", StringComparison.OrdinalIgnoreCase) ||
                text.EndsWith(" " + phrase, StringComparison.OrdinalIgnoreCase) ||
                text.IndexOf(" " + phrase + " ", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // Điền dữ liệu suy ra hoặc dữ liệu liên quan vào model của danh mục chỉ số chất lượng.
        private static void FillTargetFromText(ChiSoViewModel model, string targetText)
        {
            if (string.IsNullOrWhiteSpace(model.ToanTuSoSanh))
            {
                var trimmed = targetText.Trim();
                if (trimmed.StartsWith(">=") || trimmed.StartsWith("≥")) model.ToanTuSoSanh = ">=";
                else if (trimmed.StartsWith("<=") || trimmed.StartsWith("≤")) model.ToanTuSoSanh = "<=";
                else if (trimmed.StartsWith(">")) model.ToanTuSoSanh = ">";
                else if (trimmed.StartsWith("<")) model.ToanTuSoSanh = "<";
                else model.ToanTuSoSanh = "=";
            }

            if (!model.GiaTriMucTieu.HasValue)
            {
                var match = Regex.Match(targetText, @"[-+]?\d+([,.]\d+)?");
                decimal value;
                if (match.Success && decimal.TryParse(match.Value.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                {
                    model.GiaTriMucTieu = value;
                }
            }
        }

    }
}
