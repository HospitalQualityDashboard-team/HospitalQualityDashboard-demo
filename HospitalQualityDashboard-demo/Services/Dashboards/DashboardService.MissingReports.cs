// Mục đích: phát hiện báo cáo còn thiếu hoặc quá hạn để cảnh báo trên dashboard.
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
        public IList<MissingReportAlertViewModel> GetMissingReportsForDepartment(int departmentId)
        {
            return GetMissingReportsForDepartment(departmentId, null, false, null);
        }

        // Xử lý chức năng báo cáo định kỳ của method GetMissingReportsForDepartment, giữ logic nghiệp vụ tập trung trong tầng phù hợp.
        public IList<MissingReportAlertViewModel> GetMissingReportsForDepartment(int departmentId, int? periodId, bool overdueOnly)
        {
            return GetMissingReportsForDepartment(departmentId, periodId, overdueOnly, null);
        }

        public IList<MissingReportAlertViewModel> GetMissingReportsForDepartment(
            int departmentId,
            int? periodId,
            bool overdueOnly,
            int? indicatorId)
        {
            const string sql = @"
SELECT ky.KyBaoCaoId, ky.TenKyBaoCao, ky.HanNop, cs.ChiSoChatLuongId, pc.PhanCongChiSoId,
       cs.MaChiSo, cs.TenChiSo, DATEDIFF(day, @Today, ky.HanNop) AS DaysUntilDue
FROM dbo.KyBaoCao ky
INNER JOIN dbo.PhanCongChiSo pc ON pc.DangHoatDong = 1
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = pc.ChiSoChatLuongId
INNER JOIN dbo.ChiSoTanSuatBaoCao cst ON cst.ChiSoChatLuongId = pc.ChiSoChatLuongId AND cst.TanSuatBaoCao = ky.LoaiKyBaoCao
LEFT JOIN dbo.BaoCao bc ON bc.KyBaoCaoId = ky.KyBaoCaoId
    AND bc.KhoaPhongId = pc.KhoaPhongId
    AND bc.ChiSoChatLuongId = pc.ChiSoChatLuongId
    AND bc.TrangThai IN (@DaGui, @QuaHan, @DaKhoa, @DaDuyet)
WHERE ky.TrangThai = @Mo
  AND pc.KhoaPhongId = @KhoaPhongId
  AND EXISTS (SELECT 1 FROM dbo.KhoaPhong kp WHERE kp.KhoaPhongId = pc.KhoaPhongId AND kp.Used = 1)
  AND (@KyBaoCaoId IS NULL OR ky.KyBaoCaoId = @KyBaoCaoId)
  AND (@ChiSoChatLuongId IS NULL OR cs.ChiSoChatLuongId = @ChiSoChatLuongId)
  AND (@OverdueOnly = 0 OR DATEDIFF(day, @Today, ky.HanNop) < 0)
  AND dbo.fn_ChiSoDuocTrienKhaiTrongKy(pc.ChiSoChatLuongId, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay) = 1
  AND bc.BaoCaoId IS NULL
ORDER BY ky.HanNop, cs.MaChiSo";

            return Query(sql, reader =>
            {
                var daysUntilDue = Int(reader, "DaysUntilDue");
                return new MissingReportAlertViewModel
                {
                    KyBaoCaoId = Int(reader, "KyBaoCaoId"),
                    TenKyBaoCao = String(reader, "TenKyBaoCao"),
                    HanNop = reader.GetDateTime(reader.GetOrdinal("HanNop")),
                    ChiSoChatLuongId = Int(reader, "ChiSoChatLuongId"),
                    PhanCongChiSoId = Int(reader, "PhanCongChiSoId"),
                    MaChiSo = String(reader, "MaChiSo"),
                    TenChiSo = String(reader, "TenChiSo"),
                    DaysUntilDue = daysUntilDue,
                    IsOverdue = daysUntilDue < 0,
                    IsDueSoon = daysUntilDue >= 0 && daysUntilDue <= 10
                };
            },
                Param("@DaGui", (byte)TrangThaiBaoCao.DaGui),
                Param("@QuaHan", (byte)TrangThaiBaoCao.QuaHan),
                Param("@DaKhoa", (byte)TrangThaiBaoCao.DaKhoa),
                Param("@DaDuyet", (byte)TrangThaiBaoCao.DaDuyet),
                Param("@Mo", (byte)TrangThaiKyBaoCao.Mo),
                Param("@KhoaPhongId", departmentId),
                Param("@KyBaoCaoId", periodId),
                Param("@ChiSoChatLuongId", indicatorId),
                Param("@OverdueOnly", overdueOnly ? 1 : 0),
                Param("@Today", GetVietnamLocalNow().Date));
        }

        // Lấy thời điểm hiện tại theo múi giờ Việt Nam và có phương án dự phòng.
        private static DateTime GetVietnamLocalNow()
        {
            try
            {
                var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
            }
            catch (TimeZoneNotFoundException)
            {
                return DateTime.Now;
            }
            catch (InvalidTimeZoneException)
            {
                return DateTime.Now;
            }
        }
    }
}
