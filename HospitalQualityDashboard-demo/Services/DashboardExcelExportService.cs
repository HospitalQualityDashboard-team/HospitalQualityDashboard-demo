// Mục đích: truy vấn dữ liệu Dashboard, tạo workbook nhiều sheet và ghi lịch sử xuất có kiểm soát quyền.
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
    public class DashboardExcelExportService : DbServiceBase
    {
        private const string ReportType = "DashboardChiSoChatLuong";
        private const string HospitalName = "Bệnh viện";

        // Tạo nội dung xuất dữ liệu theo bộ lọc và phạm vi được phép.
        public DashboardExcelExportResultDto BuildDashboardExcel(DashboardExcelExportQueryDto query, ExportUserContextDto userContext)
        {
            if (userContext == null || userContext.TaiKhoanId <= 0)
            {
                throw new InvalidOperationException("Không xác định được người xuất báo cáo.");
            }

            // Chuẩn hóa phạm vi trước khi truy vấn để User không thể xuất dữ liệu của khoa/phòng khác.
            var effectiveQuery = NormalizeQuery(query, userContext);
            var details = QueryDetailRows(effectiveQuery);
            var missing = QueryMissingRows(effectiveQuery);
            var failed = details.Where(x => x.DatMucTieu == false).ToList();
            var byDepartment = BuildDepartmentSummary(details, missing);
            var reviewHistory = QueryReviewHistory(effectiveQuery);
            var fileName = BuildFileName(effectiveQuery, userContext);

            var content = CreateWorkbook(effectiveQuery, userContext, details, byDepartment, missing, failed, reviewHistory);
            EnsureExportHistoryTable();
            LogExportHistory(effectiveQuery, userContext, fileName, details.Count);

            return new DashboardExcelExportResultDto
            {
                Content = content,
                FileName = fileName,
                RowCount = details.Count
            };
        }

        // Chuẩn hóa dữ liệu đầu vào trước khi dùng cho workbook Dashboard và lịch sử xuất.
        private DashboardExcelExportQueryDto NormalizeQuery(DashboardExcelExportQueryDto query, ExportUserContextDto userContext)
        {
            query = query ?? new DashboardExcelExportQueryDto();
            var normalized = new DashboardExcelExportQueryDto
            {
                NamBaoCao = query.NamBaoCao,
                KyBaoCaoId = query.KyBaoCaoId,
                TanSuat = query.TanSuat,
                KhoaPhongId = userContext.IsAdmin ? query.KhoaPhongId : userContext.KhoaPhongId,
                LinhVuc = string.IsNullOrWhiteSpace(query.LinhVuc) ? null : query.LinhVuc.Trim(),
                TrangThaiNhapLieu = query.TrangThaiNhapLieu,
                TrangThaiDuyet = query.TrangThaiDuyet,
                DatMucTieu = query.DatMucTieu
            };

            if (!userContext.IsAdmin && !normalized.KhoaPhongId.HasValue)
            {
                throw new InvalidOperationException("Tài khoản User chưa được gắn khoa/phòng.");
            }

            return normalized;
        }

        // Truy vấn workbook Dashboard và lịch sử xuất theo điều kiện được cung cấp.
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

        // Truy vấn workbook Dashboard và lịch sử xuất theo điều kiện được cung cấp.
        private IList<DashboardMissingIndicatorRow> QueryMissingRows(DashboardExcelExportQueryDto query)
        {
            // Một chỉ số được xem là thiếu khi có phân công phù hợp tần suất nhưng chưa có báo cáo ở trạng thái đã nộp.
            const string sql = @"
SELECT
    ky.TenKyBaoCao,
    ky.HanNop,
    cs.MaChiSo,
    cs.TenChiSo,
    kp.TenKhoaPhong,
    cs.LinhVucApDung,
    DATEDIFF(day, @Today, ky.HanNop) AS DaysUntilDue
FROM dbo.KyBaoCao ky
INNER JOIN dbo.PhanCongChiSo pc ON pc.DangHoatDong = 1
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = pc.KhoaPhongId
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = pc.ChiSoChatLuongId AND cs.DangHoatDong = 1
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
ORDER BY ky.HanNop, kp.TenKhoaPhong, cs.MaChiSo";

            var rows = Query(sql, reader =>
            {
                var daysUntilDue = Int(reader, "DaysUntilDue");
                return new DashboardMissingIndicatorRow
                {
                    TenKyBaoCao = String(reader, "TenKyBaoCao"),
                    HanNop = reader.GetDateTime(reader.GetOrdinal("HanNop")),
                    MaChiSo = String(reader, "MaChiSo"),
                    TenChiSo = String(reader, "TenChiSo"),
                    TenKhoaPhong = String(reader, "TenKhoaPhong"),
                    LinhVuc = String(reader, "LinhVucApDung"),
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

        // Truy vấn workbook Dashboard và lịch sử xuất theo điều kiện được cung cấp.
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

        // Tạo cấu trúc dữ liệu phục vụ workbook Dashboard và lịch sử xuất.
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

        // Tạo cấu trúc dữ liệu phục vụ workbook Dashboard và lịch sử xuất.
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

        // Tạo cấu trúc dữ liệu phục vụ workbook Dashboard và lịch sử xuất.
        private byte[] CreateWorkbook(
            DashboardExcelExportQueryDto query,
            ExportUserContextDto userContext,
            IList<DashboardExcelDetailRow> details,
            IList<DashboardDepartmentSummaryRow> byDepartment,
            IList<DashboardMissingIndicatorRow> missing,
            IList<DashboardExcelDetailRow> failed,
            IList<DashboardReviewHistoryRow> reviewHistory)
        {
            // Tách nhóm dữ liệu thành các sheet để hỗ trợ cả tổng quan và đối soát chi tiết.
            using (var workbook = new XLWorkbook())
            {
                AddSummarySheet(workbook, query, userContext, details, byDepartment, missing, failed);
                AddDetailSheet(workbook, "ChiTietChiSo", query, userContext, details);
                AddDepartmentSheet(workbook, query, userContext, byDepartment);
                AddMissingSheet(workbook, query, userContext, missing);
                AddFailedSheet(workbook, query, userContext, failed);

                if (reviewHistory.Any())
                {
                    AddReviewHistorySheet(workbook, query, userContext, reviewHistory);
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        // Bổ sung dữ liệu mới phục vụ workbook Dashboard và lịch sử xuất.
        private void AddSummarySheet(
            XLWorkbook workbook,
            DashboardExcelExportQueryDto query,
            ExportUserContextDto userContext,
            IList<DashboardExcelDetailRow> details,
            IList<DashboardDepartmentSummaryRow> byDepartment,
            IList<DashboardMissingIndicatorRow> missing,
            IList<DashboardExcelDetailRow> failed)
        {
            var worksheet = workbook.Worksheets.Add("TongQuan");
            var row = AddMetadata(worksheet, query, userContext, "Tổng quan Dashboard chỉ số chất lượng");
            var total = details.Count + missing.Count;
            var metrics = new List<SummaryMetricRow>
            {
                new SummaryMetricRow("Tổng chỉ số", total),
                new SummaryMetricRow("Đã nhập", details.Count),
                new SummaryMetricRow("Chưa nhập", missing.Count),
                new SummaryMetricRow("Đã gửi/đã khóa/đã duyệt", byDepartment.Sum(x => x.DaGui)),
                new SummaryMetricRow("Quá hạn", byDepartment.Sum(x => x.QuaHan)),
                new SummaryMetricRow("Đạt mục tiêu", details.Count(x => x.DatMucTieu == true)),
                new SummaryMetricRow("Chưa đạt mục tiêu", failed.Count),
                new SummaryMetricRow("Tỷ lệ hoàn tất (%)", total > 0 ? Math.Round((decimal)details.Count * 100 / total, 2) : 0)
            };

            WriteTable(worksheet, row, metrics, new[]
            {
                new ExcelColumn<SummaryMetricRow>("Chỉ tiêu", x => x.Label),
                new ExcelColumn<SummaryMetricRow>("Giá trị", x => x.Value)
            });
        }

        // Bổ sung dữ liệu mới phục vụ workbook Dashboard và lịch sử xuất.
        private void AddDetailSheet(XLWorkbook workbook, string sheetName, DashboardExcelExportQueryDto query, ExportUserContextDto userContext, IList<DashboardExcelDetailRow> rows)
        {
            var worksheet = workbook.Worksheets.Add(sheetName);
            var row = AddMetadata(worksheet, query, userContext, "Chi tiết chỉ số chất lượng");
            WriteTable(worksheet, row, rows, new[]
            {
                new ExcelColumn<DashboardExcelDetailRow>("STT", x => x.STT),
                new ExcelColumn<DashboardExcelDetailRow>("Mã chỉ số", x => x.MaChiSo),
                new ExcelColumn<DashboardExcelDetailRow>("Tên chỉ số", x => x.TenChiSo),
                new ExcelColumn<DashboardExcelDetailRow>("Khoa/phòng phụ trách", x => x.TenKhoaPhong),
                new ExcelColumn<DashboardExcelDetailRow>("Lĩnh vực", x => x.LinhVuc),
                new ExcelColumn<DashboardExcelDetailRow>("Tử số", x => x.TuSo),
                new ExcelColumn<DashboardExcelDetailRow>("Mẫu số", x => x.MauSo),
                new ExcelColumn<DashboardExcelDetailRow>("Kết quả", x => x.KetQua.HasValue ? (object)Math.Round(x.KetQua.Value, 2, MidpointRounding.AwayFromZero) : null),
                new ExcelColumn<DashboardExcelDetailRow>("Đơn vị tính", x => x.DonViTinh),
                new ExcelColumn<DashboardExcelDetailRow>("Mục tiêu", x => x.MucTieu),
                new ExcelColumn<DashboardExcelDetailRow>("Đánh giá đạt/chưa đạt", x => x.DanhGiaDatMucTieu),
                new ExcelColumn<DashboardExcelDetailRow>("Trạng thái nhập liệu", x => x.TrangThaiNhapLieu),
                new ExcelColumn<DashboardExcelDetailRow>("Trạng thái duyệt", x => x.TrangThaiDuyet),
                new ExcelColumn<DashboardExcelDetailRow>("Người nhập", x => x.NguoiNhap),
                new ExcelColumn<DashboardExcelDetailRow>("Ngày nhập", x => FormatExcelDateTime(x.NgayNhap)),
                new ExcelColumn<DashboardExcelDetailRow>("Người duyệt", x => x.NguoiDuyet),
                new ExcelColumn<DashboardExcelDetailRow>("Ngày duyệt", x => FormatExcelDateTime(x.NgayDuyet)),
                new ExcelColumn<DashboardExcelDetailRow>("Ghi chú", x => x.GhiChu)
            }, ApplyDetailRowStyle);
        }

        // Bổ sung dữ liệu mới phục vụ workbook Dashboard và lịch sử xuất.
        private void AddDepartmentSheet(XLWorkbook workbook, DashboardExcelExportQueryDto query, ExportUserContextDto userContext, IList<DashboardDepartmentSummaryRow> rows)
        {
            var worksheet = workbook.Worksheets.Add("TheoKhoaPhong");
            var row = AddMetadata(worksheet, query, userContext, "Tổng hợp theo khoa/phòng");
            WriteTable(worksheet, row, rows, new[]
            {
                new ExcelColumn<DashboardDepartmentSummaryRow>("Khoa/phòng", x => x.TenKhoaPhong),
                new ExcelColumn<DashboardDepartmentSummaryRow>("Tổng chỉ số", x => x.TongChiSo),
                new ExcelColumn<DashboardDepartmentSummaryRow>("Đã nhập", x => x.DaNhap),
                new ExcelColumn<DashboardDepartmentSummaryRow>("Chưa nhập", x => x.ChuaNhap),
                new ExcelColumn<DashboardDepartmentSummaryRow>("Đã gửi", x => x.DaGui),
                new ExcelColumn<DashboardDepartmentSummaryRow>("Quá hạn", x => x.QuaHan),
                new ExcelColumn<DashboardDepartmentSummaryRow>("Đạt mục tiêu", x => x.DatMucTieu),
                new ExcelColumn<DashboardDepartmentSummaryRow>("Chưa đạt mục tiêu", x => x.ChuaDatMucTieu),
                new ExcelColumn<DashboardDepartmentSummaryRow>("Tỷ lệ hoàn tất (%)", x => x.TyLeHoanTat)
            });
        }

        // Bổ sung dữ liệu mới phục vụ workbook Dashboard và lịch sử xuất.
        private void AddMissingSheet(XLWorkbook workbook, DashboardExcelExportQueryDto query, ExportUserContextDto userContext, IList<DashboardMissingIndicatorRow> rows)
        {
            var worksheet = workbook.Worksheets.Add("ChiSoChuaNhap");
            var row = AddMetadata(worksheet, query, userContext, "Chỉ số chưa nhập");
            WriteTable(worksheet, row, rows, new[]
            {
                new ExcelColumn<DashboardMissingIndicatorRow>("STT", x => x.STT),
                new ExcelColumn<DashboardMissingIndicatorRow>("Kỳ báo cáo", x => x.TenKyBaoCao),
                new ExcelColumn<DashboardMissingIndicatorRow>("Hạn nộp", x => x.HanNop),
                new ExcelColumn<DashboardMissingIndicatorRow>("Mã chỉ số", x => x.MaChiSo),
                new ExcelColumn<DashboardMissingIndicatorRow>("Tên chỉ số", x => x.TenChiSo),
                new ExcelColumn<DashboardMissingIndicatorRow>("Khoa/phòng", x => x.TenKhoaPhong),
                new ExcelColumn<DashboardMissingIndicatorRow>("Lĩnh vực", x => x.LinhVuc),
                new ExcelColumn<DashboardMissingIndicatorRow>("Trạng thái", x => x.TrangThai)
            }, ApplyMissingRowStyle);
        }

        // Bổ sung dữ liệu mới phục vụ workbook Dashboard và lịch sử xuất.
        private void AddFailedSheet(XLWorkbook workbook, DashboardExcelExportQueryDto query, ExportUserContextDto userContext, IList<DashboardExcelDetailRow> rows)
        {
            AddDetailSheet(workbook, "ChiSoChuaDat", query, userContext, rows);
        }

        // Bổ sung dữ liệu mới phục vụ workbook Dashboard và lịch sử xuất.
        private void AddReviewHistorySheet(XLWorkbook workbook, DashboardExcelExportQueryDto query, ExportUserContextDto userContext, IList<DashboardReviewHistoryRow> rows)
        {
            var worksheet = workbook.Worksheets.Add("LichSuDuyet");
            var row = AddMetadata(worksheet, query, userContext, "Lịch sử duyệt báo cáo");
            WriteTable(worksheet, row, rows, new[]
            {
                new ExcelColumn<DashboardReviewHistoryRow>("STT", x => x.STT),
                new ExcelColumn<DashboardReviewHistoryRow>("Kỳ báo cáo", x => x.TenKyBaoCao),
                new ExcelColumn<DashboardReviewHistoryRow>("Khoa/phòng", x => x.TenKhoaPhong),
                new ExcelColumn<DashboardReviewHistoryRow>("Mã chỉ số", x => x.MaChiSo),
                new ExcelColumn<DashboardReviewHistoryRow>("Tên chỉ số", x => x.TenChiSo),
                new ExcelColumn<DashboardReviewHistoryRow>("Hành động", x => x.HanhDong),
                new ExcelColumn<DashboardReviewHistoryRow>("Người thực hiện", x => x.NguoiThucHien),
                new ExcelColumn<DashboardReviewHistoryRow>("Thời gian", x => x.ThoiGian),
                new ExcelColumn<DashboardReviewHistoryRow>("Nội dung", x => x.NoiDung)
            });
        }

        // Bổ sung dữ liệu mới phục vụ workbook Dashboard và lịch sử xuất.
        private int AddMetadata(IXLWorksheet worksheet, DashboardExcelExportQueryDto query, ExportUserContextDto userContext, string reportTitle)
        {
            worksheet.Cell(1, 1).Value = HospitalName;
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(2, 1).Value = reportTitle;
            worksheet.Cell(2, 1).Style.Font.Bold = true;
            worksheet.Cell(2, 1).Style.Font.FontSize = 14;

            worksheet.Cell(4, 1).Value = "Tên báo cáo";
            worksheet.Cell(4, 2).Value = "Dashboard chỉ số chất lượng bệnh viện";
            worksheet.Cell(5, 1).Value = "Kỳ báo cáo";
            worksheet.Cell(5, 2).Value = BuildFilterDescription(query);
            worksheet.Cell(6, 1).Value = "Khoa/phòng";
            worksheet.Cell(6, 2).Value = query.KhoaPhongId.HasValue ? GetDepartmentName(query.KhoaPhongId.Value) : (userContext.IsAdmin ? "Toàn viện" : userContext.TenKhoaPhong);
            worksheet.Cell(7, 1).Value = "Ngày xuất file";
            worksheet.Cell(7, 2).Value = GetVietnamLocalNow();
            worksheet.Cell(8, 1).Value = "Người xuất file";
            worksheet.Cell(8, 2).Value = userContext.TenDangNhap;
            worksheet.Range(4, 1, 8, 1).Style.Font.Bold = true;
            worksheet.Range(4, 1, 8, 2).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            worksheet.Range(4, 1, 8, 2).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            return 10;
        }

        // Ghi dữ liệu đã chuẩn hóa vào đầu ra của workbook Dashboard và lịch sử xuất.
        private void WriteTable<T>(IXLWorksheet worksheet, int startRow, IList<T> rows, IList<ExcelColumn<T>> columns, Action<IXLRow, T> rowStyle = null)
        {
            for (var i = 0; i < columns.Count; i++)
            {
                var cell = worksheet.Cell(startRow, i + 1);
                cell.Value = columns[i].Header;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#DDEBF7");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            if (rows == null || rows.Count == 0)
            {
                worksheet.Cell(startRow + 1, 1).Value = "Không có dữ liệu phù hợp bộ lọc.";
                worksheet.Range(startRow + 1, 1, startRow + 1, Math.Max(1, columns.Count)).Merge();
                worksheet.Cell(startRow + 1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF2CC");
            }
            else
            {
                for (var r = 0; r < rows.Count; r++)
                {
                    var row = rows[r];
                    var worksheetRow = worksheet.Row(startRow + r + 1);
                    for (var c = 0; c < columns.Count; c++)
                    {
                        SetCellValue(worksheet.Cell(startRow + r + 1, c + 1), columns[c].Value(row));
                    }

                    if (rowStyle != null)
                    {
                        rowStyle(worksheetRow, row);
                    }
                }
            }

            var lastRow = startRow + Math.Max(rows == null ? 0 : rows.Count, 1);
            var range = worksheet.Range(startRow, 1, lastRow, Math.Max(1, columns.Count));
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.SheetView.FreezeRows(startRow);
            worksheet.Columns().AdjustToContents();
        }

        // Kiểm tra và cập nhật dữ liệu của workbook Dashboard và lịch sử xuất.
        private static void SetCellValue(IXLCell cell, object value)
        {
            if (value == null)
            {
                cell.Value = string.Empty;
                return;
            }

            if (value is DateTime)
            {
                cell.Value = (DateTime)value;
                cell.Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                return;
            }

            if (value is decimal)
            {
                cell.Value = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                cell.Style.NumberFormat.Format = "#,##0.00";
                return;
            }

            if (value is int)
            {
                cell.Value = (int)value;
                return;
            }

            cell.Value = Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        // Định dạng giá trị theo quy ước hiển thị của workbook Dashboard và lịch sử xuất.
        private static string FormatExcelDateTime(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) : string.Empty;
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

        // Áp dụng định dạng hoặc quy tắc trình bày cho workbook Dashboard và lịch sử xuất.
        private static void ApplyDetailRowStyle(IXLRow row, DashboardExcelDetailRow item)
        {
            if (item.DatMucTieu == true)
            {
                row.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2F0D9");
            }
            else if (item.DatMucTieu == false)
            {
                row.Style.Fill.BackgroundColor = XLColor.FromHtml("#FCE4D6");
            }
            else if (item.TrangThaiNhapLieu == "Nháp")
            {
                row.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF2CC");
            }
            else if (item.TrangThaiNhapLieu == "Quá hạn")
            {
                row.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8CBAD");
            }
        }

        // Áp dụng định dạng hoặc quy tắc trình bày cho workbook Dashboard và lịch sử xuất.
        private static void ApplyMissingRowStyle(IXLRow row, DashboardMissingIndicatorRow item)
        {
            row.Style.Fill.BackgroundColor = item.TrangThai.Contains("Quá hạn")
                ? XLColor.FromHtml("#F8CBAD")
                : XLColor.FromHtml("#FFF2CC");
        }

        // Kiểm tra các điều kiện hợp lệ trước khi tiếp tục xử lý workbook Dashboard và lịch sử xuất.
        private void EnsureExportHistoryTable()
        {
            // Giữ khả năng tương thích với database cũ chưa chạy script bổ sung lịch sử xuất.
            Execute(@"
IF OBJECT_ID('dbo.LichSuXuatBaoCao', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LichSuXuatBaoCao (
        LichSuXuatBaoCaoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LichSuXuatBaoCao PRIMARY KEY,
        NguoiDungId INT NOT NULL,
        LoaiBaoCao NVARCHAR(100) NOT NULL,
        BoLoc NVARCHAR(MAX) NULL,
        TenFile NVARCHAR(255) NOT NULL,
        SoDongDuLieu INT NOT NULL CONSTRAINT DF_LichSuXuatBaoCao_SoDongDuLieu DEFAULT (0),
        NgayXuat DATETIME NOT NULL CONSTRAINT DF_LichSuXuatBaoCao_NgayXuat DEFAULT (GETDATE()),
        DiaChiIP NVARCHAR(45) NULL,
        VaiTro NVARCHAR(20) NULL,
        KhoaPhongId INT NULL,
        CONSTRAINT FK_LichSuXuatBaoCao_TaiKhoan FOREIGN KEY (NguoiDungId) REFERENCES dbo.TaiKhoan(TaiKhoanId),
        CONSTRAINT FK_LichSuXuatBaoCao_KhoaPhong FOREIGN KEY (KhoaPhongId) REFERENCES dbo.KhoaPhong(KhoaPhongId),
        CONSTRAINT CK_LichSuXuatBaoCao_SoDongDuLieu CHECK (SoDongDuLieu >= 0)
    );
END");
        }

        // Ghi lại thông tin phục vụ theo dõi và kiểm toán workbook Dashboard và lịch sử xuất.
        private void LogExportHistory(DashboardExcelExportQueryDto query, ExportUserContextDto userContext, string fileName, int rowCount)
        {
            // Bộ lọc được lưu dạng JSON để có thể tái dựng chính xác phạm vi của lần xuất.
            Execute(@"
INSERT INTO dbo.LichSuXuatBaoCao(NguoiDungId, LoaiBaoCao, BoLoc, TenFile, SoDongDuLieu, DiaChiIP, VaiTro, KhoaPhongId, NgayXuat)
VALUES(@NguoiDungId, @LoaiBaoCao, @BoLoc, @TenFile, @SoDongDuLieu, @DiaChiIP, @VaiTro, @KhoaPhongId, @NgayXuat)",
                Param("@NguoiDungId", userContext.TaiKhoanId),
                Param("@LoaiBaoCao", ReportType),
                Param("@BoLoc", JsonConvert.SerializeObject(query)),
                Param("@TenFile", fileName),
                Param("@SoDongDuLieu", rowCount),
                Param("@DiaChiIP", userContext.DiaChiIP),
                Param("@VaiTro", userContext.IsAdmin ? "Admin" : "User"),
                Param("@KhoaPhongId", query.KhoaPhongId),
                Param("@NgayXuat", GetVietnamLocalNow()));
        }

        // Tạo cấu trúc dữ liệu phục vụ workbook Dashboard và lịch sử xuất.
        private string BuildFileName(DashboardExcelExportQueryDto query, ExportUserContextDto userContext)
        {
            var scope = query.KhoaPhongId.HasValue
                ? GetDepartmentName(query.KhoaPhongId.Value)
                : (userContext.IsAdmin ? "ToanVien" : userContext.TenKhoaPhong);
            var period = BuildPeriodToken(query);
            return string.Format(CultureInfo.InvariantCulture, "Dashboard_{0}_{1}.xlsx", SanitizeFileToken(scope), period);
        }

        // Tạo cấu trúc dữ liệu phục vụ workbook Dashboard và lịch sử xuất.
        private string BuildPeriodToken(DashboardExcelExportQueryDto query)
        {
            if (query.KyBaoCaoId.HasValue)
            {
                var periodName = Convert.ToString(Scalar("SELECT TenKyBaoCao FROM dbo.KyBaoCao WHERE KyBaoCaoId=@Id", Param("@Id", query.KyBaoCaoId.Value)));
                if (!string.IsNullOrWhiteSpace(periodName))
                {
                    return SanitizeFileToken(periodName);
                }
            }

            var year = query.NamBaoCao.GetValueOrDefault(DateTime.Today.Year);
            if (query.TanSuat.HasValue)
            {
                return SanitizeFileToken(FormatTanSuatBaoCao((TanSuatBaoCao)query.TanSuat.Value)) + "_" + year.ToString(CultureInfo.InvariantCulture);
            }

            return year.ToString(CultureInfo.InvariantCulture);
        }

        // Tạo cấu trúc dữ liệu phục vụ workbook Dashboard và lịch sử xuất.
        private string BuildFilterDescription(DashboardExcelExportQueryDto query)
        {
            var parts = new List<string>();
            if (query.NamBaoCao.HasValue) parts.Add("Năm " + query.NamBaoCao.Value.ToString(CultureInfo.InvariantCulture));
            if (query.KyBaoCaoId.HasValue) parts.Add(GetPeriodName(query.KyBaoCaoId.Value));
            if (query.TanSuat.HasValue) parts.Add(FormatTanSuatBaoCao((TanSuatBaoCao)query.TanSuat.Value));
            if (!string.IsNullOrWhiteSpace(query.LinhVuc)) parts.Add("Lĩnh vực: " + query.LinhVuc);
            if (query.TrangThaiNhapLieu.HasValue) parts.Add("Trạng thái nhập liệu: " + FormatFilterStatus(query.TrangThaiNhapLieu.Value));
            if (query.TrangThaiDuyet.HasValue) parts.Add("Trạng thái duyệt: " + FormatFilterStatus(query.TrangThaiDuyet.Value));
            if (query.DatMucTieu.HasValue) parts.Add("Đánh giá: " + FormatDatMucTieu(query.DatMucTieu));
            return parts.Count == 0 ? "Tất cả dữ liệu" : string.Join("; ", parts);
        }

        // Truy vấn workbook Dashboard và lịch sử xuất theo điều kiện được cung cấp.
        private string GetDepartmentName(int departmentId)
        {
            var value = Convert.ToString(Scalar("SELECT TenKhoaPhong FROM dbo.KhoaPhong WHERE KhoaPhongId=@Id", Param("@Id", departmentId)));
            return string.IsNullOrWhiteSpace(value) ? "KhoaPhong" + departmentId.ToString(CultureInfo.InvariantCulture) : value;
        }

        // Truy vấn workbook Dashboard và lịch sử xuất theo điều kiện được cung cấp.
        private string GetPeriodName(int periodId)
        {
            var value = Convert.ToString(Scalar("SELECT TenKyBaoCao FROM dbo.KyBaoCao WHERE KyBaoCaoId=@Id", Param("@Id", periodId)));
            return string.IsNullOrWhiteSpace(value) ? "Kỳ báo cáo " + periodId.ToString(CultureInfo.InvariantCulture) : value;
        }

        // Chuẩn hóa dữ liệu đầu vào trước khi dùng cho workbook Dashboard và lịch sử xuất.
        private static string SanitizeFileToken(string value)
        {
            value = RemoveDiacritics(string.IsNullOrWhiteSpace(value) ? "BaoCao" : value);
            value = Regex.Replace(value, @"[^A-Za-z0-9]+", "_").Trim('_');
            return string.IsNullOrWhiteSpace(value) ? "BaoCao" : value;
        }

        // Loại bỏ dấu tiếng Việt để tạo chuỗi an toàn cho tên tệp.
        private static string RemoveDiacritics(string value)
        {
            var normalized = value.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();
            foreach (var ch in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                builder.Append(ch == 'đ' || ch == 'Đ' ? 'd' : ch);
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        // Định dạng giá trị theo quy ước hiển thị của workbook Dashboard và lịch sử xuất.
        private static string FormatMucTieu(string op, decimal? value, string description)
        {
            if (!string.IsNullOrWhiteSpace(description)) return description;
            if (string.IsNullOrWhiteSpace(op) || !value.HasValue) return string.Empty;
            return op.Trim() + " " + value.Value.ToString("0.####", CultureInfo.InvariantCulture);
        }

        // Định dạng giá trị theo quy ước hiển thị của workbook Dashboard và lịch sử xuất.
        private static string FormatDatMucTieu(bool? value)
        {
            if (!value.HasValue) return "Chưa đánh giá";
            return value.Value ? "Đạt" : "Chưa đạt";
        }

        // Định dạng giá trị theo quy ước hiển thị của workbook Dashboard và lịch sử xuất.
        private static string FormatFilterStatus(int status)
        {
            if (status == 0) return "Chưa nhập";
            return FormatTrangThaiBaoCao((TrangThaiBaoCao)status);
        }

        // Định dạng giá trị theo quy ước hiển thị của workbook Dashboard và lịch sử xuất.
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

        // Định dạng giá trị theo quy ước hiển thị của workbook Dashboard và lịch sử xuất.
        private static string FormatTrangThaiDuyet(TrangThaiBaoCao status)
        {
            switch (status)
            {
                case TrangThaiBaoCao.DaDuyet: return "Đã duyệt";
                case TrangThaiBaoCao.TraLai: return "Trả lại";
                case TrangThaiBaoCao.DaKhoa: return "Đã khóa";
                default: return "Chưa duyệt";
            }
        }

        // Định dạng giá trị theo quy ước hiển thị của workbook Dashboard và lịch sử xuất.
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

        private class ExcelColumn<T>
        {
            // Khởi tạo thành phần và các giá trị cần thiết cho dữ liệu nội bộ của ExcelColumn.
            public ExcelColumn(string header, Func<T, object> value)
            {
                Header = header;
                Value = value;
            }

            public string Header { get; private set; }
            public Func<T, object> Value { get; private set; }
        }

        private class SummaryMetricRow
        {
            // Khởi tạo thành phần và các giá trị cần thiết cho dữ liệu nội bộ của SummaryMetricRow.
            public SummaryMetricRow(string label, object value)
            {
                Label = label;
                Value = value;
            }

            public string Label { get; private set; }
            public object Value { get; private set; }
        }
    }
}
