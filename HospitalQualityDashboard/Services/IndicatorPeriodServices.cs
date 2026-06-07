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
using HospitalQualityDashboard.Models.Enums;
using HospitalQualityDashboard.Models.ViewModels;


namespace HospitalQualityDashboard.Services
{
    public class IndicatorService : DbServiceBase
    {
        private readonly ExcelImportExportService _excel = new ExcelImportExportService();

        public IList<ChiSoViewModel> GetAll(bool includeInactive = true, int? filterKhoaPhongId = null)
        {
            string sql;
            if (filterKhoaPhongId.HasValue)
            {
                sql = @"
SELECT cs.ChiSoChatLuongId, cs.MaChiSo, cs.SoThuTu, cs.TenChiSo, cs.DinhNghia, cs.LinhVucApDung, cs.KhiaCanhChatLuong, cs.ThanhToChatLuong,
       cs.LyDoLuaChon, cs.PhuongPhapTinh, cs.TuSoMoTa, cs.MauSoMoTa, cs.NguonSoLieu, cs.ThuThapTongHop, cs.GiaTriSoLieu,
       cs.TanSuatBaoCao, cs.LoaiCongThuc, cs.DonViTinh, cs.DangHoatDong
FROM dbo.ChiSoChatLuong cs
INNER JOIN dbo.PhanCongChiSo pc ON pc.ChiSoChatLuongId = cs.ChiSoChatLuongId
WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1
  AND (@IncludeInactive = 1 OR cs.DangHoatDong = 1)
ORDER BY ISNULL(cs.SoThuTu, 9999), cs.MaChiSo";
            }
            else
            {
                sql = @"
SELECT ChiSoChatLuongId, MaChiSo, SoThuTu, TenChiSo, DinhNghia, LinhVucApDung, KhiaCanhChatLuong, ThanhToChatLuong,
       LyDoLuaChon, PhuongPhapTinh, TuSoMoTa, MauSoMoTa, NguonSoLieu, ThuThapTongHop, GiaTriSoLieu,
       TanSuatBaoCao, LoaiCongThuc, DonViTinh, DangHoatDong
FROM dbo.ChiSoChatLuong
WHERE (@IncludeInactive = 1 OR DangHoatDong = 1)
ORDER BY ISNULL(SoThuTu, 9999), MaChiSo";
            }

            var items = Query(sql, MapIndicator, 
                Param("@IncludeInactive", includeInactive),
                Param("@KhoaPhongId", filterKhoaPhongId));
            PopulateIndicatorFrequencies(items);
            return items;
        }

        public bool IsAssigned(int indicatorId, int khoaPhongId)
        {
            var count = Convert.ToInt32(Scalar(@"
SELECT COUNT(*) FROM dbo.PhanCongChiSo 
WHERE ChiSoChatLuongId = @IndicatorId AND KhoaPhongId = @KhoaPhongId AND DangHoatDong = 1",
                Param("@IndicatorId", indicatorId),
                Param("@KhoaPhongId", khoaPhongId)));
            return count > 0;
        }

        public IList<SelectListItem> GetOptions()
        {
            return GetAll(false).Select(x => new SelectListItem
            {
                Value = x.ChiSoChatLuongId.ToString(),
                Text = x.MaChiSo + " - " + x.TenChiSo
            }).ToList();
        }

