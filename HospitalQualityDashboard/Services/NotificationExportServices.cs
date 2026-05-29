using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using HospitalQualityDashboard.Models.Enums;
using HospitalQualityDashboard.Models.ViewModels;

namespace HospitalQualityDashboard.Services
{
    public class NotificationService : DbServiceBase
    {
        public IList<NotificationViewModel> GetForUser(int accountId, bool admin)
        {
            const string sql = @"
SELECT tb.ThongBaoId, tb.TieuDe, tb.NoiDung, tb.LoaiThongBao, tb.KyBaoCaoId, tb.BaoCaoId, tb.NgayTao, ISNULL(tbn.DaDoc, 0) AS DaDoc
FROM dbo.ThongBao tb
LEFT JOIN dbo.ThongBaoNguoiNhan tbn ON tbn.ThongBaoId = tb.ThongBaoId AND tbn.TaiKhoanId = @TaiKhoanId
WHERE @IsAdmin = 1 OR tbn.TaiKhoanId = @TaiKhoanId
ORDER BY tb.NgayTao DESC";
            return Query(sql, MapNotification, Param("@TaiKhoanId", accountId), Param("@IsAdmin", admin));
        }

        public NotificationViewModel GetDetailForUser(int notificationId, int accountId, bool admin)
        {
            const string sql = @"
SELECT tb.ThongBaoId, tb.TieuDe, tb.NoiDung, tb.LoaiThongBao, tb.KyBaoCaoId, tb.BaoCaoId, tb.NgayTao, ISNULL(tbn.DaDoc, 0) AS DaDoc
FROM dbo.ThongBao tb
LEFT JOIN dbo.ThongBaoNguoiNhan tbn ON tbn.ThongBaoId = tb.ThongBaoId AND tbn.TaiKhoanId = @TaiKhoanId
WHERE tb.ThongBaoId = @ThongBaoId
  AND (@IsAdmin = 1 OR tbn.TaiKhoanId = @TaiKhoanId)";
            return QuerySingle(sql, MapNotification,
                Param("@ThongBaoId", notificationId),
                Param("@TaiKhoanId", accountId),
                Param("@IsAdmin", admin));
        }

