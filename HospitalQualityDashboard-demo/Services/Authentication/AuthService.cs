// Mục đích: xử lý xác thực, đổi mật khẩu và truy vấn hồ sơ tài khoản.
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.Enums;
using HospitalQualityDashboardDemo.Models.ViewModels;
using System;
using System.Data.SqlClient;

namespace HospitalQualityDashboardDemo.Services
{
    public class AuthenticatedUser
    {
        public int TaiKhoanId { get; set; }
        public string TenDangNhap { get; set; }
        public LoaiTaiKhoan LoaiTaiKhoan { get; set; }
        public int? NhanVienId { get; set; }
        public int? KhoaPhongId { get; set; }
        public string TenKhoaPhong { get; set; }
        public bool IsLocked { get; set; }
        public int FailedLoginCount { get; set; }
        public DateTime? LockoutUntil { get; set; }
    }

    public class AuthService
    {
        private const int MaxFailedLoginAttempts = 5;
        private const int LockoutMinutes = 15;
        private readonly string _connectionString;

        // Khởi tạo thành phần và các giá trị cần thiết cho xác thực và hồ sơ người dùng.
        public AuthService()
            : this(DatabaseConfiguration.GetConnectionString())
        {
        }

        // Khởi tạo thành phần và các giá trị cần thiết cho xác thực và hồ sơ người dùng.
        public AuthService(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string is required.", "connectionString");
            }

            _connectionString = connectionString;
        }

        // Xác thực tên đăng nhập, mật khẩu và trả về ngữ cảnh người dùng hợp lệ.
        public AuthenticatedUser Authenticate(string username, string password)
        {
            const string sql = @"
SELECT TOP 1
    tk.TaiKhoanId,
    tk.TenDangNhap,
    tk.MatKhauHash,
    tk.LoaiTaiKhoan,
    tk.NhanVienId,
    tk.KhoaPhongId,
    tk.DangHoatDong AS TaiKhoanDangHoatDong,
    tk.FailedLoginCount,
    tk.LockoutUntil,
    nv.DangHoatDong AS NhanVienDangHoatDong,
    kp.TenKhoaPhong
FROM dbo.TaiKhoan tk
LEFT JOIN dbo.NhanVien nv ON nv.NhanVienId = tk.NhanVienId
LEFT JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = tk.KhoaPhongId
WHERE tk.TenDangNhap = @TenDangNhap";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@TenDangNhap", username ?? string.Empty);
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    var lockoutUntil = ReadNullableDateTime(reader, "LockoutUntil");
                    if (lockoutUntil.HasValue && lockoutUntil.Value > DateTime.Now)
                    {
                        return MapAuthenticatedUser(reader, true);
                    }

                    var storedHash = reader.GetString(reader.GetOrdinal("MatKhauHash"));
                    if (!PasswordHasher.Verify(password, storedHash))
                    {
                        return null;
                    }

