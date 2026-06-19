// Mục đích: hiển thị thông báo của khoa/phòng và đánh dấu thông báo đã đọc.
using HospitalQualityDashboardDemo.Models.Enums;
using HospitalQualityDashboardDemo.Models.ViewModels;
using HospitalQualityDashboardDemo.Services;
using System.Collections.Generic;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.User.Controllers
{
    public class NotificationController : UserBaseController
    {
        private const int DefaultPageSize = 20;
        private readonly NotificationService _service = new NotificationService();
        private readonly DashboardService _dashboard = new DashboardService();

        // Hiển thị danh sách và các bộ lọc của thông báo.
        public ActionResult Index(int page = 1)
        {
            int totalItems;
            var items = _service.GetForUser(CurrentTaiKhoanId.Value, false, page, DefaultPageSize, out totalItems);
            return View(new NotificationIndexViewModel
            {
                Items = items,
                Page = NormalizePage(page),
                PageSize = DefaultPageSize,
                TotalItems = totalItems,
                TotalPages = GetTotalPages(totalItems, DefaultPageSize)
            });
        }

        // Tải và hiển thị thông tin chi tiết của thông báo.
        public ActionResult Details(int id)
        {
            var notification = _service.GetDetailForUser(id, CurrentTaiKhoanId.Value, false);
            if (notification == null)
            {
                return HttpNotFound();
            }

            var missingReports = new List<MissingReportAlertViewModel>();
            if (notification.KyBaoCaoId.HasValue)
            {
                var overdueOnly = notification.LoaiThongBao == LoaiThongBao.QuaHan;
                missingReports = new List<MissingReportAlertViewModel>(
                    _dashboard.GetMissingReportsForDepartment(
                        CurrentKhoaPhongId.Value,
                        notification.KyBaoCaoId.Value,
                        overdueOnly,
                        notification.ChiSoChatLuongId));
            }

            return View(new NotificationDetailViewModel
            {
                Notification = notification,
                MissingReports = missingReports
            });
        }

        // Đánh dấu trạng thái xử lý tương ứng trong thông báo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkAsRead(int id, int page = 1)
        {
            _service.MarkAsRead(id, CurrentTaiKhoanId.Value);
            page = NormalizePage(page);
            return RedirectToAction("Index", new { page = page });
        }

        // Đánh dấu trạng thái xử lý tương ứng trong thông báo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkDetailAsRead(int id)
        {
            _service.MarkAsRead(id, CurrentTaiKhoanId.Value);
            return RedirectToAction("Details", new { id = id });
        }

        // Chuẩn hóa dữ liệu đầu vào trước khi dùng cho thông báo.
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
