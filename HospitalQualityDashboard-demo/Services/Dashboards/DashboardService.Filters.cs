// Mục đích: dựng danh sách bộ lọc dashboard theo kỳ báo cáo, tần suất và phạm vi người xem.
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
    public partial class DashboardService
    {
        private static IList<SelectListItem> BuildDashboardFrequencyOptions(int? tanSuatFilter)
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Tất cả tần suất", Selected = !tanSuatFilter.HasValue },
                new SelectListItem { Value = "1", Text = "Hàng ngày", Selected = tanSuatFilter == 1 },
                new SelectListItem { Value = "2", Text = "Hàng tuần", Selected = tanSuatFilter == 2 },
                new SelectListItem { Value = "3", Text = "Hàng tháng", Selected = tanSuatFilter == 3 },
                new SelectListItem { Value = "4", Text = "Hàng quý", Selected = tanSuatFilter == 4 },
                new SelectListItem { Value = "5", Text = "6 tháng", Selected = tanSuatFilter == 5 },
                new SelectListItem { Value = "9", Text = "9 tháng", Selected = tanSuatFilter == 9 },
                new SelectListItem { Value = "6", Text = "Hàng năm", Selected = tanSuatFilter == 6 },
                new SelectListItem { Value = "7", Text = "Khi phát sinh", Selected = tanSuatFilter == 7 },
                new SelectListItem { Value = "8", Text = "Trước và sau khi thực hiện", Selected = tanSuatFilter == 8 }
            };
        }

        // Chuẩn bị các tùy chọn bộ lọc và phạm vi dữ liệu cho màn hình xuất.
        public void PrepareExportFilters(DashboardViewModel model, DashboardExcelExportQueryDto query, bool admin, int? departmentId)
        {
            if (model == null)
            {
                return;
            }

            query = query ?? new DashboardExcelExportQueryDto();
            model.NamBaoCao = query.NamBaoCao;
            model.KyBaoCaoId = query.KyBaoCaoId;
            model.KhoaPhongId = admin ? query.KhoaPhongId : departmentId;
            model.LinhVuc = string.IsNullOrWhiteSpace(query.LinhVuc) ? null : query.LinhVuc.Trim();
            model.TrangThaiNhapLieu = query.TrangThaiNhapLieu;
            model.TrangThaiDuyet = query.TrangThaiDuyet;
            model.DatMucTieu = query.DatMucTieu;
            model.SelectedTanSuat = query.TanSuat ?? model.SelectedTanSuat;

            model.NamBaoCaoOptions = BuildYearOptions(model.NamBaoCao);
            model.KyBaoCaoOptions = BuildPeriodOptions(model.KyBaoCaoId);
            model.KhoaPhongOptions = admin
                ? BuildDepartmentOptions(model.KhoaPhongId)
                : BuildCurrentDepartmentOptions(departmentId);
            model.LinhVucOptions = BuildFieldOptions(model.LinhVuc);
            model.TrangThaiNhapLieuOptions = BuildInputStatusOptions(model.TrangThaiNhapLieu);
            model.TrangThaiDuyetOptions = BuildReviewStatusOptions(model.TrangThaiDuyet);
            model.DatMucTieuOptions = BuildTargetStatusOptions(model.DatMucTieu);
            model.TanSuatOptions = BuildDashboardFrequencyOptions(model.SelectedTanSuat);
            model.ComparisonPeriods = BuildComparisonPeriods();
        }

        private IList<DashboardComparisonPeriodDto> BuildComparisonPeriods()
        {
            return Query(@"
SELECT KyBaoCaoId, TenKyBaoCao, LoaiKyBaoCao, TuNgay, TrangThai
FROM dbo.KyBaoCao
WHERE LoaiKyBaoCao IN (3, 4, 5, 9, 6) AND TrangThai <> 1
ORDER BY TuNgay DESC",
                reader => new DashboardComparisonPeriodDto
                {
                    KyBaoCaoId = Int(reader, "KyBaoCaoId"),
                    TenKyBaoCao = String(reader, "TenKyBaoCao"),
                    TanSuat = Convert.ToInt32(reader["LoaiKyBaoCao"], CultureInfo.InvariantCulture),
                    TuNgay = reader.GetDateTime(reader.GetOrdinal("TuNgay")),
                    TrangThai = Convert.ToInt32(reader["TrangThai"], CultureInfo.InvariantCulture)
                });
        }

        // Tạo danh sách năm có dữ liệu báo cáo để bộ lọc dashboard không hiển thị năm rỗng.
        private IList<SelectListItem> BuildYearOptions(int? selectedYear)
        {
            var years = Query("SELECT DISTINCT DATEPART(YEAR, TuNgay) AS Nam FROM dbo.KyBaoCao ORDER BY Nam DESC",
                r => Int(r, "Nam"));

            if (years.Count == 0)
            {
                years.Add(DateTime.Today.Year);
            }

            var options = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Tất cả năm", Selected = !selectedYear.HasValue }
            };
            options.AddRange(years.Select(year => new SelectListItem
            {
                Value = year.ToString(),
                Text = year.ToString(),
                Selected = selectedYear == year
            }));
            return options;
        }

        // Tạo danh sách kỳ báo cáo cho bộ lọc, giữ đúng kỳ đang được người dùng chọn.
        private IList<SelectListItem> BuildPeriodOptions(int? selectedPeriodId)
        {
            var periods = Query("SELECT KyBaoCaoId, TenKyBaoCao FROM dbo.KyBaoCao ORDER BY TuNgay DESC",
                r => new SelectListItem
                {
                    Value = Int(r, "KyBaoCaoId").ToString(),
                    Text = String(r, "TenKyBaoCao"),
                    Selected = selectedPeriodId == Int(r, "KyBaoCaoId")
                });

            periods.Insert(0, new SelectListItem { Value = "", Text = "Tất cả kỳ báo cáo", Selected = !selectedPeriodId.HasValue });
            return periods;
        }

        // Tạo bộ lọc khoa/phòng toàn viện dành cho admin khi xem dashboard tổng hợp.
        private IList<SelectListItem> BuildDepartmentOptions(int? selectedDepartmentId)
        {
            var departments = Query("SELECT KhoaPhongId, TenKhoaPhong FROM dbo.KhoaPhong ORDER BY TenKhoaPhong",
                r => new SelectListItem
                {
                    Value = Int(r, "KhoaPhongId").ToString(),
                    Text = String(r, "TenKhoaPhong"),
                    Selected = selectedDepartmentId == Int(r, "KhoaPhongId")
                });

            departments.Insert(0, new SelectListItem { Value = "", Text = "Toàn viện", Selected = !selectedDepartmentId.HasValue });
            return departments;
        }

        // Giới hạn bộ lọc khoa/phòng theo phạm vi của người dùng thường để tránh xem dữ liệu ngoài quyền.
        private IList<SelectListItem> BuildCurrentDepartmentOptions(int? departmentId)
        {
            if (!departmentId.HasValue)
            {
                return new List<SelectListItem>();
            }

            return Query("SELECT KhoaPhongId, TenKhoaPhong FROM dbo.KhoaPhong WHERE KhoaPhongId=@KhoaPhongId",
                r => new SelectListItem
                {
                    Value = Int(r, "KhoaPhongId").ToString(),
                    Text = String(r, "TenKhoaPhong"),
                    Selected = true
                },
                Param("@KhoaPhongId", departmentId.Value));
        }

        // Tạo danh sách lĩnh vực có phát sinh chỉ số để người dùng lọc dashboard theo nhóm chuyên môn.
        private IList<SelectListItem> BuildFieldOptions(string selectedField)
        {
            var fields = Query(@"
SELECT DISTINCT LinhVucApDung
FROM dbo.ChiSoChatLuong
WHERE LinhVucApDung IS NOT NULL AND LTRIM(RTRIM(LinhVucApDung)) <> N''
ORDER BY LinhVucApDung",
                r => String(r, "LinhVucApDung"));

            var options = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Tất cả lĩnh vực", Selected = string.IsNullOrWhiteSpace(selectedField) }
            };
            options.AddRange(fields.Select(field => new SelectListItem
            {
                Value = field,
                Text = field,
                Selected = string.Equals(selectedField, field, StringComparison.OrdinalIgnoreCase)
            }));
            return options;
        }

        // Gom các trạng thái nhập liệu thành lựa chọn nghiệp vụ dễ đọc trên màn hình dashboard.
        private static IList<SelectListItem> BuildInputStatusOptions(int? selectedStatus)
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Tất cả trạng thái nhập liệu", Selected = !selectedStatus.HasValue },
                new SelectListItem { Value = "0", Text = "Chưa nhập", Selected = selectedStatus == 0 },
                new SelectListItem { Value = ((byte)TrangThaiBaoCao.Nhap).ToString(), Text = "Nháp", Selected = selectedStatus == (byte)TrangThaiBaoCao.Nhap },
                new SelectListItem { Value = ((byte)TrangThaiBaoCao.DaGui).ToString(), Text = "Đã gửi", Selected = selectedStatus == (byte)TrangThaiBaoCao.DaGui },
                new SelectListItem { Value = ((byte)TrangThaiBaoCao.QuaHan).ToString(), Text = "Quá hạn", Selected = selectedStatus == (byte)TrangThaiBaoCao.QuaHan },
                new SelectListItem { Value = ((byte)TrangThaiBaoCao.DaKhoa).ToString(), Text = "Đã khóa", Selected = selectedStatus == (byte)TrangThaiBaoCao.DaKhoa },
                new SelectListItem { Value = ((byte)TrangThaiBaoCao.DaDuyet).ToString(), Text = "Đã duyệt", Selected = selectedStatus == (byte)TrangThaiBaoCao.DaDuyet },
                new SelectListItem { Value = ((byte)TrangThaiBaoCao.TraLai).ToString(), Text = "Trả lại", Selected = selectedStatus == (byte)TrangThaiBaoCao.TraLai }
            };
        }

        // Gom các trạng thái duyệt báo cáo để bộ lọc phản ánh đúng luồng khóa, duyệt và trả lại.
        private static IList<SelectListItem> BuildReviewStatusOptions(int? selectedStatus)
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Tất cả trạng thái duyệt", Selected = !selectedStatus.HasValue },
                new SelectListItem { Value = ((byte)TrangThaiBaoCao.DaKhoa).ToString(), Text = "Đã khóa", Selected = selectedStatus == (byte)TrangThaiBaoCao.DaKhoa },
                new SelectListItem { Value = ((byte)TrangThaiBaoCao.DaDuyet).ToString(), Text = "Đã duyệt", Selected = selectedStatus == (byte)TrangThaiBaoCao.DaDuyet },
                new SelectListItem { Value = ((byte)TrangThaiBaoCao.TraLai).ToString(), Text = "Trả lại", Selected = selectedStatus == (byte)TrangThaiBaoCao.TraLai }
            };
        }

        // Tạo lựa chọn đạt/chưa đạt mục tiêu để phân tích nhanh chất lượng theo ngưỡng chỉ số.
        private static IList<SelectListItem> BuildTargetStatusOptions(bool? selectedStatus)
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Tất cả đánh giá", Selected = !selectedStatus.HasValue },
                new SelectListItem { Value = "true", Text = "Đạt mục tiêu", Selected = selectedStatus == true },
                new SelectListItem { Value = "false", Text = "Chưa đạt mục tiêu", Selected = selectedStatus == false }
            };
        }

    }
}
