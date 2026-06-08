using System;
using System.Web.Mvc;
using HospitalQualityDashboard.Models.DTOs;
using HospitalQualityDashboard.Models.ViewModels;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard.Areas.Admin.Controllers
{
    public class NotificationController : AdminBaseController
    {
        private readonly NotificationService _service = new NotificationService();
        private readonly NotificationAutomationService _automation = new NotificationAutomationService();
        private readonly ReportingPeriodScheduleService _periodSchedule = new ReportingPeriodScheduleService();
        private readonly DepartmentService _departments = new DepartmentService();

        public ActionResult Index()
        {
            return View(_service.GetForUser(CurrentTaiKhoanId.Value, true));
        }

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

        public ActionResult Create()
        {
            return View(new NotificationViewModel { KhoaPhongOptions = _departments.GetOptions() });
        }

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
                BaoCaoId = model.BaoCaoId,
                SelectedKhoaPhongIds = model.SelectedKhoaPhongIds
            }, CurrentTaiKhoanId.Value);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkAsRead(int id)
        {
            _service.MarkAsRead(id, CurrentTaiKhoanId.Value);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OpenDuePeriodsAndRunAutomation()
        {
            _periodSchedule.OpenDuePeriods(DateTime.Now);
            _automation.Run(DateTime.Now);
            TempData["Message"] = "Đã chạy kiểm tra thông báo tự động.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RunAutomation()
        {
            return OpenDuePeriodsAndRunAutomation();
        }
    }
}
