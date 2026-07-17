// Mục đích: khai báo enum dùng chung cho role, trạng thái, tần suất và loại nghiệp vụ.
namespace HospitalQualityDashboardDemo.Models.Enums
{
    // Vai trò người dùng trong hệ thống (RBAC). Giá trị phải khớp với RoleId trong bảng dbo.Role.
    public enum AppRole : int
    {
        Admin = 1,
        User = 2,
        BoardOfDirectors = 3
    }

    public enum LoaiCongThuc : byte
    {
        TyLe = 1,
        SoLuong = 2,
        ThoiGianTrungBinh = 3,
        DiemTrungBinh = 4,
        GiaTriTrucTiep = 5,
        TySo = 6
    }

    public enum TanSuatBaoCao : byte
    {
        HangNgay = 1,
        HangTuan = 2,
        HangThang = 3,
        HangQuy = 4,
        SauThang = 5,
        HangNam = 6,
        KhiPhatSinh = 7,
        TruocSauKhiThucHien = 8,
        ChinThang = 9
    }

    public enum TrangThaiKyBaoCao : byte
    {
        Nhap = 1,
        Mo = 2,
        Khoa = 3
    }

    public enum TrangThaiBaoCao : byte
    {
        Nhap = 1,
        DaGui = 2, // Đã gửi chờ duyệt (giữ giá trị cũ để tương thích database)
        QuaHan = 3,
        DaKhoa = 4,
        DaDuyet = 5,
        TraLai = 6
    }

    public enum LoaiImport : byte
    {
        KhoaPhong = 1,
        NhanVien = 2,
        ChiSoChatLuong = 3
    }

    public enum LoaiThongBao : byte
    {
        ThuCong = 1,
        KyBaoCaoMo = 2,
        NhacHan = 3,
        QuaHan = 4,
        TongHopAdmin = 5,
        TuDong = 6,
        HanNopHomNay = 7
    }

    public enum IndicatorWarningResult : byte
    {
        Sent = 1,
        AlreadySentToday = 2,
        AlreadySubmitted = 3,
        NotEligible = 4
    }
}
