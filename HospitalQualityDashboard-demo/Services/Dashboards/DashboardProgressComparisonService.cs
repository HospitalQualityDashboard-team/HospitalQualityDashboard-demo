// Mục đích: truy vấn và chuẩn hóa dữ liệu so sánh tiến độ dashboard theo bộ lọc.
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Services
{
    public class DashboardProgressComparisonService : DbServiceBase
    {
        private static readonly int[] SupportedFrequencies = { 3, 4, 5, 9, 6 };
        private static readonly string[] SupportedProgressStatuses =
        {
            DashboardProgressComparisonBuilder.OnTime,
            DashboardProgressComparisonBuilder.Late,
            DashboardProgressComparisonBuilder.Missing,
            DashboardProgressComparisonBuilder.OverdueMissing
        };

        public DashboardProgressComparisonService()
            : base(DatabaseConfiguration.GetConnectionString(), 60)
        {
        }

        public ProgressComparisonViewModel GetComparison(
            DashboardAnalysisQueryDto query,
            bool isAdmin,
            int? currentDepartmentId)
        {
            query = query ?? new DashboardAnalysisQueryDto();
            query.Page = query.Page < 1 ? 1 : query.Page;
            query.PageSize = query.PageSize < 1 || query.PageSize > 100 ? 20 : query.PageSize;
            query.KhoaPhongId = isAdmin ? query.KhoaPhongId : currentDepartmentId;

            var periods = QueryPeriods();
            var frequency = query.TanSuat ?? SupportedFrequencies.First();
            var eligible = periods.Where(x => x.TanSuat == frequency).OrderByDescending(x => x.TuNgay).ToList();
            var primaryId = NormalizePrimaryPeriodId(frequency, query.KyBaoCaoId, periods);
            IList<DashboardComparisonPeriodDto> selected = new List<DashboardComparisonPeriodDto>();
            if (primaryId.HasValue)
            {
                var comparisonIds = NormalizeComparisonPeriodIds(primaryId.Value, query.ComparisonPeriodIds, eligible);

                selected = DashboardComparisonBuilder.ValidateAndOrderPeriods(primaryId.Value, comparisonIds, periods);
                query.ComparisonPeriodIds = selected.Skip(1).Select(x => x.KyBaoCaoId).ToArray();
            }

            var slots = selected.Count == 0
                ? new List<ProgressSlot>()
                : QuerySlots(selected.Select(x => x.KyBaoCaoId), query.KhoaPhongId);
            var metrics = BuildMetrics(selected, slots);
            var allRows = BuildRows(selected, slots);
            var orderedRows = allRows
                .OrderBy(x => ChangeOrder(x.OverallChangeCode))
                .ThenBy(x => x.TenKhoaPhong)
                .ThenBy(x => x.MaChiSo)
                .ToList();
            var statusPeriodId = NormalizeStatusPeriodId(query.StatusPeriodId, selected);
            var progressStatus = NormalizeProgressStatus(query.ProgressStatus);
            var detailRows = FilterRowsByStatus(orderedRows, statusPeriodId, progressStatus).ToList();
            var totalPages = detailRows.Count == 0 ? 0 : (int)Math.Ceiling(detailRows.Count / (decimal)query.PageSize);

            return new ProgressComparisonViewModel
            {
                IsAdmin = isAdmin,
                TanSuat = frequency,
                KyBaoCaoId = primaryId,
                KhoaPhongId = query.KhoaPhongId,
                ComparisonPeriodIds = query.ComparisonPeriodIds ?? new int[0],
                StatusPeriodId = statusPeriodId,
                ProgressStatus = progressStatus,
                TanSuatOptions = BuildFrequencyOptions(frequency),
                KyBaoCaoOptions = BuildPeriodOptions(eligible, primaryId),
                KhoaPhongOptions = isAdmin ? BuildDepartmentOptions(query.KhoaPhongId) : new List<SelectListItem>(),
                AvailablePeriods = periods,
                Metrics = metrics,
                Rows = detailRows.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList(),
                BetterCount = orderedRows.Count(x => x.OverallChangeCode == "Better"),
                WorseCount = orderedRows.Count(x => x.OverallChangeCode == "Worse"),
                UnchangedCount = orderedRows.Count(x => x.OverallChangeCode == "Unchanged"),
                InsufficientCount = orderedRows.Count(x => x.OverallChangeCode == "Insufficient"),
                CurrentPage = query.Page,
                TotalPages = totalPages,
                TotalRows = detailRows.Count
            };
        }

        private static int? NormalizeStatusPeriodId(
            int? requestedStatusPeriodId,
            IEnumerable<DashboardComparisonPeriodDto> selectedPeriods)
        {
            if (!requestedStatusPeriodId.HasValue)
            {
                return null;
            }

            return (selectedPeriods ?? Enumerable.Empty<DashboardComparisonPeriodDto>())
                .Any(x => x.KyBaoCaoId == requestedStatusPeriodId.Value)
                    ? requestedStatusPeriodId
                    : null;
        }

        private static string NormalizeProgressStatus(string requestedProgressStatus)
        {
            return SupportedProgressStatuses.Contains(requestedProgressStatus)
                ? requestedProgressStatus
                : null;
        }

        private static IEnumerable<ProgressComparisonRowViewModel> FilterRowsByStatus(
            IEnumerable<ProgressComparisonRowViewModel> rows,
            int? statusPeriodId,
            string progressStatus)
        {
            if (!statusPeriodId.HasValue || string.IsNullOrWhiteSpace(progressStatus))
            {
                return rows;
            }

            return rows.Where(row => row.Periods != null && row.Periods.Any(period =>
                period.KyBaoCaoId == statusPeriodId.Value
                && string.Equals(period.StatusCode, progressStatus, StringComparison.Ordinal)));
        }

        private static int? NormalizePrimaryPeriodId(
            int frequency,
            int? requestedPrimaryId,
            IEnumerable<DashboardComparisonPeriodDto> periods)
        {
            var eligible = (periods ?? Enumerable.Empty<DashboardComparisonPeriodDto>())
                .Where(x => x.TanSuat == frequency)
                .OrderByDescending(x => x.TuNgay)
                .ToList();

            if (requestedPrimaryId.HasValue && eligible.Any(x => x.KyBaoCaoId == requestedPrimaryId.Value))
            {
                return requestedPrimaryId.Value;
            }

            return eligible.Select(x => (int?)x.KyBaoCaoId).FirstOrDefault();
        }

        private static int[] NormalizeComparisonPeriodIds(
            int primaryId,
            IEnumerable<int> requestedComparisonIds,
            IList<DashboardComparisonPeriodDto> eligiblePeriods)
        {
            var primary = eligiblePeriods.FirstOrDefault(x => x.KyBaoCaoId == primaryId);
            if (primary == null)
            {
                return new int[0];
            }

            var requested = (requestedComparisonIds ?? Enumerable.Empty<int>())
                .Where(x => x != primaryId)
                .Distinct()
                .ToList();

            if (requested.Count > 0)
            {
                var validRequested = eligiblePeriods
                    .Where(x => requested.Contains(x.KyBaoCaoId) && x.TuNgay < primary.TuNgay)
                    .OrderByDescending(x => x.TuNgay)
                    .Select(x => x.KyBaoCaoId)
                    .ToArray();

                if (validRequested.Length > 0)
                {
                    return validRequested;
                }
            }

            return eligiblePeriods
                .Where(x => x.TuNgay < primary.TuNgay)
                .OrderByDescending(x => x.TuNgay)
                .Take(1)
                .Select(x => x.KyBaoCaoId)
                .ToArray();
        }

        private IList<DashboardComparisonPeriodDto> QueryPeriods()
        {
            return Query(@"
SELECT KyBaoCaoId, TenKyBaoCao, LoaiKyBaoCao, TuNgay, TrangThai
FROM dbo.KyBaoCao
WHERE LoaiKyBaoCao IN (3,4,5,9,6) AND TrangThai <> 1
ORDER BY TuNgay DESC", reader => new DashboardComparisonPeriodDto
            {
                KyBaoCaoId = Int(reader, "KyBaoCaoId"),
                TenKyBaoCao = String(reader, "TenKyBaoCao"),
                TanSuat = Convert.ToInt32(reader["LoaiKyBaoCao"], CultureInfo.InvariantCulture),
                TuNgay = reader.GetDateTime(reader.GetOrdinal("TuNgay")),
                TrangThai = Convert.ToInt32(reader["TrangThai"], CultureInfo.InvariantCulture)
            });
        }

        private IList<ProgressSlot> QuerySlots(IEnumerable<int> periodIds, int? departmentId)
        {
            var ids = periodIds.Distinct().ToList();
            var parameters = ids.Select((id, index) => Param("@Period" + index, id)).ToList();
            parameters.Add(Param("@KhoaPhongId", departmentId.HasValue ? (object)departmentId.Value : null));
            var inClause = string.Join(",", ids.Select((id, index) => "@Period" + index));
            var sql = @"
SELECT ROW_NUMBER() OVER (ORDER BY kp.TenKhoaPhong, cs.MaChiSo, ky.TuNgay DESC) AS STT,
       ky.KyBaoCaoId, ky.TenKyBaoCao, ky.TuNgay, ky.HanNop, ky.TrangThai AS TrangThaiKy,
       pc.KhoaPhongId, pc.ChiSoChatLuongId, kp.TenKhoaPhong, cs.MaChiSo, cs.TenChiSo,
       bc.NgayGui
FROM dbo.KyBaoCao ky
INNER JOIN dbo.PhanCongChiSo pc ON pc.DangHoatDong = 1
    AND (pc.TuNgay IS NULL OR pc.TuNgay <= ky.DenNgay)
    AND (pc.DenNgay IS NULL OR pc.DenNgay >= ky.TuNgay)
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = pc.KhoaPhongId
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = pc.ChiSoChatLuongId
INNER JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId
    AND ts.TanSuatBaoCao = ky.LoaiKyBaoCao
LEFT JOIN dbo.BaoCao bc ON bc.KyBaoCaoId = ky.KyBaoCaoId
    AND bc.KhoaPhongId = pc.KhoaPhongId
    AND bc.ChiSoChatLuongId = pc.ChiSoChatLuongId
WHERE ky.KyBaoCaoId IN (" + inClause + @")
  AND (@KhoaPhongId IS NULL OR pc.KhoaPhongId = @KhoaPhongId)
  AND dbo.fn_ChiSoDuocTrienKhaiTrongKy(pc.ChiSoChatLuongId, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay) = 1";

            return Query(sql, reader => new ProgressSlot
            {
                KyBaoCaoId = Int(reader, "KyBaoCaoId"),
                TenKyBaoCao = String(reader, "TenKyBaoCao"),
                TuNgay = reader.GetDateTime(reader.GetOrdinal("TuNgay")),
                HanNop = reader.GetDateTime(reader.GetOrdinal("HanNop")),
                TrangThaiKy = Convert.ToInt32(reader["TrangThaiKy"], CultureInfo.InvariantCulture),
                KhoaPhongId = Int(reader, "KhoaPhongId"),
                ChiSoChatLuongId = Int(reader, "ChiSoChatLuongId"),
                TenKhoaPhong = String(reader, "TenKhoaPhong"),
                MaChiSo = String(reader, "MaChiSo"),
                TenChiSo = String(reader, "TenChiSo"),
                NgayGui = NullableDateTime(reader, "NgayGui")
            }, parameters.ToArray());
        }

        private static IList<ProgressPeriodMetricViewModel> BuildMetrics(
            IEnumerable<DashboardComparisonPeriodDto> periods,
            IEnumerable<ProgressSlot> slots)
        {
            var today = GetVietnamLocalNow().Date;
            return periods.Select(period =>
            {
                var periodSlots = slots.Where(x => x.KyBaoCaoId == period.KyBaoCaoId).ToList();
                var statuses = periodSlots.Select(x => DashboardProgressComparisonBuilder.Classify(x.NgayGui, x.HanNop, today)).ToList();
                var submitted = statuses.Count(x => x == "OnTime" || x == "Late");
                return new ProgressPeriodMetricViewModel
                {
                    KyBaoCaoId = period.KyBaoCaoId,
                    TenKyBaoCao = period.TenKyBaoCao,
                    TuNgay = period.TuNgay,
                    TrangThaiKy = period.TrangThai,
                    TongCanNop = statuses.Count,
                    DungHan = statuses.Count(x => x == "OnTime"),
                    NopTre = statuses.Count(x => x == "Late"),
                    ChuaNop = statuses.Count(x => x == "Missing"),
                    QuaHanChuaNop = statuses.Count(x => x == "OverdueMissing"),
                    TyLeHoanThanh = statuses.Count == 0 ? 0 : Math.Round(submitted * 100m / statuses.Count, 2),
                    TyLeDungHan = statuses.Count == 0 ? 0 : Math.Round(statuses.Count(x => x == "OnTime") * 100m / statuses.Count, 2)
                };
            }).ToList();
        }

        private static IList<ProgressComparisonRowViewModel> BuildRows(
            IList<DashboardComparisonPeriodDto> periods,
            IEnumerable<ProgressSlot> slots)
        {
            if (periods.Count == 0) return new List<ProgressComparisonRowViewModel>();
            var today = GetVietnamLocalNow().Date;
            var primaryId = periods[0].KyBaoCaoId;
            return slots.GroupBy(x => x.KhoaPhongId + "|" + x.ChiSoChatLuongId)
                .Select(group =>
                {
                    var identity = group.First();
                    var primary = group.FirstOrDefault(x => x.KyBaoCaoId == primaryId);
                    var primaryStatus = primary == null ? null : DashboardProgressComparisonBuilder.Classify(primary.NgayGui, primary.HanNop, today);
                    var values = periods.Select(period =>
                    {
                        var slot = group.FirstOrDefault(x => x.KyBaoCaoId == period.KyBaoCaoId);
                        var status = slot == null ? null : DashboardProgressComparisonBuilder.Classify(slot.NgayGui, slot.HanNop, today);
                        return new ProgressPeriodStatusViewModel
                        {
                            KyBaoCaoId = period.KyBaoCaoId,
                            TenKyBaoCao = period.TenKyBaoCao,
                            StatusCode = status,
                            NgayGui = slot == null ? null : slot.NgayGui,
                            ChangeCode = period.KyBaoCaoId == primaryId ? null : DashboardProgressComparisonBuilder.Compare(primaryStatus, status)
                        };
                    }).ToList();
                    var changes = values.Where(x => x.ChangeCode != null).Select(x => x.ChangeCode).ToList();
                    var overall = changes.Contains("Worse") ? "Worse"
                        : changes.Contains("Better") ? "Better"
                        : changes.Count > 0 && changes.All(x => x == "Unchanged") ? "Unchanged"
                        : "Insufficient";
                    return new ProgressComparisonRowViewModel
                    {
                        KhoaPhongId = identity.KhoaPhongId,
                        ChiSoChatLuongId = identity.ChiSoChatLuongId,
                        TenKhoaPhong = identity.TenKhoaPhong,
                        MaChiSo = identity.MaChiSo,
                        TenChiSo = identity.TenChiSo,
                        OverallChangeCode = overall,
                        Periods = values
                    };
                }).ToList();
        }

        private IList<SelectListItem> BuildDepartmentOptions(int? selected)
        {
            var options = Query("SELECT KhoaPhongId, TenKhoaPhong FROM dbo.KhoaPhong WHERE Used=1 ORDER BY TenKhoaPhong",
                reader => new SelectListItem
                {
                    Value = Int(reader, "KhoaPhongId").ToString(CultureInfo.InvariantCulture),
                    Text = String(reader, "TenKhoaPhong"),
                    Selected = selected == Int(reader, "KhoaPhongId")
                });
            options.Insert(0, new SelectListItem { Value = "", Text = "Toàn viện", Selected = !selected.HasValue });
            return options;
        }

        private static IList<SelectListItem> BuildPeriodOptions(IEnumerable<DashboardComparisonPeriodDto> periods, int? selected)
        {
            return periods.Select(x => new SelectListItem
            {
                Value = x.KyBaoCaoId.ToString(CultureInfo.InvariantCulture),
                Text = x.TenKyBaoCao,
                Selected = selected == x.KyBaoCaoId
            }).ToList();
        }

        private static IList<SelectListItem> BuildFrequencyOptions(int selected)
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "3", Text = "Hàng tháng", Selected = selected == 3 },
                new SelectListItem { Value = "4", Text = "Hàng quý", Selected = selected == 4 },
                new SelectListItem { Value = "5", Text = "6 tháng", Selected = selected == 5 },
                new SelectListItem { Value = "9", Text = "9 tháng", Selected = selected == 9 },
                new SelectListItem { Value = "6", Text = "Hàng năm", Selected = selected == 6 }
            };
        }

        private static int ChangeOrder(string code)
        {
            if (code == "Worse") return 0;
            if (code == "Insufficient") return 1;
            if (code == "Better") return 2;
            return 3;
        }

        private static DateTime GetVietnamLocalNow()
        {
            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            }
            catch (TimeZoneNotFoundException) { return DateTime.Now; }
            catch (InvalidTimeZoneException) { return DateTime.Now; }
        }

        private class ProgressSlot
        {
            public int KyBaoCaoId { get; set; }
            public string TenKyBaoCao { get; set; }
            public DateTime TuNgay { get; set; }
            public DateTime HanNop { get; set; }
            public int TrangThaiKy { get; set; }
            public int KhoaPhongId { get; set; }
            public int ChiSoChatLuongId { get; set; }
            public string TenKhoaPhong { get; set; }
            public string MaChiSo { get; set; }
            public string TenChiSo { get; set; }
            public DateTime? NgayGui { get; set; }
        }
    }
}
