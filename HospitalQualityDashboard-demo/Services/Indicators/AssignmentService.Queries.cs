// Mục đích: truy vấn dữ liệu phân công chỉ số theo bộ lọc và phạm vi quyền hạn.
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
    public partial class AssignmentService
    {
        // Xử lý chức năng phân công chỉ số của method GetCount, giữ logic nghiệp vụ tập trung trong tầng phù hợp.
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
            string sql = @"
SELECT COUNT(*)
FROM dbo.PhanCongChiSo pc
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = pc.KhoaPhongId
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = pc.ChiSoChatLuongId
" + whereClause;

            return Convert.ToInt32(Scalar(sql, parameters.ToArray()));
        }

        // Lấy danh sách phân công chỉ số theo bộ lọc, trạng thái hoạt động và phạm vi quyền đang áp dụng.
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

            string sql = @"
SELECT pc.PhanCongChiSoId, pc.KhoaPhongId, pc.ChiSoChatLuongId, kp.TenKhoaPhong,
       cs.MaChiSo, cs.TenChiSo, cs.LoaiCongThuc, pc.DangHoatDong, pc.NgayTao
FROM dbo.PhanCongChiSo pc
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = pc.KhoaPhongId
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = pc.ChiSoChatLuongId
" + whereClause + @"
ORDER BY kp.TenKhoaPhong, cs.MaChiSo
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var items = Query(sql, r => new AssignmentItemViewModel
            {
                PhanCongChiSoId = Int(r, "PhanCongChiSoId"),
                KhoaPhongId = Int(r, "KhoaPhongId"),
                ChiSoChatLuongId = Int(r, "ChiSoChatLuongId"),
                TenKhoaPhong = String(r, "TenKhoaPhong"),
                MaChiSo = String(r, "MaChiSo"),
                TenChiSo = String(r, "TenChiSo"),
                DangHoatDong = r.GetBoolean(r.GetOrdinal("DangHoatDong")),
                LoaiCongThucText = FormatFormula((LoaiCongThuc)r.GetByte(r.GetOrdinal("LoaiCongThuc"))),
                NgayTao = r.GetDateTime(r.GetOrdinal("NgayTao"))
            }, parameters.ToArray());
            PopulateAssignmentItemFrequencies(items);
            return items;
        }

        // Lấy danh sách phân công chỉ số theo bộ lọc, trạng thái hoạt động và phạm vi quyền đang áp dụng.
        public IList<AssignmentItemViewModel> GetAll(int? khoaPhongId = null)
        {
            return GetAll(khoaPhongId, null, null, null, 1, 999999);
        }

        // Điền dữ liệu suy ra hoặc dữ liệu liên quan vào model của phân công chỉ số.
        private void PopulateAssignmentItemFrequencies(IList<AssignmentItemViewModel> items)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            var frequencyTexts = GetFrequencyTextByIndicatorIds(items.Select(x => x.ChiSoChatLuongId));
            foreach (var item in items)
            {
                string text;
                item.TanSuatBaoCaoText = frequencyTexts.TryGetValue(item.ChiSoChatLuongId, out text) ? text : string.Empty;
            }
        }

        // Xử lý chức năng phân công chỉ số của method GetFrequencyTextByIndicatorIds, giữ logic nghiệp vụ tập trung trong tầng phù hợp.
        private IDictionary<int, string> GetFrequencyTextByIndicatorIds(IEnumerable<int> indicatorIds)
        {
            var ids = (indicatorIds ?? new int[0]).Distinct().ToList();
            if (ids.Count == 0)
            {
                return new Dictionary<int, string>();
            }

            var idPlaceholders = new List<string>();
            var parameters = new List<SqlParameter>();
            for (var i = 0; i < ids.Count; i++)
            {
                var paramName = "@Id" + i;
                parameters.Add(Param(paramName, ids[i]));
                idPlaceholders.Add(paramName);
            }

            var sql = @"
SELECT ChiSoChatLuongId, TanSuatBaoCao
FROM dbo.ChiSoTanSuatBaoCao
WHERE ChiSoChatLuongId IN (" + string.Join(",", idPlaceholders) + @")
ORDER BY ChiSoChatLuongId, TanSuatBaoCao";

            var frequencies = Query(sql, r => new
            {
                ChiSoChatLuongId = Int(r, "ChiSoChatLuongId"),
                TanSuatBaoCao = (TanSuatBaoCao)r.GetByte(r.GetOrdinal("TanSuatBaoCao"))
            }, parameters.ToArray());

            return frequencies
                .GroupBy(x => x.ChiSoChatLuongId)
                .ToDictionary(g => g.Key, g => FormatFrequencies(g.Select(x => x.TanSuatBaoCao)));
        }

        // Xử lý chức năng phân công chỉ số của method GetExportRows, giữ logic nghiệp vụ tập trung trong tầng phù hợp.
        public IList<AssignmentExportRow> GetExportRows(
            int? khoaPhongId = null,
            int? chiSoId = null,
            string trangThai = null,
            string trangThaiPhanCong = null,
            string search = null)
        {
            if (string.Equals(trangThaiPhanCong, "unassigned", StringComparison.OrdinalIgnoreCase))
            {
                return new List<AssignmentExportRow>();
            }

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

            var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
            var sql = @"
SELECT pc.ChiSoChatLuongId, kp.TenKhoaPhong,
       cs.TenChiSo, cs.PhuongPhapTinh, cs.TuSoMoTa, cs.MauSoMoTa, cs.ThuThapTongHop
FROM dbo.PhanCongChiSo pc
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = pc.KhoaPhongId
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = pc.ChiSoChatLuongId
" + whereClause + @"
ORDER BY cs.MaChiSo, kp.TenKhoaPhong";

            var rows = Query(sql, r =>
            {
                return new AssignmentExportRow
                {
                    ChiSoChatLuongId = Int(r, "ChiSoChatLuongId"),
                    TenKhoaPhong = String(r, "TenKhoaPhong"),
                    TenChiSo = String(r, "TenChiSo"),
                    PhuongPhapTinh = String(r, "PhuongPhapTinh"),
                    TuSoMoTa = String(r, "TuSoMoTa"),
                    MauSoMoTa = String(r, "MauSoMoTa"),
                    ThuThapTongHop = String(r, "ThuThapTongHop")
                };
            }, parameters.ToArray());

            PopulateExportFrequencies(rows);
            return rows;
        }

        // Điền dữ liệu suy ra hoặc dữ liệu liên quan vào model của phân công chỉ số.
        private void PopulateExportFrequencies(IList<AssignmentExportRow> rows)
        {
            if (rows == null || rows.Count == 0)
            {
                return;
            }

            var indicatorIds = rows.Select(x => x.ChiSoChatLuongId).Distinct().ToList();
            var idPlaceholders = new List<string>();
            var parameters = new List<SqlParameter>();
            for (var i = 0; i < indicatorIds.Count; i++)
            {
                var paramName = "@Id" + i;
                parameters.Add(Param(paramName, indicatorIds[i]));
                idPlaceholders.Add(paramName);
            }

            var sql = @"
SELECT ChiSoChatLuongId, TanSuatBaoCao
FROM dbo.ChiSoTanSuatBaoCao
WHERE ChiSoChatLuongId IN (" + string.Join(",", idPlaceholders) + @")
ORDER BY ChiSoChatLuongId, TanSuatBaoCao";

            var frequencies = Query(sql, r => new
            {
                ChiSoChatLuongId = Int(r, "ChiSoChatLuongId"),
                TanSuatBaoCao = (TanSuatBaoCao)r.GetByte(r.GetOrdinal("TanSuatBaoCao"))
            }, parameters.ToArray());

            var groups = frequencies
                .GroupBy(x => x.ChiSoChatLuongId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.TanSuatBaoCao).ToList());

            foreach (var row in rows)
            {
                List<TanSuatBaoCao> rowFrequencies;
                if (groups.TryGetValue(row.ChiSoChatLuongId, out rowFrequencies) && rowFrequencies.Count > 0)
                {
                    row.TanSuatBaoCaoText = FormatFrequencies(rowFrequencies);
                }
            }
        }
    }
}
