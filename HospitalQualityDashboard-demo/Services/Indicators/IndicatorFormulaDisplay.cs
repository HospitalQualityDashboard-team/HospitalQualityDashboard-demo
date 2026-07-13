// Mục đích: chuẩn hóa nhãn công thức và đơn vị tính mặc định để form chỉ số và backend dùng cùng một nguồn.
using HospitalQualityDashboardDemo.Models.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Script.Serialization;

namespace HospitalQualityDashboardDemo.Services
{
    public static class IndicatorFormulaDisplay
    {
        public static string GetLabel(LoaiCongThuc formula)
        {
            switch (formula)
            {
                case LoaiCongThuc.TyLe: return "Tỷ lệ (%)";
                case LoaiCongThuc.SoLuong: return "Số lượng";
                case LoaiCongThuc.ThoiGianTrungBinh: return "Thời gian trung bình";
                case LoaiCongThuc.DiemTrungBinh: return "Điểm trung bình";
                case LoaiCongThuc.GiaTriTrucTiep: return "Giá trị trực tiếp";
                case LoaiCongThuc.TySo: return "Tỷ số";
                default: return formula.ToString();
            }
        }

        public static string GetUnit(LoaiCongThuc formula)
        {
            switch (formula)
            {
                case LoaiCongThuc.TyLe: return "%";
                case LoaiCongThuc.TySo: return "tỷ số";
                case LoaiCongThuc.SoLuong: return "số lượng";
                case LoaiCongThuc.ThoiGianTrungBinh: return "thời gian";
                case LoaiCongThuc.DiemTrungBinh: return "điểm";
                case LoaiCongThuc.GiaTriTrucTiep: return "giá trị";
                default: return string.Empty;
            }
        }

        public static IDictionary<string, string> GetFormulaLabels()
        {
            return Enum.GetValues(typeof(LoaiCongThuc))
                .Cast<LoaiCongThuc>()
                .ToDictionary(formula => formula.ToString(), GetLabel);
        }

        public static string GetUnitMapJson()
        {
            var units = new Dictionary<string, string>();
            foreach (LoaiCongThuc formula in Enum.GetValues(typeof(LoaiCongThuc)))
            {
                var unit = GetUnit(formula);
                units[formula.ToString()] = unit;
                units[((int)formula).ToString(CultureInfo.InvariantCulture)] = unit;
            }

            return new JavaScriptSerializer().Serialize(units);
        }
    }
}
