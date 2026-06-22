using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.Enums;
using HospitalQualityDashboardDemo.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Data.SqlClient;
using System.Linq;

namespace HospitalQualityDashboardDemo.Services
{
    public class ExportService : DbServiceBase
    {
        private readonly DepartmentService _departments = new DepartmentService();
        private readonly EmployeeService _employees = new EmployeeService();
        private readonly IndicatorService _indicators = new IndicatorService();
        private readonly AssignmentService _assignments = new AssignmentService();
        private readonly ReportService _reports = new ReportService();
        private readonly ExcelImportExportService _excel = new ExcelImportExportService();

        // Tạo nội dung xuất dữ liệu theo bộ lọc và phạm vi được phép.
        public byte[] ExportDepartments()
        {
            return _excel.CreateXlsx(_departments.GetAll(), new List<KeyValuePair<string, Func<KhoaPhongViewModel, object>>>
            {
                new KeyValuePair<string, Func<KhoaPhongViewModel, object>>("IDKHOAPHONG", x => x.IdKhoaPhongNguon),
                new KeyValuePair<string, Func<KhoaPhongViewModel, object>>("TENKHOAPHONG", x => x.TenKhoaPhong),
                new KeyValuePair<string, Func<KhoaPhongViewModel, object>>("USED", x => x.Used ? 1 : 0),
                new KeyValuePair<string, Func<KhoaPhongViewModel, object>>("GHICHU", x => x.GhiChu)
            });
        }

        // Tạo nội dung xuất dữ liệu theo bộ lọc và phạm vi được phép.
        public byte[] ExportEmployees(EmployeeExportQueryDto dto)
        {
            return ExportEmployees(dto.DepartmentId, dto.IsAdmin, dto.CurrentDepartmentId);
        }

        // Tạo nội dung xuất dữ liệu theo bộ lọc và phạm vi được phép.
        public byte[] ExportEmployees(int? departmentId, bool admin, int? currentDepartmentId)
        {
            var effectiveDepartmentId = admin ? departmentId : currentDepartmentId;
            return _excel.CreateXlsx(_employees.GetAll(effectiveDepartmentId), new List<KeyValuePair<string, Func<NhanVienViewModel, object>>>
            {
                new KeyValuePair<string, Func<NhanVienViewModel, object>>("MaNhanVien", x => x.MaNhanVien),
                new KeyValuePair<string, Func<NhanVienViewModel, object>>("HoTen", x => x.HoTen),
                new KeyValuePair<string, Func<NhanVienViewModel, object>>("KhoaPhong", x => x.TenKhoaPhong),
                new KeyValuePair<string, Func<NhanVienViewModel, object>>("Email", x => x.Email),
                new KeyValuePair<string, Func<NhanVienViewModel, object>>("SoDienThoai", x => x.SoDienThoai)
            });
        }

        // Tạo nội dung xuất dữ liệu theo bộ lọc và phạm vi được phép.
        public byte[] ExportIndicators()
        {
            return _excel.CreateXlsx(_indicators.GetAll(), new List<KeyValuePair<string, Func<ChiSoViewModel, object>>>
            {
                new KeyValuePair<string, Func<ChiSoViewModel, object>>("MaChiSo", x => x.MaChiSo),
                new KeyValuePair<string, Func<ChiSoViewModel, object>>("TenChiSo", x => x.TenChiSo),
                new KeyValuePair<string, Func<ChiSoViewModel, object>>("TanSuatBaoCao", x => x.TanSuatBaoCaoText),
                new KeyValuePair<string, Func<ChiSoViewModel, object>>("LoaiCongThuc", x => x.LoaiCongThuc),
                new KeyValuePair<string, Func<ChiSoViewModel, object>>("DonViTinh", x => x.DonViTinh)
            });
        }

        // Tạo nội dung xuất dữ liệu theo bộ lọc và phạm vi được phép.
        public byte[] ExportReports(ReportExportQueryDto dto)
        {
            return ExportReports(dto.PeriodId, dto.DepartmentId, dto.IndicatorId, dto.IsAdmin, dto.CurrentDepartmentId);
        }

