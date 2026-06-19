// Mục đích: xử lý thông báo và các chức năng export dữ liệu.
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

        // Truy vấn thông báo theo điều kiện được cung cấp.
        public IList<NotificationViewModel> GetForUser(int accountId, bool admin)
        {
            int totalItems;
            return GetForUser(accountId, admin, 1, int.MaxValue, out totalItems);
        }

        // Truy vấn thông báo theo điều kiện được cung cấp.
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

        // Truy vấn thông báo theo điều kiện được cung cấp.
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

        // Đánh dấu trạng thái xử lý tương ứng trong thông báo.
        public void MarkAsRead(int notificationId, int accountId)
        {
            Execute("UPDATE dbo.ThongBaoNguoiNhan SET DaDoc=1, NgayDoc=GETDATE() WHERE ThongBaoId=@ThongBaoId AND TaiKhoanId=@TaiKhoanId",
                Param("@ThongBaoId", notificationId), Param("@TaiKhoanId", accountId));
        }

        // Chuyển dữ liệu nguồn sang cấu trúc dùng cho thông báo.
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

        // Chuẩn hóa dữ liệu đầu vào trước khi dùng cho thông báo.
        private static int NormalizePage(int page)
        {
            return page < 1 ? 1 : page;
        }

        // Chuẩn hóa dữ liệu đầu vào trước khi dùng cho thông báo.
        private static int NormalizePageSize(int pageSize)
        {
            if (pageSize < 1) return 20;
            return pageSize > 100 ? 100 : pageSize;
        }
    }

    public class NotificationAutomationService : DbServiceBase
    {
        private readonly IndicatorService _indicators = new IndicatorService();

        // Thực thi quy trình xử lý của thông báo tự động.
        public void Run(DateTime now)
        {
            EnsureAutomationLogTable();

            SendPeriodOpenedNotifications(now);
            SendDueReminderNotifications(now);
            SendOverdueNotifications(now);
            SendAdminDailySummary(now);
        }

        // Gửi dữ liệu và cập nhật trạng thái tương ứng của thông báo tự động.
        public void SendPeriodOpenedNotifications(DateTime now)
        {
            const string sql = @"
SELECT DISTINCT ky.KyBaoCaoId, ky.TenKyBaoCao, ky.HanNop, kp.KhoaPhongId, kp.TenKhoaPhong
FROM dbo.KyBaoCao ky
INNER JOIN dbo.PhanCongChiSo pc ON pc.DangHoatDong = 1
INNER JOIN dbo.ChiSoTanSuatBaoCao cst ON cst.ChiSoChatLuongId = pc.ChiSoChatLuongId AND cst.TanSuatBaoCao = ky.LoaiKyBaoCao
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = pc.KhoaPhongId
WHERE ky.TrangThai = @Mo";

            var rows = Query(sql, MapAutomationRow, Param("@Mo", (byte)TrangThaiKyBaoCao.Mo));
            foreach (var row in rows)
            {
                var key = string.Format("period-opened:{0}:{1}", row.KyBaoCaoId, row.KhoaPhongId);
                var title = "Kỳ báo cáo đã mở";
                var body = string.Format("Kỳ báo cáo \"{0}\" đã mở. Khoa/phòng vui lòng nhập và gửi số liệu trước 23:59 ngày {1:dd/MM/yyyy}.", row.TenKyBaoCao, row.HanNop);
                var notificationId = CreateAutoNotification(key, LoaiThongBao.KyBaoCaoMo, title, body, row.KyBaoCaoId, row.KhoaPhongId, row.HanNop.Date);
                AddDepartmentRecipients(notificationId, row.KhoaPhongId);
            }
        }

        // Gửi dữ liệu và cập nhật trạng thái tương ứng của thông báo tự động.
        public void SendDueReminderNotifications(DateTime now)
        {
            foreach (var daysBeforeDue in new[] { 10, 7, 3, 1, 0 })
            {
                const string sql = @"
SELECT ky.KyBaoCaoId, ky.TenKyBaoCao, ky.HanNop, kp.KhoaPhongId, kp.TenKhoaPhong,
       COUNT(pc.PhanCongChiSoId) - COUNT(bc.BaoCaoId) AS MissingCount
FROM dbo.KyBaoCao ky
INNER JOIN dbo.PhanCongChiSo pc ON pc.DangHoatDong = 1
INNER JOIN dbo.ChiSoTanSuatBaoCao cst ON cst.ChiSoChatLuongId = pc.ChiSoChatLuongId AND cst.TanSuatBaoCao = ky.LoaiKyBaoCao
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = pc.KhoaPhongId
LEFT JOIN dbo.BaoCao bc ON bc.KyBaoCaoId = ky.KyBaoCaoId
    AND bc.KhoaPhongId = pc.KhoaPhongId
    AND bc.ChiSoChatLuongId = pc.ChiSoChatLuongId
    AND bc.TrangThai IN (@DaGui, @QuaHan, @DaKhoa, @DaDuyet)
WHERE ky.TrangThai = @Mo
  AND DATEDIFF(day, CAST(@Now AS date), ky.HanNop) = @DaysBeforeDue
GROUP BY ky.KyBaoCaoId, ky.TenKyBaoCao, ky.HanNop, kp.KhoaPhongId, kp.TenKhoaPhong
HAVING COUNT(pc.PhanCongChiSoId) - COUNT(bc.BaoCaoId) > 0";

                var rows = Query(sql, MapAutomationRow,
                    Param("@Mo", (byte)TrangThaiKyBaoCao.Mo),
                    Param("@DaGui", (byte)TrangThaiBaoCao.DaGui),
                    Param("@QuaHan", (byte)TrangThaiBaoCao.QuaHan),
                    Param("@DaKhoa", (byte)TrangThaiBaoCao.DaKhoa),
                    Param("@DaDuyet", (byte)TrangThaiBaoCao.DaDuyet),
                    Param("@Now", now),
                    Param("@DaysBeforeDue", daysBeforeDue));

                foreach (var row in rows)
                {
                    var key = string.Format("due:{0}:{1}:{2}", row.KyBaoCaoId, row.KhoaPhongId, daysBeforeDue);
                    var title = daysBeforeDue == 0 ? "Hôm nay là hạn nộp báo cáo" : string.Format("Còn {0} ngày đến hạn nộp báo cáo", daysBeforeDue);
                    var body = string.Format("Kỳ báo cáo \"{0}\" còn {1} chỉ số chưa gửi. Hạn nộp: 23:59 ngày {2:dd/MM/yyyy}.", row.TenKyBaoCao, row.MissingCount, row.HanNop);
                    var notificationId = CreateAutoNotification(key, LoaiThongBao.NhacHan, title, body, row.KyBaoCaoId, row.KhoaPhongId, row.HanNop.Date);
                    AddDepartmentRecipients(notificationId, row.KhoaPhongId);
                }
            }
        }

        public IndicatorWarningResult SendIndicatorWarning(
            int periodId,
            int departmentId,
            int indicatorId,
            int adminUserId,
            DateTime now)
        {
            EnsureAutomationLogTable();

            const string sql = @"
SELECT TOP 1 ky.KyBaoCaoId, ky.TenKyBaoCao, ky.HanNop,
       kp.KhoaPhongId, kp.TenKhoaPhong,
       cs.ChiSoChatLuongId, cs.MaChiSo, cs.TenChiSo,
       CASE WHEN bc.TrangThai IN (@DaGui, @QuaHan, @DaKhoa, @DaDuyet) THEN 1 ELSE 0 END AS HasSubmitted
FROM dbo.KyBaoCao ky
INNER JOIN dbo.PhanCongChiSo pc
    ON pc.KhoaPhongId = @KhoaPhongId
   AND pc.ChiSoChatLuongId = @ChiSoChatLuongId
   AND pc.DangHoatDong = 1
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = pc.KhoaPhongId
INNER JOIN dbo.ChiSoChatLuong cs
    ON cs.ChiSoChatLuongId = pc.ChiSoChatLuongId
   AND cs.DangHoatDong = 1
INNER JOIN dbo.ChiSoTanSuatBaoCao cst
    ON cst.ChiSoChatLuongId = cs.ChiSoChatLuongId
   AND cst.TanSuatBaoCao = ky.LoaiKyBaoCao
LEFT JOIN dbo.BaoCao bc
    ON bc.KyBaoCaoId = ky.KyBaoCaoId
   AND bc.KhoaPhongId = pc.KhoaPhongId
   AND bc.ChiSoChatLuongId = pc.ChiSoChatLuongId
WHERE ky.KyBaoCaoId = @KyBaoCaoId
  AND ky.TrangThai = @Mo";

            var row = QuerySingle(sql, reader => new IndicatorWarningRow
            {
                KyBaoCaoId = Int(reader, "KyBaoCaoId"),
                TenKyBaoCao = String(reader, "TenKyBaoCao"),
                HanNop = reader.GetDateTime(reader.GetOrdinal("HanNop")),
                KhoaPhongId = Int(reader, "KhoaPhongId"),
                TenKhoaPhong = String(reader, "TenKhoaPhong"),
                ChiSoChatLuongId = Int(reader, "ChiSoChatLuongId"),
                MaChiSo = String(reader, "MaChiSo"),
                TenChiSo = String(reader, "TenChiSo"),
                HasSubmitted = Int(reader, "HasSubmitted") == 1
            },
                Param("@KyBaoCaoId", periodId),
                Param("@KhoaPhongId", departmentId),
                Param("@ChiSoChatLuongId", indicatorId),
                Param("@Mo", (byte)TrangThaiKyBaoCao.Mo),
                Param("@DaGui", (byte)TrangThaiBaoCao.DaGui),
                Param("@QuaHan", (byte)TrangThaiBaoCao.QuaHan),
                Param("@DaKhoa", (byte)TrangThaiBaoCao.DaKhoa),
                Param("@DaDuyet", (byte)TrangThaiBaoCao.DaDuyet));

            if (row == null)
            {
                return IndicatorWarningResult.NotEligible;
            }

            if (row.HasSubmitted)
            {
                return IndicatorWarningResult.AlreadySubmitted;
            }

            var today = now.Date;
            var key = string.Format(
                "indicator-warning:{0}:{1}:{2}:{3:yyyyMMdd}",
                row.KyBaoCaoId,
                row.KhoaPhongId,
                row.ChiSoChatLuongId,
                today);
            var warning = IndicatorWarningMessageBuilder.Build(
                row.MaChiSo,
                row.TenChiSo,
                row.HanNop,
                now);
            var notificationId = CreateAutoNotification(
                key,
                warning.NotificationType,
                warning.Title,
                warning.Body,
                row.KyBaoCaoId,
                row.KhoaPhongId,
                today,
                row.ChiSoChatLuongId,
                adminUserId);

            if (notificationId <= 0)
            {
                return IndicatorWarningResult.AlreadySentToday;
            }

            AddDepartmentRecipients(notificationId, row.KhoaPhongId);
            return IndicatorWarningResult.Sent;
        }

        // Gửi dữ liệu và cập nhật trạng thái tương ứng của thông báo tự động.
        public void SendOverdueNotifications(DateTime now)
        {
            const string sql = @"
SELECT ky.KyBaoCaoId, ky.TenKyBaoCao, ky.HanNop, kp.KhoaPhongId, kp.TenKhoaPhong,
       COUNT(pc.PhanCongChiSoId) - COUNT(bc.BaoCaoId) AS MissingCount
FROM dbo.KyBaoCao ky
INNER JOIN dbo.PhanCongChiSo pc ON pc.DangHoatDong = 1
INNER JOIN dbo.ChiSoTanSuatBaoCao cst ON cst.ChiSoChatLuongId = pc.ChiSoChatLuongId AND cst.TanSuatBaoCao = ky.LoaiKyBaoCao
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = pc.KhoaPhongId
LEFT JOIN dbo.BaoCao bc ON bc.KyBaoCaoId = ky.KyBaoCaoId
    AND bc.KhoaPhongId = pc.KhoaPhongId
    AND bc.ChiSoChatLuongId = pc.ChiSoChatLuongId
    AND bc.TrangThai IN (@DaGui, @QuaHan, @DaKhoa, @DaDuyet)
WHERE ky.TrangThai = @Mo
  AND CAST(@Now AS date) > ky.HanNop
GROUP BY ky.KyBaoCaoId, ky.TenKyBaoCao, ky.HanNop, kp.KhoaPhongId, kp.TenKhoaPhong
HAVING COUNT(pc.PhanCongChiSoId) - COUNT(bc.BaoCaoId) > 0";

            var rows = Query(sql, MapAutomationRow,
                Param("@Mo", (byte)TrangThaiKyBaoCao.Mo),
                Param("@DaGui", (byte)TrangThaiBaoCao.DaGui),
                Param("@QuaHan", (byte)TrangThaiBaoCao.QuaHan),
                Param("@DaKhoa", (byte)TrangThaiBaoCao.DaKhoa),
                Param("@DaDuyet", (byte)TrangThaiBaoCao.DaDuyet),
                Param("@Now", now));

            foreach (var row in rows)
            {
                var key = string.Format("overdue:{0}:{1}", row.KyBaoCaoId, row.KhoaPhongId);
                var title = "Báo cáo đã quá hạn";
                var body = string.Format("Kỳ báo cáo \"{0}\" đã quá hạn từ ngày {1:dd/MM/yyyy}. Khoa/phòng còn {2} chỉ số chưa gửi.", row.TenKyBaoCao, row.HanNop, row.MissingCount);
                var notificationId = CreateAutoNotification(key, LoaiThongBao.QuaHan, title, body, row.KyBaoCaoId, row.KhoaPhongId, row.HanNop.Date);
                AddDepartmentRecipients(notificationId, row.KhoaPhongId);
            }
        }

        // Gửi dữ liệu và cập nhật trạng thái tương ứng của thông báo tự động.
        public void SendAdminDailySummary(DateTime now)
        {
            var summary = QuerySingle(@"
SELECT
    (SELECT COUNT(*) FROM dbo.BaoCao WHERE TrangThai IN (2,3,4,5)) AS SubmittedCount,
    (SELECT COUNT(*) FROM dbo.BaoCao WHERE TrangThai = 3) AS LateCount,
    (SELECT COUNT(*)
     FROM dbo.KyBaoCao ky
     INNER JOIN dbo.PhanCongChiSo pc ON pc.DangHoatDong = 1
     INNER JOIN dbo.ChiSoTanSuatBaoCao cst ON cst.ChiSoChatLuongId = pc.ChiSoChatLuongId AND cst.TanSuatBaoCao = ky.LoaiKyBaoCao
     LEFT JOIN dbo.BaoCao bc ON bc.KyBaoCaoId = ky.KyBaoCaoId
        AND bc.KhoaPhongId = pc.KhoaPhongId
        AND bc.ChiSoChatLuongId = pc.ChiSoChatLuongId
        AND bc.TrangThai IN (2,3,4,5)
     WHERE ky.TrangThai = 2 AND bc.BaoCaoId IS NULL) AS MissingCount",
                r => new AdminSummary
                {
                    SubmittedCount = Int(r, "SubmittedCount"),
                    LateCount = Int(r, "LateCount"),
                    MissingCount = Int(r, "MissingCount")
                });

            if (summary == null)
            {
                return;
            }

            var key = string.Format("admin-summary:{0:yyyyMMdd}", now.Date);
            var title = "Tổng hợp tiến độ báo cáo trong ngày";
            var body = string.Format("Đã nộp: {0}. Nộp trễ: {1}. Còn thiếu: {2}.", summary.SubmittedCount, summary.LateCount, summary.MissingCount);
            var notificationId = CreateAutoNotification(key, LoaiThongBao.TongHopAdmin, title, body, null, null, now.Date);
            AddAdminRecipients(notificationId);
        }

        // Kiểm tra các điều kiện hợp lệ trước khi tiếp tục xử lý thông báo tự động.
        private void EnsureAutomationLogTable()
        {
            Execute(@"
IF COL_LENGTH('dbo.ThongBao', 'ChiSoChatLuongId') IS NULL
BEGIN
    ALTER TABLE dbo.ThongBao ADD ChiSoChatLuongId INT NULL;
END

IF OBJECT_ID('dbo.ThongBaoTuDongLog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ThongBaoTuDongLog (
        ThongBaoTuDongLogId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ThongBaoTuDongLog PRIMARY KEY,
        DedupKey NVARCHAR(255) NOT NULL,
        LoaiThongBao TINYINT NOT NULL,
        KyBaoCaoId INT NULL,
        KhoaPhongId INT NULL,
        ChiSoChatLuongId INT NULL,
        NgayMoc DATE NULL,
        NgayTao DATETIME NOT NULL CONSTRAINT DF_ThongBaoTuDongLog_NgayTao DEFAULT (GETDATE()),
        CONSTRAINT UQ_ThongBaoTuDongLog_DedupKey UNIQUE (DedupKey),
        CONSTRAINT FK_ThongBaoTuDongLog_KyBaoCao FOREIGN KEY (KyBaoCaoId) REFERENCES dbo.KyBaoCao(KyBaoCaoId),
        CONSTRAINT FK_ThongBaoTuDongLog_KhoaPhong FOREIGN KEY (KhoaPhongId) REFERENCES dbo.KhoaPhong(KhoaPhongId)
    );
END

IF COL_LENGTH('dbo.ThongBaoTuDongLog', 'ChiSoChatLuongId') IS NULL
BEGIN
    ALTER TABLE dbo.ThongBaoTuDongLog ADD ChiSoChatLuongId INT NULL;
END");
        }

        // Tạo cấu trúc dữ liệu phục vụ thông báo tự động.
        private int CreateAutoNotification(
            string dedupKey,
            LoaiThongBao type,
            string title,
            string body,
            int? periodId,
            int? departmentId,
            DateTime? markerDate,
            int? indicatorId = null,
            int? createdByUserId = null)
        {
            var result = Scalar(@"
DECLARE @ThongBaoId INT;
IF NOT EXISTS (SELECT 1 FROM dbo.ThongBaoTuDongLog WHERE DedupKey=@DedupKey)
BEGIN
    INSERT INTO dbo.ThongBao(TieuDe, NoiDung, LoaiThongBao, KyBaoCaoId, ChiSoChatLuongId, NguoiTaoId)
    VALUES(@TieuDe, @NoiDung, @LoaiThongBao, @KyBaoCaoId, @ChiSoChatLuongId, @NguoiTaoId);
    SET @ThongBaoId = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO dbo.ThongBaoTuDongLog(DedupKey, LoaiThongBao, KyBaoCaoId, KhoaPhongId, ChiSoChatLuongId, NgayMoc)
    VALUES(@DedupKey, @LoaiThongBao, @KyBaoCaoId, @KhoaPhongId, @ChiSoChatLuongId, @NgayMoc);
END
SELECT ISNULL(@ThongBaoId, 0);",
                Param("@DedupKey", dedupKey),
                Param("@TieuDe", title),
                Param("@NoiDung", body),
                Param("@LoaiThongBao", (byte)type),
                Param("@KyBaoCaoId", periodId),
                Param("@KhoaPhongId", departmentId),
                Param("@ChiSoChatLuongId", indicatorId),
                Param("@NguoiTaoId", createdByUserId),
                Param("@NgayMoc", markerDate));

            return Convert.ToInt32(result);
        }

        // Bổ sung dữ liệu mới phục vụ thông báo tự động.
        private void AddDepartmentRecipients(int notificationId, int departmentId)
        {
            if (notificationId <= 0)
            {
                return;
            }

            Execute(@"
INSERT INTO dbo.ThongBaoNguoiNhan(ThongBaoId, TaiKhoanId)
SELECT @ThongBaoId, tk.TaiKhoanId
FROM dbo.TaiKhoan tk
WHERE tk.KhoaPhongId=@KhoaPhongId
  AND tk.LoaiTaiKhoan=@UserType
  AND tk.DangHoatDong=1
  AND NOT EXISTS (
      SELECT 1 FROM dbo.ThongBaoNguoiNhan existing
      WHERE existing.ThongBaoId=@ThongBaoId AND existing.TaiKhoanId=tk.TaiKhoanId
  )",
                Param("@ThongBaoId", notificationId),
                Param("@KhoaPhongId", departmentId),
                Param("@UserType", (byte)LoaiTaiKhoan.User));
        }

        // Bổ sung dữ liệu mới phục vụ thông báo tự động.
        private void AddAdminRecipients(int notificationId)
        {
            if (notificationId <= 0)
            {
                return;
            }

            Execute(@"
INSERT INTO dbo.ThongBaoNguoiNhan(ThongBaoId, TaiKhoanId)
SELECT @ThongBaoId, tk.TaiKhoanId
FROM dbo.TaiKhoan tk
WHERE tk.LoaiTaiKhoan=@AdminType
  AND tk.DangHoatDong=1
  AND NOT EXISTS (
      SELECT 1 FROM dbo.ThongBaoNguoiNhan existing
      WHERE existing.ThongBaoId=@ThongBaoId AND existing.TaiKhoanId=tk.TaiKhoanId
  )",
                Param("@ThongBaoId", notificationId),
                Param("@AdminType", (byte)LoaiTaiKhoan.Admin));
        }

        // Chuyển dữ liệu nguồn sang cấu trúc dùng cho thông báo tự động.
        private static AutomationNotificationRow MapAutomationRow(System.Data.SqlClient.SqlDataReader reader)
        {
            return new AutomationNotificationRow
            {
                KyBaoCaoId = Int(reader, "KyBaoCaoId"),
                TenKyBaoCao = String(reader, "TenKyBaoCao"),
                HanNop = reader.GetDateTime(reader.GetOrdinal("HanNop")),
                KhoaPhongId = Int(reader, "KhoaPhongId"),
                TenKhoaPhong = String(reader, "TenKhoaPhong"),
                MissingCount = HasColumn(reader, "MissingCount") && !reader.IsDBNull(reader.GetOrdinal("MissingCount")) ? Int(reader, "MissingCount") : 0
            };
        }

        // Xác định dữ liệu có thỏa điều kiện nghiệp vụ của thông báo tự động hay không.
        private static bool HasColumn(System.Data.SqlClient.SqlDataReader reader, string name)
        {
            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (string.Equals(reader.GetName(i), name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private class AutomationNotificationRow
        {
            public int KyBaoCaoId { get; set; }
            public string TenKyBaoCao { get; set; }
            public DateTime HanNop { get; set; }
            public int KhoaPhongId { get; set; }
            public string TenKhoaPhong { get; set; }
            public int MissingCount { get; set; }
        }

        private class IndicatorWarningRow
        {
            public int KyBaoCaoId { get; set; }
            public string TenKyBaoCao { get; set; }
            public DateTime HanNop { get; set; }
            public int KhoaPhongId { get; set; }
            public string TenKhoaPhong { get; set; }
            public int ChiSoChatLuongId { get; set; }
            public string MaChiSo { get; set; }
            public string TenChiSo { get; set; }
            public bool HasSubmitted { get; set; }
        }

        private class AdminSummary
        {
            public int SubmittedCount { get; set; }
            public int LateCount { get; set; }
            public int MissingCount { get; set; }
        }
    }

    public class ExportService : DbServiceBase
    {
        private readonly DepartmentService _departments = new DepartmentService();
        private readonly EmployeeService _employees = new EmployeeService();
        private readonly IndicatorService _indicators = new IndicatorService();
        private readonly AssignmentService _assignments = new AssignmentService();
        private readonly ReportService _reports = new ReportService();
        private readonly ExcelImportExportService _excel = new ExcelImportExportService();

        // Tạo nội dung xuất dữ liệu theo bộ lọc và phạm vi được phép.
        public byte[] ExportDepartments()
        {
            return _excel.CreateXlsx(_departments.GetAll(), new List<KeyValuePair<string, Func<KhoaPhongViewModel, object>>>
            {
                new KeyValuePair<string, Func<KhoaPhongViewModel, object>>("IDKHOAPHONG", x => x.IdKhoaPhongNguon),
                new KeyValuePair<string, Func<KhoaPhongViewModel, object>>("TENKHOAPHONG", x => x.TenKhoaPhong),
                new KeyValuePair<string, Func<KhoaPhongViewModel, object>>("USED", x => x.Used ? 1 : 0),
                new KeyValuePair<string, Func<KhoaPhongViewModel, object>>("GHICHU", x => x.GhiChu)
            });
        }

        // Tạo nội dung xuất dữ liệu theo bộ lọc và phạm vi được phép.
        public byte[] ExportEmployees(EmployeeExportQueryDto dto)
        {
            return ExportEmployees(dto.DepartmentId, dto.IsAdmin, dto.CurrentDepartmentId);
        }

        // Tạo nội dung xuất dữ liệu theo bộ lọc và phạm vi được phép.
        public byte[] ExportEmployees(int? departmentId, bool admin, int? currentDepartmentId)
        {
            var effectiveDepartmentId = admin ? departmentId : currentDepartmentId;
            return _excel.CreateXlsx(_employees.GetAll(effectiveDepartmentId), new List<KeyValuePair<string, Func<NhanVienViewModel, object>>>
            {
                new KeyValuePair<string, Func<NhanVienViewModel, object>>("MaNhanVien", x => x.MaNhanVien),
                new KeyValuePair<string, Func<NhanVienViewModel, object>>("HoTen", x => x.HoTen),
                new KeyValuePair<string, Func<NhanVienViewModel, object>>("KhoaPhong", x => x.TenKhoaPhong),
                new KeyValuePair<string, Func<NhanVienViewModel, object>>("Email", x => x.Email),
                new KeyValuePair<string, Func<NhanVienViewModel, object>>("SoDienThoai", x => x.SoDienThoai)
            });
        }

        // Tạo nội dung xuất dữ liệu theo bộ lọc và phạm vi được phép.
        public byte[] ExportIndicators()
        {
            return _excel.CreateXlsx(_indicators.GetAll(), new List<KeyValuePair<string, Func<ChiSoViewModel, object>>>
            {
                new KeyValuePair<string, Func<ChiSoViewModel, object>>("MaChiSo", x => x.MaChiSo),
                new KeyValuePair<string, Func<ChiSoViewModel, object>>("TenChiSo", x => x.TenChiSo),
                new KeyValuePair<string, Func<ChiSoViewModel, object>>("TanSuatBaoCao", x => x.TanSuatBaoCaoText),
                new KeyValuePair<string, Func<ChiSoViewModel, object>>("LoaiCongThuc", x => x.LoaiCongThuc),
                new KeyValuePair<string, Func<ChiSoViewModel, object>>("DonViTinh", x => x.DonViTinh)
            });
        }

        // Tạo nội dung xuất dữ liệu theo bộ lọc và phạm vi được phép.
        public byte[] ExportReports(ReportExportQueryDto dto)
        {
            return ExportReports(dto.PeriodId, dto.DepartmentId, dto.IndicatorId, dto.IsAdmin, dto.CurrentDepartmentId);
        }

        // Tạo nội dung xuất dữ liệu theo bộ lọc và phạm vi được phép.
        public byte[] ExportReports(int? periodId, int? departmentId, int? indicatorId, bool admin, int? currentDepartmentId)
        {
            var rows = _reports.GetAll(periodId, departmentId, indicatorId, admin, currentDepartmentId);
            return _excel.CreateXlsx(rows, new List<KeyValuePair<string, Func<ReportEntryViewModel, object>>>
            {
                new KeyValuePair<string, Func<ReportEntryViewModel, object>>("KyBaoCao", x => x.TenKyBaoCao),
                new KeyValuePair<string, Func<ReportEntryViewModel, object>>("KhoaPhong", x => x.TenKhoaPhong),
                new KeyValuePair<string, Func<ReportEntryViewModel, object>>("MaChiSo", x => x.MaChiSo),
                new KeyValuePair<string, Func<ReportEntryViewModel, object>>("TenChiSo", x => x.TenChiSo),
                new KeyValuePair<string, Func<ReportEntryViewModel, object>>("KetQua", x => FormatDecimal(x.KetQua)),
                new KeyValuePair<string, Func<ReportEntryViewModel, object>>("TrangThai", x => x.TrangThai),
                new KeyValuePair<string, Func<ReportEntryViewModel, object>>("DatMucTieu", x => x.DatMucTieu)
            });
        }

        // Tạo nội dung xuất dữ liệu theo bộ lọc và phạm vi được phép.
        public byte[] ExportAssignments(AssignmentExportQueryDto dto)
        {
            return ExportAssignments(dto.DepartmentId, dto.IndicatorId, dto.Status, dto.AssignmentStatus, dto.Search, dto.Columns);
        }

        // Tạo nội dung xuất dữ liệu theo bộ lọc và phạm vi được phép.
        public byte[] ExportAssignments(
            int? departmentId,
            int? indicatorId,
            string status,
            string assignmentStatus,
            string search,
            string[] selectedColumnKeys)
        {
            var rows = _assignments.GetExportRows(departmentId, indicatorId, status, assignmentStatus, search);
            var columns = ResolveAssignmentExportColumns(selectedColumnKeys)
                .Select(x => new KeyValuePair<string, Func<AssignmentExportRow, object>>(x.Header, x.Value))
                .ToList();

            return _excel.CreateXlsx(rows, columns);
        }

        // Tạo nội dung xuất dữ liệu theo bộ lọc và phạm vi được phép.
        public byte[] ExportDashboardProgress(string[] selectedColumnKeys, int? tanSuatFilter = null)
        {
            var rows = QueryDepartmentProgress(tanSuatFilter);
            var columns = ResolveDashboardProgressExportColumns(selectedColumnKeys)
                .Select(x => new KeyValuePair<string, Func<DepartmentProgressViewModel, object>>(x.Header, x.Value))
                .ToList();
            var detailRows = QueryDashboardReportDetails(tanSuatFilter);

            return _excel.CreateXlsxWorkbook(new List<ExcelWorksheetExport>
            {
                ExcelWorksheetExport.From("TongHopTienDo", rows, columns),
                ExcelWorksheetExport.From("ChiTietSoLieu", detailRows, DashboardReportDetailExportColumns
                    .Select(x => new KeyValuePair<string, Func<DashboardReportDetailExportRow, object>>(x.Header, x.Value))
                    .ToList())
            });
        }

        // Truy vấn xuất dữ liệu quản trị theo điều kiện được cung cấp.
        private IList<DepartmentProgressViewModel> QueryDepartmentProgress(int? tanSuatFilter = null)
        {
            string query;
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            
            if (tanSuatFilter.HasValue)
            {
                query = @"
SELECT 
    kp.TenKhoaPhong,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = @TanSuat) AS Tong,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = @TanSuat AND bc.TrangThai IN (2,3,4)) AS DaGui,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = @TanSuat AND bc.TrangThai = 1) AS LuuNhap,
    (SELECT COUNT(*) FROM dbo.BaoCao bc INNER JOIN dbo.KyBaoCao ky ON ky.KyBaoCaoId = bc.KyBaoCaoId INNER JOIN dbo.BaoCaoChiTiet ct ON ct.BaoCaoId = bc.BaoCaoId WHERE bc.KhoaPhongId = kp.KhoaPhongId AND ky.LoaiKyBaoCao = @TanSuat AND YEAR(ky.TuNgay) = YEAR(GETDATE()) AND bc.TrangThai IN (2,3,4) AND ct.DatMucTieu = 1) AS SoBaoCaoDatMucTieuNam,
    (SELECT COUNT(*) FROM dbo.BaoCao bc INNER JOIN dbo.KyBaoCao ky ON ky.KyBaoCaoId = bc.KyBaoCaoId INNER JOIN dbo.BaoCaoChiTiet ct ON ct.BaoCaoId = bc.BaoCaoId WHERE bc.KhoaPhongId = kp.KhoaPhongId AND ky.LoaiKyBaoCao = @TanSuat AND YEAR(ky.TuNgay) = YEAR(GETDATE()) AND bc.TrangThai IN (2,3,4) AND ct.DatMucTieu IS NOT NULL) AS SoBaoCaoDanhGiaMucTieuNam,
    
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 3) AS TongThang,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 3 AND bc.TrangThai IN (2,3,4)) AS DaGuiThang,

    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 4) AS TongQuy,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 4 AND bc.TrangThai IN (2,3,4)) AS DaGuiQuy,

    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 6) AS TongNam,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 6 AND bc.TrangThai IN (2,3,4)) AS DaGuiNam
