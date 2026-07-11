// Mục đích: áp dụng kiểm tra đăng nhập, vai trò User và khoa/phòng hợp lệ cho các controller người dùng.
using HospitalQualityDashboardDemo.Controllers;
using HospitalQualityDashboardDemo.Models.Enums;
using HospitalQualityDashboardDemo.Services;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.User.Controllers
{
    public abstract class UserBaseController : PageController
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

            if (CurrentLoaiTaiKhoan != LoaiTaiKhoan.User || !CurrentKhoaPhongId.HasValue)
            {
                filterContext.Result = new HttpUnauthorizedResult();
                return;
            }

            var notificationPreview = _notificationService.GetPreviewForAccount(CurrentTaiKhoanId.Value);
            ViewBag.NotificationPreview = notificationPreview;
            ViewBag.UnreadNotificationCount = notificationPreview.UnreadCount;
        }
    }
}
