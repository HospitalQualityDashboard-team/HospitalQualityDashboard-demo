IF NOT EXISTS (SELECT 1 FROM dbo.TaiKhoan WHERE TenDangNhap = N'admin')
BEGIN
    INSERT INTO dbo.TaiKhoan(TenDangNhap, MatKhauHash, LoaiTaiKhoan, DangHoatDong)
    VALUES(
        N'admin',
        N'10000:AQIDBAUGBwgJCgsMDQ4PEA==:rJPsxUC5qMZAY/awUbhVdQPeOX+4Z12BD7E/F7/y5pA=',
        1,
        1
    );
END;
