IF OBJECT_ID(N'dbo.ChiSoTanSuatBaoCao', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChiSoTanSuatBaoCao (
        ChiSoTanSuatBaoCaoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChiSoTanSuatBaoCao PRIMARY KEY,
        ChiSoChatLuongId INT NOT NULL,
        TanSuatBaoCao TINYINT NOT NULL,
        CONSTRAINT FK_ChiSoTanSuatBaoCao_ChiSo FOREIGN KEY (ChiSoChatLuongId) REFERENCES dbo.ChiSoChatLuong(ChiSoChatLuongId),
        CONSTRAINT UQ_ChiSoTanSuatBaoCao UNIQUE (ChiSoChatLuongId, TanSuatBaoCao)
    );

    CREATE INDEX IX_ChiSoTanSuatBaoCao_ChiSoChatLuongId ON dbo.ChiSoTanSuatBaoCao(ChiSoChatLuongId);

    INSERT INTO dbo.ChiSoTanSuatBaoCao(ChiSoChatLuongId, TanSuatBaoCao)
    SELECT ChiSoChatLuongId, TanSuatBaoCao
    FROM dbo.ChiSoChatLuong;
END;
