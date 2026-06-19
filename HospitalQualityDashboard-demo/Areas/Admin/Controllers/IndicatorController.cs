// Mục đích: quản trị danh mục chỉ số chất lượng, công thức tính, mục tiêu và dữ liệu import.
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.Enums;
using HospitalQualityDashboardDemo.Models.ViewModels;
using HospitalQualityDashboardDemo.Services;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Areas.Admin.Controllers
{
    public class IndicatorController : AdminBaseController
    {
        private const int DefaultPageSize = 20;
        private readonly IndicatorService _service = new IndicatorService();

        // Hiển thị danh sách và các bộ lọc của chỉ số chất lượng.
        public ActionResult Index(int page = 1)
        {
            int totalItems;
            var items = _service.GetAll(true, null, page, DefaultPageSize, out totalItems);
            return View(CreateIndexViewModel(items, page, totalItems));
        }

        // Tải và hiển thị thông tin chi tiết của chỉ số chất lượng.
        public ActionResult Details(int id)
        {
            var model = _service.Get(id);
            return model == null ? (ActionResult)HttpNotFound() : View(model);
        }

        // Khởi tạo dữ liệu cho màn hình tạo mới chỉ số chất lượng.
        public ActionResult Create()
        {
            return View("Edit", Prepare(new ChiSoViewModel
            {
                DangHoatDong = true,
                TanSuatBaoCao = TanSuatBaoCao.HangThang,
                TanSuatBaoCaos = new[] { TanSuatBaoCao.HangThang },
                SelectedTanSuatBaoCaoValues = new[] { (int)TanSuatBaoCao.HangThang },
                LoaiCongThuc = LoaiCongThuc.TyLe
            }));
        }

        // Kiểm tra dữ liệu gửi lên và tạo mới chỉ số chất lượng.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ChiSoViewModel model)
        {
            return Save(model);
        }

        // Tải dữ liệu hiện tại lên màn hình chỉnh sửa chỉ số chất lượng.
        public ActionResult Edit(int id)
        {
            return View(Prepare(_service.Get(id)));
        }

        // Kiểm tra và lưu các thay đổi của chỉ số chất lượng.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ChiSoViewModel model)
        {
            return Save(model);
        }

        // Chuyển bản ghi sang trạng thái không còn cho phép chỉnh sửa.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Lock(int id)
        {
            _service.SetActive(id, false);
            return RedirectToAction("Index");
        }

        // Điều phối yêu cầu HTTP và phản hồi cho chỉ số chất lượng.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Unlock(int id)
        {
            _service.SetActive(id, true);
            return RedirectToAction("Index");
        }

        // Xóa bản ghi được chọn sau khi áp dụng các ràng buộc của chỉ số chất lượng.
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
            var items = _service.GetAll(true, null, 1, DefaultPageSize, out totalItems);
            var viewModel = CreateIndexViewModel(items, 1, totalItems);
            viewModel.ImportResult = result;
            return View("Index", viewModel);
        }

        // Kiểm tra và cập nhật dữ liệu của chỉ số chất lượng.
        private ActionResult Save(ChiSoViewModel model)
        {
            if (!ModelState.IsValid) return View("Edit", Prepare(model));
            _service.Save(new IndicatorSaveDto
            {
                ChiSoChatLuongId = model.ChiSoChatLuongId,
                MaChiSo = model.MaChiSo,
                SoThuTu = model.SoThuTu,
                TenChiSo = model.TenChiSo,
                DinhNghia = model.DinhNghia,
                LinhVucApDung = model.LinhVucApDung,
                KhiaCanhChatLuong = model.KhiaCanhChatLuong,
                ThanhToChatLuong = model.ThanhToChatLuong,
                LyDoLuaChon = model.LyDoLuaChon,
                PhuongPhapTinh = model.PhuongPhapTinh,
                TuSoMoTa = model.TuSoMoTa,
                MauSoMoTa = model.MauSoMoTa,
                NguonSoLieu = model.NguonSoLieu,
                ThuThapTongHop = model.ThuThapTongHop,
                KhoaPhongThuThapId = model.KhoaPhongThuThapId,
                KhoaPhongTongHopId = model.KhoaPhongTongHopId,
                GiaTriSoLieu = model.GiaTriSoLieu,
                TanSuatBaoCao = model.TanSuatBaoCao,
                SelectedTanSuatBaoCaoValues = model.SelectedTanSuatBaoCaoValues,
                TanSuatBaoCaos = model.TanSuatBaoCaos,
                LoaiCongThuc = model.LoaiCongThuc,
                DonViTinh = model.DonViTinh,
                DangHoatDong = model.DangHoatDong,
                NamMucTieu = model.NamMucTieu,
                ToanTuSoSanh = model.ToanTuSoSanh,
                GiaTriMucTieu = model.GiaTriMucTieu,
                MoTaMucTieu = model.MoTaMucTieu
            });
            return RedirectToAction("Index");
        }

        // Điều phối yêu cầu HTTP và phản hồi cho chỉ số chất lượng.
        private ChiSoViewModel Prepare(ChiSoViewModel model)
        {
            model.TanSuatBaoCaoOptions = FrequencyHelper.GetFrequencyOptions(model.TanSuatBaoCaos ?? new[] { model.TanSuatBaoCao });
            return model;
        }

        // Tạo cấu trúc dữ liệu phục vụ chỉ số chất lượng.
        private static ChiSoIndexViewModel CreateIndexViewModel(System.Collections.Generic.IList<ChiSoViewModel> items, int page, int totalItems)
        {
            return new ChiSoIndexViewModel
            {
                Items = items,
                Page = NormalizePage(page),
                PageSize = DefaultPageSize,
                TotalItems = totalItems,
                TotalPages = GetTotalPages(totalItems, DefaultPageSize)
            };
        }

        // Chuẩn hóa dữ liệu đầu vào trước khi dùng cho chỉ số chất lượng.
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
