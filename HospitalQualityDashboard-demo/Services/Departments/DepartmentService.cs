// Mục đích: quản lý danh mục khoa/phòng và các ràng buộc khi cập nhật hoặc xóa.
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

        // Lấy danh sách danh mục khoa/phòng theo bộ lọc, trạng thái hoạt động và phạm vi quyền đang áp dụng.
        public IList<KhoaPhongViewModel> GetAll(string search = null, bool includeInactive = true)
        {
            return GetAll(search, includeInactive ? "all" : "active");
        }

        public IList<KhoaPhongViewModel> GetAll(string search, string statusFilter)
        {
            const string sql = @"
SELECT KhoaPhongId, IdKhoaPhongNguon, TenKhoaPhong, Used, GhiChu
FROM dbo.KhoaPhong
WHERE (@Search IS NULL OR TenKhoaPhong LIKE @SearchLike OR CONVERT(NVARCHAR(20), IdKhoaPhongNguon) = @Search)
  AND (@StatusFilter = N'all'
       OR (@StatusFilter = N'active' AND Used = 1)
       OR (@StatusFilter = N'locked' AND Used = 0))
ORDER BY KhoaPhongId DESC";
            return Query(sql, MapDepartment,
                Param("@Search", string.IsNullOrWhiteSpace(search) ? null : search),
                Param("@SearchLike", string.IsNullOrWhiteSpace(search) ? null : "%" + search + "%"),
                Param("@StatusFilter", NormalizeStatusFilter(statusFilter)));
        }

        // Lay danh sach khoa/phong theo trang de man hinh quan ly khong tai qua nhieu dong mot luc.
        public IList<KhoaPhongViewModel> GetAll(string search, int page, int pageSize, out int totalItems, bool includeInactive = true)
        {
            return GetAll(search, includeInactive ? "all" : "active", page, pageSize, out totalItems);
        }

        public IList<KhoaPhongViewModel> GetAll(string search, string statusFilter, int page, int pageSize, out int totalItems)
        {
            page = NormalizePage(page);
            pageSize = NormalizePageSize(pageSize);
            var normalizedStatusFilter = NormalizeStatusFilter(statusFilter);
            totalItems = Convert.ToInt32(Scalar(@"
SELECT COUNT(*)
FROM dbo.KhoaPhong
WHERE (@Search IS NULL OR TenKhoaPhong LIKE @SearchLike OR CONVERT(NVARCHAR(20), IdKhoaPhongNguon) = @Search)
  AND (@StatusFilter = N'all'
       OR (@StatusFilter = N'active' AND Used = 1)
       OR (@StatusFilter = N'locked' AND Used = 0))",
                Param("@Search", string.IsNullOrWhiteSpace(search) ? null : search),
                Param("@SearchLike", string.IsNullOrWhiteSpace(search) ? null : "%" + search + "%"),
                Param("@StatusFilter", normalizedStatusFilter)));

            const string sql = @"
SELECT KhoaPhongId, IdKhoaPhongNguon, TenKhoaPhong, Used, GhiChu
FROM dbo.KhoaPhong
WHERE (@Search IS NULL OR TenKhoaPhong LIKE @SearchLike OR CONVERT(NVARCHAR(20), IdKhoaPhongNguon) = @Search)
  AND (@StatusFilter = N'all'
       OR (@StatusFilter = N'active' AND Used = 1)
       OR (@StatusFilter = N'locked' AND Used = 0))
ORDER BY KhoaPhongId DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            return Query(sql, MapDepartment,
                Param("@Search", string.IsNullOrWhiteSpace(search) ? null : search),
                Param("@SearchLike", string.IsNullOrWhiteSpace(search) ? null : "%" + search + "%"),
                Param("@StatusFilter", normalizedStatusFilter),
                Param("@Offset", (page - 1) * pageSize),
                Param("@PageSize", pageSize));
        }

        // Lay mot ban ghi danh muc khoa/phong theo khoa chinh.
        public KhoaPhongViewModel Get(int id)
        {
            return QuerySingle("SELECT KhoaPhongId, IdKhoaPhongNguon, TenKhoaPhong, Used, GhiChu FROM dbo.KhoaPhong WHERE KhoaPhongId = @Id",
                MapDepartment, Param("@Id", id));
        }

        public bool IsSourceIdExists(int sourceId, int excludingDepartmentId)
        {
            var count = Convert.ToInt32(Scalar(@"
SELECT COUNT(*)
FROM dbo.KhoaPhong
WHERE IdKhoaPhongNguon = @IdKhoaPhongNguon
  AND KhoaPhongId <> @KhoaPhongId",
                Param("@IdKhoaPhongNguon", sourceId),
                Param("@KhoaPhongId", excludingDepartmentId)));
            return count > 0;
        }

        // Dựng danh sách lựa chọn danh mục khoa/phòng cho dropdown, chỉ gồm các bản ghi phù hợp với trạng thái sử dụng.
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

        public IList<SelectListItem> GetStatusOptions(string selectedStatus)
        {
            selectedStatus = NormalizeStatusFilter(selectedStatus);
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "all", Text = "Tất cả", Selected = selectedStatus == "all" },
                new SelectListItem { Value = "active", Text = "Đang hoạt động", Selected = selectedStatus == "active" },
                new SelectListItem { Value = "locked", Text = "Đã khóa", Selected = selectedStatus == "locked" }
            };
        }

        public bool IsActiveDepartment(int departmentId)
        {
            var result = Scalar("SELECT Used FROM dbo.KhoaPhong WHERE KhoaPhongId=@KhoaPhongId", Param("@KhoaPhongId", departmentId));
            return result != null && result != DBNull.Value && Convert.ToBoolean(result);
        }

        public void RequireActiveDepartment(int departmentId)
        {
            if (!IsActiveDepartment(departmentId))
            {
                throw new InvalidOperationException("Khoa/phòng đã bị khóa, không thể phát sinh dữ liệu mới.");
            }
        }

        // Lưu danh mục khoa/phòng theo model/dto đã validate, bao gồm cả nhánh thêm mới và cập nhật.
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

        // Lưu danh mục khoa/phòng theo model/dto đã validate, bao gồm cả nhánh thêm mới và cập nhật.
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

        // Khóa hoặc mở sử dụng khoa/phòng nhưng giữ dữ liệu liên quan để lịch sử báo cáo không bị mất.
        public void SetUsed(int id, bool used)
        {
            Execute("UPDATE dbo.KhoaPhong SET Used = @Used, NgayCapNhat = GETDATE() WHERE KhoaPhongId = @Id", Param("@Used", used), Param("@Id", id));
            DropdownCache.Remove("dropdown:departments");
        }

        // Xóa bản ghi được chọn sau khi áp dụng các ràng buộc của danh mục khoa/phòng.
        public void Delete(int id)
        {
            try
            {
                if (HasDepartmentDependency("SELECT COUNT(*) FROM dbo.NhanVien WHERE KhoaPhongId=@Id", id))
                {
                    throw new InvalidOperationException("Không thể xóa vì khoa/phòng đang có nhân viên. Vui lòng khóa khoa/phòng thay vì xóa.");
                }

                if (HasDepartmentDependency("SELECT COUNT(*) FROM dbo.TaiKhoan WHERE KhoaPhongId=@Id", id))
                {
                    throw new InvalidOperationException("Không thể xóa vì khoa/phòng đang có tài khoản người dùng.");
                }

                if (HasDepartmentDependency("SELECT COUNT(*) FROM dbo.PhanCongChiSo WHERE KhoaPhongId=@Id", id))
                {
                    throw new InvalidOperationException("Không thể xóa vì khoa/phòng đang được phân công chỉ số. Vui lòng tạm dừng phân công hoặc khóa khoa/phòng.");
                }

                if (HasDepartmentDependency("SELECT COUNT(*) FROM dbo.BaoCao WHERE KhoaPhongId=@Id", id))
                {
                    throw new InvalidOperationException("Không thể xóa vì khoa/phòng đã phát sinh báo cáo. Vui lòng khóa khoa/phòng thay vì xóa.");
                }

                if (HasDepartmentDependency("SELECT COUNT(*) FROM dbo.ChiSoChatLuong WHERE KhoaPhongThuThapId=@Id OR KhoaPhongTongHopId=@Id", id))
                {
                    throw new InvalidOperationException("Không thể xóa vì khoa/phòng đang được gắn làm đơn vị thu thập/tổng hợp chỉ số.");
                }

                Execute("DELETE FROM dbo.KhoaPhong WHERE KhoaPhongId=@Id", Param("@Id", id));
                DropdownCache.Remove("dropdown:departments");
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Dữ liệu đã được sử dụng, không thể xóa.", ex);
            }
        }

        private bool HasDepartmentDependency(string sql, int id)
        {
            var count = Convert.ToInt32(Scalar(sql, Param("@Id", id)));
            return count > 0;
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

        // Chuan hoa so trang de tranh page am/0 lam sai truy van phan trang.
        private static int NormalizePage(int page)
        {
            return page < 1 ? 1 : page;
        }

        // Gioi han kich thuoc trang de tranh truy van qua lon hoac gia tri khong hop le.
        private static int NormalizePageSize(int pageSize)
        {
            if (pageSize < 1) return 10;
            return pageSize > 100 ? 100 : pageSize;
        }

        public static string NormalizeStatusFilter(string statusFilter)
        {
            if (string.Equals(statusFilter, "active", StringComparison.OrdinalIgnoreCase)) return "active";
            if (string.Equals(statusFilter, "locked", StringComparison.OrdinalIgnoreCase)) return "locked";
            return "all";
        }

        // Chuyen mot dong khoa/phong tu database sang view model quan tri danh muc.
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
