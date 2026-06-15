using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.ViewModels;
using HospitalQualityDashboardDemo.Services;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.Admin.Controllers
{
    public class EmployeeController : AdminBaseController
    {
        private const int DefaultPageSize = 20;
        private readonly EmployeeService _service = new EmployeeService();
        private readonly DepartmentService _departments = new DepartmentService();

        public ActionResult Index(int? khoaPhongId, int page = 1)
        {
            int totalItems;
            var items = _service.GetAll(khoaPhongId, page, DefaultPageSize, out totalItems);
            return View(new NhanVienIndexViewModel
            {
                KhoaPhongId = khoaPhongId,
                KhoaPhongOptions = _departments.GetOptions(),
                Items = items,
                Page = NormalizePage(page),
                PageSize = DefaultPageSize,
                TotalItems = totalItems,
                TotalPages = GetTotalPages(totalItems, DefaultPageSize)
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
                TempData["Error"] = "Nhân viên này đã có tài khoản trong hệ thống.";
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
                ModelState.AddModelError("", "Nhân viên này đã có tài khoản trong hệ thống.");
                return View(model);
            }

            if (_service.IsUsernameExists(model.TenDangNhap))
            {
                ModelState.AddModelError("TenDangNhap", "Tên đăng nhập đã tồn tại trong hệ thống. Vui lòng chọn tên khác.");
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
            int totalItems;
            var items = _service.GetAll(model.KhoaPhongId, 1, DefaultPageSize, out totalItems);
            return View("Index", new NhanVienIndexViewModel
            {
                KhoaPhongId = model.KhoaPhongId,
                KhoaPhongOptions = _departments.GetOptions(),
                Items = items,
                ImportResult = result,
                Page = 1,
                PageSize = DefaultPageSize,
                TotalItems = totalItems,
                TotalPages = GetTotalPages(totalItems, DefaultPageSize)
            });
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

        private static int NormalizePage(int page)
        {
            return page < 1 ? 1 : page;
        }

        private static int GetTotalPages(int totalItems, int pageSize)
        {
            return totalItems <= 0 ? 1 : (int)System.Math.Ceiling((decimal)totalItems / pageSize);
        }
    }
}
