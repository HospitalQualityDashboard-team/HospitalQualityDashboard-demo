// Mục đích: áp dụng kiểm tra đăng nhập và vai trò Admin chung cho toàn bộ controller quản trị.
using HospitalQualityDashboardDemo.Controllers;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.Admin.Controllers
{
    public abstract class AdminBaseController : PageController
    {
        // Kiểm tra session và quyền truy cập trước khi action được thực thi.
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            if (filterContext.Result != null)
            {
                return;
            }

            var admin = RequireAdmin();
            if (admin != null)
            {
                filterContext.Result = admin;
            }
        }
    }
}
