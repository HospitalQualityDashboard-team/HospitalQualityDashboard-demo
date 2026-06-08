using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using HospitalQualityDashboard.Models.DTOs;
using HospitalQualityDashboard.Models.Enums;
using HospitalQualityDashboard.Models.ViewModels;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard.Areas.User.Controllers
{
    public class ReportController : UserBaseController
    {
        private readonly ReportService _service = new ReportService();
        private readonly ReportingPeriodService _periods = new ReportingPeriodService();
        private readonly ReportingPeriodScheduleService _periodSchedule = new ReportingPeriodScheduleService();
        private readonly IndicatorService _indicators = new IndicatorService();

        public ActionResult Index(int? kyBaoCaoId, int? chiSoChatLuongId)
        {
            _periodSchedule.OpenDuePeriods(System.DateTime.Now);

            var activePeriods = GetActivePeriodsForCurrentViewer();
            if (kyBaoCaoId.HasValue && activePeriods.All(p => p.KyBaoCaoId != kyBaoCaoId.Value))
            {
                kyBaoCaoId = null;
            }

            return View(new ReportListViewModel
            {
                IsAdmin = false,
                KyBaoCaoId = kyBaoCaoId,
                KhoaPhongId = CurrentKhoaPhongId,
                ChiSoChatLuongId = chiSoChatLuongId,
                KyBaoCaoOptions = BuildUserPeriodOptions(activePeriods),
                KhoaPhongOptions = new List<SelectListItem>(),
                ChiSoOptions = _indicators.GetOptions(),
                Items = _service.GetAll(new ReportListQueryDto
                {
                    PeriodId = kyBaoCaoId,
                    DepartmentId = CurrentKhoaPhongId,
                    IndicatorId = chiSoChatLuongId,
                    IsAdmin = false,
                    CurrentDepartmentId = CurrentKhoaPhongId
                }),
                ActivePeriods = activePeriods
            });
        }

        public ActionResult Nhap(int kyBaoCaoId)
        {
            var periodGate = EnsureOpenPeriodForUser(kyBaoCaoId);
            if (periodGate != null) return periodGate;
            return View(_service.GetAssignedForUser(kyBaoCaoId, CurrentKhoaPhongId.Value));
        }

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
            }
            else
            {
                if (!kyBaoCaoId.HasValue || !chiSoChatLuongId.HasValue)
                {
                    return new HttpStatusCodeResult(400, "Thieu thong tin ky bao cao hoac chi so.");
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ReportEntryViewModel model)
        {
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

            var id = _service.SaveDraft(new ReportDraftDto
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
            return RedirectToAction("Edit", new { id = id });
        }

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
            return RedirectToAction("Index");
        }

        private IList<KyBaoCaoViewModel> GetActivePeriodsForCurrentViewer()
        {
            var activePeriods = _periods.GetAll()
                .Where(p => p.TrangThai == TrangThaiKyBaoCao.Mo)
                .ToList();

            var userFreqs = _periods.GetFrequenciesForDepartment(CurrentKhoaPhongId.Value);
            return activePeriods
                .Where(p => userFreqs.Contains(p.LoaiKyBaoCao))
                .ToList();
        }

        private static IList<SelectListItem> BuildUserPeriodOptions(IEnumerable<KyBaoCaoViewModel> activePeriods)
        {
            return activePeriods
                .Select(x => new SelectListItem { Value = x.KyBaoCaoId.ToString(), Text = x.TenKyBaoCao })
                .ToList();
        }

        private ActionResult EnsureOpenPeriodForUser(int kyBaoCaoId)
        {
            if (!_periods.IsOpenForDepartment(kyBaoCaoId, CurrentKhoaPhongId.Value))
            {
                return new HttpStatusCodeResult(403, "Ky bao cao chua mo hoac khong phu hop voi phan cong cua khoa/phong.");
            }

            return null;
        }
    }
}
