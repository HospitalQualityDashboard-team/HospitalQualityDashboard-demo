// Mục đích: cấu hình filter toàn cục cho pipeline MVC.
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
