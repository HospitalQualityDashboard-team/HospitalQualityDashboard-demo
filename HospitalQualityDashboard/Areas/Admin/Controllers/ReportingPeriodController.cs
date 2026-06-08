using System;
using System.Web.Mvc;
using HospitalQualityDashboard.Models.DTOs;
using HospitalQualityDashboard.Models.Enums;
using HospitalQualityDashboard.Models.ViewModels;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard.Areas.Admin.Controllers
{
    public class ReportingPeriodController : AdminBaseController
    {
        private readonly ReportingPeriodService _service = new ReportingPeriodService();
        private readonly ReportingPeriodScheduleService _schedule = new ReportingPeriodScheduleService();

        public ActionResult Index()
        {
            return View(_service.GetAll());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OpenDuePeriods()
        {
            var openedCount = _schedule.OpenDuePeriods(DateTime.Now);
            TempData["Message"] = string.Format("Đã mở {0} kỳ báo cáo đến ngày bắt đầu.", openedCount);
            return RedirectToAction("Index");
        }

        public ActionResult GenerateSchedule()
        {
            return View(_schedule.CreateDefaultRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PreviewSchedule(ReportingPeriodScheduleRequestViewModel model)
        {
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
            if (!ModelState.IsValid)
            {
                return View("GenerateSchedule", _schedule.PopulateOptions(model));
            }

            try
            {
                var result = _schedule.GenerateSchedule(new ReportingPeriodScheduleDto
                {
                    Year = model.Year,
                    SelectedFrequencyValues = model.SelectedFrequencyValues,
                    DueDayOffset = model.DueDayOffset,
                    DefaultStatus = model.DefaultStatus
                }, DateTime.Now);
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
            _service.SetStatus(id, TrangThaiKyBaoCao.Mo);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Lock(int id)
        {
            _service.SetStatus(id, TrangThaiKyBaoCao.Khoa);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
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
            if (!ModelState.IsValid) return View("Edit", model);
            _service.Save(new ReportingPeriodSaveDto
            {
                KyBaoCaoId = model.KyBaoCaoId,
                TenKyBaoCao = model.TenKyBaoCao,
                LoaiKyBaoCao = model.LoaiKyBaoCao,
                TuNgay = model.TuNgay,
                DenNgay = model.DenNgay,
                HanNop = model.HanNop,
                TrangThai = model.TrangThai
            });
            return RedirectToAction("Index");
        }
    }
}
