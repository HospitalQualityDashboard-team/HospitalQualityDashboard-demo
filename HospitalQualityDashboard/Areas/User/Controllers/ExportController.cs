using HospitalQualityDashboard.Models.DTOs;
using HospitalQualityDashboard.Services;
using System.Web.Mvc;

namespace HospitalQualityDashboard.Areas.User.Controllers
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
            }), "text/csv", "bao-cao.csv");
        }
    }
}
