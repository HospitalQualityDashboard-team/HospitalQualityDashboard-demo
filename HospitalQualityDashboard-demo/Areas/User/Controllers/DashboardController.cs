using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Services;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.User.Controllers
{
    public class DashboardController : UserBaseController
    {
        private readonly DashboardService _service = new DashboardService();

        public ActionResult Index(DashboardExcelExportQueryDto query)
        {
            query = query ?? new DashboardExcelExportQueryDto();
            query.KhoaPhongId = CurrentKhoaPhongId;
            var model = _service.GetDashboard(false, CurrentKhoaPhongId, query.TanSuat);
            _service.PrepareExportFilters(model, query, false, CurrentKhoaPhongId);
            return View(model);
        }
    }
}
