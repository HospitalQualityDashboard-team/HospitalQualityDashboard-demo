using System.Web.Mvc;
using HospitalQualityDashboard.Models.ViewModels;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard.Controllers
{
    public class EmployeeController : PageController
    {
        private readonly EmployeeService _service = new EmployeeService();
        private readonly DepartmentService _departments = new DepartmentService();

        public ActionResult Index(int? khoaPhongId)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return View(new NhanVienIndexViewModel
            {
                KhoaPhongId = khoaPhongId,
                KhoaPhongOptions = _departments.GetOptions(),
                Items = _service.GetAll(khoaPhongId)
            });
        }

        public ActionResult Create()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return View("Edit", Prepare(new NhanVienViewModel { DangHoatDong = true }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(NhanVienViewModel model)
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
        public ActionResult Edit(NhanVienViewModel model)
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

        public ActionResult CreateAccount(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            var employee = _service.Get(id);
            return View(new CreateUserAccountViewModel { NhanVienId = id, HoTen = employee.HoTen, TenDangNhap = employee.MaNhanVien });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateAccount(CreateUserAccountViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            if (!ModelState.IsValid) return View(model);
            _service.CreateUserAccount(model);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Import(ImportFileViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            var result = _service.Import(model.File, model.KhoaPhongId, CurrentTaiKhoanId.Value);
            return View("Index", new NhanVienIndexViewModel { KhoaPhongOptions = _departments.GetOptions(), Items = _service.GetAll(model.KhoaPhongId), ImportResult = result });
        }

        private ActionResult Save(NhanVienViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            if (!ModelState.IsValid) return View("Edit", Prepare(model));
            _service.Save(model);
            return RedirectToAction("Index");
        }

        private NhanVienViewModel Prepare(NhanVienViewModel model)
        {
            model.KhoaPhongOptions = _departments.GetOptions();
            return model;
        }
    }
}
