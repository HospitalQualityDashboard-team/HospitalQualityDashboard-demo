-- ============================================================
-- PHẦN MỀM QUẢN LÝ BỘ CHỈ SỐ CHẤT LƯỢNG BỆNH VIỆN
-- Version: 2.0 - Revised Schema
-- Người chỉnh sửa: Mentor IT
-- Ngày: 2024
-- ============================================================
-- CHANGELOG so với schema cũ (001_CreateSchema.sql):
--
-- [SỬA] TaiKhoan.LoaiTaiKhoan: mở rộng từ (1,2) → (1,2,3)
--       1=Admin, 2=KhoaPhong, 3=BanGiamDoc (xem báo cáo tổng hợp)
--
-- [SỬA] ChiSoChatLuong: bỏ cột TanSuatBaoCao (đã có bảng ChiSoTanSuatBaoCao riêng → trùng lặp)
--       Thêm NhomChiSo (phân nhóm theo đơn vị thu thập: Tài chính, KSNK,...)
--       Thêm KhoaPhongThuThapId (FK đến KhoaPhong: đơn vị chịu trách nhiệm thu thập)
--
-- [SỬA] ChiSoTanSuatBaoCao: đổi tên thành DanhMucTanSuat (lookup table) để tường minh
--       Thêm bảng ChiSoLichBaoCao thay thế, rõ ràng hơn
--
-- [THÊM] NhomChiSo: bảng danh mục nhóm chỉ số (Tài chính, Điều dưỡng, KSNK, v.v.)
--
-- [THÊM] DanhMucLookup: bảng danh mục tập trung cho các TINYINT code
--        (LoaiTaiKhoan, LoaiCongThuc, TrangThaiBaoCao, LoaiThongBao...)
--        → Giúp Frontend/Code không hardcode magic number
--
-- [SỬA] PhanCongChiSo: bỏ UNIQUE (ChiSoChatLuongId, KhoaPhongId) cứng
--       vì thực tế 1 chỉ số có thể được phân công cho nhiều khoa khác nhau
--       và có thể phân công lại theo từng năm/kỳ.
--       Thêm Nam để phân biệt phân công theo năm.
--
-- [SỬA] BaoCaoChiTiet: thêm NguoiNhapId, NgayNhap để audit trail
--       Thêm SoLieu gốc (TuSoNhap, MauSoNhap) vs tính toán (TuSo, MauSo, KetQua)
--
-- [THÊM] LichSuThayDoiChiSo: audit log khi chỉ số bị sửa (tên, định nghĩa...)
--
-- [SỬA] KyBaoCao: thêm NguoiTaoId (ai tạo kỳ), SoNhacNho (đã nhắc bao nhiêu lần)
--
-- [SỬA] Đặt lại toàn bộ CHECK constraint cho TanSuatBaoCao thành tham chiếu DanhMucLookup
--       (không hardcode IN (1,2,3,...) vì khó maintain)
--
-- [GIỮ NGUYÊN] NhanVien, KhoaPhong, ThongBao, ThongBaoNguoiNhan,
--              ThongBaoTuDongLog, LichSuImport, NhatKyHeThong
-- ============================================================

SET NOCOUNT ON;
GO

-- ============================================================
-- 0. BẢNG DANH MỤC TẬP TRUNG (Lookup / Reference Data)
--    Tránh hardcode magic number trong code và constraint
-- ============================================================

-- Bảng danh mục chung: lưu tất cả enum/code của hệ thống
-- Ví dụ: NhomMa='LoaiTaiKhoan', Ma=1, TenHienThi='Quản trị viên'
CREATE TABLE dbo.DanhMucLookup (
    DanhMucLookupId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DanhMucLookup PRIMARY KEY,
    NhomMa          NVARCHAR(100) NOT NULL,   -- 'LoaiTaiKhoan','LoaiCongThuc','TrangThaiBaoCao',...
    Ma              TINYINT       NOT NULL,
    TenHienThi      NVARCHAR(255) NOT NULL,
    MoTa            NVARCHAR(500) NULL,
    ThuTu           INT           NOT NULL DEFAULT (0),
    DangHoatDong    BIT           NOT NULL DEFAULT (1),
    CONSTRAINT UQ_DanhMucLookup_Nhom_Ma UNIQUE (NhomMa, Ma)
);
GO

-- ============================================================
-- Seed dữ liệu danh mục
-- ============================================================

-- LoaiTaiKhoan: 1=Admin, 2=KhoaPhong (nhập liệu), 3=BanGiamDoc (xem tổng hợp)
INSERT INTO dbo.DanhMucLookup(NhomMa, Ma, TenHienThi, MoTa, ThuTu) VALUES
(N'LoaiTaiKhoan', 1, N'Quản trị viên',    N'Toàn quyền hệ thống',                          1),
(N'LoaiTaiKhoan', 2, N'Khoa/Phòng',       N'Nhập số liệu báo cáo cho khoa/phòng mình',     2),
(N'LoaiTaiKhoan', 3, N'Ban Giám đốc',     N'Xem báo cáo tổng hợp, không nhập liệu',        3);

-- LoaiCongThuc: cách tính kết quả của chỉ số
-- 1=TiLe(TuSo/MauSo*100%), 2=TySo(TuSo/MauSo), 3=SoLuong(nhập trực tiếp),
-- 4=ThoiGian(phút/giờ), 5=VanBan(text), 6=CongThucKhac
INSERT INTO dbo.DanhMucLookup(NhomMa, Ma, TenHienThi, MoTa, ThuTu) VALUES
(N'LoaiCongThuc', 1, N'Tỷ lệ (%)',        N'Kết quả = Tử số / Mẫu số × 100',              1),
(N'LoaiCongThuc', 2, N'Tỷ số',            N'Kết quả = Tử số / Mẫu số',                    2),
(N'LoaiCongThuc', 3, N'Số lượng',         N'Nhập trực tiếp một con số',                    3),
(N'LoaiCongThuc', 4, N'Thời gian',        N'Đo bằng phút hoặc giờ',                        4),
(N'LoaiCongThuc', 5, N'Văn bản / Mô tả', N'Kết quả là mô tả định tính',                   5),
(N'LoaiCongThuc', 6, N'Công thức khác',   N'Công thức tính đặc biệt',                      6);

