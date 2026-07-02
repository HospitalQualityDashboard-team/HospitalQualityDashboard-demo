-- Mục đích: tạo schema nền cho cơ sở dữ liệu chất lượng bệnh viện khi triển khai mới.

SET NOCOUNT ON;

-- TanSuatBaoCao:
-- 1 = Hàng Ngày
-- 2 = Tuần / Hàng tuần
-- 3 = Hàng Tháng
-- 4 = 3 Tháng / Quý / Hàng quý
-- 5 = 6 Tháng
-- 6 = 12 tháng / Hàng năm / 1 lần/năm
-- 7 = Khi có xảy ra phản ứng có hại / Khi phát sinh / Thường xuyên
-- 8 = Trước/sau khi thực hiện
-- 9 = 9 Tháng
--
-- TrangThaiKyBaoCao:
-- 1 = Nháp / chưa mở cho nhập liệu
-- 2 = Mở / đang cho khoa phòng ghi số
-- 3 = Khóa / đã khóa kỳ báo cáo
--
-- TrangThaiBaoCao (trạng thái ghi số):
-- 1 = Nháp / khoa phòng đang ghi số
-- 2 = Đã gửi / đã nộp đúng hạn
-- 3 = Quá hạn / đã nộp sau hạn
-- 4 = Đã khóa / admin đã chốt báo cáo
-- 5 = Đã duyệt / dự phòng nếu bật quy trình duyệt
-- 6 = Trả lại / dự phòng nếu bật quy trình duyệt

CREATE TABLE dbo.KhoaPhong (
    KhoaPhongId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_KhoaPhong PRIMARY KEY,
    IdKhoaPhongNguon INT NOT NULL,
    TenKhoaPhong NVARCHAR(255) NOT NULL,
    Used BIT NOT NULL CONSTRAINT DF_KhoaPhong_Used DEFAULT (1),
    GhiChu NVARCHAR(500) NULL,
    NgayTao DATETIME NOT NULL CONSTRAINT DF_KhoaPhong_NgayTao DEFAULT (GETDATE()),
    NgayCapNhat DATETIME NULL,
    CONSTRAINT UQ_KhoaPhong_IdKhoaPhongNguon UNIQUE (IdKhoaPhongNguon)
);

CREATE TABLE dbo.NhanVien (
    NhanVienId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NhanVien PRIMARY KEY,
    MaNhanVien NVARCHAR(50) NOT NULL,
    HoTen NVARCHAR(255) NOT NULL,
    NgaySinh DATE NULL,
    GioiTinh NVARCHAR(20) NULL,
    ChucVu NVARCHAR(255) NULL,
    Email NVARCHAR(255) NULL,
    SoDienThoai NVARCHAR(50) NULL,
    KhoaPhongId INT NOT NULL,
    DangHoatDong BIT NOT NULL CONSTRAINT DF_NhanVien_DangHoatDong DEFAULT (1),
    NgayTao DATETIME NOT NULL CONSTRAINT DF_NhanVien_NgayTao DEFAULT (GETDATE()),
    NgayCapNhat DATETIME NULL,
    CONSTRAINT UQ_NhanVien_MaNhanVien UNIQUE (MaNhanVien),
    CONSTRAINT FK_NhanVien_KhoaPhong FOREIGN KEY (KhoaPhongId) REFERENCES dbo.KhoaPhong(KhoaPhongId)
);

