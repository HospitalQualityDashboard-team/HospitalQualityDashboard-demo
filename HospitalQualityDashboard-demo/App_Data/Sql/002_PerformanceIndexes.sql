-- Mục đích: bổ sung index hiệu năng cho các truy vấn dashboard, báo cáo và phân công chỉ số.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BaoCao_PeriodDeptIndicatorStatus' AND object_id = OBJECT_ID(N'dbo.BaoCao'))
BEGIN
    CREATE INDEX IX_BaoCao_PeriodDeptIndicatorStatus
    ON dbo.BaoCao(KyBaoCaoId, KhoaPhongId, ChiSoChatLuongId, TrangThai);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_NhanVien_DepartmentName' AND object_id = OBJECT_ID(N'dbo.NhanVien'))
BEGIN
    CREATE INDEX IX_NhanVien_DepartmentName
    ON dbo.NhanVien(KhoaPhongId, HoTen);
END
GO
