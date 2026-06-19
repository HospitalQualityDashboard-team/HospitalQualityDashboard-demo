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

        // Kiểm tra session và quyền truy cập trước khi action được thực thi.
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

        // Điều phối yêu cầu HTTP và phản hồi cho session và phạm vi truy cập dùng chung.
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

        // Xác định dữ liệu có thỏa điều kiện nghiệp vụ của session và phạm vi truy cập dùng chung hay không.
        private bool HasRequiredSessionData()
        {
            if (!CurrentLoaiTaiKhoan.HasValue || string.IsNullOrWhiteSpace(CurrentTenDangNhap))
            {
                return false;
            }

            return IsAdmin || CurrentKhoaPhongId.HasValue;
        }

        // Xác định dữ liệu có thỏa điều kiện nghiệp vụ của session và phạm vi truy cập dùng chung hay không.
        private bool IsSessionRevalidationFresh()
        {
            var lastRevalidatedUtc = SessionUserAccessor.GetDateTime(Session, SessionUserAccessor.LastSessionRevalidatedUtcKey);
            return lastRevalidatedUtc.HasValue && DateTime.UtcNow - lastRevalidatedUtc.Value < SessionRevalidationInterval;
        }

        // Điều phối yêu cầu HTTP và phản hồi cho session và phạm vi truy cập dùng chung.
        protected ActionResult RequireAdmin()
        {
            return IsAdmin ? null : new HttpUnauthorizedResult();
        }

        // Kiểm tra các điều kiện hợp lệ trước khi tiếp tục xử lý session và phạm vi truy cập dùng chung.
        protected ActionResult EnsureUserDepartment(int khoaPhongId)
        {
            if (IsAdmin)
            {
                return null;
            }

            return CurrentKhoaPhongId == khoaPhongId ? null : new HttpUnauthorizedResult();
        }

        // Kiểm tra và cập nhật dữ liệu của session và phạm vi truy cập dùng chung.
        protected void SetLoginSession(AuthenticatedUser user)
        {
            SessionUserAccessor.SetLoginSession(Session, user);
        }

        // Xóa trạng thái tạm để chuẩn bị lượt xử lý mới của session và phạm vi truy cập dùng chung.
        protected void ClearLoginSession()
        {
            SessionUserAccessor.ClearLoginSession(Session);
        }
    }
}
