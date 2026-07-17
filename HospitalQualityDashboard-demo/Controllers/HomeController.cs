// Mục đích: điều hướng các trang thông tin cơ bản của ứng dụng.
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Controllers
{
    public class HomeController : Controller
    {
        // Hiển thị danh sách và các bộ lọc của trang chủ ứng dụng.
        public ActionResult Index()
        {
            var user = new HospitalQualityDashboardDemo.Services.AuthService().TryAutoLogin(Request, Session);
            if (user != null)
            {
                if (user.RoleName == "Admin" || user.RoleName == "BoardOfDirectors")
                {
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                }
                return RedirectToAction("Index", "Dashboard", new { area = "User" });
            }
            return View();
        }
    }
}
