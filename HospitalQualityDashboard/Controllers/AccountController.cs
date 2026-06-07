// Mục đích: xử lý đăng nhập, đăng xuất, hồ sơ và đổi mật khẩu người dùng.
using System.Web.Mvc;
using HospitalQualityDashboard.Models.Enums;
using HospitalQualityDashboard.Models.ViewModels;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _authService;

        public AccountController()
            : this(new AuthService())
        {
        }

        public AccountController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public ActionResult Login()
        {
            return RedirectToAction("UserLogin");
        }

        [HttpGet]
        public ActionResult AdminLogin()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AdminLogin(LoginViewModel model)
        {
            return LoginForRole(model, LoaiTaiKhoan.Admin, "AdminLogin", "Tai khoan nay khong phai tai khoan Admin.");
        }

        [HttpGet]
        public ActionResult UserLogin()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UserLogin(LoginViewModel model)
        {
            return LoginForRole(model, LoaiTaiKhoan.User, "UserLogin", "Tai khoan nay khong phai tai khoan User.");
        }

        private ActionResult LoginForRole(LoginViewModel model, LoaiTaiKhoan expectedRole, string viewName, string wrongRoleMessage)
        {
            if (!ModelState.IsValid)
            {
                return View(viewName, model);
            }

            var user = _authService.Authenticate(model.TenDangNhap, model.MatKhau);
            if (user == null)
            {
                ModelState.AddModelError("", "Ten dang nhap hoac mat khau khong dung.");
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

            _authService.UpdateLastLogin(user.TaiKhoanId);

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            SessionUserAccessor.ClearLoginSession(Session);
            return RedirectToAction("UserLogin");
        }

        [HttpGet]
        public ActionResult ChangePassword()
        {
            if (!SessionUserAccessor.IsAuthenticated(Session))
            {
                return RedirectToAction("UserLogin");
            }

            return RedirectToAction("Profile");
        }

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
                ModelState.AddModelError("", "Mat khau hien tai khong dung.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Doi mat khau thanh cong.";
            return RedirectToAction("Profile");
        }

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
    }
}
