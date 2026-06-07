// Mục đích: quản lý nhân viên, tài khoản nhân viên và import danh sách nhân viên.
using System.Web.Mvc;
using HospitalQualityDashboard.Models.ViewModels;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard.Controllers
{
    public class EmployeeController : PageController
    {
        // Service xử lý dữ liệu nhân viên.
        private readonly EmployeeService _service = new EmployeeService();

        // Service lấy danh sách khoa/phòng cho bộ lọc và form nhập.
        private readonly DepartmentService _departments = new DepartmentService();

        // Hiển thị danh sách nhân viên, có thể lọc theo khoa/phòng.
        public ActionResult Index(int? khoaPhongId)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;

            // Tạo view model gồm bộ lọc, dropdown khoa/phòng và dữ liệu bảng.
            return View(new NhanVienIndexViewModel
            {
                KhoaPhongId = khoaPhongId,
                KhoaPhongOptions = _departments.GetOptions(),
                Items = _service.GetAll(khoaPhongId)
            });
        }

        // Hiển thị màn hình thêm nhân viên.
        public ActionResult Create()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;

            // Hiển thị form thêm mới, mặc định nhân viên đang hoạt động.
            return View("Edit", Prepare(new NhanVienViewModel { DangHoatDong = true }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Xử lý submit form thêm nhân viên.
        public ActionResult Create(NhanVienViewModel model)
        {
            // Nhận dữ liệu từ form thêm nhân viên và dùng chung hàm Save.
            return Save(model);
        }

        // Hiển thị màn hình sửa thông tin nhân viên.
        public ActionResult Edit(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;

            // Lấy nhân viên theo id và nạp thêm danh sách khoa/phòng cho dropdown.
            return View(Prepare(_service.Get(id)));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Xử lý submit form sửa nhân viên.
        public ActionResult Edit(NhanVienViewModel model)
        {
            return Save(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Khóa nhân viên và tài khoản liên kết.
        public ActionResult Lock(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            _service.SetActive(id, false);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Mở khóa nhân viên và tài khoản liên kết.
        public ActionResult Unlock(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            _service.SetActive(id, true);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Xóa nhân viên khi chưa có tài khoản phụ thuộc.
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
                // Nếu có ràng buộc dữ liệu, hiển thị lỗi ở màn hình danh sách.
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        // Hiển thị form tạo tài khoản đăng nhập cho nhân viên.
        public ActionResult CreateAccount(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            var employee = _service.Get(id);

            // Nếu đã có tài khoản thì không cho tạo trùng.
            if (employee.HasAccount)
            {
                TempData["Error"] = "Nhân viên này đã có tài khoản trong hệ thống.";
                return RedirectToAction("Index");
            }

            // Gợi ý tên đăng nhập bằng mã nhân viên.
            return View(new CreateUserAccountViewModel { NhanVienId = id, HoTen = employee.HoTen, TenDangNhap = employee.MaNhanVien });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Xử lý tạo tài khoản User cho nhân viên.
        public ActionResult CreateAccount(CreateUserAccountViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            // Kiểm tra các trường bắt buộc trên form.
            if (!ModelState.IsValid) return View(model);

            // Chống tạo tài khoản trùng cho cùng một nhân viên.
            if (_service.HasAccount(model.NhanVienId))
            {
                ModelState.AddModelError("", "Nhân viên này đã có tài khoản trong hệ thống.");
                return View(model);
            }

            // Tên đăng nhập phải duy nhất trong hệ thống.
            if (_service.IsUsernameExists(model.TenDangNhap))
            {
                ModelState.AddModelError("TenDangNhap", "Tên đăng nhập đã tồn tại trong hệ thống. Vui lòng chọn tên khác.");
                return View(model);
            }

            // Tạo tài khoản với mật khẩu đã băm trong service.
            _service.CreateUserAccount(model);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Import danh sách nhân viên từ file Excel/CSV.
        public ActionResult Import(ImportFileViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            // Import theo khoa/phòng đang chọn và ghi nhận người thực hiện.
            var result = _service.Import(model.File, model.KhoaPhongId, CurrentTaiKhoanId.Value);
            return View("Index", new NhanVienIndexViewModel { KhoaPhongOptions = _departments.GetOptions(), Items = _service.GetAll(model.KhoaPhongId), ImportResult = result });
        }

        // Hàm dùng chung cho thêm mới và cập nhật nhân viên.
        private ActionResult Save(NhanVienViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;

            // Nếu dữ liệu nhập sai, trả lại form kèm danh sách lỗi.
            if (!ModelState.IsValid) return View("Edit", Prepare(model));

            // Lưu nhân viên mới hoặc cập nhật nhân viên cũ tùy theo NhanVienId.
            _service.Save(model);
            return RedirectToAction("Index");
        }

        private NhanVienViewModel Prepare(NhanVienViewModel model)
        {
            // Nạp danh sách khoa/phòng để hiển thị trong combobox.
            model.KhoaPhongOptions = _departments.GetOptions();
            return model;
        }
    }
}