        public void SendManual(NotificationViewModel model, int userId)
        {
            var notificationId = Convert.ToInt32(Scalar(@"INSERT INTO dbo.ThongBao(TieuDe, NoiDung, LoaiThongBao, NguoiTaoId)
OUTPUT INSERTED.ThongBaoId VALUES(@TieuDe, @NoiDung, @LoaiThongBao, @NguoiTaoId)",
                Param("@TieuDe", model.TieuDe),
                Param("@NoiDung", model.NoiDung),
                Param("@LoaiThongBao", (byte)LoaiThongBao.ThuCong),
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

        public void MarkAsRead(int notificationId, int accountId)
        {
            Execute("UPDATE dbo.ThongBaoNguoiNhan SET DaDoc=1, NgayDoc=GETDATE() WHERE ThongBaoId=@ThongBaoId AND TaiKhoanId=@TaiKhoanId",
                Param("@ThongBaoId", notificationId), Param("@TaiKhoanId", accountId));
        }

        private static NotificationViewModel MapNotification(SqlDataReader r)
        {
            return new NotificationViewModel
            {
                ThongBaoId = Int(r, "ThongBaoId"),
                TieuDe = String(r, "TieuDe"),
                NoiDung = String(r, "NoiDung"),
                LoaiThongBao = (LoaiThongBao)r.GetByte(r.GetOrdinal("LoaiThongBao")),
                KyBaoCaoId = NullableInt(r, "KyBaoCaoId"),
                BaoCaoId = NullableInt(r, "BaoCaoId"),
                NgayTao = r.GetDateTime(r.GetOrdinal("NgayTao")),
                DaDoc = r.GetBoolean(r.GetOrdinal("DaDoc"))
            };
        }
    }

    public class NotificationAutomationService : DbServiceBase
    {
        private readonly IndicatorService _indicators = new IndicatorService();

        public void Run(DateTime now)
        {
            _indicators.EnsureIndicatorFrequencyTable();
            EnsureAutomationLogTable();

            SendPeriodOpenedNotifications(now);
            SendDueReminderNotifications(now);
            SendOverdueNotifications(now);
            SendAdminDailySummary(now);
        }

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
                var body = string.Format("Kỳ báo cáo \"{0}\" đã mở. Khoa/phòng vui lòng nhập và gửi số liệu trước ngày {1:dd/MM/yyyy}.", row.TenKyBaoCao, row.HanNop);
                var notificationId = CreateAutoNotification(key, LoaiThongBao.KyBaoCaoMo, title, body, row.KyBaoCaoId, row.KhoaPhongId, row.HanNop.Date);
                AddDepartmentRecipients(notificationId, row.KhoaPhongId);
            }
        }

        public void SendDueReminderNotifications(DateTime now)
        {
            foreach (var daysBeforeDue in new[] { 7, 3, 1, 0 })
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
    AND bc.TrangThai IN (@DaGui, @QuaHan, @DaKhoa)
WHERE ky.TrangThai = @Mo
  AND DATEDIFF(day, CAST(@Now AS date), ky.HanNop) = @DaysBeforeDue
GROUP BY ky.KyBaoCaoId, ky.TenKyBaoCao, ky.HanNop, kp.KhoaPhongId, kp.TenKhoaPhong
HAVING COUNT(pc.PhanCongChiSoId) - COUNT(bc.BaoCaoId) > 0";

                var rows = Query(sql, MapAutomationRow,
                    Param("@Mo", (byte)TrangThaiKyBaoCao.Mo),
                    Param("@DaGui", (byte)TrangThaiBaoCao.DaGui),
                    Param("@QuaHan", (byte)TrangThaiBaoCao.QuaHan),
                    Param("@DaKhoa", (byte)TrangThaiBaoCao.DaKhoa),
                    Param("@Now", now),
                    Param("@DaysBeforeDue", daysBeforeDue));

                foreach (var row in rows)
                {
                    var key = string.Format("due:{0}:{1}:{2}", row.KyBaoCaoId, row.KhoaPhongId, daysBeforeDue);
                    var title = daysBeforeDue == 0 ? "Hôm nay là hạn nộp báo cáo" : string.Format("Còn {0} ngày đến hạn nộp báo cáo", daysBeforeDue);
                    var body = string.Format("Kỳ báo cáo \"{0}\" còn {1} chỉ số chưa gửi. Hạn nộp: {2:dd/MM/yyyy}.", row.TenKyBaoCao, row.MissingCount, row.HanNop);
                    var notificationId = CreateAutoNotification(key, LoaiThongBao.NhacHan, title, body, row.KyBaoCaoId, row.KhoaPhongId, row.HanNop.Date);
                    AddDepartmentRecipients(notificationId, row.KhoaPhongId);
                }
            }
        }

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
    AND bc.TrangThai IN (@DaGui, @QuaHan, @DaKhoa)
WHERE ky.TrangThai = @Mo
  AND CAST(@Now AS date) > ky.HanNop
GROUP BY ky.KyBaoCaoId, ky.TenKyBaoCao, ky.HanNop, kp.KhoaPhongId, kp.TenKhoaPhong
HAVING COUNT(pc.PhanCongChiSoId) - COUNT(bc.BaoCaoId) > 0";

