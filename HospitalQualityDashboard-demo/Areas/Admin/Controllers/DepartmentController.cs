// Mục đích: cung cấp các màn hình quản trị danh mục khoa/phòng và nhập dữ liệu từ Excel.
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.ViewModels;
using HospitalQualityDashboardDemo.Services;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.Admin.Controllers
{
    public class DepartmentController : AdminBaseController
    {
        private readonly DepartmentService _service = new DepartmentService();

        // Hiển thị danh sách và các bộ lọc của danh mục khoa/phòng.
        public ActionResult Index(string search)
        {
            return View(new KhoaPhongIndexViewModel { Search = search, Items = _service.GetAll(search) });
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
            return View("Index", new KhoaPhongIndexViewModel { Items = _service.GetAll(), ImportResult = result });
        }

        // Lưu danh mục khoa/phòng theo model/dto đã validate, bao gồm cả nhánh thêm mới và cập nhật.
        private ActionResult Save(KhoaPhongViewModel model)
        {
            if (!ModelState.IsValid) return View("Edit", model);
            _service.Save(new DepartmentSaveDto
            {
                KhoaPhongId = model.KhoaPhongId,
                IdKhoaPhongNguon = model.IdKhoaPhongNguon,
                TenKhoaPhong = model.TenKhoaPhong,
                Used = model.Used,
                GhiChu = model.GhiChu
            });
            return RedirectToAction("Index");
        }
    }
}
