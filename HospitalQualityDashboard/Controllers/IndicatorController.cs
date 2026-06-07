// Mục đích: quản lý danh mục chỉ số chất lượng và quyền xem theo khoa/phòng.
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
            if (IsAdmin)
            {
                return View(new ChiSoIndexViewModel { Items = _service.GetAll() });
            }
            else
            {
                return View(new ChiSoIndexViewModel { Items = _service.GetAll(includeInactive: false, filterKhoaPhongId: CurrentKhoaPhongId) });
            }
        }

        public ActionResult Details(int id)
        {
            var model = _service.Get(id);
            if (model == null)
            {
                return HttpNotFound();
            }

            if (!IsAdmin)
            {
                if (!CurrentKhoaPhongId.HasValue || !_service.IsAssigned(id, CurrentKhoaPhongId.Value))
                {
                    return new HttpUnauthorizedResult("Bạn không có quyền xem chi tiết chỉ số này.");
                }
            }

            return View(model);
        }

        public ActionResult Create()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return View("Edit", Prepare(new ChiSoViewModel
            {
                DangHoatDong = true,
                TanSuatBaoCao = TanSuatBaoCao.HangThang,
                TanSuatBaoCaos = new[] { TanSuatBaoCao.HangThang },
                SelectedTanSuatBaoCaoValues = new[] { (int)TanSuatBaoCao.HangThang },
                LoaiCongThuc = LoaiCongThuc.TyLe
            }));
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
            return View(Prepare(_service.Get(id)));
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Import(ImportFileViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            var result = _service.Import(model.File, CurrentTaiKhoanId.Value);
            return View("Index", new ChiSoIndexViewModel { Items = _service.GetAll(), ImportResult = result });
        }

        private ActionResult Save(ChiSoViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            if (!ModelState.IsValid) return View("Edit", Prepare(model));
            _service.Save(model);
            return RedirectToAction("Index");
        }

        private ChiSoViewModel Prepare(ChiSoViewModel model)
        {
            model.TanSuatBaoCaoOptions = IndicatorService.GetFrequencyOptions(model.TanSuatBaoCaos ?? new[] { model.TanSuatBaoCao });
            return model;
        }
    }
}
