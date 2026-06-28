-- Mục đích: theo dõi lịch sử triển khai/ngừng triển khai chỉ số theo tần suất báo cáo.
IF OBJECT_ID('dbo.LichSuTrienKhaiChiSo', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LichSuTrienKhaiChiSo (
        LichSuTrienKhaiChiSoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LichSuTrienKhaiChiSo PRIMARY KEY,
        ChiSoChatLuongId INT NOT NULL,
        TanSuatBaoCao TINYINT NOT NULL,
        TuNgayApDung DATE NOT NULL,
        DenNgayApDung DATE NULL,
        NguoiTaoId INT NULL,
        NgayTao DATETIME NOT NULL CONSTRAINT DF_LichSuTrienKhaiChiSo_NgayTao DEFAULT (GETDATE()),
        NguoiKetThucId INT NULL,
        NgayKetThuc DATETIME NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_LichSuTrienKhaiChiSo_ChiSo')
BEGIN
    ALTER TABLE dbo.LichSuTrienKhaiChiSo WITH CHECK
    ADD CONSTRAINT FK_LichSuTrienKhaiChiSo_ChiSo
        FOREIGN KEY (ChiSoChatLuongId) REFERENCES dbo.ChiSoChatLuong(ChiSoChatLuongId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_LichSuTrienKhaiChiSo_NguoiTao')
BEGIN
    ALTER TABLE dbo.LichSuTrienKhaiChiSo WITH CHECK
    ADD CONSTRAINT FK_LichSuTrienKhaiChiSo_NguoiTao
        FOREIGN KEY (NguoiTaoId) REFERENCES dbo.TaiKhoan(TaiKhoanId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_LichSuTrienKhaiChiSo_NguoiKetThuc')
BEGIN
    ALTER TABLE dbo.LichSuTrienKhaiChiSo WITH CHECK
    ADD CONSTRAINT FK_LichSuTrienKhaiChiSo_NguoiKetThuc
        FOREIGN KEY (NguoiKetThucId) REFERENCES dbo.TaiKhoan(TaiKhoanId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_LichSuTrienKhaiChiSo_TanSuat')
BEGIN
    ALTER TABLE dbo.LichSuTrienKhaiChiSo WITH CHECK
    ADD CONSTRAINT CK_LichSuTrienKhaiChiSo_TanSuat
        CHECK (TanSuatBaoCao IN (1, 2, 3, 4, 5, 6, 7, 8, 9));
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_LichSuTrienKhaiChiSo_DateRange')
BEGIN
    ALTER TABLE dbo.LichSuTrienKhaiChiSo WITH CHECK
    ADD CONSTRAINT CK_LichSuTrienKhaiChiSo_DateRange
        CHECK (DenNgayApDung IS NULL OR TuNgayApDung <= DenNgayApDung);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_LichSuTrienKhaiChiSo_Open'
      AND object_id = OBJECT_ID(N'dbo.LichSuTrienKhaiChiSo')
)
BEGIN
    CREATE UNIQUE INDEX IX_LichSuTrienKhaiChiSo_Open
    ON dbo.LichSuTrienKhaiChiSo(ChiSoChatLuongId, TanSuatBaoCao)
    WHERE DenNgayApDung IS NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_LichSuTrienKhaiChiSo_PeriodLookup'
      AND object_id = OBJECT_ID(N'dbo.LichSuTrienKhaiChiSo')
)
BEGIN
    CREATE INDEX IX_LichSuTrienKhaiChiSo_PeriodLookup
    ON dbo.LichSuTrienKhaiChiSo(ChiSoChatLuongId, TanSuatBaoCao, TuNgayApDung, DenNgayApDung);
END
GO

INSERT INTO dbo.LichSuTrienKhaiChiSo(ChiSoChatLuongId, TanSuatBaoCao, TuNgayApDung, DenNgayApDung)
SELECT cs.ChiSoChatLuongId, ts.TanSuatBaoCao, CONVERT(date, '19000101'), NULL
FROM dbo.ChiSoChatLuong cs
INNER JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = cs.ChiSoChatLuongId
WHERE cs.DangHoatDong = 1
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.LichSuTrienKhaiChiSo ls
      WHERE ls.ChiSoChatLuongId = cs.ChiSoChatLuongId
        AND ls.TanSuatBaoCao = ts.TanSuatBaoCao
        AND ls.DenNgayApDung IS NULL
  );
GO

IF OBJECT_ID('dbo.fn_ChiSoDuocTrienKhaiTrongKy', 'FN') IS NOT NULL
BEGIN
    DROP FUNCTION dbo.fn_ChiSoDuocTrienKhaiTrongKy;
END
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
