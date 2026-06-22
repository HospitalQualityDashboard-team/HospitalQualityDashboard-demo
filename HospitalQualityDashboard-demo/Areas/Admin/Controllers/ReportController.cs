// Mục đích: cho Admin tra cứu, khóa hoặc xóa báo cáo; quy trình duyệt cũ không còn được sử dụng.
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
        private const int DefaultPageSize = 20;
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

        // Xử lý trạng thái phản hồi quản trị của báo cáo định kỳ.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(int id)
        {
            return new HttpStatusCodeResult(410, "Quy trình duyệt báo cáo hiện không được sử dụng.");
        }

        // Xử lý trạng thái phản hồi quản trị của báo cáo định kỳ.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reject(int id, string yKienPhanHoi)
        {
            return new HttpStatusCodeResult(410, "Quy trình duyệt báo cáo hiện không được sử dụng.");
        }

        // Chuyển bản ghi sang trạng thái không còn cho phép chỉnh sửa.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Lock(int id)
        {
            _service.Lock(id);
            return RedirectToAction("Index");
        }

        // Ẩn bản ghi (soft delete) thay vì xóa hoàn toàn.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            _service.Hide(id, CurrentTaiKhoanId.Value);
            return RedirectToAction("Index");
        }

        // Truy vấn báo cáo định kỳ theo điều kiện được cung cấp.
        private IList<KyBaoCaoViewModel> GetActivePeriods()
        {
            return _periods.GetAll().Where(p => p.TrangThai == TrangThaiKyBaoCao.Mo).ToList();
        }

        // Chuẩn hóa dữ liệu đầu vào trước khi dùng cho báo cáo định kỳ.
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
