using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HospitalQualityDashboard.Models.Enums;
using HospitalQualityDashboard.Models.ViewModels;

namespace HospitalQualityDashboard.Services
{
    public class DepartmentService : DbServiceBase
    {
        private readonly ExcelImportExportService _excel = new ExcelImportExportService();

        public IList<KhoaPhongViewModel> GetAll(string search = null, bool includeInactive = true)
        {
            const string sql = @"
SELECT KhoaPhongId, IdKhoaPhongNguon, TenKhoaPhong, Used, GhiChu
FROM dbo.KhoaPhong
WHERE (@Search IS NULL OR TenKhoaPhong LIKE @SearchLike OR CONVERT(NVARCHAR(20), IdKhoaPhongNguon) = @Search)
  AND (@IncludeInactive = 1 OR Used = 1)
ORDER BY TenKhoaPhong";
            return Query(sql, MapDepartment,
                Param("@Search", string.IsNullOrWhiteSpace(search) ? null : search),
                Param("@SearchLike", string.IsNullOrWhiteSpace(search) ? null : "%" + search + "%"),
                Param("@IncludeInactive", includeInactive));
        }

        public KhoaPhongViewModel Get(int id)
        {
            return QuerySingle("SELECT KhoaPhongId, IdKhoaPhongNguon, TenKhoaPhong, Used, GhiChu FROM dbo.KhoaPhong WHERE KhoaPhongId = @Id",
                MapDepartment, Param("@Id", id));
        }

        public IList<SelectListItem> GetOptions()
        {
            return GetAll(null, false)
                .Select(x => new SelectListItem { Value = x.KhoaPhongId.ToString(), Text = x.TenKhoaPhong })
                .ToList();
        }

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
        }