        // Tạo nội dung xuất dữ liệu theo bộ lọc và phạm vi được phép.
        public byte[] ExportReports(int? periodId, int? departmentId, int? indicatorId, bool admin, int? currentDepartmentId)
        {
            var rows = _reports.GetAll(periodId, departmentId, indicatorId, admin, currentDepartmentId);
            return _excel.CreateXlsx(rows, new List<KeyValuePair<string, Func<ReportEntryViewModel, object>>>
            {
                new KeyValuePair<string, Func<ReportEntryViewModel, object>>("KyBaoCao", x => x.TenKyBaoCao),
                new KeyValuePair<string, Func<ReportEntryViewModel, object>>("KhoaPhong", x => x.TenKhoaPhong),
                new KeyValuePair<string, Func<ReportEntryViewModel, object>>("MaChiSo", x => x.MaChiSo),
                new KeyValuePair<string, Func<ReportEntryViewModel, object>>("TenChiSo", x => x.TenChiSo),
                new KeyValuePair<string, Func<ReportEntryViewModel, object>>("KetQua", x => FormatDecimal(x.KetQua)),
                new KeyValuePair<string, Func<ReportEntryViewModel, object>>("TrangThai", x => x.TrangThai),
                new KeyValuePair<string, Func<ReportEntryViewModel, object>>("DatMucTieu", x => x.DatMucTieu)
            });
        }

        // Tạo nội dung xuất dữ liệu theo bộ lọc và phạm vi được phép.
        public byte[] ExportAssignments(AssignmentExportQueryDto dto)
        {
            return ExportAssignments(dto.DepartmentId, dto.IndicatorId, dto.Status, dto.AssignmentStatus, dto.Search, dto.Columns);
        }

        // Tạo nội dung xuất dữ liệu theo bộ lọc và phạm vi được phép.
        public byte[] ExportAssignments(
            int? departmentId,
            int? indicatorId,
            string status,
            string assignmentStatus,
            string search,
            string[] selectedColumnKeys)
        {
            var rows = _assignments.GetExportRows(departmentId, indicatorId, status, assignmentStatus, search);
            var columns = ResolveAssignmentExportColumns(selectedColumnKeys)
                .Select(x => new KeyValuePair<string, Func<AssignmentExportRow, object>>(x.Header, x.Value))
                .ToList();

            return _excel.CreateXlsx(rows, columns);
        }

        // Tạo nội dung xuất dữ liệu theo bộ lọc và phạm vi được phép.
        public byte[] ExportDashboardProgress(string[] selectedColumnKeys, int? tanSuatFilter = null)
        {
            var rows = QueryDepartmentProgress(tanSuatFilter);
            var columns = ResolveDashboardProgressExportColumns(selectedColumnKeys)
                .Select(x => new KeyValuePair<string, Func<DepartmentProgressViewModel, object>>(x.Header, x.Value))
                .ToList();
            var detailRows = QueryDashboardReportDetails(tanSuatFilter);

            return _excel.CreateXlsxWorkbook(new List<ExcelWorksheetExport>
            {
                ExcelWorksheetExport.From("TongHopTienDo", rows, columns),
                ExcelWorksheetExport.From("ChiTietSoLieu", detailRows, DashboardReportDetailExportColumns
                    .Select(x => new KeyValuePair<string, Func<DashboardReportDetailExportRow, object>>(x.Header, x.Value))
                    .ToList())
            });
        }

