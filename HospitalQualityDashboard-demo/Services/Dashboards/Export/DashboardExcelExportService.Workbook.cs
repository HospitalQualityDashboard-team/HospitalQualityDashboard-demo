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
        private byte[] CreateWorkbook(
            DashboardExcelExportQueryDto query,
            ExportUserContextDto userContext,
            IList<DashboardExcelDetailRow> details,
            IList<DashboardDepartmentSummaryRow> byDepartment,
            IList<DashboardMissingIndicatorRow> missing,
            IList<DashboardExcelDetailRow> failed,
            IList<DashboardReviewHistoryRow> reviewHistory,
            IList<ComparisonPeriodSnapshot> comparisonSnapshots)
        {
            // Tách nhóm dữ liệu thành các sheet để hỗ trợ cả tổng quan và đối soát chi tiết.
            using (var workbook = new XLWorkbook())
            {
                if (comparisonSnapshots.Any())
                {
                    AddComparisonSummarySheet(workbook, query, userContext, comparisonSnapshots);
                    AddIndicatorComparisonSheet(workbook, query, userContext, comparisonSnapshots);
                }

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

        // Kiểm tra các điều kiện hợp lệ trước khi tiếp tục xử lý workbook Dashboard và lịch sử xuất.
    }
}
