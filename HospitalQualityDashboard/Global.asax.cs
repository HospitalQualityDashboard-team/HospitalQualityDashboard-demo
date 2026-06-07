// Mục đích: khởi động ứng dụng, bootstrap dữ liệu debug và đăng ký MVC pipeline.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            DatabaseBootstrapper.BootstrapIfDebug();
            new ReportingPeriodScheduleService().OpenDuePeriods(DateTime.Now);

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
