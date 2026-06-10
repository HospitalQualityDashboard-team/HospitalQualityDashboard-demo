using HospitalQualityDashboard.Services;
using System.Web.Mvc;

namespace HospitalQualityDashboard.Areas.Admin.Controllers
{
    public class DashboardController : AdminBaseController
    {
        private readonly DashboardService _service = new DashboardService();
        private readonly ReportingPeriodScheduleService _periodSchedule = new ReportingPeriodScheduleService();

        public ActionResult Index()
        {
            _periodSchedule.OpenDuePeriods(System.DateTime.Now);
            return View(_service.GetDashboard(true, null));
        }
    }
}
