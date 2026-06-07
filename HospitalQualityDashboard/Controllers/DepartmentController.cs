// Mục đích: quản lý danh mục khoa/phòng và import dữ liệu khoa/phòng.
using System.Web.Mvc;
using HospitalQualityDashboard.Models.ViewModels;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard.Controllers
{
    public class DepartmentController : PageController
    {
        private readonly DepartmentService _service = new DepartmentService();

        public ActionResult Index(string search)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return View(new KhoaPhongIndexViewModel { Search = search, Items = _service.GetAll(search) });
        }

        public ActionResult Create()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
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
            var admin = RequireAdmin();
            if (admin != null) return admin;
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
            var admin = RequireAdmin();
            if (admin != null) return admin;
            _service.SetUsed(id, false);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Unlock(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            _service.SetUsed(id, true);
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
            return View("Index", new KhoaPhongIndexViewModel { Items = _service.GetAll(), ImportResult = result });
        }

        private ActionResult Save(KhoaPhongViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            if (!ModelState.IsValid) return View("Edit", model);
            _service.Save(model);
            return RedirectToAction("Index");
        }
    }
}
