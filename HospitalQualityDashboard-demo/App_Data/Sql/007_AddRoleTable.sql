-- Mục đích: tạo bảng Role và chuyển đổi bảng TaiKhoan sang mô hình RBAC.
-- Bước 1: Tạo bảng Role
-- Bước 2: Seed 3 roles mặc định (Admin, User, BoardOfDirectors)
-- Bước 3: Thêm cột RoleId vào TaiKhoan, migrate dữ liệu từ LoaiTaiKhoan
-- Bước 4: Xóa cột LoaiTaiKhoan và các constraint cũ

SET NOCOUNT ON;

-- Bước 1: Tạo bảng Role nếu chưa tồn tại
IF OBJECT_ID(N'dbo.Role', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Role (
        RoleId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Role PRIMARY KEY,
        RoleName NVARCHAR(50) NOT NULL,
        Description NVARCHAR(255) NULL,
        CONSTRAINT UQ_Role_RoleName UNIQUE (RoleName)
    );
END
GO

-- Bước 2: Seed 3 roles mặc định
IF NOT EXISTS (SELECT 1 FROM dbo.Role WHERE RoleName = N'Admin')
BEGIN
    INSERT INTO dbo.Role (RoleName, Description) VALUES (N'Admin', N'Quản trị viên hệ thống');
END

IF NOT EXISTS (SELECT 1 FROM dbo.Role WHERE RoleName = N'User')
BEGIN
    INSERT INTO dbo.Role (RoleName, Description) VALUES (N'User', N'Nhân sự khoa/phòng nhập liệu');
END

IF NOT EXISTS (SELECT 1 FROM dbo.Role WHERE RoleName = N'BoardOfDirectors')
BEGIN
    INSERT INTO dbo.Role (RoleName, Description) VALUES (N'BoardOfDirectors', N'Ban Giám Đốc - Xem dashboard và phê duyệt');
END
GO

-- Bước 3: Thêm cột RoleId vào TaiKhoan nếu chưa có
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaiKhoan') AND name = N'RoleId')
BEGIN
    ALTER TABLE dbo.TaiKhoan ADD RoleId INT NULL;
END
GO

-- Bước 4: Migrate dữ liệu từ LoaiTaiKhoan sang RoleId
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaiKhoan') AND name = N'LoaiTaiKhoan')
BEGIN
    EXEC sp_executesql N'
        UPDATE tk
        SET tk.RoleId = r.RoleId
        FROM dbo.TaiKhoan tk
        INNER JOIN dbo.Role r ON (
            (tk.LoaiTaiKhoan = 1 AND r.RoleName = N''Admin'')
            OR (tk.LoaiTaiKhoan = 2 AND r.RoleName = N''User'')
        )
        WHERE tk.RoleId IS NULL;
    ';
END
GO

-- Bước 5: Đặt RoleId NOT NULL sau khi migrate xong
ALTER TABLE dbo.TaiKhoan ALTER COLUMN RoleId INT NOT NULL;
GO

-- Bước 6: Thêm khóa ngoại
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TaiKhoan_Role')
BEGIN
    ALTER TABLE dbo.TaiKhoan ADD CONSTRAINT FK_TaiKhoan_Role FOREIGN KEY (RoleId) REFERENCES dbo.Role(RoleId);
END
GO

-- Bước 7: Xóa constraint cũ liên quan đến LoaiTaiKhoan
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_TaiKhoan_LoaiTaiKhoan')
BEGIN
    ALTER TABLE dbo.TaiKhoan DROP CONSTRAINT CK_TaiKhoan_LoaiTaiKhoan;
END

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_TaiKhoan_UserHasKhoaPhong')
BEGIN
    ALTER TABLE dbo.TaiKhoan DROP CONSTRAINT CK_TaiKhoan_UserHasKhoaPhong;
END
GO

-- Bước 8: Xóa index cũ phụ thuộc vào LoaiTaiKhoan nếu tồn tại
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TaiKhoan_Loai_Active' AND object_id = OBJECT_ID(N'dbo.TaiKhoan'))
BEGIN
    DROP INDEX IX_TaiKhoan_Loai_Active ON dbo.TaiKhoan;
END
GO

-- Bước 9: Xóa cột LoaiTaiKhoan
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaiKhoan') AND name = N'LoaiTaiKhoan')
BEGIN
    ALTER TABLE dbo.TaiKhoan DROP COLUMN LoaiTaiKhoan;
END
GO

-- Bước 10: Tạo index mới thay thế dựa trên RoleId
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TaiKhoan_Role_Active' AND object_id = OBJECT_ID(N'dbo.TaiKhoan'))
BEGIN
    CREATE INDEX IX_TaiKhoan_Role_Active ON dbo.TaiKhoan(RoleId, DangHoatDong, KhoaPhongId);
END
GO
