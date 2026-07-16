-- Thêm 2 cột để lưu trữ Persistent Token hỗ trợ tính năng Ghi nhớ đăng nhập
IF COL_LENGTH('dbo.TaiKhoan', 'RememberTokenHash') IS NULL
BEGIN
    ALTER TABLE dbo.TaiKhoan 
    ADD RememberTokenHash NVARCHAR(128) NULL,
        RememberTokenExpiry DATETIME NULL;
END
GO
