// Mục đích: xuất dữ liệu so sánh dashboard nhiều kỳ ra Excel theo đúng bộ lọc nghiệp vụ.
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
        private IList<ComparisonPeriodSnapshot> BuildComparisonSnapshots(DashboardExcelExportQueryDto query)
        {
            if (query.ComparisonPeriodIds == null || query.ComparisonPeriodIds.Length == 0)
            {
                return new List<ComparisonPeriodSnapshot>();
            }

            if (!query.KyBaoCaoId.HasValue)
            {
                throw new InvalidOperationException("Vui lòng chọn kỳ báo cáo chính khi xuất so sánh.");
            }

            var periods = Query(@"
SELECT KyBaoCaoId, TenKyBaoCao, LoaiKyBaoCao, TuNgay, TrangThai
FROM dbo.KyBaoCao",
                reader => new DashboardComparisonPeriodDto
                {
                    KyBaoCaoId = Int(reader, "KyBaoCaoId"),
                    TenKyBaoCao = String(reader, "TenKyBaoCao"),
                    TanSuat = Convert.ToInt32(reader["LoaiKyBaoCao"], CultureInfo.InvariantCulture),
                    TuNgay = reader.GetDateTime(reader.GetOrdinal("TuNgay")),
                    TrangThai = Convert.ToInt32(reader["TrangThai"], CultureInfo.InvariantCulture)
                });

            var orderedPeriods = DashboardComparisonBuilder.ValidateAndOrderPeriods(
                query.KyBaoCaoId.Value,
                query.ComparisonPeriodIds,
                periods);

            return orderedPeriods.Select(period =>
            {
                var periodQuery = CopyQueryForPeriod(query, period);
                var periodDetails = QueryDetailRows(periodQuery);
                var detailKeys = new HashSet<string>(periodDetails
                    .Where(x => x.NgayGui.HasValue)
                    .Select(x => BuildIndicatorKey(x.KhoaPhongId, x.ChiSoChatLuongId)));
                var periodMissing = QueryMissingRows(periodQuery)
                    .Where(x => !detailKeys.Contains(BuildIndicatorKey(x.KhoaPhongId, x.ChiSoChatLuongId)))
                    .ToList();
                return new ComparisonPeriodSnapshot
                {
                    Period = period,
                    Details = periodDetails,
                    Missing = periodMissing,
                    Metrics = BuildComparisonMetrics(periodDetails, periodMissing)
                };
            }).ToList();
        }

        private static DashboardExcelExportQueryDto CopyQueryForPeriod(
            DashboardExcelExportQueryDto source,
            DashboardComparisonPeriodDto period)
        {
            return new DashboardExcelExportQueryDto
            {
                NamBaoCao = null,
                KyBaoCaoId = period.KyBaoCaoId,
                TanSuat = period.TanSuat,
                KhoaPhongId = source.KhoaPhongId,
                LinhVuc = source.LinhVuc,
                TrangThaiNhapLieu = source.TrangThaiNhapLieu,
                TrangThaiDuyet = source.TrangThaiDuyet,
                DatMucTieu = source.DatMucTieu,
                ComparisonPeriodIds = new int[0]
            };
        }

        private static ComparisonMetrics BuildComparisonMetrics(
            IList<DashboardExcelDetailRow> details,
            IList<DashboardMissingIndicatorRow> missing)
        {
            var submitted = details.Where(x => x.NgayGui.HasValue).ToList();
            var total = submitted.Count + missing.Count;
            return new ComparisonMetrics
            {
                TotalRequired = total,
                Entered = submitted.Count,
                SubmittedOnTime = submitted.Count(x => x.NgayGui.Value.Date <= x.HanNop.Date),
                SubmittedLate = submitted.Count(x => x.NgayGui.Value.Date > x.HanNop.Date),
                OverdueMissing = missing.Count(x => x.TrangThai.Contains("Quá hạn")),
                CompletionRate = total > 0 ? Math.Round((decimal)submitted.Count * 100 / total, 2) : 0
            };
        }

        private void AddComparisonSummarySheet(
            XLWorkbook workbook,
            DashboardExcelExportQueryDto query,
            ExportUserContextDto userContext,
            IList<ComparisonPeriodSnapshot> snapshots)
        {
            var worksheet = workbook.Worksheets.Add("SoSanhTongQuan");
            var startRow = AddMetadata(worksheet, query, userContext, "So sánh tổng quan nhiều kỳ");
            var primary = snapshots[0];
            var metrics = new[]
            {
                new ComparisonMetricDefinition("Tổng cần nộp", x => x.TotalRequired, null),
                new ComparisonMetricDefinition("Đã nhập", x => x.Entered, true),
                new ComparisonMetricDefinition("Đã gửi đúng hạn", x => x.SubmittedOnTime, true),
                new ComparisonMetricDefinition("Nộp quá hạn", x => x.SubmittedLate, false),
                new ComparisonMetricDefinition("Quá hạn chưa nộp", x => x.OverdueMissing, false),
                new ComparisonMetricDefinition("Tỷ lệ hoàn tất (%)", x => x.CompletionRate, true)
            };

            StyleComparisonHeader(worksheet.Cell(startRow, 1), "Chỉ tiêu");
            StyleComparisonHeader(worksheet.Cell(startRow, 2), primary.Period.TenKyBaoCao + " (kỳ chính)");
            var column = 3;
            foreach (var snapshot in snapshots.Skip(1))
            {
                StyleComparisonHeader(worksheet.Cell(startRow, column), snapshot.Period.TenKyBaoCao);
                StyleComparisonHeader(worksheet.Cell(startRow, column + 1), "Chênh lệch");
                column += 2;
            }

            for (var index = 0; index < metrics.Length; index++)
            {
                var row = startRow + index + 1;
                var definition = metrics[index];
                var primaryValue = definition.Selector(primary.Metrics);
                worksheet.Cell(row, 1).Value = definition.Label;
                SetCellValue(worksheet.Cell(row, 2), primaryValue);
                column = 3;
                foreach (var snapshot in snapshots.Skip(1))
                {
                    var comparisonValue = definition.Selector(snapshot.Metrics);
                    var delta = primaryValue - comparisonValue;
                    SetCellValue(worksheet.Cell(row, column), comparisonValue);
                    SetCellValue(worksheet.Cell(row, column + 1), delta);
                    ApplyDeltaStyle(worksheet.Cell(row, column + 1), delta, definition.HigherIsBetter);
                    column += 2;
                }
            }

            var lastRow = startRow + metrics.Length;
            var lastColumn = 2 + ((snapshots.Count - 1) * 2);
            var range = worksheet.Range(startRow, 1, lastRow, lastColumn);
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            worksheet.SheetView.FreezeRows(startRow);
            worksheet.SheetView.FreezeColumns(1);
            worksheet.Columns(1, lastColumn).AdjustToContents();
        }

        private void AddIndicatorComparisonSheet(
            XLWorkbook workbook,
            DashboardExcelExportQueryDto query,
            ExportUserContextDto userContext,
            IList<ComparisonPeriodSnapshot> snapshots)
        {
            var worksheet = workbook.Worksheets.Add("SoSanhChiSo");
            var startRow = AddMetadata(worksheet, query, userContext, "So sánh chi tiết chỉ số nhiều kỳ");
            var periodValues = snapshots.ToDictionary(x => x.Period.KyBaoCaoId, BuildIndicatorValues);
            var keys = periodValues.Values.SelectMany(x => x.Keys).Distinct().OrderBy(x => x).ToList();
            var primary = snapshots[0];

            var headers = new List<string> { "STT", "Khoa/phòng", "Mã chỉ số", "Tên chỉ số" };
            headers.Add(primary.Period.TenKyBaoCao + " - Trạng thái nộp");
            headers.Add(primary.Period.TenKyBaoCao + " - Ngày gửi");
            foreach (var snapshot in snapshots.Skip(1))
            {
                headers.Add(snapshot.Period.TenKyBaoCao + " - Trạng thái nộp");
                headers.Add(snapshot.Period.TenKyBaoCao + " - Ngày gửi");
                headers.Add("Đánh giá với kỳ chính");
            }

            for (var headerIndex = 0; headerIndex < headers.Count; headerIndex++)
            {
                StyleComparisonHeader(worksheet.Cell(startRow, headerIndex + 1), headers[headerIndex]);
            }

            for (var index = 0; index < keys.Count; index++)
            {
                var row = startRow + index + 1;
                var key = keys[index];
                var values = snapshots.Select(x => GetIndicatorValue(periodValues[x.Period.KyBaoCaoId], key)).ToList();
                var identity = values.First(x => x != null);
                var primaryValue = values[0];

                worksheet.Cell(row, 1).Value = index + 1;
                worksheet.Cell(row, 2).Value = identity.TenKhoaPhong;
                worksheet.Cell(row, 3).Value = identity.MaChiSo;
                worksheet.Cell(row, 4).Value = identity.TenChiSo;
                worksheet.Cell(row, 5).Value = FormatProgressStatus(primaryValue);
                worksheet.Cell(row, 6).Value = FormatSubmittedAt(primaryValue);

                var column = 7;
                for (var periodIndex = 1; periodIndex < snapshots.Count; periodIndex++)
                {
                    var comparisonValue = values[periodIndex];
                    var change = DashboardProgressComparisonBuilder.Compare(
                        primaryValue == null ? null : primaryValue.ProgressStatus,
                        comparisonValue == null ? null : comparisonValue.ProgressStatus);
                    worksheet.Cell(row, column).Value = FormatProgressStatus(comparisonValue);
                    worksheet.Cell(row, column + 1).Value = FormatSubmittedAt(comparisonValue);
                    worksheet.Cell(row, column + 2).Value = FormatProgressChange(change);
                    ApplyProgressChangeStyle(worksheet.Cell(row, column + 2), change);
                    column += 3;
                }
            }

            if (keys.Count == 0)
            {
                worksheet.Cell(startRow + 1, 1).Value = "Không có dữ liệu phù hợp bộ lọc.";
                worksheet.Range(startRow + 1, 1, startRow + 1, headers.Count).Merge();
            }

            var lastRow = startRow + Math.Max(keys.Count, 1);
            var tableRange = worksheet.Range(startRow, 1, lastRow, headers.Count);
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            tableRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.SheetView.FreezeRows(startRow);
            worksheet.SheetView.FreezeColumns(5);
            worksheet.Columns(1, headers.Count).AdjustToContents();
        }

        private static IDictionary<string, DashboardIndicatorPeriodValueDto> BuildIndicatorValues(ComparisonPeriodSnapshot snapshot)
        {
            var values = new Dictionary<string, DashboardIndicatorPeriodValueDto>();
            foreach (var detail in snapshot.Details)
            {
                var key = BuildIndicatorKey(detail.KhoaPhongId, detail.ChiSoChatLuongId);
                values[key] = new DashboardIndicatorPeriodValueDto
                {
                    KyBaoCaoId = snapshot.Period.KyBaoCaoId,
                    KhoaPhongId = detail.KhoaPhongId,
                    ChiSoChatLuongId = detail.ChiSoChatLuongId,
                    TenKhoaPhong = detail.TenKhoaPhong,
                    MaChiSo = detail.MaChiSo,
                    TenChiSo = detail.TenChiSo,
                    DonViTinh = detail.DonViTinh,
                    IsExpected = true,
                    IsSubmitted = detail.NgayGui.HasValue || detail.TrangThaiNhapLieu != "Nháp",
                    NgayGui = detail.NgayGui,
                    HanNop = detail.HanNop,
                    ProgressStatus = DashboardProgressComparisonBuilder.Classify(detail.NgayGui, detail.HanNop, GetVietnamLocalNow())
                };
            }

            foreach (var missing in snapshot.Missing)
            {
                var key = BuildIndicatorKey(missing.KhoaPhongId, missing.ChiSoChatLuongId);
                if (values.ContainsKey(key)) continue;
                values[key] = new DashboardIndicatorPeriodValueDto
                {
                    KyBaoCaoId = snapshot.Period.KyBaoCaoId,
                    KhoaPhongId = missing.KhoaPhongId,
                    ChiSoChatLuongId = missing.ChiSoChatLuongId,
                    TenKhoaPhong = missing.TenKhoaPhong,
                    MaChiSo = missing.MaChiSo,
                    TenChiSo = missing.TenChiSo,
                    DonViTinh = missing.DonViTinh,
                    IsExpected = true,
                    IsSubmitted = false,
                    HanNop = missing.HanNop,
                    ProgressStatus = DashboardProgressComparisonBuilder.Classify(null, missing.HanNop, GetVietnamLocalNow())
                };
            }

            return values;
        }

        private static string FormatProgressStatus(DashboardIndicatorPeriodValueDto value)
        {
            if (value == null) return "Không áp dụng";
            switch (value.ProgressStatus)
            {
                case "OnTime": return "Đúng hạn";
                case "Late": return "Nộp trễ";
                case "Missing": return "Chưa nộp";
                case "OverdueMissing": return "Quá hạn chưa nộp";
                default: return "Không xác định";
            }
        }

        private static string FormatSubmittedAt(DashboardIndicatorPeriodValueDto value)
        {
            return value != null && value.NgayGui.HasValue
                ? value.NgayGui.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
                : string.Empty;
        }

        private static string FormatProgressChange(string change)
        {
            if (change == "Better") return "Tốt hơn";
            if (change == "Worse") return "Xấu hơn";
            if (change == "Unchanged") return "Không đổi";
            return "Chưa đủ dữ liệu";
        }

        private static void ApplyProgressChangeStyle(IXLCell cell, string change)
        {
            if (change == "Better")
            {
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2F0D9");
                cell.Style.Font.FontColor = XLColor.FromHtml("#1B5E20");
            }
            else if (change == "Worse")
            {
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FCE4D6");
                cell.Style.Font.FontColor = XLColor.FromHtml("#B71C1C");
            }
        }

        private static string BuildIndicatorKey(int departmentId, int indicatorId)
        {
            return departmentId.ToString(CultureInfo.InvariantCulture) + "|" + indicatorId.ToString(CultureInfo.InvariantCulture);
        }

        private static DashboardIndicatorPeriodValueDto GetIndicatorValue(
            IDictionary<string, DashboardIndicatorPeriodValueDto> values,
            string key)
        {
            DashboardIndicatorPeriodValueDto value;
            return values.TryGetValue(key, out value) ? value : null;
        }

        private static string FormatIndicatorAvailability(DashboardIndicatorPeriodValueDto value)
        {
            if (value == null || !value.IsExpected) return "Không áp dụng";
            if (!value.IsSubmitted) return "Chưa nộp";
            return value.KetQua.HasValue ? "Có dữ liệu" : "Không có số liệu";
        }

        private static void StyleComparisonHeader(IXLCell cell, string value)
        {
            cell.Value = value;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#DDEBF7");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.WrapText = true;
        }

        private static void ApplyDeltaStyle(IXLCell cell, decimal delta, bool? higherIsBetter)
        {
            if (!higherIsBetter.HasValue || delta == 0) return;
            var improved = higherIsBetter.Value ? delta > 0 : delta < 0;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(improved ? "#E2F0D9" : "#FCE4D6");
            cell.Style.Font.FontColor = XLColor.FromHtml(improved ? "#1B5E20" : "#B71C1C");
        }

        private static void ApplyTrendStyle(IXLCell cell, string trend)
        {
            if (trend == "Cải thiện")
            {
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2F0D9");
                cell.Style.Font.FontColor = XLColor.FromHtml("#1B5E20");
            }
            else if (trend == "Giảm")
            {
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FCE4D6");
                cell.Style.Font.FontColor = XLColor.FromHtml("#B71C1C");
            }
        }

        private class ComparisonPeriodSnapshot
        {
            public DashboardComparisonPeriodDto Period { get; set; }
            public IList<DashboardExcelDetailRow> Details { get; set; }
            public IList<DashboardMissingIndicatorRow> Missing { get; set; }
            public ComparisonMetrics Metrics { get; set; }
        }

        private class ComparisonMetrics
        {
            public decimal TotalRequired { get; set; }
            public decimal Entered { get; set; }
            public decimal SubmittedOnTime { get; set; }
            public decimal SubmittedLate { get; set; }
            public decimal OverdueMissing { get; set; }
            public decimal CompletionRate { get; set; }
        }

        private class ComparisonMetricDefinition
        {
            public ComparisonMetricDefinition(string label, Func<ComparisonMetrics, decimal> selector, bool? higherIsBetter)
            {
                Label = label;
                Selector = selector;
                HigherIsBetter = higherIsBetter;
            }

            public string Label { get; private set; }
            public Func<ComparisonMetrics, decimal> Selector { get; private set; }
            public bool? HigherIsBetter { get; private set; }
        }
    }
}
