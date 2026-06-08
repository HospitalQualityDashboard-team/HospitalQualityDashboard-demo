using System.Web.Mvc;
using HospitalQualityDashboard.Models.DTOs;
using HospitalQualityDashboard.Models.ViewModels;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard.Areas.Admin.Controllers
{
    public class EmployeeController : AdminBaseController
    {
        private readonly EmployeeService _service = new EmployeeService();
        private readonly DepartmentService _departments = new DepartmentService();

        public ActionResult Index(int? khoaPhongId)
        {
            return View(new NhanVienIndexViewModel
            {
                KhoaPhongId = khoaPhongId,
                KhoaPhongOptions = _departments.GetOptions(),
                Items = _service.GetAll(khoaPhongId)
            });
        }

        public ActionResult Create()
        {
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
            _service.SetActive(id, false);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Unlock(int id)
        {
            _service.SetActive(id, true);
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

        public ActionResult CreateAccount(int id)
        {
            var employee = _service.Get(id);
            if (employee.HasAccount)
            {
                TempData["Error"] = "Nhan vien nay da co tai khoan trong he thong.";
                return RedirectToAction("Index");
            }

            return View(new CreateUserAccountViewModel { NhanVienId = id, HoTen = employee.HoTen, TenDangNhap = employee.MaNhanVien });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateAccount(CreateUserAccountViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            if (_service.HasAccount(model.NhanVienId))
            {
                ModelState.AddModelError("", "Nhan vien nay da co tai khoan trong he thong.");
                return View(model);
            }

            if (_service.IsUsernameExists(model.TenDangNhap))
            {
                ModelState.AddModelError("TenDangNhap", "Ten dang nhap da ton tai trong he thong. Vui long chon ten khac.");
                return View(model);
            }

            _service.CreateUserAccount(new CreateUserAccountDto
            {
                NhanVienId = model.NhanVienId,
                TenDangNhap = model.TenDangNhap,
                MatKhau = model.MatKhau
            });
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Import(ImportFileViewModel model)
        {
            var result = _service.Import(model.File, model.KhoaPhongId, CurrentTaiKhoanId.Value);
            return View("Index", new NhanVienIndexViewModel { KhoaPhongOptions = _departments.GetOptions(), Items = _service.GetAll(model.KhoaPhongId), ImportResult = result });
        }

        private ActionResult Save(NhanVienViewModel model)
        {
            if (!ModelState.IsValid) return View("Edit", Prepare(model));
            _service.Save(new EmployeeSaveDto
            {
                NhanVienId = model.NhanVienId,
                MaNhanVien = model.MaNhanVien,
                HoTen = model.HoTen,
                NgaySinh = model.NgaySinh,
                GioiTinh = model.GioiTinh,
                ChucVu = model.ChucVu,
                Email = model.Email,
                SoDienThoai = model.SoDienThoai,
                KhoaPhongId = model.KhoaPhongId,
                DangHoatDong = model.DangHoatDong
            });
            return RedirectToAction("Index");
        }

        private NhanVienViewModel Prepare(NhanVienViewModel model)
        {
            model.KhoaPhongOptions = _departments.GetOptions();
            return model;
        }
    }
}