CREATE TABLE dbo.TaiKhoan (
    TaiKhoanId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TaiKhoan PRIMARY KEY,
    TenDangNhap NVARCHAR(100) NOT NULL,
    MatKhauHash NVARCHAR(500) NOT NULL,
    LoaiTaiKhoan TINYINT NOT NULL,
    NhanVienId INT NULL,
    KhoaPhongId INT NULL,
    DangHoatDong BIT NOT NULL CONSTRAINT DF_TaiKhoan_DangHoatDong DEFAULT (1),
    FailedLoginCount INT NOT NULL CONSTRAINT DF_TaiKhoan_FailedLoginCount DEFAULT (0),
    LockoutUntil DATETIME NULL,
    LanDangNhapCuoi DATETIME NULL,
    NgayTao DATETIME NOT NULL CONSTRAINT DF_TaiKhoan_NgayTao DEFAULT (GETDATE()),
    NgayCapNhat DATETIME NULL,
    CONSTRAINT UQ_TaiKhoan_TenDangNhap UNIQUE (TenDangNhap),
    CONSTRAINT FK_TaiKhoan_NhanVien FOREIGN KEY (NhanVienId) REFERENCES dbo.NhanVien(NhanVienId),
    CONSTRAINT FK_TaiKhoan_KhoaPhong FOREIGN KEY (KhoaPhongId) REFERENCES dbo.KhoaPhong(KhoaPhongId),
    CONSTRAINT CK_TaiKhoan_LoaiTaiKhoan CHECK (LoaiTaiKhoan IN (1, 2)),
    CONSTRAINT CK_TaiKhoan_UserHasKhoaPhong CHECK (LoaiTaiKhoan <> 2 OR KhoaPhongId IS NOT NULL)
);

CREATE TABLE dbo.ChiSoChatLuong (
    ChiSoChatLuongId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChiSoChatLuong PRIMARY KEY,
    MaChiSo NVARCHAR(50) NOT NULL,
    SoThuTu INT NULL,
    TenChiSo NVARCHAR(1000) NOT NULL,
    DinhNghia NVARCHAR(MAX) NULL,
    LinhVucApDung NVARCHAR(500) NULL,
    KhiaCanhChatLuong NVARCHAR(500) NULL,
    ThanhToChatLuong NVARCHAR(500) NULL,
    LyDoLuaChon NVARCHAR(MAX) NULL,
    PhuongPhapTinh NVARCHAR(MAX) NULL,
    TuSoMoTa NVARCHAR(MAX) NULL,
    MauSoMoTa NVARCHAR(MAX) NULL,
    NguonSoLieu NVARCHAR(MAX) NULL,
    ThuThapTongHop NVARCHAR(MAX) NULL,
    KhoaPhongThuThapId INT NULL,
    KhoaPhongTongHopId INT NULL,
    GiaTriSoLieu NVARCHAR(MAX) NULL,
    LoaiCongThuc TINYINT NOT NULL,
    DonViTinh NVARCHAR(100) NULL,
    DangHoatDong BIT NOT NULL CONSTRAINT DF_ChiSoChatLuong_DangHoatDong DEFAULT (1),
    NgayTao DATETIME NOT NULL CONSTRAINT DF_ChiSoChatLuong_NgayTao DEFAULT (GETDATE()),
    NgayCapNhat DATETIME NULL,
    CONSTRAINT UQ_ChiSoChatLuong_MaChiSo UNIQUE (MaChiSo),
    CONSTRAINT CK_ChiSoChatLuong_LoaiCongThuc CHECK (LoaiCongThuc IN (1, 2, 3, 4, 5, 6)),
    CONSTRAINT FK_ChiSo_KhoaPhongThuThap FOREIGN KEY (KhoaPhongThuThapId) REFERENCES dbo.KhoaPhong(KhoaPhongId),
    CONSTRAINT FK_ChiSo_KhoaPhongTongHop FOREIGN KEY (KhoaPhongTongHopId) REFERENCES dbo.KhoaPhong(KhoaPhongId)
);

CREATE TABLE dbo.ChiSoMucTieu (
    ChiSoMucTieuId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChiSoMucTieu PRIMARY KEY,
    ChiSoChatLuongId INT NOT NULL,
    Nam INT NOT NULL,
    ToanTuSoSanh NVARCHAR(10) NOT NULL,
    GiaTriMucTieu DECIMAL(18,4) NULL,
    MoTaMucTieu NVARCHAR(500) NULL,
    CONSTRAINT FK_ChiSoMucTieu_ChiSoChatLuong FOREIGN KEY (ChiSoChatLuongId) REFERENCES dbo.ChiSoChatLuong(ChiSoChatLuongId),
    CONSTRAINT UQ_ChiSoMucTieu_ChiSo_Nam UNIQUE (ChiSoChatLuongId, Nam),
    CONSTRAINT CK_ChiSoMucTieu_Nam CHECK (Nam BETWEEN 2000 AND 2100),
    CONSTRAINT CK_ChiSoMucTieu_ToanTu CHECK (ToanTuSoSanh IN (N'>', N'>=', N'<', N'<=', N'=', N'=='))
);

