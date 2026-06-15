// M?c dích: c?u hình filter toàn c?c cho pipeline MVC.
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
