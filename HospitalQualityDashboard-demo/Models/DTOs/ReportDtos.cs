// Mục đích: định nghĩa bộ lọc danh sách báo cáo và dữ liệu lưu nháp/gửi báo cáo.
using HospitalQualityDashboardDemo.Models.Enums;

namespace HospitalQualityDashboardDemo.Models.DTOs
{
    public class ReportListQueryDto
    {
        public int? PeriodId { get; set; }
        public int? DepartmentId { get; set; }
        public int? IndicatorId { get; set; }
        // User luôn bị giới hạn bởi CurrentDepartmentId; Admin mới được dùng DepartmentId tự chọn.
        public bool IsAdmin { get; set; }
        public int? CurrentDepartmentId { get; set; }
    }

    public class ReportDraftDto
    {
        // Service tính lại KetQua và DatMucTieu từ dữ liệu đầu vào, không tin giá trị do client gửi lên.
        public int BaoCaoId { get; set; }
        public int KyBaoCaoId { get; set; }
        public int KhoaPhongId { get; set; }
        public int ChiSoChatLuongId { get; set; }
        public int PhanCongChiSoId { get; set; }
        public TrangThaiBaoCao TrangThai { get; set; }
        public decimal? TuSo { get; set; }
        public decimal? MauSo { get; set; }
        public decimal? GiaTriNhap { get; set; }
        public decimal? KetQua { get; set; }
        public bool? DatMucTieu { get; set; }
        public string GhiChu { get; set; }
        public string YKienPhanHoi { get; set; }
    }
}
