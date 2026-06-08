// Muc dich: chuyen huong Admin sang Area de quan ly nhan vien.
using System.Web.Mvc;
using HospitalQualityDashboard.Models.ViewModels;

namespace HospitalQualityDashboard.Controllers
{
    public class EmployeeController : PageController
    {
        public ActionResult Index(int? khoaPhongId)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Index", "Employee", new { area = "Admin", khoaPhongId });
        }

        public ActionResult Create()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Create", "Employee", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(NhanVienViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Create", "Employee", new { area = "Admin" });
        }

        public ActionResult Edit(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Edit", "Employee", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(NhanVienViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Edit", "Employee", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Lock(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Lock", "Employee", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Unlock(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Unlock", "Employee", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Delete", "Employee", new { area = "Admin", id });
        }

        public ActionResult CreateAccount(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("CreateAccount", "Employee", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateAccount(CreateUserAccountViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("CreateAccount", "Employee", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Import(ImportFileViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Import", "Employee", new { area = "Admin" });
        }
    }
}
