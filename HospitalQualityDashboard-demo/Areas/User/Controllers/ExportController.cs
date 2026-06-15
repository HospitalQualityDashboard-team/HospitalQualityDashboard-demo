using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Services;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.User.Controllers
{
    public class ExportController : UserBaseController
    {
        private readonly ExportService _service = new ExportService();

        public ActionResult Reports(int? kyBaoCaoId, int? chiSoChatLuongId)
        {
            return File(_service.ExportReports(new ReportExportQueryDto
            {
                PeriodId = kyBaoCaoId,
                DepartmentId = CurrentKhoaPhongId,
                IndicatorId = chiSoChatLuongId,
                IsAdmin = false,
                CurrentDepartmentId = CurrentKhoaPhongId
            }), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "bao-cao.xlsx");
        }
    }
}
