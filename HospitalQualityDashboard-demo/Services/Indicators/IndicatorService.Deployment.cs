using HospitalQualityDashboardDemo.Models.Enums;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace HospitalQualityDashboardDemo.Services
{
    public partial class IndicatorService
    {
        public void StopDeployment(int id, int userId)
        {
            var today = GetVietnamLocalNow().Date;
            ExecuteInTransaction((connection, transaction) =>
            {
                EnsureDeploymentHistory(connection, transaction);

                Execute(connection, transaction, @"
UPDATE dbo.LichSuTrienKhaiChiSo
SET DenNgayApDung = @Today,
    NguoiKetThucId = @UserId,
    NgayKetThuc = GETDATE()
WHERE ChiSoChatLuongId = @Id
  AND DenNgayApDung IS NULL",
                    Param("@Id", id),
                    Param("@UserId", userId),
                    Param("@Today", today));

                Execute(connection, transaction, @"
UPDATE dbo.ChiSoChatLuong
SET DangHoatDong = 0,
    NgayCapNhat = GETDATE()
WHERE ChiSoChatLuongId = @Id",
                    Param("@Id", id));

                LogIndicatorDeploymentAction(connection, transaction, userId, id, "NgungTrienKhai", today);
            });

            DropdownCache.Remove("dropdown:indicators");
        }

        public void Deploy(int id, int userId)
        {
            var today = GetVietnamLocalNow().Date;
            ExecuteInTransaction((connection, transaction) =>
            {
                EnsureDeploymentHistory(connection, transaction);

                Execute(connection, transaction, @"
INSERT INTO dbo.LichSuTrienKhaiChiSo(ChiSoChatLuongId, TanSuatBaoCao, TuNgayApDung, DenNgayApDung, NguoiTaoId)
SELECT @Id, cst.TanSuatBaoCao, @Today, NULL, @UserId
FROM dbo.ChiSoTanSuatBaoCao cst
WHERE cst.ChiSoChatLuongId = @Id
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.LichSuTrienKhaiChiSo existing
      WHERE existing.ChiSoChatLuongId = cst.ChiSoChatLuongId
        AND existing.TanSuatBaoCao = cst.TanSuatBaoCao
        AND existing.DenNgayApDung IS NULL
  )",
                    Param("@Id", id),
                    Param("@UserId", userId),
                    Param("@Today", today));

                Execute(connection, transaction, @"
UPDATE dbo.ChiSoChatLuong
SET DangHoatDong = 1,
    NgayCapNhat = GETDATE()
WHERE ChiSoChatLuongId = @Id",
                    Param("@Id", id));

                LogIndicatorDeploymentAction(connection, transaction, userId, id, "TrienKhai", today);
            });

            DropdownCache.Remove("dropdown:indicators");
        }

        private void ReconcileDeploymentHistory(
            SqlConnection connection,
            SqlTransaction transaction,
            int indicatorId,
            IEnumerable<TanSuatBaoCao> frequencies,
            bool active)
        {
            EnsureDeploymentHistory(connection, transaction);
            var today = GetVietnamLocalNow().Date;
            var values = (frequencies ?? new[] { TanSuatBaoCao.HangThang })
                .Distinct()
                .Select(x => (byte)x)
                .ToList();

            if (values.Count == 0)
            {
                values.Add((byte)TanSuatBaoCao.HangThang);
            }

            var placeholders = new List<string>();
            var parameters = new List<SqlParameter>
            {
                Param("@Id", indicatorId),
                Param("@Today", today)
            };

            for (var i = 0; i < values.Count; i++)
            {
                var name = "@Frequency" + i;
                placeholders.Add(name);
                parameters.Add(Param(name, values[i]));
            }

            Execute(connection, transaction, @"
UPDATE dbo.LichSuTrienKhaiChiSo
SET DenNgayApDung = @Today,
    NgayKetThuc = GETDATE()
WHERE ChiSoChatLuongId = @Id
  AND DenNgayApDung IS NULL
  AND TanSuatBaoCao NOT IN (" + string.Join(",", placeholders) + ")",
                parameters.ToArray());

            if (!active)
            {
                Execute(connection, transaction, @"
UPDATE dbo.LichSuTrienKhaiChiSo
SET DenNgayApDung = @Today,
    NgayKetThuc = GETDATE()
WHERE ChiSoChatLuongId = @Id
  AND DenNgayApDung IS NULL",
                    Param("@Id", indicatorId),
                    Param("@Today", today));
                return;
            }

            foreach (var value in values)
            {
                Execute(connection, transaction, @"
IF NOT EXISTS (
    SELECT 1
    FROM dbo.LichSuTrienKhaiChiSo
    WHERE ChiSoChatLuongId = @Id
      AND TanSuatBaoCao = @TanSuatBaoCao
      AND DenNgayApDung IS NULL
)
BEGIN
    INSERT INTO dbo.LichSuTrienKhaiChiSo(ChiSoChatLuongId, TanSuatBaoCao, TuNgayApDung, DenNgayApDung)
    VALUES(@Id, @TanSuatBaoCao, @Today, NULL);
END",
                    Param("@Id", indicatorId),
                    Param("@TanSuatBaoCao", value),
                    Param("@Today", today));
            }
        }

        private void EnsureDeploymentHistory(SqlConnection connection, SqlTransaction transaction)
        {
            Execute(connection, transaction, @"
IF OBJECT_ID('dbo.LichSuTrienKhaiChiSo', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LichSuTrienKhaiChiSo (
        LichSuTrienKhaiChiSoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LichSuTrienKhaiChiSo PRIMARY KEY,
        ChiSoChatLuongId INT NOT NULL,
        TanSuatBaoCao TINYINT NOT NULL,
        TuNgayApDung DATE NOT NULL,
        DenNgayApDung DATE NULL,
        NguoiTaoId INT NULL,
        NgayTao DATETIME NOT NULL CONSTRAINT DF_LichSuTrienKhaiChiSo_NgayTao DEFAULT (GETDATE()),
        NguoiKetThucId INT NULL,
        NgayKetThuc DATETIME NULL
    );
END");
        }

        private void LogIndicatorDeploymentAction(
            SqlConnection connection,
            SqlTransaction transaction,
            int userId,
            int indicatorId,
            string action,
            DateTime effectiveDate)
        {
            Execute(connection, transaction, @"
INSERT INTO dbo.NhatKyHeThong(TaiKhoanId, ChucNang, HanhDong, DoiTuong, DoiTuongId, NoiDung)
VALUES(@TaiKhoanId, N'ChiSoChatLuong', @HanhDong, N'ChiSoChatLuong', @DoiTuongId, @NoiDung)",
                Param("@TaiKhoanId", userId),
                Param("@HanhDong", action),
                Param("@DoiTuongId", indicatorId),
                Param("@NoiDung", "Thay doi trang thai trien khai chi so. Ngay ap dung: " + effectiveDate.ToString("yyyy-MM-dd")));
        }

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
    }
}
