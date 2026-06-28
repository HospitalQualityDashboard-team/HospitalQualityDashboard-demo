// Mục đích: quản lý hồ sơ nhân viên, tài khoản liên kết và dữ liệu phân quyền khoa/phòng.
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.Enums;
using HospitalQualityDashboardDemo.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Services
{
    // Service quản lý nhân viên, tài khoản liên kết và import nhân viên.
    public class EmployeeService : DbServiceBase
    {
        // Service đọc file Excel/CSV import.
        private readonly ExcelImportExportService _excel = new ExcelImportExportService();

        // Lấy danh sách nhân viên, có thể lọc theo khoa/phòng.
        public IList<NhanVienViewModel> GetAll(int? khoaPhongId)
        {
            const string sql = @"
SELECT nv.NhanVienId, nv.MaNhanVien, nv.HoTen, nv.NgaySinh, nv.GioiTinh, nv.ChucVu, nv.Email, nv.SoDienThoai,
       nv.KhoaPhongId, kp.TenKhoaPhong, nv.DangHoatDong,
       CAST(CASE WHEN EXISTS (
           SELECT 1
           FROM dbo.TaiKhoan tk
           WHERE tk.NhanVienId = nv.NhanVienId OR tk.TenDangNhap = nv.MaNhanVien
       ) THEN 1 ELSE 0 END AS BIT) AS HasAccount
FROM dbo.NhanVien nv
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = nv.KhoaPhongId
WHERE (@KhoaPhongId IS NULL OR nv.KhoaPhongId = @KhoaPhongId)
ORDER BY nv.NhanVienId";
            // Map từng dòng SQL thành NhanVienViewModel cho màn hình danh sách.
            return Query(sql, MapEmployee, Param("@KhoaPhongId", khoaPhongId));
        }

        // Lấy danh sách hồ sơ nhân viên theo bộ lọc, trạng thái hoạt động và phạm vi quyền đang áp dụng.
        public IList<NhanVienViewModel> GetAll(int? khoaPhongId, int page, int pageSize, out int totalItems)
        {
            page = NormalizePage(page);
            pageSize = NormalizePageSize(pageSize);
            totalItems = Convert.ToInt32(Scalar(@"
SELECT COUNT(*)
FROM dbo.NhanVien nv
WHERE (@KhoaPhongId IS NULL OR nv.KhoaPhongId = @KhoaPhongId)",
                Param("@KhoaPhongId", khoaPhongId)));

            const string sql = @"
SELECT nv.NhanVienId, nv.MaNhanVien, nv.HoTen, nv.NgaySinh, nv.GioiTinh, nv.ChucVu, nv.Email, nv.SoDienThoai,
       nv.KhoaPhongId, kp.TenKhoaPhong, nv.DangHoatDong,
       CAST(CASE WHEN EXISTS (
           SELECT 1
           FROM dbo.TaiKhoan tk
           WHERE tk.NhanVienId = nv.NhanVienId OR tk.TenDangNhap = nv.MaNhanVien
       ) THEN 1 ELSE 0 END AS BIT) AS HasAccount
FROM dbo.NhanVien nv
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = nv.KhoaPhongId
WHERE (@KhoaPhongId IS NULL OR nv.KhoaPhongId = @KhoaPhongId)
ORDER BY nv.NhanVienId
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            return Query(sql, MapEmployee,
                Param("@KhoaPhongId", khoaPhongId),
                Param("@Offset", (page - 1) * pageSize),
                Param("@PageSize", pageSize));
        }

        // Lấy một nhân viên theo id để hiển thị form sửa hoặc tạo tài khoản.
        public NhanVienViewModel Get(int id)
        {
            const string sql = @"
SELECT nv.NhanVienId, nv.MaNhanVien, nv.HoTen, nv.NgaySinh, nv.GioiTinh, nv.ChucVu, nv.Email, nv.SoDienThoai,
       nv.KhoaPhongId, kp.TenKhoaPhong, nv.DangHoatDong,
       CAST(CASE WHEN EXISTS (
           SELECT 1
           FROM dbo.TaiKhoan tk
           WHERE tk.NhanVienId = nv.NhanVienId OR tk.TenDangNhap = nv.MaNhanVien
       ) THEN 1 ELSE 0 END AS BIT) AS HasAccount
FROM dbo.NhanVien nv
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = nv.KhoaPhongId
WHERE nv.NhanVienId = @Id";
            // QuerySingle trả về duy nhất một nhân viên.
            return QuerySingle(sql, MapEmployee, Param("@Id", id));
        }

        // Lưu thông tin nhân viên: thêm mới nếu id = 0, ngược lại thì cập nhật.
        public void Save(EmployeeSaveDto dto)
        {
            Save(new NhanVienViewModel
            {
                NhanVienId = dto.NhanVienId,
                MaNhanVien = dto.MaNhanVien,
                HoTen = dto.HoTen,
                NgaySinh = dto.NgaySinh,
                GioiTinh = dto.GioiTinh,
                ChucVu = dto.ChucVu,
                Email = dto.Email,
                SoDienThoai = dto.SoDienThoai,
                KhoaPhongId = dto.KhoaPhongId,
                DangHoatDong = dto.DangHoatDong
            });
        }

        // Lưu hồ sơ nhân viên theo model/dto đã validate, bao gồm cả nhánh thêm mới và cập nhật.
        public void Save(NhanVienViewModel model)
        {
            if (model.NhanVienId == 0)
            {
                // NhanVienId = 0 nghĩa là thêm nhân viên mới.
                Execute(@"INSERT INTO dbo.NhanVien(MaNhanVien, HoTen, NgaySinh, GioiTinh, ChucVu, Email, SoDienThoai, KhoaPhongId, DangHoatDong)
VALUES(@MaNhanVien, @HoTen, @NgaySinh, @GioiTinh, @ChucVu, @Email, @SoDienThoai, @KhoaPhongId, @DangHoatDong)",
                    EmployeeParams(model));
                return;
            }

            // Đã có NhanVienId thì cập nhật thông tin nhân viên hiện tại.
            var parameters = EmployeeParams(model).Concat(new[] { Param("@NhanVienId", model.NhanVienId) }).ToArray();
            Execute(@"UPDATE dbo.NhanVien SET MaNhanVien=@MaNhanVien, HoTen=@HoTen, NgaySinh=@NgaySinh, GioiTinh=@GioiTinh,
ChucVu=@ChucVu, Email=@Email, SoDienThoai=@SoDienThoai, KhoaPhongId=@KhoaPhongId, DangHoatDong=@DangHoatDong, NgayCapNhat=GETDATE()
WHERE NhanVienId=@NhanVienId", parameters);
        }

        // Khóa/mở khóa nhân viên và tài khoản đăng nhập liên quan.
        public void SetActive(int id, bool active)
        {
            Execute(@"
UPDATE dbo.NhanVien SET DangHoatDong = @Active, NgayCapNhat = GETDATE() WHERE NhanVienId = @Id;
UPDATE tk SET DangHoatDong = @Active, NgayCapNhat = GETDATE()
FROM dbo.TaiKhoan tk
INNER JOIN dbo.NhanVien nv ON nv.NhanVienId = @Id
WHERE tk.NhanVienId = nv.NhanVienId OR tk.TenDangNhap = nv.MaNhanVien;",
                Param("@Active", active),
                Param("@Id", id));
        }

        // Xóa nhân viên nếu nhân viên chưa có tài khoản.
        public void Delete(int id)
        {
            var accountCount = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.TaiKhoan WHERE NhanVienId=@Id", Param("@Id", id)));
            if (accountCount > 0)
            {
                // Đã có tài khoản thì không xóa cứng, chỉ nên khóa.
                throw new InvalidOperationException("Nhân viên đã có tài khoản, vui lòng khóa thay vì xóa.");
            }

            Execute("DELETE FROM dbo.NhanVien WHERE NhanVienId=@Id", Param("@Id", id));
        }

        // Tạo tài khoản User cho nhân viên.
        public void CreateUserAccount(CreateUserAccountDto dto)
        {
            CreateUserAccount(new CreateUserAccountViewModel
            {
                NhanVienId = dto.NhanVienId,
                TenDangNhap = dto.TenDangNhap,
                MatKhau = dto.MatKhau
            });
        }

        // Tạo tài khoản User gắn với nhân viên và khoa/phòng hiện tại của nhân viên.
        public void CreateUserAccount(CreateUserAccountViewModel model)
        {
            var employee = Get(model.NhanVienId);
            // Lưu mật khẩu đã hash, không lưu mật khẩu gốc.
            Execute(@"INSERT INTO dbo.TaiKhoan(TenDangNhap, MatKhauHash, LoaiTaiKhoan, NhanVienId, KhoaPhongId, DangHoatDong)
VALUES(@TenDangNhap, @MatKhauHash, @LoaiTaiKhoan, @NhanVienId, @KhoaPhongId, 1)",
                Param("@TenDangNhap", model.TenDangNhap),
                Param("@MatKhauHash", PasswordHasher.Hash(model.MatKhau)),
                Param("@LoaiTaiKhoan", (byte)LoaiTaiKhoan.User),
                Param("@NhanVienId", model.NhanVienId),
                Param("@KhoaPhongId", employee.KhoaPhongId));
        }

        // Kiểm tra tên đăng nhập đã tồn tại chưa.
        public bool IsUsernameExists(string username)
        {
            var count = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.TaiKhoan WHERE TenDangNhap = @TenDangNhap", Param("@TenDangNhap", username)));
            return count > 0;
        }

        // Kiểm tra nhân viên đã có tài khoản chưa.
        public bool HasAccount(int nhanVienId)
        {
            var count = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.TaiKhoan WHERE NhanVienId = @NhanVienId", Param("@NhanVienId", nhanVienId)));
            return count > 0;
        }

        // Import nhân viên từ file, dòng hợp lệ sẽ được thêm/cập nhật.
        public ImportResultViewModel Import(HttpPostedFileBase file, int? selectedKhoaPhongId, int userId)
        {
            var rows = _excel.ReadWorksheet(file);
            var result = new ImportResultViewModel { TongSoDong = rows.Count };

            ExecuteInTransaction((conn, trans) =>
            {
                for (var i = 0; i < rows.Count; i++)
                {
                    // Excel có dòng tiêu đề, nên dòng dữ liệu đầu tiên là dòng số 2.
                    var row = rows[i];
                    var rowNumber = i + 2;
                    string value;
                    var model = new NhanVienViewModel { DangHoatDong = true };

                    // Đọc các cột có trong file vào view model.
                    if (row.TryGetValue("MaNhanVien", out value)) model.MaNhanVien = value;
                    if (row.TryGetValue("HoTen", out value)) model.HoTen = value;
                    if (row.TryGetValue("GioiTinh", out value)) model.GioiTinh = value;
                    if (row.TryGetValue("ChucVu", out value)) model.ChucVu = value;
                    if (row.TryGetValue("Email", out value)) model.Email = value;
                    if (row.TryGetValue("SoDienThoai", out value)) model.SoDienThoai = value;

                    if (string.IsNullOrWhiteSpace(model.MaNhanVien) || string.IsNullOrWhiteSpace(model.HoTen))
                    {
                        // Mã nhân viên và họ tên là dữ liệu bắt buộc.
                        result.Errors.Add("Dong " + rowNumber + ": MaNhanVien va HoTen la bat buoc.");
                        result.SoDongLoi++;
                        continue;
                    }

                    if (selectedKhoaPhongId.HasValue)
                    {
                        // Nếu đang lọc theo khoa/phòng thì import vào khoa/phòng đó.
                        model.KhoaPhongId = selectedKhoaPhongId.Value;
                    }
                    else
                    {
                        // Import toàn viện cần cột IDKHOAPHONG để tìm khoa/phòng.
                        string sourceIdText;
                        int sourceId;
                        if (!row.TryGetValue("IDKHOAPHONG", out sourceIdText) || !int.TryParse(sourceIdText, out sourceId))
                        {
                            result.Errors.Add("Dong " + rowNumber + ": thieu IDKHOAPHONG khi import toan vien.");
                            result.SoDongLoi++;
                            continue;
                        }

                        var khoaPhongId = Scalar(conn, trans, "SELECT KhoaPhongId FROM dbo.KhoaPhong WHERE IdKhoaPhongNguon = @SourceId", Param("@SourceId", sourceId));
                        if (khoaPhongId == null)
                        {
                            // Bỏ qua dòng nếu khoa/phòng trong file không có trong hệ thống.
                            result.Errors.Add("Dong " + rowNumber + ": khoa/phong khong ton tai.");
                            result.SoDongLoi++;
                            continue;
                        }

                        model.KhoaPhongId = Convert.ToInt32(khoaPhongId);
                    }

                    var nvId = Scalar(conn, trans, @"
IF EXISTS (SELECT 1 FROM dbo.NhanVien WHERE MaNhanVien = @MaNhanVien)
BEGIN
    UPDATE dbo.NhanVien SET HoTen=@HoTen, GioiTinh=@GioiTinh, ChucVu=@ChucVu, Email=@Email, SoDienThoai=@SoDienThoai, KhoaPhongId=@KhoaPhongId, NgayCapNhat=GETDATE() WHERE MaNhanVien=@MaNhanVien;
    SELECT NhanVienId FROM dbo.NhanVien WHERE MaNhanVien=@MaNhanVien;
END
ELSE
BEGIN
    INSERT INTO dbo.NhanVien(MaNhanVien, HoTen, GioiTinh, ChucVu, Email, SoDienThoai, KhoaPhongId, DangHoatDong)
    OUTPUT INSERTED.NhanVienId
    VALUES(@MaNhanVien, @HoTen, @GioiTinh, @ChucVu, @Email, @SoDienThoai, @KhoaPhongId, 1);
END", EmployeeParams(model));

                    int employeeId = Convert.ToInt32(nvId);

                    // Sau khi import nhân viên, tạo tài khoản mặc định nếu chưa có.
                    var hasAccount = Scalar(conn, trans, "SELECT 1 FROM dbo.TaiKhoan WHERE NhanVienId=@EmployeeId OR TenDangNhap=@TenDangNhap",
                        Param("@EmployeeId", employeeId),
                        Param("@TenDangNhap", model.MaNhanVien));

                    if (hasAccount == null)
                    {
                        // Mật khẩu mặc định bằng mã nhân viên và được hash trước khi lưu.
                        Execute(conn, trans, @"INSERT INTO dbo.TaiKhoan(TenDangNhap, MatKhauHash, LoaiTaiKhoan, NhanVienId, KhoaPhongId, DangHoatDong)
VALUES(@TenDangNhap, @MatKhauHash, @LoaiTaiKhoan, @NhanVienId, @KhoaPhongId, 1)",
                            Param("@TenDangNhap", model.MaNhanVien),
                            Param("@MatKhauHash", PasswordHasher.Hash(model.MaNhanVien)),
                            Param("@LoaiTaiKhoan", (byte)LoaiTaiKhoan.User),
                            Param("@NhanVienId", employeeId),
                            Param("@KhoaPhongId", model.KhoaPhongId));
                    }

                    result.SoDongThanhCong++;
                }

                // Ghi lịch sử import để truy vết người import và kết quả.
                Execute(conn, trans, @"INSERT INTO dbo.LichSuImport(LoaiImport, TenFile, TongSoDong, SoDongThanhCong, SoDongLoi, NguoiImportId)
VALUES(@LoaiImport, @TenFile, @TongSoDong, @SoDongThanhCong, @SoDongLoi, @NguoiImportId)",
                    Param("@LoaiImport", (byte)LoaiImport.NhanVien),
                    Param("@TenFile", file == null ? null : file.FileName),
                    Param("@TongSoDong", result.TongSoDong),
                    Param("@SoDongThanhCong", result.SoDongThanhCong),
                    Param("@SoDongLoi", result.SoDongLoi),
                    Param("@NguoiImportId", userId));
            });

            return result;
        }

        // Chuyển view model thành danh sách tham số SQL.
        private static int NormalizePage(int page)
        {
            return page < 1 ? 1 : page;
        }

        // Giới hạn kích thước trang để tránh truy vấn quá lớn hoặc giá trị không hợp lệ.
        private static int NormalizePageSize(int pageSize)
        {
            if (pageSize < 1) return 20;
            return pageSize > 100 ? 100 : pageSize;
        }

        // Tạo tập tham số SQL từ model để dùng cho thao tác ghi dữ liệu.
        private static SqlParameter[] EmployeeParams(NhanVienViewModel model)
        {
            // Gom dữ liệu trên form thành tham số SQL, tránh nối chuỗi câu lệnh.
            return new[]
            {
                Param("@MaNhanVien", model.MaNhanVien),
                Param("@HoTen", model.HoTen),
                Param("@NgaySinh", model.NgaySinh),
                Param("@GioiTinh", model.GioiTinh),
                Param("@ChucVu", model.ChucVu),
                Param("@Email", model.Email),
                Param("@SoDienThoai", model.SoDienThoai),
                Param("@KhoaPhongId", model.KhoaPhongId),
                Param("@DangHoatDong", model.DangHoatDong)
            };
        }

        // Chuyển một dòng SqlDataReader thành view model nhân viên.
        private static NhanVienViewModel MapEmployee(SqlDataReader reader)
        {
            return new NhanVienViewModel
            {
                NhanVienId = Int(reader, "NhanVienId"),
                MaNhanVien = String(reader, "MaNhanVien"),
                HoTen = String(reader, "HoTen"),
                NgaySinh = NullableDateTime(reader, "NgaySinh"),
                GioiTinh = String(reader, "GioiTinh"),
                ChucVu = String(reader, "ChucVu"),
                Email = String(reader, "Email"),
                SoDienThoai = String(reader, "SoDienThoai"),
                KhoaPhongId = Int(reader, "KhoaPhongId"),
                TenKhoaPhong = String(reader, "TenKhoaPhong"),
                DangHoatDong = reader.GetBoolean(reader.GetOrdinal("DangHoatDong")),
                HasAccount = reader.GetBoolean(reader.GetOrdinal("HasAccount"))
            };
        }
    }
}
