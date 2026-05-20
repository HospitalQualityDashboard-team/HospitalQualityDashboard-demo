using System.Web.Mvc;
using HospitalQualityDashboard.Models.Enums;
using HospitalQualityDashboard.Models.ViewModels;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard.Controllers
{
    public class IndicatorController : PageController
    {
        private readonly IndicatorService _service = new IndicatorService();

        public ActionResult Index()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return View(_service.GetAll());
        }

        public ActionResult Details(int id)
        {
            return View(_service.Get(id));
        }

        public ActionResult Create()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return View("Edit", new ChiSoViewModel { DangHoatDong = true, TanSuatBaoCao = TanSuatBaoCao.HangThang, LoaiCongThuc = LoaiCongThuc.TyLe });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ChiSoViewModel model)
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
        public ActionResult Edit(ChiSoViewModel model)
        {
            return Save(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Lock(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            _service.SetActive(id, false);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Unlock(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            _service.SetActive(id, true);
            return RedirectToAction("Index");
        }

        private ActionResult Save(ChiSoViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            if (!ModelState.IsValid) return View("Edit", model);
            _service.Save(model);
            return RedirectToAction("Index");
        }
    }
}
