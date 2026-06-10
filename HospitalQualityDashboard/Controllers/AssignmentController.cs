// Muc dich: chuyen huong Admin sang Area de quan ly phan cong chi so chat luong.
using HospitalQualityDashboard.Models.ViewModels;
using System.Web.Mvc;

namespace HospitalQualityDashboard.Controllers
{
    public class AssignmentController : PageController
    {
        public ActionResult Index(
            int? khoaPhongId,
            int? chiSoId,
            string trangThai,
            string trangThaiPhanCong,
            string search,
            string viewMode,
            int page = 1)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Index", "Assignment", new
            {
                area = "Admin",
                khoaPhongId,
                chiSoId,
                trangThai,
                trangThaiPhanCong,
                search,
                viewMode,
                page
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Assign(AssignmentViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Assign", "Assignment", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Deactivate(int id, string viewMode, int? khoaPhongId, int? chiSoId, string trangThai, string search, int page = 1)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Deactivate", "Assignment", new
            {
                area = "Admin",
                id,
                viewMode,
                khoaPhongId,
                chiSoId,
                trangThai,
                search,
                page
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Activate(int id, string viewMode, int? khoaPhongId, int? chiSoId, string trangThai, string search, int page = 1)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Activate", "Assignment", new
            {
                area = "Admin",
                id,
                viewMode,
                khoaPhongId,
                chiSoId,
                trangThai,
                search,
                page
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SyncFromIndicators(string viewMode)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("SyncFromIndicators", "Assignment", new { area = "Admin", viewMode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, string viewMode, int? khoaPhongId, int? chiSoId, string trangThai, string search, int page = 1)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Delete", "Assignment", new
            {
                area = "Admin",
                id,
                viewMode,
                khoaPhongId,
                chiSoId,
                trangThai,
                search,
                page
            });
        }

        [HttpPost]
        public ActionResult Preview(int[] departmentIds, int[] indicatorIds)
        {
            var admin = RequireAdmin();
            if (admin != null) return new HttpUnauthorizedResult();
            return RedirectToAction("Preview", "Assignment", new { area = "Admin", departmentIds, indicatorIds });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BulkDeactivate(int[] ids, string viewMode)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("BulkDeactivate", "Assignment", new { area = "Admin", ids, viewMode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BulkActivate(int[] ids, string viewMode)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("BulkActivate", "Assignment", new { area = "Admin", ids, viewMode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BulkDelete(int[] ids, string viewMode)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("BulkDelete", "Assignment", new { area = "Admin", ids, viewMode });
        }
    }
}
