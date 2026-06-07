// Mục đích: filter bảo vệ action theo trạng thái đăng nhập và role được phép.
using System.Web.Mvc;
using HospitalQualityDashboard.Models.Enums;

namespace HospitalQualityDashboard.Filters
{
    public class RequireLoginAttribute : ActionFilterAttribute
    {
        public LoaiTaiKhoan[] AllowedRoles { get; set; }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var session = filterContext.HttpContext.Session;
            if (session == null || session["TaiKhoanId"] == null)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary
                    {
                        { "controller", "Account" },
                        { "action", "Login" }
                    });
                return;
            }

            if (AllowedRoles != null && AllowedRoles.Length > 0)
            {
                var role = (LoaiTaiKhoan)session["LoaiTaiKhoan"];
                var allowed = false;
                foreach (var allowedRole in AllowedRoles)
                {
                    if (allowedRole == role)
                    {
                        allowed = true;
                        break;
                    }
                }

                if (!allowed)
                {
                    filterContext.Result = new HttpUnauthorizedResult();
                    return;
                }
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
