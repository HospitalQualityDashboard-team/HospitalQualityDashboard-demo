// Mục đích: xử lý đăng nhập, đăng xuất, hồ sơ và đổi mật khẩu người dùng.
using System;
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.Enums;
using HospitalQualityDashboardDemo.Models.ViewModels;
using HospitalQualityDashboardDemo.Services;
using System.Globalization;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _authService;

        // Khởi tạo controller với AuthService mặc định cho luồng đăng nhập thực tế.
        public AccountController()
            : this(new AuthService())
        {
        }

        // Cho phép truyền AuthService khi kiểm thử hoặc khi cần thay đổi cách xác thực.
        public AccountController(AuthService authService)
        {
            _authService = authService;
        }

        // Hiển thị biểu mẫu đăng nhập phù hợp với vai trò.
        [HttpGet]
        public ActionResult Login()
        {
            return RedirectToAction("UserLogin");
        }

        // Hiển thị biểu mẫu đăng nhập dành cho Admin.
        [HttpGet]
        public ActionResult AdminLogin()
        {
            return View(new LoginViewModel());
        }

        // Xác thực tài khoản Admin và chuyển vào khu vực quản trị.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AdminLogin(LoginViewModel model)
        {
            return LoginForRole(model, LoaiTaiKhoan.Admin, "AdminLogin", "Tài khoản này không phải tài khoản Admin.");
        }

        // Hiển thị biểu mẫu đăng nhập dành cho User.
        [HttpGet]
        public ActionResult UserLogin()
        {
            return View(new LoginViewModel());
        }

        // Xác thực tài khoản User và chuyển vào khu vực khoa/phòng.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UserLogin(LoginViewModel model)
        {
            return LoginForRole(model, LoaiTaiKhoan.User, "UserLogin", "Tài khoản này không phải tài khoản User.");
        }

        // Dùng chung quy trình xác thực và từ chối tài khoản sai vai trò.
        private ActionResult LoginForRole(LoginViewModel model, LoaiTaiKhoan expectedRole, string viewName, string wrongRoleMessage)
        {
            if (!ModelState.IsValid)
            {
                return View(viewName, model);
            }

            var user = _authService.Authenticate(model.TenDangNhap, model.MatKhau);
            if (user == null)
            {
                _authService.RecordFailedLogin(model.TenDangNhap);
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View(viewName, model);
            }

            if (user.LoaiTaiKhoan != expectedRole)
            {
                ModelState.AddModelError("", wrongRoleMessage);
                return View(viewName, model);
            }

            if (user.IsLocked)
            {
                ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa");
                return View(viewName, model);
            }

            SessionUserAccessor.SetLoginSession(Session, user);

            _authService.ResetFailedLogin(user.TaiKhoanId);
            _authService.UpdateLastLogin(user.TaiKhoanId);

            if (user.LoaiTaiKhoan == LoaiTaiKhoan.Admin)
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            return RedirectToAction("Index", "Dashboard", new { area = "User" });
        }

        // Xóa session hiện tại và đưa người dùng về trang đăng nhập.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            SessionUserAccessor.ClearLoginSession(Session);
            return RedirectToAction("UserLogin");
        }

        // Kiểm tra mật khẩu hiện tại và lưu mật khẩu mới an toàn.
        [HttpGet]
        public ActionResult ChangePassword()
        {
            if (!SessionUserAccessor.IsAuthenticated(Session))
            {
                return RedirectToAction("UserLogin");
            }

            return RedirectToAction("Profile");
        }

        // Kiểm tra mật khẩu hiện tại và lưu mật khẩu mới an toàn.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!SessionUserAccessor.IsAuthenticated(Session))
            {
                return RedirectToAction("UserLogin");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var taiKhoanId = SessionUserAccessor.GetInt(Session, SessionUserAccessor.TaiKhoanIdKey).Value;
            if (!_authService.ChangePassword(taiKhoanId, model.MatKhauCu, model.MatKhauMoi))
            {
                ModelState.AddModelError("", "Mật khẩu hiện tại không đúng.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công.";
            return RedirectToAction("Profile");
        }

        // Tải hồ sơ của người dùng hiện tại để hiển thị.
        [HttpGet]
        public new ActionResult Profile()
        {
            if (!SessionUserAccessor.IsAuthenticated(Session))
            {
                return RedirectToAction("UserLogin");
            }

            var taiKhoanId = SessionUserAccessor.GetInt(Session, SessionUserAccessor.TaiKhoanIdKey).Value;
            var model = _authService.GetUserProfile(taiKhoanId);
            if (model == null)
            {
                SessionUserAccessor.ClearLoginSession(Session);
                return RedirectToAction("UserLogin");
            }

            return View(model);
        }

        // Kiểm tra và cập nhật thông tin hồ sơ của người dùng hiện tại.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public new ActionResult Profile(UserProfileViewModel model)
        {
            if (!SessionUserAccessor.IsAuthenticated(Session))
            {
                return RedirectToAction("UserLogin");
            }

            var taiKhoanId = SessionUserAccessor.GetInt(Session, SessionUserAccessor.TaiKhoanIdKey).Value;
            var profile = _authService.GetUserProfile(taiKhoanId);
            if (profile == null)
            {
                SessionUserAccessor.ClearLoginSession(Session);
                return RedirectToAction("UserLogin");
            }

            if (model == null || model.ChangePassword == null)
            {
                ModelState.AddModelError("", "Vui lòng nhập thông tin đổi mật khẩu.");
                return View(profile);
            }

            if (!ModelState.IsValid)
            {
                profile.ChangePassword = model.ChangePassword;
                return View(profile);
            }

            if (!_authService.ChangePassword(taiKhoanId, model.ChangePassword.MatKhauCu, model.ChangePassword.MatKhauMoi))
            {
                ModelState.AddModelError("", "Mật khẩu hiện tại không đúng.");
                profile.ChangePassword = model.ChangePassword;
                return View(profile);
            }

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công.";
            return RedirectToAction("Profile");
        }

        // Cập nhật nhanh hồ sơ người dùng qua DTO, dùng cho luồng profile không đổi thông tin đăng nhập.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(ProfileUpdateDto model)
        {
            if (!SessionUserAccessor.IsAuthenticated(Session))
            {
                return RedirectToAction("UserLogin");
            }

            var taiKhoanId = SessionUserAccessor.GetInt(Session, SessionUserAccessor.TaiKhoanIdKey).Value;
            NormalizeProfileUpdateDate(model);

            if (!ModelState.IsValid)
            {
                var profile = _authService.GetUserProfile(taiKhoanId);
                if (profile == null)
                {
                    SessionUserAccessor.ClearLoginSession(Session);
                    return RedirectToAction("UserLogin");
                }
                profile.HoTen = model.HoTen;
                profile.NgaySinh = model.NgaySinh;
                profile.GioiTinh = model.GioiTinh;
                profile.ChucVu = model.ChucVu;
                profile.Email = model.Email;
                profile.SoDienThoai = model.SoDienThoai;
                return View("Profile", profile);
            }

            try
            {
                _authService.UpdateProfile(taiKhoanId, model);
                TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công.";
            }
            catch (InvalidOperationException ex)
            {
                var profile = _authService.GetUserProfile(taiKhoanId);
                ModelState.AddModelError("", ex.Message);
                if (profile != null)
                {
                    profile.HoTen = model.HoTen;
                    profile.NgaySinh = model.NgaySinh;
                    profile.GioiTinh = model.GioiTinh;
                    profile.ChucVu = model.ChucVu;
                    profile.Email = model.Email;
                    profile.SoDienThoai = model.SoDienThoai;
                }
                return View("Profile", profile ?? new UserProfileViewModel());
            }

            return RedirectToAction("Profile");
        }

        private void NormalizeProfileUpdateDate(ProfileUpdateDto model)
        {
            if (model == null)
            {
                return;
            }

            var rawNgaySinh = Request.Form["NgaySinh"];
            if (string.IsNullOrWhiteSpace(rawNgaySinh))
            {
                ModelState.Remove("NgaySinh");
                model.NgaySinh = null;
                return;
            }

            DateTime parsedDate;
            var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" };
            if (DateTime.TryParseExact(
                rawNgaySinh.Trim(),
                formats,
                CultureInfo.GetCultureInfo("vi-VN"),
                DateTimeStyles.None,
                out parsedDate))
            {
                ModelState.Remove("NgaySinh");
                model.NgaySinh = parsedDate.Date;
                return;
            }

            ModelState.Remove("NgaySinh");
            ModelState.AddModelError("NgaySinh", "Ngày sinh không hợp lệ. Vui lòng nhập đúng định dạng dd/MM/yyyy.");
            model.NgaySinh = null;
        }
    }
}
