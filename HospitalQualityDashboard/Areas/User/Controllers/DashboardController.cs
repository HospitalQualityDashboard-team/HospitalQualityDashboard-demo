using System.Web.Mvc;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard.Areas.User.Controllers
{
    public class DashboardController : UserBaseController
    {
        private readonly DashboardService _service = new DashboardService();
        private readonly ReportingPeriodScheduleService _periodSchedule = new ReportingPeriodScheduleService();

        public ActionResult Index()
        {
            _periodSchedule.OpenDuePeriods(System.DateTime.Now);
            return View(_service.GetDashboard(false, CurrentKhoaPhongId));
        }
    }
}
