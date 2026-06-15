IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BaoCao_PeriodDeptIndicatorStatus' AND object_id = OBJECT_ID(N'dbo.BaoCao'))
BEGIN
    CREATE INDEX IX_BaoCao_PeriodDeptIndicatorStatus
    ON dbo.BaoCao(KyBaoCaoId, KhoaPhongId, ChiSoChatLuongId, TrangThai);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PhanCongChiSo_DepartmentActiveIndicator' AND object_id = OBJECT_ID(N'dbo.PhanCongChiSo'))
BEGIN
    CREATE INDEX IX_PhanCongChiSo_DepartmentActiveIndicator
    ON dbo.PhanCongChiSo(KhoaPhongId, DangHoatDong, ChiSoChatLuongId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ChiSoTanSuatBaoCao_IndicatorFrequency' AND object_id = OBJECT_ID(N'dbo.ChiSoTanSuatBaoCao'))
BEGIN
    CREATE INDEX IX_ChiSoTanSuatBaoCao_IndicatorFrequency
    ON dbo.ChiSoTanSuatBaoCao(ChiSoChatLuongId, TanSuatBaoCao);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ThongBaoNguoiNhan_AccountRead' AND object_id = OBJECT_ID(N'dbo.ThongBaoNguoiNhan'))
BEGIN
    CREATE INDEX IX_ThongBaoNguoiNhan_AccountRead
    ON dbo.ThongBaoNguoiNhan(TaiKhoanId, DaDoc);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_NhanVien_DepartmentName' AND object_id = OBJECT_ID(N'dbo.NhanVien'))
BEGIN
    CREATE INDEX IX_NhanVien_DepartmentName
    ON dbo.NhanVien(KhoaPhongId, HoTen);
END
GO
