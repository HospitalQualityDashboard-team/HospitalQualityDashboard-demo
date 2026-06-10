// Muc dich: dieu huong thong bao theo vai tro sang Area tuong ung.
using HospitalQualityDashboard.Models.ViewModels;
using System.Web.Mvc;

namespace HospitalQualityDashboard.Controllers
{
    public class NotificationController : PageController
    {
        public ActionResult Index()
        {
            if (IsAdmin)
            {
                return RedirectToAction("Index", "Notification", new { area = "Admin" });
            }
            else
            {
                return RedirectToAction("Index", "Notification", new { area = "User" });
            }
        }

        public ActionResult Details(int id)
        {
            if (IsAdmin)
            {
                return RedirectToAction("Details", "Notification", new { area = "Admin", id });
            }
            else
            {
                return RedirectToAction("Details", "Notification", new { area = "User", id });
            }
        }

        public ActionResult Create()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Create", "Notification", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(NotificationViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Create", "Notification", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkAsRead(int id)
        {
            if (IsAdmin)
            {
                return RedirectToAction("MarkAsRead", "Notification", new { area = "Admin", id });
            }
            else
            {
                return RedirectToAction("MarkAsRead", "Notification", new { area = "User", id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkDetailAsRead(int id)
        {
            if (IsAdmin)
            {
                return RedirectToAction("MarkDetailAsRead", "Notification", new { area = "Admin", id });
            }
            else
            {
                return RedirectToAction("MarkDetailAsRead", "Notification", new { area = "User", id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OpenDuePeriodsAndRunAutomation()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("OpenDuePeriodsAndRunAutomation", "Notification", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RunAutomation()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("RunAutomation", "Notification", new { area = "Admin" });
        }
    }
}
