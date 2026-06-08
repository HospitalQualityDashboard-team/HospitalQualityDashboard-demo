// Muc dich: chuyen huong Admin sang Area de quan ly ky bao cao.
using System.Web.Mvc;
using HospitalQualityDashboard.Models.ViewModels;

namespace HospitalQualityDashboard.Controllers
{
    public class ReportingPeriodController : PageController
    {
        public ActionResult Index()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Index", "ReportingPeriod", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OpenDuePeriods()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("OpenDuePeriods", "ReportingPeriod", new { area = "Admin" });
        }

        public ActionResult GenerateSchedule()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("GenerateSchedule", "ReportingPeriod", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PreviewSchedule(ReportingPeriodScheduleRequestViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("PreviewSchedule", "ReportingPeriod", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateSchedule(ReportingPeriodScheduleRequestViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("CreateSchedule", "ReportingPeriod", new { area = "Admin" });
        }

        public ActionResult Create()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Create", "ReportingPeriod", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(KyBaoCaoViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Create", "ReportingPeriod", new { area = "Admin" });
        }

        public ActionResult Edit(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Edit", "ReportingPeriod", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(KyBaoCaoViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Edit", "ReportingPeriod", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Open(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Open", "ReportingPeriod", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Lock(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Lock", "ReportingPeriod", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Delete", "ReportingPeriod", new { area = "Admin", id });
        }
    }
}
