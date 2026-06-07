// Mục đích: điều phối quy trình nhập, gửi, duyệt, từ chối và khóa báo cáo.
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using HospitalQualityDashboard.Models.Enums;
using HospitalQualityDashboard.Models.ViewModels;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard.Controllers
{
    public class ReportController : PageController
    {
        private readonly ReportService _service = new ReportService();
        private readonly ReportingPeriodService _periods = new ReportingPeriodService();
        private readonly ReportingPeriodScheduleService _periodSchedule = new ReportingPeriodScheduleService();
        private readonly DepartmentService _departments = new DepartmentService();
        private readonly IndicatorService _indicators = new IndicatorService();

        public ActionResult Index(int? kyBaoCaoId, int? khoaPhongId, int? chiSoChatLuongId)
        {
            _periodSchedule.OpenDuePeriods(System.DateTime.Now);

            var effectiveDepartmentId = IsAdmin ? khoaPhongId : CurrentKhoaPhongId;
            var activePeriods = GetActivePeriodsForCurrentViewer();

            if (!IsAdmin && kyBaoCaoId.HasValue && activePeriods.All(p => p.KyBaoCaoId != kyBaoCaoId.Value))
            {
                kyBaoCaoId = null;
            }

            return View(new ReportListViewModel
            {
                IsAdmin = IsAdmin,
                KyBaoCaoId = kyBaoCaoId,
                KhoaPhongId = effectiveDepartmentId,
                ChiSoChatLuongId = chiSoChatLuongId,
                KyBaoCaoOptions = IsAdmin ? _periods.GetOptions() : BuildUserPeriodOptions(activePeriods),
                KhoaPhongOptions = _departments.GetOptions(),
                ChiSoOptions = _indicators.GetOptions(),
                Items = _service.GetAll(kyBaoCaoId, effectiveDepartmentId, chiSoChatLuongId, IsAdmin, CurrentKhoaPhongId),
                ActivePeriods = activePeriods
            });
        }

        public ActionResult Nhap(int kyBaoCaoId)
        {
            var periodGate = EnsureOpenPeriodForUser(kyBaoCaoId);
            if (periodGate != null) return periodGate;

            return View(_service.GetAssignedForUser(kyBaoCaoId, CurrentKhoaPhongId.Value));
        }

        public ActionResult Edit(int? id, int? kyBaoCaoId, int? khoaPhongId, int? chiSoChatLuongId)
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
                if (IsAdmin)
                {
                    return new HttpUnauthorizedResult();
                }

                if (!kyBaoCaoId.HasValue || !chiSoChatLuongId.HasValue)
                {
                    return new HttpStatusCodeResult(400, "Thieu thong tin ky bao cao hoac chi so.");
                }

                var periodGate = EnsureOpenPeriodForUser(kyBaoCaoId.Value);
                if (periodGate != null) return periodGate;

                var departmentId = IsAdmin ? khoaPhongId.GetValueOrDefault() : CurrentKhoaPhongId.GetValueOrDefault();
                var gate = EnsureUserDepartment(departmentId);
                if (gate != null) return gate;
                model = _service.GetAssignedForUser(kyBaoCaoId.Value, departmentId).FirstOrDefault(x => x.ChiSoChatLuongId == chiSoChatLuongId.Value);
                if (model == null)
                {
                    return HttpNotFound();
                }
            }

            ViewBag.IsAdmin = IsAdmin;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ReportEntryViewModel model)
        {
            if (IsAdmin)
            {
                return new HttpUnauthorizedResult();
            }

            var gate = EnsureUserDepartment(model.KhoaPhongId);
            if (gate != null) return gate;
            var periodGate = EnsureOpenPeriodForUser(model.KyBaoCaoId);
            if (periodGate != null) return periodGate;
            if (!ModelState.IsValid) return View(model);
            var id = _service.SaveDraft(model, CurrentTaiKhoanId.Value);
            return RedirectToAction("Edit", new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Submit(int id)
        {
            if (IsAdmin)
            {
                return new HttpUnauthorizedResult();
            }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(int id)
        {
            return new HttpStatusCodeResult(410, "Approval workflow is disabled.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reject(int id, string yKienPhanHoi)
        {
            return new HttpStatusCodeResult(410, "Approval workflow is disabled.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Lock(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            _service.Lock(id);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            _service.Delete(id);
            return RedirectToAction("Index");
        }

        private IList<KyBaoCaoViewModel> GetActivePeriodsForCurrentViewer()
        {
            var activePeriods = _periods.GetAll()
                .Where(p => p.TrangThai == TrangThaiKyBaoCao.Mo)
                .ToList();

            if (!IsAdmin)
            {
                if (!CurrentKhoaPhongId.HasValue)
                {
                    return new List<KyBaoCaoViewModel>();
                }

                var userFreqs = _periods.GetFrequenciesForDepartment(CurrentKhoaPhongId.Value);
                activePeriods = activePeriods
                    .Where(p => userFreqs.Contains(p.LoaiKyBaoCao))
                    .ToList();
            }

            return activePeriods;
        }

        private static IList<SelectListItem> BuildUserPeriodOptions(IEnumerable<KyBaoCaoViewModel> activePeriods)
        {
            return activePeriods
                .Select(x => new SelectListItem { Value = x.KyBaoCaoId.ToString(), Text = x.TenKyBaoCao })
                .ToList();
        }

        private ActionResult EnsureOpenPeriodForUser(int kyBaoCaoId)
        {
            if (IsAdmin)
            {
                return new HttpUnauthorizedResult();
            }

            if (!CurrentKhoaPhongId.HasValue)
            {
                return new HttpUnauthorizedResult();
            }

            if (!_periods.IsOpenForDepartment(kyBaoCaoId, CurrentKhoaPhongId.Value))
            {
                return new HttpStatusCodeResult(403, "Ky bao cao chua mo hoac khong phu hop voi phan cong cua khoa/phong.");
            }

            return null;
        }
    }
}
