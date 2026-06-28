// Mục đích: xuất báo cáo và dashboard Excel, luôn giới hạn dữ liệu theo khoa/phòng đăng nhập.
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Services;
using System;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.User.Controllers
{
    public class ExportController : UserBaseController
    {
        private readonly ExportService _service = new ExportService();
        private readonly DashboardExcelExportService _dashboardExcelExport = new DashboardExcelExportService();

        // Trả file Excel cho nhóm dữ liệu Reports, áp dụng bộ lọc và quyền truy cập trước khi xuất.
        public ActionResult Reports(int? kyBaoCaoId, int? chiSoChatLuongId)
        {
            return File(_service.ExportReports(new ReportExportQueryDto
            {
                PeriodId = kyBaoCaoId,
                DepartmentId = CurrentKhoaPhongId,
                IndicatorId = chiSoChatLuongId,
                IsAdmin = false,
                CurrentDepartmentId = CurrentKhoaPhongId
            }), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "bao-cao.xlsx");
        }

        // Trả file Excel cho nhóm dữ liệu Dashboard, áp dụng bộ lọc và quyền truy cập trước khi xuất.
        public ActionResult Dashboard(DashboardExcelExportQueryDto query)
        {
            query = query ?? new DashboardExcelExportQueryDto();
            query.KhoaPhongId = CurrentKhoaPhongId;

            try
            {
                var result = _dashboardExcelExport.BuildDashboardExcel(query, new ExportUserContextDto
                {
                    TaiKhoanId = CurrentTaiKhoanId.Value,
                    TenDangNhap = CurrentTenDangNhap,
                    IsAdmin = false,
                    KhoaPhongId = CurrentKhoaPhongId,
                    TenKhoaPhong = CurrentTenKhoaPhong,
                    DiaChiIP = Request.UserHostAddress
                });

                return File(result.Content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.FileName);
            }
            catch (InvalidOperationException exception)
            {
                return new HttpStatusCodeResult(400, exception.Message);
            }
        }
    }
}