        public void SetUsed(int id, bool used)
        {
            Execute("UPDATE dbo.KhoaPhong SET Used = @Used, NgayCapNhat = GETDATE() WHERE KhoaPhongId = @Id", Param("@Used", used), Param("@Id", id));
        }

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
                throw new InvalidOperationException("Khoa/phong da co du lieu lien quan, vui long khoa thay vi xoa.");
            }

            Execute("DELETE FROM dbo.KhoaPhong WHERE KhoaPhongId=@Id", Param("@Id", id));
        }

        public ImportResultViewModel Import(HttpPostedFileBase file, int userId)
        {
            var rows = _excel.ReadWorksheet(file);
            var result = new ImportResultViewModel { TongSoDong = rows.Count };
            var seen = new HashSet<int>();

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

                Execute(@"
IF EXISTS (SELECT 1 FROM dbo.KhoaPhong WHERE IdKhoaPhongNguon = @IdKhoaPhongNguon)
    UPDATE dbo.KhoaPhong SET TenKhoaPhong = @TenKhoaPhong, Used = @Used, NgayCapNhat = GETDATE() WHERE IdKhoaPhongNguon = @IdKhoaPhongNguon
ELSE
    INSERT INTO dbo.KhoaPhong(IdKhoaPhongNguon, TenKhoaPhong, Used) VALUES(@IdKhoaPhongNguon, @TenKhoaPhong, @Used)",
                    Param("@IdKhoaPhongNguon", sourceId),
                    Param("@TenKhoaPhong", name.Trim()),
                    Param("@Used", usedNumber == 1));
                result.SoDongThanhCong++;
            }

            Execute(@"INSERT INTO dbo.LichSuImport(LoaiImport, TenFile, TongSoDong, SoDongThanhCong, SoDongLoi, NguoiImportId)
VALUES(@LoaiImport, @TenFile, @TongSoDong, @SoDongThanhCong, @SoDongLoi, @NguoiImportId)",
                Param("@LoaiImport", (byte)LoaiImport.KhoaPhong),
                Param("@TenFile", file == null ? null : file.FileName),
                Param("@TongSoDong", result.TongSoDong),
                Param("@SoDongThanhCong", result.SoDongThanhCong),
                Param("@SoDongLoi", result.SoDongLoi),
                Param("@NguoiImportId", userId));

            return result;
        }

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

    public class EmployeeService : DbServiceBase
    {
        private readonly ExcelImportExportService _excel = new ExcelImportExportService();

        public IList<NhanVienViewModel> GetAll(int? khoaPhongId)
        {
            const string sql = @"
SELECT nv.NhanVienId, nv.MaNhanVien, nv.HoTen, nv.NgaySinh, nv.GioiTinh, nv.ChucVu, nv.Email, nv.SoDienThoai,
       nv.KhoaPhongId, kp.TenKhoaPhong, nv.DangHoatDong,
       CAST(CASE WHEN EXISTS (
           SELECT 1
           FROM dbo.TaiKhoan tk
           WHERE tk.NhanVienId = nv.NhanVienId OR tk.TenDangNhap = nv.MaNhanVien
       ) THEN 1 ELSE 0 END AS BIT) AS HasAccount
FROM dbo.NhanVien nv
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = nv.KhoaPhongId
WHERE (@KhoaPhongId IS NULL OR nv.KhoaPhongId = @KhoaPhongId)
ORDER BY kp.TenKhoaPhong, nv.HoTen";
            return Query(sql, MapEmployee, Param("@KhoaPhongId", khoaPhongId));
        }

        public NhanVienViewModel Get(int id)
        {
            const string sql = @"
SELECT nv.NhanVienId, nv.MaNhanVien, nv.HoTen, nv.NgaySinh, nv.GioiTinh, nv.ChucVu, nv.Email, nv.SoDienThoai,
       nv.KhoaPhongId, kp.TenKhoaPhong, nv.DangHoatDong,
       CAST(CASE WHEN EXISTS (
           SELECT 1
           FROM dbo.TaiKhoan tk
           WHERE tk.NhanVienId = nv.NhanVienId OR tk.TenDangNhap = nv.MaNhanVien
       ) THEN 1 ELSE 0 END AS BIT) AS HasAccount
FROM dbo.NhanVien nv
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = nv.KhoaPhongId
WHERE nv.NhanVienId = @Id";
            return QuerySingle(sql, MapEmployee, Param("@Id", id));
        }

        public void Save(NhanVienViewModel model)
        {
            if (model.NhanVienId == 0)
            {
                Execute(@"INSERT INTO dbo.NhanVien(MaNhanVien, HoTen, NgaySinh, GioiTinh, ChucVu, Email, SoDienThoai, KhoaPhongId, DangHoatDong)
VALUES(@MaNhanVien, @HoTen, @NgaySinh, @GioiTinh, @ChucVu, @Email, @SoDienThoai, @KhoaPhongId, @DangHoatDong)",
                    EmployeeParams(model));
                return;
            }

            var parameters = EmployeeParams(model).Concat(new[] { Param("@NhanVienId", model.NhanVienId) }).ToArray();
            Execute(@"UPDATE dbo.NhanVien SET MaNhanVien=@MaNhanVien, HoTen=@HoTen, NgaySinh=@NgaySinh, GioiTinh=@GioiTinh,
ChucVu=@ChucVu, Email=@Email, SoDienThoai=@SoDienThoai, KhoaPhongId=@KhoaPhongId, DangHoatDong=@DangHoatDong, NgayCapNhat=GETDATE()
WHERE NhanVienId=@NhanVienId", parameters);
        }

        public void SetActive(int id, bool active)
        {
            Execute("UPDATE dbo.NhanVien SET DangHoatDong = @Active, NgayCapNhat = GETDATE() WHERE NhanVienId = @Id", Param("@Active", active), Param("@Id", id));
        }

        public void Delete(int id)
        {
            var accountCount = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.TaiKhoan WHERE NhanVienId=@Id", Param("@Id", id)));
            if (accountCount > 0)
            {
                throw new InvalidOperationException("Nhan vien da co tai khoan, vui long khoa thay vi xoa.");
            }

            Execute("DELETE FROM dbo.NhanVien WHERE NhanVienId=@Id", Param("@Id", id));
        }

        public void CreateUserAccount(CreateUserAccountViewModel model)
        {
            var employee = Get(model.NhanVienId);
            Execute(@"INSERT INTO dbo.TaiKhoan(TenDangNhap, MatKhauHash, LoaiTaiKhoan, NhanVienId, KhoaPhongId, DangHoatDong)
VALUES(@TenDangNhap, @MatKhauHash, @LoaiTaiKhoan, @NhanVienId, @KhoaPhongId, 1)",
                Param("@TenDangNhap", model.TenDangNhap),
                Param("@MatKhauHash", PasswordHasher.Hash(model.MatKhau)),
                Param("@LoaiTaiKhoan", (byte)LoaiTaiKhoan.User),
                Param("@NhanVienId", model.NhanVienId),
                Param("@KhoaPhongId", employee.KhoaPhongId));
        }

        public bool IsUsernameExists(string username)
        {
            var count = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.TaiKhoan WHERE TenDangNhap = @TenDangNhap", Param("@TenDangNhap", username)));
            return count > 0;
        }

        public bool HasAccount(int nhanVienId)
        {
            var count = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.TaiKhoan WHERE NhanVienId = @NhanVienId", Param("@NhanVienId", nhanVienId)));
            return count > 0;
        }

        public ImportResultViewModel Import(HttpPostedFileBase file, int? selectedKhoaPhongId, int userId)
        {
            var rows = _excel.ReadWorksheet(file);
            var result = new ImportResultViewModel { TongSoDong = rows.Count };
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var rowNumber = i + 2;
                string value;
                var model = new NhanVienViewModel { DangHoatDong = true };
                if (row.TryGetValue("MaNhanVien", out value)) model.MaNhanVien = value;
                if (row.TryGetValue("HoTen", out value)) model.HoTen = value;
                if (row.TryGetValue("GioiTinh", out value)) model.GioiTinh = value;
                if (row.TryGetValue("ChucVu", out value)) model.ChucVu = value;
                if (row.TryGetValue("Email", out value)) model.Email = value;
                if (row.TryGetValue("SoDienThoai", out value)) model.SoDienThoai = value;

                if (string.IsNullOrWhiteSpace(model.MaNhanVien) || string.IsNullOrWhiteSpace(model.HoTen))
                {
                    result.Errors.Add("Dong " + rowNumber + ": MaNhanVien va HoTen la bat buoc.");
                    result.SoDongLoi++;
                    continue;
                }

                if (selectedKhoaPhongId.HasValue)
                {
                    model.KhoaPhongId = selectedKhoaPhongId.Value;
                }
                else
                {
                    string sourceIdText;
                    int sourceId;
                    if (!row.TryGetValue("IDKHOAPHONG", out sourceIdText) || !int.TryParse(sourceIdText, out sourceId))
                    {
                        result.Errors.Add("Dong " + rowNumber + ": thieu IDKHOAPHONG khi import toan vien.");
                        result.SoDongLoi++;
                        continue;
                    }

                    var khoaPhongId = Scalar("SELECT KhoaPhongId FROM dbo.KhoaPhong WHERE IdKhoaPhongNguon = @SourceId", Param("@SourceId", sourceId));
                    if (khoaPhongId == null)
                    {
                        result.Errors.Add("Dong " + rowNumber + ": khoa/phong khong ton tai.");
                        result.SoDongLoi++;
                        continue;
                    }

                    model.KhoaPhongId = Convert.ToInt32(khoaPhongId);
                }

                var nvId = Scalar(@"
IF EXISTS (SELECT 1 FROM dbo.NhanVien WHERE MaNhanVien = @MaNhanVien)
BEGIN
    UPDATE dbo.NhanVien SET HoTen=@HoTen, GioiTinh=@GioiTinh, ChucVu=@ChucVu, Email=@Email, SoDienThoai=@SoDienThoai, KhoaPhongId=@KhoaPhongId, NgayCapNhat=GETDATE() WHERE MaNhanVien=@MaNhanVien;
    SELECT NhanVienId FROM dbo.NhanVien WHERE MaNhanVien=@MaNhanVien;
END
ELSE
BEGIN
    INSERT INTO dbo.NhanVien(MaNhanVien, HoTen, GioiTinh, ChucVu, Email, SoDienThoai, KhoaPhongId, DangHoatDong)
    OUTPUT INSERTED.NhanVienId
    VALUES(@MaNhanVien, @HoTen, @GioiTinh, @ChucVu, @Email, @SoDienThoai, @KhoaPhongId, 1);
END", EmployeeParams(model));

                int employeeId = Convert.ToInt32(nvId);

                var hasAccount = Scalar("SELECT 1 FROM dbo.TaiKhoan WHERE NhanVienId=@EmployeeId OR TenDangNhap=@TenDangNhap",
                    Param("@EmployeeId", employeeId),
                    Param("@TenDangNhap", model.MaNhanVien));

                if (hasAccount == null)
                {
                    Execute(@"INSERT INTO dbo.TaiKhoan(TenDangNhap, MatKhauHash, LoaiTaiKhoan, NhanVienId, KhoaPhongId, DangHoatDong)
VALUES(@TenDangNhap, @MatKhauHash, @LoaiTaiKhoan, @NhanVienId, @KhoaPhongId, 1)",
                        Param("@TenDangNhap", model.MaNhanVien),
                        Param("@MatKhauHash", PasswordHasher.Hash(model.MaNhanVien)),
                        Param("@LoaiTaiKhoan", (byte)LoaiTaiKhoan.User),
                        Param("@NhanVienId", employeeId),
                        Param("@KhoaPhongId", model.KhoaPhongId));
                }

                result.SoDongThanhCong++;
            }

            Execute(@"INSERT INTO dbo.LichSuImport(LoaiImport, TenFile, TongSoDong, SoDongThanhCong, SoDongLoi, NguoiImportId)
VALUES(@LoaiImport, @TenFile, @TongSoDong, @SoDongThanhCong, @SoDongLoi, @NguoiImportId)",
                Param("@LoaiImport", (byte)LoaiImport.NhanVien),
                Param("@TenFile", file == null ? null : file.FileName),
                Param("@TongSoDong", result.TongSoDong),
                Param("@SoDongThanhCong", result.SoDongThanhCong),
                Param("@SoDongLoi", result.SoDongLoi),
                Param("@NguoiImportId", userId));

            return result;
        }

        private static SqlParameter[] EmployeeParams(NhanVienViewModel model)
        {
            return new[]
            {
                Param("@MaNhanVien", model.MaNhanVien),
                Param("@HoTen", model.HoTen),
                Param("@NgaySinh", model.NgaySinh),
                Param("@GioiTinh", model.GioiTinh),
                Param("@ChucVu", model.ChucVu),
                Param("@Email", model.Email),
                Param("@SoDienThoai", model.SoDienThoai),
                Param("@KhoaPhongId", model.KhoaPhongId),
                Param("@DangHoatDong", model.DangHoatDong)
            };
        }

        private static NhanVienViewModel MapEmployee(SqlDataReader reader)
        {
            return new NhanVienViewModel
            {
                NhanVienId = Int(reader, "NhanVienId"),
                MaNhanVien = String(reader, "MaNhanVien"),
                HoTen = String(reader, "HoTen"),
                NgaySinh = NullableDateTime(reader, "NgaySinh"),
                GioiTinh = String(reader, "GioiTinh"),
                ChucVu = String(reader, "ChucVu"),
                Email = String(reader, "Email"),
                SoDienThoai = String(reader, "SoDienThoai"),
                KhoaPhongId = Int(reader, "KhoaPhongId"),
                TenKhoaPhong = String(reader, "TenKhoaPhong"),
                DangHoatDong = reader.GetBoolean(reader.GetOrdinal("DangHoatDong")),
                HasAccount = reader.GetBoolean(reader.GetOrdinal("HasAccount"))
            };
        }
    }
}
