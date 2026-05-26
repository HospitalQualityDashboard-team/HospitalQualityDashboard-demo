-- SQL Migration: 005_AddApprovalAndRejection.sql
-- Description: Drop old report status check constraint, add YKienPhanHoi column, and add updated check constraint.

PRINT 'Executing SQL Migration: 005_AddApprovalAndRejection.sql';

-- 1. Drop old constraint if exists
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CK_BaoCao_TrangThai]') AND type = 'C')
BEGIN
    ALTER TABLE dbo.BaoCao DROP CONSTRAINT CK_BaoCao_TrangThai;
    PRINT 'Dropped old constraint CK_BaoCao_TrangThai';
END

-- 2. Add YKienPhanHoi column to dbo.BaoCao if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BaoCao]') AND name = 'YKienPhanHoi')
BEGIN
    ALTER TABLE dbo.BaoCao ADD YKienPhanHoi NVARCHAR(MAX) NULL;
    PRINT 'Added column YKienPhanHoi to dbo.BaoCao';
END

-- 3. Add updated check constraint CK_BaoCao_TrangThai accepting values 1 to 6
-- 1: Nhap, 2: DaGui, 3: QuaHan, 4: DaKhoa, 5: DaDuyet, 6: TraLai
ALTER TABLE dbo.BaoCao ADD CONSTRAINT CK_BaoCao_TrangThai CHECK (TrangThai IN (1, 2, 3, 4, 5, 6));
PRINT 'Created updated constraint CK_BaoCao_TrangThai';
