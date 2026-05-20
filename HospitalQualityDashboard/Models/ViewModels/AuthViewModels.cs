using System.ComponentModel.DataAnnotations;

namespace HospitalQualityDashboard.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui long nhap ten dang nhap.")]
        [Display(Name = "Ten dang nhap")]
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Vui long nhap mat khau.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mat khau")]
        public string MatKhau { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Vui long nhap mat khau hien tai.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mat khau hien tai")]
        public string MatKhauCu { get; set; }

        [Required(ErrorMessage = "Vui long nhap mat khau moi.")]
        [MinLength(6, ErrorMessage = "Mat khau moi phai co it nhat 6 ky tu.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mat khau moi")]
        public string MatKhauMoi { get; set; }

        [Required(ErrorMessage = "Vui long xac nhan mat khau moi.")]
        [Compare("MatKhauMoi", ErrorMessage = "Mat khau xac nhan khong khop.")]
        [DataType(DataType.Password)]
        [Display(Name = "Xac nhan mat khau moi")]
        public string XacNhanMatKhauMoi { get; set; }
    }
}
