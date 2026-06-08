// Mục đích: điều hướng xuất dữ liệu theo vai trò sang Area tương ứng.
using System.Web.Mvc;

namespace HospitalQualityDashboard.Controllers
{
    public class ExportController : PageController
    {
        public ActionResult Departments()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Departments", "Export", new { area = "Admin" });
        }

        public ActionResult Employees(int? khoaPhongId)
        {
            if (IsAdmin)
            {
                return RedirectToAction("Employees", "Export", new { area = "Admin", khoaPhongId });
            }
            else
            {
                return new HttpUnauthorizedResult();
            }
        }

        public ActionResult Indicators()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Indicators", "Export", new { area = "Admin" });
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
            return RedirectToAction("Assignments", "Export", new
            {
                area = "Admin",
                khoaPhongId,
                chiSoId,
                trangThai,
                trangThaiPhanCong,
                search,
                columns
            });
        }

        public ActionResult Reports(int? kyBaoCaoId, int? khoaPhongId, int? chiSoChatLuongId)
        {
            if (IsAdmin)
            {
                return RedirectToAction("Reports", "Export", new { area = "Admin", kyBaoCaoId, khoaPhongId, chiSoChatLuongId });
            }
            else
            {
                return RedirectToAction("Reports", "Export", new { area = "User", kyBaoCaoId, chiSoChatLuongId });
            }
        }
    }
}
