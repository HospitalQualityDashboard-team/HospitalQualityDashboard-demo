// Mục đích: đóng gói nội dung, liên kết nghiệp vụ và danh sách khoa/phòng nhận thông báo.
using HospitalQualityDashboardDemo.Models.Enums;

namespace HospitalQualityDashboardDemo.Models.DTOs
{
    public class NotificationSendDto
    {
        public string TieuDe { get; set; }
        public string NoiDung { get; set; }
        public LoaiThongBao LoaiThongBao { get; set; }
        public int? KyBaoCaoId { get; set; }
        public int? ChiSoChatLuongId { get; set; }
        public int? BaoCaoId { get; set; }
        public int[] SelectedKhoaPhongIds { get; set; }
    }

    public class IndicatorWarningTargetDto
    {
        public int KyBaoCaoId { get; set; }
        public int KhoaPhongId { get; set; }
        public int ChiSoChatLuongId { get; set; }
    }

    public class IndicatorWarningBatchResultDto
    {
        public int TotalRequested { get; set; }
        public int Sent { get; set; }
        public int AlreadySentToday { get; set; }
        public int AlreadySubmitted { get; set; }
        public int NotEligible { get; set; }
    }
}