                    return MapAuthenticatedUser(reader, false);
                }
            }
        }

        // Truy vấn xác thực và hồ sơ người dùng theo điều kiện được cung cấp.
        public AuthenticatedUser GetAuthenticatedUser(int taiKhoanId)
        {
            const string sql = @"
SELECT TOP 1
    tk.TaiKhoanId,
    tk.TenDangNhap,
    tk.LoaiTaiKhoan,
    tk.NhanVienId,
    tk.KhoaPhongId,
    tk.DangHoatDong AS TaiKhoanDangHoatDong,
    tk.FailedLoginCount,
    tk.LockoutUntil,
    nv.DangHoatDong AS NhanVienDangHoatDong,
    kp.TenKhoaPhong
FROM dbo.TaiKhoan tk
LEFT JOIN dbo.NhanVien nv ON nv.NhanVienId = tk.NhanVienId
LEFT JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = tk.KhoaPhongId
WHERE tk.TaiKhoanId = @TaiKhoanId";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@TaiKhoanId", taiKhoanId);
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    var lockoutUntil = ReadNullableDateTime(reader, "LockoutUntil");
                    return MapAuthenticatedUser(reader, lockoutUntil.HasValue && lockoutUntil.Value > DateTime.Now);
                }
            }
        }

        // Kiểm tra và cập nhật dữ liệu của xác thực và hồ sơ người dùng.
        public void UpdateLastLogin(int taiKhoanId)
        {
            ExecuteNonQuery(
                "UPDATE dbo.TaiKhoan SET LanDangNhapCuoi = GETDATE(), NgayCapNhat = GETDATE() WHERE TaiKhoanId = @TaiKhoanId",
                new SqlParameter("@TaiKhoanId", taiKhoanId));
        }

        // Ghi nhận lần đăng nhập thất bại để phục vụ cơ chế khóa tạm thời.
        public void RecordFailedLogin(string username)
        {
            ExecuteNonQuery(@"
UPDATE dbo.TaiKhoan
SET FailedLoginCount = FailedLoginCount + 1,
    LockoutUntil = CASE WHEN FailedLoginCount + 1 >= @MaxAttempts THEN DATEADD(minute, @LockoutMinutes, GETDATE()) ELSE LockoutUntil END,
    NgayCapNhat = GETDATE()
WHERE TenDangNhap = @TenDangNhap",
                new SqlParameter("@MaxAttempts", MaxFailedLoginAttempts),
                new SqlParameter("@LockoutMinutes", LockoutMinutes),
                new SqlParameter("@TenDangNhap", username ?? string.Empty));
        }

        // Xóa trạng thái tạm để chuẩn bị lượt xử lý mới của xác thực và hồ sơ người dùng.
        public void ResetFailedLogin(int taiKhoanId)
        {
            ExecuteNonQuery(
                "UPDATE dbo.TaiKhoan SET FailedLoginCount = 0, LockoutUntil = NULL, NgayCapNhat = GETDATE() WHERE TaiKhoanId = @TaiKhoanId",
                new SqlParameter("@TaiKhoanId", taiKhoanId));
        }

        // Kiểm tra mật khẩu hiện tại và lưu mật khẩu mới an toàn.
        public bool ChangePassword(int taiKhoanId, string currentPassword, string newPassword)
        {
            var currentHash = GetPasswordHash(taiKhoanId);
            if (!PasswordHasher.Verify(currentPassword, currentHash))
            {
                return false;
            }

            ExecuteNonQuery(
                "UPDATE dbo.TaiKhoan SET MatKhauHash = @MatKhauHash, NgayCapNhat = GETDATE() WHERE TaiKhoanId = @TaiKhoanId",
                new SqlParameter("@MatKhauHash", PasswordHasher.Hash(newPassword)),
                new SqlParameter("@TaiKhoanId", taiKhoanId));

            return true;
        }

        // Truy vấn xác thực và hồ sơ người dùng theo điều kiện được cung cấp.
        public UserProfileViewModel GetUserProfile(int taiKhoanId)
        {
            const string sql = @"
SELECT TOP 1
    tk.TaiKhoanId,
    tk.TenDangNhap,
    tk.LoaiTaiKhoan,
    tk.NhanVienId,
    tk.KhoaPhongId AS TaiKhoanKhoaPhongId,
    tk.DangHoatDong AS TaiKhoanDangHoatDong,
    tk.LanDangNhapCuoi,
    nv.MaNhanVien,
    nv.HoTen,
    nv.NgaySinh,
    nv.GioiTinh,
    nv.ChucVu,
    nv.Email,
    nv.SoDienThoai,
    nv.KhoaPhongId AS NhanVienKhoaPhongId,
    nv.DangHoatDong AS NhanVienDangHoatDong,
    kp.TenKhoaPhong
FROM dbo.TaiKhoan tk
LEFT JOIN dbo.NhanVien nv ON nv.NhanVienId = tk.NhanVienId
LEFT JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = ISNULL(tk.KhoaPhongId, nv.KhoaPhongId)
WHERE tk.TaiKhoanId = @TaiKhoanId";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@TaiKhoanId", taiKhoanId);
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return new UserProfileViewModel
                    {
                        TaiKhoanId = reader.GetInt32(reader.GetOrdinal("TaiKhoanId")),
                        TenDangNhap = reader.GetString(reader.GetOrdinal("TenDangNhap")),
                        LoaiTaiKhoan = (LoaiTaiKhoan)reader.GetByte(reader.GetOrdinal("LoaiTaiKhoan")),
                        NhanVienId = ReadNullableInt(reader, "NhanVienId"),
                        MaNhanVien = ReadNullableString(reader, "MaNhanVien"),
                        HoTen = ReadNullableString(reader, "HoTen"),
                        NgaySinh = ReadNullableDateTime(reader, "NgaySinh"),
                        GioiTinh = ReadNullableString(reader, "GioiTinh"),
                        ChucVu = ReadNullableString(reader, "ChucVu"),
                        Email = ReadNullableString(reader, "Email"),
                        SoDienThoai = ReadNullableString(reader, "SoDienThoai"),
                        KhoaPhongId = ReadNullableInt(reader, "TaiKhoanKhoaPhongId") ?? ReadNullableInt(reader, "NhanVienKhoaPhongId"),
                        TenKhoaPhong = ReadNullableString(reader, "TenKhoaPhong"),
                        TaiKhoanDangHoatDong = reader.GetBoolean(reader.GetOrdinal("TaiKhoanDangHoatDong")),
                        NhanVienDangHoatDong = ReadNullableBool(reader, "NhanVienDangHoatDong"),
                        LanDangNhapCuoi = ReadNullableDateTime(reader, "LanDangNhapCuoi"),
                        ChangePassword = new ChangePasswordViewModel()
                    };
                }
            }
        }

        // Kiểm tra và cập nhật dữ liệu của xác thực và hồ sơ người dùng.
        public void UpdateProfile(int taiKhoanId, ProfileUpdateDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException("dto");
            }

            int? nhanVienId = GetNhanVienId(taiKhoanId);
            if (!nhanVienId.HasValue)
            {
                throw new InvalidOperationException("Tài khoản chưa liên kết với nhân viên, không thể cập nhật thông tin cá nhân.");
            }

            const string sql = @"
