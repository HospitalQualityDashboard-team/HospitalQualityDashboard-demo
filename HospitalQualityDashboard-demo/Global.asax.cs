// Mục đích: khởi tạo ứng dụng MVC và đăng ký cấu hình toàn cục khi website bắt đầu chạy.
using HospitalQualityDashboardDemo.Services;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace HospitalQualityDashboardDemo
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            DatabaseBootstrapper.BootstrapIfExplicitlyEnabled();
            DatabaseBootstrapper.EnsureIndicatorDeploymentLifecycle();
        }
    }
}
