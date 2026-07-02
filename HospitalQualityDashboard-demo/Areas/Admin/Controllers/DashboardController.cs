// Mục đích: hiển thị dashboard toàn viện và chuyển bộ lọc tần suất xuống tầng dịch vụ.
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.Enums;
using HospitalQualityDashboardDemo.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.Admin.Controllers
{
    public class DashboardController : AdminBaseController
    {
        private readonly DashboardService _service = new DashboardService();
        private readonly DashboardProgressComparisonService _comparisonService = new DashboardProgressComparisonService();
        private readonly NotificationAutomationService _automation = new NotificationAutomationService();
        private readonly ReportingPeriodMaintenanceService _maintenance = new ReportingPeriodMaintenanceService();

        // Hiển thị danh sách và các bộ lọc của Dashboard chất lượng.
        public ActionResult Index(DashboardExcelExportQueryDto query)
        {
            RunReportingPeriodMaintenance();
            query = query ?? new DashboardExcelExportQueryDto();
            var model = _service.GetDashboard(true, null, query.TanSuat);
            _service.PrepareExportFilters(model, query, true, null);
            model.ActiveTab = NormalizeDashboardTab(query.DashboardTab);
            if (model.ActiveTab == "comparison")
            {
                model.Comparison = _comparisonService.GetComparison(new DashboardAnalysisQueryDto
                {
                    TanSuat = query.TanSuat,
                    KyBaoCaoId = query.KyBaoCaoId,
                    ComparisonPeriodIds = query.ComparisonPeriodIds,
                    KhoaPhongId = query.KhoaPhongId,
                    Page = query.Page,
                    PageSize = query.PageSize
                }, true, null);
            }
            else if (model.ActiveTab == "trend")
            {
                model.Trend = _comparisonService.GetTrend(new DashboardTrendQueryDto
                {
                    TanSuat = query.TanSuat,
                    KhoaPhongId = query.KhoaPhongId,
                    PeriodCount = query.PeriodCount
                }, true, null);
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
            try
            {
                var model = _comparisonService.GetComparison(query, true, null);
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
            var model = _comparisonService.GetTrend(query, true, null);
            return PartialView("~/Views/Shared/_DashboardTrend.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult WarnIndicator(
            int kyBaoCaoId,
            int khoaPhongId,
            int chiSoChatLuongId,
            int? tanSuat)
        {
            var result = _automation.SendIndicatorWarning(
                kyBaoCaoId,
                khoaPhongId,
                chiSoChatLuongId,
                CurrentTaiKhoanId.Value,
                GetVietnamLocalNow());

            switch (result)
            {
                case IndicatorWarningResult.Sent:
                    TempData["Message"] = "Đã gửi cảnh báo tới khoa/phòng phụ trách chỉ số.";
                    break;
                case IndicatorWarningResult.AlreadySentToday:
                    TempData["Message"] = "Chỉ số này đã được cảnh báo trong hôm nay.";
                    break;
                case IndicatorWarningResult.AlreadySubmitted:
                    TempData["Message"] = "Chỉ số vừa được nộp nên không cần cảnh báo.";
                    break;
                default:
                    TempData["Message"] = "Không thể cảnh báo vì chỉ số không còn thuộc phạm vi cần nộp.";
                    break;
            }

            return RedirectToAction("Index", new { tanSuat = tanSuat });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult WarnIndicators(string[] warningTargets, int? tanSuat)
        {
            var targets = ParseWarningTargets(warningTargets);
            if (targets.Count == 0)
            {
                TempData["Message"] = "Vui lòng chọn ít nhất một chỉ số cần cảnh báo.";
                return RedirectToAction("Index", new { tanSuat = tanSuat });
            }

            var summary = _automation.SendIndicatorWarnings(
                targets,
                CurrentTaiKhoanId.Value,
                GetVietnamLocalNow());

            TempData["Message"] = string.Format(
                "Đã gửi {0}/{1} cảnh báo. Đã cảnh báo hôm nay: {2}. Đã nộp hoặc không còn hợp lệ: {3}.",
                summary.Sent,
                summary.TotalRequested,
                summary.AlreadySentToday,
                summary.AlreadySubmitted + summary.NotEligible);

            return RedirectToAction("Index", new { tanSuat = tanSuat });
        }

        private static IList<IndicatorWarningTargetDto> ParseWarningTargets(string[] warningTargets)
        {
            var targets = new List<IndicatorWarningTargetDto>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (warningTargets == null)
            {
                return targets;
            }

            foreach (var warningTarget in warningTargets)
            {
                if (string.IsNullOrWhiteSpace(warningTarget) || !seen.Add(warningTarget))
                {
                    continue;
                }

                var parts = warningTarget.Split('|');
                int periodId;
                int departmentId;
                int indicatorId;
                if (parts.Length != 3
                    || !int.TryParse(parts[0], out periodId)
                    || !int.TryParse(parts[1], out departmentId)
                    || !int.TryParse(parts[2], out indicatorId))
                {
                    continue;
                }

                targets.Add(new IndicatorWarningTargetDto
                {
                    KyBaoCaoId = periodId,
                    KhoaPhongId = departmentId,
                    ChiSoChatLuongId = indicatorId
                });
            }

            return targets;
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
