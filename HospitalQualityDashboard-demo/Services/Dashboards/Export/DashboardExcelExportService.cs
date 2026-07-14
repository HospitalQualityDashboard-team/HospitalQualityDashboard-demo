// Mục đích: điều phối xuất Excel dashboard và ghi nhận lịch sử xuất báo cáo.
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
    public partial class DashboardExcelExportService : DbServiceBase
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
            var comparisonSnapshots = BuildComparisonSnapshots(effectiveQuery);
            if (comparisonSnapshots.Any())
            {
                effectiveQuery.NamBaoCao = null;
                effectiveQuery.TanSuat = comparisonSnapshots[0].Period.TanSuat;
            }
            var details = comparisonSnapshots.Any()
                ? comparisonSnapshots[0].Details
                : QueryDetailRows(effectiveQuery);
            var missing = comparisonSnapshots.Any()
                ? comparisonSnapshots[0].Missing
                : QueryMissingRows(effectiveQuery);
            var failed = details.Where(x => x.DatMucTieu == false).ToList();
            var byDepartment = BuildDepartmentSummary(details, missing);
            var reviewHistory = comparisonSnapshots.Any()
                ? QueryReviewHistory(CopyQueryForPeriod(effectiveQuery, comparisonSnapshots[0].Period))
                : QueryReviewHistory(effectiveQuery);
            var fileName = BuildFileName(effectiveQuery, userContext);

            var content = CreateWorkbook(effectiveQuery, userContext, details, byDepartment, missing, failed, reviewHistory, comparisonSnapshots);
            EnsureExportHistoryTable();
            var rowCount = comparisonSnapshots.Any()
                ? comparisonSnapshots.Sum(x => x.Details.Count + x.Missing.Count)
                : details.Count;
            LogExportHistory(effectiveQuery, userContext, fileName, rowCount);

            return new DashboardExcelExportResultDto
            {
                Content = content,
                FileName = fileName,
                RowCount = rowCount
            };
        }

        // Chuẩn hóa phạm vi xuất: admin được chọn khoa/phòng, user luôn bị khóa về khoa/phòng của mình.
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
                DatMucTieu = query.DatMucTieu,
                DepartmentStatusFilter = userContext.IsAdmin ? NormalizeDepartmentStatusFilter(query.DepartmentStatusFilter) : "active",
                ComparisonPeriodIds = query.ComparisonPeriodIds == null
                    ? new int[0]
                    : query.ComparisonPeriodIds.Where(x => x > 0).Distinct().ToArray()
            };

            if (!userContext.IsAdmin && !normalized.KhoaPhongId.HasValue)
            {
                throw new InvalidOperationException("Tài khoản User chưa được gắn khoa/phòng.");
            }

            if (normalized.KyBaoCaoId.HasValue)
            {
                normalized.ComparisonPeriodIds = normalized.ComparisonPeriodIds
                    .Where(x => x != normalized.KyBaoCaoId.Value)
                    .ToArray();
            }

            return normalized;
        }

        private static string NormalizeDepartmentStatusFilter(string statusFilter)
        {
            if (string.Equals(statusFilter, "locked", StringComparison.OrdinalIgnoreCase)) return "locked";
            if (string.Equals(statusFilter, "all", StringComparison.OrdinalIgnoreCase)) return "all";
            return "active";
        }
    }
}