CREATE TABLE dbo.ChiSoTanSuatBaoCao (
    ChiSoTanSuatBaoCaoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChiSoTanSuatBaoCao PRIMARY KEY,
    ChiSoChatLuongId INT NOT NULL,
    TanSuatBaoCao TINYINT NOT NULL,
    CONSTRAINT FK_ChiSoTanSuatBaoCao_ChiSo FOREIGN KEY (ChiSoChatLuongId) REFERENCES dbo.ChiSoChatLuong(ChiSoChatLuongId),
    CONSTRAINT UQ_ChiSoTanSuatBaoCao UNIQUE (ChiSoChatLuongId, TanSuatBaoCao),
    CONSTRAINT CK_ChiSoTanSuatBaoCao_TanSuat CHECK (TanSuatBaoCao IN (1, 2, 3, 4, 5, 6, 7, 8, 9))
);

CREATE TABLE dbo.LichSuTrienKhaiChiSo (
    LichSuTrienKhaiChiSoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LichSuTrienKhaiChiSo PRIMARY KEY,
    ChiSoChatLuongId INT NOT NULL,
    TanSuatBaoCao TINYINT NOT NULL,
    TuNgayApDung DATE NOT NULL,
    DenNgayApDung DATE NULL,
    NguoiTaoId INT NULL,
    NgayTao DATETIME NOT NULL CONSTRAINT DF_LichSuTrienKhaiChiSo_NgayTao DEFAULT (GETDATE()),
    NguoiKetThucId INT NULL,
    NgayKetThuc DATETIME NULL,
    CONSTRAINT FK_LichSuTrienKhaiChiSo_ChiSo FOREIGN KEY (ChiSoChatLuongId) REFERENCES dbo.ChiSoChatLuong(ChiSoChatLuongId),
    CONSTRAINT FK_LichSuTrienKhaiChiSo_NguoiTao FOREIGN KEY (NguoiTaoId) REFERENCES dbo.TaiKhoan(TaiKhoanId),
    CONSTRAINT FK_LichSuTrienKhaiChiSo_NguoiKetThuc FOREIGN KEY (NguoiKetThucId) REFERENCES dbo.TaiKhoan(TaiKhoanId),
    CONSTRAINT CK_LichSuTrienKhaiChiSo_TanSuat CHECK (TanSuatBaoCao IN (1, 2, 3, 4, 5, 6, 7, 8, 9)),
    CONSTRAINT CK_LichSuTrienKhaiChiSo_DateRange CHECK (DenNgayApDung IS NULL OR TuNgayApDung <= DenNgayApDung)
);

CREATE TABLE dbo.PhanCongChiSo (
    PhanCongChiSoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PhanCongChiSo PRIMARY KEY,
    ChiSoChatLuongId INT NOT NULL,
    KhoaPhongId INT NOT NULL,
    TuNgay DATE NULL,
    DenNgay DATE NULL,
    DangHoatDong BIT NOT NULL CONSTRAINT DF_PhanCongChiSo_DangHoatDong DEFAULT (1),
    NguoiTaoId INT NOT NULL,
    NgayTao DATETIME NOT NULL CONSTRAINT DF_PhanCongChiSo_NgayTao DEFAULT (GETDATE()),
    CONSTRAINT FK_PhanCongChiSo_ChiSo FOREIGN KEY (ChiSoChatLuongId) REFERENCES dbo.ChiSoChatLuong(ChiSoChatLuongId),
    CONSTRAINT FK_PhanCongChiSo_KhoaPhong FOREIGN KEY (KhoaPhongId) REFERENCES dbo.KhoaPhong(KhoaPhongId),
    CONSTRAINT FK_PhanCongChiSo_NguoiTao FOREIGN KEY (NguoiTaoId) REFERENCES dbo.TaiKhoan(TaiKhoanId),
    CONSTRAINT CK_PhanCongChiSo_DateRange CHECK (DenNgay IS NULL OR TuNgay IS NULL OR TuNgay <= DenNgay),
    CONSTRAINT UQ_PhanCongChiSo_ChiSo_KhoaPhong UNIQUE (ChiSoChatLuongId, KhoaPhongId)
);

