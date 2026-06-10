// Mục đích: tập hợp entity thuần phản ánh các bảng dữ liệu cốt lõi của hệ thống.
using HospitalQualityDashboard.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HospitalQualityDashboard.Models.Entities
{
    public class KhoaPhong
    {
        public int KhoaPhongId { get; set; }

        [Required]
        public int IdKhoaPhongNguon { get; set; }

        [Required]
        [StringLength(255)]
        public string TenKhoaPhong { get; set; }

        public bool Used { get; set; }

        [StringLength(500)]
        public string GhiChu { get; set; }

        public DateTime NgayTao { get; set; }
        public DateTime? NgayCapNhat { get; set; }

        public virtual ICollection<NhanVien> NhanViens { get; set; }
        public virtual ICollection<TaiKhoan> TaiKhoans { get; set; }
        public virtual ICollection<PhanCongChiSo> PhanCongChiSos { get; set; }

        public KhoaPhong()
        {
            Used = true;
            NgayTao = DateTime.UtcNow;
            NhanViens = new List<NhanVien>();
            TaiKhoans = new List<TaiKhoan>();
            PhanCongChiSos = new List<PhanCongChiSo>();
        }
    }

    public class NhanVien
    {
        public int NhanVienId { get; set; }

        [Required]
        [StringLength(50)]
        public string MaNhanVien { get; set; }

        [Required]
        [StringLength(255)]
        public string HoTen { get; set; }

        public DateTime? NgaySinh { get; set; }

        [StringLength(20)]
        public string GioiTinh { get; set; }

        [StringLength(255)]
        public string ChucVu { get; set; }

        [StringLength(255)]
        public string Email { get; set; }

        [StringLength(50)]
        public string SoDienThoai { get; set; }

        public int KhoaPhongId { get; set; }
        public bool DangHoatDong { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime? NgayCapNhat { get; set; }

        public virtual KhoaPhong KhoaPhong { get; set; }
        public virtual TaiKhoan TaiKhoan { get; set; }

        public NhanVien()
        {
            DangHoatDong = true;
            NgayTao = DateTime.UtcNow;
        }
    }

    public class TaiKhoan
    {
        public int TaiKhoanId { get; set; }

        [Required]
        [StringLength(100)]
        public string TenDangNhap { get; set; }

        [Required]
        [StringLength(500)]
        public string MatKhauHash { get; set; }

        public LoaiTaiKhoan LoaiTaiKhoan { get; set; }
        public int? NhanVienId { get; set; }
        public int? KhoaPhongId { get; set; }
        public bool DangHoatDong { get; set; }
        public DateTime? LanDangNhapCuoi { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime? NgayCapNhat { get; set; }

        public virtual NhanVien NhanVien { get; set; }
        public virtual KhoaPhong KhoaPhong { get; set; }

        public TaiKhoan()
        {
            DangHoatDong = true;
            NgayTao = DateTime.UtcNow;
        }
    }

    public class ChiSoChatLuong
    {
        public int ChiSoChatLuongId { get; set; }

        [Required]
        [StringLength(50)]
        public string MaChiSo { get; set; }

        public int? SoThuTu { get; set; }

        [Required]
        [StringLength(1000)]
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

        [StringLength(100)]
        public string DonViTinh { get; set; }

        public bool DangHoatDong { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime? NgayCapNhat { get; set; }

        public virtual ICollection<ChiSoMucTieu> MucTieus { get; set; }
        public virtual ICollection<PhanCongChiSo> PhanCongChiSos { get; set; }

        public ChiSoChatLuong()
        {
            DangHoatDong = true;
            NgayTao = DateTime.UtcNow;
            MucTieus = new List<ChiSoMucTieu>();
            PhanCongChiSos = new List<PhanCongChiSo>();
        }
    }

    public class ChiSoMucTieu
    {
        public int ChiSoMucTieuId { get; set; }
        public int ChiSoChatLuongId { get; set; }
        public int Nam { get; set; }

        [Required]
        [StringLength(10)]
        public string ToanTuSoSanh { get; set; }

        public decimal? GiaTriMucTieu { get; set; }

        [StringLength(500)]
        public string MoTaMucTieu { get; set; }

        public virtual ChiSoChatLuong ChiSoChatLuong { get; set; }
    }

    public class PhanCongChiSo
    {
        public int PhanCongChiSoId { get; set; }
        public int ChiSoChatLuongId { get; set; }
        public int KhoaPhongId { get; set; }
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }
        public bool DangHoatDong { get; set; }
        public int NguoiTaoId { get; set; }
        public DateTime NgayTao { get; set; }

        public virtual ChiSoChatLuong ChiSoChatLuong { get; set; }
        public virtual KhoaPhong KhoaPhong { get; set; }
        public virtual TaiKhoan NguoiTao { get; set; }

        public PhanCongChiSo()
        {
            DangHoatDong = true;
            NgayTao = DateTime.UtcNow;
        }
    }

    public class KyBaoCao
    {
        public int KyBaoCaoId { get; set; }

        [Required]
        [StringLength(255)]
        public string TenKyBaoCao { get; set; }

        public TanSuatBaoCao LoaiKyBaoCao { get; set; }
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }
        public DateTime HanNop { get; set; }
        public TrangThaiKyBaoCao TrangThai { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime? NgayCapNhat { get; set; }

        public virtual ICollection<BaoCao> BaoCaos { get; set; }

        public KyBaoCao()
        {
            TrangThai = TrangThaiKyBaoCao.Nhap;
            NgayTao = DateTime.UtcNow;
            BaoCaos = new List<BaoCao>();
        }
    }

    public class BaoCao
    {
        public int BaoCaoId { get; set; }
        public int KyBaoCaoId { get; set; }
        public int KhoaPhongId { get; set; }
        public int ChiSoChatLuongId { get; set; }
        public int? PhanCongChiSoId { get; set; }
        public TrangThaiBaoCao TrangThai { get; set; }
        public int NguoiTaoId { get; set; }
        public int? NguoiGuiId { get; set; }
        public DateTime? NgayGui { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime? NgayCapNhat { get; set; }

        public virtual KyBaoCao KyBaoCao { get; set; }
        public virtual KhoaPhong KhoaPhong { get; set; }
        public virtual ChiSoChatLuong ChiSoChatLuong { get; set; }
        public virtual PhanCongChiSo PhanCongChiSo { get; set; }
        public virtual TaiKhoan NguoiTao { get; set; }
        public virtual TaiKhoan NguoiGui { get; set; }
        public virtual BaoCaoChiTiet ChiTiet { get; set; }

        public BaoCao()
        {
            TrangThai = TrangThaiBaoCao.Nhap;
            NgayTao = DateTime.UtcNow;
        }
    }

    public class BaoCaoChiTiet
    {
        public int BaoCaoChiTietId { get; set; }
        public int BaoCaoId { get; set; }
        public decimal? TuSo { get; set; }
        public decimal? MauSo { get; set; }
        public decimal? GiaTriNhap { get; set; }
        public decimal? KetQua { get; set; }

        [StringLength(255)]
        public string KetQuaText { get; set; }

        public bool? DatMucTieu { get; set; }
        public string GhiChu { get; set; }
        public DateTime? NgayCapNhat { get; set; }

        public virtual BaoCao BaoCao { get; set; }
    }

    public class ThongBao
    {
        public int ThongBaoId { get; set; }

        [Required]
        [StringLength(255)]
        public string TieuDe { get; set; }

        [Required]
        public string NoiDung { get; set; }

        public LoaiThongBao LoaiThongBao { get; set; }
        public int? KyBaoCaoId { get; set; }
        public int? BaoCaoId { get; set; }
        public int? NguoiTaoId { get; set; }
        public DateTime NgayTao { get; set; }

        public virtual KyBaoCao KyBaoCao { get; set; }
        public virtual BaoCao BaoCao { get; set; }
        public virtual TaiKhoan NguoiTao { get; set; }
        public virtual ICollection<ThongBaoNguoiNhan> NguoiNhans { get; set; }

        public ThongBao()
        {
            NgayTao = DateTime.UtcNow;
            NguoiNhans = new List<ThongBaoNguoiNhan>();
        }
    }

    public class ThongBaoNguoiNhan
    {
        public int ThongBaoNguoiNhanId { get; set; }
        public int ThongBaoId { get; set; }
        public int TaiKhoanId { get; set; }
        public bool DaDoc { get; set; }
        public DateTime? NgayDoc { get; set; }

        public virtual ThongBao ThongBao { get; set; }
        public virtual TaiKhoan TaiKhoan { get; set; }
    }

    public class LichSuImport
    {
        public int LichSuImportId { get; set; }
        public LoaiImport LoaiImport { get; set; }

        [Required]
        [StringLength(255)]
        public string TenFile { get; set; }

        public int TongSoDong { get; set; }
        public int SoDongThanhCong { get; set; }
        public int SoDongLoi { get; set; }
        public int NguoiImportId { get; set; }
        public DateTime NgayImport { get; set; }

        public virtual TaiKhoan NguoiImport { get; set; }

        public LichSuImport()
        {
            NgayImport = DateTime.UtcNow;
        }
    }

    public class NhatKyHeThong
    {
        public int NhatKyHeThongId { get; set; }
        public int? TaiKhoanId { get; set; }

        [Required]
        [StringLength(100)]
        public string ChucNang { get; set; }

        [Required]
        [StringLength(100)]
        public string HanhDong { get; set; }

        [StringLength(100)]
        public string DoiTuong { get; set; }

        public int? DoiTuongId { get; set; }
        public string NoiDung { get; set; }
        public DateTime ThoiGian { get; set; }

        public virtual TaiKhoan TaiKhoan { get; set; }

        public NhatKyHeThong()
        {
            ThoiGian = DateTime.UtcNow;
        }
    }
}