        public ChiSoViewModel Get(int id)
        {
            var model = QuerySingle(@"SELECT ChiSoChatLuongId, MaChiSo, SoThuTu, TenChiSo, DinhNghia, LinhVucApDung, KhiaCanhChatLuong, ThanhToChatLuong,
LyDoLuaChon, PhuongPhapTinh, TuSoMoTa, MauSoMoTa, NguonSoLieu, ThuThapTongHop, GiaTriSoLieu, TanSuatBaoCao, LoaiCongThuc, DonViTinh, DangHoatDong
FROM dbo.ChiSoChatLuong WHERE ChiSoChatLuongId = @Id", MapIndicator, Param("@Id", id));
            if (model == null)
            {
                return null;
            }

            PopulateIndicatorFrequencies(new[] { model });

            var target = QuerySingle("SELECT TOP 1 Nam, ToanTuSoSanh, GiaTriMucTieu, MoTaMucTieu FROM dbo.ChiSoMucTieu WHERE ChiSoChatLuongId = @Id ORDER BY Nam DESC",
                r => new ChiSoViewModel
                {
                    NamMucTieu = Int(r, "Nam"),
                    ToanTuSoSanh = String(r, "ToanTuSoSanh"),
                    GiaTriMucTieu = NullableDecimal(r, "GiaTriMucTieu"),
                    MoTaMucTieu = String(r, "MoTaMucTieu")
                },
                Param("@Id", id));
            if (target != null)
            {
                model.NamMucTieu = target.NamMucTieu;
                model.ToanTuSoSanh = target.ToanTuSoSanh;
                model.GiaTriMucTieu = target.GiaTriMucTieu;
                model.MoTaMucTieu = target.MoTaMucTieu;
            }

            return model;
        }

        public void Save(ChiSoViewModel model)
        {
            ApplySelectedFrequencies(model);
            if (model.ChiSoChatLuongId == 0)
            {
                var id = Convert.ToInt32(Scalar(@"INSERT INTO dbo.ChiSoChatLuong(MaChiSo, SoThuTu, TenChiSo, DinhNghia, LinhVucApDung, KhiaCanhChatLuong,
ThanhToChatLuong, LyDoLuaChon, PhuongPhapTinh, TuSoMoTa, MauSoMoTa, NguonSoLieu, ThuThapTongHop, GiaTriSoLieu, TanSuatBaoCao, LoaiCongThuc, DonViTinh, DangHoatDong)
OUTPUT INSERTED.ChiSoChatLuongId
VALUES(@MaChiSo, @SoThuTu, @TenChiSo, @DinhNghia, @LinhVucApDung, @KhiaCanhChatLuong, @ThanhToChatLuong, @LyDoLuaChon,
@PhuongPhapTinh, @TuSoMoTa, @MauSoMoTa, @NguonSoLieu, @ThuThapTongHop, @GiaTriSoLieu, @TanSuatBaoCao, @LoaiCongThuc, @DonViTinh, @DangHoatDong)",
                    IndicatorParams(model)));
                model.ChiSoChatLuongId = id;
            }
            else
            {
                var parameters = IndicatorParams(model).Concat(new[] { Param("@ChiSoChatLuongId", model.ChiSoChatLuongId) }).ToArray();
                Execute(@"UPDATE dbo.ChiSoChatLuong SET MaChiSo=@MaChiSo, SoThuTu=@SoThuTu, TenChiSo=@TenChiSo, DinhNghia=@DinhNghia,
LinhVucApDung=@LinhVucApDung, KhiaCanhChatLuong=@KhiaCanhChatLuong, ThanhToChatLuong=@ThanhToChatLuong, LyDoLuaChon=@LyDoLuaChon,
PhuongPhapTinh=@PhuongPhapTinh, TuSoMoTa=@TuSoMoTa, MauSoMoTa=@MauSoMoTa, NguonSoLieu=@NguonSoLieu, ThuThapTongHop=@ThuThapTongHop,
GiaTriSoLieu=@GiaTriSoLieu, TanSuatBaoCao=@TanSuatBaoCao, LoaiCongThuc=@LoaiCongThuc, DonViTinh=@DonViTinh, DangHoatDong=@DangHoatDong, NgayCapNhat=GETDATE()
WHERE ChiSoChatLuongId=@ChiSoChatLuongId", parameters);
            }

            SaveTarget(model);
            SaveFrequencies(model.ChiSoChatLuongId, model.TanSuatBaoCaos);
        }

        public void SetActive(int id, bool active)
        {
            Execute("UPDATE dbo.ChiSoChatLuong SET DangHoatDong = @Active, NgayCapNhat = GETDATE() WHERE ChiSoChatLuongId = @Id", Param("@Active", active), Param("@Id", id));
        }

        public void Delete(int id)
        {
            var reportCount = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.BaoCao WHERE ChiSoChatLuongId=@Id", Param("@Id", id)));
            if (reportCount > 0)
            {
                throw new InvalidOperationException("Chi so da co bao cao, vui long khoa thay vi xoa.");
            }

            EnsureIndicatorFrequencyTable();
            Execute("DELETE FROM dbo.ChiSoTanSuatBaoCao WHERE ChiSoChatLuongId=@Id", Param("@Id", id));
            Execute("DELETE FROM dbo.PhanCongChiSo WHERE ChiSoChatLuongId=@Id", Param("@Id", id));
            Execute("DELETE FROM dbo.ChiSoMucTieu WHERE ChiSoChatLuongId=@Id", Param("@Id", id));
            Execute("DELETE FROM dbo.ChiSoChatLuong WHERE ChiSoChatLuongId=@Id", Param("@Id", id));
        }

        public ImportResultViewModel Import(HttpPostedFileBase file, int userId)
        {
            var rows = ReadIndicatorImportRows(file);
            var result = new ImportResultViewModel { TongSoDong = rows.Count };
            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var departments = GetDepartmentLookups();

            for (var i = 0; i < rows.Count; i++)
            {
                var rowNumber = i + 2;
                var row = rows[i];
                var model = BuildIndicatorFromRow(row);
                if (string.IsNullOrWhiteSpace(model.TenChiSo))
                {
                    result.Errors.Add("Dong " + rowNumber + ": TenChiSo la bat buoc.");
                    result.SoDongLoi++;
                    continue;
                }

                var duplicateKey = string.IsNullOrWhiteSpace(model.MaChiSo) ? NormalizeKey(model.TenChiSo) : model.MaChiSo.Trim();
                if (!seenKeys.Add(duplicateKey))
                {
                    result.Errors.Add("Dong " + rowNumber + ": chi so bi trung trong file.");
                    result.SoDongLoi++;
                    continue;
                }

                string frequencyText;
                if (!TryGetValue(row, out frequencyText, "TanSuatBaoCao", "TAN_SUAT_BAO_CAO", "Tan suat bao cao", "Tần suất báo cáo"))
                {
                    result.Errors.Add("Dong " + rowNumber + ": TanSuatBaoCao la bat buoc.");
                    result.SoDongLoi++;
                    continue;
                }

                IList<TanSuatBaoCao> frequencies;
                if (!TryParseFrequencies(frequencyText, out frequencies))
                {
                    result.Errors.Add("Dong " + rowNumber + ": TanSuatBaoCao khong hop le.");
                    result.SoDongLoi++;
                    continue;
                }

                model.TanSuatBaoCaos = frequencies;
                model.TanSuatBaoCao = frequencies.First();

                var departmentSource = GetDepartmentSource(row, model.ThuThapTongHop);
                var departmentIds = ResolveDepartmentIds(departmentSource, departments);

                try
                {
                    SaveImportedIndicator(model, departmentIds, userId);
                    result.SoDongThanhCong++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add("Dong " + rowNumber + ": " + ex.Message);
                    result.SoDongLoi++;
                }
            }

            Execute(@"INSERT INTO dbo.LichSuImport(LoaiImport, TenFile, TongSoDong, SoDongThanhCong, SoDongLoi, NguoiImportId)
VALUES(@LoaiImport, @TenFile, @TongSoDong, @SoDongThanhCong, @SoDongLoi, @NguoiImportId)",
                Param("@LoaiImport", (byte)LoaiImport.ChiSoChatLuong),
                Param("@TenFile", file == null ? null : file.FileName),
                Param("@TongSoDong", result.TongSoDong),
                Param("@SoDongThanhCong", result.SoDongThanhCong),
                Param("@SoDongLoi", result.SoDongLoi),
                Param("@NguoiImportId", userId));

            return result;
        }

        private IList<IDictionary<string, string>> ReadIndicatorImportRows(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
            {
                return _excel.ReadWorksheet(file);
            }

            var extension = Path.GetExtension(file.FileName);
            return string.Equals(extension, ".docx", StringComparison.OrdinalIgnoreCase)
                ? _excel.ReadIndicatorDocxTables(file)
                : _excel.ReadWorksheet(file);
        }

        private ChiSoViewModel BuildIndicatorFromRow(IDictionary<string, string> row)
        {
            var model = new ChiSoViewModel
            {
                DangHoatDong = ParseBool(GetValue(row, "DangHoatDong", "DANGHOATDONG", "USED"), true),
                LoaiCongThuc = ParseFormulaType(GetValue(row, "LoaiCongThuc", "LOAICONGTHUC", "Loai cong thuc")),
                DonViTinh = GetValue(row, "DonViTinh", "DONVITINH", "Don vi tinh")
            };

            model.MaChiSo = NullIfWhiteSpace(GetValue(row, "MaChiSo", "MACHISO", "Ma chi so", "Mã chỉ số"));
            model.SoThuTu = ParseNullableInt(GetValue(row, "SoThuTu", "SOTHUTU", "So thu tu", "Số thứ tự"));
            model.TenChiSo = GetValue(row, "TenChiSo", "TENCHISO", "Ten chi so", "Tên chỉ số");
            model.DinhNghia = GetValue(row, "DinhNghia", "DINHNGHIA", "Dinh nghia chi so", "Định nghĩa chỉ số");
            model.LinhVucApDung = GetValue(row, "LinhVucApDung", "LINHVUCAPDUNG", "Linh vuc ap dung", "Lĩnh vực áp dụng");
            model.KhiaCanhChatLuong = GetValue(row, "KhiaCanhChatLuong", "KHIACANHCHATLUONG", "Khia canh chat luong", "Khía cạnh chất lượng");
            model.ThanhToChatLuong = GetValue(row, "ThanhToChatLuong", "THANHTOCHATLUONG", "Thanh to chat luong", "Thành tố chất lượng");
            model.LyDoLuaChon = GetValue(row, "LyDoLuaChon", "LYDOLUACHON", "Ly do lua chon", "Lý do lựa chọn");
            model.PhuongPhapTinh = GetValue(row, "PhuongPhapTinh", "PHUONGPHAPTINH", "Phuong phap tinh", "Phương pháp tính");
            model.TuSoMoTa = GetValue(row, "TuSoMoTa", "TUSOMOTA", "Tu so", "Tử số");
            model.MauSoMoTa = GetValue(row, "MauSoMoTa", "MAUSOMOTA", "Mau so", "Mẫu số");
            model.NguonSoLieu = GetValue(row, "NguonSoLieu", "NGUONSOLIEU", "Nguon so lieu", "Nguồn số liệu");
            model.ThuThapTongHop = GetValue(row, "ThuThapTongHop", "THUTHAPTONGHOP", "Thu thap va tong hop so lieu", "Thu thập và tổng hợp số liệu");
            model.GiaTriSoLieu = GetValue(row, "GiaTriSoLieu", "GIATRISOLIEU", "Gia tri cua so lieu", "Giá trị của số liệu");

            var targetText = GetTargetText(row);
            model.NamMucTieu = ParseNullableInt(GetValue(row, "NamMucTieu", "NAMMUCTIEU", "Nam muc tieu"));
            if (!model.NamMucTieu.HasValue)
            {
                model.NamMucTieu = GetTargetYear(row);
            }

            model.MoTaMucTieu = GetValue(row, "MoTaMucTieu", "MOTAMUCTIEU", "Mo ta muc tieu");
            if (string.IsNullOrWhiteSpace(model.MoTaMucTieu))
            {
                model.MoTaMucTieu = targetText;
            }

            model.ToanTuSoSanh = GetValue(row, "ToanTuSoSanh", "TOANTUSOSANH", "Toan tu so sanh");
            model.GiaTriMucTieu = ParseNullableDecimal(GetValue(row, "GiaTriMucTieu", "GIATRIMUCTIEU", "Gia tri muc tieu"));
            if (!string.IsNullOrWhiteSpace(targetText))
            {
                FillTargetFromText(model, targetText);
            }

            if (model.LoaiCongThuc == 0)
            {
                model.LoaiCongThuc = string.IsNullOrWhiteSpace(model.MauSoMoTa) ? LoaiCongThuc.GiaTriTrucTiep : LoaiCongThuc.TyLe;
            }

            return model;
        }

        private void SaveImportedIndicator(ChiSoViewModel model, IList<int> departmentIds, int userId)
        {
            var existingId = FindIndicatorId(model.MaChiSo, model.TenChiSo);
            if (existingId.HasValue)
            {
                model.ChiSoChatLuongId = existingId.Value;
                if (string.IsNullOrWhiteSpace(model.MaChiSo))
                {
                    model.MaChiSo = Convert.ToString(Scalar("SELECT MaChiSo FROM dbo.ChiSoChatLuong WHERE ChiSoChatLuongId=@Id", Param("@Id", model.ChiSoChatLuongId)));
                }

                Save(model);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(model.MaChiSo))
                {
                    model.MaChiSo = "__AUTO_" + Guid.NewGuid().ToString("N");
                    Save(model);
                    model.MaChiSo = "CS" + model.ChiSoChatLuongId.ToString("0000");
                    Execute("UPDATE dbo.ChiSoChatLuong SET MaChiSo=@MaChiSo WHERE ChiSoChatLuongId=@Id",
                        Param("@MaChiSo", model.MaChiSo),
                        Param("@Id", model.ChiSoChatLuongId));
                }
                else
                {
                    Save(model);
                }
            }

            foreach (var departmentId in departmentIds)
            {
                Execute(@"
IF EXISTS (SELECT 1 FROM dbo.PhanCongChiSo WHERE KhoaPhongId=@KhoaPhongId AND ChiSoChatLuongId=@ChiSoChatLuongId)
    UPDATE dbo.PhanCongChiSo SET DangHoatDong=1 WHERE KhoaPhongId=@KhoaPhongId AND ChiSoChatLuongId=@ChiSoChatLuongId
ELSE
    INSERT INTO dbo.PhanCongChiSo(KhoaPhongId, ChiSoChatLuongId, DangHoatDong, NguoiTaoId) VALUES(@KhoaPhongId, @ChiSoChatLuongId, 1, @NguoiTaoId)",
                    Param("@KhoaPhongId", departmentId),
                    Param("@ChiSoChatLuongId", model.ChiSoChatLuongId),
                    Param("@NguoiTaoId", userId));
            }
        }

        private void PopulateIndicatorFrequencies(IEnumerable<ChiSoViewModel> models)
        {
            var items = models == null ? new List<ChiSoViewModel>() : models.ToList();
            foreach (var item in items)
            {
                item.TanSuatBaoCaos = new List<TanSuatBaoCao> { item.TanSuatBaoCao };
                item.SelectedTanSuatBaoCaoValues = new[] { (int)item.TanSuatBaoCao };
                item.TanSuatBaoCaoText = FormatFrequencies(item.TanSuatBaoCaos);
            }

            if (items.Count == 0)
            {
                return;
            }

            EnsureIndicatorFrequencyTable();
            var ids = items.Select(x => x.ChiSoChatLuongId).ToArray();
            var parameters = ids.Select((id, index) => Param("@Id" + index, id)).ToArray();
            var sql = @"SELECT ChiSoChatLuongId, TanSuatBaoCao
FROM dbo.ChiSoTanSuatBaoCao
WHERE ChiSoChatLuongId IN (" + string.Join(",", parameters.Select(p => p.ParameterName)) + @")
ORDER BY ChiSoChatLuongId, TanSuatBaoCao";

            var frequencyRows = Query(sql, r => new
            {
                ChiSoChatLuongId = Int(r, "ChiSoChatLuongId"),
                TanSuatBaoCao = (TanSuatBaoCao)r.GetByte(r.GetOrdinal("TanSuatBaoCao"))
            }, parameters);

            var groups = frequencyRows.GroupBy(x => x.ChiSoChatLuongId).ToDictionary(g => g.Key, g => (IList<TanSuatBaoCao>)SortFrequencies(g.Select(x => x.TanSuatBaoCao)).ToList());
            foreach (var item in items)
            {
                IList<TanSuatBaoCao> frequencies;
                if (groups.TryGetValue(item.ChiSoChatLuongId, out frequencies) && frequencies.Count > 0)
                {
                    item.TanSuatBaoCaos = frequencies;
                    item.TanSuatBaoCao = frequencies.First();
                    item.SelectedTanSuatBaoCaoValues = frequencies.Select(x => (int)x).ToArray();
                    item.TanSuatBaoCaoText = FormatFrequencies(frequencies);
                }
            }
        }