UPDATE dbo.NhanVien
SET HoTen = @HoTen,
    NgaySinh = @NgaySinh,
    GioiTinh = @GioiTinh,
    ChucVu = @ChucVu,
    Email = @Email,
    SoDienThoai = @SoDienThoai,
    NgayCapNhat = GETDATE()
WHERE NhanVienId = @NhanVienId";

            ExecuteNonQuery(sql,
                new SqlParameter("@HoTen", (object)dto.HoTen ?? DBNull.Value),
                new SqlParameter("@NgaySinh", (object)dto.NgaySinh ?? DBNull.Value),
                new SqlParameter("@GioiTinh", (object)dto.GioiTinh ?? DBNull.Value),
                new SqlParameter("@ChucVu", (object)dto.ChucVu ?? DBNull.Value),
                new SqlParameter("@Email", (object)dto.Email ?? DBNull.Value),
                new SqlParameter("@SoDienThoai", (object)dto.SoDienThoai ?? DBNull.Value),
                new SqlParameter("@NhanVienId", nhanVienId.Value));
        }

        // Truy vấn xác thực và hồ sơ người dùng theo điều kiện được cung cấp.
        private int? GetNhanVienId(int taiKhoanId)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("SELECT NhanVienId FROM dbo.TaiKhoan WHERE TaiKhoanId = @TaiKhoanId", connection))
            {
                command.Parameters.AddWithValue("@TaiKhoanId", taiKhoanId);
                connection.Open();
                var result = command.ExecuteScalar();
                return result == DBNull.Value || result == null ? (int?)null : Convert.ToInt32(result);
            }
        }

        // Truy vấn xác thực và hồ sơ người dùng theo điều kiện được cung cấp.
        private string GetPasswordHash(int taiKhoanId)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("SELECT MatKhauHash FROM dbo.TaiKhoan WHERE TaiKhoanId = @TaiKhoanId", connection))
            {
                command.Parameters.AddWithValue("@TaiKhoanId", taiKhoanId);
                connection.Open();
                return command.ExecuteScalar() as string;
            }
        }

        // Thực thi quy trình xử lý của xác thực và hồ sơ người dùng.
        private void ExecuteNonQuery(string sql, params SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddRange(parameters);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        // Chuyển dữ liệu nguồn sang cấu trúc dùng cho xác thực và hồ sơ người dùng.
        private static int? ReadNullableInt(SqlDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (int?)null : reader.GetInt32(ordinal);
        }

        // Chuyển dữ liệu nguồn sang cấu trúc dùng cho xác thực và hồ sơ người dùng.
        private static string ReadNullableString(SqlDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }



        // Chuyển dữ liệu nguồn sang cấu trúc dùng cho xác thực và hồ sơ người dùng.
        private static DateTime? ReadNullableDateTime(SqlDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (DateTime?)null : reader.GetDateTime(ordinal);
        }

        // Chuyển dữ liệu nguồn sang cấu trúc dùng cho xác thực và hồ sơ người dùng.
        private static bool? ReadNullableBool(SqlDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (bool?)null : reader.GetBoolean(ordinal);
        }

        // Xác định dữ liệu có thỏa điều kiện nghiệp vụ của xác thực và hồ sơ người dùng hay không.
        private static bool IsEmployeeLocked(SqlDataReader reader)
        {
            var ordinal = reader.GetOrdinal("NhanVienDangHoatDong");
            return !reader.IsDBNull(ordinal) && !reader.GetBoolean(ordinal);
        }

        // Chuyển dữ liệu nguồn sang cấu trúc dùng cho xác thực và hồ sơ người dùng.
        private static AuthenticatedUser MapAuthenticatedUser(SqlDataReader reader, bool isTemporarilyLocked)
        {
            return new AuthenticatedUser
            {
                TaiKhoanId = reader.GetInt32(reader.GetOrdinal("TaiKhoanId")),
                TenDangNhap = reader.GetString(reader.GetOrdinal("TenDangNhap")),
                LoaiTaiKhoan = (LoaiTaiKhoan)reader.GetByte(reader.GetOrdinal("LoaiTaiKhoan")),
                NhanVienId = ReadNullableInt(reader, "NhanVienId"),
                KhoaPhongId = ReadNullableInt(reader, "KhoaPhongId"),
                TenKhoaPhong = ReadNullableString(reader, "TenKhoaPhong"),
                FailedLoginCount = reader.IsDBNull(reader.GetOrdinal("FailedLoginCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("FailedLoginCount")),
                LockoutUntil = ReadNullableDateTime(reader, "LockoutUntil"),
                IsLocked = isTemporarilyLocked || !reader.GetBoolean(reader.GetOrdinal("TaiKhoanDangHoatDong")) || IsEmployeeLocked(reader)
            };
        }
    }
}
