// Mục đích: quản lý thông báo và kích hoạt các thông báo tự động theo tiến độ kỳ báo cáo.
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.ViewModels;
using HospitalQualityDashboardDemo.Services;
using System;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.Admin.Controllers
{
    public class NotificationController : AdminBaseController
    {
        private const int DefaultPageSize = 10;
        private readonly NotificationService _service = new NotificationService();
        private readonly ReportingPeriodMaintenanceService _maintenance = new ReportingPeriodMaintenanceService();
        private readonly DepartmentService _departments = new DepartmentService();

        // Hiển thị danh sách và các bộ lọc của thông báo.
        public ActionResult Index(int page = 1)
        {
            int totalItems;
            var items = _service.GetForUser(CurrentTaiKhoanId.Value, true, page, DefaultPageSize, out totalItems);
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
            var notification = _service.GetDetailForUser(id, CurrentTaiKhoanId.Value, true);
            if (notification == null)
            {
                return HttpNotFound();
            }

            return View(new NotificationDetailViewModel
            {
                Notification = notification,
                MissingReports = new System.Collections.Generic.List<MissingReportAlertViewModel>()
            });
        }

        // Khởi tạo dữ liệu cho màn hình tạo mới thông báo.
        public ActionResult Create()
        {
            return View(new NotificationViewModel { KhoaPhongOptions = _departments.GetOptions() });
        }

        // Kiểm tra dữ liệu gửi lên và tạo mới thông báo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(NotificationViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.TieuDe) || string.IsNullOrWhiteSpace(model.NoiDung))
            {
                ModelState.AddModelError("", "Vui lòng nhập tiêu đề và nội dung.");
            }

            if (!ModelState.IsValid)
            {
                model.KhoaPhongOptions = _departments.GetOptions();
                return View(model);
            }

            _service.SendManual(new NotificationSendDto
            {
                TieuDe = model.TieuDe,
                NoiDung = model.NoiDung,
                LoaiThongBao = model.LoaiThongBao,
                KyBaoCaoId = model.KyBaoCaoId,
                ChiSoChatLuongId = model.ChiSoChatLuongId,
                BaoCaoId = model.BaoCaoId,
                SelectedKhoaPhongIds = model.SelectedKhoaPhongIds
            }, CurrentTaiKhoanId.Value);
            return RedirectToAction("Index");
        }

        // Đánh dấu thông báo đã đọc cho người nhận hiện tại, không làm thay đổi nội dung thông báo gốc.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkAsRead(int id)
        {
            _service.MarkAsRead(id, CurrentTaiKhoanId.Value);
            return RedirectToAction("Index");
        }

        // Kích hoạt tự động mở kỳ và sinh thông báo nhắc hạn/quá hạn theo lịch nghiệp vụ.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OpenDuePeriodsAndRunAutomation()
        {
            var result = _maintenance.Run(DateTime.Now);
            TempData["Message"] = string.Format("Đã chạy tự động: mở {0} kỳ, khóa {1} kỳ.",
                result.OpenedCount,
                result.ClosedCount);
            return RedirectToAction("Index");
        }

        // Chạy sinh thông báo tự động theo tiến độ kỳ báo cáo và quay lại danh sách để Admin kiểm tra.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RunAutomation()
        {
            return OpenDuePeriodsAndRunAutomation();
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
