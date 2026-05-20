using System.Web.Mvc;
using HospitalQualityDashboard.Models.ViewModels;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard.Controllers
{
    public class NotificationController : PageController
    {
        private readonly NotificationService _service = new NotificationService();
        private readonly DepartmentService _departments = new DepartmentService();

        public ActionResult Index()
        {
            return View(_service.GetForUser(CurrentTaiKhoanId.Value, IsAdmin));
        }

        public ActionResult Create()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return View(new NotificationViewModel { KhoaPhongOptions = _departments.GetOptions() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(NotificationViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            if (string.IsNullOrWhiteSpace(model.TieuDe) || string.IsNullOrWhiteSpace(model.NoiDung))
            {
                ModelState.AddModelError("", "Vui long nhap tieu de va noi dung.");
            }

            if (!ModelState.IsValid)
            {
                model.KhoaPhongOptions = _departments.GetOptions();
                return View(model);
            }

            _service.SendManual(model, CurrentTaiKhoanId.Value);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkAsRead(int id)
        {
            _service.MarkAsRead(id, CurrentTaiKhoanId.Value);
            return RedirectToAction("Index");
        }
    }
}