        private void SaveFrequencies(int indicatorId, IEnumerable<TanSuatBaoCao> frequencies)
        {
            EnsureIndicatorFrequencyTable();
            var values = SortFrequencies(frequencies == null ? new[] { TanSuatBaoCao.HangThang } : frequencies)
                .Distinct()
                .ToList();
            if (values.Count == 0)
            {
                values.Add(TanSuatBaoCao.HangThang);
            }

            Execute("DELETE FROM dbo.ChiSoTanSuatBaoCao WHERE ChiSoChatLuongId=@Id", Param("@Id", indicatorId));
            foreach (var frequency in values)
            {
                Execute(@"INSERT INTO dbo.ChiSoTanSuatBaoCao(ChiSoChatLuongId, TanSuatBaoCao)
VALUES(@ChiSoChatLuongId, @TanSuatBaoCao)",
                    Param("@ChiSoChatLuongId", indicatorId),
                    Param("@TanSuatBaoCao", (byte)frequency));
            }
        }

        private static void ApplySelectedFrequencies(ChiSoViewModel model)
        {
            var selected = model.SelectedTanSuatBaoCaoValues == null
                ? new List<TanSuatBaoCao>()
                : model.SelectedTanSuatBaoCaoValues
                    .Where(v => Enum.IsDefined(typeof(TanSuatBaoCao), (byte)v))
                    .Select(v => (TanSuatBaoCao)(byte)v)
                    .Distinct()
                    .ToList();

            if (selected.Count == 0)
            {
                if (model.TanSuatBaoCaos != null && model.TanSuatBaoCaos.Count > 0)
                {
                    selected = SortFrequencies(model.TanSuatBaoCaos).ToList();
                }
                else
                {
                    selected.Add(model.TanSuatBaoCao == 0 ? TanSuatBaoCao.HangThang : model.TanSuatBaoCao);
                }
            }

            selected = SortFrequencies(selected).ToList();
            model.TanSuatBaoCaos = selected;
            model.TanSuatBaoCao = selected.First();
            model.SelectedTanSuatBaoCaoValues = selected.Select(x => (int)x).ToArray();
            model.TanSuatBaoCaoText = FormatFrequencies(selected);
        }

        public void EnsureIndicatorFrequencyTable()
        {
            Execute(@"
IF OBJECT_ID(N'dbo.ChiSoTanSuatBaoCao', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChiSoTanSuatBaoCao (
        ChiSoTanSuatBaoCaoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChiSoTanSuatBaoCao PRIMARY KEY,
        ChiSoChatLuongId INT NOT NULL,
        TanSuatBaoCao TINYINT NOT NULL,
        CONSTRAINT FK_ChiSoTanSuatBaoCao_ChiSo FOREIGN KEY (ChiSoChatLuongId) REFERENCES dbo.ChiSoChatLuong(ChiSoChatLuongId),
        CONSTRAINT UQ_ChiSoTanSuatBaoCao UNIQUE (ChiSoChatLuongId, TanSuatBaoCao)
    );

    CREATE INDEX IX_ChiSoTanSuatBaoCao_ChiSoChatLuongId ON dbo.ChiSoTanSuatBaoCao(ChiSoChatLuongId);

    INSERT INTO dbo.ChiSoTanSuatBaoCao(ChiSoChatLuongId, TanSuatBaoCao)
    SELECT ChiSoChatLuongId, TanSuatBaoCao
    FROM dbo.ChiSoChatLuong;
END");
        }

        private int? FindIndicatorId(string code, string name)
        {
            if (!string.IsNullOrWhiteSpace(code))
            {
                var idByCode = Scalar("SELECT ChiSoChatLuongId FROM dbo.ChiSoChatLuong WHERE MaChiSo=@MaChiSo", Param("@MaChiSo", code.Trim()));
                if (idByCode != null)
                {
                    return Convert.ToInt32(idByCode);
                }
            }

            var idByName = Scalar("SELECT ChiSoChatLuongId FROM dbo.ChiSoChatLuong WHERE TenChiSo=@TenChiSo", Param("@TenChiSo", name.Trim()));
            return idByName == null ? (int?)null : Convert.ToInt32(idByName);
        }

        private IList<DepartmentLookup> GetDepartmentLookups()
        {
            return Query("SELECT KhoaPhongId, IdKhoaPhongNguon, TenKhoaPhong FROM dbo.KhoaPhong WHERE Used=1",
                r => new DepartmentLookup
                {
                    KhoaPhongId = Int(r, "KhoaPhongId"),
                    IdKhoaPhongNguon = Int(r, "IdKhoaPhongNguon"),
                    TenKhoaPhong = String(r, "TenKhoaPhong"),
                    NormalizedName = NormalizeKey(String(r, "TenKhoaPhong"))
                });
        }

        internal static IList<int> ResolveDepartmentIds(string value, IList<DepartmentLookup> departments)
        {
            var ids = new List<int>();
            if (string.IsNullOrWhiteSpace(value))
            {
                return ids;
            }

            var tokens = value.Split(new[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                var normalizedToken = NormalizeKey(token);
                int sourceId;
                DepartmentLookup department = null;
                if (int.TryParse(token.Trim(), out sourceId))
                {
                    department = departments.FirstOrDefault(x => x.IdKhoaPhongNguon == sourceId);
                }

                if (department == null)
                {
                    var alias = ResolveDepartmentAlias(normalizedToken);
                    if (!string.IsNullOrWhiteSpace(alias))
                    {
                        department = departments.FirstOrDefault(x => x.NormalizedName == alias);
                    }
                }

                if (department == null)
                {
                    department = departments.FirstOrDefault(x => x.NormalizedName == normalizedToken)
                        ?? departments.FirstOrDefault(x => normalizedToken.Contains(x.NormalizedName) || x.NormalizedName.Contains(normalizedToken));
                }

                if (department != null && !ids.Contains(department.KhoaPhongId))
                {
                    ids.Add(department.KhoaPhongId);
                }
            }

            if (ids.Count == 0)
            {
                var normalizedValue = NormalizeKey(value);
                var alias = ResolveDepartmentAlias(normalizedValue);
                if (!string.IsNullOrWhiteSpace(alias))
                {
                    foreach (var department in departments.Where(x => x.NormalizedName == alias))
                    {
                        if (!ids.Contains(department.KhoaPhongId))
                        {
                            ids.Add(department.KhoaPhongId);
                        }
                    }
                }

                foreach (var department in departments.Where(x => normalizedValue.Contains(x.NormalizedName)))
                {
                    if (!ids.Contains(department.KhoaPhongId))
                    {
                        ids.Add(department.KhoaPhongId);
                    }
                }
            }

            AddAllMatchingDepartments(ids, NormalizeKey(value), departments);
            return ids;
        }

        private static void AddAllMatchingDepartments(IList<int> ids, string normalizedValue, IList<DepartmentLookup> departments)
        {
            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                return;
            }

            foreach (var alias in ResolveDepartmentAliases(normalizedValue))
            {
                foreach (var department in departments.Where(x => x.NormalizedName == alias))
                {
                    AddDepartmentId(ids, department);
                }
            }

            foreach (var department in departments.Where(x => normalizedValue.Contains(x.NormalizedName)))
            {
                AddDepartmentId(ids, department);
            }
        }

        private static void AddDepartmentId(IList<int> ids, DepartmentLookup department)
        {
            if (department != null && !ids.Contains(department.KhoaPhongId))
            {
                ids.Add(department.KhoaPhongId);
            }
        }

        private static IEnumerable<string> ResolveDepartmentAliases(string normalizedValue)
        {
            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                yield break;
            }

            if (normalizedValue.Contains("ksnk") || normalizedValue.Contains("kiem soat nhiem khuan"))
            {
                yield return "kiem soat nhiem khuan";
            }

            if (normalizedValue.Contains("kht h") || normalizedValue.Contains("khth") || normalizedValue.Contains("ke hoach tong hop"))
            {
                yield return "ke hoach tong hop";
            }

            if (normalizedValue.Contains("tccb") || normalizedValue.Contains("to chuc can bo"))
            {
                yield return "to chuc can bo";
            }

            if (normalizedValue.Contains("hanh chanh quan tri") || normalizedValue.Contains("hcqt") || normalizedValue.Contains("hanh chinh quan tri"))
            {
                yield return "hanh chinh quan tri";
            }
        }

        private static string ResolveDepartmentAlias(string normalizedValue)
        {
            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                return null;
            }

            if (normalizedValue.Contains("ksnk") || normalizedValue.Contains("kiem soat nhiem khuan"))
            {
                return NormalizeKey("Kiểm soát nhiễm khuẩn");
            }

            if (normalizedValue.Contains("kht h") || normalizedValue.Contains("khth") || normalizedValue.Contains("ke hoach tong hop"))
            {
                return NormalizeKey("Kế hoạch tổng hợp");
            }

            if (normalizedValue.Contains("tccb") || normalizedValue.Contains("to chuc can bo"))
            {
                return NormalizeKey("Tổ chức cán bộ");
            }

            if (normalizedValue.Contains("hanh chanh quan tri") || normalizedValue.Contains("hcqt") || normalizedValue.Contains("hanh chinh quan tri"))
            {
                return NormalizeKey("Hành chính quản trị");
            }

            return null;
        }

        private static string GetDepartmentSource(IDictionary<string, string> row, string collectionText)
        {
            var explicitValue = GetValue(row, "KhoaPhongQuanLy", "KHOAPHONGQUANLY", "Khoa phong quan ly", "Khoa/Phong quan ly", "Đơn vị thu thập", "Don vi thu thap");
            return string.IsNullOrWhiteSpace(explicitValue) ? collectionText : explicitValue;
        }

        private static bool TryParseFrequencies(string value, out IList<TanSuatBaoCao> frequencies)
        {
            frequencies = new List<TanSuatBaoCao>();
            var text = NormalizeKey(value);
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (Regex.IsMatch(text, @"(^|\D)3(\D|$)") || text.Contains("ba thang") || ContainsQuarterFrequency(text))
            {
                frequencies.Add(TanSuatBaoCao.HangQuy);
            }

            if (Regex.IsMatch(text, @"(^|\D)6(\D|$)") || text.Contains("sau thang"))
            {
                frequencies.Add(TanSuatBaoCao.SauThang);
            }

            if (Regex.IsMatch(text, @"(^|\D)9(\D|$)") || text.Contains("chin thang"))
            {
                frequencies.Add(TanSuatBaoCao.ChinThang);
            }

            if (Regex.IsMatch(text, @"(^|\D)12(\D|$)") || text.Contains("muoi hai thang"))
            {
                frequencies.Add(TanSuatBaoCao.HangNam);
            }

            if (frequencies.Count == 0)
            {
                foreach (var part in value.Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    TanSuatBaoCao frequency;
                    if (TryParseFrequency(part, out frequency) && !frequencies.Contains(frequency))
                    {
                        frequencies.Add(frequency);
                    }
                }
            }

            frequencies = frequencies.Distinct().ToList();
            return frequencies.Count > 0;
        }

