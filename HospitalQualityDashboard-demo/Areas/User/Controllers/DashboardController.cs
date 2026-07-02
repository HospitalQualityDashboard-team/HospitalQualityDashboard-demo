// Mục đích: hiển thị dashboard trong phạm vi khoa/phòng của người dùng hiện tại.
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Services;
using System;
using System.Diagnostics;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.User.Controllers
{
    public class DashboardController : UserBaseController
    {
        private readonly DashboardService _service = new DashboardService();
        private readonly DashboardProgressComparisonService _comparisonService = new DashboardProgressComparisonService();
        private readonly ReportingPeriodMaintenanceService _maintenance = new ReportingPeriodMaintenanceService();

        // Hiển thị danh sách và các bộ lọc của Dashboard chất lượng.
        public ActionResult Index(DashboardExcelExportQueryDto query)
        {
            RunReportingPeriodMaintenance();
            query = query ?? new DashboardExcelExportQueryDto();
            query.KhoaPhongId = CurrentKhoaPhongId;
            var model = _service.GetDashboard(false, CurrentKhoaPhongId, query.TanSuat);
            _service.PrepareExportFilters(model, query, false, CurrentKhoaPhongId);
            model.ActiveTab = NormalizeDashboardTab(query.DashboardTab);
            if (model.ActiveTab == "comparison")
            {
                model.Comparison = _comparisonService.GetComparison(new DashboardAnalysisQueryDto
                {
                    TanSuat = query.TanSuat,
                    KyBaoCaoId = query.KyBaoCaoId,
                    ComparisonPeriodIds = query.ComparisonPeriodIds,
                    KhoaPhongId = CurrentKhoaPhongId,
                    Page = query.Page,
                    PageSize = query.PageSize
                }, false, CurrentKhoaPhongId);
            }
            else if (model.ActiveTab == "trend")
            {
                model.Trend = _comparisonService.GetTrend(new DashboardTrendQueryDto
                {
                    TanSuat = query.TanSuat,
                    KhoaPhongId = CurrentKhoaPhongId,
                    PeriodCount = query.PeriodCount
                }, false, CurrentKhoaPhongId);
            }
            return View(model);
        }

        private static string NormalizeDashboardTab(string value)
        {
            return value == "overview" || value == "trend" ? value : "comparison";
        }

        [HttpGet]
        public ActionResult Comparison(DashboardAnalysisQueryDto query)
        {
            query = query ?? new DashboardAnalysisQueryDto();
            query.KhoaPhongId = CurrentKhoaPhongId;
            try
            {
                var model = _comparisonService.GetComparison(query, false, CurrentKhoaPhongId);
                return PartialView("~/Views/Shared/_DashboardComparison.cshtml", model);
            }
            catch (InvalidOperationException exception)
            {
                return new HttpStatusCodeResult(400, exception.Message);
            }
        }

        [HttpGet]
        public ActionResult Trend(DashboardTrendQueryDto query)
        {
            query = query ?? new DashboardTrendQueryDto();
            query.KhoaPhongId = CurrentKhoaPhongId;
            var model = _comparisonService.GetTrend(query, false, CurrentKhoaPhongId);
            return PartialView("~/Views/Shared/_DashboardTrend.cshtml", model);
        }

        private void RunReportingPeriodMaintenance()
        {
            try
            {
                _maintenance.Run(GetVietnamLocalNow());
            }
            catch (Exception exception)
            {
                Trace.TraceError("Dashboard reporting period maintenance failed: {0}", exception);
            }
        }

        private static DateTime GetVietnamLocalNow()
        {
            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            }
            catch (TimeZoneNotFoundException)
            {
                return DateTime.Now;
            }
            catch (InvalidTimeZoneException)
            {
                return DateTime.Now;
            }
        }
    }
}
