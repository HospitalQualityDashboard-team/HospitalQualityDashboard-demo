using System.Web.Mvc;
using HospitalQualityDashboard.Controllers;
using HospitalQualityDashboard.Models.Enums;

namespace HospitalQualityDashboard.Areas.User.Controllers
{
    public abstract class UserBaseController : PageController
    {
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
            }
        }
    }
}
