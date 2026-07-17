// Mục đích: chuẩn hóa các key và thao tác đọc/ghi session đăng nhập.
using System;
using System.Web;
using System.Web.SessionState;

namespace HospitalQualityDashboardDemo.Services
{
    public static class SessionUserAccessor
    {
        public const string TaiKhoanIdKey = "TaiKhoanId";
        public const string TenDangNhapKey = "TenDangNhap";
        public const string RoleNameKey = "RoleName";
        public const string NhanVienIdKey = "NhanVienId";
        public const string KhoaPhongIdKey = "KhoaPhongId";
        public const string TenKhoaPhongKey = "TenKhoaPhong";
        public const string LastSessionRevalidatedUtcKey = "LastSessionRevalidatedUtc";

        // Xác định session đã có tài khoản đăng nhập hay chưa trước khi controller kiểm tra role/phạm vi.
        public static bool IsAuthenticated(HttpSessionStateBase session)
        {
            return session != null && session[TaiKhoanIdKey] != null;
        }

        // Đọc giá trị số từ session và trả null khi session/key không tồn tại để controller tự quyết định điều hướng.
        public static int? GetInt(HttpSessionStateBase session, string key)
        {
            if (session == null || session[key] == null)
            {
                return null;
            }

            return Convert.ToInt32(session[key]);
        }

        // Đọc chuỗi session như tên đăng nhập hoặc tên khoa/phòng, không ép kiểu khi dữ liệu vắng mặt.
        public static string GetString(HttpSessionStateBase session, string key)
        {
            return session == null ? null : session[key] as string;
        }

        // Lấy tên vai trò (RoleName) từ session để các controller kiểm tra quyền nhất quán.
        public static string GetRoleName(HttpSessionStateBase session)
        {
            if (session == null || session[RoleNameKey] == null)
            {
                return null;
            }

            return session[RoleNameKey] as string;
        }

        // Đọc mốc thời gian session như lần revalidate gần nhất, trả null nếu chưa từng ghi.
        public static DateTime? GetDateTime(HttpSessionStateBase session, string key)
        {
            if (session == null || session[key] == null)
            {
                return null;
            }

            return Convert.ToDateTime(session[key]);
        }

        // Ghi thông tin đăng nhập tối thiểu vào session để các controller kiểm tra quyền nhanh.
        public static void SetLoginSession(HttpSessionStateBase session, AuthenticatedUser user)
        {
            if (session == null)
            {
                throw new ArgumentNullException("session");
            }

            session[TaiKhoanIdKey] = user.TaiKhoanId;
            session[TenDangNhapKey] = user.TenDangNhap;
            session[RoleNameKey] = user.RoleName;
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

        // Ghi thông tin đăng nhập tối thiểu vào session để các controller kiểm tra quyền nhanh.
        public static void SetLoginSession(HttpSessionState session, AuthenticatedUser user)
        {
            if (session == null)
            {
                throw new ArgumentNullException("session");
            }

            session[TaiKhoanIdKey] = user.TaiKhoanId;
            session[TenDangNhapKey] = user.TenDangNhap;
            session[RoleNameKey] = user.RoleName;
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
