-- Add IsHidden column to BaoCao table for soft delete
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BaoCao') AND name = 'IsHidden')
BEGIN
    ALTER TABLE dbo.BaoCao ADD IsHidden BIT NOT NULL DEFAULT 0;
END
