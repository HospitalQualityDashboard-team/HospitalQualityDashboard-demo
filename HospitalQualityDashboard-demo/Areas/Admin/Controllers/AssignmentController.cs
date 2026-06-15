using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.ViewModels;
using HospitalQualityDashboardDemo.Services;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.Admin.Controllers
{
    public class AssignmentController : AdminBaseController
    {
        private readonly AssignmentService _service = new AssignmentService();
        private readonly DepartmentService _departments = new DepartmentService();
        private readonly IndicatorService _indicators = new IndicatorService();
        private const int PageSize = 20;

        public ActionResult Index(int? khoaPhongId, int? chiSoId, string trangThai, string trangThaiPhanCong, string search, string viewMode, int page = 1)
        {
            if (string.IsNullOrEmpty(viewMode))
            {
                viewMode = "byIndicator";
            }

            var query = new AssignmentQueryDto
            {
                KhoaPhongId = khoaPhongId,
                ChiSoId = chiSoId,
                TrangThai = trangThai,
                TrangThaiPhanCong = trangThaiPhanCong,
                Search = search,
                ViewMode = viewMode,
                Page = page,
                PageSize = PageSize
            };

            var model = _service.GetStatistics();
            model.KhoaPhongId = khoaPhongId.GetValueOrDefault();
            model.ChiSoChatLuongId = chiSoId.GetValueOrDefault();
            model.FilterKhoaPhongId = khoaPhongId;
            model.FilterChiSoId = chiSoId;
            model.FilterTrangThai = trangThai;
            model.FilterTrangThaiPhanCong = trangThaiPhanCong;
            model.Search = search;
            model.ViewMode = viewMode;
            model.PageSize = PageSize;
            model.KhoaPhongOptions = _departments.GetOptions();
            model.ChiSoOptions = _indicators.GetOptions();

            var totalItems = 0;
            if (query.ViewMode == "byDepartment")
            {
                totalItems = _service.GetDepartmentsCount(query.Search, query.KhoaPhongId, query.ChiSoId, query.TrangThai);
                model.TotalItems = totalItems;
                model.TotalPages = totalItems > 0 ? (int)System.Math.Ceiling((double)totalItems / query.PageSize) : 1;
                model.CurrentPage = System.Math.Max(1, System.Math.Min(query.Page, model.TotalPages));
                model.DepartmentGroups = _service.GetAllDepartmentGroups(query.Search, model.CurrentPage, query.PageSize, query.KhoaPhongId, query.ChiSoId, query.TrangThai);
                model.Items = new System.Collections.Generic.List<AssignmentItemViewModel>();
                model.IndicatorGroups = new System.Collections.Generic.List<IndicatorAssignmentGroup>();
            }
            else if (query.ViewMode == "byIndicator")
            {
                totalItems = _service.GetIndicatorsCount(query.TrangThaiPhanCong, query.Search, query.KhoaPhongId, query.ChiSoId, query.TrangThai);
                model.TotalItems = totalItems;
                model.TotalPages = totalItems > 0 ? (int)System.Math.Ceiling((double)totalItems / query.PageSize) : 1;
                model.CurrentPage = System.Math.Max(1, System.Math.Min(query.Page, model.TotalPages));
                model.IndicatorGroups = _service.GetAllIndicatorGroups(query.TrangThaiPhanCong, query.Search, model.CurrentPage, query.PageSize, query.KhoaPhongId, query.ChiSoId, query.TrangThai);
                model.Items = new System.Collections.Generic.List<AssignmentItemViewModel>();
                model.DepartmentGroups = new System.Collections.Generic.List<DepartmentAssignmentGroup>();
            }
            else
            {
                totalItems = _service.GetCount(query.KhoaPhongId, query.ChiSoId, query.TrangThai, query.Search);
                model.TotalItems = totalItems;
                model.TotalPages = totalItems > 0 ? (int)System.Math.Ceiling((double)totalItems / query.PageSize) : 1;
                model.CurrentPage = System.Math.Max(1, System.Math.Min(query.Page, model.TotalPages));
                model.Items = _service.GetAll(query.KhoaPhongId, query.ChiSoId, query.TrangThai, query.Search, model.CurrentPage, query.PageSize);
                model.DepartmentGroups = new System.Collections.Generic.List<DepartmentAssignmentGroup>();
                model.IndicatorGroups = new System.Collections.Generic.List<IndicatorAssignmentGroup>();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Assign(AssignmentViewModel model)
        {
            _service.Assign(new AssignmentCommandDto
            {
                DepartmentIds = model.SelectedKhoaPhongIds,
                IndicatorIds = model.SelectedChiSoIds,
                CurrentUserId = CurrentTaiKhoanId.Value
            });
            TempData["Success"] = "Đã phân công chỉ số thành công!";
            return RedirectToAction("Index", new { viewMode = model.ViewMode });
        }

        [HttpPost]
        public JsonResult Preview(int[] departmentIds, int[] indicatorIds)
        {
            return Json(_service.Preview(departmentIds, indicatorIds));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Deactivate(int id, string viewMode, int? khoaPhongId, int? chiSoId, string trangThai, string search, int page = 1)
        {
            _service.Deactivate(id);
            return RedirectToAction("Index", new { viewMode = viewMode, khoaPhongId = khoaPhongId, chiSoId = chiSoId, trangThai = trangThai, search = search, page = page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Activate(int id, string viewMode, int? khoaPhongId, int? chiSoId, string trangThai, string search, int page = 1)
        {
            _service.Activate(id);
            return RedirectToAction("Index", new { viewMode = viewMode, khoaPhongId = khoaPhongId, chiSoId = chiSoId, trangThai = trangThai, search = search, page = page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SyncFromIndicators(string viewMode)
        {
            var changed = _service.SyncFromIndicatorSources(CurrentTaiKhoanId.Value);
            TempData["Success"] = "Đã đồng bộ " + changed + " phân công từ dữ liệu chỉ số.";
            return RedirectToAction("Index", new { viewMode = viewMode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, string viewMode, int? khoaPhongId, int? chiSoId, string trangThai, string search, int page = 1)
        {
            try
            {
                _service.Delete(id);
            }
            catch (System.InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index", new { viewMode = viewMode, khoaPhongId = khoaPhongId, chiSoId = chiSoId, trangThai = trangThai, search = search, page = page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BulkDeactivate(int[] ids, string viewMode)
        {
            if (ids != null && ids.Length > 0) _service.BulkDeactivate(ids);
            return RedirectToAction("Index", new { viewMode = viewMode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BulkActivate(int[] ids, string viewMode)
        {
            if (ids != null && ids.Length > 0) _service.BulkActivate(ids);
            return RedirectToAction("Index", new { viewMode = viewMode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BulkDelete(int[] ids, string viewMode)
        {
            if (ids != null && ids.Length > 0)
            {
                try
                {
                    _service.BulkDelete(ids);
                }
                catch (System.InvalidOperationException ex)
                {
                    TempData["Error"] = ex.Message;
                }
            }
            return RedirectToAction("Index", new { viewMode = viewMode });
        }
    }
}
