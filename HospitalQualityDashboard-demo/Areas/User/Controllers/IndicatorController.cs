using HospitalQualityDashboardDemo.Models.ViewModels;
using HospitalQualityDashboardDemo.Services;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.User.Controllers
{
    public class IndicatorController : UserBaseController
    {
        private const int DefaultPageSize = 20;
        private readonly IndicatorService _service = new IndicatorService();

        public ActionResult Index(int page = 1)
        {
            int totalItems;
            var items = _service.GetAll(false, CurrentKhoaPhongId, page, DefaultPageSize, out totalItems);
            return View(new ChiSoIndexViewModel
            {
                Items = items,
                Page = NormalizePage(page),
                PageSize = DefaultPageSize,
                TotalItems = totalItems,
                TotalPages = GetTotalPages(totalItems, DefaultPageSize)
            });
        }

        public ActionResult Details(int id)
        {
            var model = _service.Get(id);
            if (model == null)
            {
                return HttpNotFound();
            }

            if (!CurrentKhoaPhongId.HasValue || !_service.IsAssigned(id, CurrentKhoaPhongId.Value))
            {
                return new HttpUnauthorizedResult("Bạn không có quyền xem chi tiết chỉ số này.");
            }

            return View(model);
        }

        private static int NormalizePage(int page)
        {
            return page < 1 ? 1 : page;
        }

        private static int GetTotalPages(int totalItems, int pageSize)
        {
            return totalItems <= 0 ? 1 : (int)System.Math.Ceiling((decimal)totalItems / pageSize);
        }
    }
}
