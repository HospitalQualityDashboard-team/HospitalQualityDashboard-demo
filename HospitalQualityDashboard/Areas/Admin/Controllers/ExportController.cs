using System.Web.Mvc;
using HospitalQualityDashboard.Models.DTOs;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard.Areas.Admin.Controllers
{
    public class ExportController : AdminBaseController
    {
        private readonly ExportService _service = new ExportService();

        public ActionResult Departments()
        {
            return File(_service.ExportDepartments(), "text/csv", "khoa-phong.csv");
        }

        public ActionResult Employees(int? khoaPhongId)
        {
            return File(_service.ExportEmployees(new EmployeeExportQueryDto
            {
                DepartmentId = khoaPhongId,
                IsAdmin = true,
                CurrentDepartmentId = null
            }), "text/csv", "nhan-vien.csv");
        }

        public ActionResult Indicators()
        {
            return File(_service.ExportIndicators(), "text/csv", "chi-so-chat-luong.csv");
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
            }), "text/csv", "bao-cao.csv");
        }
    }
}
