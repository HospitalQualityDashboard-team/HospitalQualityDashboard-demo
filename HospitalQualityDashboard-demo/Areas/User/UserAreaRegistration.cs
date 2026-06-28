// Mục đích: đăng ký route riêng cho khu vực User để giới hạn luồng thao tác của khoa/phòng.
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.User
{
    public class UserAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get { return "User"; }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "User_default",
                "User/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional },
                new[] { "HospitalQualityDashboardDemo.Areas.User.Controllers" });
        }
    }
}
