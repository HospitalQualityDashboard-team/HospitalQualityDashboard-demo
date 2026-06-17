using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Services;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.User.Controllers
{
    public class ExportController : UserBaseController
    {
        private readonly ExportService _service = new ExportService();
        private readonly DashboardExcelExportService _dashboardExcelExport = new DashboardExcelExportService();

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

        public ActionResult Dashboard(DashboardExcelExportQueryDto query)
        {
            query = query ?? new DashboardExcelExportQueryDto();
            query.KhoaPhongId = CurrentKhoaPhongId;

            var result = _dashboardExcelExport.BuildDashboardExcel(query, new ExportUserContextDto
            {
                TaiKhoanId = CurrentTaiKhoanId.Value,
                TenDangNhap = CurrentTenDangNhap,
                IsAdmin = false,
                KhoaPhongId = CurrentKhoaPhongId,
                TenKhoaPhong = CurrentTenKhoaPhong,
                DiaChiIP = Request.UserHostAddress
            });

            return File(result.Content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.FileName);
        }
    }
}
