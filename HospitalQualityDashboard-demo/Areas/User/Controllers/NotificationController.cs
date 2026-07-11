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
        private const int DefaultPageSize = 10;
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

        // Đánh dấu thông báo đã đọc cho người nhận hiện tại, không làm thay đổi nội dung thông báo gốc.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkAsRead(int id, int page = 1)
        {
            _service.MarkAsRead(id, CurrentTaiKhoanId.Value);
            page = NormalizePage(page);
            return RedirectToAction("Index", new { page = page });
        }

        // Đánh dấu tất cả thông báo của User hiện tại là đã đọc khi mở dropdown topbar.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkAllAsRead()
        {
            _service.MarkAllAsReadForAccount(CurrentTaiKhoanId.Value);
            return Json(new { success = true, unreadCount = 0 });
        }

        // Đánh dấu thông báo chi tiết đã đọc sau khi người dùng mở màn hình xử lý liên quan.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkDetailAsRead(int id)
        {
            _service.MarkAsRead(id, CurrentTaiKhoanId.Value);
            return RedirectToAction("Details", new { id = id });
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
