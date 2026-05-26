IF OBJECT_ID('dbo.ThongBaoTuDongLog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ThongBaoTuDongLog (
        ThongBaoTuDongLogId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ThongBaoTuDongLog PRIMARY KEY,
        DedupKey NVARCHAR(255) NOT NULL,
        LoaiThongBao TINYINT NOT NULL,
        KyBaoCaoId INT NULL,
        KhoaPhongId INT NULL,
        NgayMoc DATE NULL,
        NgayTao DATETIME NOT NULL CONSTRAINT DF_ThongBaoTuDongLog_NgayTao DEFAULT (GETDATE()),
        CONSTRAINT UQ_ThongBaoTuDongLog_DedupKey UNIQUE (DedupKey),
        CONSTRAINT FK_ThongBaoTuDongLog_KyBaoCao FOREIGN KEY (KyBaoCaoId) REFERENCES dbo.KyBaoCao(KyBaoCaoId),
        CONSTRAINT FK_ThongBaoTuDongLog_KhoaPhong FOREIGN KEY (KhoaPhongId) REFERENCES dbo.KhoaPhong(KhoaPhongId)
    );
END
GO
