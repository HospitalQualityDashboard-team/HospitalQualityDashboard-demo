using System.Web.Mvc;
using HospitalQualityDashboard.Models.ViewModels;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard.Controllers
{
    public class AssignmentController : PageController
    {
        private readonly AssignmentService _service = new AssignmentService();
        private readonly DepartmentService _departments = new DepartmentService();
        private readonly IndicatorService _indicators = new IndicatorService();
        private const int PageSize = 20;

        public ActionResult Index(
            int? khoaPhongId,
            int? chiSoId,
            string trangThai,
            string trangThaiPhanCong,
            string search,
            string viewMode,
            int page = 1)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;

            // Set default view mode to byIndicator
            if (string.IsNullOrEmpty(viewMode))
            {
                viewMode = "byIndicator";
            }

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

            int totalItems = 0;

            if (viewMode == "byDepartment")
            {
                totalItems = _service.GetDepartmentsCount(search, khoaPhongId, chiSoId, trangThai);
                model.TotalItems = totalItems;
                model.TotalPages = totalItems > 0 ? (int)System.Math.Ceiling((double)totalItems / PageSize) : 1;
                model.CurrentPage = System.Math.Max(1, System.Math.Min(page, model.TotalPages));
                model.DepartmentGroups = _service.GetAllDepartmentGroups(search, model.CurrentPage, PageSize, khoaPhongId, chiSoId, trangThai);
                model.Items = new System.Collections.Generic.List<AssignmentItemViewModel>();
                model.IndicatorGroups = new System.Collections.Generic.List<IndicatorAssignmentGroup>();
            }
            else if (viewMode == "byIndicator")
            {
                totalItems = _service.GetIndicatorsCount(trangThaiPhanCong, search, khoaPhongId, chiSoId, trangThai);
                model.TotalItems = totalItems;
                model.TotalPages = totalItems > 0 ? (int)System.Math.Ceiling((double)totalItems / PageSize) : 1;
                model.CurrentPage = System.Math.Max(1, System.Math.Min(page, model.TotalPages));
                model.IndicatorGroups = _service.GetAllIndicatorGroups(trangThaiPhanCong, search, model.CurrentPage, PageSize, khoaPhongId, chiSoId, trangThai);
                model.Items = new System.Collections.Generic.List<AssignmentItemViewModel>();
                model.DepartmentGroups = new System.Collections.Generic.List<DepartmentAssignmentGroup>();
            }
            else
            {
                totalItems = _service.GetCount(khoaPhongId, chiSoId, trangThai, search);
                model.TotalItems = totalItems;
                model.TotalPages = totalItems > 0 ? (int)System.Math.Ceiling((double)totalItems / PageSize) : 1;
                model.CurrentPage = System.Math.Max(1, System.Math.Min(page, model.TotalPages));
                model.Items = _service.GetAll(khoaPhongId, chiSoId, trangThai, search, model.CurrentPage, PageSize);
                model.DepartmentGroups = new System.Collections.Generic.List<DepartmentAssignmentGroup>();
                model.IndicatorGroups = new System.Collections.Generic.List<IndicatorAssignmentGroup>();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Assign(AssignmentViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            _service.Assign(model.SelectedKhoaPhongIds, model.SelectedChiSoIds, CurrentTaiKhoanId.Value);
            TempData["Success"] = "Đã phân công chỉ số thành công!";
            return RedirectToAction("Index", new { viewMode = model.ViewMode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Deactivate(int id, string viewMode, int? khoaPhongId, int? chiSoId, string trangThai, string search, int page = 1)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            _service.Deactivate(id);
            TempData["Success"] = "Đã tạm dừng phân công thành công!";
            return RedirectToAction("Index", new { viewMode = viewMode, khoaPhongId = khoaPhongId, chiSoId = chiSoId, trangThai = trangThai, search = search, page = page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Activate(int id, string viewMode, int? khoaPhongId, int? chiSoId, string trangThai, string search, int page = 1)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            _service.Activate(id);
            TempData["Success"] = "Đã kích hoạt lại phân công thành công!";
            return RedirectToAction("Index", new { viewMode = viewMode, khoaPhongId = khoaPhongId, chiSoId = chiSoId, trangThai = trangThai, search = search, page = page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SyncFromIndicators(string viewMode)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            var changed = _service.SyncFromIndicatorSources(CurrentTaiKhoanId.Value);
            TempData["Success"] = "Đã đồng bộ " + changed + " phân công từ dữ liệu chỉ số.";
            return RedirectToAction("Index", new { viewMode = viewMode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, string viewMode, int? khoaPhongId, int? chiSoId, string trangThai, string search, int page = 1)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            try
            {
                _service.Delete(id);
                TempData["Success"] = "Đã xóa phân công thành công!";
            }
            catch (System.InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index", new { viewMode = viewMode, khoaPhongId = khoaPhongId, chiSoId = chiSoId, trangThai = trangThai, search = search, page = page });
        }

        [HttpPost]
        public JsonResult Preview(int[] departmentIds, int[] indicatorIds)
        {
            var admin = RequireAdmin();
            if (admin != null) return Json(new { error = "Unauthorized" });
            var result = _service.Preview(departmentIds, indicatorIds);
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BulkDeactivate(int[] ids, string viewMode)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            if (ids != null && ids.Length > 0)
            {
                _service.BulkDeactivate(ids);
                TempData["Success"] = $"Đã tạm dừng {ids.Length} phân công chỉ số thành công!";
            }
            return RedirectToAction("Index", new { viewMode = viewMode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BulkActivate(int[] ids, string viewMode)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            if (ids != null && ids.Length > 0)
            {
                _service.BulkActivate(ids);
                TempData["Success"] = $"Đã kích hoạt {ids.Length} phân công chỉ số thành công!";
            }
            return RedirectToAction("Index", new { viewMode = viewMode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BulkDelete(int[] ids, string viewMode)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            if (ids != null && ids.Length > 0)
            {
                try
                {
                    _service.BulkDelete(ids);
                    TempData["Success"] = $"Đã xóa {ids.Length} phân công chỉ số thành công!";
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
