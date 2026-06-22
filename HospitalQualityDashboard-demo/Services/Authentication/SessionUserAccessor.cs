// Mục đích: chuẩn hóa các key và thao tác đọc/ghi session đăng nhập.
using HospitalQualityDashboardDemo.Models.Enums;
using System;
using System.Web;
using System.Web.SessionState;

namespace HospitalQualityDashboardDemo.Services
{
    public static class SessionUserAccessor
    {
        public const string TaiKhoanIdKey = "TaiKhoanId";
        public const string TenDangNhapKey = "TenDangNhap";
        public const string LoaiTaiKhoanKey = "LoaiTaiKhoan";
        public const string NhanVienIdKey = "NhanVienId";
        public const string KhoaPhongIdKey = "KhoaPhongId";
        public const string TenKhoaPhongKey = "TenKhoaPhong";
        public const string LastSessionRevalidatedUtcKey = "LastSessionRevalidatedUtc";

        // Xác định dữ liệu có thỏa điều kiện nghiệp vụ của người dùng trong session hay không.
        public static bool IsAuthenticated(HttpSessionStateBase session)
        {
            return session != null && session[TaiKhoanIdKey] != null;
        }

        // Truy vấn người dùng trong session theo điều kiện được cung cấp.
        public static int? GetInt(HttpSessionStateBase session, string key)
        {
            if (session == null || session[key] == null)
            {
                return null;
            }

            return Convert.ToInt32(session[key]);
        }

        // Truy vấn người dùng trong session theo điều kiện được cung cấp.
        public static string GetString(HttpSessionStateBase session, string key)
        {
            return session == null ? null : session[key] as string;
        }

        // Truy vấn người dùng trong session theo điều kiện được cung cấp.
        public static LoaiTaiKhoan? GetLoaiTaiKhoan(HttpSessionStateBase session)
        {
            if (session == null || session[LoaiTaiKhoanKey] == null)
            {
                return null;
            }

            return (LoaiTaiKhoan)session[LoaiTaiKhoanKey];
        }

        // Truy vấn người dùng trong session theo điều kiện được cung cấp.
        public static DateTime? GetDateTime(HttpSessionStateBase session, string key)
        {
            if (session == null || session[key] == null)
            {
                return null;
            }

            return Convert.ToDateTime(session[key]);
        }

        // Kiểm tra và cập nhật dữ liệu của người dùng trong session.
        public static void SetLoginSession(HttpSessionStateBase session, AuthenticatedUser user)
        {
            if (session == null)
            {
                throw new ArgumentNullException("session");
            }

            session[TaiKhoanIdKey] = user.TaiKhoanId;
            session[TenDangNhapKey] = user.TenDangNhap;
            session[LoaiTaiKhoanKey] = user.LoaiTaiKhoan;
            session[NhanVienIdKey] = user.NhanVienId;
            session[KhoaPhongIdKey] = user.KhoaPhongId;
            session[TenKhoaPhongKey] = user.TenKhoaPhong;
            session[LastSessionRevalidatedUtcKey] = DateTime.UtcNow;
        }

        // Xóa trạng thái tạm để chuẩn bị lượt xử lý mới của người dùng trong session.
        public static void ClearLoginSession(HttpSessionStateBase session)
        {
            if (session == null)
            {
                return;
            }

            session.Clear();
            session.Abandon();
        }

        // Kiểm tra và cập nhật dữ liệu của người dùng trong session.
        public static void SetLoginSession(HttpSessionState session, AuthenticatedUser user)
        {
            if (session == null)
            {
                throw new ArgumentNullException("session");
            }

            session[TaiKhoanIdKey] = user.TaiKhoanId;
            session[TenDangNhapKey] = user.TenDangNhap;
            session[LoaiTaiKhoanKey] = user.LoaiTaiKhoan;
            session[NhanVienIdKey] = user.NhanVienId;
            session[KhoaPhongIdKey] = user.KhoaPhongId;
            session[TenKhoaPhongKey] = user.TenKhoaPhong;
            session[LastSessionRevalidatedUtcKey] = DateTime.UtcNow;
        }

        // Xóa trạng thái tạm để chuẩn bị lượt xử lý mới của người dùng trong session.
        public static void ClearLoginSession(HttpSessionState session)
        {
            if (session == null)
            {
                return;
            }

            session.Clear();
            session.Abandon();
        }
    }
}
