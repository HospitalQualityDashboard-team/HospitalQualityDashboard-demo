// Má»¥c Ä‘Ã­ch: Ä‘á»‹nh nghÄ©a route máº·c Ä‘á»‹nh Ä‘á»ƒ Ä‘iá»u hÆ°á»›ng controller/action/id.
using System.Web.Mvc;
using System.Web.Routing;

namespace HospitalQualityDashboardDemo
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "HospitalQualityDashboardDemo.Controllers" }
            );
        }
    }
}
