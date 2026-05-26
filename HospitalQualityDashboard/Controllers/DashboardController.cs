using System.Web.Mvc;
using System;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard.Controllers
{
    public class DashboardController : PageController
    {
        private readonly DashboardService _service = new DashboardService();
        private readonly NotificationAutomationService _automation = new NotificationAutomationService();

        public ActionResult Index()
        {
            if (IsAdmin)
            {
                _automation.Run(DateTime.Now);
            }

            return View(_service.GetDashboard(IsAdmin, CurrentKhoaPhongId));
        }
    }
}