        private static bool TryParseFrequency(string value, out TanSuatBaoCao frequency)
        {
            var text = NormalizeKey(value);
            if (string.IsNullOrWhiteSpace(text))
            {
                frequency = TanSuatBaoCao.HangThang;
                return false;
            }

            if (text.Contains("truoc") && text.Contains("sau") && text.Contains("thuc hien"))
            {
                frequency = TanSuatBaoCao.TruocSauKhiThucHien;
                return true;
            }

            if (text.Contains("khi co") || text.Contains("xay ra") || text.Contains("phat sinh") || text.Contains("thuong xuyen"))
            {
                frequency = TanSuatBaoCao.KhiPhatSinh;
                return true;
            }

            if (text.Contains("9") || text.Contains("chin thang"))
            {
                frequency = TanSuatBaoCao.ChinThang;
                return true;
            }

            if (text.Contains("6") || text.Contains("sau thang"))
            {
                frequency = TanSuatBaoCao.SauThang;
                return true;
            }

            if (text.Contains("12") || text.Contains("nam") || text.Contains("hang nam"))
            {
                frequency = TanSuatBaoCao.HangNam;
                return true;
            }

            if (text.Contains("3") || ContainsQuarterFrequency(text) || text.Contains("ba thang"))
            {
                frequency = TanSuatBaoCao.HangQuy;
                return true;
            }

            if (text.Contains("tuan"))
            {
                frequency = TanSuatBaoCao.HangTuan;
                return true;
            }

            if (text.Contains("ngay"))
            {
                frequency = TanSuatBaoCao.HangNgay;
                return true;
            }

            if (text.Contains("thang") || text.Contains("moi thang") || text.Contains("hang thang"))
            {
                frequency = TanSuatBaoCao.HangThang;
                return true;
            }

            return Enum.TryParse(value, true, out frequency);
        }

        private static string FormatFrequencies(IEnumerable<TanSuatBaoCao> frequencies)
        {
            var values = frequencies == null ? new List<TanSuatBaoCao>() : SortFrequencies(frequencies).ToList();
            return values.Count == 0 ? string.Empty : string.Join(", ", values.Select(FormatFrequency));
        }

        private static bool ContainsQuarterFrequency(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            return Regex.IsMatch(text, @"(^|\s)(hang\s+quy|moi\s+quy|theo\s+quy|quy|hang\s+qui|moi\s+qui|theo\s+qui|qui)(\s|$)");
        }

        private static IEnumerable<TanSuatBaoCao> SortFrequencies(IEnumerable<TanSuatBaoCao> frequencies)
        {
            return (frequencies ?? new List<TanSuatBaoCao>())
                .Distinct()
                .OrderBy(GetFrequencyDisplayOrder);
        }

        private static int GetFrequencyDisplayOrder(TanSuatBaoCao frequency)
        {
            switch (frequency)
            {
                case TanSuatBaoCao.HangNgay: return 10;
                case TanSuatBaoCao.HangTuan: return 20;
                case TanSuatBaoCao.HangThang: return 30;
                case TanSuatBaoCao.HangQuy: return 40;
                case TanSuatBaoCao.SauThang: return 50;
                case TanSuatBaoCao.ChinThang: return 60;
                case TanSuatBaoCao.HangNam: return 70;
                case TanSuatBaoCao.KhiPhatSinh: return 80;
                case TanSuatBaoCao.TruocSauKhiThucHien: return 90;
                default: return 999;
            }
        }

        private static string FormatFrequency(TanSuatBaoCao frequency)
        {
            switch (frequency)
            {
                case TanSuatBaoCao.HangNgay: return "Hang ngay";
                case TanSuatBaoCao.HangTuan: return "Hang tuan";
                case TanSuatBaoCao.HangThang: return "Hang thang";
                case TanSuatBaoCao.HangQuy: return "Hang quy";
                case TanSuatBaoCao.SauThang: return "6 thang";
                case TanSuatBaoCao.ChinThang: return "9 thang";
                case TanSuatBaoCao.HangNam: return "12 thang";
                case TanSuatBaoCao.KhiPhatSinh: return "Khi phat sinh";
                case TanSuatBaoCao.TruocSauKhiThucHien: return "Truoc/sau khi thuc hien";
                default: return frequency.ToString();
            }
        }

        public static IList<SelectListItem> GetFrequencyOptions(IEnumerable<TanSuatBaoCao> selectedFrequencies)
        {
            var selected = selectedFrequencies == null
                ? new HashSet<TanSuatBaoCao>()
                : new HashSet<TanSuatBaoCao>(selectedFrequencies);
            return Enum.GetValues(typeof(TanSuatBaoCao))
                .Cast<TanSuatBaoCao>()
                .Select(x => new SelectListItem
                {
                    Value = ((int)x).ToString(),
                    Text = FormatFrequency(x),
                    Selected = selected.Contains(x)
                })
                .ToList();
        }

        private static LoaiCongThuc ParseFormulaType(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            LoaiCongThuc formula;
            if (Enum.TryParse(value, true, out formula))
            {
                return formula;
            }

            var text = NormalizeKey(value);
            if (text.Contains("ty le") || text.Contains("phan tram")) return LoaiCongThuc.TyLe;
            if (text.Contains("so luong")) return LoaiCongThuc.SoLuong;
            if (text.Contains("thoi gian")) return LoaiCongThuc.ThoiGianTrungBinh;
            if (text.Contains("diem")) return LoaiCongThuc.DiemTrungBinh;
            if (text.Contains("ty so")) return LoaiCongThuc.TySo;
            return LoaiCongThuc.GiaTriTrucTiep;
        }

