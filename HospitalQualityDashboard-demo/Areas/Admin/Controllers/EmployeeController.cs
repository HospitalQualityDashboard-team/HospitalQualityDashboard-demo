// Mục đích: quản lý hồ sơ nhân viên, tài khoản liên kết, phân trang và nhập dữ liệu nhân viên.
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

        // Hiển thị danh sách và các bộ lọc của hồ sơ nhân viên.
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

        // Khởi tạo dữ liệu cho màn hình tạo mới hồ sơ nhân viên.
        public ActionResult Create()
        {
            return View("Edit", Prepare(new NhanVienViewModel { DangHoatDong = true }));
        }

        // Kiểm tra dữ liệu gửi lên và tạo mới hồ sơ nhân viên.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(NhanVienViewModel model)
        {
            return Save(model);
        }

        // Tải dữ liệu hiện tại lên màn hình chỉnh sửa hồ sơ nhân viên.
        public ActionResult Edit(int id)
        {
            return View(Prepare(_service.Get(id)));
        }

        // Kiểm tra và lưu các thay đổi của hồ sơ nhân viên.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(NhanVienViewModel model)
        {
            return Save(model);
        }

        // Chuyển bản ghi sang trạng thái không còn cho phép chỉnh sửa.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Lock(int id)
        {
            _service.SetActive(id, false);
            return RedirectToAction("Index");
        }

        // Mở khóa nhân viên để tài khoản/khoa phòng liên quan có thể tiếp tục sử dụng trong nghiệp vụ.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Unlock(int id)
        {
            _service.SetActive(id, true);
            return RedirectToAction("Index");
        }

        // Xóa bản ghi được chọn sau khi áp dụng các ràng buộc của hồ sơ nhân viên.
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

        // Chuẩn bị form tạo tài khoản cho nhân viên chưa có tài khoản đăng nhập.
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

        // Tạo tài khoản sau khi kiểm tra username chưa trùng và nhân viên chưa được cấp tài khoản.
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

        // Đọc, kiểm tra và nhập dữ liệu từ tệp tải lên.
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

        // Lưu hồ sơ nhân viên theo model/dto đã validate, bao gồm cả nhánh thêm mới và cập nhật.
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

        // Nạp dropdown khoa/phòng và giữ dữ liệu nhân viên hiện tại khi form cần hiển thị lại.
        private NhanVienViewModel Prepare(NhanVienViewModel model)
        {
            model.KhoaPhongOptions = _departments.GetOptions();
            return model;
        }

        // Chuẩn hóa số trang để tránh page âm/0 làm sai truy vấn phân trang.
        private static int NormalizePage(int page)
        {
            return page < 1 ? 1 : page;
        }

        // Tính tổng số trang từ số bản ghi và kích thước trang.
        private static int GetTotalPages(int totalItems, int pageSize)
        {
            return totalItems <= 0 ? 1 : (int)System.Math.Ceiling((decimal)totalItems / pageSize);
        }

    }
}
