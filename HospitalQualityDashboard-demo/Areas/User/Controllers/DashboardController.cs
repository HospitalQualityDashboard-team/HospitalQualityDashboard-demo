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
        private readonly NotificationAutomationService _automation = new NotificationAutomationService();

        // Hiển thị danh sách và các bộ lọc của Dashboard chất lượng.
        public ActionResult Index(DashboardExcelExportQueryDto query)
        {
            RunNotificationAutomation();
            query = query ?? new DashboardExcelExportQueryDto();
            query.KhoaPhongId = CurrentKhoaPhongId;
            var model = _service.GetDashboard(false, CurrentKhoaPhongId, query.TanSuat);
            _service.PrepareExportFilters(model, query, false, CurrentKhoaPhongId);
            return View(model);
        }

        private void RunNotificationAutomation()
        {
            try
            {
                _automation.Run(GetVietnamLocalNow());
            }
            catch (Exception exception)
            {
                Trace.TraceError("Dashboard notification automation failed: {0}", exception);
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
