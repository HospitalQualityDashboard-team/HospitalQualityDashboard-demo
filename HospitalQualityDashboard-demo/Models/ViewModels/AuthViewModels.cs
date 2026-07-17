// Mục đích: view model cho đăng nhập, đổi mật khẩu và hồ sơ người dùng.

using System;
using System.ComponentModel.DataAnnotations;

namespace HospitalQualityDashboardDemo.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [Display(Name = "Tên đăng nhập")]
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string MatKhau { get; set; }

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string MatKhauCu { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string MatKhauMoi { get; set; }

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
        [Compare("MatKhauMoi", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        public string XacNhanMatKhauMoi { get; set; }
    }

    public class UserProfileViewModel
    {
        public int TaiKhoanId { get; set; }
        public string TenDangNhap { get; set; }
        public string RoleName { get; set; }
        public string LoaiTaiKhoanText
        {
            get 
            { 
                if (RoleName == "Admin") return "Quản trị viên";
                if (RoleName == "BoardOfDirectors") return "Ban giám đốc";
                return "Nhân viên";
            }
        }

        public int? NhanVienId { get; set; }
        public string MaNhanVien { get; set; }
        public string HoTen { get; set; }
        public DateTime? NgaySinh { get; set; }
        public string GioiTinh { get; set; }
        public string ChucVu { get; set; }
        public string Email { get; set; }
        public string SoDienThoai { get; set; }
        public int? KhoaPhongId { get; set; }
        public string TenKhoaPhong { get; set; }
        public bool TaiKhoanDangHoatDong { get; set; }
        public bool? NhanVienDangHoatDong { get; set; }
        public DateTime? LanDangNhapCuoi { get; set; }
        public ChangePasswordViewModel ChangePassword { get; set; }

        // Khởi tạo xác thực và hồ sơ người dùng với giá trị mặc định để các luồng xử lý phía sau không gặp trạng thái null ngoài ý muốn.
        public UserProfileViewModel()
        {
            ChangePassword = new ChangePasswordViewModel();
        }
    }
}
