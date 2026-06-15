using HospitalQualityDashboardDemo.Services;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.User.Controllers
{
    public class DashboardController : UserBaseController
    {
        private readonly DashboardService _service = new DashboardService();

        public ActionResult Index()
        {
            return View(_service.GetDashboard(false, CurrentKhoaPhongId));
        }
    }
}
