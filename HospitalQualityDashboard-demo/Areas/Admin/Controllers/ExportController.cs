using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Services;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.Admin.Controllers
{
    public class ExportController : AdminBaseController
    {
        private readonly ExportService _service = new ExportService();

        public ActionResult Departments()
        {
            return File(_service.ExportDepartments(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "khoa-phong.xlsx");
        }

        public ActionResult Employees(int? khoaPhongId)
        {
            return File(_service.ExportEmployees(new EmployeeExportQueryDto
            {
                DepartmentId = khoaPhongId,
                IsAdmin = true,
                CurrentDepartmentId = null
            }), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "nhan-vien.xlsx");
        }

        public ActionResult Indicators()
        {
            return File(_service.ExportIndicators(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "chi-so-chat-luong.xlsx");
        }

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

        public ActionResult DashboardProgress(string[] columns, int? tanSuat)
        {
            return File(_service.ExportDashboardProgress(columns, tanSuat), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "tien-do-khoa-phong.xlsx");
        }
    }
}
