using HospitalQualityDashboardDemo.Models.DTOs;
using HospitalQualityDashboardDemo.Models.Enums;
using HospitalQualityDashboardDemo.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Services
{
    public class DepartmentService : DbServiceBase
    {
        private readonly ExcelImportExportService _excel = new ExcelImportExportService();

        // Truy vấn danh mục khoa/phòng theo điều kiện được cung cấp.
        public IList<KhoaPhongViewModel> GetAll(string search = null, bool includeInactive = true)
        {
            const string sql = @"
SELECT KhoaPhongId, IdKhoaPhongNguon, TenKhoaPhong, Used, GhiChu
FROM dbo.KhoaPhong
WHERE (@Search IS NULL OR TenKhoaPhong LIKE @SearchLike OR CONVERT(NVARCHAR(20), IdKhoaPhongNguon) = @Search)
  AND (@IncludeInactive = 1 OR Used = 1)
ORDER BY IdKhoaPhongNguon, TenKhoaPhong";
            return Query(sql, MapDepartment,
                Param("@Search", string.IsNullOrWhiteSpace(search) ? null : search),
                Param("@SearchLike", string.IsNullOrWhiteSpace(search) ? null : "%" + search + "%"),
                Param("@IncludeInactive", includeInactive));
        }

        // Truy vấn danh mục khoa/phòng theo điều kiện được cung cấp.
        public KhoaPhongViewModel Get(int id)
        {
            return QuerySingle("SELECT KhoaPhongId, IdKhoaPhongNguon, TenKhoaPhong, Used, GhiChu FROM dbo.KhoaPhong WHERE KhoaPhongId = @Id",
                MapDepartment, Param("@Id", id));
        }

        // Truy vấn danh mục khoa/phòng theo điều kiện được cung cấp.
        public IList<SelectListItem> GetOptions()
        {
            return DropdownCache.GetOrAdd("dropdown:departments", () => Query(@"
SELECT KhoaPhongId, TenKhoaPhong
FROM dbo.KhoaPhong
WHERE Used = 1
ORDER BY TenKhoaPhong",
                r => new SelectListItem
                {
                    Value = Int(r, "KhoaPhongId").ToString(),
                    Text = String(r, "TenKhoaPhong")
                }).ToList());
        }

        // Kiểm tra và cập nhật dữ liệu của danh mục khoa/phòng.
        public void Save(DepartmentSaveDto dto)
        {
            Save(new KhoaPhongViewModel
            {
                KhoaPhongId = dto.KhoaPhongId,
                IdKhoaPhongNguon = dto.IdKhoaPhongNguon,
                TenKhoaPhong = dto.TenKhoaPhong,
                Used = dto.Used,
                GhiChu = dto.GhiChu
            });
        }

        // Kiểm tra và cập nhật dữ liệu của danh mục khoa/phòng.
        public void Save(KhoaPhongViewModel model)
        {
            if (model.KhoaPhongId == 0)
            {
                Execute(@"INSERT INTO dbo.KhoaPhong(IdKhoaPhongNguon, TenKhoaPhong, Used, GhiChu)
VALUES(@IdKhoaPhongNguon, @TenKhoaPhong, @Used, @GhiChu)",
                    Param("@IdKhoaPhongNguon", model.IdKhoaPhongNguon),
                    Param("@TenKhoaPhong", model.TenKhoaPhong),
                    Param("@Used", model.Used),
                    Param("@GhiChu", model.GhiChu));
                DropdownCache.Remove("dropdown:departments");
                return;
            }

            Execute(@"UPDATE dbo.KhoaPhong
SET IdKhoaPhongNguon = @IdKhoaPhongNguon, TenKhoaPhong = @TenKhoaPhong, Used = @Used, GhiChu = @GhiChu, NgayCapNhat = GETDATE()
WHERE KhoaPhongId = @KhoaPhongId",
                Param("@KhoaPhongId", model.KhoaPhongId),
                Param("@IdKhoaPhongNguon", model.IdKhoaPhongNguon),
                Param("@TenKhoaPhong", model.TenKhoaPhong),
                Param("@Used", model.Used),
                Param("@GhiChu", model.GhiChu));
            DropdownCache.Remove("dropdown:departments");
        }

        // Kiểm tra và cập nhật dữ liệu của danh mục khoa/phòng.
        public void SetUsed(int id, bool used)
        {
            Execute("UPDATE dbo.KhoaPhong SET Used = @Used, NgayCapNhat = GETDATE() WHERE KhoaPhongId = @Id", Param("@Used", used), Param("@Id", id));
            DropdownCache.Remove("dropdown:departments");
        }

        // Xóa bản ghi được chọn sau khi áp dụng các ràng buộc của danh mục khoa/phòng.
        public void Delete(int id)
        {
            var dependentCount = Convert.ToInt32(Scalar(@"
SELECT
    (SELECT COUNT(*) FROM dbo.NhanVien WHERE KhoaPhongId=@Id) +
    (SELECT COUNT(*) FROM dbo.TaiKhoan WHERE KhoaPhongId=@Id) +
    (SELECT COUNT(*) FROM dbo.PhanCongChiSo WHERE KhoaPhongId=@Id) +
    (SELECT COUNT(*) FROM dbo.BaoCao WHERE KhoaPhongId=@Id)",
                Param("@Id", id)));
            if (dependentCount > 0)
            {
                throw new InvalidOperationException("Khoa/phòng đã có dữ liệu liên quan, vui lòng khóa thay vì xóa.");
            }

            Execute("DELETE FROM dbo.KhoaPhong WHERE KhoaPhongId=@Id", Param("@Id", id));
            DropdownCache.Remove("dropdown:departments");
        }

        // Đọc, kiểm tra và nhập dữ liệu từ tệp tải lên.
        public ImportResultViewModel Import(HttpPostedFileBase file, int userId)
        {
            var rows = _excel.ReadWorksheet(file);
            var result = new ImportResultViewModel { TongSoDong = rows.Count };
            var seen = new HashSet<int>();

            ExecuteInTransaction((conn, trans) =>
            {
                for (var i = 0; i < rows.Count; i++)
                {
                    var rowNumber = i + 2;
                    var row = rows[i];
                    string idText;
                    string name;
                    string usedText;
                    if (!row.TryGetValue("IDKHOAPHONG", out idText) || !row.TryGetValue("TENKHOAPHONG", out name) || !row.TryGetValue("USED", out usedText) || !row.ContainsKey("ID"))
                    {
                        result.Errors.Add("Dong " + rowNumber + ": File phai co dung cac cot ID, IDKHOAPHONG, TENKHOAPHONG, USED.");
                        result.SoDongLoi++;
                        continue;
                    }

                    int sourceId;
                    int usedNumber;
                    if (!int.TryParse(idText, out sourceId) || sourceId <= 0)
                    {
                        result.Errors.Add("Dong " + rowNumber + ": IDKHOAPHONG phai la so nguyen duong.");
                        result.SoDongLoi++;
                        continue;
                    }

                    if (!seen.Add(sourceId))
                    {
                        result.Errors.Add("Dong " + rowNumber + ": IDKHOAPHONG bi trung trong file.");
                        result.SoDongLoi++;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        result.Errors.Add("Dong " + rowNumber + ": TENKHOAPHONG khong duoc trong.");
                        result.SoDongLoi++;
                        continue;
                    }

                    if (!int.TryParse(usedText, out usedNumber) || (usedNumber != 0 && usedNumber != 1))
                    {
                        result.Errors.Add("Dong " + rowNumber + ": USED chi duoc la 0 hoac 1.");
                        result.SoDongLoi++;
                        continue;
                    }

                    Execute(conn, trans, @"
IF EXISTS (SELECT 1 FROM dbo.KhoaPhong WHERE IdKhoaPhongNguon = @IdKhoaPhongNguon)
    UPDATE dbo.KhoaPhong SET TenKhoaPhong = @TenKhoaPhong, Used = @Used, NgayCapNhat = GETDATE() WHERE IdKhoaPhongNguon = @IdKhoaPhongNguon
ELSE
    INSERT INTO dbo.KhoaPhong(IdKhoaPhongNguon, TenKhoaPhong, Used) VALUES(@IdKhoaPhongNguon, @TenKhoaPhong, @Used)",
                        Param("@IdKhoaPhongNguon", sourceId),
                        Param("@TenKhoaPhong", name.Trim()),
                        Param("@Used", usedNumber == 1));
                    result.SoDongThanhCong++;
                }

                Execute(conn, trans, @"INSERT INTO dbo.LichSuImport(LoaiImport, TenFile, TongSoDong, SoDongThanhCong, SoDongLoi, NguoiImportId)
VALUES(@LoaiImport, @TenFile, @TongSoDong, @SoDongThanhCong, @SoDongLoi, @NguoiImportId)",
                    Param("@LoaiImport", (byte)LoaiImport.KhoaPhong),
                    Param("@TenFile", file == null ? null : file.FileName),
                    Param("@TongSoDong", result.TongSoDong),
                    Param("@SoDongThanhCong", result.SoDongThanhCong),
                    Param("@SoDongLoi", result.SoDongLoi),
                    Param("@NguoiImportId", userId));
            });

            DropdownCache.Remove("dropdown:departments");
            return result;
        }

        // Chuyển dữ liệu nguồn sang cấu trúc dùng cho danh mục khoa/phòng.
        private static KhoaPhongViewModel MapDepartment(SqlDataReader reader)
        {
            return new KhoaPhongViewModel
            {
                KhoaPhongId = Int(reader, "KhoaPhongId"),
                IdKhoaPhongNguon = Int(reader, "IdKhoaPhongNguon"),
                TenKhoaPhong = String(reader, "TenKhoaPhong"),
                Used = reader.GetBoolean(reader.GetOrdinal("Used")),
                GhiChu = String(reader, "GhiChu")
            };
        }
    }
}
