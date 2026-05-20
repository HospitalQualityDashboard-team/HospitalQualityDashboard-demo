using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;
using HospitalQualityDashboard.Models.Enums;

namespace HospitalQualityDashboard.Models.ViewModels
{
    public class KhoaPhongViewModel
    {
        public int KhoaPhongId { get; set; }

        [Required(ErrorMessage = "Vui long nhap ma khoa/phong nguon.")]
        [Display(Name = "Ma khoa/phong nguon")]
        public int IdKhoaPhongNguon { get; set; }

        [Required(ErrorMessage = "Vui long nhap ten khoa/phong.")]
        [Display(Name = "Ten khoa/phong")]
        public string TenKhoaPhong { get; set; }

        public bool Used { get; set; }
        public string GhiChu { get; set; }
    }

    public class KhoaPhongIndexViewModel
    {
        public string Search { get; set; }
        public IList<KhoaPhongViewModel> Items { get; set; }
        public ImportResultViewModel ImportResult { get; set; }
    }

    public class NhanVienViewModel
    {
        public int NhanVienId { get; set; }

        [Required(ErrorMessage = "Vui long nhap ma nhan vien.")]
        public string MaNhanVien { get; set; }

        [Required(ErrorMessage = "Vui long nhap ho ten.")]
        public string HoTen { get; set; }

        public DateTime? NgaySinh { get; set; }
        public string GioiTinh { get; set; }
        public string ChucVu { get; set; }
        public string Email { get; set; }
        public string SoDienThoai { get; set; }

        [Required(ErrorMessage = "Vui long chon khoa/phong.")]
        public int KhoaPhongId { get; set; }

        public string TenKhoaPhong { get; set; }
        public bool DangHoatDong { get; set; }
        public IList<SelectListItem> KhoaPhongOptions { get; set; }
    }

    public class NhanVienIndexViewModel
    {
        public int? KhoaPhongId { get; set; }
        public IList<SelectListItem> KhoaPhongOptions { get; set; }
        public IList<NhanVienViewModel> Items { get; set; }
        public ImportResultViewModel ImportResult { get; set; }
    }

    public class CreateUserAccountViewModel
    {
        public int NhanVienId { get; set; }
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Vui long nhap ten dang nhap.")]
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Vui long nhap mat khau.")]
        [MinLength(6, ErrorMessage = "Mat khau phai co it nhat 6 ky tu.")]
        public string MatKhau { get; set; }
    }

    public class ChiSoViewModel
    {
        public int ChiSoChatLuongId { get; set; }

        [Required(ErrorMessage = "Vui long nhap ma chi so.")]
        public string MaChiSo { get; set; }

        public int? SoThuTu { get; set; }

        [Required(ErrorMessage = "Vui long nhap ten chi so.")]
        public string TenChiSo { get; set; }

        public string DinhNghia { get; set; }
        public string LinhVucApDung { get; set; }
        public string KhiaCanhChatLuong { get; set; }
        public string ThanhToChatLuong { get; set; }
        public string LyDoLuaChon { get; set; }
        public string PhuongPhapTinh { get; set; }
        public string TuSoMoTa { get; set; }
        public string MauSoMoTa { get; set; }
        public string NguonSoLieu { get; set; }
        public string ThuThapTongHop { get; set; }
        public string GiaTriSoLieu { get; set; }
        public TanSuatBaoCao TanSuatBaoCao { get; set; }
        public LoaiCongThuc LoaiCongThuc { get; set; }
        public string DonViTinh { get; set; }
        public bool DangHoatDong { get; set; }
        public int? NamMucTieu { get; set; }
        public string ToanTuSoSanh { get; set; }
        public decimal? GiaTriMucTieu { get; set; }
        public string MoTaMucTieu { get; set; }
    }

    public class AssignmentViewModel
    {
        public int KhoaPhongId { get; set; }
        public int ChiSoChatLuongId { get; set; }
        public int[] SelectedChiSoIds { get; set; }
        public IList<SelectListItem> KhoaPhongOptions { get; set; }
        public IList<SelectListItem> ChiSoOptions { get; set; }
        public IList<AssignmentItemViewModel> Items { get; set; }
    }

    public class AssignmentItemViewModel
    {
        public int PhanCongChiSoId { get; set; }
        public string TenKhoaPhong { get; set; }
        public string MaChiSo { get; set; }
        public string TenChiSo { get; set; }
        public bool DangHoatDong { get; set; }
    }

    public class KyBaoCaoViewModel
    {
        public int KyBaoCaoId { get; set; }

        [Required(ErrorMessage = "Vui long nhap ten ky bao cao.")]
        public string TenKyBaoCao { get; set; }

        public TanSuatBaoCao LoaiKyBaoCao { get; set; }
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }
        public DateTime HanNop { get; set; }
        public TrangThaiKyBaoCao TrangThai { get; set; }
        public int TongBaoCao { get; set; }
        public int DaGui { get; set; }
    }

    public class ReportEntryViewModel
    {
        public int BaoCaoId { get; set; }
        public int KyBaoCaoId { get; set; }
        public int KhoaPhongId { get; set; }
        public int ChiSoChatLuongId { get; set; }
        public string TenKyBaoCao { get; set; }
        public string TenKhoaPhong { get; set; }
        public string MaChiSo { get; set; }
        public string TenChiSo { get; set; }
        public LoaiCongThuc LoaiCongThuc { get; set; }
        public TrangThaiBaoCao TrangThai { get; set; }
        public decimal? TuSo { get; set; }
        public decimal? MauSo { get; set; }
        public decimal? GiaTriNhap { get; set; }
        public decimal? KetQua { get; set; }
        public bool? DatMucTieu { get; set; }
        public string GhiChu { get; set; }
    }

    public class ReportListViewModel
    {
        public int? KyBaoCaoId { get; set; }
        public int? KhoaPhongId { get; set; }
        public int? ChiSoChatLuongId { get; set; }
        public IList<SelectListItem> KyBaoCaoOptions { get; set; }
        public IList<SelectListItem> KhoaPhongOptions { get; set; }
        public IList<SelectListItem> ChiSoOptions { get; set; }
        public IList<ReportEntryViewModel> Items { get; set; }
    }

    public class NotificationViewModel
    {
        public int ThongBaoId { get; set; }
        public string TieuDe { get; set; }
        public string NoiDung { get; set; }
        public LoaiThongBao LoaiThongBao { get; set; }
        public DateTime NgayTao { get; set; }
        public bool DaDoc { get; set; }
        public int[] SelectedKhoaPhongIds { get; set; }
        public IList<SelectListItem> KhoaPhongOptions { get; set; }
    }

    public class DashboardViewModel
    {
        public bool IsAdmin { get; set; }
        public int TongChiSo { get; set; }
        public int BaoCaoDaGui { get; set; }
        public int BaoCaoThieu { get; set; }
        public int BaoCaoQuaHan { get; set; }
        public int ChiSoDuocPhanCong { get; set; }
        public IList<DepartmentProgressViewModel> DepartmentProgress { get; set; }
    }

    public class DepartmentProgressViewModel
    {
        public string TenKhoaPhong { get; set; }
        public int Tong { get; set; }
        public int DaGui { get; set; }
    }

    public class ImportResultViewModel
    {
        public int TongSoDong { get; set; }
        public int SoDongThanhCong { get; set; }
        public int SoDongLoi { get; set; }
        public IList<string> Errors { get; set; }

        public ImportResultViewModel()
        {
            Errors = new List<string>();
        }
    }

    public class ImportFileViewModel
    {
        public HttpPostedFileBase File { get; set; }
        public int? KhoaPhongId { get; set; }
    }
}
