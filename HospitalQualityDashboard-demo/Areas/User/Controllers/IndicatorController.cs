// Mục đích: cho người dùng xem các chỉ số được phân công cho khoa/phòng của mình.
using HospitalQualityDashboardDemo.Models.ViewModels;
using HospitalQualityDashboardDemo.Services;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.User.Controllers
{
    public class IndicatorController : UserBaseController
    {
        private const int DefaultPageSize = 20;
        private readonly IndicatorService _service = new IndicatorService();

        // Hiển thị danh sách và các bộ lọc của chỉ số chất lượng.
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

        // Tải và hiển thị thông tin chi tiết của chỉ số chất lượng.
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

        // Chuẩn hóa dữ liệu đầu vào trước khi dùng cho chỉ số chất lượng.
        private static int NormalizePage(int page)
        {
            return page < 1 ? 1 : page;
        }

        // Tính tổng số trang từ số bản ghi và kích thước trang.
        private static int GetTotalPages(int totalItems, int pageSize)
        {
            return totalItems <= 0 ? 1 : (int)System.Math.Ceiling((decimal)totalItems / pageSize);
        }
    }
}
