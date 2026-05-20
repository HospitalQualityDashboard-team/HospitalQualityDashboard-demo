using System.Web.Mvc;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard.Controllers
{
    public class DashboardController : PageController
    {
        private readonly DashboardService _service = new DashboardService();

        public ActionResult Index()
        {
            return View(_service.GetDashboard(IsAdmin, CurrentKhoaPhongId));
        }
    }
}
