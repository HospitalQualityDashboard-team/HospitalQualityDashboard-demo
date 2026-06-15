using HospitalQualityDashboardDemo.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Services
{
    public static class FrequencyHelper
    {
        public static string FormatFrequency(TanSuatBaoCao frequency)
        {
            switch (frequency)
            {
                case TanSuatBaoCao.HangNgay: return "Hàng ngày";
                case TanSuatBaoCao.HangTuan: return "Hàng tuần";
                case TanSuatBaoCao.HangThang: return "Hàng tháng";
                case TanSuatBaoCao.HangQuy: return "Hàng quý";
                case TanSuatBaoCao.SauThang: return "6 tháng";
                case TanSuatBaoCao.ChinThang: return "9 tháng";
                case TanSuatBaoCao.HangNam: return "Hàng năm";
                case TanSuatBaoCao.KhiPhatSinh: return "Khi phát sinh";
                case TanSuatBaoCao.TruocSauKhiThucHien: return "Trước/sau khi thực hiện";
                default: return frequency.ToString();
            }
        }

        public static string FormatFrequencies(IEnumerable<TanSuatBaoCao> frequencies)
        {
            var values = frequencies == null 
                ? new List<TanSuatBaoCao>() 
                : SortFrequencies(frequencies).ToList();

            return values.Count == 0 ? string.Empty : string.Join(", ", values.Select(FormatFrequency));
        }

        public static IEnumerable<TanSuatBaoCao> SortFrequencies(IEnumerable<TanSuatBaoCao> frequencies)
        {
            return (frequencies ?? new List<TanSuatBaoCao>())
                .Distinct()
                .OrderBy(GetFrequencyDisplayOrder);
        }

        public static int GetFrequencyDisplayOrder(TanSuatBaoCao frequency)
        {
            switch (frequency)
            {
                case TanSuatBaoCao.HangNgay: return 10;
                case TanSuatBaoCao.HangTuan: return 20;
                case TanSuatBaoCao.HangThang: return 30;
                case TanSuatBaoCao.HangQuy: return 40;
                case TanSuatBaoCao.SauThang: return 50;
                case TanSuatBaoCao.ChinThang: return 60;
                case TanSuatBaoCao.HangNam: return 70;
                case TanSuatBaoCao.KhiPhatSinh: return 80;
                case TanSuatBaoCao.TruocSauKhiThucHien: return 90;
                default: return 999;
            }
        }

        public static IList<SelectListItem> GetFrequencyOptions(IEnumerable<TanSuatBaoCao> selectedFrequencies)
        {
            var selected = selectedFrequencies == null
                ? new HashSet<TanSuatBaoCao>()
                : new HashSet<TanSuatBaoCao>(selectedFrequencies);

            return Enum.GetValues(typeof(TanSuatBaoCao))
                .Cast<TanSuatBaoCao>()
                .Select(x => new SelectListItem
                {
                    Value = ((int)x).ToString(),
                    Text = FormatFrequency(x),
                    Selected = selected.Contains(x)
                })
                .ToList();
        }
    }
}
