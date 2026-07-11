// Mục đích: hiển thị nhật ký thao tác hệ thống cho Admin.
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.ViewModels;
using HospitalQualityDashboardDemo.Services;
using System;
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
    }
}