-- TanSuatBaoCao: tần suất báo cáo
-- 1=Ngày, 2=Tuần, 3=Tháng, 4=Quý, 5=6 tháng, 6=Năm, 7=TrướcSauThựcHiện, 8=KhiCóSựKiện
INSERT INTO dbo.DanhMucLookup(NhomMa, Ma, TenHienThi, MoTa, ThuTu) VALUES
(N'TanSuatBaoCao', 1, N'Hằng ngày',             NULL, 1),
(N'TanSuatBaoCao', 2, N'Hằng tuần',             NULL, 2),
(N'TanSuatBaoCao', 3, N'Hằng tháng',            NULL, 3),
(N'TanSuatBaoCao', 4, N'Hằng quý (3 tháng)',    NULL, 4),
(N'TanSuatBaoCao', 5, N'6 tháng',               NULL, 5),
(N'TanSuatBaoCao', 6, N'Hằng năm (12 tháng)',   NULL, 6),
(N'TanSuatBaoCao', 7, N'Trước và sau thực hiện',NULL, 7),
(N'TanSuatBaoCao', 8, N'Khi có sự kiện',        NULL, 8);

-- TrangThaiBaoCao: vòng đời của một bản báo cáo
-- 1=ChưaNhập, 2=NháP, 3=ĐãGửi, 4=ĐãDuyệt, 5=TừChối, 6=YêuCầuChỉnhSửa
INSERT INTO dbo.DanhMucLookup(NhomMa, Ma, TenHienThi, MoTa, ThuTu) VALUES
(N'TrangThaiBaoCao', 1, N'Chưa nhập',             N'Khoa chưa nhập số liệu',              1),
(N'TrangThaiBaoCao', 2, N'Bản nháp',              N'Đang nhập, chưa gửi',                 2),
(N'TrangThaiBaoCao', 3, N'Đã gửi',                N'Khoa đã nộp, chờ duyệt',              3),
(N'TrangThaiBaoCao', 4, N'Đã duyệt',              N'Admin/BGĐ đã xác nhận',               4),
(N'TrangThaiBaoCao', 5, N'Từ chối',               N'Không hợp lệ, cần nhập lại',          5),
(N'TrangThaiBaoCao', 6, N'Yêu cầu chỉnh sửa',    N'Có ý kiến, cần bổ sung',              6);

-- TrangThaiKyBaoCao
-- 1=Mở, 2=ĐóngNhập, 3=ĐãKhóa
INSERT INTO dbo.DanhMucLookup(NhomMa, Ma, TenHienThi, MoTa, ThuTu) VALUES
(N'TrangThaiKyBaoCao', 1, N'Đang mở',      N'Các khoa đang nhập liệu',        1),
(N'TrangThaiKyBaoCao', 2, N'Đóng nhập',    N'Đã hết hạn nộp',                 2),
(N'TrangThaiKyBaoCao', 3, N'Đã khóa',      N'Đã tổng hợp xong, khóa vĩnh viễn', 3);

-- LoaiThongBao
-- 1=NhắcHạnNộp, 2=BaoCaoMới, 3=DuyệtBaoCao, 4=TừChối, 5=YêuCầuChỉnhSửa, 6=HệThống
INSERT INTO dbo.DanhMucLookup(NhomMa, Ma, TenHienThi, MoTa, ThuTu) VALUES
(N'LoaiThongBao', 1, N'Nhắc hạn nộp',        NULL, 1),
(N'LoaiThongBao', 2, N'Báo cáo mới',         NULL, 2),
(N'LoaiThongBao', 3, N'Duyệt báo cáo',       NULL, 3),
(N'LoaiThongBao', 4, N'Từ chối báo cáo',     NULL, 4),
(N'LoaiThongBao', 5, N'Yêu cầu chỉnh sửa',  NULL, 5),
(N'LoaiThongBao', 6, N'Hệ thống',            NULL, 6);

-- LoaiImport
-- 1=ChiSo, 2=KhoaPhong, 3=NhanVien
INSERT INTO dbo.DanhMucLookup(NhomMa, Ma, TenHienThi, MoTa, ThuTu) VALUES
(N'LoaiImport', 1, N'Import chỉ số',     NULL, 1),
(N'LoaiImport', 2, N'Import khoa/phòng', NULL, 2),
(N'LoaiImport', 3, N'Import nhân viên',  NULL, 3);

GO

-- ============================================================
-- 1. KHOA PHÒNG
-- ============================================================
CREATE TABLE dbo.KhoaPhong (
    KhoaPhongId         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_KhoaPhong PRIMARY KEY,
    IdKhoaPhongNguon    INT           NOT NULL,          -- ID từ HIS/hệ thống nguồn
    MaKhoaPhong         NVARCHAR(50)  NULL,              -- [THÊM] Mã rút gọn (vd: "TC", "DD", "KSNK")
    TenKhoaPhong        NVARCHAR(255) NOT NULL,
    LoaiKhoaPhong       NVARCHAR(100) NULL,              -- [THÊM] 'Lâm sàng','Cận lâm sàng','Hành chính'
    DangHoatDong        BIT           NOT NULL CONSTRAINT DF_KhoaPhong_DangHoatDong DEFAULT (1),
    -- Đổi tên Used → DangHoatDong để nhất quán với các bảng khác
    GhiChu              NVARCHAR(500) NULL,
    NgayTao             DATETIME      NOT NULL CONSTRAINT DF_KhoaPhong_NgayTao DEFAULT (GETDATE()),
    NgayCapNhat         DATETIME      NULL,
    CONSTRAINT UQ_KhoaPhong_IdKhoaPhongNguon UNIQUE (IdKhoaPhongNguon)
);
GO

