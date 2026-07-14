// Mục đích: điều phối nghiệp vụ phân công chỉ số chất lượng cho khoa/phòng phụ trách.
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
    public partial class AssignmentService : DbServiceBase
    {
        private readonly DepartmentService _departments = new DepartmentService();

        // Định dạng giá trị theo quy ước hiển thị của phân công chỉ số.
        public static string FormatFrequencies(IEnumerable<TanSuatBaoCao> frequencies)
        {
            return FrequencyHelper.FormatFrequencies(frequencies);
        }

        // Định dạng giá trị theo quy ước hiển thị của phân công chỉ số.
        public static string FormatFormula(LoaiCongThuc formula)
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
    }
}
