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
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = _authService.Authenticate(model.TenDangNhap, model.MatKhau);
            if (user == null)
            {
                ModelState.AddModelError("", "Ten dang nhap hoac mat khau khong dung.");
                return View(model);
            }

            SessionUserAccessor.SetLoginSession(Session, user);

            _authService.UpdateLastLogin(user.TaiKhoanId);

            return user.LoaiTaiKhoan == LoaiTaiKhoan.Admin
                ? RedirectToAction("Index", "Home")
                : RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            SessionUserAccessor.ClearLoginSession(Session);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public ActionResult ChangePassword()
        {
            if (!SessionUserAccessor.IsAuthenticated(Session))
            {
                return RedirectToAction("Login");
            }

            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!SessionUserAccessor.IsAuthenticated(Session))
            {
                return RedirectToAction("Login");
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
            return RedirectToAction("ChangePassword");
        }
    }
}