-- ============================================================
-- 2. NHÂN VIÊN
-- ============================================================
CREATE TABLE dbo.NhanVien (
    NhanVienId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NhanVien PRIMARY KEY,
    MaNhanVien      NVARCHAR(50)  NOT NULL,
    HoTen           NVARCHAR(255) NOT NULL,
    NgaySinh        DATE          NULL,
    GioiTinh        NVARCHAR(20)  NULL,
    ChucVu          NVARCHAR(255) NULL,
    Email           NVARCHAR(255) NULL,
    SoDienThoai     NVARCHAR(50)  NULL,
    KhoaPhongId     INT           NOT NULL,
    DangHoatDong    BIT           NOT NULL CONSTRAINT DF_NhanVien_DangHoatDong DEFAULT (1),
    NgayTao         DATETIME      NOT NULL CONSTRAINT DF_NhanVien_NgayTao DEFAULT (GETDATE()),
    NgayCapNhat     DATETIME      NULL,
    CONSTRAINT UQ_NhanVien_MaNhanVien UNIQUE (MaNhanVien),
    CONSTRAINT FK_NhanVien_KhoaPhong FOREIGN KEY (KhoaPhongId) REFERENCES dbo.KhoaPhong(KhoaPhongId)
);
GO

-- ============================================================
-- 3. TÀI KHOẢN
-- ============================================================
CREATE TABLE dbo.TaiKhoan (
    TaiKhoanId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TaiKhoan PRIMARY KEY,
    TenDangNhap     NVARCHAR(100) NOT NULL,
    MatKhauHash     NVARCHAR(500) NOT NULL,
    -- LoaiTaiKhoan: 1=Admin, 2=KhoaPhong, 3=BanGiamDoc
    -- [SỬA] Mở rộng từ (1,2) → (1,2,3) để hỗ trợ role BGĐ xem dashboard tổng hợp
    LoaiTaiKhoan    TINYINT       NOT NULL,
    NhanVienId      INT           NULL,
    KhoaPhongId     INT           NULL,
    DangHoatDong    BIT           NOT NULL CONSTRAINT DF_TaiKhoan_DangHoatDong DEFAULT (1),
    LanDangNhapCuoi DATETIME      NULL,
    NgayTao         DATETIME      NOT NULL CONSTRAINT DF_TaiKhoan_NgayTao DEFAULT (GETDATE()),
    NgayCapNhat     DATETIME      NULL,
    CONSTRAINT UQ_TaiKhoan_TenDangNhap UNIQUE (TenDangNhap),
    CONSTRAINT FK_TaiKhoan_NhanVien   FOREIGN KEY (NhanVienId)  REFERENCES dbo.NhanVien(NhanVienId),
    CONSTRAINT FK_TaiKhoan_KhoaPhong  FOREIGN KEY (KhoaPhongId) REFERENCES dbo.KhoaPhong(KhoaPhongId),
    -- [SỬA] Thêm role 3 (BGĐ) vào CHECK
    CONSTRAINT CK_TaiKhoan_LoaiTaiKhoan CHECK (LoaiTaiKhoan IN (1, 2, 3)),
    -- TaiKhoan loại 2 (KhoaPhong) bắt buộc phải có KhoaPhongId
    CONSTRAINT CK_TaiKhoan_UserHasKhoaPhong CHECK (LoaiTaiKhoan <> 2 OR KhoaPhongId IS NOT NULL)
);
GO

-- ============================================================
-- 4. NHÓM CHỈ SỐ
--    [THÊM MỚI] Phân nhóm theo đơn vị thu thập (từ file tài liệu):
--    Phòng Tài chính, Phòng Điều dưỡng, Khoa KSNK, Phòng Kế hoạch...
-- ============================================================
CREATE TABLE dbo.NhomChiSo (
    NhomChiSoId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NhomChiSo PRIMARY KEY,
    MaNhom          NVARCHAR(50)  NOT NULL,
    TenNhom         NVARCHAR(255) NOT NULL,
    MoTa            NVARCHAR(500) NULL,
    ThuTu           INT           NOT NULL DEFAULT (0),
    DangHoatDong    BIT           NOT NULL DEFAULT (1),
    CONSTRAINT UQ_NhomChiSo_MaNhom UNIQUE (MaNhom)
);

-- Seed nhóm chỉ số theo tài liệu
INSERT INTO dbo.NhomChiSo(MaNhom, TenNhom, ThuTu) VALUES
(N'TC',    N'Phòng Tài chính kế toán',         1),
(N'VTBT',  N'Phòng Vật tư thiết bị y tế',      2),
(N'CDT',   N'Phòng Chỉ đạo tuyến',             3),
(N'CNTT',  N'Phòng Công nghệ thông tin',        4),
(N'DINHDG',N'Khoa Dinh dưỡng',                 5),
(N'DUOC',  N'Khoa Dược',                        6),
(N'CTXH',  N'Phòng Công tác xã hội',           7),
(N'KH',    N'Phòng Kế hoạch tổng hợp',         8),
(N'CAPCUU',N'Khoa Cấp cứu',                    9),
(N'DD',    N'Phòng Điều dưỡng',               10),
(N'HC',    N'Phòng Hành chính quản trị',       11),
(N'KSNK',  N'Khoa Kiểm soát nhiễm khuẩn',     12),
(N'TCCB',  N'Phòng Tổ chức cán bộ',           13),
(N'BV',    N'Toàn bệnh viện (chung)',          14);
GO