        private static void FillTargetFromText(ChiSoViewModel model, string targetText)
        {
            if (string.IsNullOrWhiteSpace(model.ToanTuSoSanh))
            {
                var trimmed = targetText.Trim();
                if (trimmed.StartsWith(">=") || trimmed.StartsWith("≥")) model.ToanTuSoSanh = ">=";
                else if (trimmed.StartsWith("<=") || trimmed.StartsWith("≤")) model.ToanTuSoSanh = "<=";
                else if (trimmed.StartsWith(">")) model.ToanTuSoSanh = ">";
                else if (trimmed.StartsWith("<")) model.ToanTuSoSanh = "<";
                else model.ToanTuSoSanh = "=";
            }

            if (!model.GiaTriMucTieu.HasValue)
            {
                var match = Regex.Match(targetText, @"[-+]?\d+([,.]\d+)?");
                decimal value;
                if (match.Success && decimal.TryParse(match.Value.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                {
                    model.GiaTriMucTieu = value;
                }
            }
        }

        private static string GetTargetText(IDictionary<string, string> row)
        {
            var explicitValue = GetValue(row, "MucTieuDatDuoc", "MUCTIEUDATDUOC", "Muc tieu dat duoc", "Mục tiêu đạt được");
            if (!string.IsNullOrWhiteSpace(explicitValue))
            {
                return explicitValue;
            }

            foreach (var item in row)
            {
                var key = NormalizeKey(item.Key);
                if (key.Contains("muc tieu dat duoc") || key.Contains("muc tieu"))
                {
                    return item.Value;
                }
            }

            return null;
        }

        private static int? GetTargetYear(IDictionary<string, string> row)
        {
            foreach (var item in row)
            {
                var match = Regex.Match(item.Key ?? string.Empty, @"(19|20)\d{2}");
                int year;
                if (match.Success && int.TryParse(match.Value, out year))
                {
                    return year;
                }
            }

            return null;
        }

        private static string GetValue(IDictionary<string, string> row, params string[] names)
        {
            string value;
            return TryGetValue(row, out value, names) ? value : null;
        }

        private static bool TryGetValue(IDictionary<string, string> row, out string value, params string[] names)
        {
            foreach (var name in names)
            {
                if (row.TryGetValue(name, out value))
                {
                    return true;
                }
            }

            foreach (var item in row)
            {
                foreach (var name in names)
                {
                    if (NormalizeKey(item.Key) == NormalizeKey(name))
                    {
                        value = item.Value;
                        return true;
                    }
                }
            }

            value = null;
            return false;
        }

        private static int? ParseNullableInt(string value)
        {
            int number;
            return int.TryParse(value, out number) ? number : (int?)null;
        }

        private static decimal? ParseNullableDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            decimal number;
            return decimal.TryParse(value.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out number) ? number : (decimal?)null;
        }

        private static bool ParseBool(string value, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            if (value == "1") return true;
            if (value == "0") return false;

            bool result;
            return bool.TryParse(value, out result) ? result : defaultValue;
        }

        private static string NullIfWhiteSpace(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        internal static string NormalizeKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();
            foreach (var ch in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (ch == '\u0111')
                {
                    builder.Append('d');
                    continue;
                }

                builder.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
            }

            var text = Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
            if (text.StartsWith("phong "))
            {
                text = text.Substring(6);
            }

            if (text.StartsWith("khoa "))
            {
                text = text.Substring(5);
            }

            return text;
        }

        internal class DepartmentLookup
        {
            public int KhoaPhongId { get; set; }
            public int IdKhoaPhongNguon { get; set; }
            public string TenKhoaPhong { get; set; }
            public string NormalizedName { get; set; }
        }

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
                Param("@GiaTriSoLieu", model.GiaTriSoLieu),
                Param("@TanSuatBaoCao", (byte)model.TanSuatBaoCao),
                Param("@LoaiCongThuc", (byte)model.LoaiCongThuc),
                Param("@DonViTinh", model.DonViTinh),
                Param("@DangHoatDong", model.DangHoatDong)
            };
        }

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
                GiaTriSoLieu = String(reader, "GiaTriSoLieu"),
                TanSuatBaoCao = (TanSuatBaoCao)reader.GetByte(reader.GetOrdinal("TanSuatBaoCao")),
                LoaiCongThuc = (LoaiCongThuc)reader.GetByte(reader.GetOrdinal("LoaiCongThuc")),
                DonViTinh = String(reader, "DonViTinh"),
                DangHoatDong = reader.GetBoolean(reader.GetOrdinal("DangHoatDong"))
            };
        }
    }

    public class AssignmentService : DbServiceBase
    {
        public static string FormatFrequency(TanSuatBaoCao frequency)
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

        public static string FormatFormula(LoaiCongThuc formula)
        {
            switch (formula)
            {
                case LoaiCongThuc.TyLe: return "Tỷ lệ (%)";
                case LoaiCongThuc.SoLuong: return "Số lượng";
                case LoaiCongThuc.ThoiGianTrungBinh: return "Thời gian trung bình";
                case LoaiCongThuc.DiemTrungBinh: return "Điểm trung bình";
                case LoaiCongThuc.GiaTriTrucTiep: return "Giá trị trực tiếp";
                case LoaiCongThuc.TySo: return "Tỷ số";
                default: return formula.ToString();
            }
        }

        public int GetCount(int? khoaPhongId = null, int? chiSoId = null, string trangThai = null, string search = null)
        {
            var conditions = new List<string>();
            var parameters = new List<SqlParameter>();

            if (khoaPhongId.HasValue && khoaPhongId.Value > 0)
            {
                conditions.Add("pc.KhoaPhongId = @KhoaPhongId");
                parameters.Add(Param("@KhoaPhongId", khoaPhongId.Value));
            }

            if (chiSoId.HasValue && chiSoId.Value > 0)
            {
                conditions.Add("pc.ChiSoChatLuongId = @ChiSoId");
                parameters.Add(Param("@ChiSoId", chiSoId.Value));
            }

            if (!string.IsNullOrEmpty(trangThai))
            {
                if (trangThai == "active")
                {
                    conditions.Add("pc.DangHoatDong = 1");
                }
                else if (trangThai == "inactive")
                {
                    conditions.Add("pc.DangHoatDong = 0");
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                conditions.Add("(kp.TenKhoaPhong LIKE @Search OR cs.MaChiSo LIKE @Search OR cs.TenChiSo LIKE @Search)");
                parameters.Add(Param("@Search", "%" + search.Trim() + "%"));
            }

            string whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
            string sql = $@"
SELECT COUNT(*)
FROM dbo.PhanCongChiSo pc
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = pc.KhoaPhongId
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = pc.ChiSoChatLuongId
{whereClause}";

            return Convert.ToInt32(Scalar(sql, parameters.ToArray()));
        }

        public IList<AssignmentItemViewModel> GetAll(
            int? khoaPhongId = null,
            int? chiSoId = null,
            string trangThai = null,
            string search = null,
            int page = 1,
            int pageSize = 20)
        {
            var conditions = new List<string>();
            var parameters = new List<SqlParameter>();

            if (khoaPhongId.HasValue && khoaPhongId.Value > 0)
            {
                conditions.Add("pc.KhoaPhongId = @KhoaPhongId");
                parameters.Add(Param("@KhoaPhongId", khoaPhongId.Value));
            }

            if (chiSoId.HasValue && chiSoId.Value > 0)
            {
                conditions.Add("pc.ChiSoChatLuongId = @ChiSoId");
                parameters.Add(Param("@ChiSoId", chiSoId.Value));
            }

            if (!string.IsNullOrEmpty(trangThai))
            {
                if (trangThai == "active")
                {
                    conditions.Add("pc.DangHoatDong = 1");
                }
                else if (trangThai == "inactive")
                {
                    conditions.Add("pc.DangHoatDong = 0");
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                conditions.Add("(kp.TenKhoaPhong LIKE @Search OR cs.MaChiSo LIKE @Search OR cs.TenChiSo LIKE @Search)");
                parameters.Add(Param("@Search", "%" + search.Trim() + "%"));
            }

            string whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

            int offset = (page - 1) * pageSize;
            parameters.Add(Param("@Offset", offset));
            parameters.Add(Param("@PageSize", pageSize));

            string sql = $@"
SELECT pc.PhanCongChiSoId, pc.KhoaPhongId, pc.ChiSoChatLuongId, kp.TenKhoaPhong, 
       cs.MaChiSo, cs.TenChiSo, cs.TanSuatBaoCao, cs.LoaiCongThuc, pc.DangHoatDong, pc.NgayTao
FROM dbo.PhanCongChiSo pc
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = pc.KhoaPhongId
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = pc.ChiSoChatLuongId
{whereClause}
ORDER BY kp.TenKhoaPhong, cs.MaChiSo
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            return Query(sql, r => new AssignmentItemViewModel
            {
                PhanCongChiSoId = Int(r, "PhanCongChiSoId"),
                KhoaPhongId = Int(r, "KhoaPhongId"),
                ChiSoChatLuongId = Int(r, "ChiSoChatLuongId"),
                TenKhoaPhong = String(r, "TenKhoaPhong"),
                MaChiSo = String(r, "MaChiSo"),
                TenChiSo = String(r, "TenChiSo"),
                DangHoatDong = r.GetBoolean(r.GetOrdinal("DangHoatDong")),
                TanSuatBaoCaoText = FormatFrequency((TanSuatBaoCao)r.GetByte(r.GetOrdinal("TanSuatBaoCao"))),
                LoaiCongThucText = FormatFormula((LoaiCongThuc)r.GetByte(r.GetOrdinal("LoaiCongThuc"))),
                NgayTao = r.GetDateTime(r.GetOrdinal("NgayTao"))
            }, parameters.ToArray());
        }

        public IList<AssignmentItemViewModel> GetAll(int? khoaPhongId = null)
        {
            return GetAll(khoaPhongId, null, null, null, 1, 999999);
        }

        public int GetIndicatorsCount(string trangThaiPhanCong = null, string search = null, int? khoaPhongId = null, int? chiSoId = null, string trangThai = null)
        {
            var conditions = new List<string> { "cs.DangHoatDong = 1" };
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                conditions.Add("(cs.MaChiSo LIKE @Search OR cs.TenChiSo LIKE @Search)");
                parameters.Add(Param("@Search", "%" + search.Trim() + "%"));
            }

            if (khoaPhongId.HasValue && khoaPhongId.Value > 0)
            {
                conditions.Add("EXISTS (SELECT 1 FROM dbo.PhanCongChiSo pc WHERE pc.ChiSoChatLuongId = cs.ChiSoChatLuongId AND pc.KhoaPhongId = @FilterKhoaPhongId)");
                parameters.Add(Param("@FilterKhoaPhongId", khoaPhongId.Value));
            }

            if (chiSoId.HasValue && chiSoId.Value > 0)
            {
                conditions.Add("cs.ChiSoChatLuongId = @FilterChiSoId");
                parameters.Add(Param("@FilterChiSoId", chiSoId.Value));
            }

            if (!string.IsNullOrEmpty(trangThai))
            {
                if (trangThai == "active")
                {
                    conditions.Add("EXISTS (SELECT 1 FROM dbo.PhanCongChiSo pc WHERE pc.ChiSoChatLuongId = cs.ChiSoChatLuongId AND pc.DangHoatDong = 1)");
                }
                else if (trangThai == "inactive")
                {
                    conditions.Add("EXISTS (SELECT 1 FROM dbo.PhanCongChiSo pc WHERE pc.ChiSoChatLuongId = cs.ChiSoChatLuongId AND pc.DangHoatDong = 0)");
                }
            }

            if (!string.IsNullOrEmpty(trangThaiPhanCong))
            {
                if (trangThaiPhanCong == "assigned")
                {
                    conditions.Add("EXISTS (SELECT 1 FROM dbo.PhanCongChiSo pc WHERE pc.ChiSoChatLuongId = cs.ChiSoChatLuongId)");
                }
                else if (trangThaiPhanCong == "unassigned")
                {
                    conditions.Add("NOT EXISTS (SELECT 1 FROM dbo.PhanCongChiSo pc WHERE pc.ChiSoChatLuongId = cs.ChiSoChatLuongId AND pc.DangHoatDong = 1)");
                }
            }

            string whereClause = "WHERE " + string.Join(" AND ", conditions);
            string sql = $@"SELECT COUNT(*) FROM dbo.ChiSoChatLuong cs {whereClause}";

            return Convert.ToInt32(Scalar(sql, parameters.ToArray()));
        }

        public IList<IndicatorAssignmentGroup> GetAllIndicatorGroups(
            string trangThaiPhanCong = null,
            string search = null,
            int page = 1,
            int pageSize = 20,
            int? khoaPhongId = null,
            int? chiSoId = null,
            string trangThai = null)
        {
            var conditions = new List<string> { "cs.DangHoatDong = 1" };
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                conditions.Add("(cs.MaChiSo LIKE @Search OR cs.TenChiSo LIKE @Search)");
                parameters.Add(Param("@Search", "%" + search.Trim() + "%"));
            }

            if (khoaPhongId.HasValue && khoaPhongId.Value > 0)
            {
                conditions.Add("EXISTS (SELECT 1 FROM dbo.PhanCongChiSo pc WHERE pc.ChiSoChatLuongId = cs.ChiSoChatLuongId AND pc.KhoaPhongId = @FilterKhoaPhongId)");
                parameters.Add(Param("@FilterKhoaPhongId", khoaPhongId.Value));
            }

            if (chiSoId.HasValue && chiSoId.Value > 0)
            {
                conditions.Add("cs.ChiSoChatLuongId = @FilterChiSoId");
                parameters.Add(Param("@FilterChiSoId", chiSoId.Value));
            }

            if (!string.IsNullOrEmpty(trangThai))
            {
                if (trangThai == "active")
                {
                    conditions.Add("EXISTS (SELECT 1 FROM dbo.PhanCongChiSo pc WHERE pc.ChiSoChatLuongId = cs.ChiSoChatLuongId AND pc.DangHoatDong = 1)");
                }
                else if (trangThai == "inactive")
                {
                    conditions.Add("EXISTS (SELECT 1 FROM dbo.PhanCongChiSo pc WHERE pc.ChiSoChatLuongId = cs.ChiSoChatLuongId AND pc.DangHoatDong = 0)");
                }
            }

            if (!string.IsNullOrEmpty(trangThaiPhanCong))
            {
                if (trangThaiPhanCong == "assigned")
                {
                    conditions.Add("EXISTS (SELECT 1 FROM dbo.PhanCongChiSo pc WHERE pc.ChiSoChatLuongId = cs.ChiSoChatLuongId)");
                }
                else if (trangThaiPhanCong == "unassigned")
                {
                    conditions.Add("NOT EXISTS (SELECT 1 FROM dbo.PhanCongChiSo pc WHERE pc.ChiSoChatLuongId = cs.ChiSoChatLuongId AND pc.DangHoatDong = 1)");
                }
            }

            string whereClause = "WHERE " + string.Join(" AND ", conditions);

            int offset = (page - 1) * pageSize;
            parameters.Add(Param("@Offset", offset));
            parameters.Add(Param("@PageSize", pageSize));

            string indicatorSql = $@"
SELECT cs.ChiSoChatLuongId, cs.MaChiSo, cs.TenChiSo, cs.TanSuatBaoCao, cs.LoaiCongThuc
FROM dbo.ChiSoChatLuong cs
{whereClause}
ORDER BY ISNULL(cs.SoThuTu, 9999), cs.MaChiSo
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var indicators = Query(indicatorSql, r => new
            {
                ChiSoChatLuongId = Int(r, "ChiSoChatLuongId"),
                MaChiSo = String(r, "MaChiSo"),
                TenChiSo = String(r, "TenChiSo"),
                TanSuatBaoCao = (TanSuatBaoCao)r.GetByte(r.GetOrdinal("TanSuatBaoCao")),
                LoaiCongThuc = (LoaiCongThuc)r.GetByte(r.GetOrdinal("LoaiCongThuc"))
            }, parameters.ToArray());

            if (indicators.Count == 0)
            {
                return new List<IndicatorAssignmentGroup>();
            }

            var indicatorIds = indicators.Select(x => x.ChiSoChatLuongId).ToList();
            var assignmentsParams = new List<SqlParameter>();
            var assignmentsConditions = new List<string> { $"pc.ChiSoChatLuongId IN ({string.Join(",", indicatorIds)})" };

            if (khoaPhongId.HasValue && khoaPhongId.Value > 0)
            {
                assignmentsConditions.Add("pc.KhoaPhongId = @AssDeptId");
                assignmentsParams.Add(Param("@AssDeptId", khoaPhongId.Value));
            }
            if (!string.IsNullOrEmpty(trangThai))
            {
                if (trangThai == "active")
                {
                    assignmentsConditions.Add("pc.DangHoatDong = 1");
                }
                else if (trangThai == "inactive")
                {
                    assignmentsConditions.Add("pc.DangHoatDong = 0");
                }
            }

            string assignmentsWhere = "WHERE " + string.Join(" AND ", assignmentsConditions);
            string assignmentsSql = $@"
SELECT pc.PhanCongChiSoId, pc.KhoaPhongId, pc.ChiSoChatLuongId, kp.TenKhoaPhong,
       cs.MaChiSo, cs.TenChiSo, cs.TanSuatBaoCao, cs.LoaiCongThuc, pc.DangHoatDong, pc.NgayTao
FROM dbo.PhanCongChiSo pc
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = pc.KhoaPhongId
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = pc.ChiSoChatLuongId
{assignmentsWhere}
ORDER BY kp.TenKhoaPhong";

            var assignments = Query(assignmentsSql, r => new AssignmentItemViewModel
            {
                PhanCongChiSoId = Int(r, "PhanCongChiSoId"),
                KhoaPhongId = Int(r, "KhoaPhongId"),
                ChiSoChatLuongId = Int(r, "ChiSoChatLuongId"),
                TenKhoaPhong = String(r, "TenKhoaPhong"),
                MaChiSo = String(r, "MaChiSo"),
                TenChiSo = String(r, "TenChiSo"),
                DangHoatDong = r.GetBoolean(r.GetOrdinal("DangHoatDong")),
                TanSuatBaoCaoText = FormatFrequency((TanSuatBaoCao)r.GetByte(r.GetOrdinal("TanSuatBaoCao"))),
                LoaiCongThucText = FormatFormula((LoaiCongThuc)r.GetByte(r.GetOrdinal("LoaiCongThuc"))),
                NgayTao = r.GetDateTime(r.GetOrdinal("NgayTao"))
            }, assignmentsParams.ToArray());

            var groups = new List<IndicatorAssignmentGroup>();
            foreach (var ind in indicators)
            {
                var indAssignments = assignments.Where(x => x.ChiSoChatLuongId == ind.ChiSoChatLuongId).ToList();
                bool isUnassigned = indAssignments.Count == 0;

                groups.Add(new IndicatorAssignmentGroup
                {
                    ChiSoChatLuongId = ind.ChiSoChatLuongId,
                    MaChiSo = ind.MaChiSo,
                    TenChiSo = ind.TenChiSo,
                    TanSuatBaoCaoText = FormatFrequency(ind.TanSuatBaoCao),
                    LoaiCongThucText = FormatFormula(ind.LoaiCongThuc),
                    SoKhoaPhong = indAssignments.Count(x => x.DangHoatDong),
                    ChuaPhanCong = isUnassigned,
                    Items = indAssignments
                });
            }

            return groups;
        }

        public int GetDepartmentsCount(string search = null, int? khoaPhongId = null, int? chiSoId = null, string trangThai = null)
        {
            var conditions = new List<string> { "kp.Used = 1" };
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                conditions.Add("kp.TenKhoaPhong LIKE @Search");
                parameters.Add(Param("@Search", "%" + search.Trim() + "%"));
            }

            if (khoaPhongId.HasValue && khoaPhongId.Value > 0)
            {
                conditions.Add("kp.KhoaPhongId = @FilterKhoaPhongId");
                parameters.Add(Param("@FilterKhoaPhongId", khoaPhongId.Value));
            }

            if (chiSoId.HasValue && chiSoId.Value > 0)
            {
                conditions.Add("EXISTS (SELECT 1 FROM dbo.PhanCongChiSo pc WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.ChiSoChatLuongId = @FilterChiSoId)");
                parameters.Add(Param("@FilterChiSoId", chiSoId.Value));
            }

            if (!string.IsNullOrEmpty(trangThai))
            {
                if (trangThai == "active")
                {
                    conditions.Add("EXISTS (SELECT 1 FROM dbo.PhanCongChiSo pc WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1)");
                }
                else if (trangThai == "inactive")
                {
                    conditions.Add("EXISTS (SELECT 1 FROM dbo.PhanCongChiSo pc WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 0)");
                }
            }

            string whereClause = "WHERE " + string.Join(" AND ", conditions);
            string sql = $@"SELECT COUNT(*) FROM dbo.KhoaPhong kp {whereClause}";

            return Convert.ToInt32(Scalar(sql, parameters.ToArray()));
        }

        public IList<DepartmentAssignmentGroup> GetAllDepartmentGroups(
            string search = null,
            int page = 1,
            int pageSize = 20,
            int? khoaPhongId = null,
            int? chiSoId = null,
            string trangThai = null)
        {
            var conditions = new List<string> { "kp.Used = 1" };
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                conditions.Add("kp.TenKhoaPhong LIKE @Search");
                parameters.Add(Param("@Search", "%" + search.Trim() + "%"));
            }

            if (khoaPhongId.HasValue && khoaPhongId.Value > 0)
            {
                conditions.Add("kp.KhoaPhongId = @FilterKhoaPhongId");
                parameters.Add(Param("@FilterKhoaPhongId", khoaPhongId.Value));
            }

            if (chiSoId.HasValue && chiSoId.Value > 0)
            {
                conditions.Add("EXISTS (SELECT 1 FROM dbo.PhanCongChiSo pc WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.ChiSoChatLuongId = @FilterChiSoId)");
                parameters.Add(Param("@FilterChiSoId", chiSoId.Value));
            }

            if (!string.IsNullOrEmpty(trangThai))
            {
                if (trangThai == "active")
                {
                    conditions.Add("EXISTS (SELECT 1 FROM dbo.PhanCongChiSo pc WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 1)");
                }
                else if (trangThai == "inactive")
                {
                    conditions.Add("EXISTS (SELECT 1 FROM dbo.PhanCongChiSo pc WHERE pc.KhoaPhongId = kp.KhoaPhongId AND pc.DangHoatDong = 0)");
                }
            }

            string whereClause = "WHERE " + string.Join(" AND ", conditions);

            int offset = (page - 1) * pageSize;
            parameters.Add(Param("@Offset", offset));
            parameters.Add(Param("@PageSize", pageSize));

            string deptSql = $@"
