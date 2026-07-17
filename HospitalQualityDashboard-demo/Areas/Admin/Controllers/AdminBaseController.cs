// Mục đích: áp dụng kiểm tra đăng nhập và vai trò Admin chung cho toàn bộ controller quản trị.
using HospitalQualityDashboardDemo.Controllers;
using HospitalQualityDashboardDemo.Services;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.Admin.Controllers
{
    public abstract class AdminBaseController : PageController
    {
        private readonly NotificationService _notificationService = new NotificationService();

        // Kiểm tra session và quyền truy cập trước khi action được thực thi.
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            if (filterContext.Result != null)
            {
                return;
            }

            var admin = RequireAdminOrBanGiamDoc();
            if (admin != null)
            {
                filterContext.Result = admin;
                return;
            }

            if (CurrentTaiKhoanId.HasValue)
            {
                var notificationPreview = _notificationService.GetPreviewForAccount(CurrentTaiKhoanId.Value);
                ViewBag.NotificationPreview = notificationPreview;
                ViewBag.UnreadNotificationCount = notificationPreview.UnreadCount;
            }
        }
    }
}