-- ============================================================
-- 5. CHỈ SỐ CHẤT LƯỢNG
-- ============================================================
CREATE TABLE dbo.ChiSoChatLuong (
    ChiSoChatLuongId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChiSoChatLuong PRIMARY KEY,
    MaChiSo             NVARCHAR(50)   NOT NULL,      -- Mã định danh duy nhất (vd: "CS001")
    SoThuTu             INT            NULL,           -- Số thứ tự hiển thị
    TenChiSo            NVARCHAR(1000) NOT NULL,
    DinhNghia           NVARCHAR(MAX)  NULL,
    LinhVucApDung       NVARCHAR(500)  NULL,
    KhiaCanhChatLuong   NVARCHAR(500)  NULL,           -- An toàn/Hiệu suất/Hiệu quả/Năng lực/Hướng đến NB
    ThanhToChatLuong    NVARCHAR(500)  NULL,           -- Đầu vào/Quá trình/Đầu ra
    LyDoLuaChon        NVARCHAR(MAX)  NULL,
    PhuongPhapTinh      NVARCHAR(MAX)  NULL,
    TuSoMoTa            NVARCHAR(MAX)  NULL,           -- Mô tả tử số
    MauSoMoTa           NVARCHAR(MAX)  NULL,           -- Mô tả mẫu số
    NguonSoLieu         NVARCHAR(MAX)  NULL,
    ThuThapTongHop      NVARCHAR(MAX)  NULL,           -- Mô tả đơn vị thu thập
    GiaTriSoLieu        NVARCHAR(MAX)  NULL,           -- Nhận xét về độ tin cậy
    -- [SỬA] Bỏ cột TanSuatBaoCao ở đây (chuyển sang bảng ChiSoLichBaoCao)
    --       vì 1 chỉ số có thể báo cáo ở nhiều tần suất (vd: quý VÀ năm)
    LoaiCongThuc        TINYINT        NOT NULL,        -- 1=TiLe,2=TySo,3=SoLuong,4=ThoiGian,5=VanBan,6=Khac
    DonViTinh           NVARCHAR(100)  NULL,            -- %, phút, ca, lượt...
    -- [THÊM] Liên kết nhóm chỉ số (đơn vị thu thập)
    NhomChiSoId         INT            NULL,
    -- [THÊM] Khoa/Phòng chịu trách nhiệm THU THẬP (có thể khác với khoa báo cáo)
    KhoaPhongThuThapId  INT            NULL,
    DangHoatDong        BIT            NOT NULL CONSTRAINT DF_ChiSoChatLuong_DangHoatDong DEFAULT (1),
    NgayTao             DATETIME       NOT NULL CONSTRAINT DF_ChiSoChatLuong_NgayTao DEFAULT (GETDATE()),
    NgayCapNhat         DATETIME       NULL,
    NguoiTaoId          INT            NULL,            -- [THÊM] Audit: ai tạo chỉ số
    CONSTRAINT UQ_ChiSoChatLuong_MaChiSo UNIQUE (MaChiSo),
    CONSTRAINT CK_ChiSoChatLuong_LoaiCongThuc CHECK (LoaiCongThuc IN (1, 2, 3, 4, 5, 6)),
    CONSTRAINT FK_ChiSoChatLuong_NhomChiSo
        FOREIGN KEY (NhomChiSoId) REFERENCES dbo.NhomChiSo(NhomChiSoId),
    CONSTRAINT FK_ChiSoChatLuong_KhoaPhongThuThap
        FOREIGN KEY (KhoaPhongThuThapId) REFERENCES dbo.KhoaPhong(KhoaPhongId)
);
GO

-- ============================================================
-- 6. CHỈ SỐ - LỊCH BÁO CÁO (tần suất)
--    [SỬA] Đổi tên ChiSoTanSuatBaoCao → ChiSoLichBaoCao
--    Lý do: 1 chỉ số có thể có nhiều tần suất báo cáo khác nhau
--    Ví dụ CS_06: "Mỗi quý, 6 tháng, 12 tháng" → 3 dòng
-- ============================================================
CREATE TABLE dbo.ChiSoLichBaoCao (
    ChiSoLichBaoCaoId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChiSoLichBaoCao PRIMARY KEY,
    ChiSoChatLuongId    INT     NOT NULL,
    TanSuatBaoCao       TINYINT NOT NULL,   -- FK logic đến DanhMucLookup NhomMa='TanSuatBaoCao'
    CONSTRAINT FK_ChiSoLichBaoCao_ChiSo
        FOREIGN KEY (ChiSoChatLuongId) REFERENCES dbo.ChiSoChatLuong(ChiSoChatLuongId),
    CONSTRAINT UQ_ChiSoLichBaoCao UNIQUE (ChiSoChatLuongId, TanSuatBaoCao),
    CONSTRAINT CK_ChiSoLichBaoCao_TanSuat CHECK (TanSuatBaoCao IN (1, 2, 3, 4, 5, 6, 7, 8))
);
GO