FROM dbo.KhoaPhong kp
ORDER BY kp.TenKhoaPhong";
                sqlParams.Add(Param("@TanSuat", tanSuatFilter.Value));
            }
            else
            {
                query = @"
SELECT 
    kp.TenKhoaPhong,
    (SELECT COUNT(*) FROM dbo.PhanCongChiSo pc WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1) AS Tong,
    (SELECT COUNT(*) FROM dbo.PhanCongChiSo pc JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND bc.TrangThai IN (2,3,4)) AS DaGui,
    (SELECT COUNT(*) FROM dbo.PhanCongChiSo pc JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND bc.TrangThai = 1) AS LuuNhap,
    (SELECT COUNT(*) FROM dbo.BaoCao bc INNER JOIN dbo.KyBaoCao ky ON ky.KyBaoCaoId = bc.KyBaoCaoId INNER JOIN dbo.BaoCaoChiTiet ct ON ct.BaoCaoId = bc.BaoCaoId WHERE bc.KhoaPhongId = kp.KhoaPhongId AND YEAR(ky.TuNgay) = YEAR(GETDATE()) AND bc.TrangThai IN (2,3,4) AND ct.DatMucTieu = 1) AS SoBaoCaoDatMucTieuNam,
    (SELECT COUNT(*) FROM dbo.BaoCao bc INNER JOIN dbo.KyBaoCao ky ON ky.KyBaoCaoId = bc.KyBaoCaoId INNER JOIN dbo.BaoCaoChiTiet ct ON ct.BaoCaoId = bc.BaoCaoId WHERE bc.KhoaPhongId = kp.KhoaPhongId AND YEAR(ky.TuNgay) = YEAR(GETDATE()) AND bc.TrangThai IN (2,3,4) AND ct.DatMucTieu IS NOT NULL) AS SoBaoCaoDanhGiaMucTieuNam,
    
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 3) AS TongThang,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 3 AND bc.TrangThai IN (2,3,4)) AS DaGuiThang,

    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 4) AS TongQuy,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 4 AND bc.TrangThai IN (2,3,4)) AS DaGuiQuy,

    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 6) AS TongNam,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 6 AND bc.TrangThai IN (2,3,4)) AS DaGuiNam