SELECT kp.KhoaPhongId, kp.TenKhoaPhong
FROM dbo.KhoaPhong kp
{whereClause}
ORDER BY kp.TenKhoaPhong
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var departments = Query(deptSql, r => new
            {
                KhoaPhongId = Int(r, "KhoaPhongId"),
                TenKhoaPhong = String(r, "TenKhoaPhong")
            }, parameters.ToArray());

            if (departments.Count == 0)
            {
                return new List<DepartmentAssignmentGroup>();
            }

            var deptIds = departments.Select(x => x.KhoaPhongId).ToList();
            var assignmentsParams = new List<SqlParameter>();
            var assignmentsConditions = new List<string> { $"pc.KhoaPhongId IN ({string.Join(",", deptIds)})" };

            if (chiSoId.HasValue && chiSoId.Value > 0)
            {
                assignmentsConditions.Add("pc.ChiSoChatLuongId = @AssChiSoId");
                assignmentsParams.Add(Param("@AssChiSoId", chiSoId.Value));
            }
            if (!string.IsNullOrEmpty(trangThai))
            {
                if (trangThai == "active")
                {
                    assignmentsConditions.Add("pc.DangHoatDong = 1");
                }
                else if (trangThai == "inactive")
                {
                    assignmentsConditions.Add("pc.DangHoatDong = 0");
                }
            }

            string assignmentsWhere = "WHERE " + string.Join(" AND ", assignmentsConditions);
            string assignmentsSql = $@"
SELECT pc.PhanCongChiSoId, pc.KhoaPhongId, pc.ChiSoChatLuongId, kp.TenKhoaPhong,
       cs.MaChiSo, cs.TenChiSo, cs.TanSuatBaoCao, cs.LoaiCongThuc, pc.DangHoatDong, pc.NgayTao