CREATE TABLE dbo.KyBaoCao (
    KyBaoCaoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_KyBaoCao PRIMARY KEY,
    TenKyBaoCao NVARCHAR(255) NOT NULL,
    LoaiKyBaoCao TINYINT NOT NULL,
    TuNgay DATE NOT NULL,
    DenNgay DATE NOT NULL,
    HanNop DATE NOT NULL,
    TrangThai TINYINT NOT NULL,
    NgayTao DATETIME NOT NULL CONSTRAINT DF_KyBaoCao_NgayTao DEFAULT (GETDATE()),
    NgayCapNhat DATETIME NULL,
    CONSTRAINT CK_KyBaoCao_LoaiKyBaoCao CHECK (LoaiKyBaoCao IN (1, 2, 3, 4, 5, 6, 7, 8, 9)),
    CONSTRAINT CK_KyBaoCao_DateRange CHECK (TuNgay <= DenNgay AND HanNop >= DenNgay),
    CONSTRAINT CK_KyBaoCao_TrangThai CHECK (TrangThai IN (1, 2, 3)),
    CONSTRAINT UQ_KyBaoCao_Loai_TuNgay_DenNgay UNIQUE (LoaiKyBaoCao, TuNgay, DenNgay)
);

CREATE TABLE dbo.BaoCao (
    BaoCaoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BaoCao PRIMARY KEY,
    KyBaoCaoId INT NOT NULL,
    KhoaPhongId INT NOT NULL,
    ChiSoChatLuongId INT NOT NULL,
    PhanCongChiSoId INT NULL,
    TrangThai TINYINT NOT NULL,
    NguoiTaoId INT NOT NULL,
    NguoiGuiId INT NULL,
    NgayGui DATETIME NULL,
    YKienPhanHoi NVARCHAR(MAX) NULL,
    NgayTao DATETIME NOT NULL CONSTRAINT DF_BaoCao_NgayTao DEFAULT (GETDATE()),
    NgayCapNhat DATETIME NULL,
    CONSTRAINT FK_BaoCao_KyBaoCao FOREIGN KEY (KyBaoCaoId) REFERENCES dbo.KyBaoCao(KyBaoCaoId),
    CONSTRAINT FK_BaoCao_KhoaPhong FOREIGN KEY (KhoaPhongId) REFERENCES dbo.KhoaPhong(KhoaPhongId),
    CONSTRAINT FK_BaoCao_ChiSo FOREIGN KEY (ChiSoChatLuongId) REFERENCES dbo.ChiSoChatLuong(ChiSoChatLuongId),
    CONSTRAINT FK_BaoCao_PhanCong FOREIGN KEY (PhanCongChiSoId) REFERENCES dbo.PhanCongChiSo(PhanCongChiSoId),
    CONSTRAINT FK_BaoCao_NguoiTao FOREIGN KEY (NguoiTaoId) REFERENCES dbo.TaiKhoan(TaiKhoanId),
    CONSTRAINT FK_BaoCao_NguoiGui FOREIGN KEY (NguoiGuiId) REFERENCES dbo.TaiKhoan(TaiKhoanId),
    CONSTRAINT UQ_BaoCao_Ky_Khoa_ChiSo UNIQUE (KyBaoCaoId, KhoaPhongId, ChiSoChatLuongId),
    CONSTRAINT CK_BaoCao_TrangThai CHECK (TrangThai IN (1, 2, 3, 4, 5, 6))
);

