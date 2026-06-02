using System.Web.Mvc;
using HospitalQualityDashboard.Models.Enums;
using HospitalQualityDashboard.Models.ViewModels;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard.Controllers
{
    public class ReportingPeriodController : PageController
    {
        private readonly ReportingPeriodService _service = new ReportingPeriodService();

        public ActionResult Index()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return View(_service.GetAll());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GeneratePeriods()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            var generated = _service.GenerateMissingPeriods();
            TempData["Message"] = $"Đã tạo {generated.Count} kỳ báo cáo tự động!";
            return RedirectToAction("Index");
        }

        public ActionResult Create()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return View("Edit", new KyBaoCaoViewModel { TrangThai = TrangThaiKyBaoCao.Nhap });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(KyBaoCaoViewModel model)
        {
            return Save(model);
        }

        public ActionResult Edit(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return View(_service.Get(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(KyBaoCaoViewModel model)
        {
            return Save(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Open(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            _service.SetStatus(id, TrangThaiKyBaoCao.Mo);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Lock(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            _service.SetStatus(id, TrangThaiKyBaoCao.Khoa);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            try
            {
                _service.Delete(id);
            }
            catch (System.InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        private ActionResult Save(KyBaoCaoViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            if (!ModelState.IsValid) return View("Edit", model);
            _service.Save(model);
            return RedirectToAction("Index");
        }
    }
}