        // Truy vấn xuất dữ liệu quản trị theo điều kiện được cung cấp.
        private IList<DepartmentProgressViewModel> QueryDepartmentProgress(int? tanSuatFilter = null)
        {
            string query;
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            
            if (tanSuatFilter.HasValue)
            {
                query = @"
SELECT 
    kp.TenKhoaPhong,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = @TanSuat) AS Tong,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = @TanSuat AND bc.TrangThai IN (2,3,4)) AS DaGui,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = @TanSuat AND bc.TrangThai = 1) AS LuuNhap,
    (SELECT COUNT(*) FROM dbo.BaoCao bc INNER JOIN dbo.KyBaoCao ky ON ky.KyBaoCaoId = bc.KyBaoCaoId INNER JOIN dbo.BaoCaoChiTiet ct ON ct.BaoCaoId = bc.BaoCaoId WHERE bc.KhoaPhongId = kp.KhoaPhongId AND ky.LoaiKyBaoCao = @TanSuat AND YEAR(ky.TuNgay) = YEAR(GETDATE()) AND bc.TrangThai IN (2,3,4) AND ct.DatMucTieu = 1) AS SoBaoCaoDatMucTieuNam,
    (SELECT COUNT(*) FROM dbo.BaoCao bc INNER JOIN dbo.KyBaoCao ky ON ky.KyBaoCaoId = bc.KyBaoCaoId INNER JOIN dbo.BaoCaoChiTiet ct ON ct.BaoCaoId = bc.BaoCaoId WHERE bc.KhoaPhongId = kp.KhoaPhongId AND ky.LoaiKyBaoCao = @TanSuat AND YEAR(ky.TuNgay) = YEAR(GETDATE()) AND bc.TrangThai IN (2,3,4) AND ct.DatMucTieu IS NOT NULL) AS SoBaoCaoDanhGiaMucTieuNam,
    
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 3) AS TongThang,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 3 AND bc.TrangThai IN (2,3,4)) AS DaGuiThang,

    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 4) AS TongQuy,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 4 AND bc.TrangThai IN (2,3,4)) AS DaGuiQuy,

    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 6) AS TongNam,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 6 AND bc.TrangThai IN (2,3,4)) AS DaGuiNam
FROM dbo.KhoaPhong kp
ORDER BY kp.TenKhoaPhong";
                sqlParams.Add(Param("@TanSuat", tanSuatFilter.Value));
            }
            else
            {
                query = @"
SELECT 
    kp.TenKhoaPhong,
    (SELECT COUNT(*) FROM dbo.PhanCongChiSo pc WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1) AS Tong,
    (SELECT COUNT(*) FROM dbo.PhanCongChiSo pc JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND bc.TrangThai IN (2,3,4)) AS DaGui,
    (SELECT COUNT(*) FROM dbo.PhanCongChiSo pc JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND bc.TrangThai = 1) AS LuuNhap,
    (SELECT COUNT(*) FROM dbo.BaoCao bc INNER JOIN dbo.KyBaoCao ky ON ky.KyBaoCaoId = bc.KyBaoCaoId INNER JOIN dbo.BaoCaoChiTiet ct ON ct.BaoCaoId = bc.BaoCaoId WHERE bc.KhoaPhongId = kp.KhoaPhongId AND YEAR(ky.TuNgay) = YEAR(GETDATE()) AND bc.TrangThai IN (2,3,4) AND ct.DatMucTieu = 1) AS SoBaoCaoDatMucTieuNam,
    (SELECT COUNT(*) FROM dbo.BaoCao bc INNER JOIN dbo.KyBaoCao ky ON ky.KyBaoCaoId = bc.KyBaoCaoId INNER JOIN dbo.BaoCaoChiTiet ct ON ct.BaoCaoId = bc.BaoCaoId WHERE bc.KhoaPhongId = kp.KhoaPhongId AND YEAR(ky.TuNgay) = YEAR(GETDATE()) AND bc.TrangThai IN (2,3,4) AND ct.DatMucTieu IS NOT NULL) AS SoBaoCaoDanhGiaMucTieuNam,
    
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 3) AS TongThang,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 3 AND bc.TrangThai IN (2,3,4)) AS DaGuiThang,

    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 4) AS TongQuy,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 4 AND bc.TrangThai IN (2,3,4)) AS DaGuiQuy,

    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 6) AS TongNam,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 6 AND bc.TrangThai IN (2,3,4)) AS DaGuiNam
