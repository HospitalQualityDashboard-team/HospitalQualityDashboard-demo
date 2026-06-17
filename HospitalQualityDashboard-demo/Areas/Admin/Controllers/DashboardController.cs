using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Services;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.Admin.Controllers
{
    public class DashboardController : AdminBaseController
    {
        private readonly DashboardService _service = new DashboardService();

        public ActionResult Index(DashboardExcelExportQueryDto query)
        {
            query = query ?? new DashboardExcelExportQueryDto();
            var model = _service.GetDashboard(true, null, query.TanSuat);
            _service.PrepareExportFilters(model, query, true, null);
            return View(model);
        }
    }
}
