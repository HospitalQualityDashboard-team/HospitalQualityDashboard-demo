// Mục đích: quản lý thông báo, người nhận và trạng thái đọc/xử lý của từng tài khoản.
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.Enums;
using HospitalQualityDashboardDemo.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Data.SqlClient;
using System.Linq;

namespace HospitalQualityDashboardDemo.Services
{
    public class NotificationService : DbServiceBase
    {
        // Đếm thông báo chưa đọc của tài khoản người dùng hiện tại.
        public int CountUnreadForUser(int accountId)
        {
            return Convert.ToInt32(Scalar(@"
SELECT COUNT(*) FROM dbo.ThongBaoNguoiNhan
WHERE TaiKhoanId=@TaiKhoanId AND DaDoc=0",
                Param("@TaiKhoanId", accountId)));
        }

        // Lấy thông báo dành cho tài khoản/khoa phòng hiện tại, có phân trang để tránh tải quá nhiều dữ liệu.
        public IList<NotificationViewModel> GetForUser(int accountId, bool admin)
        {
            int totalItems;
            return GetForUser(accountId, admin, 1, int.MaxValue, out totalItems);
        }

        // Lấy thông báo dành cho tài khoản/khoa phòng hiện tại, có phân trang để tránh tải quá nhiều dữ liệu.
        public IList<NotificationViewModel> GetForUser(int accountId, bool admin, int page, int pageSize, out int totalItems)
        {
            page = NormalizePage(page);
            pageSize = NormalizePageSize(pageSize);
            const string countSql = @"
SELECT COUNT(*)
FROM dbo.ThongBao tb
LEFT JOIN dbo.ThongBaoNguoiNhan tbn ON tbn.ThongBaoId = tb.ThongBaoId AND tbn.TaiKhoanId = @TaiKhoanId
WHERE @IsAdmin = 1 OR tbn.TaiKhoanId = @TaiKhoanId";

            totalItems = Convert.ToInt32(Scalar(countSql, Param("@TaiKhoanId", accountId), Param("@IsAdmin", admin)));

            const string sql = @"
SELECT tb.ThongBaoId, tb.TieuDe, tb.NoiDung, tb.LoaiThongBao, tb.KyBaoCaoId, tb.ChiSoChatLuongId, tb.BaoCaoId, tb.NgayTao, ISNULL(tbn.DaDoc, 0) AS DaDoc
FROM dbo.ThongBao tb
LEFT JOIN dbo.ThongBaoNguoiNhan tbn ON tbn.ThongBaoId = tb.ThongBaoId AND tbn.TaiKhoanId = @TaiKhoanId
WHERE @IsAdmin = 1 OR tbn.TaiKhoanId = @TaiKhoanId
ORDER BY tb.NgayTao DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            return Query(sql, MapNotification,
                Param("@TaiKhoanId", accountId),
                Param("@IsAdmin", admin),
                Param("@Offset", (page - 1) * pageSize),
                Param("@PageSize", pageSize));
        }

        // Lấy chi tiết thông báo trong phạm vi người nhận hợp lệ, tránh đọc thông báo của tài khoản khác.
        public NotificationViewModel GetDetailForUser(int notificationId, int accountId, bool admin)
        {
            const string sql = @"
SELECT tb.ThongBaoId, tb.TieuDe, tb.NoiDung, tb.LoaiThongBao, tb.KyBaoCaoId, tb.ChiSoChatLuongId, tb.BaoCaoId, tb.NgayTao, ISNULL(tbn.DaDoc, 0) AS DaDoc
FROM dbo.ThongBao tb
LEFT JOIN dbo.ThongBaoNguoiNhan tbn ON tbn.ThongBaoId = tb.ThongBaoId AND tbn.TaiKhoanId = @TaiKhoanId
WHERE tb.ThongBaoId = @ThongBaoId
  AND (@IsAdmin = 1 OR tbn.TaiKhoanId = @TaiKhoanId)";
            return QuerySingle(sql, MapNotification,
                Param("@ThongBaoId", notificationId),
                Param("@TaiKhoanId", accountId),
                Param("@IsAdmin", admin));
        }

