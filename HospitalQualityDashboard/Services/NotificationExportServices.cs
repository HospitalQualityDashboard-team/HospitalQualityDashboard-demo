using System;
using System.Collections.Generic;
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
SELECT tb.ThongBaoId, tb.TieuDe, tb.NoiDung, tb.LoaiThongBao, tb.NgayTao, ISNULL(tbn.DaDoc, 0) AS DaDoc
FROM dbo.ThongBao tb
LEFT JOIN dbo.ThongBaoNguoiNhan tbn ON tbn.ThongBaoId = tb.ThongBaoId AND tbn.TaiKhoanId = @TaiKhoanId
WHERE @IsAdmin = 1 OR tbn.TaiKhoanId = @TaiKhoanId
ORDER BY tb.NgayTao DESC";
            return Query(sql, r => new NotificationViewModel
            {
                ThongBaoId = Int(r, "ThongBaoId"),
                TieuDe = String(r, "TieuDe"),
                NoiDung = String(r, "NoiDung"),
                LoaiThongBao = (LoaiThongBao)r.GetByte(r.GetOrdinal("LoaiThongBao")),
                NgayTao = r.GetDateTime(r.GetOrdinal("NgayTao")),
                DaDoc = r.GetBoolean(r.GetOrdinal("DaDoc"))
            }, Param("@TaiKhoanId", accountId), Param("@IsAdmin", admin));
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
    }

    public class ExportService
    {
        private readonly DepartmentService _departments = new DepartmentService();
        private readonly EmployeeService _employees = new EmployeeService();
        private readonly IndicatorService _indicators = new IndicatorService();
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
                new KeyValuePair<string, Func<ChiSoViewModel, object>>("TanSuatBaoCao", x => x.TanSuatBaoCao),
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
    }
}
