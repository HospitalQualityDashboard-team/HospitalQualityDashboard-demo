// Mục đích: cho Admin tra cứu, duyệt, trả lại hoặc khóa báo cáo.
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.Enums;
using HospitalQualityDashboardDemo.Models.ViewModels;
using HospitalQualityDashboardDemo.Services;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.Admin.Controllers
{
    public class ReportController : AdminBaseController
    {
        private const int DefaultPageSize = 10;
        private readonly ReportService _service = new ReportService();
        private readonly ReportingPeriodService _periods = new ReportingPeriodService();
        private readonly DepartmentService _departments = new DepartmentService();
        private readonly IndicatorService _indicators = new IndicatorService();

        // Hiển thị danh sách và các bộ lọc của báo cáo định kỳ.
        public ActionResult Index(int? kyBaoCaoId, int? khoaPhongId, int? chiSoChatLuongId, int page = 1)
        {
            var query = new ReportListQueryDto
            {
                PeriodId = kyBaoCaoId,
                DepartmentId = khoaPhongId,
                IndicatorId = chiSoChatLuongId,
                IsAdmin = true,
                CurrentDepartmentId = null
            };
            int totalItems;
            var items = _service.GetAll(query, page, DefaultPageSize, out totalItems);
            return View(new ReportListViewModel
            {
                IsAdmin = true,
                KyBaoCaoId = kyBaoCaoId,
                KhoaPhongId = khoaPhongId,
                ChiSoChatLuongId = chiSoChatLuongId,
                KyBaoCaoOptions = _periods.GetOptions(),
                KhoaPhongOptions = _departments.GetOptions(),
                ChiSoOptions = _indicators.GetOptions(),
                Items = items,
                ActivePeriods = GetActivePeriods(),
                Page = NormalizePage(page),
                PageSize = DefaultPageSize,
                TotalItems = totalItems,
                TotalPages = GetTotalPages(totalItems, DefaultPageSize)
            });
        }

        // Tải dữ liệu hiện tại lên màn hình chỉnh sửa báo cáo định kỳ.
        public ActionResult Edit(int id)
        {
            var model = _service.Get(id);
            if (model == null)
            {
                return HttpNotFound();
            }

            ViewBag.IsAdmin = true;
            return View(model);
        }

        // Duyệt báo cáo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(int id)
        {
            _service.Approve(id, CurrentTaiKhoanId.Value);
            return RedirectToAction("Index");
        }

        // Trả lại báo cáo với nhận xét.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reject(int id, string yKienPhanHoi)
        {
            if (string.IsNullOrWhiteSpace(yKienPhanHoi))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do trả lại!";
                return RedirectToAction("Edit", new { id });
            }
            _service.Reject(id, CurrentTaiKhoanId.Value, yKienPhanHoi);
            return RedirectToAction("Index");
        }

        // Chuyển bản ghi sang trạng thái không còn cho phép chỉnh sửa.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Lock(int id)
        {
            _service.Lock(id);
            return RedirectToAction("Index");
        }

        // Lấy các kỳ báo cáo đang mở và còn phù hợp với phạm vi khoa/phòng của người xem hiện tại.
        private IList<KyBaoCaoViewModel> GetActivePeriods()
        {
            return _periods.GetAll().Where(p => p.TrangThai == TrangThaiKyBaoCao.Mo).ToList();
        }

        // Chuẩn hóa số trang để tránh page âm/0 làm sai truy vấn phân trang.
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