FROM dbo.KhoaPhong kp
ORDER BY kp.TenKhoaPhong";
            }

            var list = Query(query, r => new DepartmentProgressViewModel
            {
                TenKhoaPhong = String(r, "TenKhoaPhong"),
                Tong = Int(r, "Tong"),
                DaGui = Int(r, "DaGui"),
                LuuNhap = Int(r, "LuuNhap"),
                SoBaoCaoDatMucTieuNam = Int(r, "SoBaoCaoDatMucTieuNam"),
                SoBaoCaoDanhGiaMucTieuNam = Int(r, "SoBaoCaoDanhGiaMucTieuNam"),
                
                TongThang = Int(r, "TongThang"),
                DaGuiThang = Int(r, "DaGuiThang"),
                
                TongQuy = Int(r, "TongQuy"),
                DaGuiQuy = Int(r, "DaGuiQuy"),
                
                TongNam = Int(r, "TongNam"),
                DaGuiNam = Int(r, "DaGuiNam")
            }, sqlParams.ToArray());

            foreach (var progress in list)
            {
                progress.XepLoai = CalculateXepLoai(progress.DaGui, progress.Tong);
                progress.XepLoaiThang = CalculateXepLoai(progress.DaGuiThang, progress.TongThang);
                progress.XepLoaiQuy = CalculateXepLoai(progress.DaGuiQuy, progress.TongQuy);
                progress.XepLoaiNam = CalculateXepLoai(progress.DaGuiNam, progress.TongNam);
            }

            return list;
        }

        // Truy vấn xuất dữ liệu quản trị theo điều kiện được cung cấp.
        private IList<DashboardReportDetailExportRow> QueryDashboardReportDetails(int? tanSuatFilter = null)
        {
            var sql = @"
SELECT
    kp.TenKhoaPhong,
    ky.TenKyBaoCao,
    DATEPART(YEAR, ky.TuNgay) AS NamBaoCao,
    cs.MaChiSo,
    cs.TenChiSo,
    ky.LoaiKyBaoCao AS TanSuatBaoCao,
    ct.TuSo,
    ct.MauSo,
    ct.GiaTriNhap,
    ct.KetQua,
    COALESCE(mt.ToanTuSoSanh, mtFallback.ToanTuSoSanh) AS ToanTuSoSanh,
    COALESCE(mt.GiaTriMucTieu, mtFallback.GiaTriMucTieu) AS GiaTriMucTieu,
    COALESCE(mt.MoTaMucTieu, mtFallback.MoTaMucTieu) AS MoTaMucTieu,
    ct.DatMucTieu,
    bc.TrangThai,
    bc.NgayGui,
    ct.GhiChu
FROM dbo.BaoCao bc
INNER JOIN dbo.KyBaoCao ky ON ky.KyBaoCaoId = bc.KyBaoCaoId
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = bc.KhoaPhongId
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = bc.ChiSoChatLuongId
LEFT JOIN dbo.BaoCaoChiTiet ct ON ct.BaoCaoId = bc.BaoCaoId
LEFT JOIN dbo.ChiSoMucTieu mt ON mt.ChiSoChatLuongId = bc.ChiSoChatLuongId AND mt.Nam = DATEPART(YEAR, ky.TuNgay)
OUTER APPLY (
    SELECT TOP 1 mt2.ToanTuSoSanh, mt2.GiaTriMucTieu, mt2.MoTaMucTieu
    FROM dbo.ChiSoMucTieu mt2
    WHERE mt2.ChiSoChatLuongId = bc.ChiSoChatLuongId
    ORDER BY
        CASE WHEN mt2.Nam <= DATEPART(YEAR, ky.TuNgay) THEN 0 ELSE 1 END,
        CASE WHEN mt2.Nam <= DATEPART(YEAR, ky.TuNgay) THEN mt2.Nam END DESC,
        mt2.Nam DESC
) mtFallback
WHERE (@TanSuat IS NULL OR ky.LoaiKyBaoCao = @TanSuat)
ORDER BY kp.TenKhoaPhong, ky.TuNgay DESC, cs.MaChiSo";

            return Query(sql, r => new DashboardReportDetailExportRow
            {
                TenKhoaPhong = String(r, "TenKhoaPhong"),
                TenKyBaoCao = String(r, "TenKyBaoCao"),
                NamBaoCao = Int(r, "NamBaoCao"),
                MaChiSo = String(r, "MaChiSo"),
                TenChiSo = String(r, "TenChiSo"),
                TanSuatBaoCaoText = FormatTanSuatBaoCao((TanSuatBaoCao)Convert.ToByte(r["TanSuatBaoCao"])),
                TuSo = NullableDecimal(r, "TuSo"),
                MauSo = NullableDecimal(r, "MauSo"),
                GiaTriNhap = NullableDecimal(r, "GiaTriNhap"),
                KetQua = NullableDecimal(r, "KetQua"),
                MucTieuNam = FormatMucTieuNam(String(r, "ToanTuSoSanh"), NullableDecimal(r, "GiaTriMucTieu"), String(r, "MoTaMucTieu")),
                DatMucTieu = r.IsDBNull(r.GetOrdinal("DatMucTieu")) ? (bool?)null : r.GetBoolean(r.GetOrdinal("DatMucTieu")),
                TrangThaiBaoCaoText = FormatTrangThaiBaoCao((TrangThaiBaoCao)Convert.ToByte(r["TrangThai"])),
                NgayGui = NullableDateTime(r, "NgayGui"),
                GhiChu = String(r, "GhiChu")
            }, Param("@TanSuat", tanSuatFilter));
        }

        // Tính toán giá trị nghiệp vụ phục vụ xuất dữ liệu quản trị.
        private static string CalculateXepLoai(int daGui, int tong)
        {
            if (tong == 0) return "N/A";
            decimal rate = (decimal)daGui * 100 / tong;
            if (rate >= 90) return "Xuất sắc";
            if (rate >= 70) return "Khá";
            if (rate >= 50) return "Trung bình";
            return "Yếu";
        }

        // Định dạng giá trị theo quy ước hiển thị của xuất dữ liệu quản trị.
        private static string FormatMucTieuNam(string op, decimal? value, string description)
        {
            if (!string.IsNullOrWhiteSpace(description))
            {
                return description;
            }

            if (string.IsNullOrWhiteSpace(op) || !value.HasValue)
            {
                return string.Empty;
            }

            return op.Trim() + " " + FormatDecimal(value);
        }

        // Định dạng giá trị theo quy ước hiển thị của xuất dữ liệu quản trị.
        private static string FormatDecimal(decimal? value)
        {
            return value.HasValue ? value.Value.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty;
        }

        // Định dạng giá trị theo quy ước hiển thị của xuất dữ liệu quản trị.
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

        // Định dạng giá trị theo quy ước hiển thị của xuất dữ liệu quản trị.
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

        // Xác định giá trị phù hợp từ các nguồn dữ liệu của xuất dữ liệu quản trị.
        private static IList<AssignmentExportColumn> ResolveAssignmentExportColumns(string[] selectedColumnKeys)
        {
            if (selectedColumnKeys == null || selectedColumnKeys.Length == 0)
            {
                return AssignmentExportColumns.ToList();
            }

            var selected = new HashSet<string>(selectedColumnKeys.Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
            var columns = AssignmentExportColumns.Where(x => selected.Contains(x.Key)).ToList();
            return columns.Count == 0 ? AssignmentExportColumns.ToList() : columns;
        }

        private static readonly IList<AssignmentExportColumn> AssignmentExportColumns = new List<AssignmentExportColumn>
        {
            new AssignmentExportColumn("TenChiSo", "Tên Chỉ số", x => x.TenChiSo),
            new AssignmentExportColumn("TanSuatBaoCao", "Tần suất báo cáo", x => x.TanSuatBaoCaoText),
            new AssignmentExportColumn("PhuongPhapTinh", "Phương pháp tính", x => x.PhuongPhapTinh),
            new AssignmentExportColumn("TuSo", "Tử số", x => x.TuSoMoTa),
            new AssignmentExportColumn("MauSo", "Mẫu số", x => x.MauSoMoTa),
            new AssignmentExportColumn("ThuThapTongHop", "Thu thập và tổng hợp số liệu", x => x.ThuThapTongHop),
            new AssignmentExportColumn("KhoaPhongDuocPhanCong", "Khoa/Phòng được phân công", x => x.TenKhoaPhong)
        };

        private class AssignmentExportColumn
        {
            // Khởi tạo thành phần và các giá trị cần thiết cho dữ liệu nội bộ của AssignmentExportColumn.
            public AssignmentExportColumn(string key, string header, Func<AssignmentExportRow, object> value)
            {
                Key = key;
                Header = header;
                Value = value;
            }

            public string Key { get; private set; }
            public string Header { get; private set; }
            public Func<AssignmentExportRow, object> Value { get; private set; }
        }

        // Xác định giá trị phù hợp từ các nguồn dữ liệu của xuất dữ liệu quản trị.
        private static IList<DashboardProgressExportColumn> ResolveDashboardProgressExportColumns(string[] selectedColumnKeys)
        {
            if (selectedColumnKeys == null || selectedColumnKeys.Length == 0)
            {
                return AllDashboardProgressExportColumns().ToList();
            }

            var selected = new HashSet<string>(selectedColumnKeys.Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
            var columns = AllDashboardProgressExportColumns().Where(x => selected.Contains(x.Key)).ToList();
            return columns.Count == 0 ? AllDashboardProgressExportColumns().ToList() : columns;
        }

        private static readonly IList<DashboardProgressExportColumn> DashboardProgressExportColumns = new List<DashboardProgressExportColumn>
        {
            new DashboardProgressExportColumn("TenKhoaPhong", "Khoa / Phòng", x => x.TenKhoaPhong),
            new DashboardProgressExportColumn("DaGui", "Số báo cáo đã gửi", x => x.DaGui),
            new DashboardProgressExportColumn("Tong", "Tổng số chỉ số cần nộp", x => x.Tong),
            new DashboardProgressExportColumn("PhanTram", "Tỷ lệ hoàn tất (%)", x => x.Tong > 0 ? (object)Math.Round((decimal)x.DaGui * 100 / x.Tong, 1) : 0),
            new DashboardProgressExportColumn("LuuNhap", "Số báo cáo lưu nháp", x => x.LuuNhap),
            new DashboardProgressExportColumn("ConThieu", "Số báo cáo còn thiếu", x => x.ConThieu),
            new DashboardProgressExportColumn("XepLoai", "Xếp loại tổng thể", x => x.XepLoai),
            new DashboardProgressExportColumn("TiendoThang", "Tiến độ Hàng tháng", x => x.TiendoHangThang),
            new DashboardProgressExportColumn("XepLoaiThang", "Xếp loại Hàng tháng", x => x.XepLoaiThang),
            new DashboardProgressExportColumn("TiendoQuy", "Tiến độ Hàng quý", x => x.TiendoHangQuy),
            new DashboardProgressExportColumn("XepLoaiQuy", "Xếp loại Hàng quý", x => x.XepLoaiQuy),
            new DashboardProgressExportColumn("TiendoNam", "Tiến độ Hàng năm", x => x.TiendoHangNam),
            new DashboardProgressExportColumn("XepLoaiNam", "Xếp loại Hàng năm", x => x.XepLoaiNam)
        };

        // Trả về đầy đủ cấu hình cột được hỗ trợ cho xuất dữ liệu quản trị.
        private static IEnumerable<DashboardProgressExportColumn> AllDashboardProgressExportColumns()
        {
            return DashboardProgressExportColumns.Concat(DashboardProgressTargetYearExportColumns);
        }

        private static readonly IList<DashboardProgressExportColumn> DashboardProgressTargetYearExportColumns = new List<DashboardProgressExportColumn>
        {
            new DashboardProgressExportColumn("SoBaoCaoDatMucTieuNam", "Số báo cáo đạt mục tiêu năm", x => x.SoBaoCaoDatMucTieuNam),
            new DashboardProgressExportColumn("SoBaoCaoDanhGiaMucTieuNam", "Số báo cáo đã đánh giá mục tiêu năm", x => x.SoBaoCaoDanhGiaMucTieuNam),
            new DashboardProgressExportColumn("TyLeDatMucTieuNam", "Tỷ lệ đạt mục tiêu năm (%)", x => x.TyLeDatMucTieuNam)
        };

        private static readonly IList<DashboardReportDetailExportColumn> DashboardReportDetailExportColumns = new List<DashboardReportDetailExportColumn>
        {
            new DashboardReportDetailExportColumn("TenKhoaPhong", "Khoa / Phòng", x => x.TenKhoaPhong),
            new DashboardReportDetailExportColumn("TenKyBaoCao", "Kỳ báo cáo", x => x.TenKyBaoCao),
            new DashboardReportDetailExportColumn("NamBaoCao", "Năm báo cáo", x => x.NamBaoCao),
            new DashboardReportDetailExportColumn("MaChiSo", "Mã chỉ số", x => x.MaChiSo),
            new DashboardReportDetailExportColumn("TenChiSo", "Tên chỉ số", x => x.TenChiSo),
            new DashboardReportDetailExportColumn("TanSuatBaoCao", "Tần suất báo cáo", x => x.TanSuatBaoCaoText),
            new DashboardReportDetailExportColumn("TuSo", "Tử số", x => x.TuSo),
            new DashboardReportDetailExportColumn("MauSo", "Mẫu số", x => x.MauSo),
            new DashboardReportDetailExportColumn("GiaTriNhap", "Giá trị nhập", x => x.GiaTriNhap),
            new DashboardReportDetailExportColumn("KetQua", "Kết quả", x => FormatDecimal(x.KetQua)),
            new DashboardReportDetailExportColumn("MucTieuNam", "Mục tiêu năm", x => x.MucTieuNam),
            new DashboardReportDetailExportColumn("DatMucTieu", "Đạt mục tiêu", x => x.DatMucTieuText),
            new DashboardReportDetailExportColumn("TrangThaiBaoCao", "Trạng thái báo cáo", x => x.TrangThaiBaoCaoText),
            new DashboardReportDetailExportColumn("NgayGui", "Ngày gửi", x => x.NgayGui.HasValue ? x.NgayGui.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) : string.Empty),
            new DashboardReportDetailExportColumn("GhiChu", "Ghi chú", x => x.GhiChu)
        };

        private class DashboardProgressExportColumn
        {
            // Khởi tạo thành phần và các giá trị cần thiết cho dữ liệu nội bộ của DashboardProgressExportColumn.
            public DashboardProgressExportColumn(string key, string header, Func<DepartmentProgressViewModel, object> value)
            {
                Key = key;
                Header = header;
                Value = value;
            }

            public string Key { get; private set; }
            public string Header { get; private set; }
            public Func<DepartmentProgressViewModel, object> Value { get; private set; }
        }

        private class DashboardReportDetailExportColumn
        {
            // Khởi tạo thành phần và các giá trị cần thiết cho dữ liệu nội bộ của DashboardReportDetailExportColumn.
            public DashboardReportDetailExportColumn(string key, string header, Func<DashboardReportDetailExportRow, object> value)
            {
                Key = key;
                Header = header;
                Value = value;
            }

            public string Key { get; private set; }
            public string Header { get; private set; }
            public Func<DashboardReportDetailExportRow, object> Value { get; private set; }
        }
    }
}
