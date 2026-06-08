using System.Web.Mvc;
using HospitalQualityDashboard.Models.DTOs;
using HospitalQualityDashboard.Models.Enums;
using HospitalQualityDashboard.Models.ViewModels;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard.Areas.Admin.Controllers
{
    public class IndicatorController : AdminBaseController
    {
        private readonly IndicatorService _service = new IndicatorService();

        public ActionResult Index()
        {
            return View(new ChiSoIndexViewModel { Items = _service.GetAll() });
        }

        public ActionResult Details(int id)
        {
            var model = _service.Get(id);
            return model == null ? (ActionResult)HttpNotFound() : View(model);
        }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ChiSoViewModel model)
        {
            return Save(model);
        }

        public ActionResult Edit(int id)
        {
            return View(Prepare(_service.Get(id)));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ChiSoViewModel model)
        {
            return Save(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Lock(int id)
        {
            _service.SetActive(id, false);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Unlock(int id)
        {
            _service.SetActive(id, true);
            return RedirectToAction("Index");
        }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Import(ImportFileViewModel model)
        {
            var result = _service.Import(model.File, CurrentTaiKhoanId.Value);
            return View("Index", new ChiSoIndexViewModel { Items = _service.GetAll(), ImportResult = result });
        }

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

        private ChiSoViewModel Prepare(ChiSoViewModel model)
        {
            model.TanSuatBaoCaoOptions = IndicatorService.GetFrequencyOptions(model.TanSuatBaoCaos ?? new[] { model.TanSuatBaoCao });
            return model;
        }
    }
}
