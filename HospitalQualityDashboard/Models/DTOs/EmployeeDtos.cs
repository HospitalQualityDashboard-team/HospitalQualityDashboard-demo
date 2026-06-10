using System;

namespace HospitalQualityDashboard.Models.DTOs
{
    public class EmployeeSaveDto
    {
        public int NhanVienId { get; set; }
        public string MaNhanVien { get; set; }
        public string HoTen { get; set; }
        public DateTime? NgaySinh { get; set; }
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

    public class ProfileUpdateDto
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [System.ComponentModel.DataAnnotations.Display(Name = "Họ tên")]
        public string HoTen { get; set; }

        [System.ComponentModel.DataAnnotations.Display(Name = "Ngày sinh")]
        public DateTime? NgaySinh { get; set; }

        [System.ComponentModel.DataAnnotations.Display(Name = "Giới tính")]
        public string GioiTinh { get; set; }

        [System.ComponentModel.DataAnnotations.Display(Name = "Chức vụ")]
        public string ChucVu { get; set; }

        [System.ComponentModel.DataAnnotations.Display(Name = "Email")]
        public string Email { get; set; }

        [System.ComponentModel.DataAnnotations.Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; }
    }
}
