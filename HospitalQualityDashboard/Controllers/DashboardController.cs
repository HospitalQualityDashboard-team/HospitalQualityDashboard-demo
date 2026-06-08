// Mục đích: hiển thị dashboard tổng hợp và kích hoạt tác vụ tự động liên quan.
using System.Web.Mvc;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard.Controllers
{
    public class DashboardController : PageController
    {
        private readonly DashboardService _service = new DashboardService();
        private readonly ReportingPeriodScheduleService _periodSchedule = new ReportingPeriodScheduleService();

        public ActionResult Index()
        {
            _periodSchedule.OpenDuePeriods(System.DateTime.Now);
            return View(_service.GetDashboard(IsAdmin, CurrentKhoaPhongId));
        }
    }
}
