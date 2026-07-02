// Mục đích: endpoint bảo trì để scheduler ngoài app kích hoạt automation theo lịch.
using HospitalQualityDashboardDemo.Services;
using System;
using System.Configuration;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Controllers
{
    public class MaintenanceController : Controller
    {
        private const string TokenHeaderName = "X-Maintenance-Token";
        private const string TokenConfigKey = "HospitalQualityMaintenanceToken";
        private readonly ReportingPeriodMaintenanceService _maintenance = new ReportingPeriodMaintenanceService();

        [HttpPost]
        public ActionResult RunReportingPeriodAutomation(string token)
        {
            if (!IsAuthorized(token))
            {
                return new HttpStatusCodeResult(403);
            }

            var now = GetVietnamLocalNow();
            var result = _maintenance.Run(now);
            return Json(new
            {
                openedCount = result.OpenedCount,
                closedCount = result.ClosedCount,
                ranAt = result.RanAt.ToString("o")
            });
        }

        private bool IsAuthorized(string queryToken)
        {
            var configuredToken = ConfigurationManager.AppSettings[TokenConfigKey];
            if (string.IsNullOrWhiteSpace(configuredToken))
            {
                return false;
            }

            var suppliedToken = Request.Headers[TokenHeaderName];
            if (string.IsNullOrWhiteSpace(suppliedToken))
            {
                suppliedToken = queryToken;
            }

            return string.Equals(configuredToken, suppliedToken, StringComparison.Ordinal);
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
