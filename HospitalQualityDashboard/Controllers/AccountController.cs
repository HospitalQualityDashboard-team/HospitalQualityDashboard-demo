using System.Web.Mvc;
using HospitalQualityDashboard.Filters;
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

            Session["TaiKhoanId"] = user.TaiKhoanId;
            Session["TenDangNhap"] = user.TenDangNhap;
            Session["LoaiTaiKhoan"] = user.LoaiTaiKhoan;
            Session["NhanVienId"] = user.NhanVienId;
            Session["KhoaPhongId"] = user.KhoaPhongId;
            Session["TenKhoaPhong"] = user.TenKhoaPhong;

            _authService.UpdateLastLogin(user.TaiKhoanId);

            return user.LoaiTaiKhoan == LoaiTaiKhoan.Admin
                ? RedirectToAction("Index", "Home")
                : RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login");
        }

        [HttpGet]
        [RequireLogin]
        public ActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireLogin]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var taiKhoanId = (int)Session["TaiKhoanId"];
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
