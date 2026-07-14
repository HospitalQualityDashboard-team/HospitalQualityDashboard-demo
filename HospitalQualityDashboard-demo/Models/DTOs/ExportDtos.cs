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
        public string DepartmentStatusFilter { get; set; }
        public int[] ComparisonPeriodIds { get; set; }
        public string DashboardTab { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class DashboardComparisonPeriodDto
    {
        public int KyBaoCaoId { get; set; }
        public string TenKyBaoCao { get; set; }
        public int TanSuat { get; set; }
        public System.DateTime TuNgay { get; set; }
        public int TrangThai { get; set; }
    }

    public class DashboardAnalysisQueryDto
    {
        public int? TanSuat { get; set; }
        public int? KyBaoCaoId { get; set; }
        public int[] ComparisonPeriodIds { get; set; }
        public int? KhoaPhongId { get; set; }
        public int? StatusPeriodId { get; set; }
        public string ProgressStatus { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class DashboardIndicatorPeriodValueDto
    {
        public int KyBaoCaoId { get; set; }
        public int KhoaPhongId { get; set; }
        public int ChiSoChatLuongId { get; set; }
        public string TenKhoaPhong { get; set; }
        public string MaChiSo { get; set; }
        public string TenChiSo { get; set; }
        public string DonViTinh { get; set; }
        public decimal? KetQua { get; set; }
        public bool IsExpected { get; set; }
        public bool IsSubmitted { get; set; }
        public bool? DatMucTieu { get; set; }
        public System.DateTime? NgayGui { get; set; }
        public System.DateTime HanNop { get; set; }
        public string ProgressStatus { get; set; }
    }

    public class DashboardIndicatorComparisonDto
    {
        public decimal? ChenhLech { get; set; }
        public string TrangThaiKyChinh { get; set; }
        public string TrangThaiKySoSanh { get; set; }
        public string XuHuong { get; set; }
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
