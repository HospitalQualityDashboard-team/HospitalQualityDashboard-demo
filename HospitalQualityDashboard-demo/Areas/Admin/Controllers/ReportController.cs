using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.Enums;
using HospitalQualityDashboardDemo.Models.ViewModels;
using HospitalQualityDashboardDemo.Services;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.Admin.Controllers
{
    public class ReportController : AdminBaseController
    {
        private const int DefaultPageSize = 20;
        private readonly ReportService _service = new ReportService();
        private readonly ReportingPeriodService _periods = new ReportingPeriodService();
        private readonly DepartmentService _departments = new DepartmentService();
        private readonly IndicatorService _indicators = new IndicatorService();

        public ActionResult Index(int? kyBaoCaoId, int? khoaPhongId, int? chiSoChatLuongId, int page = 1)
        {
            var query = new ReportListQueryDto
            {
                PeriodId = kyBaoCaoId,
                DepartmentId = khoaPhongId,
                IndicatorId = chiSoChatLuongId,
                IsAdmin = true,
                CurrentDepartmentId = null
            };
            int totalItems;
            var items = _service.GetAll(query, page, DefaultPageSize, out totalItems);
            return View(new ReportListViewModel
            {
                IsAdmin = true,
                KyBaoCaoId = kyBaoCaoId,
                KhoaPhongId = khoaPhongId,
                ChiSoChatLuongId = chiSoChatLuongId,
                KyBaoCaoOptions = _periods.GetOptions(),
                KhoaPhongOptions = _departments.GetOptions(),
                ChiSoOptions = _indicators.GetOptions(),
                Items = items,
                ActivePeriods = GetActivePeriods(),
                Page = NormalizePage(page),
                PageSize = DefaultPageSize,
                TotalItems = totalItems,
                TotalPages = GetTotalPages(totalItems, DefaultPageSize)
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
            return new HttpStatusCodeResult(410, "Quy trình duyệt báo cáo hiện không được sử dụng.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reject(int id, string yKienPhanHoi)
        {
            return new HttpStatusCodeResult(410, "Quy trình duyệt báo cáo hiện không được sử dụng.");
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
            _service.Delete(id, CurrentTaiKhoanId.Value);
            return RedirectToAction("Index");
        }

        private IList<KyBaoCaoViewModel> GetActivePeriods()
        {
            return _periods.GetAll().Where(p => p.TrangThai == TrangThaiKyBaoCao.Mo).ToList();
        }

        private static int NormalizePage(int page)
        {
            return page < 1 ? 1 : page;
        }

        private static int GetTotalPages(int totalItems, int pageSize)
        {
            return totalItems <= 0 ? 1 : (int)System.Math.Ceiling((decimal)totalItems / pageSize);
        }
    }
}