-- ============================================================
-- 7. CHỈ SỐ MỤC TIÊU (theo năm)
-- ============================================================
CREATE TABLE dbo.ChiSoMucTieu (
    ChiSoMucTieuId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChiSoMucTieu PRIMARY KEY,
    ChiSoChatLuongId    INT           NOT NULL,
    Nam                 INT           NOT NULL,
    ToanTuSoSanh        NVARCHAR(10)  NOT NULL,    -- '>', '>=', '<', '<=', '=', 'text'
    GiaTriMucTieu       DECIMAL(18,4) NULL,         -- NULL nếu mục tiêu là văn bản
    MoTaMucTieu         NVARCHAR(500) NULL,         -- Mô tả mục tiêu dạng text (vd: "≥ 100 bác sĩ/buổi")
    NguoiTaoId          INT           NULL,         -- [THÊM] Ai đặt mục tiêu
    NgayTao             DATETIME      NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT FK_ChiSoMucTieu_ChiSoChatLuong
        FOREIGN KEY (ChiSoChatLuongId) REFERENCES dbo.ChiSoChatLuong(ChiSoChatLuongId),
    CONSTRAINT UQ_ChiSoMucTieu_ChiSo_Nam UNIQUE (ChiSoChatLuongId, Nam),
    CONSTRAINT CK_ChiSoMucTieu_Nam   CHECK (Nam BETWEEN 2000 AND 2100),
    CONSTRAINT CK_ChiSoMucTieu_ToanTu CHECK (ToanTuSoSanh IN (N'>', N'>=', N'<', N'<=', N'=', N'text'))
);
GO

-- ============================================================
-- 8. LỊCH SỬ THAY ĐỔI CHỈ SỐ
--    [THÊM MỚI] Audit log khi định nghĩa chỉ số bị chỉnh sửa
-- ============================================================
CREATE TABLE dbo.LichSuThayDoiChiSo (
    LichSuThayDoiId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LichSuThayDoiChiSo PRIMARY KEY,
    ChiSoChatLuongId    INT          NOT NULL,
    TruongThayDoi       NVARCHAR(100) NOT NULL,    -- Tên cột bị thay đổi
    GiaTriCu            NVARCHAR(MAX) NULL,
    GiaTriMoi           NVARCHAR(MAX) NULL,
    NguoiThayDoiId      INT          NOT NULL,
    ThoiGianThayDoi     DATETIME     NOT NULL DEFAULT (GETDATE()),
    LyDo                NVARCHAR(500) NULL,
    CONSTRAINT FK_LichSuThayDoi_ChiSo
        FOREIGN KEY (ChiSoChatLuongId) REFERENCES dbo.ChiSoChatLuong(ChiSoChatLuongId),
    CONSTRAINT FK_LichSuThayDoi_NguoiThayDoi
        FOREIGN KEY (NguoiThayDoiId) REFERENCES dbo.TaiKhoan(TaiKhoanId)
);
GO

-- ============================================================
-- 9. PHÂN CÔNG CHỈ SỐ
--    [SỬA] Bỏ UNIQUE(ChiSoChatLuongId, KhoaPhongId) vì cần phân công theo năm
--    Thực tế: cùng 1 chỉ số, năm 2020 giao cho Phòng A, năm 2021 có thể giao lại
-- ============================================================
CREATE TABLE dbo.PhanCongChiSo (
    PhanCongChiSoId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PhanCongChiSo PRIMARY KEY,
    ChiSoChatLuongId    INT  NOT NULL,
    KhoaPhongId         INT  NOT NULL,
    Nam                 INT  NOT NULL,              -- [THÊM] Năm áp dụng phân công
    TuNgay              DATE NULL,
    DenNgay             DATE NULL,
    DangHoatDong        BIT  NOT NULL CONSTRAINT DF_PhanCongChiSo_DangHoatDong DEFAULT (1),
    NguoiTaoId          INT  NOT NULL,
    NgayTao             DATETIME NOT NULL CONSTRAINT DF_PhanCongChiSo_NgayTao DEFAULT (GETDATE()),
    GhiChu              NVARCHAR(500) NULL,         -- [THÊM] Ghi chú lý do phân công
    CONSTRAINT FK_PhanCongChiSo_ChiSo
        FOREIGN KEY (ChiSoChatLuongId) REFERENCES dbo.ChiSoChatLuong(ChiSoChatLuongId),
    CONSTRAINT FK_PhanCongChiSo_KhoaPhong
        FOREIGN KEY (KhoaPhongId) REFERENCES dbo.KhoaPhong(KhoaPhongId),
    CONSTRAINT FK_PhanCongChiSo_NguoiTao
        FOREIGN KEY (NguoiTaoId) REFERENCES dbo.TaiKhoan(TaiKhoanId),
    CONSTRAINT CK_PhanCongChiSo_DateRange
        CHECK (DenNgay IS NULL OR TuNgay IS NULL OR TuNgay <= DenNgay),
    CONSTRAINT CK_PhanCongChiSo_Nam CHECK (Nam BETWEEN 2000 AND 2100),
    -- [SỬA] UNIQUE theo (ChiSo, KhoaPhong, Nam) thay vì chỉ (ChiSo, KhoaPhong)
    CONSTRAINT UQ_PhanCongChiSo_ChiSo_Khoa_Nam UNIQUE (ChiSoChatLuongId, KhoaPhongId, Nam)
);
GO

-- ============================================================
-- 10. KỲ BÁO CÁO
-- ============================================================
CREATE TABLE dbo.KyBaoCao (
    KyBaoCaoId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_KyBaoCao PRIMARY KEY,
    TenKyBaoCao     NVARCHAR(255) NOT NULL,
    LoaiKyBaoCao    TINYINT       NOT NULL,   -- Tham chiếu DanhMucLookup 'TanSuatBaoCao'
    TuNgay          DATE          NOT NULL,
    DenNgay         DATE          NOT NULL,
    HanNop          DATE          NOT NULL,
    TrangThai       TINYINT       NOT NULL,   -- 1=Mở, 2=ĐóngNhập, 3=ĐãKhóa
    -- [THÊM] Ai tạo kỳ báo cáo
    NguoiTaoId      INT           NULL,
    -- [THÊM] Đếm số lần đã gửi nhắc nhở để tránh spam
    SoLanNhacNho    INT           NOT NULL DEFAULT (0),
    NgayNhacCuoi    DATETIME      NULL,
    NgayTao         DATETIME      NOT NULL CONSTRAINT DF_KyBaoCao_NgayTao DEFAULT (GETDATE()),
    NgayCapNhat     DATETIME      NULL,
    CONSTRAINT CK_KyBaoCao_LoaiKyBaoCao CHECK (LoaiKyBaoCao IN (1, 2, 3, 4, 5, 6, 7, 8)),
    CONSTRAINT CK_KyBaoCao_DateRange CHECK (TuNgay <= DenNgay AND HanNop >= DenNgay),
    CONSTRAINT CK_KyBaoCao_TrangThai CHECK (TrangThai IN (1, 2, 3)),
    CONSTRAINT UQ_KyBaoCao_Loai_TuNgay_DenNgay UNIQUE (LoaiKyBaoCao, TuNgay, DenNgay),
    CONSTRAINT FK_KyBaoCao_NguoiTao FOREIGN KEY (NguoiTaoId) REFERENCES dbo.TaiKhoan(TaiKhoanId)
);
GO

