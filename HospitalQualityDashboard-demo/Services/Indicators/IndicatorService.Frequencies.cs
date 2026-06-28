// Mục đích: quản lý tần suất báo cáo gắn với chỉ số chất lượng.
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
        private void PopulateIndicatorFrequencies(IEnumerable<ChiSoViewModel> models)
        {
            var items = models == null ? new List<ChiSoViewModel>() : models.ToList();
            foreach (var item in items)
            {
                item.TanSuatBaoCao = item.TanSuatBaoCao == 0 ? TanSuatBaoCao.HangThang : item.TanSuatBaoCao;
                item.TanSuatBaoCaos = new List<TanSuatBaoCao> { item.TanSuatBaoCao };
                item.SelectedTanSuatBaoCaoValues = new[] { (int)item.TanSuatBaoCao };
                item.TanSuatBaoCaoText = FrequencyHelper.FormatFrequencies(item.TanSuatBaoCaos);
            }

            if (items.Count == 0)
            {
                return;
            }

            var ids = items.Select(x => x.ChiSoChatLuongId).ToArray();
            var parameters = new List<SqlParameter>();
            var idPlaceholders = new List<string>();
            for (var i = 0; i < ids.Length; i++)
            {
                var paramName = "@Id" + i;
                parameters.Add(Param(paramName, ids[i]));
                idPlaceholders.Add(paramName);
            }

            var sql = @"SELECT ChiSoChatLuongId, TanSuatBaoCao
FROM dbo.ChiSoTanSuatBaoCao
WHERE ChiSoChatLuongId IN (" + string.Join(",", idPlaceholders) + @")
ORDER BY ChiSoChatLuongId, TanSuatBaoCao";

            var frequencyRows = Query(sql, r => new
            {
                ChiSoChatLuongId = Int(r, "ChiSoChatLuongId"),
                TanSuatBaoCao = (TanSuatBaoCao)r.GetByte(r.GetOrdinal("TanSuatBaoCao"))
            }, parameters.ToArray());

            var groups = frequencyRows.GroupBy(x => x.ChiSoChatLuongId).ToDictionary(g => g.Key, g => (IList<TanSuatBaoCao>)FrequencyHelper.SortFrequencies(g.Select(x => x.TanSuatBaoCao)).ToList());
            foreach (var item in items)
            {
                IList<TanSuatBaoCao> frequencies;
                if (groups.TryGetValue(item.ChiSoChatLuongId, out frequencies) && frequencies.Count > 0)
                {
                    item.TanSuatBaoCaos = frequencies;
                    item.TanSuatBaoCao = frequencies.First();
                    item.SelectedTanSuatBaoCaoValues = frequencies.Select(x => (int)x).ToArray();
                    item.TanSuatBaoCaoText = FrequencyHelper.FormatFrequencies(frequencies);
                }
            }
        }

        // Xử lý chức năng chỉ số chất lượng của method SaveFrequencies, giữ logic nghiệp vụ tập trung trong tầng phù hợp.
        private void SaveFrequencies(int indicatorId, IEnumerable<TanSuatBaoCao> frequencies)
        {
            var values = FrequencyHelper.SortFrequencies(frequencies == null ? new[] { TanSuatBaoCao.HangThang } : frequencies)
                .Distinct()
                .ToList();
            if (values.Count == 0)
            {
                values.Add(TanSuatBaoCao.HangThang);
            }

            Execute("DELETE FROM dbo.ChiSoTanSuatBaoCao WHERE ChiSoChatLuongId=@Id", Param("@Id", indicatorId));
            foreach (var frequency in values)
            {
                Execute(@"INSERT INTO dbo.ChiSoTanSuatBaoCao(ChiSoChatLuongId, TanSuatBaoCao)
VALUES(@ChiSoChatLuongId, @TanSuatBaoCao)",
                    Param("@ChiSoChatLuongId", indicatorId),
                    Param("@TanSuatBaoCao", (byte)frequency));
            }
        }

        // Xử lý chức năng chỉ số chất lượng của method SaveFrequencies, giữ logic nghiệp vụ tập trung trong tầng phù hợp.
        private void SaveFrequencies(SqlConnection connection, SqlTransaction transaction, int indicatorId, IEnumerable<TanSuatBaoCao> frequencies)
        {
            var values = FrequencyHelper.SortFrequencies(frequencies == null ? new[] { TanSuatBaoCao.HangThang } : frequencies)
                .Distinct()
                .ToList();
            if (values.Count == 0)
            {
                values.Add(TanSuatBaoCao.HangThang);
            }

            Execute(connection, transaction, "DELETE FROM dbo.ChiSoTanSuatBaoCao WHERE ChiSoChatLuongId=@Id", Param("@Id", indicatorId));
            foreach (var frequency in values)
            {
                Execute(connection, transaction, @"INSERT INTO dbo.ChiSoTanSuatBaoCao(ChiSoChatLuongId, TanSuatBaoCao)
VALUES(@ChiSoChatLuongId, @TanSuatBaoCao)",
                    Param("@ChiSoChatLuongId", indicatorId),
                    Param("@TanSuatBaoCao", (byte)frequency));
            }
        }

        // Đồng bộ danh sách tần suất được chọn thành các cờ lưu database của chỉ số chất lượng.
        private static void ApplySelectedFrequencies(ChiSoViewModel model)
        {
            var selected = model.SelectedTanSuatBaoCaoValues == null
                ? new List<TanSuatBaoCao>()
                : model.SelectedTanSuatBaoCaoValues
                    .Where(v => Enum.IsDefined(typeof(TanSuatBaoCao), (byte)v))
                    .Select(v => (TanSuatBaoCao)(byte)v)
                    .Distinct()
                    .ToList();

            if (selected.Count == 0)
            {
                if (model.TanSuatBaoCaos != null && model.TanSuatBaoCaos.Count > 0)
                {
                    selected = FrequencyHelper.SortFrequencies(model.TanSuatBaoCaos).ToList();
                }
                else
                {
                    selected.Add(model.TanSuatBaoCao == 0 ? TanSuatBaoCao.HangThang : model.TanSuatBaoCao);
                }
            }

            selected = FrequencyHelper.SortFrequencies(selected).ToList();
            model.TanSuatBaoCaos = selected;
            model.TanSuatBaoCao = selected.First();
            model.SelectedTanSuatBaoCaoValues = selected.Select(x => (int)x).ToArray();
            model.TanSuatBaoCaoText = FrequencyHelper.FormatFrequencies(selected);
        }



        // Xử lý chức năng chỉ số chất lượng của method TryParseFrequencies, giữ logic nghiệp vụ tập trung trong tầng phù hợp.
        private static bool TryParseFrequencies(string value, out IList<TanSuatBaoCao> frequencies)
        {
            frequencies = new List<TanSuatBaoCao>();
            var text = NormalizeKey(value);
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (Regex.IsMatch(text, @"(^|\D)3(\D|$)") || text.Contains("ba thang") || ContainsQuarterFrequency(text))
            {
                frequencies.Add(TanSuatBaoCao.HangQuy);
            }

            if (Regex.IsMatch(text, @"(^|\D)6(\D|$)") || text.Contains("sau thang"))
            {
                frequencies.Add(TanSuatBaoCao.SauThang);
            }

            if (Regex.IsMatch(text, @"(^|\D)9(\D|$)") || text.Contains("chin thang"))
            {
                frequencies.Add(TanSuatBaoCao.ChinThang);
            }

            if (Regex.IsMatch(text, @"(^|\D)12(\D|$)") || text.Contains("muoi hai thang"))
            {
                frequencies.Add(TanSuatBaoCao.HangNam);
            }

            if (frequencies.Count == 0)
            {
                foreach (var part in value.Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    TanSuatBaoCao frequency;
                    if (TryParseFrequency(part, out frequency) && !frequencies.Contains(frequency))
                    {
                        frequencies.Add(frequency);
                    }
                }
            }

            frequencies = frequencies.Distinct().ToList();
            return frequencies.Count > 0;
        }

        // Phân tích giá trị đầu vào và chuyển sang kiểu dữ liệu cần dùng.
        private static bool TryParseFrequency(string value, out TanSuatBaoCao frequency)
        {
            var text = NormalizeKey(value);
            if (string.IsNullOrWhiteSpace(text))
            {
                frequency = TanSuatBaoCao.HangThang;
                return false;
            }

            if (text.Contains("truoc") && text.Contains("sau") && text.Contains("thuc hien"))
            {
                frequency = TanSuatBaoCao.TruocSauKhiThucHien;
                return true;
            }

            if (text.Contains("khi co") || text.Contains("xay ra") || text.Contains("phat sinh") || text.Contains("thuong xuyen"))
            {
                frequency = TanSuatBaoCao.KhiPhatSinh;
                return true;
            }

            if (text.Contains("9") || text.Contains("chin thang"))
            {
                frequency = TanSuatBaoCao.ChinThang;
                return true;
            }

            if (text.Contains("6") || text.Contains("sau thang"))
            {
                frequency = TanSuatBaoCao.SauThang;
                return true;
            }

            if (text.Contains("12") || text.Contains("nam") || text.Contains("hang nam"))
            {
                frequency = TanSuatBaoCao.HangNam;
                return true;
            }

            if (text.Contains("3") || ContainsQuarterFrequency(text) || text.Contains("ba thang"))
            {
                frequency = TanSuatBaoCao.HangQuy;
                return true;
            }

            if (text.Contains("tuan"))
            {
                frequency = TanSuatBaoCao.HangTuan;
                return true;
            }

            if (text.Contains("ngay"))
            {
                frequency = TanSuatBaoCao.HangNgay;
                return true;
            }

            if (text.Contains("thang") || text.Contains("moi thang") || text.Contains("hang thang"))
            {
                frequency = TanSuatBaoCao.HangThang;
                return true;
            }

            return Enum.TryParse(value, true, out frequency);
        }

        // Định dạng giá trị theo quy ước hiển thị của danh mục chỉ số chất lượng.
        private static string FormatFrequency(TanSuatBaoCao frequency)
        {
            switch (frequency)
            {
                case TanSuatBaoCao.HangNgay: return "Hang ngay";
                case TanSuatBaoCao.HangTuan: return "Hang tuan";
                case TanSuatBaoCao.HangThang: return "Hang thang";
                case TanSuatBaoCao.HangQuy: return "Hang quy";
                case TanSuatBaoCao.SauThang: return "6 thang";
                case TanSuatBaoCao.ChinThang: return "9 thang";
                case TanSuatBaoCao.HangNam: return "12 thang";
                case TanSuatBaoCao.KhiPhatSinh: return "Khi phat sinh";
                case TanSuatBaoCao.TruocSauKhiThucHien: return "Truoc/sau khi thuc hien";
                default: return frequency.ToString();
            }
        }

        // Kiểm tra nội dung có chứa mẫu cần nhận diện hay không.
        private static bool ContainsQuarterFrequency(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            return Regex.IsMatch(text, @"(^|\s)(hang\s+quy|moi\s+quy|theo\s+quy|quy|hang\s+qui|moi\s+qui|theo\s+qui|qui)(\s|$)");
        }





        // Phân tích giá trị đầu vào và chuyển sang kiểu dữ liệu cần dùng.
    }
}
