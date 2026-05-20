using System.Linq;
using System.Web.Mvc;
using HospitalQualityDashboard.Models.ViewModels;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard.Controllers
{
    public class ReportController : PageController
    {
        private readonly ReportService _service = new ReportService();
        private readonly ReportingPeriodService _periods = new ReportingPeriodService();
        private readonly DepartmentService _departments = new DepartmentService();
        private readonly IndicatorService _indicators = new IndicatorService();

        public ActionResult Index(int? kyBaoCaoId, int? khoaPhongId, int? chiSoChatLuongId)
        {
            var effectiveDepartmentId = IsAdmin ? khoaPhongId : CurrentKhoaPhongId;
            return View(new ReportListViewModel
            {
                KyBaoCaoId = kyBaoCaoId,
                KhoaPhongId = effectiveDepartmentId,
                ChiSoChatLuongId = chiSoChatLuongId,
                KyBaoCaoOptions = _periods.GetOptions(),
                KhoaPhongOptions = _departments.GetOptions(),
                ChiSoOptions = _indicators.GetOptions(),
                Items = _service.GetAll(kyBaoCaoId, effectiveDepartmentId, chiSoChatLuongId, IsAdmin, CurrentKhoaPhongId)
            });
        }

        public ActionResult Nhap(int kyBaoCaoId)
        {
            if (!CurrentKhoaPhongId.HasValue)
            {
                return new HttpUnauthorizedResult();
            }

            return View(_service.GetAssignedForUser(kyBaoCaoId, CurrentKhoaPhongId.Value));
        }

        public ActionResult Edit(int? id, int? kyBaoCaoId, int? khoaPhongId, int? chiSoChatLuongId)
        {
            ReportEntryViewModel model;
            if (id.HasValue && id.Value > 0)
            {
                model = _service.Get(id.Value);
                var gate = EnsureUserDepartment(model.KhoaPhongId);
                if (gate != null) return gate;
            }
            else
            {
                var departmentId = IsAdmin ? khoaPhongId.GetValueOrDefault() : CurrentKhoaPhongId.GetValueOrDefault();
                var gate = EnsureUserDepartment(departmentId);
                if (gate != null) return gate;
                model = _service.GetAssignedForUser(kyBaoCaoId.Value, departmentId).First(x => x.ChiSoChatLuongId == chiSoChatLuongId.Value);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ReportEntryViewModel model)
        {
            var gate = EnsureUserDepartment(model.KhoaPhongId);
            if (gate != null) return gate;
            if (!ModelState.IsValid) return View(model);
            var id = _service.SaveDraft(model, CurrentTaiKhoanId.Value);
            return RedirectToAction("Edit", new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Submit(int id)
        {
            var report = _service.Get(id);
            var gate = EnsureUserDepartment(report.KhoaPhongId);
            if (gate != null) return gate;
            _service.Submit(id, CurrentTaiKhoanId.Value);
            return RedirectToAction("Index");
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
    }
}