-- ============================================================
-- 11. BÁO CÁO (header)
-- ============================================================
CREATE TABLE dbo.BaoCao (
    BaoCaoId            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BaoCao PRIMARY KEY,
    KyBaoCaoId          INT      NOT NULL,
    KhoaPhongId         INT      NOT NULL,
    ChiSoChatLuongId    INT      NOT NULL,
    PhanCongChiSoId     INT      NULL,
    TrangThai           TINYINT  NOT NULL DEFAULT (1),  -- 1=ChưaNhập,...,6=YêuCầuChỉnhSửa
    NguoiTaoId          INT      NOT NULL,
    NguoiGuiId          INT      NULL,
    NgayGui             DATETIME NULL,
    -- [THÊM] Ai duyệt/từ chối
    NguoiDuyetId        INT      NULL,
    NgayDuyet           DATETIME NULL,
    YKienPhanHoi        NVARCHAR(MAX) NULL,
    NgayTao             DATETIME NOT NULL CONSTRAINT DF_BaoCao_NgayTao DEFAULT (GETDATE()),
    NgayCapNhat         DATETIME NULL,
    CONSTRAINT FK_BaoCao_KyBaoCao
        FOREIGN KEY (KyBaoCaoId)        REFERENCES dbo.KyBaoCao(KyBaoCaoId),
    CONSTRAINT FK_BaoCao_KhoaPhong
        FOREIGN KEY (KhoaPhongId)       REFERENCES dbo.KhoaPhong(KhoaPhongId),
    CONSTRAINT FK_BaoCao_ChiSo
        FOREIGN KEY (ChiSoChatLuongId)  REFERENCES dbo.ChiSoChatLuong(ChiSoChatLuongId),
    CONSTRAINT FK_BaoCao_PhanCong
        FOREIGN KEY (PhanCongChiSoId)   REFERENCES dbo.PhanCongChiSo(PhanCongChiSoId),
    CONSTRAINT FK_BaoCao_NguoiTao
        FOREIGN KEY (NguoiTaoId)        REFERENCES dbo.TaiKhoan(TaiKhoanId),
    CONSTRAINT FK_BaoCao_NguoiGui
        FOREIGN KEY (NguoiGuiId)        REFERENCES dbo.TaiKhoan(TaiKhoanId),
    CONSTRAINT FK_BaoCao_NguoiDuyet
        FOREIGN KEY (NguoiDuyetId)      REFERENCES dbo.TaiKhoan(TaiKhoanId),
    CONSTRAINT UQ_BaoCao_Ky_Khoa_ChiSo UNIQUE (KyBaoCaoId, KhoaPhongId, ChiSoChatLuongId),
    CONSTRAINT CK_BaoCao_TrangThai CHECK (TrangThai IN (1, 2, 3, 4, 5, 6))
);
GO

-- ============================================================
-- 12. BÁO CÁO CHI TIẾT (số liệu thực tế)
-- ============================================================
CREATE TABLE dbo.BaoCaoChiTiet (
    BaoCaoChiTietId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BaoCaoChiTiet PRIMARY KEY,
    BaoCaoId        INT           NOT NULL,
    -- Giá trị nhập thô (người dùng nhập vào)
    TuSoNhap        DECIMAL(18,4) NULL,    -- [THÊM] Giá trị tử số gốc người dùng nhập
    MauSoNhap       DECIMAL(18,4) NULL,    -- [THÊM] Giá trị mẫu số gốc người dùng nhập
    GiaTriNhap      DECIMAL(18,4) NULL,    -- Nhập trực tiếp (dùng khi LoaiCongThuc=3,4)
    -- Giá trị tính toán (hệ thống tự tính)
    TuSo            DECIMAL(18,4) NULL,    -- Sau khi validate/làm tròn
    MauSo           DECIMAL(18,4) NULL,
    KetQua          DECIMAL(18,4) NULL,    -- KQ số (tỷ lệ, tỷ số, số lượng...)
    KetQuaText      NVARCHAR(255) NULL,    -- KQ văn bản (khi LoaiCongThuc=5)
    DatMucTieu      BIT           NULL,    -- NULL=chưa đánh giá, 1=đạt, 0=không đạt
    GhiChu          NVARCHAR(MAX) NULL,
    -- [THÊM] Audit nhập liệu
    NguoiNhapId     INT           NULL,
    NgayNhap        DATETIME      NULL,
    NgayCapNhat     DATETIME      NULL,
    CONSTRAINT FK_BaoCaoChiTiet_BaoCao
        FOREIGN KEY (BaoCaoId) REFERENCES dbo.BaoCao(BaoCaoId),
    CONSTRAINT FK_BaoCaoChiTiet_NguoiNhap
        FOREIGN KEY (NguoiNhapId) REFERENCES dbo.TaiKhoan(TaiKhoanId),
    CONSTRAINT UQ_BaoCaoChiTiet_BaoCao UNIQUE (BaoCaoId)   -- 1 báo cáo chỉ có 1 chi tiết
);
GO

