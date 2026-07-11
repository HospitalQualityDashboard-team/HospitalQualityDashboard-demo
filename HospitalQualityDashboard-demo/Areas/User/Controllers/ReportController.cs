// Mục đích: điều phối quy trình nhập, lưu nháp và gửi báo cáo của khoa/phòng được phân quyền.
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.Enums;
using HospitalQualityDashboardDemo.Models.ViewModels;
using HospitalQualityDashboardDemo.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.User.Controllers
{
    public class ReportController : UserBaseController
    {
        private const int DefaultPageSize = 10;
        private const string LockedPeriodMessage = "Kỳ báo cáo đã khóa, không thể gửi báo cáo lại.";
        private readonly ReportService _service = new ReportService();
        private readonly ReportingPeriodService _periods = new ReportingPeriodService();
        private readonly IndicatorService _indicators = new IndicatorService();

        // Hiển thị danh sách và các bộ lọc của báo cáo định kỳ.
        public ActionResult Index(int? kyBaoCaoId, int? chiSoChatLuongId, int page = 1)
        {
            var activePeriods = GetActivePeriodsForCurrentViewer();
            if (kyBaoCaoId.HasValue && activePeriods.All(p => p.KyBaoCaoId != kyBaoCaoId.Value))
            {
                kyBaoCaoId = null;
            }

            var query = new ReportListQueryDto
            {
                PeriodId = kyBaoCaoId,
                DepartmentId = CurrentKhoaPhongId,
                IndicatorId = chiSoChatLuongId,
                IsAdmin = false,
                CurrentDepartmentId = CurrentKhoaPhongId
            };
            int totalItems;
            var items = _service.GetAll(query, page, DefaultPageSize, out totalItems);

            return View(new ReportListViewModel
            {
                IsAdmin = false,
                KyBaoCaoId = kyBaoCaoId,
                KhoaPhongId = CurrentKhoaPhongId,
                ChiSoChatLuongId = chiSoChatLuongId,
                KyBaoCaoOptions = BuildUserPeriodOptions(activePeriods),
                KhoaPhongOptions = new List<SelectListItem>(),
                ChiSoOptions = _indicators.GetOptions(),
                Items = items,
                ActivePeriods = activePeriods,
                Page = NormalizePage(page),
                PageSize = DefaultPageSize,
                TotalItems = totalItems,
                TotalPages = GetTotalPages(totalItems, DefaultPageSize)
            });
        }

        // Kiểm tra kỳ báo cáo và mở màn hình nhập số liệu cho khoa/phòng hiện tại.
        public ActionResult Nhap(int kyBaoCaoId)
        {
            var periodGate = EnsureOpenPeriodForUser(kyBaoCaoId);
            if (periodGate != null) return periodGate;
            return View(_service.GetAssignedForUser(kyBaoCaoId, CurrentKhoaPhongId.Value));
        }

        // Tải dữ liệu hiện tại lên màn hình chỉnh sửa báo cáo định kỳ.
        public ActionResult Edit(int? id, int? kyBaoCaoId, int? chiSoChatLuongId)
        {
            ReportEntryViewModel model;
            if (id.HasValue && id.Value > 0)
            {
                model = _service.Get(id.Value);
                if (model == null)
                {
                    return HttpNotFound();
                }

                var gate = EnsureUserDepartment(model.KhoaPhongId);
                if (gate != null) return gate;
                SetPeriodMessageIfLocked(model);
            }
            else
            {
                if (!kyBaoCaoId.HasValue || !chiSoChatLuongId.HasValue)
                {
                    return new HttpStatusCodeResult(400, "Thiếu thông tin kỳ báo cáo hoặc chỉ số.");
                }

                var periodGate = EnsureOpenPeriodForUser(kyBaoCaoId.Value);
                if (periodGate != null) return periodGate;
                model = _service.GetAssignedForUser(kyBaoCaoId.Value, CurrentKhoaPhongId.Value).FirstOrDefault(x => x.ChiSoChatLuongId == chiSoChatLuongId.Value);
                if (model == null)
                {
                    return HttpNotFound();
                }
            }

            ViewBag.IsAdmin = false;
            return View(model);
        }

        // Kiểm tra và lưu các thay đổi của báo cáo định kỳ.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ReportEntryViewModel model, string submitAction)
        {
            ViewBag.IsAdmin = false;
            ReportEntryViewModel existingReport = null;
            if (model.BaoCaoId > 0)
            {
                existingReport = _service.Get(model.BaoCaoId);
                if (existingReport == null)
                {
                    return HttpNotFound();
                }
            }

            var departmentId = existingReport == null ? model.KhoaPhongId : existingReport.KhoaPhongId;
            var periodId = existingReport == null ? model.KyBaoCaoId : existingReport.KyBaoCaoId;
            var gate = EnsureUserDepartment(departmentId);
            if (gate != null) return gate;
            var periodGate = EnsureOpenPeriodForUser(periodId);
            if (periodGate != null) return periodGate;
            if (!ModelState.IsValid) return View(model);

            int id;
            try
            {
                id = _service.SaveDraft(new ReportDraftDto
                {
                    BaoCaoId = model.BaoCaoId,
                    KyBaoCaoId = model.KyBaoCaoId,
                    KhoaPhongId = model.KhoaPhongId,
                    ChiSoChatLuongId = model.ChiSoChatLuongId,
                    PhanCongChiSoId = model.PhanCongChiSoId,
                    TrangThai = model.TrangThai,
                    TuSo = model.TuSo,
                    MauSo = model.MauSo,
                    GiaTriNhap = model.GiaTriNhap,
                    KetQua = model.KetQua,
                    DatMucTieu = model.DatMucTieu,
                    GhiChu = model.GhiChu,
                    YKienPhanHoi = model.YKienPhanHoi
                }, CurrentTaiKhoanId.Value);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == IndicatorCalculationService.NumeratorCannotExceedDenominatorMessage)
                {
                    ModelState.AddModelError("TuSo", IndicatorCalculationService.NumeratorCannotExceedDenominatorMessage);
                }
                else
                {
                    ModelState.AddModelError("", ex.Message);
                }

                return View(model);
            }

            if (submitAction == "submit")
            {
                _service.Submit(id, CurrentTaiKhoanId.Value);
                return RedirectToAction("Nhap", new { kyBaoCaoId = model.KyBaoCaoId });
            }

            return RedirectToAction("Edit", new { id = id });
        }

        // Gửi dữ liệu và cập nhật trạng thái tương ứng của báo cáo định kỳ.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Submit(int id)
        {
            var report = _service.Get(id);
            if (report == null)
            {
                return HttpNotFound();
            }

            var gate = EnsureUserDepartment(report.KhoaPhongId);
            if (gate != null) return gate;
            var periodGate = EnsureOpenPeriodForUser(report.KyBaoCaoId);
            if (periodGate != null) return periodGate;
            _service.Submit(id, CurrentTaiKhoanId.Value);
            return RedirectToAction("Nhap", new { kyBaoCaoId = report.KyBaoCaoId });
        }

        // Lấy các kỳ báo cáo đang mở và còn phù hợp với phạm vi khoa/phòng của người xem hiện tại.
        private IList<KyBaoCaoViewModel> GetActivePeriodsForCurrentViewer()
        {
            var activePeriods = _periods.GetAll()
                .Where(p => p.TrangThai == TrangThaiKyBaoCao.Mo)
                .ToList();

            return activePeriods
                .Where(p => _periods.IsOpenForDepartment(p.KyBaoCaoId, CurrentKhoaPhongId.Value))
                .ToList();
        }

        // Dựng cấu trúc dữ liệu báo cáo định kỳ từ input đã lọc để tái sử dụng cho truy vấn, view hoặc xuất file.
        private static IList<SelectListItem> BuildUserPeriodOptions(IEnumerable<KyBaoCaoViewModel> activePeriods)
        {
            return activePeriods
                .Select(x => new SelectListItem { Value = x.KyBaoCaoId.ToString(), Text = x.TenKyBaoCao })
                .ToList();
        }

        // Chỉ cho nhập hoặc gửi báo cáo khi kỳ đang mở với khoa/phòng của người dùng hiện tại.
        private ActionResult EnsureOpenPeriodForUser(int kyBaoCaoId)
        {
            if (!_periods.IsOpenForDepartment(kyBaoCaoId, CurrentKhoaPhongId.Value))
            {
                TempData["Error"] = GetPeriodGateMessage(kyBaoCaoId);
                return RedirectToAction("Index");
            }

            return null;
        }

        private string GetPeriodGateMessage(int kyBaoCaoId)
        {
            var period = _periods.Get(kyBaoCaoId);
            if (period != null && period.TrangThai == TrangThaiKyBaoCao.Khoa)
            {
                return LockedPeriodMessage;
            }

            return "Kỳ báo cáo chưa mở hoặc không phù hợp với phân công của khoa/phòng.";
        }

        private void SetPeriodMessageIfLocked(ReportEntryViewModel model)
        {
            if (model != null && model.TrangThaiKyBaoCao == TrangThaiKyBaoCao.Khoa)
            {
                ViewBag.PeriodLockedMessage = LockedPeriodMessage;
            }
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
