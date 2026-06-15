using HospitalQualityDashboardDemo.Services;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.Admin.Controllers
{
    public class DashboardController : AdminBaseController
    {
        private readonly DashboardService _service = new DashboardService();

        public ActionResult Index(int? tanSuat)
        {
            return View(_service.GetDashboard(true, null, tanSuat));
        }
    }
}