CREATE TABLE dbo.BaoCaoChiTiet (
    BaoCaoChiTietId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BaoCaoChiTiet PRIMARY KEY,
    BaoCaoId INT NOT NULL,
    TuSo DECIMAL(18,4) NULL,
    MauSo DECIMAL(18,4) NULL,
    GiaTriNhap DECIMAL(18,4) NULL,
    KetQua DECIMAL(18,4) NULL,
    KetQuaText NVARCHAR(255) NULL,
    DatMucTieu BIT NULL,
    GhiChu NVARCHAR(MAX) NULL,
    NgayCapNhat DATETIME NULL,
    CONSTRAINT FK_BaoCaoChiTiet_BaoCao FOREIGN KEY (BaoCaoId) REFERENCES dbo.BaoCao(BaoCaoId),
    CONSTRAINT UQ_BaoCaoChiTiet_BaoCao UNIQUE (BaoCaoId)
);

CREATE TABLE dbo.ThongBao (
    ThongBaoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ThongBao PRIMARY KEY,
    TieuDe NVARCHAR(255) NOT NULL,
    NoiDung NVARCHAR(MAX) NOT NULL,
    LoaiThongBao TINYINT NOT NULL,
    KyBaoCaoId INT NULL,
    ChiSoChatLuongId INT NULL,
    BaoCaoId INT NULL,
    NguoiTaoId INT NULL,
    NgayTao DATETIME NOT NULL CONSTRAINT DF_ThongBao_NgayTao DEFAULT (GETDATE()),
    CONSTRAINT FK_ThongBao_KyBaoCao FOREIGN KEY (KyBaoCaoId) REFERENCES dbo.KyBaoCao(KyBaoCaoId),
    CONSTRAINT FK_ThongBao_ChiSoChatLuong FOREIGN KEY (ChiSoChatLuongId) REFERENCES dbo.ChiSoChatLuong(ChiSoChatLuongId),
    CONSTRAINT FK_ThongBao_BaoCao FOREIGN KEY (BaoCaoId) REFERENCES dbo.BaoCao(BaoCaoId),
    CONSTRAINT FK_ThongBao_NguoiTao FOREIGN KEY (NguoiTaoId) REFERENCES dbo.TaiKhoan(TaiKhoanId),
    CONSTRAINT CK_ThongBao_LoaiThongBao CHECK (LoaiThongBao IN (1, 2, 3, 4, 5, 6, 7))
);

CREATE TABLE dbo.ThongBaoNguoiNhan (
    ThongBaoNguoiNhanId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ThongBaoNguoiNhan PRIMARY KEY,
    ThongBaoId INT NOT NULL,
    TaiKhoanId INT NOT NULL,
    DaDoc BIT NOT NULL CONSTRAINT DF_ThongBaoNguoiNhan_DaDoc DEFAULT (0),
    NgayDoc DATETIME NULL,
    CONSTRAINT FK_ThongBaoNguoiNhan_ThongBao FOREIGN KEY (ThongBaoId) REFERENCES dbo.ThongBao(ThongBaoId),
    CONSTRAINT FK_ThongBaoNguoiNhan_TaiKhoan FOREIGN KEY (TaiKhoanId) REFERENCES dbo.TaiKhoan(TaiKhoanId),
    CONSTRAINT UQ_ThongBaoNguoiNhan UNIQUE (ThongBaoId, TaiKhoanId)
);

CREATE TABLE dbo.ThongBaoTuDongLog (
    ThongBaoTuDongLogId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ThongBaoTuDongLog PRIMARY KEY,
    DedupKey NVARCHAR(255) NOT NULL,
    LoaiThongBao TINYINT NOT NULL,
    KyBaoCaoId INT NULL,
    KhoaPhongId INT NULL,
    ChiSoChatLuongId INT NULL,
    NgayMoc DATE NULL,
    NgayTao DATETIME NOT NULL CONSTRAINT DF_ThongBaoTuDongLog_NgayTao DEFAULT (GETDATE()),
    CONSTRAINT UQ_ThongBaoTuDongLog_DedupKey UNIQUE (DedupKey),
    CONSTRAINT FK_ThongBaoTuDongLog_KyBaoCao FOREIGN KEY (KyBaoCaoId) REFERENCES dbo.KyBaoCao(KyBaoCaoId),
    CONSTRAINT FK_ThongBaoTuDongLog_KhoaPhong FOREIGN KEY (KhoaPhongId) REFERENCES dbo.KhoaPhong(KhoaPhongId),
    CONSTRAINT FK_ThongBaoTuDongLog_ChiSoChatLuong FOREIGN KEY (ChiSoChatLuongId) REFERENCES dbo.ChiSoChatLuong(ChiSoChatLuongId),
    CONSTRAINT CK_ThongBaoTuDongLog_LoaiThongBao CHECK (LoaiThongBao IN (1, 2, 3, 4, 5, 6, 7))
);

