// Mục đích: điều hướng các trang thông tin cơ bản của ứng dụng.
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Controllers
{
    public class HomeController : Controller
    {
        // Hiển thị danh sách và các bộ lọc của trang chủ ứng dụng.
        public ActionResult Index()
        {
            return View();
        }
    }
}
