using System;
using System.Web;
using System.Web.SessionState;
using HospitalQualityDashboard.Models.Enums;

namespace HospitalQualityDashboard.Services
{
    public static class SessionUserAccessor
    {
        public const string TaiKhoanIdKey = "TaiKhoanId";
        public const string TenDangNhapKey = "TenDangNhap";
        public const string LoaiTaiKhoanKey = "LoaiTaiKhoan";
        public const string NhanVienIdKey = "NhanVienId";
        public const string KhoaPhongIdKey = "KhoaPhongId";
        public const string TenKhoaPhongKey = "TenKhoaPhong";

        public static bool IsAuthenticated(HttpSessionStateBase session)
        {
            return session != null && session[TaiKhoanIdKey] != null;
        }

        public static int? GetInt(HttpSessionStateBase session, string key)
        {
            if (session == null || session[key] == null)
            {
                return null;
            }

            return Convert.ToInt32(session[key]);
        }

        public static string GetString(HttpSessionStateBase session, string key)
        {
            return session == null ? null : session[key] as string;
        }

        public static LoaiTaiKhoan? GetLoaiTaiKhoan(HttpSessionStateBase session)
        {
            if (session == null || session[LoaiTaiKhoanKey] == null)
            {
                return null;
            }

            return (LoaiTaiKhoan)session[LoaiTaiKhoanKey];
        }

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
        }

        public static void ClearLoginSession(HttpSessionStateBase session)
        {
            if (session == null)
            {
                return;
            }

            session.Clear();
            session.Abandon();
        }

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
        }

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
