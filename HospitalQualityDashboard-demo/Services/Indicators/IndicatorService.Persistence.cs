// Mục đích: lưu, cập nhật và xóa chỉ số cùng các bảng phụ thuộc trong cơ sở dữ liệu.
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.Enums;
using HospitalQualityDashboardDemo.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Services
{
    public partial class IndicatorService
    {
        // Xử lý chức năng chỉ số chất lượng của method SaveTarget, giữ logic nghiệp vụ tập trung trong tầng phù hợp.
        private void SaveTarget(ChiSoViewModel model)
        {
            if (!model.NamMucTieu.HasValue || string.IsNullOrWhiteSpace(model.ToanTuSoSanh))
            {
                return;
            }

            Execute(@"
IF EXISTS (SELECT 1 FROM dbo.ChiSoMucTieu WHERE ChiSoChatLuongId=@ChiSoChatLuongId AND Nam=@Nam)
    UPDATE dbo.ChiSoMucTieu SET ToanTuSoSanh=@ToanTuSoSanh, GiaTriMucTieu=@GiaTriMucTieu, MoTaMucTieu=@MoTaMucTieu WHERE ChiSoChatLuongId=@ChiSoChatLuongId AND Nam=@Nam
ELSE
    INSERT INTO dbo.ChiSoMucTieu(ChiSoChatLuongId, Nam, ToanTuSoSanh, GiaTriMucTieu, MoTaMucTieu) VALUES(@ChiSoChatLuongId, @Nam, @ToanTuSoSanh, @GiaTriMucTieu, @MoTaMucTieu)",
                Param("@ChiSoChatLuongId", model.ChiSoChatLuongId),
                Param("@Nam", model.NamMucTieu.Value),
                Param("@ToanTuSoSanh", model.ToanTuSoSanh),
                Param("@GiaTriMucTieu", model.GiaTriMucTieu),
                Param("@MoTaMucTieu", model.MoTaMucTieu));
        }

        // Xử lý chức năng chỉ số chất lượng của method SaveTarget, giữ logic nghiệp vụ tập trung trong tầng phù hợp.
        private void SaveTarget(SqlConnection connection, SqlTransaction transaction, ChiSoViewModel model)
        {
            if (!model.NamMucTieu.HasValue || string.IsNullOrWhiteSpace(model.ToanTuSoSanh))
            {
                return;
            }

            Execute(connection, transaction, @"
IF EXISTS (SELECT 1 FROM dbo.ChiSoMucTieu WHERE ChiSoChatLuongId=@ChiSoChatLuongId AND Nam=@Nam)
    UPDATE dbo.ChiSoMucTieu SET ToanTuSoSanh=@ToanTuSoSanh, GiaTriMucTieu=@GiaTriMucTieu, MoTaMucTieu=@MoTaMucTieu WHERE ChiSoChatLuongId=@ChiSoChatLuongId AND Nam=@Nam
ELSE
    INSERT INTO dbo.ChiSoMucTieu(ChiSoChatLuongId, Nam, ToanTuSoSanh, GiaTriMucTieu, MoTaMucTieu) VALUES(@ChiSoChatLuongId, @Nam, @ToanTuSoSanh, @GiaTriMucTieu, @MoTaMucTieu)",
                Param("@ChiSoChatLuongId", model.ChiSoChatLuongId),
                Param("@Nam", model.NamMucTieu.Value),
                Param("@ToanTuSoSanh", model.ToanTuSoSanh),
                Param("@GiaTriMucTieu", model.GiaTriMucTieu),
                Param("@MoTaMucTieu", model.MoTaMucTieu));
        }

        // Tạo tập tham số SQL từ model để dùng cho thao tác ghi dữ liệu.
        private static SqlParameter[] IndicatorParams(ChiSoViewModel model)
        {
            return new[]
            {
                Param("@MaChiSo", model.MaChiSo),
                Param("@SoThuTu", model.SoThuTu),
                Param("@TenChiSo", model.TenChiSo),
                Param("@DinhNghia", model.DinhNghia),
                Param("@LinhVucApDung", model.LinhVucApDung),
                Param("@KhiaCanhChatLuong", model.KhiaCanhChatLuong),
                Param("@ThanhToChatLuong", model.ThanhToChatLuong),
                Param("@LyDoLuaChon", model.LyDoLuaChon),
                Param("@PhuongPhapTinh", model.PhuongPhapTinh),
                Param("@TuSoMoTa", model.TuSoMoTa),
                Param("@MauSoMoTa", model.MauSoMoTa),
                Param("@NguonSoLieu", model.NguonSoLieu),
                Param("@ThuThapTongHop", model.ThuThapTongHop),
                Param("@KhoaPhongThuThapId", model.KhoaPhongThuThapId),
                Param("@KhoaPhongTongHopId", model.KhoaPhongTongHopId),
                Param("@GiaTriSoLieu", model.GiaTriSoLieu),
                Param("@LoaiCongThuc", (byte)model.LoaiCongThuc),
                Param("@DonViTinh", model.DonViTinh),
                Param("@DangHoatDong", model.DangHoatDong)
            };
        }

        // Chuyển một dòng dữ liệu từ SqlDataReader sang view model/dto chỉ số chất lượng đúng kiểu và tên trường.
        private static ChiSoViewModel MapIndicator(SqlDataReader reader)
        {
            return new ChiSoViewModel
            {
                ChiSoChatLuongId = Int(reader, "ChiSoChatLuongId"),
                MaChiSo = String(reader, "MaChiSo"),
                SoThuTu = NullableInt(reader, "SoThuTu"),
                TenChiSo = String(reader, "TenChiSo"),
                DinhNghia = String(reader, "DinhNghia"),
                LinhVucApDung = String(reader, "LinhVucApDung"),
                KhiaCanhChatLuong = String(reader, "KhiaCanhChatLuong"),
                ThanhToChatLuong = String(reader, "ThanhToChatLuong"),
                LyDoLuaChon = String(reader, "LyDoLuaChon"),
                PhuongPhapTinh = String(reader, "PhuongPhapTinh"),
                TuSoMoTa = String(reader, "TuSoMoTa"),
                MauSoMoTa = String(reader, "MauSoMoTa"),
                NguonSoLieu = String(reader, "NguonSoLieu"),
                ThuThapTongHop = String(reader, "ThuThapTongHop"),
                KhoaPhongThuThapId = NullableInt(reader, "KhoaPhongThuThapId"),
                KhoaPhongTongHopId = NullableInt(reader, "KhoaPhongTongHopId"),
                GiaTriSoLieu = String(reader, "GiaTriSoLieu"),
                TanSuatBaoCao = TanSuatBaoCao.HangThang,
                LoaiCongThuc = (LoaiCongThuc)reader.GetByte(reader.GetOrdinal("LoaiCongThuc")),
                DonViTinh = String(reader, "DonViTinh"),
                DangHoatDong = reader.GetBoolean(reader.GetOrdinal("DangHoatDong"))
            };
        }
    }
}
