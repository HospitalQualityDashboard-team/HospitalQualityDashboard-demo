// Mục đích: chuẩn hóa nhãn, thứ tự hiển thị và tùy chọn chọn tần suất báo cáo trên toàn ứng dụng.
using HospitalQualityDashboardDemo.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Services
{
    public static class FrequencyHelper
    {
        // Định dạng giá trị theo quy ước hiển thị của tần suất báo cáo.
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

        // Định dạng giá trị theo quy ước hiển thị của tần suất báo cáo.
        public static string FormatFrequencies(IEnumerable<TanSuatBaoCao> frequencies)
        {
            var values = frequencies == null 
                ? new List<TanSuatBaoCao>() 
                : SortFrequencies(frequencies).ToList();

            return values.Count == 0 ? string.Empty : string.Join(", ", values.Select(FormatFrequency));
        }

        // Sắp xếp dữ liệu theo thứ tự hiển thị chuẩn của tần suất báo cáo.
        public static IEnumerable<TanSuatBaoCao> SortFrequencies(IEnumerable<TanSuatBaoCao> frequencies)
        {
            return (frequencies ?? new List<TanSuatBaoCao>())
                .Distinct()
                .OrderBy(GetFrequencyDisplayOrder);
        }

        // Trả về thứ tự hiển thị ổn định cho từng tần suất báo cáo.
        public static int GetFrequencyDisplayOrder(TanSuatBaoCao frequency)
        {
            // Khoảng cách 10 cho phép chèn thêm tần suất mà không phải đổi toàn bộ thứ tự hiện có.
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

        // Dựng danh sách tần suất báo cáo thống nhất cho form và bộ lọc toàn hệ thống.
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