-- ============================================================
-- 13. THÔNG BÁO
-- ============================================================
CREATE TABLE dbo.ThongBao (
    ThongBaoId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ThongBao PRIMARY KEY,
    TieuDe          NVARCHAR(255) NOT NULL,
    NoiDung         NVARCHAR(MAX) NOT NULL,
    LoaiThongBao    TINYINT       NOT NULL,    -- 1-6, xem DanhMucLookup
    KyBaoCaoId      INT           NULL,
    BaoCaoId        INT           NULL,
    NguoiTaoId      INT           NULL,        -- NULL = thông báo tự động
    NgayTao         DATETIME      NOT NULL CONSTRAINT DF_ThongBao_NgayTao DEFAULT (GETDATE()),
    CONSTRAINT FK_ThongBao_KyBaoCao  FOREIGN KEY (KyBaoCaoId) REFERENCES dbo.KyBaoCao(KyBaoCaoId),
    CONSTRAINT FK_ThongBao_BaoCao    FOREIGN KEY (BaoCaoId)   REFERENCES dbo.BaoCao(BaoCaoId),
    CONSTRAINT FK_ThongBao_NguoiTao  FOREIGN KEY (NguoiTaoId) REFERENCES dbo.TaiKhoan(TaiKhoanId),
    CONSTRAINT CK_ThongBao_LoaiThongBao CHECK (LoaiThongBao IN (1, 2, 3, 4, 5, 6))
);
GO

-- ============================================================
-- 14. THÔNG BÁO NGƯỜI NHẬN
-- ============================================================
CREATE TABLE dbo.ThongBaoNguoiNhan (
    ThongBaoNguoiNhanId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ThongBaoNguoiNhan PRIMARY KEY,
    ThongBaoId          INT      NOT NULL,
    TaiKhoanId          INT      NOT NULL,
    DaDoc               BIT      NOT NULL CONSTRAINT DF_ThongBaoNguoiNhan_DaDoc DEFAULT (0),
    NgayDoc             DATETIME NULL,
    CONSTRAINT FK_ThongBaoNguoiNhan_ThongBao
        FOREIGN KEY (ThongBaoId)  REFERENCES dbo.ThongBao(ThongBaoId),
    CONSTRAINT FK_ThongBaoNguoiNhan_TaiKhoan
        FOREIGN KEY (TaiKhoanId)  REFERENCES dbo.TaiKhoan(TaiKhoanId),
    CONSTRAINT UQ_ThongBaoNguoiNhan UNIQUE (ThongBaoId, TaiKhoanId)
);
GO

-- ============================================================
-- 15. THÔNG BÁO TỰ ĐỘNG LOG (dedup)
-- ============================================================
CREATE TABLE dbo.ThongBaoTuDongLog (
    ThongBaoTuDongLogId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ThongBaoTuDongLog PRIMARY KEY,
    DedupKey            NVARCHAR(255) NOT NULL,
    LoaiThongBao        TINYINT       NOT NULL,
    KyBaoCaoId          INT           NULL,
    KhoaPhongId         INT           NULL,
    NgayMoc             DATE          NULL,
    NgayTao             DATETIME      NOT NULL CONSTRAINT DF_ThongBaoTuDongLog_NgayTao DEFAULT (GETDATE()),
    CONSTRAINT UQ_ThongBaoTuDongLog_DedupKey UNIQUE (DedupKey),
    CONSTRAINT FK_ThongBaoTuDongLog_KyBaoCao
        FOREIGN KEY (KyBaoCaoId)  REFERENCES dbo.KyBaoCao(KyBaoCaoId),
    CONSTRAINT FK_ThongBaoTuDongLog_KhoaPhong
        FOREIGN KEY (KhoaPhongId) REFERENCES dbo.KhoaPhong(KhoaPhongId),
    CONSTRAINT CK_ThongBaoTuDongLog_LoaiThongBao CHECK (LoaiThongBao IN (1, 2, 3, 4, 5, 6))
);
GO

-- ============================================================
-- 16. LỊCH SỬ IMPORT
-- ============================================================
CREATE TABLE dbo.LichSuImport (
    LichSuImportId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LichSuImport PRIMARY KEY,
    LoaiImport      TINYINT       NOT NULL,    -- 1=ChiSo, 2=KhoaPhong, 3=NhanVien
    TenFile         NVARCHAR(255) NOT NULL,
    TongSoDong      INT           NOT NULL,
    SoDongThanhCong INT           NOT NULL,
    SoDongLoi       INT           NOT NULL,
    NoiDungLoi      NVARCHAR(MAX) NULL,        -- [THÊM] Chi tiết dòng bị lỗi
    NguoiImportId   INT           NOT NULL,
    NgayImport      DATETIME      NOT NULL CONSTRAINT DF_LichSuImport_NgayImport DEFAULT (GETDATE()),
    CONSTRAINT FK_LichSuImport_NguoiImport
        FOREIGN KEY (NguoiImportId) REFERENCES dbo.TaiKhoan(TaiKhoanId),
    CONSTRAINT CK_LichSuImport_LoaiImport CHECK (LoaiImport IN (1, 2, 3)),
    CONSTRAINT CK_LichSuImport_SoDong
        CHECK (TongSoDong >= 0 AND SoDongThanhCong >= 0 AND SoDongLoi >= 0)
);
GO

