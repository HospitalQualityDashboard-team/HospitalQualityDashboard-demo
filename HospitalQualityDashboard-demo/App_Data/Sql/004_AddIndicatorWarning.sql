IF COL_LENGTH('dbo.ThongBao', 'ChiSoChatLuongId') IS NULL
BEGIN
    ALTER TABLE dbo.ThongBao ADD ChiSoChatLuongId INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ThongBao_ChiSoChatLuong')
BEGIN
    ALTER TABLE dbo.ThongBao WITH CHECK
    ADD CONSTRAINT FK_ThongBao_ChiSoChatLuong
        FOREIGN KEY (ChiSoChatLuongId) REFERENCES dbo.ChiSoChatLuong(ChiSoChatLuongId);
END
GO

IF COL_LENGTH('dbo.ThongBaoTuDongLog', 'ChiSoChatLuongId') IS NULL
BEGIN
    ALTER TABLE dbo.ThongBaoTuDongLog ADD ChiSoChatLuongId INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ThongBaoTuDongLog_ChiSoChatLuong')
BEGIN
    ALTER TABLE dbo.ThongBaoTuDongLog WITH CHECK
    ADD CONSTRAINT FK_ThongBaoTuDongLog_ChiSoChatLuong
        FOREIGN KEY (ChiSoChatLuongId) REFERENCES dbo.ChiSoChatLuong(ChiSoChatLuongId);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ThongBaoTuDongLog_IndicatorMarker'
      AND object_id = OBJECT_ID(N'dbo.ThongBaoTuDongLog')
)
BEGIN
    CREATE INDEX IX_ThongBaoTuDongLog_IndicatorMarker
    ON dbo.ThongBaoTuDongLog(KyBaoCaoId, KhoaPhongId, ChiSoChatLuongId, NgayMoc, LoaiThongBao);
END
GO
