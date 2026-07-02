// Mục đích: quản lý kỳ báo cáo, trạng thái mở/khóa và phạm vi áp dụng cho khoa/phòng.
using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.Enums;
using HospitalQualityDashboardDemo.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Services
{
    public class ReportingPeriodService : DbServiceBase
    {
        // Lấy danh sách kỳ báo cáo theo bộ lọc, trạng thái hoạt động và phạm vi quyền đang áp dụng.
        public IList<KyBaoCaoViewModel> GetAll()
        {
            const string sql = @"
WITH ExpectedSlots AS
(
    SELECT DISTINCT ky.KyBaoCaoId, pc.KhoaPhongId, pc.ChiSoChatLuongId
    FROM dbo.KyBaoCao ky
    INNER JOIN dbo.PhanCongChiSo pc ON pc.DangHoatDong = 1
    INNER JOIN dbo.ChiSoTanSuatBaoCao cst
        ON cst.ChiSoChatLuongId = pc.ChiSoChatLuongId
       AND cst.TanSuatBaoCao = ky.LoaiKyBaoCao
    WHERE dbo.fn_ChiSoDuocTrienKhaiTrongKy(pc.ChiSoChatLuongId, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay) = 1
),
CompletedSlots AS
(
    SELECT DISTINCT bc.KyBaoCaoId, bc.KhoaPhongId, bc.ChiSoChatLuongId
    FROM dbo.BaoCao bc
    WHERE bc.TrangThai IN (@DaGuiStatus, @QuaHanStatus, @DaKhoaStatus, @DaDuyetStatus)
)
SELECT ky.KyBaoCaoId, ky.TenKyBaoCao, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay, ky.HanNop, ky.TrangThai,
       COUNT(es.ChiSoChatLuongId) AS TongBaoCao,
       ISNULL(SUM(CASE WHEN bc.KyBaoCaoId IS NOT NULL THEN 1 ELSE 0 END), 0) AS DaGui
FROM dbo.KyBaoCao ky
LEFT JOIN ExpectedSlots es ON es.KyBaoCaoId = ky.KyBaoCaoId
LEFT JOIN CompletedSlots bc
    ON bc.KyBaoCaoId = es.KyBaoCaoId
   AND bc.KhoaPhongId = es.KhoaPhongId
   AND bc.ChiSoChatLuongId = es.ChiSoChatLuongId
GROUP BY ky.KyBaoCaoId, ky.TenKyBaoCao, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay, ky.HanNop, ky.TrangThai
ORDER BY ky.TuNgay DESC";
            return Query(sql, MapPeriod, SubmittedStatusParams());
        }

        // Lấy danh sách kỳ báo cáo theo bộ lọc, trạng thái hoạt động và phạm vi quyền đang áp dụng.
        public IList<KyBaoCaoViewModel> GetAll(int page, int pageSize, out int totalItems)
        {
            page = NormalizePage(page);
            pageSize = NormalizePageSize(pageSize);
            totalItems = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.KyBaoCao"));

            const string sql = @"
WITH ExpectedSlots AS
(
    SELECT DISTINCT ky.KyBaoCaoId, pc.KhoaPhongId, pc.ChiSoChatLuongId
    FROM dbo.KyBaoCao ky
    INNER JOIN dbo.PhanCongChiSo pc ON pc.DangHoatDong = 1
    INNER JOIN dbo.ChiSoTanSuatBaoCao cst
        ON cst.ChiSoChatLuongId = pc.ChiSoChatLuongId
       AND cst.TanSuatBaoCao = ky.LoaiKyBaoCao
    WHERE dbo.fn_ChiSoDuocTrienKhaiTrongKy(pc.ChiSoChatLuongId, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay) = 1
),
CompletedSlots AS
(
    SELECT DISTINCT bc.KyBaoCaoId, bc.KhoaPhongId, bc.ChiSoChatLuongId
    FROM dbo.BaoCao bc
    WHERE bc.TrangThai IN (@DaGuiStatus, @QuaHanStatus, @DaKhoaStatus, @DaDuyetStatus)
)
SELECT ky.KyBaoCaoId, ky.TenKyBaoCao, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay, ky.HanNop, ky.TrangThai,
       COUNT(es.ChiSoChatLuongId) AS TongBaoCao,
       ISNULL(SUM(CASE WHEN bc.KyBaoCaoId IS NOT NULL THEN 1 ELSE 0 END), 0) AS DaGui
FROM dbo.KyBaoCao ky
LEFT JOIN ExpectedSlots es ON es.KyBaoCaoId = ky.KyBaoCaoId
LEFT JOIN CompletedSlots bc
    ON bc.KyBaoCaoId = es.KyBaoCaoId
   AND bc.KhoaPhongId = es.KhoaPhongId
   AND bc.ChiSoChatLuongId = es.ChiSoChatLuongId
GROUP BY ky.KyBaoCaoId, ky.TenKyBaoCao, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay, ky.HanNop, ky.TrangThai
ORDER BY ky.TuNgay DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            return Query(sql, MapPeriod,
                SubmittedStatusParams()
                    .Concat(new[]
                    {
                        Param("@Offset", (page - 1) * pageSize),
                        Param("@PageSize", pageSize)
                    })
                    .ToArray());
        }

        // Xử lý chức năng kỳ báo cáo của method GetFrequenciesForDepartment, giữ logic nghiệp vụ tập trung trong tầng phù hợp.
        public IList<TanSuatBaoCao> GetFrequenciesForDepartment(int departmentId)
        {
            const string sql = @"
SELECT DISTINCT tsb.TanSuatBaoCao
FROM dbo.PhanCongChiSo pc
INNER JOIN dbo.ChiSoTanSuatBaoCao tsb ON tsb.ChiSoChatLuongId = pc.ChiSoChatLuongId
WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1";

            return Query(sql, r => (TanSuatBaoCao)r.GetByte(0), Param("@KhoaPhongId", departmentId));
        }

        // Dựng danh sách lựa chọn kỳ báo cáo cho dropdown, chỉ gồm các bản ghi phù hợp với trạng thái sử dụng.
        public IList<SelectListItem> GetOptions()
        {
            return DropdownCache.GetOrAdd("dropdown:periods", () => Query(@"
SELECT KyBaoCaoId, TenKyBaoCao
FROM dbo.KyBaoCao
ORDER BY TuNgay DESC",
                r => new SelectListItem
                {
                    Value = Int(r, "KyBaoCaoId").ToString(),
                    Text = String(r, "TenKyBaoCao")
                }).ToList());
        }

        // Xác định điều kiện nghiệp vụ của kỳ báo cáo để controller/service chọn nhánh xử lý an toàn.
        public bool IsOpenForDepartment(int periodId, int departmentId)
        {
            var count = Convert.ToInt32(Scalar(@"
SELECT COUNT(DISTINCT ky.KyBaoCaoId)
FROM dbo.KyBaoCao ky
INNER JOIN dbo.PhanCongChiSo pc ON pc.KhoaPhongId=@KhoaPhongId AND pc.DangHoatDong=1
INNER JOIN dbo.ChiSoTanSuatBaoCao cst ON cst.ChiSoChatLuongId = pc.ChiSoChatLuongId AND cst.TanSuatBaoCao = ky.LoaiKyBaoCao
WHERE ky.KyBaoCaoId=@KyBaoCaoId
  AND ky.TrangThai=@Mo
  AND dbo.fn_ChiSoDuocTrienKhaiTrongKy(pc.ChiSoChatLuongId, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay) = 1",
                Param("@KyBaoCaoId", periodId),
                Param("@KhoaPhongId", departmentId),
                Param("@Mo", (byte)TrangThaiKyBaoCao.Mo)));

            return count > 0;
        }

        // Lấy một bản ghi kỳ báo cáo theo khóa chính; trả null khi không tìm thấy để tầng gọi xử lý 404/empty state.
        public KyBaoCaoViewModel Get(int id)
        {
            return QuerySingle(@"SELECT KyBaoCaoId, TenKyBaoCao, LoaiKyBaoCao, TuNgay, DenNgay, HanNop, TrangThai, 0 AS TongBaoCao, 0 AS DaGui FROM dbo.KyBaoCao WHERE KyBaoCaoId=@Id",
                MapPeriod, Param("@Id", id));
        }

        // Lưu kỳ báo cáo theo model/dto đã validate, bao gồm cả nhánh thêm mới và cập nhật.
        public ReportingPeriodDetailsViewModel GetDetails(int id)
        {
            var period = Get(id);
            if (period == null)
            {
                return null;
            }

            const string sql = @"
SELECT ky.KyBaoCaoId, ky.TenKyBaoCao, ky.HanNop, ky.TrangThai AS TrangThaiKyBaoCao,
       pc.KhoaPhongId, pc.ChiSoChatLuongId, kp.TenKhoaPhong, cs.MaChiSo, cs.TenChiSo,
       bc.BaoCaoId, bc.TrangThai AS TrangThaiBaoCao, ct.KetQua, ct.DatMucTieu,
       CASE WHEN bc.TrangThai IN (@DaGuiStatus, @QuaHanStatus, @DaKhoaStatus, @DaDuyetStatus) THEN 1 ELSE 0 END AS IsSubmitted
FROM dbo.KyBaoCao ky
INNER JOIN dbo.PhanCongChiSo pc ON pc.DangHoatDong = 1
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = pc.KhoaPhongId
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = pc.ChiSoChatLuongId
INNER JOIN dbo.ChiSoTanSuatBaoCao cst
    ON cst.ChiSoChatLuongId = pc.ChiSoChatLuongId
   AND cst.TanSuatBaoCao = ky.LoaiKyBaoCao
LEFT JOIN dbo.BaoCao bc
    ON bc.KyBaoCaoId = ky.KyBaoCaoId
   AND bc.KhoaPhongId = pc.KhoaPhongId
   AND bc.ChiSoChatLuongId = pc.ChiSoChatLuongId
LEFT JOIN dbo.BaoCaoChiTiet ct ON ct.BaoCaoId = bc.BaoCaoId
WHERE ky.KyBaoCaoId = @KyBaoCaoId
  AND dbo.fn_ChiSoDuocTrienKhaiTrongKy(pc.ChiSoChatLuongId, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay) = 1
ORDER BY kp.TenKhoaPhong, cs.MaChiSo";

            var items = Query(sql, reader => new ReportingPeriodIndicatorViewModel
            {
                KyBaoCaoId = Int(reader, "KyBaoCaoId"),
                KhoaPhongId = Int(reader, "KhoaPhongId"),
                ChiSoChatLuongId = Int(reader, "ChiSoChatLuongId"),
                BaoCaoId = NullableInt(reader, "BaoCaoId"),
                TenKyBaoCao = String(reader, "TenKyBaoCao"),
                HanNop = reader.GetDateTime(reader.GetOrdinal("HanNop")),
                TenKhoaPhong = String(reader, "TenKhoaPhong"),
                MaChiSo = String(reader, "MaChiSo"),
                TenChiSo = String(reader, "TenChiSo"),
                TrangThaiKyBaoCao = (TrangThaiKyBaoCao)reader.GetByte(reader.GetOrdinal("TrangThaiKyBaoCao")),
                TrangThaiBaoCao = reader.IsDBNull(reader.GetOrdinal("TrangThaiBaoCao"))
                    ? (TrangThaiBaoCao?)null
                    : (TrangThaiBaoCao)reader.GetByte(reader.GetOrdinal("TrangThaiBaoCao")),
                KetQua = NullableDecimal(reader, "KetQua"),
                DatMucTieu = reader.IsDBNull(reader.GetOrdinal("DatMucTieu"))
                    ? (bool?)null
                    : reader.GetBoolean(reader.GetOrdinal("DatMucTieu")),
                IsSubmitted = Int(reader, "IsSubmitted") == 1
            }, SubmittedStatusParams()
                .Concat(new[] { Param("@KyBaoCaoId", id) })
                .ToArray());

            return new ReportingPeriodDetailsViewModel
            {
                KyBaoCaoId = period.KyBaoCaoId,
                TenKyBaoCao = period.TenKyBaoCao,
                LoaiKyBaoCao = period.LoaiKyBaoCao,
                TuNgay = period.TuNgay,
                DenNgay = period.DenNgay,
                HanNop = period.HanNop,
                TrangThai = period.TrangThai,
                TongCanNop = items.Count,
                DaNop = items.Count(x => x.IsSubmitted),
                Items = items
            };
        }

        public void Save(ReportingPeriodSaveDto dto)
        {
            Save(new KyBaoCaoViewModel
            {
                KyBaoCaoId = dto.KyBaoCaoId,
                TenKyBaoCao = dto.TenKyBaoCao,
                LoaiKyBaoCao = dto.LoaiKyBaoCao,
                TuNgay = dto.TuNgay,
                DenNgay = dto.DenNgay,
                HanNop = dto.HanNop,
                TrangThai = dto.TrangThai
            });
        }

        // Lưu kỳ báo cáo theo model/dto đã validate, bao gồm cả nhánh thêm mới và cập nhật.
        public void Save(KyBaoCaoViewModel model)
        {
            if (model.TuNgay > model.DenNgay || model.HanNop < model.DenNgay)
            {
                throw new InvalidOperationException("Ngày báo cáo và hạn nộp không hợp lệ.");
            }

            if (model.KyBaoCaoId == 0)
            {
                Execute(@"INSERT INTO dbo.KyBaoCao(TenKyBaoCao, LoaiKyBaoCao, TuNgay, DenNgay, HanNop, TrangThai)
VALUES(@TenKyBaoCao, @LoaiKyBaoCao, @TuNgay, @DenNgay, @HanNop, @TrangThai)",
                    PeriodParams(model));
                DropdownCache.Remove("dropdown:periods");
                return;
            }

            var parameters = PeriodParams(model).Concat(new[] { Param("@KyBaoCaoId", model.KyBaoCaoId) }).ToArray();
            Execute(@"UPDATE dbo.KyBaoCao SET TenKyBaoCao=@TenKyBaoCao, LoaiKyBaoCao=@LoaiKyBaoCao, TuNgay=@TuNgay, DenNgay=@DenNgay,
HanNop=@HanNop, TrangThai=@TrangThai, NgayCapNhat=GETDATE() WHERE KyBaoCaoId=@KyBaoCaoId", parameters);
            DropdownCache.Remove("dropdown:periods");
        }

        // Xử lý chức năng kỳ báo cáo của method SetStatus, giữ logic nghiệp vụ tập trung trong tầng phù hợp.
        public void SetStatus(int id, TrangThaiKyBaoCao status)
        {
            Execute("UPDATE dbo.KyBaoCao SET TrangThai=@TrangThai, NgayCapNhat=GETDATE() WHERE KyBaoCaoId=@Id", Param("@TrangThai", (byte)status), Param("@Id", id));
            DropdownCache.Remove("dropdown:periods");
        }

        // Xóa bản ghi được chọn sau khi áp dụng các ràng buộc của kỳ báo cáo.
        public void Delete(int id)
        {
            var dependentCount = Convert.ToInt32(Scalar(@"
SELECT
    (SELECT COUNT(*) FROM dbo.BaoCao WHERE KyBaoCaoId=@Id) +
    (SELECT COUNT(*) FROM dbo.ThongBao WHERE KyBaoCaoId=@Id)",
                Param("@Id", id)));
            if (dependentCount > 0)
            {
                throw new InvalidOperationException("Kỳ báo cáo đã có dữ liệu liên quan, vui lòng khóa thay vì xóa.");
            }

            Execute("DELETE FROM dbo.KyBaoCao WHERE KyBaoCaoId=@Id", Param("@Id", id));
            DropdownCache.Remove("dropdown:periods");
        }

        // Tạo tập tham số SQL từ model để dùng cho thao tác ghi dữ liệu.
        private static SqlParameter[] PeriodParams(KyBaoCaoViewModel model)
        {
            return new[]
            {
                Param("@TenKyBaoCao", model.TenKyBaoCao),
                Param("@LoaiKyBaoCao", (byte)model.LoaiKyBaoCao),
                Param("@TuNgay", model.TuNgay),
                Param("@DenNgay", model.DenNgay),
                Param("@HanNop", model.HanNop),
                Param("@TrangThai", (byte)model.TrangThai)
            };
        }

        // Chuyển một dòng dữ liệu từ SqlDataReader sang view model/dto kỳ báo cáo đúng kiểu và tên trường.
        private static SqlParameter[] SubmittedStatusParams()
        {
            return new[]
            {
                Param("@DaGuiStatus", (byte)TrangThaiBaoCao.DaGui),
                Param("@QuaHanStatus", (byte)TrangThaiBaoCao.QuaHan),
                Param("@DaKhoaStatus", (byte)TrangThaiBaoCao.DaKhoa),
                Param("@DaDuyetStatus", (byte)TrangThaiBaoCao.DaDuyet)
            };
        }

        private static KyBaoCaoViewModel MapPeriod(SqlDataReader reader)
        {
            return new KyBaoCaoViewModel
            {
                KyBaoCaoId = Int(reader, "KyBaoCaoId"),
                TenKyBaoCao = String(reader, "TenKyBaoCao"),
                LoaiKyBaoCao = (TanSuatBaoCao)reader.GetByte(reader.GetOrdinal("LoaiKyBaoCao")),
                TuNgay = reader.GetDateTime(reader.GetOrdinal("TuNgay")),
                DenNgay = reader.GetDateTime(reader.GetOrdinal("DenNgay")),
                HanNop = reader.GetDateTime(reader.GetOrdinal("HanNop")),
                TrangThai = (TrangThaiKyBaoCao)reader.GetByte(reader.GetOrdinal("TrangThai")),
                TongBaoCao = Int(reader, "TongBaoCao"),
                DaGui = Int(reader, "DaGui")
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
            if (pageSize < 1) return 20;
            return pageSize > 100 ? 100 : pageSize;
        }
    }
}
