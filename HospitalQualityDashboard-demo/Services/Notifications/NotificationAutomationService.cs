// Mục đích: tự động tạo thông báo nhắc hạn, quá hạn và cảnh báo chỉ số theo kỳ báo cáo.
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
    public class NotificationAutomationService : DbServiceBase
    {
        private readonly IndicatorService _indicators = new IndicatorService();

        // Chạy quy trình tự động của thông báo theo thời điểm hiện tại và dữ liệu kỳ báo cáo đang mở.
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
WHERE ky.TrangThai = @Mo
  AND dbo.fn_ChiSoDuocTrienKhaiTrongKy(pc.ChiSoChatLuongId, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay) = 1";

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
  AND dbo.fn_ChiSoDuocTrienKhaiTrongKy(pc.ChiSoChatLuongId, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay) = 1
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
INNER JOIN dbo.ChiSoTanSuatBaoCao cst
    ON cst.ChiSoChatLuongId = cs.ChiSoChatLuongId
   AND cst.TanSuatBaoCao = ky.LoaiKyBaoCao
LEFT JOIN dbo.BaoCao bc
    ON bc.KyBaoCaoId = ky.KyBaoCaoId
   AND bc.KhoaPhongId = pc.KhoaPhongId
   AND bc.ChiSoChatLuongId = pc.ChiSoChatLuongId
WHERE ky.KyBaoCaoId = @KyBaoCaoId
  AND ky.TrangThai = @Mo
  AND dbo.fn_ChiSoDuocTrienKhaiTrongKy(pc.ChiSoChatLuongId, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay) = 1";

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
  AND dbo.fn_ChiSoDuocTrienKhaiTrongKy(pc.ChiSoChatLuongId, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay) = 1
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
     WHERE ky.TrangThai = 2
       AND dbo.fn_ChiSoDuocTrienKhaiTrongKy(pc.ChiSoChatLuongId, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay) = 1
       AND bc.BaoCaoId IS NULL) AS MissingCount",
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

        // Tự tạo bảng log nếu môi trường chưa chạy migration, giúp job thông báo không lỗi ngay khi khởi động.
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

        // Tạo thông báo tự động theo loại nhắc hạn/quá hạn/cảnh báo và gắn người nhận phù hợp.
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

        // Thêm người nhận thuộc khoa/phòng chịu trách nhiệm để thông báo đi đúng đơn vị xử lý.
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

        // Thêm Admin làm người nhận khi thông báo cần theo dõi hoặc phối hợp xử lý toàn viện.
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

        // Chuyển dữ liệu kỳ báo cáo/khoa phòng cần nhắc việc thành dòng xử lý thông báo tự động.
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

        // Kiểm tra cột tùy chọn trong SqlDataReader vì từng loại thông báo tự động có SELECT khác nhau.
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
}
