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
            var user = new AuthService().TryAutoLogin(filterContext.HttpContext.Request, filterContext.HttpContext.Session);
            if (user == null)
            {
                var cookie = filterContext.HttpContext.Request.Cookies["HQD_AuthToken"];
                if (cookie != null)
                {
                    cookie.Expires = DateTime.Now.AddDays(-1);
                    cookie.Path = "/";
                    filterContext.HttpContext.Response.Cookies.Add(cookie);
                }
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

        // Định kỳ kiểm tra lại session với dữ liệu tài khoản để phát hiện tài khoản bị khóa hoặc đổi quyền.
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

        // Kiểm tra session có đủ role, tên đăng nhập và phạm vi khoa/phòng trước khi cho request đi tiếp.
        private bool HasRequiredSessionData()
        {
            if (!CurrentLoaiTaiKhoan.HasValue || string.IsNullOrWhiteSpace(CurrentTenDangNhap))
            {
                return false;
            }

            return IsAdmin || CurrentKhoaPhongId.HasValue;
        }

        // Tránh kiểm tra session bằng database quá dày; chỉ revalidate sau khoảng thời gian cấu hình.
        private bool IsSessionRevalidationFresh()
        {
            var lastRevalidatedUtc = SessionUserAccessor.GetDateTime(Session, SessionUserAccessor.LastSessionRevalidatedUtcKey);
            return lastRevalidatedUtc.HasValue && DateTime.UtcNow - lastRevalidatedUtc.Value < SessionRevalidationInterval;
        }

        // Bảo vệ action chỉ dành cho Admin; tài khoản sai vai trò sẽ bị trả lỗi truy cập.
        protected ActionResult RequireAdmin()
        {
            return IsAdmin ? null : new HttpUnauthorizedResult();
        }

        // Chặn người dùng thường thao tác dữ liệu của khoa/phòng khác; admin được bỏ qua ràng buộc này.
        protected ActionResult EnsureUserDepartment(int khoaPhongId)
        {
            if (IsAdmin)
            {
                return null;
            }

            return CurrentKhoaPhongId == khoaPhongId ? null : new HttpUnauthorizedResult();
        }

        // Ghi thông tin đăng nhập tối thiểu vào session để các controller kiểm tra quyền nhanh.
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
