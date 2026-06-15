using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.ViewModels;
using HospitalQualityDashboardDemo.Services;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.Admin.Controllers
{
    public class DepartmentController : AdminBaseController
    {
        private readonly DepartmentService _service = new DepartmentService();

        public ActionResult Index(string search)
        {
            return View(new KhoaPhongIndexViewModel { Search = search, Items = _service.GetAll(search) });
        }

        public ActionResult Create()
        {
            return View("Edit", new KhoaPhongViewModel { Used = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(KhoaPhongViewModel model)
        {
            return Save(model);
        }

        public ActionResult Edit(int id)
        {
            return View(_service.Get(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(KhoaPhongViewModel model)
        {
            return Save(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Lock(int id)
        {
            _service.SetUsed(id, false);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Unlock(int id)
        {
            _service.SetUsed(id, true);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
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
            var result = _service.Import(model.File, CurrentTaiKhoanId.Value);
            return View("Index", new KhoaPhongIndexViewModel { Items = _service.GetAll(), ImportResult = result });
        }

        private ActionResult Save(KhoaPhongViewModel model)
        {
            if (!ModelState.IsValid) return View("Edit", model);
            _service.Save(new DepartmentSaveDto
            {
                KhoaPhongId = model.KhoaPhongId,
                IdKhoaPhongNguon = model.IdKhoaPhongNguon,
                TenKhoaPhong = model.TenKhoaPhong,
                Used = model.Used,
                GhiChu = model.GhiChu
            });
            return RedirectToAction("Index");
        }
    }
}
