// Mục đích: gom bộ lọc xuất dữ liệu, ngữ cảnh người xuất và kết quả tạo workbook.
namespace HospitalQualityDashboardDemo.Models.DTOs
{
    public class EmployeeExportQueryDto
    {
        public int? DepartmentId { get; set; }
        // Service dùng cặp IsAdmin/CurrentDepartmentId để khóa phạm vi dữ liệu ở phía máy chủ.
        public bool IsAdmin { get; set; }
        public int? CurrentDepartmentId { get; set; }
    }

    public class ReportExportQueryDto
    {
        public int? PeriodId { get; set; }
        public int? DepartmentId { get; set; }
        public int? IndicatorId { get; set; }
        public bool IsAdmin { get; set; }
        public int? CurrentDepartmentId { get; set; }
    }

    public class AssignmentExportQueryDto
    {
        public int? DepartmentId { get; set; }
        public int? IndicatorId { get; set; }
        public string Status { get; set; }
        public string AssignmentStatus { get; set; }
        public string Search { get; set; }
        public string[] Columns { get; set; }
    }

    public class DashboardExcelExportQueryDto
    {
        // Các giá trị null có nghĩa là không áp dụng bộ lọc tương ứng.
        public int? NamBaoCao { get; set; }
        public int? KyBaoCaoId { get; set; }
        public int? TanSuat { get; set; }
        public int? KhoaPhongId { get; set; }
        public string LinhVuc { get; set; }
        public int? TrangThaiNhapLieu { get; set; }
        public int? TrangThaiDuyet { get; set; }
        public bool? DatMucTieu { get; set; }
    }

    public class ExportUserContextDto
    {
        // Ngữ cảnh này vừa giới hạn dữ liệu vừa cung cấp thông tin ghi audit lịch sử xuất.
        public int TaiKhoanId { get; set; }
        public string TenDangNhap { get; set; }
        public bool IsAdmin { get; set; }
        public int? KhoaPhongId { get; set; }
        public string TenKhoaPhong { get; set; }
        public string DiaChiIP { get; set; }
    }

    public class DashboardExcelExportResultDto
    {
        public byte[] Content { get; set; }
        public string FileName { get; set; }
        public int RowCount { get; set; }
    }
}
