// Mục đích: quản lý vòng đời báo cáo từ nháp, gửi, duyệt, trả lại đến khóa dữ liệu.
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
    public class ReportService : DbServiceBase
    {
        private readonly IndicatorService _indicators = new IndicatorService();
        private readonly IndicatorCalculationService _calculator = new IndicatorCalculationService();

        // Lấy danh sách báo cáo định kỳ theo bộ lọc, trạng thái hoạt động và phạm vi quyền đang áp dụng.
        public IList<ReportEntryViewModel> GetAll(ReportListQueryDto dto)
        {
            return GetAll(dto.PeriodId, dto.DepartmentId, dto.IndicatorId, dto.IsAdmin, dto.CurrentDepartmentId);
        }

        // Lấy danh sách báo cáo định kỳ theo bộ lọc, trạng thái hoạt động và phạm vi quyền đang áp dụng.
        public IList<ReportEntryViewModel> GetAll(ReportListQueryDto dto, int page, int pageSize, out int totalItems)
        {
            page = NormalizePage(page);
            pageSize = NormalizePageSize(pageSize);

            const string countSql = @"
SELECT COUNT(*)
FROM dbo.BaoCao bc
INNER JOIN dbo.KyBaoCao ky ON ky.KyBaoCaoId = bc.KyBaoCaoId
WHERE (@KyBaoCaoId IS NULL OR bc.KyBaoCaoId = @KyBaoCaoId)
  AND (@KhoaPhongId IS NULL OR bc.KhoaPhongId = @KhoaPhongId)
  AND (@ChiSoChatLuongId IS NULL OR bc.ChiSoChatLuongId = @ChiSoChatLuongId)
  AND (@IsAdmin = 1 OR ky.TrangThai <> @DraftPeriodStatus)
  AND ((@IsAdmin = 1 AND bc.TrangThai IN (@DaGuiStatus, @QuaHanStatus, @DaKhoaStatus, @DaDuyetStatus, @TraLaiStatus))
       OR (@IsAdmin = 0 AND bc.KhoaPhongId = @CurrentKhoaPhongId))";

            var parameters = BuildReportListParameters(dto);
            totalItems = Convert.ToInt32(Scalar(countSql, parameters));

            const string sql = @"
SELECT bc.BaoCaoId, bc.KyBaoCaoId, bc.KhoaPhongId, bc.ChiSoChatLuongId, ISNULL(bc.PhanCongChiSoId, 0) AS PhanCongChiSoId, ky.TenKyBaoCao, kp.TenKhoaPhong,
       cs.MaChiSo, cs.TenChiSo, cs.LoaiCongThuc, bc.TrangThai, bc.YKienPhanHoi,
       ct.TuSo, ct.MauSo, ct.GiaTriNhap, ct.KetQua, ct.DatMucTieu, ct.GhiChu,
       bc.NgayGui, COALESCE(nvNguoiGui.HoTen, nguoiGui.TenDangNhap) AS TenNguoiGui
FROM dbo.BaoCao bc
INNER JOIN dbo.KyBaoCao ky ON ky.KyBaoCaoId = bc.KyBaoCaoId
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = bc.KhoaPhongId
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = bc.ChiSoChatLuongId
LEFT JOIN dbo.BaoCaoChiTiet ct ON ct.BaoCaoId = bc.BaoCaoId
LEFT JOIN dbo.TaiKhoan nguoiGui ON nguoiGui.TaiKhoanId = bc.NguoiGuiId
LEFT JOIN dbo.NhanVien nvNguoiGui ON nvNguoiGui.NhanVienId = nguoiGui.NhanVienId
WHERE (@KyBaoCaoId IS NULL OR bc.KyBaoCaoId = @KyBaoCaoId)
  AND (@KhoaPhongId IS NULL OR bc.KhoaPhongId = @KhoaPhongId)
  AND (@ChiSoChatLuongId IS NULL OR bc.ChiSoChatLuongId = @ChiSoChatLuongId)
  AND (@IsAdmin = 1 OR ky.TrangThai <> @DraftPeriodStatus)
  AND ((@IsAdmin = 1 AND bc.TrangThai IN (@DaGuiStatus, @QuaHanStatus, @DaKhoaStatus, @DaDuyetStatus, @TraLaiStatus))
       OR (@IsAdmin = 0 AND bc.KhoaPhongId = @CurrentKhoaPhongId))
ORDER BY ky.TuNgay DESC, kp.TenKhoaPhong, cs.MaChiSo
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var pagedParameters = BuildReportListParameters(dto)
                .Concat(new[] { Param("@Offset", (page - 1) * pageSize), Param("@PageSize", pageSize) })
                .ToArray();

            return Query(sql, MapReport, pagedParameters);
        }

        // Dựng cấu trúc dữ liệu báo cáo định kỳ từ input đã lọc để tái sử dụng cho truy vấn, view hoặc xuất file.
        private static SqlParameter[] BuildReportListParameters(ReportListQueryDto dto)
        {
            return new[]
            {
                Param("@KyBaoCaoId", dto.PeriodId),
                Param("@KhoaPhongId", dto.DepartmentId),
                Param("@ChiSoChatLuongId", dto.IndicatorId),
                Param("@IsAdmin", dto.IsAdmin),
                Param("@CurrentKhoaPhongId", dto.CurrentDepartmentId),
                Param("@DraftPeriodStatus", (byte)TrangThaiKyBaoCao.Nhap),
                Param("@DaGuiStatus", (byte)TrangThaiBaoCao.DaGui),
                Param("@QuaHanStatus", (byte)TrangThaiBaoCao.QuaHan),
                Param("@DaKhoaStatus", (byte)TrangThaiBaoCao.DaKhoa),
                Param("@DaDuyetStatus", (byte)TrangThaiBaoCao.DaDuyet),
                Param("@TraLaiStatus", (byte)TrangThaiBaoCao.TraLai)
            };
        }

        // Chuẩn hóa số trang để tránh page âm/0 làm sai truy vấn phân trang.
        private static int NormalizePage(int page)
        {
            return page < 1 ? 1 : page;
        }

        // Giới hạn kích thước trang để tránh truy vấn quá lớn hoặc giá trị không hợp lệ.
        private static int NormalizePageSize(int pageSize)
        {
            if (pageSize < 1) return 10;
            return pageSize > 100 ? 100 : pageSize;
        }

        // Lấy danh sách báo cáo định kỳ theo bộ lọc, trạng thái hoạt động và phạm vi quyền đang áp dụng.
        public IList<ReportEntryViewModel> GetAll(int? periodId, int? departmentId, int? indicatorId, bool admin, int? currentDepartmentId)
        {
            const string sql = @"
SELECT bc.BaoCaoId, bc.KyBaoCaoId, bc.KhoaPhongId, bc.ChiSoChatLuongId, ISNULL(bc.PhanCongChiSoId, 0) AS PhanCongChiSoId, ky.TenKyBaoCao, kp.TenKhoaPhong,
       cs.MaChiSo, cs.TenChiSo, cs.LoaiCongThuc, bc.TrangThai, bc.YKienPhanHoi,
       ct.TuSo, ct.MauSo, ct.GiaTriNhap, ct.KetQua, ct.DatMucTieu, ct.GhiChu,
       bc.NgayGui, COALESCE(nvNguoiGui.HoTen, nguoiGui.TenDangNhap) AS TenNguoiGui
FROM dbo.BaoCao bc
INNER JOIN dbo.KyBaoCao ky ON ky.KyBaoCaoId = bc.KyBaoCaoId
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = bc.KhoaPhongId
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = bc.ChiSoChatLuongId
LEFT JOIN dbo.BaoCaoChiTiet ct ON ct.BaoCaoId = bc.BaoCaoId
LEFT JOIN dbo.TaiKhoan nguoiGui ON nguoiGui.TaiKhoanId = bc.NguoiGuiId
LEFT JOIN dbo.NhanVien nvNguoiGui ON nvNguoiGui.NhanVienId = nguoiGui.NhanVienId
WHERE (@KyBaoCaoId IS NULL OR bc.KyBaoCaoId = @KyBaoCaoId)
  AND (@KhoaPhongId IS NULL OR bc.KhoaPhongId = @KhoaPhongId)
  AND (@ChiSoChatLuongId IS NULL OR bc.ChiSoChatLuongId = @ChiSoChatLuongId)
  AND (@IsAdmin = 1 OR ky.TrangThai <> @DraftPeriodStatus)
  AND ((@IsAdmin = 1 AND bc.TrangThai IN (@DaGuiStatus, @QuaHanStatus, @DaKhoaStatus, @DaDuyetStatus, @TraLaiStatus))
       OR (@IsAdmin = 0 AND bc.KhoaPhongId = @CurrentKhoaPhongId))
ORDER BY ky.TuNgay DESC, kp.TenKhoaPhong, cs.MaChiSo";
            return Query(sql, MapReport,
                Param("@KyBaoCaoId", periodId),
                Param("@KhoaPhongId", departmentId),
                Param("@ChiSoChatLuongId", indicatorId),
                Param("@IsAdmin", admin),
                Param("@CurrentKhoaPhongId", currentDepartmentId),
                Param("@DraftPeriodStatus", (byte)TrangThaiKyBaoCao.Nhap),
                Param("@DaGuiStatus", (byte)TrangThaiBaoCao.DaGui),
                Param("@QuaHanStatus", (byte)TrangThaiBaoCao.QuaHan),
                Param("@DaKhoaStatus", (byte)TrangThaiBaoCao.DaKhoa),
                Param("@DaDuyetStatus", (byte)TrangThaiBaoCao.DaDuyet),
                Param("@TraLaiStatus", (byte)TrangThaiBaoCao.TraLai));
        }

        // Lấy các chỉ số đã phân công cho khoa/phòng trong kỳ, tạo dữ liệu nhập báo cáo nếu cần.
        public IList<ReportEntryViewModel> GetAssignedForUser(int periodId, int departmentId)
        {
            const string sql = @"
SELECT ISNULL(bc.BaoCaoId, 0) AS BaoCaoId, @KyBaoCaoId AS KyBaoCaoId, pc.KhoaPhongId, pc.ChiSoChatLuongId, pc.PhanCongChiSoId,
       ky.TenKyBaoCao, kp.TenKhoaPhong, cs.MaChiSo, cs.TenChiSo, cs.LoaiCongThuc,
       ISNULL(bc.TrangThai, 1) AS TrangThai, bc.YKienPhanHoi,
       ct.TuSo, ct.MauSo, ct.GiaTriNhap, ct.KetQua, ct.DatMucTieu, ct.GhiChu,
       bc.NgayGui, COALESCE(nvNguoiGui.HoTen, nguoiGui.TenDangNhap) AS TenNguoiGui
FROM dbo.PhanCongChiSo pc
INNER JOIN dbo.KyBaoCao ky ON ky.KyBaoCaoId = @KyBaoCaoId
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = pc.KhoaPhongId
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = pc.ChiSoChatLuongId
INNER JOIN dbo.ChiSoTanSuatBaoCao cst ON cst.ChiSoChatLuongId = pc.ChiSoChatLuongId AND cst.TanSuatBaoCao = ky.LoaiKyBaoCao
LEFT JOIN dbo.BaoCao bc ON bc.KyBaoCaoId = ky.KyBaoCaoId AND bc.KhoaPhongId = pc.KhoaPhongId AND bc.ChiSoChatLuongId = pc.ChiSoChatLuongId
LEFT JOIN dbo.BaoCaoChiTiet ct ON ct.BaoCaoId = bc.BaoCaoId
LEFT JOIN dbo.TaiKhoan nguoiGui ON nguoiGui.TaiKhoanId = bc.NguoiGuiId
LEFT JOIN dbo.NhanVien nvNguoiGui ON nvNguoiGui.NhanVienId = nguoiGui.NhanVienId
WHERE pc.DangHoatDong = 1 AND pc.KhoaPhongId = @KhoaPhongId
AND ky.TrangThai = @Mo
AND dbo.fn_ChiSoDuocTrienKhaiTrongKy(pc.ChiSoChatLuongId, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay) = 1
ORDER BY cs.MaChiSo";
            return Query(sql, MapReport,
                Param("@KyBaoCaoId", periodId),
                Param("@KhoaPhongId", departmentId),
                Param("@Mo", (byte)TrangThaiKyBaoCao.Mo));
        }

        // Lấy một bản ghi báo cáo định kỳ theo khóa chính; trả null khi không tìm thấy để tầng gọi xử lý 404/empty state.
        public ReportEntryViewModel Get(int id)
        {
            const string sql = @"
SELECT bc.BaoCaoId, bc.KyBaoCaoId, bc.KhoaPhongId, bc.ChiSoChatLuongId, ISNULL(bc.PhanCongChiSoId, 0) AS PhanCongChiSoId, ky.TenKyBaoCao, kp.TenKhoaPhong,
       cs.MaChiSo, cs.TenChiSo, cs.LoaiCongThuc, bc.TrangThai, bc.YKienPhanHoi,
       ct.TuSo, ct.MauSo, ct.GiaTriNhap, ct.KetQua, ct.DatMucTieu, ct.GhiChu,
       bc.NgayGui, COALESCE(nvNguoiGui.HoTen, nguoiGui.TenDangNhap) AS TenNguoiGui
FROM dbo.BaoCao bc
INNER JOIN dbo.KyBaoCao ky ON ky.KyBaoCaoId = bc.KyBaoCaoId
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = bc.KhoaPhongId
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = bc.ChiSoChatLuongId
LEFT JOIN dbo.BaoCaoChiTiet ct ON ct.BaoCaoId = bc.BaoCaoId
LEFT JOIN dbo.TaiKhoan nguoiGui ON nguoiGui.TaiKhoanId = bc.NguoiGuiId
LEFT JOIN dbo.NhanVien nvNguoiGui ON nvNguoiGui.NhanVienId = nguoiGui.NhanVienId
WHERE bc.BaoCaoId=@Id";
            return Query(sql, MapReport, Param("@Id", id)).FirstOrDefault();
        }

        // Lưu nháp báo cáo định kỳ, tính lại kết quả chỉ số và giữ trạng thái cho phép chỉnh sửa trước khi gửi.
        public int SaveDraft(ReportDraftDto dto, int userId)
        {
            return SaveDraft(new ReportEntryViewModel
            {
                BaoCaoId = dto.BaoCaoId,
                KyBaoCaoId = dto.KyBaoCaoId,
                KhoaPhongId = dto.KhoaPhongId,
                ChiSoChatLuongId = dto.ChiSoChatLuongId,
                PhanCongChiSoId = dto.PhanCongChiSoId,
                TrangThai = dto.TrangThai,
                TuSo = dto.TuSo,
                MauSo = dto.MauSo,
                GiaTriNhap = dto.GiaTriNhap,
                KetQua = dto.KetQua,
                DatMucTieu = dto.DatMucTieu,
                GhiChu = dto.GhiChu,
                YKienPhanHoi = dto.YKienPhanHoi
            }, userId);
        }

        // Lưu nháp báo cáo định kỳ, tính lại kết quả chỉ số và giữ trạng thái cho phép chỉnh sửa trước khi gửi.
        public int SaveDraft(ReportEntryViewModel model, int userId)
        {
            var isNewReport = model.BaoCaoId == 0;
            ReportEntryViewModel existingReport = null;
            if (!isNewReport)
            {
                existingReport = Get(model.BaoCaoId);
                if (existingReport == null)
                {
                    throw new InvalidOperationException("Không tìm thấy báo cáo cần sửa.");
                }

                if (existingReport.TrangThai != TrangThaiBaoCao.Nhap && existingReport.TrangThai != TrangThaiBaoCao.TraLai)
                {
                    throw new InvalidOperationException("Chỉ được sửa báo cáo ở trạng thái Nháp hoặc Trả lại.");
                }

                model.KyBaoCaoId = existingReport.KyBaoCaoId;
                model.KhoaPhongId = existingReport.KhoaPhongId;
                model.ChiSoChatLuongId = existingReport.ChiSoChatLuongId;
                model.PhanCongChiSoId = existingReport.PhanCongChiSoId;
            }

            var targetYear = GetReportingYear(model.KyBaoCaoId);
            var indicator = _indicators.Get(model.ChiSoChatLuongId, targetYear);
            _calculator.Calculate(model, indicator);
            var beforeSnapshot = isNewReport ? null : GetReportDetailSnapshot(model.BaoCaoId);
            var now = GetVietnamLocalNow();

            ExecuteInTransaction((conn, trans) =>
            {
                if (isNewReport)
                {
                    var assignmentId = model.PhanCongChiSoId > 0
                        ? model.PhanCongChiSoId
                        : Convert.ToInt32(Scalar(conn, trans, @"SELECT TOP 1 PhanCongChiSoId FROM dbo.PhanCongChiSo
WHERE KhoaPhongId=@KhoaPhongId AND ChiSoChatLuongId=@ChiSoChatLuongId
ORDER BY DangHoatDong DESC, PhanCongChiSoId",
                            Param("@KhoaPhongId", model.KhoaPhongId),
                            Param("@ChiSoChatLuongId", model.ChiSoChatLuongId)));

                    model.BaoCaoId = Convert.ToInt32(Scalar(conn, trans, @"INSERT INTO dbo.BaoCao(KyBaoCaoId, KhoaPhongId, ChiSoChatLuongId, PhanCongChiSoId, TrangThai, NguoiTaoId, NgayTao, NgayCapNhat)
OUTPUT INSERTED.BaoCaoId VALUES(@KyBaoCaoId, @KhoaPhongId, @ChiSoChatLuongId, @PhanCongChiSoId, @TrangThai, @NguoiTaoId, @Now, @Now)",
                        Param("@KyBaoCaoId", model.KyBaoCaoId),
                        Param("@KhoaPhongId", model.KhoaPhongId),
                        Param("@ChiSoChatLuongId", model.ChiSoChatLuongId),
                        Param("@PhanCongChiSoId", assignmentId),
                        Param("@TrangThai", (byte)TrangThaiBaoCao.Nhap),
                        Param("@NguoiTaoId", userId),
                        Param("@Now", now)));
                }
                else
                {
                    Execute(conn, trans, "UPDATE dbo.BaoCao SET NgayCapNhat=@Now WHERE BaoCaoId=@Id AND TrangThai IN (@Nhap, @TraLai)",
                        Param("@Id", model.BaoCaoId),
                        Param("@Nhap", (byte)TrangThaiBaoCao.Nhap),
                        Param("@TraLai", (byte)TrangThaiBaoCao.TraLai),
                        Param("@Now", now));
                }

                Execute(conn, trans, @"
IF EXISTS (SELECT 1 FROM dbo.BaoCaoChiTiet WHERE BaoCaoId=@BaoCaoId)
    UPDATE dbo.BaoCaoChiTiet SET TuSo=@TuSo, MauSo=@MauSo, GiaTriNhap=@GiaTriNhap, KetQua=@KetQua, DatMucTieu=@DatMucTieu, GhiChu=@GhiChu, NgayCapNhat=@Now WHERE BaoCaoId=@BaoCaoId
ELSE
    INSERT INTO dbo.BaoCaoChiTiet(BaoCaoId, TuSo, MauSo, GiaTriNhap, KetQua, DatMucTieu, GhiChu) VALUES(@BaoCaoId, @TuSo, @MauSo, @GiaTriNhap, @KetQua, @DatMucTieu, @GhiChu)",
                    Param("@BaoCaoId", model.BaoCaoId),
                    Param("@TuSo", model.TuSo),
                    Param("@MauSo", model.MauSo),
                    Param("@GiaTriNhap", model.GiaTriNhap),
                    Param("@KetQua", model.KetQua),
                    Param("@DatMucTieu", model.DatMucTieu),
                    Param("@GhiChu", model.GhiChu),
                    Param("@Now", now));

                var afterSnapshot = BuildReportDetailSnapshot(model);
                LogSystemAction(conn, trans,
                    userId,
                    "BaoCao",
                    isNewReport ? "TaoNhap" : "SuaNhap",
                    "BaoCao",
                    model.BaoCaoId,
                    isNewReport
                        ? "Tạo báo cáo nháp. Sau: " + afterSnapshot
                        : "Sửa báo cáo nháp. Trước: " + beforeSnapshot + " | Sau: " + afterSnapshot);
            });

            return model.BaoCaoId;
        }

        // Xử lý chức năng báo cáo định kỳ của method GetReportingYear, giữ logic nghiệp vụ tập trung trong tầng phù hợp.
        private int? GetReportingYear(int reportingPeriodId)
        {
            var value = Scalar("SELECT DATEPART(YEAR, TuNgay) FROM dbo.KyBaoCao WHERE KyBaoCaoId=@KyBaoCaoId",
                Param("@KyBaoCaoId", reportingPeriodId));
            return value == null || value == DBNull.Value ? (int?)null : Convert.ToInt32(value);
        }

        // Gửi dữ liệu và cập nhật trạng thái tương ứng của quy trình báo cáo.
        public void Submit(int id, int userId)
        {
            var now = GetVietnamLocalNow();
            var affectedRows = Execute(@"UPDATE bc
SET TrangThai=CASE WHEN CAST(@Now AS date) > ky.HanNop THEN @QuaHan ELSE @DaGui END,
    NguoiGuiId=@NguoiGuiId,
    NgayGui=@Now,
    NgayCapNhat=@Now
FROM dbo.BaoCao bc
INNER JOIN dbo.KyBaoCao ky ON ky.KyBaoCaoId = bc.KyBaoCaoId
WHERE bc.BaoCaoId=@Id AND bc.TrangThai IN (@Nhap, @TraLai)",
                Param("@DaGui", (byte)TrangThaiBaoCao.DaGui),
                Param("@QuaHan", (byte)TrangThaiBaoCao.QuaHan),
                Param("@NguoiGuiId", userId),
                Param("@Id", id),
                Param("@Nhap", (byte)TrangThaiBaoCao.Nhap),
                Param("@TraLai", (byte)TrangThaiBaoCao.TraLai),
                Param("@Now", now));

            if (affectedRows > 0)
            {
                LogSystemAction(
                    userId,
                    "BaoCao",
                    "GuiBaoCao",
                    "BaoCao",
                    id,
                    "Gửi báo cáo. Dữ liệu tại thời điểm gửi: " + GetReportDetailSnapshot(id));
            }
        }

        // Chuyển bản ghi sang trạng thái không còn cho phép chỉnh sửa.
        public void Lock(int id)
        {
            Execute("UPDATE dbo.BaoCao SET TrangThai=@TrangThai, NgayCapNhat=@Now WHERE BaoCaoId=@Id AND TrangThai IN (@DaGui, @QuaHan, @DaDuyet)",
                Param("@TrangThai", (byte)TrangThaiBaoCao.DaKhoa),
                Param("@Id", id),
                Param("@DaGui", (byte)TrangThaiBaoCao.DaGui),
                Param("@QuaHan", (byte)TrangThaiBaoCao.QuaHan),
                Param("@DaDuyet", (byte)TrangThaiBaoCao.DaDuyet),
                Param("@Now", GetVietnamLocalNow()));
        }

        // Duyệt báo cáo.
        public void Approve(int id, int userId)
        {
            var now = GetVietnamLocalNow();
            var affectedRows = Execute(@"UPDATE dbo.BaoCao
SET TrangThai=@DaDuyet,
    NgayCapNhat=@Now
WHERE BaoCaoId=@Id AND TrangThai=@DaGui",
                Param("@DaDuyet", (byte)TrangThaiBaoCao.DaDuyet),
                Param("@Id", id),
                Param("@DaGui", (byte)TrangThaiBaoCao.DaGui),
                Param("@Now", now));

            if (affectedRows > 0)
            {
                LogSystemAction(
                    userId,
                    "BaoCao",
                    "DuyetBaoCao",
                    "BaoCao",
                    id,
                    "Duyệt báo cáo.");
            }
        }

        // Trả lại báo cáo với nhận xét.
        public void Reject(int id, int userId, string yKienPhanHoi)
        {
            var now = GetVietnamLocalNow();
            var affectedRows = Execute(@"UPDATE dbo.BaoCao
SET TrangThai=@TraLai,
    YKienPhanHoi=@YKienPhanHoi,
    NgayCapNhat=@Now
WHERE BaoCaoId=@Id AND TrangThai=@DaGui",
                Param("@TraLai", (byte)TrangThaiBaoCao.TraLai),
                Param("@YKienPhanHoi", yKienPhanHoi),
                Param("@Id", id),
                Param("@DaGui", (byte)TrangThaiBaoCao.DaGui),
                Param("@Now", now));

            if (affectedRows > 0)
            {
                LogSystemAction(
                    userId,
                    "BaoCao",
                    "TraLaiBaoCao",
                    "BaoCao",
                    id,
                    "Trả lại báo cáo. Nhận xét: " + yKienPhanHoi);
            }
        }

        // Chuyển một dòng dữ liệu từ SqlDataReader sang view model/dto báo cáo định kỳ đúng kiểu và tên trường.
        private static ReportEntryViewModel MapReport(SqlDataReader reader)
        {
            return new ReportEntryViewModel
            {
                BaoCaoId = Int(reader, "BaoCaoId"),
                KyBaoCaoId = Int(reader, "KyBaoCaoId"),
                KhoaPhongId = Int(reader, "KhoaPhongId"),
                ChiSoChatLuongId = Int(reader, "ChiSoChatLuongId"),
                PhanCongChiSoId = Int(reader, "PhanCongChiSoId"),
                TenKyBaoCao = String(reader, "TenKyBaoCao"),
                TenKhoaPhong = String(reader, "TenKhoaPhong"),
                MaChiSo = String(reader, "MaChiSo"),
                TenChiSo = String(reader, "TenChiSo"),
                LoaiCongThuc = (LoaiCongThuc)reader.GetByte(reader.GetOrdinal("LoaiCongThuc")),
                TuSoKhongVuotMauSo = IndicatorCalculationService.RequiresNumeratorWithinDenominator(String(reader, "MaChiSo"), String(reader, "TenChiSo")),
                TrangThai = (TrangThaiBaoCao)Convert.ToByte(reader["TrangThai"]),
                TuSo = NullableDecimal(reader, "TuSo"),
                MauSo = NullableDecimal(reader, "MauSo"),
                GiaTriNhap = NullableDecimal(reader, "GiaTriNhap"),
                KetQua = NullableDecimal(reader, "KetQua"),
                DatMucTieu = reader.IsDBNull(reader.GetOrdinal("DatMucTieu")) ? (bool?)null : reader.GetBoolean(reader.GetOrdinal("DatMucTieu")),
                GhiChu = String(reader, "GhiChu"),
                YKienPhanHoi = String(reader, "YKienPhanHoi"),
                NgayGui = NullableDateTime(reader, "NgayGui"),
                TenNguoiGui = String(reader, "TenNguoiGui")
            };
        }

        // Xử lý chức năng báo cáo định kỳ của method GetReportDetailSnapshot, giữ logic nghiệp vụ tập trung trong tầng phù hợp.
        private string GetReportDetailSnapshot(int reportId)
        {
            var snapshot = QuerySingle(@"
SELECT bc.BaoCaoId, bc.KyBaoCaoId, bc.KhoaPhongId, bc.ChiSoChatLuongId, ISNULL(bc.PhanCongChiSoId, 0) AS PhanCongChiSoId,
       ct.TuSo, ct.MauSo, ct.GiaTriNhap, ct.KetQua, ct.DatMucTieu, ct.GhiChu
FROM dbo.BaoCao bc
LEFT JOIN dbo.BaoCaoChiTiet ct ON ct.BaoCaoId = bc.BaoCaoId
WHERE bc.BaoCaoId = @BaoCaoId",
                reader => new ReportEntryViewModel
                {
                    BaoCaoId = Int(reader, "BaoCaoId"),
                    KyBaoCaoId = Int(reader, "KyBaoCaoId"),
                    KhoaPhongId = Int(reader, "KhoaPhongId"),
                    ChiSoChatLuongId = Int(reader, "ChiSoChatLuongId"),
                    PhanCongChiSoId = Int(reader, "PhanCongChiSoId"),
                    TuSo = NullableDecimal(reader, "TuSo"),
                    MauSo = NullableDecimal(reader, "MauSo"),
                    GiaTriNhap = NullableDecimal(reader, "GiaTriNhap"),
                    KetQua = NullableDecimal(reader, "KetQua"),
                    DatMucTieu = reader.IsDBNull(reader.GetOrdinal("DatMucTieu")) ? (bool?)null : reader.GetBoolean(reader.GetOrdinal("DatMucTieu")),
                    GhiChu = String(reader, "GhiChu")
                },
                Param("@BaoCaoId", reportId));

            return snapshot == null ? "Không tìm thấy dữ liệu báo cáo." : BuildReportDetailSnapshot(snapshot);
        }

        // Dựng cấu trúc dữ liệu báo cáo định kỳ từ input đã lọc để tái sử dụng cho truy vấn, view hoặc xuất file.
        private static string BuildReportDetailSnapshot(ReportEntryViewModel model)
        {
            return string.Format(
                "BaoCaoId={0}; KyBaoCaoId={1}; KhoaPhongId={2}; ChiSoChatLuongId={3}; PhanCongChiSoId={4}; TuSo={5}; MauSo={6}; GiaTriNhap={7}; KetQua={8}; DatMucTieu={9}; GhiChu={10}",
                model.BaoCaoId,
                model.KyBaoCaoId,
                model.KhoaPhongId,
                model.ChiSoChatLuongId,
                model.PhanCongChiSoId,
                FormatValue(model.TuSo),
                FormatValue(model.MauSo),
                FormatValue(model.GiaTriNhap),
                FormatValue(model.KetQua),
                model.DatMucTieu.HasValue ? model.DatMucTieu.Value.ToString() : "NULL",
                string.IsNullOrWhiteSpace(model.GhiChu) ? "NULL" : model.GhiChu);
        }

        // Định dạng giá trị theo quy ước hiển thị của quy trình báo cáo.
        private static string FormatValue(decimal? value)
        {
            return value.HasValue ? value.Value.ToString("0.####") : "NULL";
        }

        // Lấy thời điểm hiện tại theo múi giờ Việt Nam và có phương án dự phòng.
        private static DateTime GetVietnamLocalNow()
        {
            try
            {
                var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
            }
            catch (TimeZoneNotFoundException)
            {
                return DateTime.Now;
            }
            catch (InvalidTimeZoneException)
            {
                return DateTime.Now;
            }
        }

        // Ghi lại thông tin phục vụ theo dõi và kiểm toán quy trình báo cáo.
        private void LogSystemAction(int userId, string feature, string action, string entityName, int? entityId, string content)
        {
            Execute(@"INSERT INTO dbo.NhatKyHeThong(TaiKhoanId, ChucNang, HanhDong, DoiTuong, DoiTuongId, NoiDung)
VALUES(@TaiKhoanId, @ChucNang, @HanhDong, @DoiTuong, @DoiTuongId, @NoiDung)",
                Param("@TaiKhoanId", userId),
                Param("@ChucNang", feature),
                Param("@HanhDong", action),
                Param("@DoiTuong", entityName),
                Param("@DoiTuongId", entityId),
                Param("@NoiDung", content));
        }

        // Ghi lại thông tin phục vụ theo dõi và kiểm toán quy trình báo cáo.
        private void LogSystemAction(SqlConnection connection, SqlTransaction transaction, int userId, string feature, string action, string entityName, int? entityId, string content)
        {
            Execute(connection, transaction, @"INSERT INTO dbo.NhatKyHeThong(TaiKhoanId, ChucNang, HanhDong, DoiTuong, DoiTuongId, NoiDung)
VALUES(@TaiKhoanId, @ChucNang, @HanhDong, @DoiTuong, @DoiTuongId, @NoiDung)",
                Param("@TaiKhoanId", userId),
                Param("@ChucNang", feature),
                Param("@HanhDong", action),
                Param("@DoiTuong", entityName),
                Param("@DoiTuongId", entityId),
                Param("@NoiDung", content));
        }
    }
}
