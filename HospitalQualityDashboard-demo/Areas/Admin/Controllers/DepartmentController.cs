// Mục đích: cung cấp các màn hình quản trị danh mục khoa/phòng và nhập dữ liệu từ Excel.
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.ViewModels;
using HospitalQualityDashboardDemo.Services;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.Admin.Controllers
{
    public class DepartmentController : AdminBaseController
    {
        private const int DefaultPageSize = 10;
        private readonly DepartmentService _service = new DepartmentService();

        // Hiển thị danh sách và các bộ lọc của danh mục khoa/phòng.
        public ActionResult Index(string search, int page = 1)
        {
            int totalItems;
            var items = _service.GetAll(search, page, DefaultPageSize, out totalItems);
            return View(new KhoaPhongIndexViewModel
            {
                Search = search,
                Items = items,
                KhoaPhongOptions = _service.GetOptions(),
                Page = NormalizePage(page),
                PageSize = DefaultPageSize,
                TotalItems = totalItems,
                TotalPages = GetTotalPages(totalItems, DefaultPageSize)
            });
        }

        // Khởi tạo dữ liệu cho màn hình tạo mới danh mục khoa/phòng.
        public ActionResult Create()
        {
            return View("Edit", new KhoaPhongViewModel { Used = true });
        }

        // Kiểm tra dữ liệu gửi lên và tạo mới danh mục khoa/phòng.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(KhoaPhongViewModel model)
        {
            return Save(model);
        }

        // Tải dữ liệu hiện tại lên màn hình chỉnh sửa danh mục khoa/phòng.
        public ActionResult Edit(int id)
        {
            return View(_service.Get(id));
        }

        // Kiểm tra và lưu các thay đổi của danh mục khoa/phòng.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(KhoaPhongViewModel model)
        {
            return Save(model);
        }

        // Chuyển bản ghi sang trạng thái không còn cho phép chỉnh sửa.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Lock(int id)
        {
            _service.SetUsed(id, false);
            return RedirectToAction("Index");
        }

        // Mở lại khoa/phòng đã khóa để tiếp tục chọn trong nhân viên, phân công và báo cáo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Unlock(int id)
        {
            _service.SetUsed(id, true);
            return RedirectToAction("Index");
        }

        // Xóa bản ghi được chọn sau khi áp dụng các ràng buộc của danh mục khoa/phòng.
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

        // Đọc, kiểm tra và nhập dữ liệu từ tệp tải lên.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Import(ImportFileViewModel model)
        {
            var result = _service.Import(model.File, CurrentTaiKhoanId.Value);
            int totalItems;
            var items = _service.GetAll(null, 1, DefaultPageSize, out totalItems);
            return View("Index", new KhoaPhongIndexViewModel
            {
                Items = items,
                KhoaPhongOptions = _service.GetOptions(),
                ImportResult = result,
                Page = 1,
                PageSize = DefaultPageSize,
                TotalItems = totalItems,
                TotalPages = GetTotalPages(totalItems, DefaultPageSize)
            });
        }

        // Lưu danh mục khoa/phòng theo model/dto đã validate, bao gồm cả nhánh thêm mới và cập nhật.
        private ActionResult Save(KhoaPhongViewModel model)
        {
            if (!ModelState.IsValid) return View("Edit", model);
            if (_service.IsSourceIdExists(model.IdKhoaPhongNguon, model.KhoaPhongId))
            {
                ModelState.AddModelError("IdKhoaPhongNguon", "Mã khoa/phòng nguồn đã tồn tại, vui lòng nhập mã khác.");
                return View("Edit", model);
            }

            try
            {
                _service.Save(new DepartmentSaveDto
                {
                    KhoaPhongId = model.KhoaPhongId,
                    IdKhoaPhongNguon = model.IdKhoaPhongNguon,
                    TenKhoaPhong = model.TenKhoaPhong,
                    Used = model.Used,
                    GhiChu = model.GhiChu
                });
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2601 || ex.Number == 2627)
                {
                    ModelState.AddModelError("IdKhoaPhongNguon", "Mã khoa/phòng nguồn đã tồn tại, vui lòng nhập mã khác.");
                    return View("Edit", model);
                }

                throw;
            }

            return RedirectToAction("Index");
        }

        // Chuan hoa so trang de tranh page am/0 lam sai truy van phan trang.
        private static int NormalizePage(int page)
        {
            return page < 1 ? 1 : page;
        }

        // Tinh tong so trang tu so ban ghi va kich thuoc trang.
        private static int GetTotalPages(int totalItems, int pageSize)
        {
            return totalItems <= 0 ? 1 : (int)System.Math.Ceiling((decimal)totalItems / pageSize);
        }
    }
}
