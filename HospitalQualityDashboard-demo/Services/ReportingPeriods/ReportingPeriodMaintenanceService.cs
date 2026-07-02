// Mục đích: điều phối tự động mở kỳ, gửi thông báo và khóa kỳ báo cáo quá hạn.
using HospitalQualityDashboardDemo.Models.DTOs;
using System;

namespace HospitalQualityDashboardDemo.Services
{
    public class ReportingPeriodMaintenanceService
    {
        private readonly ReportingPeriodScheduleService _schedule;
        private readonly NotificationAutomationService _automation;

        public ReportingPeriodMaintenanceService()
            : this(new ReportingPeriodScheduleService(), new NotificationAutomationService())
        {
        }

        public ReportingPeriodMaintenanceService(
            ReportingPeriodScheduleService schedule,
            NotificationAutomationService automation)
        {
            if (schedule == null) throw new ArgumentNullException("schedule");
            if (automation == null) throw new ArgumentNullException("automation");

            _schedule = schedule;
            _automation = automation;
        }

        public ReportingPeriodMaintenanceResultDto Run(DateTime now)
        {
            var openedCount = _schedule.OpenDuePeriods(now);
            _automation.Run(now);
            var closedCount = _schedule.CloseOverduePeriods(now);

            return new ReportingPeriodMaintenanceResultDto
            {
                OpenedCount = openedCount,
                ClosedCount = closedCount,
                NotificationAutomationRan = true,
                RanAt = now
            };
        }
    }
}
