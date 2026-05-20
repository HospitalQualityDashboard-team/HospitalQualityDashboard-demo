using System;
using System.Configuration;
using System.Data.SqlClient;
using HospitalQualityDashboard.Models.Enums;

namespace HospitalQualityDashboard.Services
{
    public class AuthenticatedUser
    {
        public int TaiKhoanId { get; set; }
        public string TenDangNhap { get; set; }
        public LoaiTaiKhoan LoaiTaiKhoan { get; set; }
        public int? NhanVienId { get; set; }
        public int? KhoaPhongId { get; set; }
        public string TenKhoaPhong { get; set; }
    }

    public class AuthService
    {
        private readonly string _connectionString;

        public AuthService()
            : this(ConfigurationManager.ConnectionStrings["HospitalQualityConnection"].ConnectionString)
        {
        }

        public AuthService(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string is required.", "connectionString");
            }

            _connectionString = connectionString;
        }

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
    kp.TenKhoaPhong
FROM dbo.TaiKhoan tk
LEFT JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = tk.KhoaPhongId
WHERE tk.TenDangNhap = @TenDangNhap AND tk.DangHoatDong = 1";

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

                    var storedHash = reader.GetString(reader.GetOrdinal("MatKhauHash"));
                    if (!PasswordHasher.Verify(password, storedHash))
                    {
                        return null;
                    }

                    return new AuthenticatedUser
                    {
                        TaiKhoanId = reader.GetInt32(reader.GetOrdinal("TaiKhoanId")),
                        TenDangNhap = reader.GetString(reader.GetOrdinal("TenDangNhap")),
                        LoaiTaiKhoan = (LoaiTaiKhoan)reader.GetByte(reader.GetOrdinal("LoaiTaiKhoan")),
                        NhanVienId = ReadNullableInt(reader, "NhanVienId"),
                        KhoaPhongId = ReadNullableInt(reader, "KhoaPhongId"),
                        TenKhoaPhong = ReadNullableString(reader, "TenKhoaPhong")
                    };
                }
            }
        }

        public void UpdateLastLogin(int taiKhoanId)
        {
            ExecuteNonQuery(
                "UPDATE dbo.TaiKhoan SET LanDangNhapCuoi = GETDATE(), NgayCapNhat = GETDATE() WHERE TaiKhoanId = @TaiKhoanId",
                new SqlParameter("@TaiKhoanId", taiKhoanId));
        }

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

        private static int? ReadNullableInt(SqlDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (int?)null : reader.GetInt32(ordinal);
        }

        private static string ReadNullableString(SqlDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }
    }
}
