// Mục đích: xử lý xác thực, đổi mật khẩu và truy vấn hồ sơ tài khoản.
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.Enums;
using HospitalQualityDashboardDemo.Models.ViewModels;
using System;
using System.Data.SqlClient;
using System.Web;

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
        public bool IsDepartmentLocked { get; set; }
        public int FailedLoginCount { get; set; }
        public DateTime? LockoutUntil { get; set; }
    }

    public class AuthService
    {
        private const int MaxFailedLoginAttempts = 5;
        private const int LockoutMinutes = 15;
        private readonly string _connectionString;

        // Khởi tạo xác thực và hồ sơ người dùng với giá trị mặc định để các luồng xử lý phía sau không gặp trạng thái null ngoài ý muốn.
        public AuthService()
            : this(DatabaseConfiguration.GetConnectionString())
        {
        }

        // Khởi tạo xác thực và hồ sơ người dùng với giá trị mặc định để các luồng xử lý phía sau không gặp trạng thái null ngoài ý muốn.
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
    kp.TenKhoaPhong,
    kp.Used AS KhoaPhongUsed
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

        // Nạp lại ngữ cảnh tài khoản từ database để session phản ánh khóa tài khoản, role và khoa/phòng mới nhất.
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
    kp.TenKhoaPhong,
    kp.Used AS KhoaPhongUsed
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

        // Ghi thời điểm đăng nhập thành công để phục vụ audit và theo dõi hoạt động tài khoản.
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

        // Lấy hồ sơ tài khoản kèm thông tin nhân viên/khoa phòng để hiển thị trang profile.
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

        // Cập nhật nhanh hồ sơ người dùng qua DTO, dùng cho luồng profile không đổi thông tin đăng nhập.
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

        // Lấy nhân viên liên kết với tài khoản trước khi cho phép cập nhật thông tin cá nhân.
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

        // Lấy hash mật khẩu hiện tại để xác minh trước khi đổi mật khẩu.
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

        // Thực thi câu lệnh ghi dữ liệu có tham số, dùng chung cho các cập nhật nhỏ trong service.
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

        // Đọc trường có thể null từ SqlDataReader và chuyển về kiểu C# tương ứng để tránh lỗi DBNull.
        private static int? ReadNullableInt(SqlDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (int?)null : reader.GetInt32(ordinal);
        }

        // Đọc trường có thể null từ SqlDataReader và chuyển về kiểu C# tương ứng để tránh lỗi DBNull.
        private static string ReadNullableString(SqlDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }



        // Đọc trường có thể null từ SqlDataReader và chuyển về kiểu C# tương ứng để tránh lỗi DBNull.
        private static DateTime? ReadNullableDateTime(SqlDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (DateTime?)null : reader.GetDateTime(ordinal);
        }

        // Đọc trường có thể null từ SqlDataReader và chuyển về kiểu C# tương ứng để tránh lỗi DBNull.
        private static bool? ReadNullableBool(SqlDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (bool?)null : reader.GetBoolean(ordinal);
        }

        // Xác định nhân viên liên kết tài khoản có đang bị khóa hay không để chặn đăng nhập gián tiếp.
        private static bool IsEmployeeLocked(SqlDataReader reader)
        {
            var ordinal = reader.GetOrdinal("NhanVienDangHoatDong");
            return !reader.IsDBNull(ordinal) && !reader.GetBoolean(ordinal);
        }

        private static bool IsDepartmentLocked(SqlDataReader reader)
        {
            var ordinal = reader.GetOrdinal("KhoaPhongUsed");
            return !reader.IsDBNull(ordinal) && !reader.GetBoolean(ordinal);
        }

        // Khởi tạo token ghi nhớ đăng nhập an toàn, lưu hash vào DB và trả về cookie value
        public string CreatePersistentToken(int taiKhoanId)
        {
            var randomToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            var tokenHash = PasswordHasher.Hash(randomToken);
            var expiry = DateTime.Now.AddDays(30);

            ExecuteNonQuery(
                "UPDATE dbo.TaiKhoan SET RememberTokenHash = @Hash, RememberTokenExpiry = @Expiry, NgayCapNhat = GETDATE() WHERE TaiKhoanId = @Id",
                new SqlParameter("@Hash", tokenHash),
                new SqlParameter("@Expiry", expiry),
                new SqlParameter("@Id", taiKhoanId));

            return $"{taiKhoanId}|{randomToken}";
        }

        // Xác thực token từ cookie, nếu hợp lệ thì trả về AuthenticatedUser
        public AuthenticatedUser ValidatePersistentToken(string cookieValue)
        {
            if (string.IsNullOrWhiteSpace(cookieValue)) return null;

            var parts = cookieValue.Split('|');
            if (parts.Length != 2) return null;

            if (!int.TryParse(parts[0], out int taiKhoanId)) return null;
            var randomToken = parts[1];

            const string sql = @"
SELECT TOP 1
    tk.TaiKhoanId, tk.TenDangNhap, tk.LoaiTaiKhoan, tk.NhanVienId, tk.KhoaPhongId, 
    tk.DangHoatDong AS TaiKhoanDangHoatDong, tk.FailedLoginCount, tk.LockoutUntil, 
    tk.RememberTokenHash, tk.RememberTokenExpiry,
    nv.DangHoatDong AS NhanVienDangHoatDong, kp.TenKhoaPhong, kp.Used AS KhoaPhongUsed
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
                    if (!reader.Read()) return null;

                    var expiry = ReadNullableDateTime(reader, "RememberTokenExpiry");
                    if (!expiry.HasValue || expiry.Value <= DateTime.Now) return null;

                    var storedHash = ReadNullableString(reader, "RememberTokenHash");
                    if (string.IsNullOrEmpty(storedHash) || !PasswordHasher.Verify(randomToken, storedHash)) return null;

                    var lockoutUntil = ReadNullableDateTime(reader, "LockoutUntil");
                    var user = MapAuthenticatedUser(reader, lockoutUntil.HasValue && lockoutUntil.Value > DateTime.Now);
                    
                    if (user.IsLocked) return null;

                    return user;
                }
            }
        }

        // Tự động khôi phục session từ cookie hoặc trả về user hiện tại
        public AuthenticatedUser TryAutoLogin(HttpRequestBase request, HttpSessionStateBase session)
        {
            if (SessionUserAccessor.IsAuthenticated(session))
            {
                var taiKhoanId = SessionUserAccessor.GetInt(session, SessionUserAccessor.TaiKhoanIdKey).Value;
                var user = GetAuthenticatedUser(taiKhoanId);
                if (user != null && !user.IsLocked) return user;
            }

            var cookie = request.Cookies["HQD_AuthToken"];
            if (cookie != null && !string.IsNullOrWhiteSpace(cookie.Value))
            {
                var authUser = ValidatePersistentToken(cookie.Value);
                if (authUser != null)
                {
                    SessionUserAccessor.SetLoginSession(session, authUser);
                    UpdateLastLogin(authUser.TaiKhoanId);
                    return authUser;
                }
            }

            return null;
        }

        // Hủy token khi người dùng đăng xuất
        public void RevokeToken(int taiKhoanId)
        {
            ExecuteNonQuery(
                "UPDATE dbo.TaiKhoan SET RememberTokenHash = NULL, RememberTokenExpiry = NULL, NgayCapNhat = GETDATE() WHERE TaiKhoanId = @TaiKhoanId",
                new SqlParameter("@TaiKhoanId", taiKhoanId));
        }

        // Chuyển dữ liệu tài khoản, role, khóa tạm và khoa/phòng thành ngữ cảnh đăng nhập dùng trong session.
        private static AuthenticatedUser MapAuthenticatedUser(SqlDataReader reader, bool isTemporarilyLocked)
        {
            var isDepartmentLocked = IsDepartmentLocked(reader);
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
                IsDepartmentLocked = isDepartmentLocked,
                IsLocked = isTemporarilyLocked || !reader.GetBoolean(reader.GetOrdinal("TaiKhoanDangHoatDong")) || IsEmployeeLocked(reader) || isDepartmentLocked
            };
        }
    }
}
