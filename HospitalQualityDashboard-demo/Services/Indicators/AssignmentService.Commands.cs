// Mục đích: xử lý lệnh phân công, hủy phân công và cập nhật trạng thái chỉ số cho khoa/phòng.
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
        public PreviewAssignmentResultViewModel Preview(PreviewAssignmentDto dto)
        {
            return Preview(dto.DepartmentIds, dto.IndicatorIds);
        }

        // Tạo dữ liệu xem trước để người dùng kiểm tra trước khi ghi chính thức.
        public PreviewAssignmentResultViewModel Preview(IEnumerable<int> departmentIds, IEnumerable<int> indicatorIds)
        {
            var result = new PreviewAssignmentResultViewModel();
            var deptList = (departmentIds ?? new int[0]).Distinct().ToList();
            var indList = (indicatorIds ?? new int[0]).Distinct().ToList();

            if (deptList.Count == 0 || indList.Count == 0)
            {
                return result;
            }

            var deptPlaceholders = new List<string>();
            var deptParams = new List<SqlParameter>();
            for (var i = 0; i < deptList.Count; i++)
            {
                var paramName = "@DeptId" + i;
                deptParams.Add(Param(paramName, deptList[i]));
                deptPlaceholders.Add(paramName);
            }

            var indPlaceholders = new List<string>();
            var indParams = new List<SqlParameter>();
            for (var i = 0; i < indList.Count; i++)
            {
                var paramName = "@IndId" + i;
                indParams.Add(Param(paramName, indList[i]));
                indPlaceholders.Add(paramName);
            }

            var depts = Query("SELECT KhoaPhongId, TenKhoaPhong FROM dbo.KhoaPhong WHERE Used = 1 AND KhoaPhongId IN (" + string.Join(",", deptPlaceholders) + ")",
                r => new { Id = Int(r, "KhoaPhongId"), Name = String(r, "TenKhoaPhong") },
                deptParams.ToArray());

            var inds = Query("SELECT ChiSoChatLuongId, MaChiSo, TenChiSo FROM dbo.ChiSoChatLuong WHERE ChiSoChatLuongId IN (" + string.Join(",", indPlaceholders) + ")",
                r => new { Id = Int(r, "ChiSoChatLuongId"), Code = String(r, "MaChiSo"), Name = String(r, "TenChiSo") },
                indParams.ToArray());

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

        // Tạo hoặc kích hoạt lại các phân công giữa khoa/phòng và chỉ số.
        public void Assign(AssignmentCommandDto dto)
        {
            Assign(dto.DepartmentIds, dto.IndicatorIds, dto.CurrentUserId);
        }

        // Tạo hoặc kích hoạt lại các phân công giữa khoa/phòng và chỉ số.
        public void Assign(IEnumerable<int> departmentIds, IEnumerable<int> indicatorIds, int currentUserId)
        {
            var departments = (departmentIds ?? new int[0]).Distinct().ToList();
            var indicators = (indicatorIds ?? new int[0]).Distinct().ToList();

            foreach (var departmentId in departments)
            {
                _departments.RequireActiveDepartment(departmentId);

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

        // Đồng bộ phân công từ nguồn thu thập đã cấu hình trên chỉ số.
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

        // Ngừng kích hoạt một hoặc nhiều phân công chỉ số đã chọn.
        public void Deactivate(int id)
        {
            Execute("UPDATE dbo.PhanCongChiSo SET DangHoatDong = 0 WHERE PhanCongChiSoId = @Id", Param("@Id", id));
        }

        // Kích hoạt lại một hoặc nhiều phân công chỉ số đã chọn.
        public void Activate(int id)
        {
            var departmentId = Scalar("SELECT KhoaPhongId FROM dbo.PhanCongChiSo WHERE PhanCongChiSoId = @Id", Param("@Id", id));
            if (departmentId == null)
            {
                return;
            }

            _departments.RequireActiveDepartment(Convert.ToInt32(departmentId));
            Execute("UPDATE dbo.PhanCongChiSo SET DangHoatDong = 1 WHERE PhanCongChiSoId = @Id", Param("@Id", id));
        }

        // Ngừng kích hoạt một hoặc nhiều phân công chỉ số đã chọn.
        public void BulkDeactivate(int[] ids)
        {
            if (ids == null || ids.Length == 0) return;
            var parameters = ids.Select((id, i) => Param("@Id" + i, id)).ToArray();
            var placeholders = string.Join(",", parameters.Select(p => p.ParameterName));
            Execute("UPDATE dbo.PhanCongChiSo SET DangHoatDong = 0 WHERE PhanCongChiSoId IN (" + placeholders + ")", parameters);
        }

        // Kích hoạt lại một hoặc nhiều phân công chỉ số đã chọn.
        public void BulkActivate(int[] ids)
        {
            if (ids == null || ids.Length == 0) return;
            var parameters = ids.Select((id, i) => Param("@Id" + i, id)).ToArray();
            var placeholders = string.Join(",", parameters.Select(p => p.ParameterName));
            var lockedCount = Convert.ToInt32(Scalar(@"
SELECT COUNT(*)
FROM dbo.PhanCongChiSo pc
INNER JOIN dbo.KhoaPhong kp ON kp.KhoaPhongId = pc.KhoaPhongId
WHERE pc.PhanCongChiSoId IN (" + placeholders + @")
  AND kp.Used = 0", parameters));
            if (lockedCount > 0)
            {
                throw new InvalidOperationException("Không thể kích hoạt phân công thuộc khoa/phòng đã khóa.");
            }

            Execute("UPDATE dbo.PhanCongChiSo SET DangHoatDong = 1 WHERE PhanCongChiSoId IN (" + placeholders + ")", parameters);
        }

        // Xóa hàng loạt các phân công chỉ số đã chọn.
        public void BulkDelete(int[] ids)
        {
            if (ids == null || ids.Length == 0) return;

            var parameters = ids.Select((id, i) => Param("@Id" + i, id)).ToArray();
            var placeholders = string.Join(",", parameters.Select(p => p.ParameterName));

            var reportCount = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.BaoCao WHERE PhanCongChiSoId IN (" + placeholders + ")", parameters));
            if (reportCount > 0)
            {
                throw new InvalidOperationException("Một số phân công được chọn đã có báo cáo, vui lòng ngừng kích hoạt thay vì xóa.");
            }

            Execute("DELETE FROM dbo.PhanCongChiSo WHERE PhanCongChiSoId IN (" + placeholders + ")", parameters);
        }

        // Xóa bản ghi được chọn sau khi áp dụng các ràng buộc của phân công chỉ số.
        public void Delete(int id)
        {
            var reportCount = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.BaoCao WHERE PhanCongChiSoId=@Id", Param("@Id", id)));
            if (reportCount > 0)
            {
                throw new InvalidOperationException("Phân công đã có báo cáo, vui lòng ngừng kích hoạt thay vì xóa.");
            }

            Execute("DELETE FROM dbo.PhanCongChiSo WHERE PhanCongChiSoId=@Id", Param("@Id", id));
        }
    }
}
