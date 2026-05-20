using System.Web.Mvc;
using HospitalQualityDashboard.Models.ViewModels;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard.Controllers
{
    public class AssignmentController : PageController
    {
        private readonly AssignmentService _service = new AssignmentService();
        private readonly DepartmentService _departments = new DepartmentService();
        private readonly IndicatorService _indicators = new IndicatorService();

        public ActionResult Index(int? khoaPhongId)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return View(new AssignmentViewModel
            {
                KhoaPhongId = khoaPhongId.GetValueOrDefault(),
                KhoaPhongOptions = _departments.GetOptions(),
                ChiSoOptions = _indicators.GetOptions(),
                Items = _service.GetAll(khoaPhongId)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Assign(AssignmentViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            _service.Assign(model.KhoaPhongId, model.SelectedChiSoIds, CurrentTaiKhoanId.Value);
            return RedirectToAction("Index", new { khoaPhongId = model.KhoaPhongId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Deactivate(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            _service.Deactivate(id);
            return RedirectToAction("Index");
        }
    }
}
