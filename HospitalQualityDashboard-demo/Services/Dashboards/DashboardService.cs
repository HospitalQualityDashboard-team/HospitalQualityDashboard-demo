// Mục đích: cung cấp số liệu tổng quan dashboard theo vai trò, khoa/phòng và tần suất báo cáo.
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.Enums;
using HospitalQualityDashboardDemo.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Services
{
    public partial class DashboardService : DbServiceBase
    {
        // Dùng timeout dài hơn mặc định vì dashboard phải tổng hợp nhiều bảng báo cáo và chỉ số.
        public DashboardService()
            : base(DatabaseConfiguration.GetConnectionString(), 60)
        {
        }

        #pragma warning disable 0162
        // Tổng hợp số liệu dashboard theo role, khoa/phòng và tần suất để tránh lộ dữ liệu ngoài phạm vi.
        public DashboardViewModel GetDashboard(bool admin, int? departmentId, int? tanSuatFilter = null, string departmentStatusFilter = "active")
        {
            return GetDashboardOptimized(admin, departmentId, tanSuatFilter, NormalizeDepartmentStatusFilter(departmentStatusFilter));

            var model = new DashboardViewModel
            {
                IsAdmin = admin,
                DepartmentProgress = new List<DepartmentProgressViewModel>(),
                MissingReports = new List<MissingReportAlertViewModel>(),
                SelectedTanSuat = tanSuatFilter
            };

            model.TanSuatOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Tất cả tần suất", Selected = !tanSuatFilter.HasValue },
                new SelectListItem { Value = "1", Text = "Hàng ngày", Selected = tanSuatFilter == 1 },
                new SelectListItem { Value = "2", Text = "Hàng tuần", Selected = tanSuatFilter == 2 },
                new SelectListItem { Value = "3", Text = "Hàng tháng", Selected = tanSuatFilter == 3 },
                new SelectListItem { Value = "4", Text = "Hàng quý", Selected = tanSuatFilter == 4 },
                new SelectListItem { Value = "5", Text = "6 tháng", Selected = tanSuatFilter == 5 },
                new SelectListItem { Value = "9", Text = "9 tháng", Selected = tanSuatFilter == 9 },
                new SelectListItem { Value = "6", Text = "Hàng năm", Selected = tanSuatFilter == 6 },
                new SelectListItem { Value = "7", Text = "Khi phát sinh", Selected = tanSuatFilter == 7 },
                new SelectListItem { Value = "8", Text = "Trước và sau khi thực hiện", Selected = tanSuatFilter == 8 }
            };

            if (admin)
            {
                if (tanSuatFilter.HasValue)
                {
                    model.BaoCaoDaGui = Convert.ToInt32(Scalar(@"
                        SELECT COUNT(DISTINCT bc.BaoCaoId) 
                        FROM dbo.BaoCao bc 
                        JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = bc.ChiSoChatLuongId
                        WHERE bc.TrangThai IN (2,3,4) AND ts.TanSuatBaoCao = @TanSuat", Param("@TanSuat", tanSuatFilter.Value)));

                    model.ChiSoDuocPhanCong = Convert.ToInt32(Scalar(@"
                        SELECT COUNT(DISTINCT pc.PhanCongChiSoId) 
                        FROM dbo.PhanCongChiSo pc 
                        JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId
                        WHERE pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = @TanSuat", Param("@TanSuat", tanSuatFilter.Value)));

                    model.TongChiSo = Convert.ToInt32(Scalar(@"
                        SELECT COUNT(DISTINCT cs.ChiSoChatLuongId) 
                        FROM dbo.ChiSoChatLuong cs 
                        JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = cs.ChiSoChatLuongId
                        WHERE cs.DangHoatDong = 1 AND ts.TanSuatBaoCao = @TanSuat", Param("@TanSuat", tanSuatFilter.Value)));

                    model.BaoCaoQuaHan = Convert.ToInt32(Scalar(@"
                        SELECT COUNT(DISTINCT bc.BaoCaoId) 
                        FROM dbo.BaoCao bc 
                        JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = bc.ChiSoChatLuongId
                        WHERE bc.TrangThai = @QuaHanStatus AND ts.TanSuatBaoCao = @TanSuat", 
                        Param("@QuaHanStatus", (byte)TrangThaiBaoCao.QuaHan), Param("@TanSuat", tanSuatFilter.Value)));
                }
                else
                {
                    model.BaoCaoDaGui = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.BaoCao WHERE TrangThai IN (2,3,4)"));
                    model.ChiSoDuocPhanCong = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.PhanCongChiSo WHERE DangHoatDong=1"));
                    model.TongChiSo = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.ChiSoChatLuong WHERE DangHoatDong=1"));
                    model.BaoCaoQuaHan = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.BaoCao bc WHERE bc.TrangThai=@QuaHanStatus", Param("@QuaHanStatus", (byte)TrangThaiBaoCao.QuaHan)));
                }
            }
            else
            {
                if (tanSuatFilter.HasValue)
                {
                    model.BaoCaoDaGui = Convert.ToInt32(Scalar(@"
                        SELECT COUNT(DISTINCT bc.BaoCaoId) 
                        FROM dbo.BaoCao bc 
                        JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = bc.ChiSoChatLuongId
                        WHERE bc.TrangThai IN (2,3,4) AND bc.KhoaPhongId=@KhoaPhongId AND ts.TanSuatBaoCao = @TanSuat", 
                        Param("@KhoaPhongId", departmentId), Param("@TanSuat", tanSuatFilter.Value)));

                    model.ChiSoDuocPhanCong = Convert.ToInt32(Scalar(@"
                        SELECT COUNT(DISTINCT pc.PhanCongChiSoId) 
                        FROM dbo.PhanCongChiSo pc 
                        JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId
                        WHERE pc.DangHoatDong = 1 AND pc.KhoaPhongId=@KhoaPhongId AND ts.TanSuatBaoCao = @TanSuat", 
                        Param("@KhoaPhongId", departmentId), Param("@TanSuat", tanSuatFilter.Value)));

                    model.TongChiSo = model.ChiSoDuocPhanCong;

                    model.BaoCaoQuaHan = Convert.ToInt32(Scalar(@"
                        SELECT COUNT(DISTINCT bc.BaoCaoId) 
                        FROM dbo.BaoCao bc 
                        JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = bc.ChiSoChatLuongId
                        WHERE bc.TrangThai = @QuaHanStatus AND bc.KhoaPhongId=@KhoaPhongId AND ts.TanSuatBaoCao = @TanSuat", 
                        Param("@QuaHanStatus", (byte)TrangThaiBaoCao.QuaHan), Param("@KhoaPhongId", departmentId), Param("@TanSuat", tanSuatFilter.Value)));
                }
                else
                {
                    model.BaoCaoDaGui = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.BaoCao WHERE TrangThai IN (2,3,4) AND KhoaPhongId=@KhoaPhongId", Param("@KhoaPhongId", departmentId)));
                    model.ChiSoDuocPhanCong = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.PhanCongChiSo WHERE DangHoatDong=1 AND KhoaPhongId=@KhoaPhongId", Param("@KhoaPhongId", departmentId)));
                    model.TongChiSo = model.ChiSoDuocPhanCong;
                    model.BaoCaoQuaHan = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.BaoCao bc WHERE bc.TrangThai=@QuaHanStatus AND bc.KhoaPhongId=@KhoaPhongId", Param("@QuaHanStatus", (byte)TrangThaiBaoCao.QuaHan), Param("@KhoaPhongId", departmentId)));
                }
            }

            model.BaoCaoThieu = model.ChiSoDuocPhanCong - model.BaoCaoDaGui;

            if (admin)
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
    
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 3) AS TongThang,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 3 AND bc.TrangThai IN (2,3,4)) AS DaGuiThang,

    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 4) AS TongQuy,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 4 AND bc.TrangThai IN (2,3,4)) AS DaGuiQuy,

    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 6) AS TongNam,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = kp.KhoaPhongId WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 6 AND bc.TrangThai IN (2,3,4)) AS DaGuiNam
FROM dbo.KhoaPhong kp
ORDER BY kp.TenKhoaPhong";
                }

                model.DepartmentProgress = Query(query, r => new DepartmentProgressViewModel
                {
                    TenKhoaPhong = String(r, "TenKhoaPhong"),
                    Tong = Int(r, "Tong"),
                    DaGui = Int(r, "DaGui"),
                    LuuNhap = Int(r, "LuuNhap"),
                    
                    TongThang = Int(r, "TongThang"),
                    DaGuiThang = Int(r, "DaGuiThang"),
                    
                    TongQuy = Int(r, "TongQuy"),
                    DaGuiQuy = Int(r, "DaGuiQuy"),
                    
                    TongNam = Int(r, "TongNam"),
                    DaGuiNam = Int(r, "DaGuiNam")
                }, sqlParams.ToArray());

                foreach (var progress in model.DepartmentProgress)
                {
                    progress.XepLoai = CalculateXepLoai(progress.DaGui, progress.Tong);
                    progress.XepLoaiThang = CalculateXepLoai(progress.DaGuiThang, progress.TongThang);
                    progress.XepLoaiQuy = CalculateXepLoai(progress.DaGuiQuy, progress.TongQuy);
                    progress.XepLoaiNam = CalculateXepLoai(progress.DaGuiNam, progress.TongNam);
                }
            }
            else if (departmentId.HasValue)
            {
                model.MissingReports = GetMissingReportsForDepartment(departmentId.Value);
                model.BaoCaoThieu = model.MissingReports.Count;
                model.DueSoonReportCount = model.MissingReports.Count(x => x.IsDueSoon);
                model.OverdueMissingReportCount = model.MissingReports.Count(x => x.IsOverdue);

                var departmentName = Convert.ToString(Scalar("SELECT TenKhoaPhong FROM dbo.KhoaPhong WHERE KhoaPhongId=@KhoaPhongId", Param("@KhoaPhongId", departmentId)));
                if (!string.IsNullOrWhiteSpace(departmentName))
                {
                    string query;
                    List<SqlParameter> sqlParams = new List<SqlParameter>();
                    sqlParams.Add(Param("@KhoaPhongId", departmentId.Value));
                    
                    if (tanSuatFilter.HasValue)
                    {
                        query = @"
SELECT 
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = @TanSuat) AS Tong,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = @KhoaPhongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = @TanSuat AND bc.TrangThai IN (2,3,4)) AS DaGui,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = @KhoaPhongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = @TanSuat AND bc.TrangThai = 1) AS LuuNhap,
    
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 3) AS TongThang,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = @KhoaPhongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 3 AND bc.TrangThai IN (2,3,4)) AS DaGuiThang,

    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 4) AS TongQuy,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = @KhoaPhongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 4 AND bc.TrangThai IN (2,3,4)) AS DaGuiQuy,

    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 6) AS TongNam,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = @KhoaPhongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 6 AND bc.TrangThai IN (2,3,4)) AS DaGuiNam";
                        sqlParams.Add(Param("@TanSuat", tanSuatFilter.Value));
                    }
                    else
                    {
                        query = @"
SELECT 
    (SELECT COUNT(*) FROM dbo.PhanCongChiSo pc WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1) AS Tong,
    (SELECT COUNT(*) FROM dbo.PhanCongChiSo pc JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = @KhoaPhongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND bc.TrangThai IN (2,3,4)) AS DaGui,
    (SELECT COUNT(*) FROM dbo.PhanCongChiSo pc JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = @KhoaPhongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND bc.TrangThai = 1) AS LuuNhap,
    
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 3) AS TongThang,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = @KhoaPhongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 3 AND bc.TrangThai IN (2,3,4)) AS DaGuiThang,

    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 4) AS TongQuy,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = @KhoaPhongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 4 AND bc.TrangThai IN (2,3,4)) AS DaGuiQuy,

    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 6) AS TongNam,
    (SELECT COUNT(DISTINCT pc.ChiSoChatLuongId) FROM dbo.PhanCongChiSo pc JOIN dbo.ChiSoTanSuatBaoCao ts ON ts.ChiSoChatLuongId = pc.ChiSoChatLuongId JOIN dbo.BaoCao bc ON bc.ChiSoChatLuongId = pc.ChiSoChatLuongId AND bc.KhoaPhongId = @KhoaPhongId WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1 AND ts.TanSuatBaoCao = 6 AND bc.TrangThai IN (2,3,4)) AS DaGuiNam";
                    }

                    var progress = Query(query, r => new DepartmentProgressViewModel
                    {
                        TenKhoaPhong = departmentName,
                        Tong = Int(r, "Tong"),
                        DaGui = Int(r, "DaGui"),
                        LuuNhap = Int(r, "LuuNhap"),
                        
                        TongThang = Int(r, "TongThang"),
                        DaGuiThang = Int(r, "DaGuiThang"),
                        
                        TongQuy = Int(r, "TongQuy"),
                        DaGuiQuy = Int(r, "DaGuiQuy"),
                        
                        TongNam = Int(r, "TongNam"),
                        DaGuiNam = Int(r, "DaGuiNam")
                    }, sqlParams.ToArray()).FirstOrDefault();

                    if (progress != null)
                    {
                        progress.XepLoai = CalculateXepLoai(progress.DaGui, progress.Tong);
                        progress.XepLoaiThang = CalculateXepLoai(progress.DaGuiThang, progress.TongThang);
                        progress.XepLoaiQuy = CalculateXepLoai(progress.DaGuiQuy, progress.TongQuy);
                        progress.XepLoaiNam = CalculateXepLoai(progress.DaGuiNam, progress.TongNam);
                        model.DepartmentProgress.Add(progress);
                    }
                }
            }

            return model;
        }
        #pragma warning restore 0162
    }
}
