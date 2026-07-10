// Mục đích: gom nhóm phân công chỉ số theo khoa/phòng và tần suất để hiển thị quản trị.
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
            string sql = "SELECT COUNT(*) FROM dbo.ChiSoChatLuong cs " + whereClause;

            return Convert.ToInt32(Scalar(sql, parameters.ToArray()));
        }

        // Xử lý chức năng phân công chỉ số của method GetAllIndicatorGroups, giữ logic nghiệp vụ tập trung trong tầng phù hợp.
        public IList<IndicatorAssignmentGroup> GetAllIndicatorGroups(
            string trangThaiPhanCong = null,
            string search = null,
            int page = 1,
            int pageSize = 10,
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

            string indicatorSql = @"
SELECT cs.ChiSoChatLuongId, cs.MaChiSo, cs.TenChiSo, cs.LoaiCongThuc
FROM dbo.ChiSoChatLuong cs
" + whereClause + @"
ORDER BY ISNULL(cs.SoThuTu, 9999), cs.MaChiSo
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var indicators = Query(indicatorSql, r => new
            {
                ChiSoChatLuongId = Int(r, "ChiSoChatLuongId"),
                MaChiSo = String(r, "MaChiSo"),
                TenChiSo = String(r, "TenChiSo"),
                LoaiCongThuc = (LoaiCongThuc)r.GetByte(r.GetOrdinal("LoaiCongThuc"))
            }, parameters.ToArray());

            if (indicators.Count == 0)
            {
                return new List<IndicatorAssignmentGroup>();
            }

            var indicatorIds = indicators.Select(x => x.ChiSoChatLuongId).ToList();
            var assignmentsParams = new List<SqlParameter>();
            var assignmentsConditions = new List<string>();
            var idPlaceholders = new List<string>();
            for (var i = 0; i < indicatorIds.Count; i++)
            {
                var paramName = "@IndId" + i;
                assignmentsParams.Add(Param(paramName, indicatorIds[i]));
                idPlaceholders.Add(paramName);
            }
            assignmentsConditions.Add("pc.ChiSoChatLuongId IN (" + string.Join(",", idPlaceholders) + ")");

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
            string assignmentsSql = @"
SELECT pc.PhanCongChiSoId, pc.KhoaPhongId, pc.ChiSoChatLuongId, kp.TenKhoaPhong,
       cs.MaChiSo, cs.TenChiSo, cs.LoaiCongThuc, pc.DangHoatDong, pc.NgayTao
FROM dbo.PhanCongChiSo pc
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = pc.KhoaPhongId
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = pc.ChiSoChatLuongId
" + assignmentsWhere + @"
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
                LoaiCongThucText = FormatFormula((LoaiCongThuc)r.GetByte(r.GetOrdinal("LoaiCongThuc"))),
                NgayTao = r.GetDateTime(r.GetOrdinal("NgayTao"))
            }, assignmentsParams.ToArray());
            PopulateAssignmentItemFrequencies(assignments);
            var frequencyTexts = GetFrequencyTextByIndicatorIds(indicatorIds);

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
                    TanSuatBaoCaoText = frequencyTexts.ContainsKey(ind.ChiSoChatLuongId) ? frequencyTexts[ind.ChiSoChatLuongId] : string.Empty,
                    LoaiCongThucText = FormatFormula(ind.LoaiCongThuc),
                    SoKhoaPhong = indAssignments.Count(x => x.DangHoatDong),
                    ChuaPhanCong = isUnassigned,
                    Items = indAssignments
                });
            }

            return groups;
        }

        // Xử lý chức năng phân công chỉ số của method GetDepartmentsCount, giữ logic nghiệp vụ tập trung trong tầng phù hợp.
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
            string sql = "SELECT COUNT(*) FROM dbo.KhoaPhong kp " + whereClause;

            return Convert.ToInt32(Scalar(sql, parameters.ToArray()));
        }

        // Xử lý chức năng phân công chỉ số của method GetAllDepartmentGroups, giữ logic nghiệp vụ tập trung trong tầng phù hợp.
        public IList<DepartmentAssignmentGroup> GetAllDepartmentGroups(
            string search = null,
            int page = 1,
            int pageSize = 10,
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

            string deptSql = @"
SELECT kp.KhoaPhongId, kp.TenKhoaPhong
FROM dbo.KhoaPhong kp
" + whereClause + @"
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
            var assignmentsConditions = new List<string>();
            var deptPlaceholders = new List<string>();
            for (var i = 0; i < deptIds.Count; i++)
            {
                var paramName = "@DeptId" + i;
                assignmentsParams.Add(Param(paramName, deptIds[i]));
                deptPlaceholders.Add(paramName);
            }
            assignmentsConditions.Add("pc.KhoaPhongId IN (" + string.Join(",", deptPlaceholders) + ")");

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
            string assignmentsSql = @"
SELECT pc.PhanCongChiSoId, pc.KhoaPhongId, pc.ChiSoChatLuongId, kp.TenKhoaPhong,
       cs.MaChiSo, cs.TenChiSo, cs.LoaiCongThuc, pc.DangHoatDong, pc.NgayTao
FROM dbo.PhanCongChiSo pc
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = pc.KhoaPhongId
INNER JOIN dbo.ChiSoChatLuong cs ON cs.ChiSoChatLuongId = pc.ChiSoChatLuongId
" + assignmentsWhere + @"
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
                LoaiCongThucText = FormatFormula((LoaiCongThuc)r.GetByte(r.GetOrdinal("LoaiCongThuc"))),
                NgayTao = r.GetDateTime(r.GetOrdinal("NgayTao"))
            }, assignmentsParams.ToArray());
            PopulateAssignmentItemFrequencies(assignments);

            var groups = new List<DepartmentAssignmentGroup>();
            foreach (var dept in departments)
            {
                var deptAssignments = assignments.Where(x => x.KhoaPhongId == dept.KhoaPhongId).ToList();
                groups.Add(new DepartmentAssignmentGroup
                {
                    KhoaPhongId = dept.KhoaPhongId,
                    TenKhoaPhong = dept.TenKhoaPhong,
                    SoChiSo = deptAssignments.Count,
                    SoChiSoHoatDong = deptAssignments.Count(x => x.DangHoatDong),
                    Items = deptAssignments
                });
            }

            return groups;
        }

        // Xử lý chức năng phân công chỉ số của method GetStatistics, giữ logic nghiệp vụ tập trung trong tầng phù hợp.
        public AssignmentViewModel GetStatistics()
        {
            var model = new AssignmentViewModel();
            model.TongChiSo = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.ChiSoChatLuong WHERE DangHoatDong=1"));
            model.TongPhanCong = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.PhanCongChiSo WHERE DangHoatDong=1"));
            model.SoChiSoChuaPhanCong = Convert.ToInt32(Scalar(@"
SELECT COUNT(*) FROM dbo.ChiSoChatLuong cs WHERE cs.DangHoatDong=1
AND NOT EXISTS (SELECT 1 FROM dbo.PhanCongChiSo pc WHERE pc.ChiSoChatLuongId=cs.ChiSoChatLuongId AND pc.DangHoatDong=1)"));
            model.SoChiSoDaPhanCong = model.TongPhanCong - model.SoChiSoChuaPhanCong;
            model.SoTamDung = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.PhanCongChiSo WHERE DangHoatDong=0"));
            return model;
        }

        // Tạo dữ liệu xem trước để người dùng kiểm tra trước khi ghi chính thức.
    }
}
