// Mục đích: định nghĩa dữ liệu lưu chỉ số, mục tiêu theo năm và file import chỉ số.
using HospitalQualityDashboardDemo.Models.Enums;
using System.Collections.Generic;

namespace HospitalQualityDashboardDemo.Models.DTOs
{
    public class IndicatorSaveDto
    {
        public int ChiSoChatLuongId { get; set; }
        public string MaChiSo { get; set; }
        public int? SoThuTu { get; set; }
        public string TenChiSo { get; set; }
        public string DinhNghia { get; set; }
        public string LinhVucApDung { get; set; }
        public string KhiaCanhChatLuong { get; set; }
        public string ThanhToChatLuong { get; set; }
        public string LyDoLuaChon { get; set; }
        public string PhuongPhapTinh { get; set; }
        public string TuSoMoTa { get; set; }
        public string MauSoMoTa { get; set; }
        public string NguonSoLieu { get; set; }
        public string ThuThapTongHop { get; set; }
        public int? KhoaPhongThuThapId { get; set; }
        public int? KhoaPhongTongHopId { get; set; }
        public string GiaTriSoLieu { get; set; }
        public TanSuatBaoCao TanSuatBaoCao { get; set; }
        public int[] SelectedTanSuatBaoCaoValues { get; set; }
        public IList<TanSuatBaoCao> TanSuatBaoCaos { get; set; }
        public LoaiCongThuc LoaiCongThuc { get; set; }
        public string DonViTinh { get; set; }
        public bool DangHoatDong { get; set; }
        public int? NamMucTieu { get; set; }
        public string ToanTuSoSanh { get; set; }
        public decimal? GiaTriMucTieu { get; set; }
        public string MoTaMucTieu { get; set; }
    }

    public class IndicatorImportDto
    {
        public System.Web.HttpPostedFileBase File { get; set; }
        public int UserId { get; set; }
    }
}
