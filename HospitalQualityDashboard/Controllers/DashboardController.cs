// Muc dich: dieu huong dashboard theo vai tro Admin/User sang Area tuong ung.
using System.Web.Mvc;

namespace HospitalQualityDashboard.Controllers
{
    public class DashboardController : PageController
    {
        public ActionResult Index()
        {
            if (IsAdmin)
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            else
            {
                return RedirectToAction("Index", "Dashboard", new { area = "User" });
            }
        }
    }
}
