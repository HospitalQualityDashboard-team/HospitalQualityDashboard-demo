-- Mục đích: bổ sung liên kết chỉ số cho cảnh báo tự động và chống gửi trùng thông báo.
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

IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_ThongBao_LoaiThongBao'
      AND parent_object_id = OBJECT_ID(N'dbo.ThongBao')
)
BEGIN
    ALTER TABLE dbo.ThongBao DROP CONSTRAINT CK_ThongBao_LoaiThongBao;
END
GO

ALTER TABLE dbo.ThongBao WITH CHECK
ADD CONSTRAINT CK_ThongBao_LoaiThongBao
CHECK (LoaiThongBao IN (1, 2, 3, 4, 5, 6, 7));
GO

IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_ThongBaoTuDongLog_LoaiThongBao'
      AND parent_object_id = OBJECT_ID(N'dbo.ThongBaoTuDongLog')
)
BEGIN
    ALTER TABLE dbo.ThongBaoTuDongLog DROP CONSTRAINT CK_ThongBaoTuDongLog_LoaiThongBao;
END
GO

ALTER TABLE dbo.ThongBaoTuDongLog WITH CHECK
ADD CONSTRAINT CK_ThongBaoTuDongLog_LoaiThongBao
CHECK (LoaiThongBao IN (1, 2, 3, 4, 5, 6, 7));
GO
