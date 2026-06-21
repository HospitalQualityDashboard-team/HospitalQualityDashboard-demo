// Mục đích: hiển thị dashboard toàn viện và chuyển bộ lọc tần suất xuống tầng dịch vụ.
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.Enums;
using HospitalQualityDashboardDemo.Models.ViewModels;
using HospitalQualityDashboardDemo.Services;
using System;
using System.Diagnostics;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.Admin.Controllers
{
    public class DashboardController : AdminBaseController
    {
        private readonly DashboardService _service = new DashboardService();
        private readonly NotificationAutomationService _automation = new NotificationAutomationService();

        // Hiển thị danh sách và các bộ lọc của Dashboard chất lượng.
        public ActionResult Index(DashboardExcelExportQueryDto query, int? kyBaoCaoId = null)
        {
            RunNotificationAutomation();
            query = query ?? new DashboardExcelExportQueryDto();
            var model = _service.GetDashboard(true, null, query.TanSuat);
            _service.PrepareExportFilters(model, query, true, null);
            ViewBag.ComparisonData = _service.GetDashboardComparison(query.TanSuat, kyBaoCaoId);
            return View(model);
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
