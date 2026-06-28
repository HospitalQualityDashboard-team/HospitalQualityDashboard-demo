// Mục đích: ghi và đọc lịch sử xuất Excel dashboard phục vụ kiểm toán thao tác.
using ClosedXML.Excel;
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.Enums;
using HospitalQualityDashboardDemo.Models.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HospitalQualityDashboardDemo.Services
{
    public partial class DashboardExcelExportService
    {
        private void EnsureExportHistoryTable()
        {
            // Giữ khả năng tương thích với database cũ chưa chạy script bổ sung lịch sử xuất.
            Execute(@"
IF OBJECT_ID('dbo.LichSuXuatBaoCao', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LichSuXuatBaoCao (
        LichSuXuatBaoCaoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LichSuXuatBaoCao PRIMARY KEY,
        NguoiDungId INT NOT NULL,
        LoaiBaoCao NVARCHAR(100) NOT NULL,
        BoLoc NVARCHAR(MAX) NULL,
        TenFile NVARCHAR(255) NOT NULL,
        SoDongDuLieu INT NOT NULL CONSTRAINT DF_LichSuXuatBaoCao_SoDongDuLieu DEFAULT (0),
        NgayXuat DATETIME NOT NULL CONSTRAINT DF_LichSuXuatBaoCao_NgayXuat DEFAULT (GETDATE()),
        DiaChiIP NVARCHAR(45) NULL,
        VaiTro NVARCHAR(20) NULL,
        KhoaPhongId INT NULL,
        CONSTRAINT FK_LichSuXuatBaoCao_TaiKhoan FOREIGN KEY (NguoiDungId) REFERENCES dbo.TaiKhoan(TaiKhoanId),
        CONSTRAINT FK_LichSuXuatBaoCao_KhoaPhong FOREIGN KEY (KhoaPhongId) REFERENCES dbo.KhoaPhong(KhoaPhongId),
        CONSTRAINT CK_LichSuXuatBaoCao_SoDongDuLieu CHECK (SoDongDuLieu >= 0)
    );
END");
        }

        // Ghi lại thông tin phục vụ theo dõi và kiểm toán workbook Dashboard và lịch sử xuất.
        private void LogExportHistory(DashboardExcelExportQueryDto query, ExportUserContextDto userContext, string fileName, int rowCount)
        {
            // Bộ lọc được lưu dạng JSON để có thể tái dựng chính xác phạm vi của lần xuất.
            Execute(@"
INSERT INTO dbo.LichSuXuatBaoCao(NguoiDungId, LoaiBaoCao, BoLoc, TenFile, SoDongDuLieu, DiaChiIP, VaiTro, KhoaPhongId, NgayXuat)
VALUES(@NguoiDungId, @LoaiBaoCao, @BoLoc, @TenFile, @SoDongDuLieu, @DiaChiIP, @VaiTro, @KhoaPhongId, @NgayXuat)",
                Param("@NguoiDungId", userContext.TaiKhoanId),
                Param("@LoaiBaoCao", ReportType),
                Param("@BoLoc", JsonConvert.SerializeObject(query)),
                Param("@TenFile", fileName),
                Param("@SoDongDuLieu", rowCount),
                Param("@DiaChiIP", userContext.DiaChiIP),
                Param("@VaiTro", userContext.IsAdmin ? "Admin" : "User"),
                Param("@KhoaPhongId", query.KhoaPhongId),
                Param("@NgayXuat", GetVietnamLocalNow()));
        }

        // Đặt tên file xuất theo phạm vi khoa/phòng và kỳ báo cáo để người dùng dễ lưu trữ, đối chiếu.
        private string BuildFileName(DashboardExcelExportQueryDto query, ExportUserContextDto userContext)
        {
            var scope = query.KhoaPhongId.HasValue
                ? GetDepartmentName(query.KhoaPhongId.Value)
                : (userContext.IsAdmin ? "ToanVien" : userContext.TenKhoaPhong);
            var period = BuildPeriodToken(query);
            if (query.ComparisonPeriodIds != null && query.ComparisonPeriodIds.Length > 0)
            {
                period += "_SoSanh_" + (query.ComparisonPeriodIds.Length + 1).ToString(CultureInfo.InvariantCulture) + "Ky";
            }
            return string.Format(CultureInfo.InvariantCulture, "Dashboard_{0}_{1}.xlsx", SanitizeFileToken(scope), period);
        }

        // Ưu tiên tên kỳ báo cáo cụ thể; nếu không có thì dùng tần suất và năm để tạo mã file ổn định.
        private string BuildPeriodToken(DashboardExcelExportQueryDto query)
        {
            if (query.KyBaoCaoId.HasValue)
            {
                var periodName = Convert.ToString(Scalar("SELECT TenKyBaoCao FROM dbo.KyBaoCao WHERE KyBaoCaoId=@Id", Param("@Id", query.KyBaoCaoId.Value)));
                if (!string.IsNullOrWhiteSpace(periodName))
                {
                    return SanitizeFileToken(periodName);
                }
            }

            var year = query.NamBaoCao.GetValueOrDefault(DateTime.Today.Year);
            if (query.TanSuat.HasValue)
            {
                return SanitizeFileToken(FormatTanSuatBaoCao((TanSuatBaoCao)query.TanSuat.Value)) + "_" + year.ToString(CultureInfo.InvariantCulture);
            }

            return year.ToString(CultureInfo.InvariantCulture);
        }

        // Chuyển bộ lọc xuất Excel thành mô tả đọc được để ghi vào sheet thông tin báo cáo.
        private string BuildFilterDescription(DashboardExcelExportQueryDto query)
        {
            var parts = new List<string>();
            if (query.NamBaoCao.HasValue) parts.Add("Năm " + query.NamBaoCao.Value.ToString(CultureInfo.InvariantCulture));
            if (query.KyBaoCaoId.HasValue) parts.Add(GetPeriodName(query.KyBaoCaoId.Value));
            if (query.TanSuat.HasValue) parts.Add(FormatTanSuatBaoCao((TanSuatBaoCao)query.TanSuat.Value));
            if (!string.IsNullOrWhiteSpace(query.LinhVuc)) parts.Add("Lĩnh vực: " + query.LinhVuc);
            if (query.TrangThaiNhapLieu.HasValue) parts.Add("Trạng thái nhập liệu: " + FormatFilterStatus(query.TrangThaiNhapLieu.Value));
            if (query.TrangThaiDuyet.HasValue) parts.Add("Trạng thái duyệt: " + FormatFilterStatus(query.TrangThaiDuyet.Value));
            if (query.DatMucTieu.HasValue) parts.Add("Đánh giá: " + FormatDatMucTieu(query.DatMucTieu));
            return parts.Count == 0 ? "Tất cả dữ liệu" : string.Join("; ", parts);
        }

        // Lấy tên khoa/phòng cho tiêu đề và tên file; dùng mã dự phòng nếu bản ghi đã bị thiếu.
        private string GetDepartmentName(int departmentId)
        {
            var value = Convert.ToString(Scalar("SELECT TenKhoaPhong FROM dbo.KhoaPhong WHERE KhoaPhongId=@Id", Param("@Id", departmentId)));
            return string.IsNullOrWhiteSpace(value) ? "KhoaPhong" + departmentId.ToString(CultureInfo.InvariantCulture) : value;
        }

        // Lấy tên kỳ báo cáo phục vụ mô tả bộ lọc, kèm giá trị dự phòng khi dữ liệu kỳ không còn tồn tại.
        private string GetPeriodName(int periodId)
        {
            var value = Convert.ToString(Scalar("SELECT TenKyBaoCao FROM dbo.KyBaoCao WHERE KyBaoCaoId=@Id", Param("@Id", periodId)));
            return string.IsNullOrWhiteSpace(value) ? "Kỳ báo cáo " + periodId.ToString(CultureInfo.InvariantCulture) : value;
        }

        // Làm sạch từng phần tên file để tránh ký tự tiếng Việt hoặc ký tự đặc biệt gây lỗi tải xuống.
        private static string SanitizeFileToken(string value)
        {
            value = RemoveDiacritics(string.IsNullOrWhiteSpace(value) ? "BaoCao" : value);
            value = Regex.Replace(value, @"[^A-Za-z0-9]+", "_").Trim('_');
            return string.IsNullOrWhiteSpace(value) ? "BaoCao" : value;
        }

        // Loại bỏ dấu tiếng Việt để tạo chuỗi an toàn cho tên tệp.
        private static string RemoveDiacritics(string value)
        {
            var normalized = value.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();
            foreach (var ch in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                builder.Append(ch == 'đ' || ch == 'Đ' ? 'd' : ch);
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        // Định dạng giá trị theo quy ước hiển thị của workbook Dashboard và lịch sử xuất.
        private static string FormatMucTieu(string op, decimal? value, string description)
        {
            if (!string.IsNullOrWhiteSpace(description)) return description;
            if (string.IsNullOrWhiteSpace(op) || !value.HasValue) return string.Empty;
            return op.Trim() + " " + value.Value.ToString("0.####", CultureInfo.InvariantCulture);
        }

        // Định dạng giá trị theo quy ước hiển thị của workbook Dashboard và lịch sử xuất.
        private static string FormatDatMucTieu(bool? value)
        {
            if (!value.HasValue) return "Chưa đánh giá";
            return value.Value ? "Đạt" : "Chưa đạt";
        }

        // Định dạng giá trị theo quy ước hiển thị của workbook Dashboard và lịch sử xuất.
        private static string FormatFilterStatus(int status)
        {
            if (status == 0) return "Chưa nhập";
            return FormatTrangThaiBaoCao((TrangThaiBaoCao)status);
        }

        // Định dạng giá trị theo quy ước hiển thị của workbook Dashboard và lịch sử xuất.
        private static string FormatTrangThaiBaoCao(TrangThaiBaoCao status)
        {
            switch (status)
            {
                case TrangThaiBaoCao.Nhap: return "Nháp";
                case TrangThaiBaoCao.DaGui: return "Đã gửi";
                case TrangThaiBaoCao.QuaHan: return "Quá hạn";
                case TrangThaiBaoCao.DaKhoa: return "Đã khóa";
                case TrangThaiBaoCao.DaDuyet: return "Đã duyệt";
                case TrangThaiBaoCao.TraLai: return "Trả lại";
                default: return status.ToString();
            }
        }

        // Định dạng giá trị theo quy ước hiển thị của workbook Dashboard và lịch sử xuất.
        private static string FormatTrangThaiDuyet(TrangThaiBaoCao status)
        {
            switch (status)
            {
                case TrangThaiBaoCao.DaDuyet: return "Đã duyệt";
                case TrangThaiBaoCao.TraLai: return "Trả lại";
                case TrangThaiBaoCao.DaKhoa: return "Đã khóa";
                default: return "Chưa duyệt";
            }
        }

        // Định dạng giá trị theo quy ước hiển thị của workbook Dashboard và lịch sử xuất.
        private static string FormatTanSuatBaoCao(TanSuatBaoCao frequency)
        {
            switch (frequency)
            {
                case TanSuatBaoCao.HangNgay: return "Hàng ngày";
                case TanSuatBaoCao.HangTuan: return "Hàng tuần";
                case TanSuatBaoCao.HangThang: return "Hàng tháng";
                case TanSuatBaoCao.HangQuy: return "Hàng quý";
                case TanSuatBaoCao.SauThang: return "6 tháng";
                case TanSuatBaoCao.ChinThang: return "9 tháng";
                case TanSuatBaoCao.HangNam: return "Hàng năm";
                case TanSuatBaoCao.KhiPhatSinh: return "Khi phát sinh";
                case TanSuatBaoCao.TruocSauKhiThucHien: return "Trước/sau khi thực hiện";
                default: return frequency.ToString();
            }
        }
    }
}
