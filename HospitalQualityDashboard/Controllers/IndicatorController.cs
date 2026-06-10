// Muc dich: dieu huong qua ly chi so chat luong theo vai tro sang Area tuong ung.
using HospitalQualityDashboard.Models.ViewModels;
using System.Web.Mvc;

namespace HospitalQualityDashboard.Controllers
{
    public class IndicatorController : PageController
    {
        public ActionResult Index()
        {
            if (IsAdmin)
            {
                return RedirectToAction("Index", "Indicator", new { area = "Admin" });
            }
            else
            {
                return RedirectToAction("Index", "Indicator", new { area = "User" });
            }
        }

        public ActionResult Details(int id)
        {
            if (IsAdmin)
            {
                return RedirectToAction("Details", "Indicator", new { area = "Admin", id });
            }
            else
            {
                return RedirectToAction("Details", "Indicator", new { area = "User", id });
            }
        }

        public ActionResult Create()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Create", "Indicator", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ChiSoViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Create", "Indicator", new { area = "Admin" });
        }

        public ActionResult Edit(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Edit", "Indicator", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ChiSoViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Edit", "Indicator", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Lock(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Lock", "Indicator", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Unlock(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Unlock", "Indicator", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Delete", "Indicator", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Import(ImportFileViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Import", "Indicator", new { area = "Admin" });
        }
    }
}
