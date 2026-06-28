// Mục đích: gom các truy vấn nguồn cho file Excel dashboard theo quyền và bộ lọc.
using ClosedXML.Excel;
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.Enums;
using HospitalQualityDashboardDemo.Models.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HospitalQualityDashboardDemo.Services
{
    public partial class DashboardExcelExportService
    {
        private IList<DashboardExcelDetailRow> QueryDetailRows(DashboardExcelExportQueryDto query)
        {
            // Ưu tiên mục tiêu đúng năm báo cáo; OUTER APPLY cung cấp mục tiêu gần nhất khi năm đó chưa cấu hình.
            const string sql = @"
SELECT
    bc.BaoCaoId,
    bc.KhoaPhongId,
    bc.ChiSoChatLuongId,
    kp.TenKhoaPhong,
    ky.TenKyBaoCao,
    ky.HanNop,
    DATEPART(YEAR, ky.TuNgay) AS NamBaoCao,
    ky.LoaiKyBaoCao,
    cs.MaChiSo,
    cs.TenChiSo,
    cs.LinhVucApDung,
    cs.DonViTinh,
    ct.TuSo,
    ct.MauSo,
    ct.KetQua,
    COALESCE(mt.ToanTuSoSanh, mtFallback.ToanTuSoSanh) AS ToanTuSoSanh,
    COALESCE(mt.GiaTriMucTieu, mtFallback.GiaTriMucTieu) AS GiaTriMucTieu,
    COALESCE(mt.MoTaMucTieu, mtFallback.MoTaMucTieu) AS MoTaMucTieu,
    ct.DatMucTieu,
    bc.TrangThai,
    bc.NgayGui,
    creator.TenDangNhap AS NguoiNhap,
    ISNULL(bc.NgayCapNhat, bc.NgayTao) AS NgayNhap,
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
LEFT JOIN dbo.TaiKhoan creator ON creator.TaiKhoanId = bc.NguoiTaoId
WHERE (@NamBaoCao IS NULL OR DATEPART(YEAR, ky.TuNgay) = @NamBaoCao)
  AND (@KyBaoCaoId IS NULL OR bc.KyBaoCaoId = @KyBaoCaoId)
  AND (@TanSuat IS NULL OR ky.LoaiKyBaoCao = @TanSuat)
  AND (@KhoaPhongId IS NULL OR bc.KhoaPhongId = @KhoaPhongId)
  AND (@LinhVuc IS NULL OR cs.LinhVucApDung = @LinhVuc)
  AND (@TrangThaiNhapLieu IS NULL OR (@TrangThaiNhapLieu > 0 AND bc.TrangThai = @TrangThaiNhapLieu))
  AND (@TrangThaiDuyet IS NULL OR bc.TrangThai = @TrangThaiDuyet)
  AND (@DatMucTieu IS NULL OR ct.DatMucTieu = @DatMucTieu)
ORDER BY kp.TenKhoaPhong, ky.TuNgay DESC, cs.MaChiSo";

            var rows = Query(sql, reader => new DashboardExcelDetailRow
            {
                BaoCaoId = Int(reader, "BaoCaoId"),
                KhoaPhongId = Int(reader, "KhoaPhongId"),
                ChiSoChatLuongId = Int(reader, "ChiSoChatLuongId"),
                TenKhoaPhong = String(reader, "TenKhoaPhong"),
                TenKyBaoCao = String(reader, "TenKyBaoCao"),
                NamBaoCao = Int(reader, "NamBaoCao"),
                TanSuatBaoCaoText = FormatTanSuatBaoCao((TanSuatBaoCao)Convert.ToByte(reader["LoaiKyBaoCao"])),
                MaChiSo = String(reader, "MaChiSo"),
                TenChiSo = String(reader, "TenChiSo"),
                LinhVuc = String(reader, "LinhVucApDung"),
                DonViTinh = String(reader, "DonViTinh"),
                TuSo = NullableDecimal(reader, "TuSo"),
                MauSo = NullableDecimal(reader, "MauSo"),
                KetQua = NullableDecimal(reader, "KetQua"),
                MucTieu = FormatMucTieu(String(reader, "ToanTuSoSanh"), NullableDecimal(reader, "GiaTriMucTieu"), String(reader, "MoTaMucTieu")),
                DatMucTieu = reader.IsDBNull(reader.GetOrdinal("DatMucTieu")) ? (bool?)null : reader.GetBoolean(reader.GetOrdinal("DatMucTieu")),
                TrangThaiNhapLieu = FormatTrangThaiBaoCao((TrangThaiBaoCao)Convert.ToByte(reader["TrangThai"])),
                TrangThaiDuyet = FormatTrangThaiDuyet((TrangThaiBaoCao)Convert.ToByte(reader["TrangThai"])),
                NguoiNhap = String(reader, "NguoiNhap"),
                NgayNhap = NullableDateTime(reader, "NgayNhap"),
                NgayGui = NullableDateTime(reader, "NgayGui"),
                HanNop = reader.GetDateTime(reader.GetOrdinal("HanNop")),
                GhiChu = String(reader, "GhiChu")
            }, BuildQueryParameters(query));

            var index = 1;
            foreach (var row in rows)
            {
                row.STT = index++;
                row.DanhGiaDatMucTieu = FormatDatMucTieu(row.DatMucTieu);
            }

            return rows;
        }

        // Truy vấn các báo cáo thiếu/quá hạn để đưa vào sheet cảnh báo trong workbook dashboard.
        private IList<DashboardMissingIndicatorRow> QueryMissingRows(DashboardExcelExportQueryDto query)
        {
            // Một chỉ số được xem là thiếu khi có phân công phù hợp tần suất nhưng chưa có báo cáo ở trạng thái đã nộp.
            const string sql = @"
SELECT
    ky.KyBaoCaoId,
    pc.KhoaPhongId,
    pc.ChiSoChatLuongId,
    ky.TenKyBaoCao,
    ky.HanNop,
    cs.MaChiSo,
    cs.TenChiSo,
    kp.TenKhoaPhong,
    cs.LinhVucApDung,
    cs.DonViTinh,
    DATEDIFF(day, @Today, ky.HanNop) AS DaysUntilDue
FROM dbo.KyBaoCao ky
INNER JOIN dbo.PhanCongChiSo pc ON pc.DangHoatDong = 1
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = pc.KhoaPhongId
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = pc.ChiSoChatLuongId
INNER JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId AND ts.TanSuatBaoCao = ky.LoaiKyBaoCao
LEFT JOIN dbo.BaoCao bc ON bc.KyBaoCaoId = ky.KyBaoCaoId
    AND bc.KhoaPhongId = pc.KhoaPhongId
    AND bc.ChiSoChatLuongId = pc.ChiSoChatLuongId
    AND bc.TrangThai IN (@DaGui, @QuaHan, @DaKhoa, @DaDuyet)
WHERE ky.TrangThai <> @DraftPeriodStatus
  AND bc.BaoCaoId IS NULL
  AND (@NamBaoCao IS NULL OR DATEPART(YEAR, ky.TuNgay) = @NamBaoCao)
  AND (@KyBaoCaoId IS NULL OR ky.KyBaoCaoId = @KyBaoCaoId)
  AND (@TanSuat IS NULL OR ky.LoaiKyBaoCao = @TanSuat)
  AND (@KhoaPhongId IS NULL OR pc.KhoaPhongId = @KhoaPhongId)
  AND (@LinhVuc IS NULL OR cs.LinhVucApDung = @LinhVuc)
  AND (@TrangThaiNhapLieu IS NULL OR @TrangThaiNhapLieu = 0)
  AND @TrangThaiDuyet IS NULL
  AND @DatMucTieu IS NULL
  AND dbo.fn_ChiSoDuocTrienKhaiTrongKy(pc.ChiSoChatLuongId, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay) = 1
ORDER BY ky.HanNop, kp.TenKhoaPhong, cs.MaChiSo";

            var rows = Query(sql, reader =>
            {
                var daysUntilDue = Int(reader, "DaysUntilDue");
                return new DashboardMissingIndicatorRow
                {
                    KyBaoCaoId = Int(reader, "KyBaoCaoId"),
                    KhoaPhongId = Int(reader, "KhoaPhongId"),
                    ChiSoChatLuongId = Int(reader, "ChiSoChatLuongId"),
                    TenKyBaoCao = String(reader, "TenKyBaoCao"),
                    HanNop = reader.GetDateTime(reader.GetOrdinal("HanNop")),
                    MaChiSo = String(reader, "MaChiSo"),
                    TenChiSo = String(reader, "TenChiSo"),
                    TenKhoaPhong = String(reader, "TenKhoaPhong"),
                    LinhVuc = String(reader, "LinhVucApDung"),
                    DonViTinh = String(reader, "DonViTinh"),
                    TrangThai = daysUntilDue < 0 ? "Quá hạn chưa nhập" : "Chưa nhập"
                };
            }, BuildQueryParameters(query));

            var index = 1;
            foreach (var row in rows)
            {
                row.STT = index++;
            }

            return rows;
        }

        // Truy vấn lịch sử duyệt/trả lại báo cáo để audit quy trình trong file xuất dashboard.
        private IList<DashboardReviewHistoryRow> QueryReviewHistory(DashboardExcelExportQueryDto query)
        {
            const string sql = @"
SELECT
    ky.TenKyBaoCao,
    kp.TenKhoaPhong,
    cs.MaChiSo,
    cs.TenChiSo,
    log.HanhDong,
    userLog.TenDangNhap AS NguoiThucHien,
    log.ThoiGian,
    log.NoiDung
FROM dbo.NhatKyHeThong log
INNER JOIN dbo.BaoCao bc ON bc.BaoCaoId = log.DoiTuongId AND log.DoiTuong = N'BaoCao'
INNER JOIN dbo.KyBaoCao ky ON ky.KyBaoCaoId = bc.KyBaoCaoId
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = bc.KhoaPhongId
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = bc.ChiSoChatLuongId
LEFT JOIN dbo.TaiKhoan userLog ON userLog.TaiKhoanId = log.TaiKhoanId
WHERE (log.HanhDong LIKE N'%Duyet%' OR log.HanhDong LIKE N'%TraLai%' OR bc.TrangThai IN (@DaDuyet, @TraLai))
  AND (@NamBaoCao IS NULL OR DATEPART(YEAR, ky.TuNgay) = @NamBaoCao)
  AND (@KyBaoCaoId IS NULL OR bc.KyBaoCaoId = @KyBaoCaoId)
  AND (@TanSuat IS NULL OR ky.LoaiKyBaoCao = @TanSuat)
  AND (@KhoaPhongId IS NULL OR bc.KhoaPhongId = @KhoaPhongId)
  AND (@LinhVuc IS NULL OR cs.LinhVucApDung = @LinhVuc)
  AND (@TrangThaiNhapLieu IS NULL OR (@TrangThaiNhapLieu > 0 AND bc.TrangThai = @TrangThaiNhapLieu))
  AND (@TrangThaiDuyet IS NULL OR bc.TrangThai = @TrangThaiDuyet)
  AND (@DatMucTieu IS NULL OR EXISTS (
      SELECT 1 FROM dbo.BaoCaoChiTiet ct WHERE ct.BaoCaoId = bc.BaoCaoId AND ct.DatMucTieu = @DatMucTieu
  ))
ORDER BY log.ThoiGian DESC";

            var rows = Query(sql, reader => new DashboardReviewHistoryRow
            {
                TenKyBaoCao = String(reader, "TenKyBaoCao"),
                TenKhoaPhong = String(reader, "TenKhoaPhong"),
                MaChiSo = String(reader, "MaChiSo"),
                TenChiSo = String(reader, "TenChiSo"),
                HanhDong = String(reader, "HanhDong"),
                NguoiThucHien = String(reader, "NguoiThucHien"),
                ThoiGian = reader.GetDateTime(reader.GetOrdinal("ThoiGian")),
                NoiDung = String(reader, "NoiDung")
            }, BuildQueryParameters(query));

            var index = 1;
            foreach (var row in rows)
            {
                row.STT = index++;
            }

            return rows;
        }

        // Quy đổi bộ lọc dashboard thành tham số SQL, bao gồm các mã trạng thái dùng trong truy vấn tổng hợp.
        private SqlParameter[] BuildQueryParameters(DashboardExcelExportQueryDto query)
        {
            return new[]
            {
                Param("@NamBaoCao", query.NamBaoCao.HasValue ? (object)query.NamBaoCao.Value : null),
                Param("@KyBaoCaoId", query.KyBaoCaoId.HasValue ? (object)query.KyBaoCaoId.Value : null),
                Param("@TanSuat", query.TanSuat.HasValue ? (object)query.TanSuat.Value : null),
                Param("@KhoaPhongId", query.KhoaPhongId.HasValue ? (object)query.KhoaPhongId.Value : null),
                Param("@LinhVuc", string.IsNullOrWhiteSpace(query.LinhVuc) ? null : query.LinhVuc),
                Param("@TrangThaiNhapLieu", query.TrangThaiNhapLieu.HasValue ? (object)query.TrangThaiNhapLieu.Value : null),
                Param("@TrangThaiDuyet", query.TrangThaiDuyet.HasValue ? (object)query.TrangThaiDuyet.Value : null),
                Param("@DatMucTieu", query.DatMucTieu.HasValue ? (object)query.DatMucTieu.Value : null),
                Param("@DaGui", (byte)TrangThaiBaoCao.DaGui),
                Param("@QuaHan", (byte)TrangThaiBaoCao.QuaHan),
                Param("@DaKhoa", (byte)TrangThaiBaoCao.DaKhoa),
                Param("@DaDuyet", (byte)TrangThaiBaoCao.DaDuyet),
                Param("@TraLai", (byte)TrangThaiBaoCao.TraLai),
                Param("@DraftPeriodStatus", (byte)TrangThaiKyBaoCao.Nhap),
                Param("@Today", GetVietnamLocalNow().Date)
            };
        }

        // Tổng hợp số lượng chỉ số đã nhập và còn thiếu theo từng khoa/phòng cho sheet tóm tắt.
        private IList<DashboardDepartmentSummaryRow> BuildDepartmentSummary(
            IList<DashboardExcelDetailRow> details,
            IList<DashboardMissingIndicatorRow> missing)
        {
            var names = details.Select(x => x.TenKhoaPhong)
                .Concat(missing.Select(x => x.TenKhoaPhong))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            return names.Select(name =>
            {
                var departmentDetails = details.Where(x => string.Equals(x.TenKhoaPhong, name, StringComparison.OrdinalIgnoreCase)).ToList();
                var departmentMissing = missing.Where(x => string.Equals(x.TenKhoaPhong, name, StringComparison.OrdinalIgnoreCase)).ToList();
                var total = departmentDetails.Count + departmentMissing.Count;
                return new DashboardDepartmentSummaryRow
                {
                    TenKhoaPhong = name,
                    TongChiSo = total,
                    DaNhap = departmentDetails.Count,
                    ChuaNhap = departmentMissing.Count,
                    DaGui = departmentDetails.Count(x => x.TrangThaiNhapLieu == "Đã gửi" || x.TrangThaiNhapLieu == "Đã khóa" || x.TrangThaiNhapLieu == "Đã duyệt"),
                    QuaHan = departmentDetails.Count(x => x.TrangThaiNhapLieu == "Quá hạn") + departmentMissing.Count(x => x.TrangThai.Contains("Quá hạn")),
                    DatMucTieu = departmentDetails.Count(x => x.DatMucTieu == true),
                    ChuaDatMucTieu = departmentDetails.Count(x => x.DatMucTieu == false),
                    TyLeHoanTat = total > 0 ? Math.Round((decimal)departmentDetails.Count * 100 / total, 2) : 0
                };
            }).ToList();
        }

    }
}
