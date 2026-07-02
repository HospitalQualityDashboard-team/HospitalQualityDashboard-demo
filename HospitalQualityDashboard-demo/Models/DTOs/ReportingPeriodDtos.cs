// Mục đích: đóng gói dữ liệu lưu kỳ báo cáo và yêu cầu sinh lịch hàng loạt.
using HospitalQualityDashboardDemo.Models.Enums;

namespace HospitalQualityDashboardDemo.Models.DTOs
{
    public class ReportingPeriodSaveDto
    {
        public int KyBaoCaoId { get; set; }
        public string TenKyBaoCao { get; set; }
        public TanSuatBaoCao LoaiKyBaoCao { get; set; }
        public System.DateTime TuNgay { get; set; }
        public System.DateTime DenNgay { get; set; }
        public System.DateTime HanNop { get; set; }
        public TrangThaiKyBaoCao TrangThai { get; set; }
    }

    public class ReportingPeriodScheduleDto
    {
        public int Year { get; set; }
        public int[] SelectedFrequencyValues { get; set; }
        public int DueDayOffset { get; set; }
        public TrangThaiKyBaoCao DefaultStatus { get; set; }
    }

    public class ReportingPeriodMaintenanceResultDto
    {
        public int OpenedCount { get; set; }
        public int ClosedCount { get; set; }
        public bool NotificationAutomationRan { get; set; }
        public System.DateTime RanAt { get; set; }
    }
}