FROM dbo.PhanCongChiSo pc
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = pc.KhoaPhongId
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = pc.ChiSoChatLuongId
{assignmentsWhere}
ORDER BY cs.MaChiSo";

            var assignments = Query(assignmentsSql, r => new AssignmentItemViewModel
            {
                PhanCongChiSoId = Int(r, "PhanCongChiSoId"),
                KhoaPhongId = Int(r, "KhoaPhongId"),
                ChiSoChatLuongId = Int(r, "ChiSoChatLuongId"),
                TenKhoaPhong = String(r, "TenKhoaPhong"),
                MaChiSo = String(r, "MaChiSo"),
                TenChiSo = String(r, "TenChiSo"),
                DangHoatDong = r.GetBoolean(r.GetOrdinal("DangHoatDong")),
                TanSuatBaoCaoText = FormatFrequency((TanSuatBaoCao)r.GetByte(r.GetOrdinal("TanSuatBaoCao"))),
                LoaiCongThucText = FormatFormula((LoaiCongThuc)r.GetByte(r.GetOrdinal("LoaiCongThuc"))),
                NgayTao = r.GetDateTime(r.GetOrdinal("NgayTao"))
            }, assignmentsParams.ToArray());

            var groups = new List<DepartmentAssignmentGroup>();
            foreach (var dept in departments)
            {
                var deptAssignments = assignments.Where(x => x.KhoaPhongId == dept.KhoaPhongId).ToList();
                groups.Add(new DepartmentAssignmentGroup
                {
                    KhoaPhongId = dept.KhoaPhongId,
                    TenKhoaPhong = dept.TenKhoaPhong,
                    SoChiSoHoatDong = deptAssignments.Count(x => x.DangHoatDong),
                    SoChiSoTamDung = deptAssignments.Count(x => !x.DangHoatDong),
                    Items = deptAssignments
                });
            }

            return groups;
        }

        public AssignmentViewModel GetStatistics()
        {
            var tongChiSo = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.ChiSoChatLuong WHERE DangHoatDong = 1"));
            var soChiSoDaPhanCong = Convert.ToInt32(Scalar(@"
SELECT COUNT(DISTINCT ChiSoChatLuongId) 
FROM dbo.PhanCongChiSo 
WHERE DangHoatDong = 1"));

            var tongPhanCong = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.PhanCongChiSo"));
            var soTamDung = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.PhanCongChiSo WHERE DangHoatDong = 0"));

            return new AssignmentViewModel
            {
                TongChiSo = tongChiSo,
                SoChiSoDaPhanCong = soChiSoDaPhanCong,
                SoChiSoChuaPhanCong = Math.Max(0, tongChiSo - soChiSoDaPhanCong),
                TongPhanCong = tongPhanCong,
                SoTamDung = soTamDung
            };
        }

        public IList<AssignmentPreviewItem> Preview(int[] departmentIds, int[] indicatorIds)
        {
            var result = new List<AssignmentPreviewItem>();
            if (departmentIds == null || departmentIds.Length == 0 || indicatorIds == null || indicatorIds.Length == 0)
            {
                return result;
            }

            var depts = Query($"SELECT KhoaPhongId, TenKhoaPhong FROM dbo.KhoaPhong WHERE KhoaPhongId IN ({string.Join(",", departmentIds)})",
                r => new { Id = Int(r, "KhoaPhongId"), Name = String(r, "TenKhoaPhong") });

            var inds = Query($"SELECT ChiSoChatLuongId, MaChiSo, TenChiSo FROM dbo.ChiSoChatLuong WHERE ChiSoChatLuongId IN ({string.Join(",", indicatorIds)})",
                r => new { Id = Int(r, "ChiSoChatLuongId"), Code = String(r, "MaChiSo"), Name = String(r, "TenChiSo") });

            foreach (var dept in depts)
            {
                foreach (var ind in inds)
                {
                    var exists = Convert.ToInt32(Scalar(@"
SELECT COUNT(*) FROM dbo.PhanCongChiSo 
WHERE KhoaPhongId = @KhoaPhongId AND ChiSoChatLuongId = @ChiSoId AND DangHoatDong = 1",
                        Param("@KhoaPhongId", dept.Id),
                        Param("@ChiSoId", ind.Id))) > 0;

                    result.Add(new AssignmentPreviewItem
                    {
                        TenKhoaPhong = dept.Name,
                        MaChiSo = ind.Code,
                        TenChiSo = ind.Name,
                        DaTonTai = exists
                    });
                }
            }

            return result;
        }

        public void Assign(IEnumerable<int> departmentIds, IEnumerable<int> indicatorIds, int currentUserId)
        {
            var departments = (departmentIds ?? new int[0]).Distinct().ToList();
            var indicators = (indicatorIds ?? new int[0]).Distinct().ToList();

            foreach (var departmentId in departments)
            {
                foreach (var indicatorId in indicators)
                {
                    Execute(@"
IF EXISTS (SELECT 1 FROM dbo.PhanCongChiSo WHERE KhoaPhongId=@KhoaPhongId AND ChiSoChatLuongId=@ChiSoChatLuongId)
    UPDATE dbo.PhanCongChiSo SET DangHoatDong=1 WHERE KhoaPhongId=@KhoaPhongId AND ChiSoChatLuongId=@ChiSoChatLuongId
ELSE
    INSERT INTO dbo.PhanCongChiSo(KhoaPhongId, ChiSoChatLuongId, DangHoatDong, NguoiTaoId) VALUES(@KhoaPhongId, @ChiSoChatLuongId, 1, @NguoiTaoId)",
                        Param("@KhoaPhongId", departmentId),
                        Param("@ChiSoChatLuongId", indicatorId),
                        Param("@NguoiTaoId", currentUserId));
                }
            }
        }

        public int SyncFromIndicatorSources(int currentUserId)
        {
            var departments = Query("SELECT KhoaPhongId, IdKhoaPhongNguon, TenKhoaPhong FROM dbo.KhoaPhong WHERE Used=1",
                r => new IndicatorService.DepartmentLookup
                {
                    KhoaPhongId = Int(r, "KhoaPhongId"),
                    IdKhoaPhongNguon = Int(r, "IdKhoaPhongNguon"),
                    TenKhoaPhong = String(r, "TenKhoaPhong"),
                    NormalizedName = IndicatorService.NormalizeKey(String(r, "TenKhoaPhong"))
                });

            var indicators = Query("SELECT ChiSoChatLuongId, ThuThapTongHop FROM dbo.ChiSoChatLuong WHERE DangHoatDong=1",
                r => new
                {
                    ChiSoChatLuongId = Int(r, "ChiSoChatLuongId"),
                    ThuThapTongHop = String(r, "ThuThapTongHop")
                });

            var changed = 0;
            foreach (var indicator in indicators)
            {
                var departmentIds = IndicatorService.ResolveDepartmentIds(indicator.ThuThapTongHop, departments);
                foreach (var departmentId in departmentIds)
                {
                    var assignmentId = Scalar(@"SELECT PhanCongChiSoId FROM dbo.PhanCongChiSo
WHERE KhoaPhongId=@KhoaPhongId AND ChiSoChatLuongId=@ChiSoChatLuongId AND DangHoatDong=1",
                        Param("@KhoaPhongId", departmentId),
                        Param("@ChiSoChatLuongId", indicator.ChiSoChatLuongId));
                    if (assignmentId != null)
                    {
                        continue;
                    }

                    Execute(@"
IF EXISTS (SELECT 1 FROM dbo.PhanCongChiSo WHERE KhoaPhongId=@KhoaPhongId AND ChiSoChatLuongId=@ChiSoChatLuongId)
    UPDATE dbo.PhanCongChiSo SET DangHoatDong=1 WHERE KhoaPhongId=@KhoaPhongId AND ChiSoChatLuongId=@ChiSoChatLuongId
ELSE
    INSERT INTO dbo.PhanCongChiSo(KhoaPhongId, ChiSoChatLuongId, DangHoatDong, NguoiTaoId) VALUES(@KhoaPhongId, @ChiSoChatLuongId, 1, @NguoiTaoId)",
                        Param("@KhoaPhongId", departmentId),
                        Param("@ChiSoChatLuongId", indicator.ChiSoChatLuongId),
                        Param("@NguoiTaoId", currentUserId));
                    changed++;
                }
            }

            return changed;
        }

        public void Deactivate(int id)
        {
            Execute("UPDATE dbo.PhanCongChiSo SET DangHoatDong = 0 WHERE PhanCongChiSoId = @Id", Param("@Id", id));
        }

        public void Activate(int id)
        {
            Execute("UPDATE dbo.PhanCongChiSo SET DangHoatDong = 1 WHERE PhanCongChiSoId = @Id", Param("@Id", id));
        }

        public void BulkDeactivate(int[] ids)
        {
            if (ids == null || ids.Length == 0) return;
            Execute($"UPDATE dbo.PhanCongChiSo SET DangHoatDong = 0 WHERE PhanCongChiSoId IN ({string.Join(",", ids)})");
        }

        public void BulkActivate(int[] ids)
        {
            if (ids == null || ids.Length == 0) return;
            Execute($"UPDATE dbo.PhanCongChiSo SET DangHoatDong = 1 WHERE PhanCongChiSoId IN ({string.Join(",", ids)})");
        }

        public void BulkDelete(int[] ids)
        {
            if (ids == null || ids.Length == 0) return;

            var reportCount = Convert.ToInt32(Scalar($"SELECT COUNT(*) FROM dbo.BaoCao WHERE PhanCongChiSoId IN ({string.Join(",", ids)})"));
            if (reportCount > 0)
            {
                throw new InvalidOperationException("Một số phân công được chọn đã có báo cáo, vui lòng ngừng kích hoạt thay vì xóa.");
            }

            Execute($"DELETE FROM dbo.PhanCongChiSo WHERE PhanCongChiSoId IN ({string.Join(",", ids)})");
        }

        public void Delete(int id)
        {
            var reportCount = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.BaoCao WHERE PhanCongChiSoId=@Id", Param("@Id", id)));
            if (reportCount > 0)
            {
                throw new InvalidOperationException("Phan cong da co bao cao, vui long ngung kich hoat thay vi xoa.");
            }

            Execute("DELETE FROM dbo.PhanCongChiSo WHERE PhanCongChiSoId=@Id", Param("@Id", id));
        }
    }

    public class ReportingPeriodService : DbServiceBase
    {
        public IList<KyBaoCaoViewModel> GetAll()
        {
            const string sql = @"
SELECT ky.KyBaoCaoId, ky.TenKyBaoCao, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay, ky.HanNop, ky.TrangThai,
       COUNT(DISTINCT pc.PhanCongChiSoId) AS TongBaoCao,
       ISNULL(SUM(CASE WHEN bc.TrangThai IN (2,3,4) THEN 1 ELSE 0 END), 0) AS DaGui
FROM dbo.KyBaoCao ky
LEFT JOIN dbo.PhanCongChiSo pc ON pc.DangHoatDong = 1
LEFT JOIN dbo.ChiSoTanSuatBaoCao cst ON cst.ChiSoChatLuongId = pc.ChiSoChatLuongId AND cst.TanSuatBaoCao = ky.LoaiKyBaoCao
LEFT JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = pc.ChiSoChatLuongId
LEFT JOIN dbo.BaoCao bc ON bc.KyBaoCaoId = ky.KyBaoCaoId 
                        AND bc.KhoaPhongId = pc.KhoaPhongId 
                        AND bc.ChiSoChatLuongId = pc.ChiSoChatLuongId
WHERE (cst.ChiSoTanSuatBaoCaoId IS NOT NULL OR cs.TanSuatBaoCao = ky.LoaiKyBaoCao)
GROUP BY ky.KyBaoCaoId, ky.TenKyBaoCao, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay, ky.HanNop, ky.TrangThai
ORDER BY ky.TuNgay DESC";
            return Query(sql, MapPeriod);
        }

        public bool HasActiveAssignmentsForFrequency(TanSuatBaoCao frequency)
        {
            const string sql = @"
SELECT COUNT(*) 
FROM dbo.PhanCongChiSo pc
INNER JOIN dbo.ChiSoTanSuatBaoCao tsb ON tsb.ChiSoChatLuongId = pc.ChiSoChatLuongId
WHERE pc.DangHoatDong = 1 AND tsb.TanSuatBaoCao = @Frequency
UNION ALL
SELECT COUNT(*)
FROM dbo.PhanCongChiSo pc
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = pc.ChiSoChatLuongId
WHERE pc.DangHoatDong = 1 AND cs.TanSuatBaoCao = @Frequency";

            var counts = Query(sql, r => r.GetInt32(0), Param("@Frequency", (byte)frequency)).ToList();
            return counts.Sum() > 0;
        }

        public (DateTime TuNgay, DateTime DenNgay, string TenKyBaoCao) CalculatePeriodDates(TanSuatBaoCao frequency, DateTime referenceDate)
        {
            DateTime tuNgay, denNgay;
            string tenKyBaoCao;

            switch (frequency)
            {
                case TanSuatBaoCao.HangNgay:
                    tuNgay = referenceDate.Date;
                    denNgay = tuNgay;
                    tenKyBaoCao = $"Kỳ báo cáo ngày {tuNgay:dd/MM/yyyy}";
                    break;

                case TanSuatBaoCao.HangTuan:
                    // Assuming week starts on Monday
                    int diff = (int)referenceDate.DayOfWeek - (int)DayOfWeek.Monday;
                    if (diff < 0) diff += 7;
                    tuNgay = referenceDate.Date.AddDays(-diff);
                    denNgay = tuNgay.AddDays(6);
                    tenKyBaoCao = $"Kỳ báo cáo tuần {tuNgay:dd/MM} - {denNgay:dd/MM/yyyy}";
                    break;

                case TanSuatBaoCao.HangThang:
                    tuNgay = new DateTime(referenceDate.Year, referenceDate.Month, 1);
                    denNgay = tuNgay.AddMonths(1).AddDays(-1);
                    tenKyBaoCao = $"Kỳ báo cáo Tháng {tuNgay:MM/yyyy}";
                    break;

                case TanSuatBaoCao.HangQuy:
                    int quarterNumber = (referenceDate.Month - 1) / 3 + 1;
                    tuNgay = new DateTime(referenceDate.Year, (quarterNumber - 1) * 3 + 1, 1);
                    denNgay = tuNgay.AddMonths(3).AddDays(-1);
                    tenKyBaoCao = $"Kỳ báo cáo Quý {quarterNumber}/{tuNgay:yyyy}";
                    break;

                case TanSuatBaoCao.SauThang:
                    int sixMonthStart = referenceDate.Month <= 6 ? 1 : 7;
                    tuNgay = new DateTime(referenceDate.Year, sixMonthStart, 1);
                    denNgay = tuNgay.AddMonths(6).AddDays(-1);
                    tenKyBaoCao = $"Kỳ báo cáo 6 tháng {tuNgay:MM/yyyy} - {denNgay:MM/yyyy}";
                    break;

                case TanSuatBaoCao.ChinThang:
                    int nineMonthStart = referenceDate.Month <= 9 ? 1 : 4; // Adjust logic as needed
                    tuNgay = new DateTime(referenceDate.Year, nineMonthStart, 1);
                    denNgay = tuNgay.AddMonths(9).AddDays(-1);
                    tenKyBaoCao = $"Kỳ báo cáo 9 tháng {tuNgay:MM/yyyy} - {denNgay:MM/yyyy}";
                    break;

                case TanSuatBaoCao.HangNam:
                    tuNgay = new DateTime(referenceDate.Year, 1, 1);
                    denNgay = tuNgay.AddYears(1).AddDays(-1);
                    tenKyBaoCao = $"Kỳ báo cáo Năm {tuNgay:yyyy}";
                    break;

                default:
                    throw new NotSupportedException($"Frequency {frequency} is not supported for auto-generation");
            }

            return (tuNgay, denNgay, tenKyBaoCao);
        }

        public bool PeriodExists(TanSuatBaoCao frequency, DateTime tuNgay)
        {
            var count = Convert.ToInt32(Scalar(@"
SELECT COUNT(*) FROM dbo.KyBaoCao 
WHERE LoaiKyBaoCao = @Frequency AND TuNgay = @TuNgay",
                Param("@Frequency", (byte)frequency),
                Param("@TuNgay", tuNgay)));
            return count > 0;
        }

        public IList<KyBaoCaoViewModel> GenerateMissingPeriods()
        {
            var generatedPeriods = new List<KyBaoCaoViewModel>();
            var referenceDate = DateTime.Today;

            // List of frequencies that can be auto-generated
            var autoGenerateFrequencies = new[]
            {
                TanSuatBaoCao.HangNgay,
                TanSuatBaoCao.HangTuan,
                TanSuatBaoCao.HangThang,
                TanSuatBaoCao.HangQuy,
                TanSuatBaoCao.SauThang,
                TanSuatBaoCao.ChinThang,
                TanSuatBaoCao.HangNam
            };

            foreach (var frequency in autoGenerateFrequencies)
            {
                if (!HasActiveAssignmentsForFrequency(frequency))
                {
                    continue;
                }

                var (tuNgay, denNgay, tenKyBaoCao) = CalculatePeriodDates(frequency, referenceDate);

                if (!PeriodExists(frequency, tuNgay))
                {
                    var model = new KyBaoCaoViewModel
                    {
                        TenKyBaoCao = tenKyBaoCao,
                        LoaiKyBaoCao = frequency,
                        TuNgay = tuNgay,
                        DenNgay = denNgay,
                        HanNop = denNgay.AddDays(5),
                        TrangThai = TrangThaiKyBaoCao.Mo
                    };

                    Save(model);
                    generatedPeriods.Add(model);
                }
            }

            return generatedPeriods;
        }

        public IList<TanSuatBaoCao> GetFrequenciesForDepartment(int departmentId)
        {
            const string sql = @"
SELECT DISTINCT tsb.TanSuatBaoCao
FROM dbo.PhanCongChiSo pc
INNER JOIN dbo.ChiSoTanSuatBaoCao tsb ON tsb.ChiSoChatLuongId = pc.ChiSoChatLuongId
WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1
UNION
SELECT DISTINCT cs.TanSuatBaoCao
FROM dbo.PhanCongChiSo pc
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = pc.ChiSoChatLuongId
WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1";

            return Query(sql, r => (TanSuatBaoCao)r.GetByte(0), Param("@KhoaPhongId", departmentId));
        }

        public IList<SelectListItem> GetOptions()
        {
            return GetAll().Select(x => new SelectListItem { Value = x.KyBaoCaoId.ToString(), Text = x.TenKyBaoCao }).ToList();
        }

        public KyBaoCaoViewModel Get(int id)
        {
            return QuerySingle(@"SELECT KyBaoCaoId, TenKyBaoCao, LoaiKyBaoCao, TuNgay, DenNgay, HanNop, TrangThai, 0 AS TongBaoCao, 0 AS DaGui FROM dbo.KyBaoCao WHERE KyBaoCaoId=@Id",
                MapPeriod, Param("@Id", id));
        }

        public void Save(KyBaoCaoViewModel model)
        {
            if (model.TuNgay > model.DenNgay || model.HanNop < model.DenNgay)
            {
                throw new InvalidOperationException("Ngay bao cao va han nop khong hop le.");
            }

            if (model.KyBaoCaoId == 0)
            {
                Execute(@"INSERT INTO dbo.KyBaoCao(TenKyBaoCao, LoaiKyBaoCao, TuNgay, DenNgay, HanNop, TrangThai)
VALUES(@TenKyBaoCao, @LoaiKyBaoCao, @TuNgay, @DenNgay, @HanNop, @TrangThai)",
                    PeriodParams(model));
                return;
            }

            var parameters = PeriodParams(model).Concat(new[] { Param("@KyBaoCaoId", model.KyBaoCaoId) }).ToArray();
            Execute(@"UPDATE dbo.KyBaoCao SET TenKyBaoCao=@TenKyBaoCao, LoaiKyBaoCao=@LoaiKyBaoCao, TuNgay=@TuNgay, DenNgay=@DenNgay,
HanNop=@HanNop, TrangThai=@TrangThai, NgayCapNhat=GETDATE() WHERE KyBaoCaoId=@KyBaoCaoId", parameters);
        }

        public void SetStatus(int id, TrangThaiKyBaoCao status)
        {
            Execute("UPDATE dbo.KyBaoCao SET TrangThai=@TrangThai, NgayCapNhat=GETDATE() WHERE KyBaoCaoId=@Id", Param("@TrangThai", (byte)status), Param("@Id", id));
        }

        public void Delete(int id)
        {
            var dependentCount = Convert.ToInt32(Scalar(@"
SELECT
    (SELECT COUNT(*) FROM dbo.BaoCao WHERE KyBaoCaoId=@Id) +
    (SELECT COUNT(*) FROM dbo.ThongBao WHERE KyBaoCaoId=@Id)",
                Param("@Id", id)));
            if (dependentCount > 0)
            {
                throw new InvalidOperationException("Ky bao cao da co du lieu lien quan, vui long khoa thay vi xoa.");
            }

            Execute("DELETE FROM dbo.KyBaoCao WHERE KyBaoCaoId=@Id", Param("@Id", id));
        }

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
    }
}