        // Gửi dữ liệu và cập nhật trạng thái tương ứng của thông báo.
        public void SendManual(NotificationSendDto dto, int userId)
        {
            SendManual(new NotificationViewModel
            {
                TieuDe = dto.TieuDe,
                NoiDung = dto.NoiDung,
                LoaiThongBao = dto.LoaiThongBao,
                KyBaoCaoId = dto.KyBaoCaoId,
                ChiSoChatLuongId = dto.ChiSoChatLuongId,
                BaoCaoId = dto.BaoCaoId,
                SelectedKhoaPhongIds = dto.SelectedKhoaPhongIds
            }, userId);
        }

        // Gửi dữ liệu và cập nhật trạng thái tương ứng của thông báo.
        public void SendManual(NotificationViewModel model, int userId)
        {
            var notificationId = Convert.ToInt32(Scalar(@"INSERT INTO dbo.ThongBao(TieuDe, NoiDung, LoaiThongBao, KyBaoCaoId, ChiSoChatLuongId, BaoCaoId, NguoiTaoId)
OUTPUT INSERTED.ThongBaoId VALUES(@TieuDe, @NoiDung, @LoaiThongBao, @KyBaoCaoId, @ChiSoChatLuongId, @BaoCaoId, @NguoiTaoId)",
                Param("@TieuDe", model.TieuDe),
                Param("@NoiDung", model.NoiDung),
                Param("@LoaiThongBao", (byte)LoaiThongBao.ThuCong),
                Param("@KyBaoCaoId", model.KyBaoCaoId),
                Param("@ChiSoChatLuongId", model.ChiSoChatLuongId),
                Param("@BaoCaoId", model.BaoCaoId),
                Param("@NguoiTaoId", userId)));

            foreach (var departmentId in model.SelectedKhoaPhongIds ?? new int[0])
            {
                Execute(@"INSERT INTO dbo.ThongBaoNguoiNhan(ThongBaoId, TaiKhoanId)
SELECT @ThongBaoId, TaiKhoanId FROM dbo.TaiKhoan WHERE KhoaPhongId=@KhoaPhongId AND LoaiTaiKhoan=@UserType AND DangHoatDong=1",
                    Param("@ThongBaoId", notificationId),
                    Param("@KhoaPhongId", departmentId),
                    Param("@UserType", (byte)LoaiTaiKhoan.User));
            }
        }

        // Đánh dấu thông báo đã đọc cho người nhận hiện tại, không làm thay đổi nội dung thông báo gốc.
        public void MarkAsRead(int notificationId, int accountId)
        {
            Execute("UPDATE dbo.ThongBaoNguoiNhan SET DaDoc=1, NgayDoc=GETDATE() WHERE ThongBaoId=@ThongBaoId AND TaiKhoanId=@TaiKhoanId",
                Param("@ThongBaoId", notificationId), Param("@TaiKhoanId", accountId));
        }

        // Chuyển dữ liệu thông báo và trạng thái đọc của người nhận sang view model hiển thị.
        private static NotificationViewModel MapNotification(SqlDataReader r)
        {
            return new NotificationViewModel
            {
                ThongBaoId = Int(r, "ThongBaoId"),
                TieuDe = String(r, "TieuDe"),
                NoiDung = String(r, "NoiDung"),
                LoaiThongBao = (LoaiThongBao)r.GetByte(r.GetOrdinal("LoaiThongBao")),
                KyBaoCaoId = NullableInt(r, "KyBaoCaoId"),
                ChiSoChatLuongId = NullableInt(r, "ChiSoChatLuongId"),
                BaoCaoId = NullableInt(r, "BaoCaoId"),
                NgayTao = r.GetDateTime(r.GetOrdinal("NgayTao")),
                DaDoc = r.GetBoolean(r.GetOrdinal("DaDoc"))
            };
        }

        // Chuẩn hóa số trang để tránh page âm/0 làm sai truy vấn phân trang.
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
    }
}
