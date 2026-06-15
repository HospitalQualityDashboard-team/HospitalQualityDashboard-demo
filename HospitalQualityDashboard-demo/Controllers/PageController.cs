// Mục đích: controller nền tập trung kiểm tra session, role và phạm vi khoa/phòng.
using HospitalQualityDashboardDemo.Models.Enums;
using HospitalQualityDashboardDemo.Services;
using System;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Controllers
{
    public abstract class PageController : Controller
    {
        private static readonly TimeSpan SessionRevalidationInterval = TimeSpan.FromMinutes(5);

        protected int? CurrentTaiKhoanId
        {
            get { return SessionUserAccessor.GetInt(Session, SessionUserAccessor.TaiKhoanIdKey); }
        }

        protected string CurrentTenDangNhap
        {
            get { return SessionUserAccessor.GetString(Session, SessionUserAccessor.TenDangNhapKey); }
        }

        protected LoaiTaiKhoan? CurrentLoaiTaiKhoan
        {
            get { return SessionUserAccessor.GetLoaiTaiKhoan(Session); }
        }

        protected int? CurrentNhanVienId
        {
            get { return SessionUserAccessor.GetInt(Session, SessionUserAccessor.NhanVienIdKey); }
        }

        protected int? CurrentKhoaPhongId
        {
            get { return SessionUserAccessor.GetInt(Session, SessionUserAccessor.KhoaPhongIdKey); }
        }

        protected string CurrentTenKhoaPhong
        {
            get { return SessionUserAccessor.GetString(Session, SessionUserAccessor.TenKhoaPhongKey); }
        }

        protected bool IsAdmin
        {
            get { return CurrentLoaiTaiKhoan == LoaiTaiKhoan.Admin; }
        }

        protected bool IsUser
        {
            get { return CurrentLoaiTaiKhoan == LoaiTaiKhoan.User; }
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!SessionUserAccessor.IsAuthenticated(Session))
            {
                filterContext.Result = RedirectToAction("UserLogin", "Account", new { area = "" });
                return;
            }

            var sessionGate = RevalidateCurrentSession();
            if (sessionGate != null)
            {
                filterContext.Result = sessionGate;
                return;
            }

            base.OnActionExecuting(filterContext);
        }

        protected ActionResult RevalidateCurrentSession()
        {
            var accountId = CurrentTaiKhoanId;
            if (!accountId.HasValue)
            {
                ClearLoginSession();
                return RedirectToAction("UserLogin", "Account", new { area = "" });
            }

            if (HasRequiredSessionData() && IsSessionRevalidationFresh())
            {
                return null;
            }

            var user = new AuthService().GetAuthenticatedUser(accountId.Value);
            if (user == null || user.IsLocked)
            {
                ClearLoginSession();
                return RedirectToAction("UserLogin", "Account", new { area = "" });
            }

            SetLoginSession(user);
            return null;
        }

        private bool HasRequiredSessionData()
        {
            if (!CurrentLoaiTaiKhoan.HasValue || string.IsNullOrWhiteSpace(CurrentTenDangNhap))
            {
                return false;
            }

            return IsAdmin || CurrentKhoaPhongId.HasValue;
        }

        private bool IsSessionRevalidationFresh()
        {
            var lastRevalidatedUtc = SessionUserAccessor.GetDateTime(Session, SessionUserAccessor.LastSessionRevalidatedUtcKey);
            return lastRevalidatedUtc.HasValue && DateTime.UtcNow - lastRevalidatedUtc.Value < SessionRevalidationInterval;
        }

        protected ActionResult RequireAdmin()
        {
            return IsAdmin ? null : new HttpUnauthorizedResult();
        }

        protected ActionResult EnsureUserDepartment(int khoaPhongId)
        {
            if (IsAdmin)
            {
                return null;
            }

            return CurrentKhoaPhongId == khoaPhongId ? null : new HttpUnauthorizedResult();
        }

        protected void SetLoginSession(AuthenticatedUser user)
        {
            SessionUserAccessor.SetLoginSession(Session, user);
        }

        protected void ClearLoginSession()
        {
            SessionUserAccessor.ClearLoginSession(Session);
        }
    }
}
