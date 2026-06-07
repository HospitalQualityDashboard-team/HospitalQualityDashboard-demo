// Mục đích: cung cấp endpoint xuất CSV/Excel cho dữ liệu quản trị và báo cáo.
using System.Web.Mvc;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard.Controllers
{
    public class ExportController : PageController
    {
        private readonly ExportService _service = new ExportService();

        public ActionResult Departments()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return File(_service.ExportDepartments(), "text/csv", "khoa-phong.csv");
        }

        public ActionResult Employees(int? khoaPhongId)
        {
            return File(_service.ExportEmployees(khoaPhongId, IsAdmin, CurrentKhoaPhongId), "text/csv", "nhan-vien.csv");
        }

        public ActionResult Indicators()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return File(_service.ExportIndicators(), "text/csv", "chi-so-chat-luong.csv");
        }

        public ActionResult Assignments(
            int? khoaPhongId,
            int? chiSoId,
            string trangThai,
            string trangThaiPhanCong,
            string search,
            string[] columns)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;

            return File(
                _service.ExportAssignments(khoaPhongId, chiSoId, trangThai, trangThaiPhanCong, search, columns),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "phan-cong-chi-so.xlsx");
        }

        public ActionResult Reports(int? kyBaoCaoId, int? khoaPhongId, int? chiSoChatLuongId)
        {
            var effectiveDepartmentId = IsAdmin ? khoaPhongId : CurrentKhoaPhongId;
            return File(_service.ExportReports(kyBaoCaoId, effectiveDepartmentId, chiSoChatLuongId, IsAdmin, CurrentKhoaPhongId), "text/csv", "bao-cao.csv");
        }
    }
}
