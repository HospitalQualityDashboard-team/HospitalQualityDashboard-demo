using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using HospitalQualityDashboard.Models.DTOs;
using HospitalQualityDashboard.Models.Enums;
using HospitalQualityDashboard.Models.ViewModels;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard.Areas.Admin.Controllers
{
    public class ReportController : AdminBaseController
    {
        private readonly ReportService _service = new ReportService();
        private readonly ReportingPeriodService _periods = new ReportingPeriodService();
        private readonly ReportingPeriodScheduleService _periodSchedule = new ReportingPeriodScheduleService();
        private readonly DepartmentService _departments = new DepartmentService();
        private readonly IndicatorService _indicators = new IndicatorService();

        public ActionResult Index(int? kyBaoCaoId, int? khoaPhongId, int? chiSoChatLuongId)
        {
            _periodSchedule.OpenDuePeriods(System.DateTime.Now);

            return View(new ReportListViewModel
            {
                IsAdmin = true,
                KyBaoCaoId = kyBaoCaoId,
                KhoaPhongId = khoaPhongId,
                ChiSoChatLuongId = chiSoChatLuongId,
                KyBaoCaoOptions = _periods.GetOptions(),
                KhoaPhongOptions = _departments.GetOptions(),
                ChiSoOptions = _indicators.GetOptions(),
                Items = _service.GetAll(new ReportListQueryDto
                {
                    PeriodId = kyBaoCaoId,
                    DepartmentId = khoaPhongId,
                    IndicatorId = chiSoChatLuongId,
                    IsAdmin = true,
                    CurrentDepartmentId = null
                }),
                ActivePeriods = GetActivePeriods()
            });
        }

        public ActionResult Edit(int id)
        {
            var model = _service.Get(id);
            if (model == null)
            {
                return HttpNotFound();
            }

            ViewBag.IsAdmin = true;
            return View(model);
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
            _service.Lock(id);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            _service.Delete(id);
            return RedirectToAction("Index");
        }

        private IList<KyBaoCaoViewModel> GetActivePeriods()
        {
            return _periods.GetAll().Where(p => p.TrangThai == TrangThaiKyBaoCao.Mo).ToList();
        }
    }
}
