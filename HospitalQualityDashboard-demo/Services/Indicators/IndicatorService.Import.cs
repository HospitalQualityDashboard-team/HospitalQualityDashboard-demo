// Mục đích: nhập danh mục chỉ số từ Excel và ghi nhận lỗi nghiệp vụ khi import.
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
        public ImportResultViewModel Import(HttpPostedFileBase file, int userId)
        {
            var rows = ReadIndicatorImportRows(file);
            var result = new ImportResultViewModel { TongSoDong = rows.Count };
            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var departments = GetDepartmentLookups();

            ExecuteInTransaction((conn, trans) =>
            {
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
                        SaveImportedIndicator(conn, trans, model, departmentIds, userId);
                        result.SoDongThanhCong++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add("Dong " + rowNumber + ": " + ex.Message);
                        result.SoDongLoi++;
                        throw;
                    }
                }

                Execute(conn, trans, @"INSERT INTO dbo.LichSuImport(LoaiImport, TenFile, TongSoDong, SoDongThanhCong, SoDongLoi, NguoiImportId)
VALUES(@LoaiImport, @TenFile, @TongSoDong, @SoDongThanhCong, @SoDongLoi, @NguoiImportId)",
                    Param("@LoaiImport", (byte)LoaiImport.ChiSoChatLuong),
                    Param("@TenFile", file == null ? null : file.FileName),
                    Param("@TongSoDong", result.TongSoDong),
                    Param("@SoDongThanhCong", result.SoDongThanhCong),
                    Param("@SoDongLoi", result.SoDongLoi),
                    Param("@NguoiImportId", userId));
            });

            DropdownCache.Remove("dropdown:indicators");
            return result;
        }

        // Xử lý chức năng chỉ số chất lượng của method ReadIndicatorImportRows, giữ logic nghiệp vụ tập trung trong tầng phù hợp.
        private IList<IDictionary<string, string>> ReadIndicatorImportRows(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
            {
                throw new ArgumentException("Vui lòng chọn tệp tin import hợp lệ và không trống.");
            }

            var extension = Path.GetExtension(file.FileName);
            return string.Equals(extension, ".docx", StringComparison.OrdinalIgnoreCase)
                ? _excel.ReadIndicatorDocxTables(file)
                : _excel.ReadWorksheet(file);
        }

        // Dựng cấu trúc dữ liệu chỉ số chất lượng từ input đã lọc để tái sử dụng cho truy vấn, view hoặc xuất file.
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
                model.LoaiCongThuc = InferFormulaType(model);
            }

            if (string.IsNullOrWhiteSpace(model.DonViTinh))
            {
                model.DonViTinh = InferUnit(model);
            }

            return model;
        }

        // Xử lý chức năng chỉ số chất lượng của method SaveImportedIndicator, giữ logic nghiệp vụ tập trung trong tầng phù hợp.
        private void SaveImportedIndicator(SqlConnection connection, SqlTransaction transaction, ChiSoViewModel model, IList<int> departmentIds, int userId)
        {
            var existingId = FindIndicatorId(connection, transaction, model.MaChiSo, model.TenChiSo);
            if (existingId.HasValue)
            {
                model.ChiSoChatLuongId = existingId.Value;
                if (string.IsNullOrWhiteSpace(model.MaChiSo))
                {
                    model.MaChiSo = Convert.ToString(Scalar(connection, transaction, "SELECT MaChiSo FROM dbo.ChiSoChatLuong WHERE ChiSoChatLuongId=@Id", Param("@Id", model.ChiSoChatLuongId)));
                }

                Save(connection, transaction, model);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(model.MaChiSo))
                {
                    model.MaChiSo = "__AUTO_" + Guid.NewGuid().ToString("N");
                    Save(connection, transaction, model);
                    model.MaChiSo = "CS" + model.ChiSoChatLuongId.ToString("0000");
                    Execute(connection, transaction, "UPDATE dbo.ChiSoChatLuong SET MaChiSo=@MaChiSo WHERE ChiSoChatLuongId=@Id",
                        Param("@MaChiSo", model.MaChiSo),
                        Param("@Id", model.ChiSoChatLuongId));
                }
                else
                {
                    Save(connection, transaction, model);
                }
            }

            foreach (var departmentId in departmentIds)
            {
                Execute(connection, transaction, @"
IF EXISTS (SELECT 1 FROM dbo.PhanCongChiSo WHERE KhoaPhongId=@KhoaPhongId AND ChiSoChatLuongId=@ChiSoChatLuongId)
    UPDATE dbo.PhanCongChiSo SET DangHoatDong=1 WHERE KhoaPhongId=@KhoaPhongId AND ChiSoChatLuongId=@ChiSoChatLuongId
ELSE
    INSERT INTO dbo.PhanCongChiSo(KhoaPhongId, ChiSoChatLuongId, DangHoatDong, NguoiTaoId) VALUES(@KhoaPhongId, @ChiSoChatLuongId, 1, @NguoiTaoId)",
                    Param("@KhoaPhongId", departmentId),
                    Param("@ChiSoChatLuongId", model.ChiSoChatLuongId),
                    Param("@NguoiTaoId", userId));
            }
        }

        // Điền dữ liệu suy ra hoặc dữ liệu liên quan vào model của danh mục chỉ số chất lượng.
    }
}
