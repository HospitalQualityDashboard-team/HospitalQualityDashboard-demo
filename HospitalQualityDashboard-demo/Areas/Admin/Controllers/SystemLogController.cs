// Mục đích: hiển thị nhật ký thao tác hệ thống cho Admin.
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.ViewModels;
using HospitalQualityDashboardDemo.Services;
using System;
using System.Globalization;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.Admin.Controllers
{
    public class SystemLogController : AdminBaseController
    {
        private const int DefaultPageSize = 10;
        private readonly SystemLogService _service = new SystemLogService();

        public ActionResult Index(SystemLogQueryDto query, int page = 1)
        {
            query = query ?? new SystemLogQueryDto();
            NormalizeDateFilters(query);

            int totalItems;
            var normalizedPage = NormalizePage(page);
            var items = _service.GetAll(query, normalizedPage, DefaultPageSize, out totalItems);

            return View(new SystemLogIndexViewModel
            {
                Query = query,
                Items = items,
                ModuleOptions = _service.GetModuleOptions(),
                ActionOptions = _service.GetActionOptions(),
                Page = normalizedPage,
                PageSize = DefaultPageSize,
                TotalItems = totalItems,
                TotalPages = GetTotalPages(totalItems, DefaultPageSize)
            });
        }

        private static int NormalizePage(int page)
        {
            return page < 1 ? 1 : page;
        }

        private static int GetTotalPages(int totalItems, int pageSize)
        {
            return totalItems <= 0 ? 1 : (int)Math.Ceiling((decimal)totalItems / pageSize);
        }

        private void NormalizeDateFilters(SystemLogQueryDto query)
        {
            var rawFromDate = Request.QueryString["FromDate"];
            var rawToDate = Request.QueryString["ToDate"];
            var hasFromDate = rawFromDate != null;
            var hasToDate = rawToDate != null;

            if (!hasFromDate && !hasToDate && !query.FromDate.HasValue && !query.ToDate.HasValue)
            {
                var today = GetVietnamLocalToday();
                query.FromDate = today;
                query.ToDate = today;
                return;
            }

            if (hasFromDate)
            {
                query.FromDate = ParseFilterDate(rawFromDate);
            }

            if (hasToDate)
            {
                query.ToDate = ParseFilterDate(rawToDate);
            }
        }

        private static DateTime? ParseFilterDate(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return null;
            }

            DateTime parsedDate;
            var formats = new[] { "dd-MM-yyyy", "d-M-yyyy", "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" };
            return DateTime.TryParseExact(
                rawValue.Trim(),
                formats,
                CultureInfo.GetCultureInfo("vi-VN"),
                DateTimeStyles.None,
                out parsedDate)
                    ? (DateTime?)parsedDate.Date
                    : null;
        }

        private static DateTime GetVietnamLocalToday()
        {
            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")).Date;
            }
            catch (TimeZoneNotFoundException)
            {
                return DateTime.Now.Date;
            }
            catch (InvalidTimeZoneException)
            {
                return DateTime.Now.Date;
            }
        }
    }
}
