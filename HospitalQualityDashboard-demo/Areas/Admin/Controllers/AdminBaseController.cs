using HospitalQualityDashboardDemo.Controllers;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.Admin.Controllers
{
    public abstract class AdminBaseController : PageController
    {
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