            var rows = Query(sql, MapAutomationRow,
                Param("@Mo", (byte)TrangThaiKyBaoCao.Mo),
                Param("@DaGui", (byte)TrangThaiBaoCao.DaGui),
                Param("@QuaHan", (byte)TrangThaiBaoCao.QuaHan),
                Param("@DaKhoa", (byte)TrangThaiBaoCao.DaKhoa),
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

        public void SendAdminDailySummary(DateTime now)
        {
            var summary = QuerySingle(@"
SELECT
    (SELECT COUNT(*) FROM dbo.BaoCao WHERE TrangThai IN (2,3,4)) AS SubmittedCount,
    (SELECT COUNT(*) FROM dbo.BaoCao WHERE TrangThai = 3) AS LateCount,
    (SELECT COUNT(*)
     FROM dbo.KyBaoCao ky
     INNER JOIN dbo.PhanCongChiSo pc ON pc.DangHoatDong = 1
     INNER JOIN dbo.ChiSoTanSuatBaoCao cst ON cst.ChiSoChatLuongId = pc.ChiSoChatLuongId AND cst.TanSuatBaoCao = ky.LoaiKyBaoCao
     LEFT JOIN dbo.BaoCao bc ON bc.KyBaoCaoId = ky.KyBaoCaoId
        AND bc.KhoaPhongId = pc.KhoaPhongId
        AND bc.ChiSoChatLuongId = pc.ChiSoChatLuongId
        AND bc.TrangThai IN (2,3,4)
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

        private void EnsureAutomationLogTable()
        {
            Execute(@"
IF OBJECT_ID('dbo.ThongBaoTuDongLog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ThongBaoTuDongLog (
        ThongBaoTuDongLogId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ThongBaoTuDongLog PRIMARY KEY,
        DedupKey NVARCHAR(255) NOT NULL,
        LoaiThongBao TINYINT NOT NULL,
        KyBaoCaoId INT NULL,
        KhoaPhongId INT NULL,
        NgayMoc DATE NULL,
        NgayTao DATETIME NOT NULL CONSTRAINT DF_ThongBaoTuDongLog_NgayTao DEFAULT (GETDATE()),
        CONSTRAINT UQ_ThongBaoTuDongLog_DedupKey UNIQUE (DedupKey),
        CONSTRAINT FK_ThongBaoTuDongLog_KyBaoCao FOREIGN KEY (KyBaoCaoId) REFERENCES dbo.KyBaoCao(KyBaoCaoId),
        CONSTRAINT FK_ThongBaoTuDongLog_KhoaPhong FOREIGN KEY (KhoaPhongId) REFERENCES dbo.KhoaPhong(KhoaPhongId)
    );
END");
        }

        private int CreateAutoNotification(string dedupKey, LoaiThongBao type, string title, string body, int? periodId, int? departmentId, DateTime? markerDate)
        {
            var result = Scalar(@"
DECLARE @ThongBaoId INT;
IF NOT EXISTS (SELECT 1 FROM dbo.ThongBaoTuDongLog WHERE DedupKey=@DedupKey)
BEGIN
    INSERT INTO dbo.ThongBao(TieuDe, NoiDung, LoaiThongBao, KyBaoCaoId)
    VALUES(@TieuDe, @NoiDung, @LoaiThongBao, @KyBaoCaoId);
    SET @ThongBaoId = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO dbo.ThongBaoTuDongLog(DedupKey, LoaiThongBao, KyBaoCaoId, KhoaPhongId, NgayMoc)
    VALUES(@DedupKey, @LoaiThongBao, @KyBaoCaoId, @KhoaPhongId, @NgayMoc);
END
SELECT ISNULL(@ThongBaoId, 0);",
                Param("@DedupKey", dedupKey),
                Param("@TieuDe", title),
                Param("@NoiDung", body),
                Param("@LoaiThongBao", (byte)type),
                Param("@KyBaoCaoId", periodId),
                Param("@KhoaPhongId", departmentId),
                Param("@NgayMoc", markerDate));

            return Convert.ToInt32(result);
        }

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

        private class AdminSummary
        {
            public int SubmittedCount { get; set; }
            public int LateCount { get; set; }
            public int MissingCount { get; set; }
        }
    }

    public class ExportService
    {
        private readonly DepartmentService _departments = new DepartmentService();
        private readonly EmployeeService _employees = new EmployeeService();
        private readonly IndicatorService _indicators = new IndicatorService();
        private readonly AssignmentService _assignments = new AssignmentService();
        private readonly ReportService _reports = new ReportService();
        private readonly ExcelImportExportService _excel = new ExcelImportExportService();

        public byte[] ExportDepartments()
        {
            return _excel.CreateCsv(_departments.GetAll(), new List<KeyValuePair<string, Func<KhoaPhongViewModel, object>>>
            {
                new KeyValuePair<string, Func<KhoaPhongViewModel, object>>("IDKHOAPHONG", x => x.IdKhoaPhongNguon),
                new KeyValuePair<string, Func<KhoaPhongViewModel, object>>("TENKHOAPHONG", x => x.TenKhoaPhong),
                new KeyValuePair<string, Func<KhoaPhongViewModel, object>>("USED", x => x.Used ? 1 : 0),
                new KeyValuePair<string, Func<KhoaPhongViewModel, object>>("GHICHU", x => x.GhiChu)
            });
        }

        public byte[] ExportEmployees(int? departmentId, bool admin, int? currentDepartmentId)
        {
            var effectiveDepartmentId = admin ? departmentId : currentDepartmentId;
            return _excel.CreateCsv(_employees.GetAll(effectiveDepartmentId), new List<KeyValuePair<string, Func<NhanVienViewModel, object>>>
            {
                new KeyValuePair<string, Func<NhanVienViewModel, object>>("MaNhanVien", x => x.MaNhanVien),
                new KeyValuePair<string, Func<NhanVienViewModel, object>>("HoTen", x => x.HoTen),
                new KeyValuePair<string, Func<NhanVienViewModel, object>>("KhoaPhong", x => x.TenKhoaPhong),
                new KeyValuePair<string, Func<NhanVienViewModel, object>>("Email", x => x.Email),
                new KeyValuePair<string, Func<NhanVienViewModel, object>>("SoDienThoai", x => x.SoDienThoai)
            });
        }

        public byte[] ExportIndicators()
        {
            return _excel.CreateCsv(_indicators.GetAll(), new List<KeyValuePair<string, Func<ChiSoViewModel, object>>>
            {
                new KeyValuePair<string, Func<ChiSoViewModel, object>>("MaChiSo", x => x.MaChiSo),
                new KeyValuePair<string, Func<ChiSoViewModel, object>>("TenChiSo", x => x.TenChiSo),
                new KeyValuePair<string, Func<ChiSoViewModel, object>>("TanSuatBaoCao", x => x.TanSuatBaoCaoText),
                new KeyValuePair<string, Func<ChiSoViewModel, object>>("LoaiCongThuc", x => x.LoaiCongThuc),
                new KeyValuePair<string, Func<ChiSoViewModel, object>>("DonViTinh", x => x.DonViTinh)
            });
        }

        public byte[] ExportReports(int? periodId, int? departmentId, int? indicatorId, bool admin, int? currentDepartmentId)
        {
            var rows = _reports.GetAll(periodId, departmentId, indicatorId, admin, currentDepartmentId);
            return _excel.CreateCsv(rows, new List<KeyValuePair<string, Func<ReportEntryViewModel, object>>>
            {
                new KeyValuePair<string, Func<ReportEntryViewModel, object>>("KyBaoCao", x => x.TenKyBaoCao),
                new KeyValuePair<string, Func<ReportEntryViewModel, object>>("KhoaPhong", x => x.TenKhoaPhong),
                new KeyValuePair<string, Func<ReportEntryViewModel, object>>("MaChiSo", x => x.MaChiSo),
                new KeyValuePair<string, Func<ReportEntryViewModel, object>>("TenChiSo", x => x.TenChiSo),
                new KeyValuePair<string, Func<ReportEntryViewModel, object>>("KetQua", x => x.KetQua),
                new KeyValuePair<string, Func<ReportEntryViewModel, object>>("TrangThai", x => x.TrangThai),
                new KeyValuePair<string, Func<ReportEntryViewModel, object>>("DatMucTieu", x => x.DatMucTieu)
            });
        }

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
            new AssignmentExportColumn("TenChiSo", "T\u00ean Ch\u1ec9 s\u1ed1", x => x.TenChiSo),
            new AssignmentExportColumn("TanSuatBaoCao", "T\u1ea7n su\u1ea5t b\u00e1o c\u00e1o", x => x.TanSuatBaoCaoText),
            new AssignmentExportColumn("PhuongPhapTinh", "Ph\u01b0\u01a1ng ph\u00e1p t\u00ednh", x => x.PhuongPhapTinh),
            new AssignmentExportColumn("TuSo", "T\u1eed s\u1ed1", x => x.TuSoMoTa),
            new AssignmentExportColumn("MauSo", "M\u1eabu s\u1ed1", x => x.MauSoMoTa),
            new AssignmentExportColumn("ThuThapTongHop", "Thu th\u1eadp v\u00e0 t\u1ed5ng h\u1ee3p s\u1ed1 li\u1ec7u", x => x.ThuThapTongHop),
            new AssignmentExportColumn("KhoaPhongDuocPhanCong", "Khoa/Ph\u00f2ng \u0111\u01b0\u1ee3c ph\u00e2n c\u00f4ng", x => x.TenKhoaPhong)
        };

        private class AssignmentExportColumn
        {
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
    }
}
