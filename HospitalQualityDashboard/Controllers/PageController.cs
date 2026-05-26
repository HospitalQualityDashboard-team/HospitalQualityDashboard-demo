using System.Web.Mvc;
using HospitalQualityDashboard.Models.Enums;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard.Controllers
{
    public abstract class PageController : Controller
    {
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
                filterContext.Result = RedirectToAction("UserLogin", "Account");
                return;
            }

            base.OnActionExecuting(filterContext);
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
