// Mục đích: khởi động ứng dụng, bootstrap dữ liệu debug và đăng ký MVC pipeline.
using HospitalQualityDashboard.Services;
using System;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace HospitalQualityDashboard
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            new ReportingPeriodScheduleService().OpenDuePeriods(DateTime.Now);

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
