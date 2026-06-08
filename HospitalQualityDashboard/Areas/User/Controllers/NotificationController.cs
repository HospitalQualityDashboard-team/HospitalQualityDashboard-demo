using System.Collections.Generic;
using System.Web.Mvc;
using HospitalQualityDashboard.Models.Enums;
using HospitalQualityDashboard.Models.ViewModels;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard.Areas.User.Controllers
{
    public class NotificationController : UserBaseController
    {
        private readonly NotificationService _service = new NotificationService();
        private readonly DashboardService _dashboard = new DashboardService();

        public ActionResult Index()
        {
            return View(_service.GetForUser(CurrentTaiKhoanId.Value, false));
        }

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
                    _dashboard.GetMissingReportsForDepartment(CurrentKhoaPhongId.Value, notification.KyBaoCaoId.Value, overdueOnly));
            }

            return View(new NotificationDetailViewModel
            {
                Notification = notification,
                MissingReports = missingReports
            });
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
        public ActionResult MarkDetailAsRead(int id)
        {
            _service.MarkAsRead(id, CurrentTaiKhoanId.Value);
            return RedirectToAction("Details", new { id = id });
        }
    }
}
