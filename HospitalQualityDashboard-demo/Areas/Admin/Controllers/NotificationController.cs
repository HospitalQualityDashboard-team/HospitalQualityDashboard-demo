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
        private const int DefaultPageSize = 20;
        private readonly NotificationService _service = new NotificationService();
        private readonly NotificationAutomationService _automation = new NotificationAutomationService();
        private readonly ReportingPeriodScheduleService _periodSchedule = new ReportingPeriodScheduleService();
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

        // Đánh dấu trạng thái xử lý tương ứng trong thông báo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkAsRead(int id)
        {
            _service.MarkAsRead(id, CurrentTaiKhoanId.Value);
            return RedirectToAction("Index");
        }

        // Mở các bản ghi đủ điều kiện trong thông báo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OpenDuePeriodsAndRunAutomation()
        {
            _periodSchedule.OpenDuePeriods(DateTime.Now);
            _automation.Run(DateTime.Now);
            TempData["Message"] = "Đã chạy kiểm tra thông báo tự động.";
            return RedirectToAction("Index");
        }

        // Thực thi quy trình xử lý của thông báo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RunAutomation()
        {
            return OpenDuePeriodsAndRunAutomation();
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
