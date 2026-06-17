IF OBJECT_ID('dbo.LichSuXuatBaoCao', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LichSuXuatBaoCao (
        LichSuXuatBaoCaoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LichSuXuatBaoCao PRIMARY KEY,
        NguoiDungId INT NOT NULL,
        LoaiBaoCao NVARCHAR(100) NOT NULL,
        BoLoc NVARCHAR(MAX) NULL,
        TenFile NVARCHAR(255) NOT NULL,
        SoDongDuLieu INT NOT NULL CONSTRAINT DF_LichSuXuatBaoCao_SoDongDuLieu DEFAULT (0),
        NgayXuat DATETIME NOT NULL CONSTRAINT DF_LichSuXuatBaoCao_NgayXuat DEFAULT (GETDATE()),
        DiaChiIP NVARCHAR(45) NULL,
        VaiTro NVARCHAR(20) NULL,
        KhoaPhongId INT NULL,
        CONSTRAINT FK_LichSuXuatBaoCao_TaiKhoan FOREIGN KEY (NguoiDungId) REFERENCES dbo.TaiKhoan(TaiKhoanId),
        CONSTRAINT FK_LichSuXuatBaoCao_KhoaPhong FOREIGN KEY (KhoaPhongId) REFERENCES dbo.KhoaPhong(KhoaPhongId),
        CONSTRAINT CK_LichSuXuatBaoCao_SoDongDuLieu CHECK (SoDongDuLieu >= 0)
    );
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_LichSuXuatBaoCao_NgayXuat'
      AND object_id = OBJECT_ID('dbo.LichSuXuatBaoCao')
)
BEGIN
    CREATE INDEX IX_LichSuXuatBaoCao_NgayXuat ON dbo.LichSuXuatBaoCao(NgayXuat DESC);
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_LichSuXuatBaoCao_NguoiDung'
      AND object_id = OBJECT_ID('dbo.LichSuXuatBaoCao')
)
BEGIN
    CREATE INDEX IX_LichSuXuatBaoCao_NguoiDung ON dbo.LichSuXuatBaoCao(NguoiDungId, NgayXuat DESC);
END