FROM dbo.KhoaPhong kp
ORDER BY kp.TenKhoaPhong";
            }

            var list = Query(query, r => new DepartmentProgressViewModel
            {
                TenKhoaPhong = String(r, "TenKhoaPhong"),
                Tong = Int(r, "Tong"),
                DaGui = Int(r, "DaGui"),
                LuuNhap = Int(r, "LuuNhap"),
                SoBaoCaoDatMucTieuNam = Int(r, "SoBaoCaoDatMucTieuNam"),
                SoBaoCaoDanhGiaMucTieuNam = Int(r, "SoBaoCaoDanhGiaMucTieuNam"),
                
                TongThang = Int(r, "TongThang"),
                DaGuiThang = Int(r, "DaGuiThang"),
                
                TongQuy = Int(r, "TongQuy"),
                DaGuiQuy = Int(r, "DaGuiQuy"),
                
                TongNam = Int(r, "TongNam"),
                DaGuiNam = Int(r, "DaGuiNam")
            }, sqlParams.ToArray());

            foreach (var progress in list)
            {
                progress.XepLoai = CalculateXepLoai(progress.DaGui, progress.Tong);
                progress.XepLoaiThang = CalculateXepLoai(progress.DaGuiThang, progress.TongThang);
                progress.XepLoaiQuy = CalculateXepLoai(progress.DaGuiQuy, progress.TongQuy);
                progress.XepLoaiNam = CalculateXepLoai(progress.DaGuiNam, progress.TongNam);
            }

            return list;
        }

        // Truy vấn xuất dữ liệu quản trị theo điều kiện được cung cấp.
        private IList<DashboardReportDetailExportRow> QueryDashboardReportDetails(int? tanSuatFilter = null)
        {
            var sql = @"
SELECT
    kp.TenKhoaPhong,
    ky.TenKyBaoCao,
    DATEPART(YEAR, ky.TuNgay) AS NamBaoCao,
    cs.MaChiSo,
    cs.TenChiSo,
    ky.LoaiKyBaoCao AS TanSuatBaoCao,
    ct.TuSo,
    ct.MauSo,
    ct.GiaTriNhap,
    ct.KetQua,
    COALESCE(mt.ToanTuSoSanh, mtFallback.ToanTuSoSanh) AS ToanTuSoSanh,
    COALESCE(mt.GiaTriMucTieu, mtFallback.GiaTriMucTieu) AS GiaTriMucTieu,
    COALESCE(mt.MoTaMucTieu, mtFallback.MoTaMucTieu) AS MoTaMucTieu,
    ct.DatMucTieu,
    bc.TrangThai,
    bc.NgayGui,
    ct.GhiChu
FROM dbo.BaoCao bc
INNER JOIN dbo.KyBaoCao ky ON ky.KyBaoCaoId = bc.KyBaoCaoId
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = bc.KhoaPhongId
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = bc.ChiSoChatLuongId
LEFT JOIN dbo.BaoCaoChiTiet ct ON ct.BaoCaoId = bc.BaoCaoId
LEFT JOIN dbo.ChiSoMucTieu mt ON mt.ChiSoChatLuongId = bc.ChiSoChatLuongId AND mt.Nam = DATEPART(YEAR, ky.TuNgay)
OUTER APPLY (
    SELECT TOP 1 mt2.ToanTuSoSanh, mt2.GiaTriMucTieu, mt2.MoTaMucTieu
    FROM dbo.ChiSoMucTieu mt2
    WHERE mt2.ChiSoChatLuongId = bc.ChiSoChatLuongId
    ORDER BY
        CASE WHEN mt2.Nam <= DATEPART(YEAR, ky.TuNgay) THEN 0 ELSE 1 END,
        CASE WHEN mt2.Nam <= DATEPART(YEAR, ky.TuNgay) THEN mt2.Nam END DESC,
        mt2.Nam DESC
) mtFallback
WHERE (@TanSuat IS NULL OR ky.LoaiKyBaoCao = @TanSuat)
ORDER BY kp.TenKhoaPhong, ky.TuNgay DESC, cs.MaChiSo";

            return Query(sql, r => new DashboardReportDetailExportRow
            {
                TenKhoaPhong = String(r, "TenKhoaPhong"),
                TenKyBaoCao = String(r, "TenKyBaoCao"),
                NamBaoCao = Int(r, "NamBaoCao"),
                MaChiSo = String(r, "MaChiSo"),
                TenChiSo = String(r, "TenChiSo"),
                TanSuatBaoCaoText = FormatTanSuatBaoCao((TanSuatBaoCao)Convert.ToByte(r["TanSuatBaoCao"])),
                TuSo = NullableDecimal(r, "TuSo"),
                MauSo = NullableDecimal(r, "MauSo"),
                GiaTriNhap = NullableDecimal(r, "GiaTriNhap"),
                KetQua = NullableDecimal(r, "KetQua"),
                MucTieuNam = FormatMucTieuNam(String(r, "ToanTuSoSanh"), NullableDecimal(r, "GiaTriMucTieu"), String(r, "MoTaMucTieu")),
                DatMucTieu = r.IsDBNull(r.GetOrdinal("DatMucTieu")) ? (bool?)null : r.GetBoolean(r.GetOrdinal("DatMucTieu")),
                TrangThaiBaoCaoText = FormatTrangThaiBaoCao((TrangThaiBaoCao)Convert.ToByte(r["TrangThai"])),
                NgayGui = NullableDateTime(r, "NgayGui"),
                GhiChu = String(r, "GhiChu")
            }, Param("@TanSuat", tanSuatFilter));
        }

        // Tính toán giá trị nghiệp vụ phục vụ xuất dữ liệu quản trị.
        private static string CalculateXepLoai(int daGui, int tong)
        {
            if (tong == 0) return "N/A";
            decimal rate = (decimal)daGui * 100 / tong;
            if (rate >= 90) return "Xuất sắc";
            if (rate >= 70) return "Khá";
            if (rate >= 50) return "Trung bình";
            return "Yếu";
        }

        // Định dạng giá trị theo quy ước hiển thị của xuất dữ liệu quản trị.
        private static string FormatMucTieuNam(string op, decimal? value, string description)
        {
            if (!string.IsNullOrWhiteSpace(description))
            {
                return description;
            }

            if (string.IsNullOrWhiteSpace(op) || !value.HasValue)
            {
                return string.Empty;
            }

            return op.Trim() + " " + FormatDecimal(value);
        }

        // Định dạng giá trị theo quy ước hiển thị của xuất dữ liệu quản trị.
        private static string FormatDecimal(decimal? value)
        {
            return value.HasValue ? value.Value.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty;
        }

        // Định dạng giá trị theo quy ước hiển thị của xuất dữ liệu quản trị.
        private static string FormatTanSuatBaoCao(TanSuatBaoCao frequency)
        {
            switch (frequency)
            {
                case TanSuatBaoCao.HangNgay: return "Hàng ngày";
                case TanSuatBaoCao.HangTuan: return "Hàng tuần";
                case TanSuatBaoCao.HangThang: return "Hàng tháng";
                case TanSuatBaoCao.HangQuy: return "Hàng quý";
                case TanSuatBaoCao.SauThang: return "6 tháng";
                case TanSuatBaoCao.ChinThang: return "9 tháng";
                case TanSuatBaoCao.HangNam: return "Hàng năm";
                case TanSuatBaoCao.KhiPhatSinh: return "Khi phát sinh";
                case TanSuatBaoCao.TruocSauKhiThucHien: return "Trước/sau khi thực hiện";
                default: return frequency.ToString();
            }
        }

        // Định dạng giá trị theo quy ước hiển thị của xuất dữ liệu quản trị.
        private static string FormatTrangThaiBaoCao(TrangThaiBaoCao status)
        {
            switch (status)
            {
                case TrangThaiBaoCao.Nhap: return "Nháp";
                case TrangThaiBaoCao.DaGui: return "Đã gửi";
                case TrangThaiBaoCao.QuaHan: return "Quá hạn";
                case TrangThaiBaoCao.DaKhoa: return "Đã khóa";
                case TrangThaiBaoCao.DaDuyet: return "Đã duyệt";
                case TrangThaiBaoCao.TraLai: return "Trả lại";
                default: return status.ToString();
            }
        }

        // Xác định giá trị phù hợp từ các nguồn dữ liệu của xuất dữ liệu quản trị.
        private static IList<AssignmentExportColumn> ResolveAssignmentExportColumns(string[] selectedColumnKeys)
        {
            if (selectedColumnKeys == null || selectedColumnKeys.Length == 0)
            {
                return AssignmentExportColumns.ToList();
            }

            var selected = new HashSet<string>(selectedColumnKeys.Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
            var columns = AssignmentExportColumns.Where(x => selected.Contains(x.Key)).ToList();
            return columns.Count == 0 ? AssignmentExportColumns.ToList() : columns;
        }

        private static readonly IList<AssignmentExportColumn> AssignmentExportColumns = new List<AssignmentExportColumn>
        {
            new AssignmentExportColumn("TenChiSo", "Tên Chỉ số", x => x.TenChiSo),
            new AssignmentExportColumn("TanSuatBaoCao", "Tần suất báo cáo", x => x.TanSuatBaoCaoText),
            new AssignmentExportColumn("PhuongPhapTinh", "Phương pháp tính", x => x.PhuongPhapTinh),
            new AssignmentExportColumn("TuSo", "Tử số", x => x.TuSoMoTa),
            new AssignmentExportColumn("MauSo", "Mẫu số", x => x.MauSoMoTa),
            new AssignmentExportColumn("ThuThapTongHop", "Thu thập và tổng hợp số liệu", x => x.ThuThapTongHop),
            new AssignmentExportColumn("KhoaPhongDuocPhanCong", "Khoa/Phòng được phân công", x => x.TenKhoaPhong)
        };

        private class AssignmentExportColumn
        {
            // Khởi tạo thành phần và các giá trị cần thiết cho dữ liệu nội bộ của AssignmentExportColumn.
            public AssignmentExportColumn(string key, string header, Func<AssignmentExportRow, object> value)
            {
                Key = key;
                Header = header;
                Value = value;
            }

            public string Key { get; private set; }
            public string Header { get; private set; }
            public Func<AssignmentExportRow, object> Value { get; private set; }
        }

        // Xác định giá trị phù hợp từ các nguồn dữ liệu của xuất dữ liệu quản trị.
        private static IList<DashboardProgressExportColumn> ResolveDashboardProgressExportColumns(string[] selectedColumnKeys)
        {
            if (selectedColumnKeys == null || selectedColumnKeys.Length == 0)
            {
                return AllDashboardProgressExportColumns().ToList();
            }

            var selected = new HashSet<string>(selectedColumnKeys.Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
            var columns = AllDashboardProgressExportColumns().Where(x => selected.Contains(x.Key)).ToList();
            return columns.Count == 0 ? AllDashboardProgressExportColumns().ToList() : columns;
        }

        private static readonly IList<DashboardProgressExportColumn> DashboardProgressExportColumns = new List<DashboardProgressExportColumn>
        {
            new DashboardProgressExportColumn("TenKhoaPhong", "Khoa / Phòng", x => x.TenKhoaPhong),
            new DashboardProgressExportColumn("DaGui", "Số báo cáo đã gửi", x => x.DaGui),
            new DashboardProgressExportColumn("Tong", "Tổng số chỉ số cần nộp", x => x.Tong),
            new DashboardProgressExportColumn("PhanTram", "Tỷ lệ hoàn tất (%)", x => x.Tong > 0 ? (object)Math.Round((decimal)x.DaGui * 100 / x.Tong, 1) : 0),
            new DashboardProgressExportColumn("LuuNhap", "Số báo cáo lưu nháp", x => x.LuuNhap),
            new DashboardProgressExportColumn("ConThieu", "Số báo cáo còn thiếu", x => x.ConThieu),
            new DashboardProgressExportColumn("XepLoai", "Xếp loại tổng thể", x => x.XepLoai),
            new DashboardProgressExportColumn("TiendoThang", "Tiến độ Hàng tháng", x => x.TiendoHangThang),
            new DashboardProgressExportColumn("XepLoaiThang", "Xếp loại Hàng tháng", x => x.XepLoaiThang),
            new DashboardProgressExportColumn("TiendoQuy", "Tiến độ Hàng quý", x => x.TiendoHangQuy),
            new DashboardProgressExportColumn("XepLoaiQuy", "Xếp loại Hàng quý", x => x.XepLoaiQuy),
            new DashboardProgressExportColumn("TiendoNam", "Tiến độ Hàng năm", x => x.TiendoHangNam),
            new DashboardProgressExportColumn("XepLoaiNam", "Xếp loại Hàng năm", x => x.XepLoaiNam)
        };

        // Trả về đầy đủ cấu hình cột được hỗ trợ cho xuất dữ liệu quản trị.
        private static IEnumerable<DashboardProgressExportColumn> AllDashboardProgressExportColumns()
        {
            return DashboardProgressExportColumns.Concat(DashboardProgressTargetYearExportColumns);
        }

        private static readonly IList<DashboardProgressExportColumn> DashboardProgressTargetYearExportColumns = new List<DashboardProgressExportColumn>
        {
            new DashboardProgressExportColumn("SoBaoCaoDatMucTieuNam", "Số báo cáo đạt mục tiêu năm", x => x.SoBaoCaoDatMucTieuNam),
            new DashboardProgressExportColumn("SoBaoCaoDanhGiaMucTieuNam", "Số báo cáo đã đánh giá mục tiêu năm", x => x.SoBaoCaoDanhGiaMucTieuNam),
            new DashboardProgressExportColumn("TyLeDatMucTieuNam", "Tỷ lệ đạt mục tiêu năm (%)", x => x.TyLeDatMucTieuNam)
        };

        private static readonly IList<DashboardReportDetailExportColumn> DashboardReportDetailExportColumns = new List<DashboardReportDetailExportColumn>
        {
            new DashboardReportDetailExportColumn("TenKhoaPhong", "Khoa / Phòng", x => x.TenKhoaPhong),
            new DashboardReportDetailExportColumn("TenKyBaoCao", "Kỳ báo cáo", x => x.TenKyBaoCao),
            new DashboardReportDetailExportColumn("NamBaoCao", "Năm báo cáo", x => x.NamBaoCao),
            new DashboardReportDetailExportColumn("MaChiSo", "Mã chỉ số", x => x.MaChiSo),
            new DashboardReportDetailExportColumn("TenChiSo", "Tên chỉ số", x => x.TenChiSo),
            new DashboardReportDetailExportColumn("TanSuatBaoCao", "Tần suất báo cáo", x => x.TanSuatBaoCaoText),
            new DashboardReportDetailExportColumn("TuSo", "Tử số", x => x.TuSo),
            new DashboardReportDetailExportColumn("MauSo", "Mẫu số", x => x.MauSo),
            new DashboardReportDetailExportColumn("GiaTriNhap", "Giá trị nhập", x => x.GiaTriNhap),
            new DashboardReportDetailExportColumn("KetQua", "Kết quả", x => FormatDecimal(x.KetQua)),
            new DashboardReportDetailExportColumn("MucTieuNam", "Mục tiêu năm", x => x.MucTieuNam),
            new DashboardReportDetailExportColumn("DatMucTieu", "Đạt mục tiêu", x => x.DatMucTieuText),
            new DashboardReportDetailExportColumn("TrangThaiBaoCao", "Trạng thái báo cáo", x => x.TrangThaiBaoCaoText),
            new DashboardReportDetailExportColumn("NgayGui", "Ngày gửi", x => x.NgayGui.HasValue ? x.NgayGui.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) : string.Empty),
            new DashboardReportDetailExportColumn("GhiChu", "Ghi chú", x => x.GhiChu)
        };

        private class DashboardProgressExportColumn
        {
            // Khởi tạo thành phần và các giá trị cần thiết cho dữ liệu nội bộ của DashboardProgressExportColumn.
            public DashboardProgressExportColumn(string key, string header, Func<DepartmentProgressViewModel, object> value)
            {
                Key = key;
                Header = header;
                Value = value;
            }

            public string Key { get; private set; }
            public string Header { get; private set; }
            public Func<DepartmentProgressViewModel, object> Value { get; private set; }
        }

        private class DashboardReportDetailExportColumn
        {
            // Khởi tạo thành phần và các giá trị cần thiết cho dữ liệu nội bộ của DashboardReportDetailExportColumn.
            public DashboardReportDetailExportColumn(string key, string header, Func<DashboardReportDetailExportRow, object> value)
            {
                Key = key;
                Header = header;
                Value = value;
            }

            public string Key { get; private set; }
            public string Header { get; private set; }
            public Func<DashboardReportDetailExportRow, object> Value { get; private set; }
        }
    }
}
