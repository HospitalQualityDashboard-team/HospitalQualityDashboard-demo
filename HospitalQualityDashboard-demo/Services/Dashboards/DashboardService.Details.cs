// Mục đích: truy vấn dữ liệu chi tiết phục vụ drill-down từ các chỉ số trên dashboard.
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
        private IList<DashboardMetricDetailViewModel> GetAdminMetricDetails(int? tanSuatFilter)
        {
            return GetMetricDetails(null, tanSuatFilter);
        }

        private IList<DashboardMetricDetailViewModel> GetUserMetricDetails(int departmentId, int? tanSuatFilter)
        {
            return GetMetricDetails(departmentId, tanSuatFilter);
        }

        private IList<DashboardMetricDetailViewModel> GetMetricDetails(int? departmentId, int? tanSuatFilter)
        {
            const string sql = @"
WITH ExpectedSlots AS
(
    SELECT DISTINCT ky.KyBaoCaoId, ky.TenKyBaoCao, ky.HanNop,
           pc.KhoaPhongId, kp.TenKhoaPhong, pc.ChiSoChatLuongId,
           cs.MaChiSo, cs.TenChiSo
    FROM dbo.KyBaoCao ky
    INNER JOIN dbo.PhanCongChiSo pc ON pc.DangHoatDong = 1
    INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = pc.KhoaPhongId
    INNER JOIN dbo.ChiSoChatLuong cs
        ON cs.ChiSoChatLuongId = pc.ChiSoChatLuongId
    INNER JOIN dbo.ChiSoTanSuatBaoCao ts
        ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId
       AND ts.TanSuatBaoCao = ky.LoaiKyBaoCao
    WHERE ky.TrangThai <> @DraftPeriodStatus
      AND (@TanSuat IS NULL OR ky.LoaiKyBaoCao = @TanSuat)
      AND (@KhoaPhongId IS NULL OR pc.KhoaPhongId = @KhoaPhongId)
      AND dbo.fn_ChiSoDuocTrienKhaiTrongKy(pc.ChiSoChatLuongId, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay) = 1
)
SELECT es.KyBaoCaoId, es.KhoaPhongId, es.ChiSoChatLuongId,
       es.TenKyBaoCao, es.HanNop, es.TenKhoaPhong,
       es.MaChiSo, es.TenChiSo, bc.BaoCaoId, bc.TrangThai,
       ct.KetQua, ct.DatMucTieu,
       CASE
           WHEN bc.TrangThai IN (@DaGuiStatus, @QuaHanStatus, @DaKhoaStatus, @DaDuyetStatus) THEN 1
           ELSE 0
       END AS IsSubmitted,
       CASE
           WHEN (bc.BaoCaoId IS NULL OR bc.TrangThai NOT IN (@DaGuiStatus, @QuaHanStatus, @DaKhoaStatus, @DaDuyetStatus))
                AND es.HanNop < @Today THEN 1
           ELSE 0
       END AS IsOverdueMissing,
       CASE WHEN EXISTS (
           SELECT 1
           FROM dbo.ThongBaoTuDongLog warningLog
           WHERE warningLog.LoaiThongBao = @NhacHan
             AND warningLog.KyBaoCaoId = es.KyBaoCaoId
             AND warningLog.KhoaPhongId = es.KhoaPhongId
             AND warningLog.ChiSoChatLuongId = es.ChiSoChatLuongId
             AND warningLog.NgayMoc = @Today
       ) THEN 1 ELSE 0 END AS HasWarningToday
FROM ExpectedSlots es
LEFT JOIN dbo.BaoCao bc
    ON bc.KyBaoCaoId = es.KyBaoCaoId
   AND bc.KhoaPhongId = es.KhoaPhongId
   AND bc.ChiSoChatLuongId = es.ChiSoChatLuongId
LEFT JOIN dbo.BaoCaoChiTiet ct ON ct.BaoCaoId = bc.BaoCaoId
ORDER BY es.HanNop, es.TenKhoaPhong, es.MaChiSo";

            return Query(sql, reader =>
            {
                var isSubmitted = Int(reader, "IsSubmitted") == 1;
                return new DashboardMetricDetailViewModel
                {
                    KyBaoCaoId = Int(reader, "KyBaoCaoId"),
                    KhoaPhongId = Int(reader, "KhoaPhongId"),
                    ChiSoChatLuongId = Int(reader, "ChiSoChatLuongId"),
                    BaoCaoId = reader.IsDBNull(reader.GetOrdinal("BaoCaoId"))
                        ? (int?)null
                        : Int(reader, "BaoCaoId"),
                    TenKyBaoCao = String(reader, "TenKyBaoCao"),
                    HanNop = reader.GetDateTime(reader.GetOrdinal("HanNop")),
                    TenKhoaPhong = String(reader, "TenKhoaPhong"),
                    MaChiSo = String(reader, "MaChiSo"),
                    TenChiSo = String(reader, "TenChiSo"),
                    KetQua = isSubmitted ? NullableDecimal(reader, "KetQua") : null,
                    DatMucTieu = isSubmitted && !reader.IsDBNull(reader.GetOrdinal("DatMucTieu"))
                        ? (bool?)reader.GetBoolean(reader.GetOrdinal("DatMucTieu"))
                        : null,
                    TrangThaiBaoCao = reader.IsDBNull(reader.GetOrdinal("TrangThai"))
                        ? (TrangThaiBaoCao?)null
                        : (TrangThaiBaoCao)Convert.ToByte(reader["TrangThai"]),
                    IsSubmitted = isSubmitted,
                    IsOverdueMissing = Int(reader, "IsOverdueMissing") == 1,
                    HasWarningToday = Int(reader, "HasWarningToday") == 1
                };
            },
                Param("@KhoaPhongId", departmentId),
                Param("@TanSuat", tanSuatFilter),
                Param("@Today", GetVietnamLocalNow().Date),
                Param("@DraftPeriodStatus", (byte)TrangThaiKyBaoCao.Nhap),
                Param("@DaGuiStatus", (byte)TrangThaiBaoCao.DaGui),
                Param("@QuaHanStatus", (byte)TrangThaiBaoCao.QuaHan),
                Param("@DaKhoaStatus", (byte)TrangThaiBaoCao.DaKhoa),
                Param("@DaDuyetStatus", (byte)TrangThaiBaoCao.DaDuyet),
                Param("@NhacHan", (byte)LoaiThongBao.NhacHan));
        }

    }
}
