using System.Web.Mvc;
using HospitalQualityDashboard.Models.ViewModels;
using HospitalQualityDashboard.Models.Enums;
using HospitalQualityDashboard.Services;
using System;
using System.Collections.Generic;

namespace HospitalQualityDashboard.Controllers
{
    public class NotificationController : PageController
    {
        private readonly NotificationService _service = new NotificationService();
        private readonly NotificationAutomationService _automation = new NotificationAutomationService();
        private readonly DepartmentService _departments = new DepartmentService();
        private readonly DashboardService _dashboard = new DashboardService();

        public ActionResult Index()
        {
            if (IsAdmin)
            {
                _automation.Run(DateTime.Now);
            }

            return View(_service.GetForUser(CurrentTaiKhoanId.Value, IsAdmin));
        }

        public ActionResult Details(int id)
        {
            var notification = _service.GetDetailForUser(id, CurrentTaiKhoanId.Value, IsAdmin);
            if (notification == null)
            {
                return HttpNotFound();
            }

            if (!IsAdmin)
            {
                _service.MarkAsRead(id, CurrentTaiKhoanId.Value);
                notification.DaDoc = true;
            }

            var missingReports = new List<MissingReportAlertViewModel>();
            if (!IsAdmin && CurrentKhoaPhongId.HasValue && notification.KyBaoCaoId.HasValue)
            {
                var overdueOnly = notification.LoaiThongBao == LoaiThongBao.QuaHan;
                missingReports = new List<MissingReportAlertViewModel>(
                    _dashboard.GetMissingReportsForDepartment(CurrentKhoaPhongId.Value, notification.KyBaoCaoId.Value, overdueOnly));
            }

            return View(new NotificationDetailViewModel
            {
                Notification = notification,
                MissingReports = missingReports
            });
        }

        public ActionResult Create()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return View(new NotificationViewModel { KhoaPhongOptions = _departments.GetOptions() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(NotificationViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            if (string.IsNullOrWhiteSpace(model.TieuDe) || string.IsNullOrWhiteSpace(model.NoiDung))
            {
                ModelState.AddModelError("", "Vui long nhap tieu de va noi dung.");
            }

            if (!ModelState.IsValid)
            {
                model.KhoaPhongOptions = _departments.GetOptions();
                return View(model);
            }

            _service.SendManual(model, CurrentTaiKhoanId.Value);
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
        public ActionResult RunAutomation()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;

            _automation.Run(DateTime.Now);
            TempData["Message"] = "Da chay kiem tra thong bao tu dong.";
            return RedirectToAction("Index");
        }
    }
}
