// Muc dich: chuyen huong Admin sang Area de quan ly khoa/phong.
using System.Web.Mvc;
using HospitalQualityDashboard.Models.ViewModels;

namespace HospitalQualityDashboard.Controllers
{
    public class DepartmentController : PageController
    {
        public ActionResult Index(string search)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Index", "Department", new { area = "Admin", search });
        }

        public ActionResult Create()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Create", "Department", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(KhoaPhongViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Create", "Department", new { area = "Admin" });
        }

        public ActionResult Edit(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Edit", "Department", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(KhoaPhongViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Edit", "Department", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Lock(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Lock", "Department", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Unlock(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Unlock", "Department", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Delete", "Department", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Import(ImportFileViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Import", "Department", new { area = "Admin" });
        }
    }
}
