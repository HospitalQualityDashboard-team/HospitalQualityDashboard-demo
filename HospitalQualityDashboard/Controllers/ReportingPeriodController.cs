// Mục đích: quản lý kỳ báo cáo và tạo lịch kỳ báo cáo tự động theo tần suất.
using System;
using System.Web.Mvc;
using HospitalQualityDashboard.Models.Enums;
using HospitalQualityDashboard.Models.ViewModels;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard.Controllers
{
    public class ReportingPeriodController : PageController
    {
        private readonly ReportingPeriodService _service = new ReportingPeriodService();
        private readonly ReportingPeriodScheduleService _schedule = new ReportingPeriodScheduleService();

        public ActionResult Index()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            _schedule.OpenDuePeriods(DateTime.Now);
            return View(_service.GetAll());
        }

        public ActionResult GenerateSchedule()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return View(_schedule.CreateDefaultRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PreviewSchedule(ReportingPeriodScheduleRequestViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;

            if (ModelState.IsValid)
            {
                try
                {
                    model.PreviewItems = _schedule.BuildSchedulePreview(model, DateTime.Now);
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            return View("GenerateSchedule", _schedule.PopulateOptions(model));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateSchedule(ReportingPeriodScheduleRequestViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;

            if (!ModelState.IsValid)
            {
                return View("GenerateSchedule", _schedule.PopulateOptions(model));
            }

            try
            {
                var result = _schedule.GenerateSchedule(model, DateTime.Now);
                TempData["Message"] = string.Format("Đã tạo {0} kỳ báo cáo mới, bỏ qua {1} kỳ đã tồn tại, tự mở {2} kỳ đến ngày bắt đầu.",
                    result.CreatedCount,
                    result.SkippedExistingCount,
                    result.OpenedCount);
                return RedirectToAction("Index");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                model.PreviewItems = new System.Collections.Generic.List<ReportingPeriodSchedulePreviewItemViewModel>();
                return View("GenerateSchedule", _schedule.PopulateOptions(model));
            }
        }

        public ActionResult Create()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return View("Edit", new KyBaoCaoViewModel { TrangThai = TrangThaiKyBaoCao.Nhap });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(KyBaoCaoViewModel model)
        {
            return Save(model);
        }

        public ActionResult Edit(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return View(_service.Get(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(KyBaoCaoViewModel model)
        {
            return Save(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Open(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            _service.SetStatus(id, TrangThaiKyBaoCao.Mo);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Lock(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            _service.SetStatus(id, TrangThaiKyBaoCao.Khoa);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            try
            {
                _service.Delete(id);
            }
            catch (System.InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        private ActionResult Save(KyBaoCaoViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            if (!ModelState.IsValid) return View("Edit", model);
            _service.Save(model);
            return RedirectToAction("Index");
        }
    }
}
