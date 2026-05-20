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
    GiaTriSoLieu NVARCHAR(MAX) NULL,
    TanSuatBaoCao TINYINT NOT NULL,
    LoaiCongThuc TINYINT NOT NULL,
    DonViTinh NVARCHAR(100) NULL,
    DangHoatDong BIT NOT NULL CONSTRAINT DF_ChiSoChatLuong_DangHoatDong DEFAULT (1),
    NgayTao DATETIME NOT NULL CONSTRAINT DF_ChiSoChatLuong_NgayTao DEFAULT (GETDATE()),
    NgayCapNhat DATETIME NULL,
    CONSTRAINT UQ_ChiSoChatLuong_MaChiSo UNIQUE (MaChiSo)
);

CREATE TABLE dbo.ChiSoMucTieu (
    ChiSoMucTieuId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChiSoMucTieu PRIMARY KEY,
    ChiSoChatLuongId INT NOT NULL,
    Nam INT NOT NULL,
    ToanTuSoSanh NVARCHAR(10) NOT NULL,
    GiaTriMucTieu DECIMAL(18,4) NULL,
    MoTaMucTieu NVARCHAR(500) NULL,
    CONSTRAINT FK_ChiSoMucTieu_ChiSoChatLuong FOREIGN KEY (ChiSoChatLuongId) REFERENCES dbo.ChiSoChatLuong(ChiSoChatLuongId),
    CONSTRAINT UQ_ChiSoMucTieu_ChiSo_Nam UNIQUE (ChiSoChatLuongId, Nam)
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
    CONSTRAINT CK_PhanCongChiSo_DateRange CHECK (DenNgay IS NULL OR TuNgay IS NULL OR TuNgay <= DenNgay)
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
    CONSTRAINT CK_KyBaoCao_DateRange CHECK (TuNgay <= DenNgay AND HanNop >= DenNgay),
    CONSTRAINT CK_KyBaoCao_TrangThai CHECK (TrangThai IN (1, 2, 3))
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
    NgayTao DATETIME NOT NULL CONSTRAINT DF_BaoCao_NgayTao DEFAULT (GETDATE()),
    NgayCapNhat DATETIME NULL,
    CONSTRAINT FK_BaoCao_KyBaoCao FOREIGN KEY (KyBaoCaoId) REFERENCES dbo.KyBaoCao(KyBaoCaoId),
    CONSTRAINT FK_BaoCao_KhoaPhong FOREIGN KEY (KhoaPhongId) REFERENCES dbo.KhoaPhong(KhoaPhongId),
    CONSTRAINT FK_BaoCao_ChiSo FOREIGN KEY (ChiSoChatLuongId) REFERENCES dbo.ChiSoChatLuong(ChiSoChatLuongId),
    CONSTRAINT FK_BaoCao_PhanCong FOREIGN KEY (PhanCongChiSoId) REFERENCES dbo.PhanCongChiSo(PhanCongChiSoId),
    CONSTRAINT FK_BaoCao_NguoiTao FOREIGN KEY (NguoiTaoId) REFERENCES dbo.TaiKhoan(TaiKhoanId),
    CONSTRAINT FK_BaoCao_NguoiGui FOREIGN KEY (NguoiGuiId) REFERENCES dbo.TaiKhoan(TaiKhoanId),
    CONSTRAINT UQ_BaoCao_Ky_Khoa_ChiSo UNIQUE (KyBaoCaoId, KhoaPhongId, ChiSoChatLuongId),
    CONSTRAINT CK_BaoCao_TrangThai CHECK (TrangThai IN (1, 2, 3, 4))
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
    BaoCaoId INT NULL,
    NguoiTaoId INT NULL,
    NgayTao DATETIME NOT NULL CONSTRAINT DF_ThongBao_NgayTao DEFAULT (GETDATE()),
    CONSTRAINT FK_ThongBao_KyBaoCao FOREIGN KEY (KyBaoCaoId) REFERENCES dbo.KyBaoCao(KyBaoCaoId),
    CONSTRAINT FK_ThongBao_BaoCao FOREIGN KEY (BaoCaoId) REFERENCES dbo.BaoCao(BaoCaoId),
    CONSTRAINT FK_ThongBao_NguoiTao FOREIGN KEY (NguoiTaoId) REFERENCES dbo.TaiKhoan(TaiKhoanId)
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

CREATE TABLE dbo.LichSuImport (
    LichSuImportId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LichSuImport PRIMARY KEY,
    LoaiImport TINYINT NOT NULL,
    TenFile NVARCHAR(255) NOT NULL,
    TongSoDong INT NOT NULL,
    SoDongThanhCong INT NOT NULL,
    SoDongLoi INT NOT NULL,
    NguoiImportId INT NOT NULL,
    NgayImport DATETIME NOT NULL CONSTRAINT DF_LichSuImport_NgayImport DEFAULT (GETDATE()),
    CONSTRAINT FK_LichSuImport_NguoiImport FOREIGN KEY (NguoiImportId) REFERENCES dbo.TaiKhoan(TaiKhoanId)
);

CREATE TABLE dbo.LichSuImportChiTiet (
    LichSuImportChiTietId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LichSuImportChiTiet PRIMARY KEY,
    LichSuImportId INT NOT NULL,
    SoDong INT NOT NULL,
    KhoaDuLieu NVARCHAR(255) NULL,
    HanhDong NVARCHAR(50) NULL,
    ThanhCong BIT NOT NULL,
    ThongBaoLoi NVARCHAR(1000) NULL,
    CONSTRAINT FK_LichSuImportChiTiet_LichSuImport FOREIGN KEY (LichSuImportId) REFERENCES dbo.LichSuImport(LichSuImportId)
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
CREATE INDEX IX_NhanVien_KhoaPhongId ON dbo.NhanVien(KhoaPhongId);
CREATE INDEX IX_TaiKhoan_KhoaPhongId ON dbo.TaiKhoan(KhoaPhongId);
CREATE INDEX IX_PhanCongChiSo_KhoaPhongId ON dbo.PhanCongChiSo(KhoaPhongId);
CREATE INDEX IX_PhanCongChiSo_ChiSoChatLuongId ON dbo.PhanCongChiSo(ChiSoChatLuongId);
CREATE INDEX IX_BaoCao_KyBaoCaoId ON dbo.BaoCao(KyBaoCaoId);
CREATE INDEX IX_BaoCao_KhoaPhongId ON dbo.BaoCao(KhoaPhongId);
CREATE INDEX IX_BaoCao_ChiSoChatLuongId ON dbo.BaoCao(ChiSoChatLuongId);
