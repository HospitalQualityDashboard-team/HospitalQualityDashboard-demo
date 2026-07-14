// Mục đích: tính các chỉ số tổng hợp cho dashboard Admin và User theo phạm vi truy cập.
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.Enums;
using HospitalQualityDashboardDemo.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Services
{
    public partial class DashboardService
    {
        // Tính toán giá trị nghiệp vụ phục vụ dữ liệu Dashboard.
        private static string CalculateXepLoai(int daGui, int tong)
        {
            if (tong == 0) return "N/A";
            decimal rate = (decimal)daGui * 100 / tong;
            if (rate >= 90) return "Xuất sắc";
            if (rate >= 70) return "Khá";
            if (rate >= 50) return "Trung bình";
            return "Yếu";
        }

        // Tổng hợp số liệu dashboard theo role, khoa/phòng và tần suất để tránh lộ dữ liệu ngoài phạm vi.
        private DashboardViewModel GetDashboardOptimized(bool admin, int? departmentId, int? tanSuatFilter, string DepartmentStatusFilter)
        {
            var model = new DashboardViewModel
            {
                IsAdmin = admin,
                DepartmentProgress = new List<DepartmentProgressViewModel>(),
                MissingReports = new List<MissingReportAlertViewModel>(),
                MetricDetails = new List<DashboardMetricDetailViewModel>(),
                SelectedTanSuat = tanSuatFilter,
                DepartmentStatusFilter = DepartmentStatusFilter,
                TanSuatOptions = BuildDashboardFrequencyOptions(tanSuatFilter)
            };

            ApplyOptimizedDashboardSummary(model, admin, departmentId, tanSuatFilter);
            model.DepartmentProgress = GetOptimizedDepartmentProgress(admin ? null : departmentId, tanSuatFilter);

            if (admin)
            {
                model.MetricDetails = GetAdminMetricDetails(tanSuatFilter, DepartmentStatusFilter);
            }
            else if (departmentId.HasValue)
            {
                model.MetricDetails = GetUserMetricDetails(departmentId.Value, tanSuatFilter, "active");
                model.MissingReports = GetMissingReportsForDepartment(departmentId.Value);
                model.BaoCaoThieu = model.MissingReports.Count;
                model.DueSoonReportCount = model.MissingReports.Count(x => x.IsDueSoon);
                model.OverdueMissingReportCount = model.MissingReports.Count(x => x.IsOverdue);
            }

            return model;
        }

        // Gán kết quả truy vấn tổng hợp vào model dashboard mà không phải chạy nhiều truy vấn nhỏ.
        private void ApplyOptimizedDashboardSummary(DashboardViewModel model, bool admin, int? departmentId, int? tanSuatFilter)
        {
            var sql = admin ? @"
WITH ExpectedSlots AS
(
    SELECT DISTINCT ky.KyBaoCaoId, pc.KhoaPhongId, pc.ChiSoChatLuongId, ky.HanNop, ky.TrangThai AS TrangThaiKyBaoCao
    FROM dbo.KyBaoCao ky
    INNER JOIN dbo.PhanCongChiSo pc ON pc.DangHoatDong = 1
    INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = pc.KhoaPhongId
    INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = pc.ChiSoChatLuongId
    INNER JOIN dbo.ChiSoTanSuatBaoCao ts
        ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId
       AND ts.TanSuatBaoCao = ky.LoaiKyBaoCao
    WHERE ky.TrangThai <> @DraftPeriodStatus
      AND kp.Used = 1
      AND (@TanSuat IS NULL OR ky.LoaiKyBaoCao = @TanSuat)
      AND dbo.fn_ChiSoDuocTrienKhaiTrongKy(pc.ChiSoChatLuongId, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay) = 1
),
CompletedSlots AS
(
    SELECT DISTINCT bc.KyBaoCaoId, bc.KhoaPhongId, bc.ChiSoChatLuongId
    FROM dbo.BaoCao bc
    WHERE bc.TrangThai IN (@DaGuiStatus, @QuaHanStatus, @DaKhoaStatus, @DaDuyetStatus)
)
SELECT
    COUNT(es.KyBaoCaoId) AS TongBaoCaoCanNop,
    ISNULL(SUM(CASE WHEN es.TrangThaiKyBaoCao = @OpenPeriodStatus THEN 1 ELSE 0 END), 0) AS TongBaoCaoCanNopKyDangMo,
    ISNULL(SUM(CASE WHEN completed.KyBaoCaoId IS NOT NULL THEN 1 ELSE 0 END), 0) AS BaoCaoDaGui,
    (SELECT COUNT(DISTINCT pc.PhanCongChiSoId)
     FROM dbo.PhanCongChiSo pc
     INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = pc.KhoaPhongId
     LEFT JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId
     WHERE pc.DangHoatDong = 1
       AND kp.Used = 1
       AND (@TanSuat IS NULL OR ts.TanSuatBaoCao = @TanSuat)) AS ChiSoDuocPhanCong,
    (SELECT COUNT(DISTINCT cs.ChiSoChatLuongId)
     FROM dbo.ChiSoChatLuong cs
     LEFT JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = cs.ChiSoChatLuongId
     WHERE cs.DangHoatDong = 1
       AND (@TanSuat IS NULL OR ts.TanSuatBaoCao = @TanSuat)) AS TongChiSo,
    ISNULL(SUM(CASE
        WHEN completed.KyBaoCaoId IS NULL AND es.HanNop < @Today THEN 1
        ELSE 0
    END), 0) AS BaoCaoQuaHan
FROM ExpectedSlots es
LEFT JOIN CompletedSlots completed
    ON completed.KyBaoCaoId = es.KyBaoCaoId
   AND completed.KhoaPhongId = es.KhoaPhongId
   AND completed.ChiSoChatLuongId = es.ChiSoChatLuongId"
            : @"
SELECT
    (SELECT COUNT(*)
     FROM (
         SELECT DISTINCT ky.KyBaoCaoId, pc.KhoaPhongId, pc.ChiSoChatLuongId
         FROM dbo.KyBaoCao ky
         INNER JOIN dbo.PhanCongChiSo pc ON pc.DangHoatDong = 1
         INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = pc.KhoaPhongId
         INNER JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId AND ts.TanSuatBaoCao = ky.LoaiKyBaoCao
         WHERE ky.TrangThai = @OpenPeriodStatus
           AND pc.KhoaPhongId = @KhoaPhongId
           AND kp.Used = 1
           AND (@TanSuat IS NULL OR ky.LoaiKyBaoCao = @TanSuat)
           AND dbo.fn_ChiSoDuocTrienKhaiTrongKy(pc.ChiSoChatLuongId, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay) = 1
     ) openSlots) AS TongBaoCaoCanNopKyDangMo,
    (SELECT COUNT(DISTINCT bc.BaoCaoId)
     FROM dbo.BaoCao bc
     LEFT JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = bc.ChiSoChatLuongId
     WHERE bc.TrangThai IN (2,3,4)
       AND bc.KhoaPhongId = @KhoaPhongId
       AND (@TanSuat IS NULL OR ts.TanSuatBaoCao = @TanSuat)) AS BaoCaoDaGui,
    (SELECT COUNT(DISTINCT pc.PhanCongChiSoId)
     FROM dbo.PhanCongChiSo pc
     INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = pc.KhoaPhongId
     LEFT JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId
     WHERE pc.DangHoatDong = 1
       AND pc.KhoaPhongId = @KhoaPhongId
       AND kp.Used = 1
       AND (@TanSuat IS NULL OR ts.TanSuatBaoCao = @TanSuat)) AS ChiSoDuocPhanCong,
    (SELECT COUNT(DISTINCT pc.PhanCongChiSoId)
     FROM dbo.PhanCongChiSo pc
     INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = pc.KhoaPhongId
     LEFT JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId
     WHERE pc.DangHoatDong = 1
       AND pc.KhoaPhongId = @KhoaPhongId
       AND kp.Used = 1
       AND (@TanSuat IS NULL OR ts.TanSuatBaoCao = @TanSuat)) AS TongChiSo,
    (SELECT COUNT(DISTINCT bc.BaoCaoId)
     FROM dbo.BaoCao bc
     LEFT JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = bc.ChiSoChatLuongId
     WHERE bc.TrangThai = @QuaHanStatus
       AND bc.KhoaPhongId = @KhoaPhongId
       AND (@TanSuat IS NULL OR ts.TanSuatBaoCao = @TanSuat)) AS BaoCaoQuaHan";

            var summary = QuerySingle(sql, r => new DashboardSummaryRow
            {
                TongBaoCaoCanNop = admin ? Int(r, "TongBaoCaoCanNop") : 0,
                TongBaoCaoCanNopKyDangMo = Int(r, "TongBaoCaoCanNopKyDangMo"),
                BaoCaoDaGui = Int(r, "BaoCaoDaGui"),
                ChiSoDuocPhanCong = Int(r, "ChiSoDuocPhanCong"),
                TongChiSo = Int(r, "TongChiSo"),
                BaoCaoQuaHan = Int(r, "BaoCaoQuaHan")
            },
                Param("@KhoaPhongId", departmentId),
                Param("@TanSuat", tanSuatFilter),
                Param("@Today", GetVietnamLocalNow().Date),
                Param("@DraftPeriodStatus", (byte)TrangThaiKyBaoCao.Nhap),
                Param("@OpenPeriodStatus", (byte)TrangThaiKyBaoCao.Mo),
                Param("@DaGuiStatus", (byte)TrangThaiBaoCao.DaGui),
                Param("@QuaHanStatus", (byte)TrangThaiBaoCao.QuaHan),
                Param("@DaKhoaStatus", (byte)TrangThaiBaoCao.DaKhoa),
                Param("@DaDuyetStatus", (byte)TrangThaiBaoCao.DaDuyet));

            if (summary == null)
            {
                return;
            }

            model.TongBaoCaoCanNop = summary.TongBaoCaoCanNop;
            model.TongBaoCaoCanNopKyDangMo = summary.TongBaoCaoCanNopKyDangMo;
            model.BaoCaoDaGui = summary.BaoCaoDaGui;
            model.ChiSoDuocPhanCong = summary.ChiSoDuocPhanCong;
            model.TongChiSo = summary.TongChiSo;
            model.BaoCaoQuaHan = summary.BaoCaoQuaHan;
            model.BaoCaoThieu = admin
                ? summary.TongBaoCaoCanNop - summary.BaoCaoDaGui
                : model.ChiSoDuocPhanCong - model.BaoCaoDaGui;
        }

        // Tính tiến độ báo cáo theo khoa/phòng bằng truy vấn tối ưu cho dashboard Admin.
        private IList<DepartmentProgressViewModel> GetOptimizedDepartmentProgress(int? departmentId, int? tanSuatFilter)
        {
            const string sql = @"
WITH AssignmentFrequency AS
(
    SELECT pc.KhoaPhongId, pc.ChiSoChatLuongId, cst.TanSuatBaoCao
    FROM dbo.PhanCongChiSo pc
    INNER JOIN dbo.ChiSoTanSuatBaoCao cst ON cst.ChiSoChatLuongId = pc.ChiSoChatLuongId
    INNER JOIN dbo.KyBaoCao ky
        ON ky.LoaiKyBaoCao = cst.TanSuatBaoCao
       AND ky.TrangThai <> @DraftPeriodStatus
       AND dbo.fn_ChiSoDuocTrienKhaiTrongKy(pc.ChiSoChatLuongId, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay) = 1
    WHERE pc.DangHoatDong = 1
),
ReportByIndicator AS
(
    SELECT bc.KhoaPhongId, bc.ChiSoChatLuongId,
           MAX(CASE WHEN bc.TrangThai IN (2,3,4) THEN 1 ELSE 0 END) AS HasSubmitted,
           MAX(CASE WHEN bc.TrangThai = 1 THEN 1 ELSE 0 END) AS HasDraft
    FROM dbo.BaoCao bc
    GROUP BY bc.KhoaPhongId, bc.ChiSoChatLuongId
)
SELECT
    kp.TenKhoaPhong,
    COUNT(DISTINCT CASE WHEN @TanSuat IS NULL OR af.TanSuatBaoCao = @TanSuat THEN af.ChiSoChatLuongId END) AS Tong,
    COUNT(DISTINCT CASE WHEN (@TanSuat IS NULL OR af.TanSuatBaoCao = @TanSuat) AND rb.HasSubmitted = 1 THEN af.ChiSoChatLuongId END) AS DaGui,
    COUNT(DISTINCT CASE WHEN (@TanSuat IS NULL OR af.TanSuatBaoCao = @TanSuat) AND rb.HasDraft = 1 THEN af.ChiSoChatLuongId END) AS LuuNhap,
    COUNT(DISTINCT CASE WHEN af.TanSuatBaoCao = 3 THEN af.ChiSoChatLuongId END) AS TongThang,
    COUNT(DISTINCT CASE WHEN af.TanSuatBaoCao = 3 AND rb.HasSubmitted = 1 THEN af.ChiSoChatLuongId END) AS DaGuiThang,
    COUNT(DISTINCT CASE WHEN af.TanSuatBaoCao = 4 THEN af.ChiSoChatLuongId END) AS TongQuy,
    COUNT(DISTINCT CASE WHEN af.TanSuatBaoCao = 4 AND rb.HasSubmitted = 1 THEN af.ChiSoChatLuongId END) AS DaGuiQuy,
    COUNT(DISTINCT CASE WHEN af.TanSuatBaoCao = 6 THEN af.ChiSoChatLuongId END) AS TongNam,
    COUNT(DISTINCT CASE WHEN af.TanSuatBaoCao = 6 AND rb.HasSubmitted = 1 THEN af.ChiSoChatLuongId END) AS DaGuiNam
FROM dbo.KhoaPhong kp
LEFT JOIN AssignmentFrequency af ON af.KhoaPhongId = kp.KhoaPhongId
LEFT JOIN ReportByIndicator rb ON rb.KhoaPhongId = af.KhoaPhongId AND rb.ChiSoChatLuongId = af.ChiSoChatLuongId
WHERE kp.Used = 1
  AND (@KhoaPhongId IS NULL OR kp.KhoaPhongId = @KhoaPhongId)
GROUP BY kp.TenKhoaPhong
ORDER BY kp.TenKhoaPhong";

            var progressItems = Query(sql, r => new DepartmentProgressViewModel
            {
                TenKhoaPhong = String(r, "TenKhoaPhong"),
                Tong = Int(r, "Tong"),
                DaGui = Int(r, "DaGui"),
                LuuNhap = Int(r, "LuuNhap"),
                TongThang = Int(r, "TongThang"),
                DaGuiThang = Int(r, "DaGuiThang"),
                TongQuy = Int(r, "TongQuy"),
                DaGuiQuy = Int(r, "DaGuiQuy"),
                TongNam = Int(r, "TongNam"),
                DaGuiNam = Int(r, "DaGuiNam")
            },
                Param("@KhoaPhongId", departmentId),
                Param("@TanSuat", tanSuatFilter),
                Param("@DraftPeriodStatus", (byte)TrangThaiKyBaoCao.Nhap));

            foreach (var progress in progressItems)
            {
                progress.XepLoai = CalculateXepLoai(progress.DaGui, progress.Tong);
                progress.XepLoaiThang = CalculateXepLoai(progress.DaGuiThang, progress.TongThang);
                progress.XepLoaiQuy = CalculateXepLoai(progress.DaGuiQuy, progress.TongQuy);
                progress.XepLoaiNam = CalculateXepLoai(progress.DaGuiNam, progress.TongNam);
            }

            return progressItems;
        }

        private class DashboardSummaryRow
        {
            public int TongBaoCaoCanNop { get; set; }
            public int TongBaoCaoCanNopKyDangMo { get; set; }
            public int BaoCaoDaGui { get; set; }
            public int ChiSoDuocPhanCong { get; set; }
            public int TongChiSo { get; set; }
            public int BaoCaoQuaHan { get; set; }
        }
    }
}
