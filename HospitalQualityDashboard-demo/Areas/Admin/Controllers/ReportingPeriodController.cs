// Mục đích: quản trị kỳ báo cáo và điều phối việc sinh lịch định kỳ theo tần suất chỉ số.
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.Enums;
using HospitalQualityDashboardDemo.Models.ViewModels;
using HospitalQualityDashboardDemo.Services;
using System;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.Admin.Controllers
{
    public class ReportingPeriodController : AdminBaseController
    {
        private const int DefaultPageSize = 20;
        private readonly ReportingPeriodService _service = new ReportingPeriodService();
        private readonly ReportingPeriodScheduleService _schedule = new ReportingPeriodScheduleService();

        // Hiển thị danh sách và các bộ lọc của kỳ báo cáo.
        public ActionResult Index(int page = 1)
        {
            int totalItems;
            var items = _service.GetAll(page, DefaultPageSize, out totalItems);
            return View(new KyBaoCaoIndexViewModel
            {
                Items = items,
                Page = NormalizePage(page),
                PageSize = DefaultPageSize,
                TotalItems = totalItems,
                TotalPages = GetTotalPages(totalItems, DefaultPageSize)
            });
        }

        // Mở các bản ghi đủ điều kiện trong kỳ báo cáo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OpenDuePeriods()
        {
            var openedCount = _schedule.OpenDuePeriods(DateTime.Now);
            TempData["Message"] = string.Format("Đã mở {0} kỳ báo cáo đến ngày bắt đầu.", openedCount);
            return RedirectToAction("Index");
        }

        // Sinh các kỳ báo cáo theo tần suất và khoảng thời gian được yêu cầu.
        public ActionResult GenerateSchedule()
        {
            return View(_schedule.CreateDefaultRequest());
        }

        // Tạo dữ liệu xem trước để người dùng kiểm tra trước khi ghi chính thức.
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

        // Tạo cấu trúc dữ liệu phục vụ kỳ báo cáo.
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

        // Khởi tạo dữ liệu cho màn hình tạo mới kỳ báo cáo.
        public ActionResult Create()
        {
            return View("Edit", new KyBaoCaoViewModel { TrangThai = TrangThaiKyBaoCao.Nhap });
        }

        // Kiểm tra dữ liệu gửi lên và tạo mới kỳ báo cáo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(KyBaoCaoViewModel model)
        {
            return Save(model);
        }

        // Tải dữ liệu hiện tại lên màn hình chỉnh sửa kỳ báo cáo.
        public ActionResult Edit(int id)
        {
            return View(_service.Get(id));
        }

        // Kiểm tra và lưu các thay đổi của kỳ báo cáo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(KyBaoCaoViewModel model)
        {
            return Save(model);
        }

        // Mở các bản ghi đủ điều kiện trong kỳ báo cáo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Open(int id)
        {
            _service.SetStatus(id, TrangThaiKyBaoCao.Mo);
            return RedirectToAction("Index");
        }

        // Chuyển bản ghi sang trạng thái không còn cho phép chỉnh sửa.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Lock(int id)
        {
            _service.SetStatus(id, TrangThaiKyBaoCao.Khoa);
            return RedirectToAction("Index");
        }

        // Xóa bản ghi được chọn sau khi áp dụng các ràng buộc của kỳ báo cáo.
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

        // Kiểm tra và cập nhật dữ liệu của kỳ báo cáo.
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

        // Chuẩn hóa dữ liệu đầu vào trước khi dùng cho kỳ báo cáo.
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
