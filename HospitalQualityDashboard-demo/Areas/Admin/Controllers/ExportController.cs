// Mục đích: cung cấp các endpoint xuất Excel dành cho Admin, bao gồm dữ liệu toàn viện và lịch sử xuất.
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Services;
using System;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.Admin.Controllers
{
    public class ExportController : AdminBaseController
    {
        private readonly ExportService _service = new ExportService();
        private readonly DashboardExcelExportService _dashboardExcelExport = new DashboardExcelExportService();

        // Trả file Excel cho nhóm dữ liệu Departments, áp dụng bộ lọc và quyền truy cập trước khi xuất.
        public ActionResult Departments()
        {
            return File(_service.ExportDepartments(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "khoa-phong.xlsx");
        }

        // Trả file Excel cho nhóm dữ liệu Employees, áp dụng bộ lọc và quyền truy cập trước khi xuất.
        public ActionResult Employees(int? khoaPhongId)
        {
            return File(_service.ExportEmployees(new EmployeeExportQueryDto
            {
                DepartmentId = khoaPhongId,
                IsAdmin = true,
                CurrentDepartmentId = null
            }), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "nhan-vien.xlsx");
        }

        // Trả file Excel cho nhóm dữ liệu Indicators, áp dụng bộ lọc và quyền truy cập trước khi xuất.
        public ActionResult Indicators()
        {
            return File(_service.ExportIndicators(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "chi-so-chat-luong.xlsx");
        }

        // Trả file Excel cho nhóm dữ liệu Assignments, áp dụng bộ lọc và quyền truy cập trước khi xuất.
        public ActionResult Assignments(int? khoaPhongId, int? chiSoId, string trangThai, string trangThaiPhanCong, string search, string[] columns)
        {
            return File(
                _service.ExportAssignments(new AssignmentExportQueryDto
                {
                    DepartmentId = khoaPhongId,
                    IndicatorId = chiSoId,
                    Status = trangThai,
                    AssignmentStatus = trangThaiPhanCong,
                    Search = search,
                    Columns = columns
                }),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "phan-cong-chi-so.xlsx");
        }

        // Trả file Excel cho nhóm dữ liệu Reports, áp dụng bộ lọc và quyền truy cập trước khi xuất.
        public ActionResult Reports(int? kyBaoCaoId, int? khoaPhongId, int? chiSoChatLuongId)
        {
            return File(_service.ExportReports(new ReportExportQueryDto
            {
                PeriodId = kyBaoCaoId,
                DepartmentId = khoaPhongId,
                IndicatorId = chiSoChatLuongId,
                IsAdmin = true,
                CurrentDepartmentId = null
            }), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "bao-cao.xlsx");
        }

        // Trả file Excel cho nhóm dữ liệu DashboardProgress, áp dụng bộ lọc và quyền truy cập trước khi xuất.
        public ActionResult DashboardProgress(string[] columns, int? tanSuat)
        {
            return File(_service.ExportDashboardProgress(columns, tanSuat), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "tien-do-khoa-phong.xlsx");
        }

        // Trả file Excel cho nhóm dữ liệu Dashboard, áp dụng bộ lọc và quyền truy cập trước khi xuất.
        public ActionResult Dashboard(DashboardExcelExportQueryDto query)
        {
            try
            {
                var result = _dashboardExcelExport.BuildDashboardExcel(query, new ExportUserContextDto
                {
                    TaiKhoanId = CurrentTaiKhoanId.Value,
                    TenDangNhap = CurrentTenDangNhap,
                    IsAdmin = true,
                    KhoaPhongId = null,
                    TenKhoaPhong = null,
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
