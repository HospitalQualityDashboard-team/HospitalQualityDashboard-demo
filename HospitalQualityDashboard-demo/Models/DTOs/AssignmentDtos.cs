// Mục đích: định nghĩa dữ liệu trao đổi cho truy vấn, xem trước và cập nhật phân công chỉ số.
namespace HospitalQualityDashboardDemo.Models.DTOs
{
    public class PreviewAssignmentDto
    {
        public int[] DepartmentIds { get; set; }
        public int[] IndicatorIds { get; set; }
    }

    public class AssignmentQueryDto
    {
        // Các bộ lọc nullable cho phép màn hình kết hợp nhiều chế độ xem trên cùng nguồn dữ liệu.
        public int? KhoaPhongId { get; set; }
        public int? ChiSoId { get; set; }
        public string TrangThai { get; set; }
        public string TrangThaiPhanCong { get; set; }
        public string Search { get; set; }
        public string ViewMode { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class AssignmentCommandDto
    {
        // Tích Descartes giữa hai tập ID tạo danh sách phân công cần thêm hoặc kích hoạt lại.
        public int[] DepartmentIds { get; set; }
        public int[] IndicatorIds { get; set; }
        public int CurrentUserId { get; set; }
    }

    public class AssignmentBulkActionDto
    {
        public int[] Ids { get; set; }
        public string ViewMode { get; set; }
    }
}
