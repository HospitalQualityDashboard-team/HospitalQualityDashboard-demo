using HospitalQualityDashboardDemo.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HospitalQualityDashboardDemo.Services
{
    public static class DashboardComparisonBuilder
    {
        private const int DraftPeriodStatus = 1;
        private const int MaximumPeriods = 12;
        private static readonly int[] SupportedFrequencies = { 3, 4, 5, 9, 6 };

        public static IList<DashboardComparisonPeriodDto> ValidateAndOrderPeriods(
            int primaryPeriodId,
            IEnumerable<int> comparisonPeriodIds,
            IEnumerable<DashboardComparisonPeriodDto> availablePeriods)
        {
            var periods = (availablePeriods ?? Enumerable.Empty<DashboardComparisonPeriodDto>()).ToList();
            var primary = periods.SingleOrDefault(x => x.KyBaoCaoId == primaryPeriodId);
            if (primary == null)
            {
                throw new InvalidOperationException("Kỳ báo cáo chính không tồn tại.");
            }

            if (primary.TrangThai == DraftPeriodStatus || !SupportedFrequencies.Contains(primary.TanSuat))
            {
                throw new InvalidOperationException("Kỳ báo cáo chính không hỗ trợ so sánh.");
            }

            var requestedIds = (comparisonPeriodIds ?? Enumerable.Empty<int>())
                .Where(x => x != primaryPeriodId)
                .Distinct()
                .ToList();
            if (requestedIds.Count + 1 > MaximumPeriods)
            {
                throw new InvalidOperationException("Chỉ được chọn tối đa 12 kỳ báo cáo.");
            }

            var comparisons = new List<DashboardComparisonPeriodDto>();
            foreach (var requestedId in requestedIds)
            {
                var comparison = periods.SingleOrDefault(x => x.KyBaoCaoId == requestedId);
                if (comparison == null)
                {
                    throw new InvalidOperationException("Kỳ báo cáo so sánh không tồn tại.");
                }

                if (comparison.TanSuat != primary.TanSuat)
                {
                    throw new InvalidOperationException("Các kỳ so sánh phải cùng tần suất với kỳ chính.");
                }

                if (comparison.TrangThai == DraftPeriodStatus || comparison.TuNgay >= primary.TuNgay)
                {
                    throw new InvalidOperationException("Kỳ so sánh phải là kỳ đã mở hoặc đã khóa và cũ hơn kỳ chính.");
                }

                comparisons.Add(comparison);
            }

            return new[] { primary }
                .Concat(comparisons.OrderByDescending(x => x.TuNgay))
                .ToList();
        }

        public static DashboardIndicatorComparisonDto CompareIndicator(
            DashboardIndicatorPeriodValueDto primary,
            DashboardIndicatorPeriodValueDto comparison)
        {
            var result = new DashboardIndicatorComparisonDto
            {
                TrangThaiKyChinh = FormatAvailability(primary),
                TrangThaiKySoSanh = FormatAvailability(comparison),
                XuHuong = DetermineTrend(primary, comparison)
            };

            if (primary != null && comparison != null && primary.KetQua.HasValue && comparison.KetQua.HasValue)
            {
                result.ChenhLech = primary.KetQua.Value - comparison.KetQua.Value;
            }

            return result;
        }

        private static string FormatAvailability(DashboardIndicatorPeriodValueDto value)
        {
            if (value == null || !value.IsExpected) return "Không áp dụng";
            if (!value.IsSubmitted) return "Chưa nộp";
            return value.KetQua.HasValue ? "Có dữ liệu" : "Không có số liệu";
        }

        private static string DetermineTrend(
            DashboardIndicatorPeriodValueDto primary,
            DashboardIndicatorPeriodValueDto comparison)
        {
            if ((primary == null || !primary.IsExpected) && comparison != null && comparison.IsExpected)
                return "Không còn phát sinh";
            if (primary != null && primary.IsExpected && (comparison == null || !comparison.IsExpected))
                return "Mới phát sinh";
            if (primary == null || comparison == null || !primary.IsSubmitted || !comparison.IsSubmitted)
                return "Chưa đủ dữ liệu";
            if (primary.DatMucTieu == true && comparison.DatMucTieu == false) return "Cải thiện";
            if (primary.DatMucTieu == false && comparison.DatMucTieu == true) return "Giảm";
            if (primary.DatMucTieu.HasValue && comparison.DatMucTieu.HasValue) return "Không đổi";
            return "Chưa xác định";
        }
    }
}
