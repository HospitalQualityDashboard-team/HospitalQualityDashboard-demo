namespace HospitalQualityDashboard.Models.DTOs
{
    public class EmployeeSaveDto
    {
        public int NhanVienId { get; set; }
        public string MaNhanVien { get; set; }
        public string HoTen { get; set; }
        public System.DateTime? NgaySinh { get; set; }
        public string GioiTinh { get; set; }
        public string ChucVu { get; set; }
        public string Email { get; set; }
        public string SoDienThoai { get; set; }
        public int KhoaPhongId { get; set; }
        public bool DangHoatDong { get; set; }
    }

    public class CreateUserAccountDto
    {
        public int NhanVienId { get; set; }
        public string TenDangNhap { get; set; }
        public string MatKhau { get; set; }
    }

    public class EmployeeImportDto
    {
        public System.Web.HttpPostedFileBase File { get; set; }
        public int? KhoaPhongId { get; set; }
        public int UserId { get; set; }
    }
}