CREATE TABLE dbo.LichSuImport (
    LichSuImportId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LichSuImport PRIMARY KEY,
    LoaiImport TINYINT NOT NULL,
    TenFile NVARCHAR(255) NOT NULL,
    TongSoDong INT NOT NULL,
    SoDongThanhCong INT NOT NULL,
    SoDongLoi INT NOT NULL,
    NguoiImportId INT NOT NULL,
    NgayImport DATETIME NOT NULL CONSTRAINT DF_LichSuImport_NgayImport DEFAULT (GETDATE()),
    CONSTRAINT FK_LichSuImport_NguoiImport FOREIGN KEY (NguoiImportId) REFERENCES dbo.TaiKhoan(TaiKhoanId),
    CONSTRAINT CK_LichSuImport_LoaiImport CHECK (LoaiImport IN (1, 2, 3)),
    CONSTRAINT CK_LichSuImport_SoDong CHECK (TongSoDong >= 0 AND SoDongThanhCong >= 0 AND SoDongLoi >= 0)
);

CREATE TABLE dbo.NhatKyHeThong (
    NhatKyHeThongId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NhatKyHeThong PRIMARY KEY,
    TaiKhoanId INT NULL,
    ChucNang NVARCHAR(100) NOT NULL,
    HanhDong NVARCHAR(100) NOT NULL,
    DoiTuong NVARCHAR(100) NULL,
    DoiTuongId INT NULL,
    NoiDung NVARCHAR(MAX) NULL,
    ThoiGian DATETIME NOT NULL CONSTRAINT DF_NhatKyHeThong_ThoiGian DEFAULT (GETDATE()),
    CONSTRAINT FK_NhatKyHeThong_TaiKhoan FOREIGN KEY (TaiKhoanId) REFERENCES dbo.TaiKhoan(TaiKhoanId)
);

CREATE INDEX IX_KhoaPhong_TenKhoaPhong ON dbo.KhoaPhong(TenKhoaPhong);
CREATE INDEX IX_KhoaPhong_Used ON dbo.KhoaPhong(Used, TenKhoaPhong);

CREATE INDEX IX_NhanVien_KhoaPhongId ON dbo.NhanVien(KhoaPhongId);
CREATE INDEX IX_NhanVien_HoTen ON dbo.NhanVien(HoTen);

CREATE INDEX IX_TaiKhoan_KhoaPhongId ON dbo.TaiKhoan(KhoaPhongId);
CREATE INDEX IX_TaiKhoan_NhanVienId ON dbo.TaiKhoan(NhanVienId);
CREATE INDEX IX_TaiKhoan_Loai_Active ON dbo.TaiKhoan(LoaiTaiKhoan, DangHoatDong, KhoaPhongId);
CREATE INDEX IX_TaiKhoan_LockoutUntil ON dbo.TaiKhoan(LockoutUntil);

CREATE INDEX IX_ChiSoChatLuong_Active_Order ON dbo.ChiSoChatLuong(DangHoatDong, SoThuTu, MaChiSo);
CREATE INDEX IX_ChiSoChatLuong_KhoaPhongThuThap ON dbo.ChiSoChatLuong(KhoaPhongThuThapId);
CREATE INDEX IX_ChiSoChatLuong_KhoaPhongTongHop ON dbo.ChiSoChatLuong(KhoaPhongTongHopId);