-- ============================================================
-- 17. NHẬT KÝ HỆ THỐNG
-- ============================================================
CREATE TABLE dbo.NhatKyHeThong (
    NhatKyHeThongId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NhatKyHeThong PRIMARY KEY,
    TaiKhoanId      INT           NULL,
    ChucNang        NVARCHAR(100) NOT NULL,   -- Module: 'ChiSo','BaoCao','TaiKhoan'...
    HanhDong        NVARCHAR(100) NOT NULL,   -- 'CREATE','UPDATE','DELETE','LOGIN','SUBMIT'...
    DoiTuong        NVARCHAR(100) NULL,       -- Tên bảng
    DoiTuongId      INT           NULL,       -- ID bản ghi bị tác động
    NoiDung         NVARCHAR(MAX) NULL,       -- JSON snapshot nếu cần
    ThoiGian        DATETIME      NOT NULL CONSTRAINT DF_NhatKyHeThong_ThoiGian DEFAULT (GETDATE()),
    CONSTRAINT FK_NhatKyHeThong_TaiKhoan
        FOREIGN KEY (TaiKhoanId) REFERENCES dbo.TaiKhoan(TaiKhoanId)
);
GO

-- ============================================================
-- INDEXES
-- ============================================================

-- KhoaPhong
CREATE INDEX IX_KhoaPhong_TenKhoaPhong  ON dbo.KhoaPhong(TenKhoaPhong);
CREATE INDEX IX_KhoaPhong_DangHoatDong  ON dbo.KhoaPhong(DangHoatDong, TenKhoaPhong);

-- NhanVien
CREATE INDEX IX_NhanVien_KhoaPhongId    ON dbo.NhanVien(KhoaPhongId);
CREATE INDEX IX_NhanVien_HoTen          ON dbo.NhanVien(HoTen);

-- TaiKhoan
CREATE INDEX IX_TaiKhoan_KhoaPhongId    ON dbo.TaiKhoan(KhoaPhongId);
CREATE INDEX IX_TaiKhoan_NhanVienId     ON dbo.TaiKhoan(NhanVienId);
CREATE INDEX IX_TaiKhoan_Loai_Active    ON dbo.TaiKhoan(LoaiTaiKhoan, DangHoatDong, KhoaPhongId);

-- ChiSoChatLuong
CREATE INDEX IX_ChiSoChatLuong_Active_Order
    ON dbo.ChiSoChatLuong(DangHoatDong, SoThuTu, MaChiSo);
CREATE INDEX IX_ChiSoChatLuong_NhomChiSo
    ON dbo.ChiSoChatLuong(NhomChiSoId, DangHoatDong);

-- ChiSoLichBaoCao
CREATE INDEX IX_ChiSoLichBaoCao_TanSuat
    ON dbo.ChiSoLichBaoCao(TanSuatBaoCao, ChiSoChatLuongId);

-- PhanCongChiSo
CREATE INDEX IX_PhanCongChiSo_Khoa_Nam_Active
    ON dbo.PhanCongChiSo(KhoaPhongId, Nam, DangHoatDong, ChiSoChatLuongId);
CREATE INDEX IX_PhanCongChiSo_ChiSo
    ON dbo.PhanCongChiSo(ChiSoChatLuongId);

-- KyBaoCao
CREATE INDEX IX_KyBaoCao_TrangThai_Loai_HanNop
    ON dbo.KyBaoCao(TrangThai, LoaiKyBaoCao, HanNop);
CREATE INDEX IX_KyBaoCao_TuNgay
    ON dbo.KyBaoCao(TuNgay DESC);

-- BaoCao
CREATE INDEX IX_BaoCao_KyBaoCaoId       ON dbo.BaoCao(KyBaoCaoId);
CREATE INDEX IX_BaoCao_Khoa_TrangThai   ON dbo.BaoCao(KhoaPhongId, TrangThai);
CREATE INDEX IX_BaoCao_ChiSo            ON dbo.BaoCao(ChiSoChatLuongId);
CREATE INDEX IX_BaoCao_TrangThai        ON dbo.BaoCao(TrangThai);

-- ThongBao
CREATE INDEX IX_ThongBao_NgayTao        ON dbo.ThongBao(NgayTao DESC);
CREATE INDEX IX_ThongBao_KyBaoCaoId     ON dbo.ThongBao(KyBaoCaoId);
CREATE INDEX IX_ThongBao_BaoCaoId       ON dbo.ThongBao(BaoCaoId);

-- ThongBaoNguoiNhan
CREATE INDEX IX_ThongBaoNguoiNhan_TaiKhoan_DaDoc
    ON dbo.ThongBaoNguoiNhan(TaiKhoanId, DaDoc);

-- ThongBaoTuDongLog
CREATE INDEX IX_ThongBaoTuDongLog_Ky_Khoa
    ON dbo.ThongBaoTuDongLog(KyBaoCaoId, KhoaPhongId);

-- LichSuImport
CREATE INDEX IX_LichSuImport_NguoiImportId
    ON dbo.LichSuImport(NguoiImportId);

-- NhatKyHeThong
CREATE INDEX IX_NhatKyHeThong_TaiKhoan_ThoiGian
    ON dbo.NhatKyHeThong(TaiKhoanId, ThoiGian DESC);
CREATE INDEX IX_NhatKyHeThong_DoiTuong
    ON dbo.NhatKyHeThong(DoiTuong, DoiTuongId);

-- LichSuThayDoiChiSo
CREATE INDEX IX_LichSuThayDoi_ChiSo_ThoiGian
    ON dbo.LichSuThayDoiChiSo(ChiSoChatLuongId, ThoiGianThayDoi DESC);

GO

-- ============================================================
-- DỮ LIỆU KHỞI TẠO
-- ============================================================

-- Tài khoản admin mặc định
INSERT INTO dbo.TaiKhoan(TenDangNhap, MatKhauHash, LoaiTaiKhoan, DangHoatDong)
VALUES(
    N'admin',
    N'10000:AQIDBAUGBwgJCgsMDQ4PEA==:rJPsxUC5qMZAY/awUbhVdQPeOX+4Z12BD7E/F7/y5pA=',
    1,
    1
);
GO