CREATE INDEX IX_ChiSoTanSuatBaoCao_TanSuat_ChiSo ON dbo.ChiSoTanSuatBaoCao(TanSuatBaoCao, ChiSoChatLuongId);
CREATE UNIQUE INDEX IX_LichSuTrienKhaiChiSo_Open ON dbo.LichSuTrienKhaiChiSo(ChiSoChatLuongId, TanSuatBaoCao) WHERE DenNgayApDung IS NULL;
CREATE INDEX IX_LichSuTrienKhaiChiSo_PeriodLookup ON dbo.LichSuTrienKhaiChiSo(ChiSoChatLuongId, TanSuatBaoCao, TuNgayApDung, DenNgayApDung);

CREATE INDEX IX_PhanCongChiSo_KhoaPhong_Active ON dbo.PhanCongChiSo(KhoaPhongId, DangHoatDong, ChiSoChatLuongId);
CREATE INDEX IX_PhanCongChiSo_ChiSoChatLuongId ON dbo.PhanCongChiSo(ChiSoChatLuongId);
CREATE INDEX IX_PhanCongChiSo_NguoiTaoId ON dbo.PhanCongChiSo(NguoiTaoId);

CREATE INDEX IX_KyBaoCao_TrangThai_Loai_HanNop ON dbo.KyBaoCao(TrangThai, LoaiKyBaoCao, HanNop);
CREATE INDEX IX_KyBaoCao_TuNgay ON dbo.KyBaoCao(TuNgay DESC);

CREATE INDEX IX_BaoCao_KyBaoCaoId ON dbo.BaoCao(KyBaoCaoId);
CREATE INDEX IX_BaoCao_KhoaPhong_TrangThai ON dbo.BaoCao(KhoaPhongId, TrangThai);
CREATE INDEX IX_BaoCao_ChiSoChatLuongId ON dbo.BaoCao(ChiSoChatLuongId);
CREATE INDEX IX_BaoCao_PhanCongChiSoId ON dbo.BaoCao(PhanCongChiSoId);
CREATE INDEX IX_BaoCao_TrangThai ON dbo.BaoCao(TrangThai);

CREATE INDEX IX_ThongBao_NgayTao ON dbo.ThongBao(NgayTao DESC);
CREATE INDEX IX_ThongBao_KyBaoCaoId ON dbo.ThongBao(KyBaoCaoId);
CREATE INDEX IX_ThongBao_BaoCaoId ON dbo.ThongBao(BaoCaoId);
CREATE INDEX IX_ThongBaoTuDongLog_IndicatorMarker ON dbo.ThongBaoTuDongLog(KyBaoCaoId, KhoaPhongId, ChiSoChatLuongId, NgayMoc, LoaiThongBao);

CREATE INDEX IX_ThongBaoNguoiNhan_TaiKhoan_DaDoc ON dbo.ThongBaoNguoiNhan(TaiKhoanId, DaDoc);

CREATE INDEX IX_ThongBaoTuDongLog_Ky_Khoa ON dbo.ThongBaoTuDongLog(KyBaoCaoId, KhoaPhongId);
CREATE INDEX IX_LichSuImport_NguoiImportId ON dbo.LichSuImport(NguoiImportId);
CREATE INDEX IX_NhatKyHeThong_TaiKhoan_ThoiGian ON dbo.NhatKyHeThong(TaiKhoanId, ThoiGian DESC);
CREATE INDEX IX_NhatKyHeThong_DoiTuong ON dbo.NhatKyHeThong(DoiTuong, DoiTuongId);
GO

CREATE FUNCTION dbo.fn_ChiSoDuocTrienKhaiTrongKy
(
    @ChiSoChatLuongId INT,
    @TanSuatBaoCao TINYINT,
    @TuNgayKy DATE,
    @DenNgayKy DATE
)
RETURNS BIT
AS
BEGIN
    DECLARE @Result BIT = 0;

    IF EXISTS (
        SELECT 1
        FROM dbo.LichSuTrienKhaiChiSo ls
        WHERE ls.ChiSoChatLuongId = @ChiSoChatLuongId
          AND ls.TanSuatBaoCao = @TanSuatBaoCao
          AND ls.TuNgayApDung <= @DenNgayKy
          AND (ls.DenNgayApDung IS NULL OR ls.DenNgayApDung >= @TuNgayKy)
    )
    BEGIN
        SET @Result = 1;
    END

    RETURN @Result;
END
GO
