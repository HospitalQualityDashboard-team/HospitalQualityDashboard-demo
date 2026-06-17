// Mục đích: xử lý kỳ báo cáo, tạo lịch kỳ báo cáo tự động và các chức năng liên quan.
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
        public IList<KyBaoCaoViewModel> GetAll()
        {
            const string sql = @"
SELECT ky.KyBaoCaoId, ky.TenKyBaoCao, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay, ky.HanNop, ky.TrangThai,
       COUNT(bc.BaoCaoId) AS TongBaoCao,
       ISNULL(SUM(CASE WHEN bc.TrangThai IN (2,3,4) THEN 1 ELSE 0 END), 0) AS DaGui
FROM dbo.KyBaoCao ky
LEFT JOIN dbo.BaoCao bc ON bc.KyBaoCaoId = ky.KyBaoCaoId
GROUP BY ky.KyBaoCaoId, ky.TenKyBaoCao, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay, ky.HanNop, ky.TrangThai
ORDER BY ky.TuNgay DESC";
            return Query(sql, MapPeriod);
        }

        public IList<KyBaoCaoViewModel> GetAll(int page, int pageSize, out int totalItems)
        {
            page = NormalizePage(page);
            pageSize = NormalizePageSize(pageSize);
            totalItems = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.KyBaoCao"));

            const string sql = @"
SELECT ky.KyBaoCaoId, ky.TenKyBaoCao, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay, ky.HanNop, ky.TrangThai,
       COUNT(bc.BaoCaoId) AS TongBaoCao,
       ISNULL(SUM(CASE WHEN bc.TrangThai IN (2,3,4) THEN 1 ELSE 0 END), 0) AS DaGui
FROM dbo.KyBaoCao ky
LEFT JOIN dbo.BaoCao bc ON bc.KyBaoCaoId = ky.KyBaoCaoId
GROUP BY ky.KyBaoCaoId, ky.TenKyBaoCao, ky.LoaiKyBaoCao, ky.TuNgay, ky.DenNgay, ky.HanNop, ky.TrangThai
ORDER BY ky.TuNgay DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            return Query(sql, MapPeriod,
                Param("@Offset", (page - 1) * pageSize),
                Param("@PageSize", pageSize));
        }

        public IList<TanSuatBaoCao> GetFrequenciesForDepartment(int departmentId)
        {
            const string sql = @"
SELECT DISTINCT tsb.TanSuatBaoCao
FROM dbo.PhanCongChiSo pc
INNER JOIN dbo.ChiSoTanSuatBaoCao tsb ON tsb.ChiSoChatLuongId = pc.ChiSoChatLuongId
WHERE pc.KhoaPhongId = @KhoaPhongId AND pc.DangHoatDong = 1";

            return Query(sql, r => (TanSuatBaoCao)r.GetByte(0), Param("@KhoaPhongId", departmentId));
        }

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

        public bool IsOpenForDepartment(int periodId, int departmentId)
        {
            var count = Convert.ToInt32(Scalar(@"
SELECT COUNT(DISTINCT ky.KyBaoCaoId)
FROM dbo.KyBaoCao ky
INNER JOIN dbo.PhanCongChiSo pc ON pc.KhoaPhongId=@KhoaPhongId AND pc.DangHoatDong=1
INNER JOIN dbo.ChiSoTanSuatBaoCao cst ON cst.ChiSoChatLuongId = pc.ChiSoChatLuongId AND cst.TanSuatBaoCao = ky.LoaiKyBaoCao
WHERE ky.KyBaoCaoId=@KyBaoCaoId AND ky.TrangThai=@Mo",
                Param("@KyBaoCaoId", periodId),
                Param("@KhoaPhongId", departmentId),
                Param("@Mo", (byte)TrangThaiKyBaoCao.Mo)));

            return count > 0;
        }

        public KyBaoCaoViewModel Get(int id)
        {
            return QuerySingle(@"SELECT KyBaoCaoId, TenKyBaoCao, LoaiKyBaoCao, TuNgay, DenNgay, HanNop, TrangThai, 0 AS TongBaoCao, 0 AS DaGui FROM dbo.KyBaoCao WHERE KyBaoCaoId=@Id",
                MapPeriod, Param("@Id", id));
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

        public void SetStatus(int id, TrangThaiKyBaoCao status)
        {
            Execute("UPDATE dbo.KyBaoCao SET TrangThai=@TrangThai, NgayCapNhat=GETDATE() WHERE KyBaoCaoId=@Id", Param("@TrangThai", (byte)status), Param("@Id", id));
            DropdownCache.Remove("dropdown:periods");
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
                throw new InvalidOperationException("Kỳ báo cáo đã có dữ liệu liên quan, vui lòng khóa thay vì xóa.");
            }

            Execute("DELETE FROM dbo.KyBaoCao WHERE KyBaoCaoId=@Id", Param("@Id", id));
            DropdownCache.Remove("dropdown:periods");
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

        private static int NormalizePage(int page)
        {
            return page < 1 ? 1 : page;
        }

        private static int NormalizePageSize(int pageSize)
        {
            if (pageSize < 1) return 20;
            return pageSize > 100 ? 100 : pageSize;
        }
    }

    public class ReportingPeriodScheduleService : DbServiceBase
    {
        private static readonly TanSuatBaoCao[] SchedulableFrequencies =
        {
            TanSuatBaoCao.HangNgay,
            TanSuatBaoCao.HangThang,
            TanSuatBaoCao.HangQuy,
            TanSuatBaoCao.SauThang,
            TanSuatBaoCao.ChinThang,
            TanSuatBaoCao.HangNam
        };

        public ReportingPeriodScheduleRequestViewModel CreateDefaultRequest()
        {
            return PopulateOptions(new ReportingPeriodScheduleRequestViewModel());
        }

        public ReportingPeriodScheduleRequestViewModel PopulateOptions(ReportingPeriodScheduleRequestViewModel model)
        {
            if (model == null)
            {
                model = new ReportingPeriodScheduleRequestViewModel();
            }

            model.FrequencyOptions = SchedulableFrequencies
                .Select(x => new SelectListItem
                {
                    Value = ((int)x).ToString(CultureInfo.InvariantCulture),
                    Text = FormatFrequencyForSchedule(x),
                    Selected = model.SelectedFrequencyValues != null && model.SelectedFrequencyValues.Contains((int)x)
                })
                .ToList();

            model.StatusOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = ((byte)TrangThaiKyBaoCao.Nhap).ToString(CultureInfo.InvariantCulture), Text = "Nhập", Selected = model.DefaultStatus == TrangThaiKyBaoCao.Nhap },
                new SelectListItem { Value = ((byte)TrangThaiKyBaoCao.Mo).ToString(CultureInfo.InvariantCulture), Text = "Mở", Selected = model.DefaultStatus == TrangThaiKyBaoCao.Mo },
                new SelectListItem { Value = ((byte)TrangThaiKyBaoCao.Khoa).ToString(CultureInfo.InvariantCulture), Text = "Khóa", Selected = model.DefaultStatus == TrangThaiKyBaoCao.Khoa }
            };

            if (model.PreviewItems == null)
            {
                model.PreviewItems = new List<ReportingPeriodSchedulePreviewItemViewModel>();
            }

            return model;
        }

        public IList<ReportingPeriodSchedulePreviewItemViewModel> BuildSchedulePreview(ReportingPeriodScheduleRequestViewModel request, DateTime now)
        {
            ValidateScheduleRequest(request);
            request.DueDayOffset = 0;
            var today = now.Date;
            var items = new List<ReportingPeriodSchedulePreviewItemViewModel>();

            foreach (var frequency in GetSelectedFrequencies(request))
            {
                switch (frequency)
                {
                    case TanSuatBaoCao.HangNgay:
                        var day = new DateTime(request.Year, 1, 1);
                        var lastDay = new DateTime(request.Year, 12, 31);
                        while (day <= lastDay)
                        {
                            AddPeriod(items, string.Format("Ngày {0:dd/MM/yyyy}", day), frequency, day, day, request.DueDayOffset, today);
                            day = day.AddDays(1);
                        }
                        break;
                    case TanSuatBaoCao.HangThang:
                        for (var month = 1; month <= 12; month++)
                        {
                            var start = new DateTime(request.Year, month, 1);
                            var end = start.AddMonths(1).AddDays(-1);
                            AddPeriod(items, string.Format("Tháng {0:00}/{1}", month, request.Year), frequency, start, end, request.DueDayOffset, today);
                        }
                        break;
                    case TanSuatBaoCao.HangQuy:
                        for (var quarter = 1; quarter <= 4; quarter++)
                        {
                            var startMonth = (quarter - 1) * 3 + 1;
                            var start = new DateTime(request.Year, startMonth, 1);
                            var end = start.AddMonths(3).AddDays(-1);
                            AddPeriod(items, string.Format("Quý {0}/{1}", ToRomanQuarter(quarter), request.Year), frequency, start, end, request.DueDayOffset, today);
                        }
                        break;
                    case TanSuatBaoCao.SauThang:
                        AddPeriod(items, string.Format("6 tháng đầu năm {0}", request.Year), frequency, new DateTime(request.Year, 1, 1), new DateTime(request.Year, 6, 30), request.DueDayOffset, today);
                        AddPeriod(items, string.Format("6 tháng cuối năm {0}", request.Year), frequency, new DateTime(request.Year, 7, 1), new DateTime(request.Year, 12, 31), request.DueDayOffset, today);
                        break;
                    case TanSuatBaoCao.ChinThang:
                        AddPeriod(items, string.Format("9 tháng năm {0}", request.Year), frequency, new DateTime(request.Year, 1, 1), new DateTime(request.Year, 9, 30), request.DueDayOffset, today);
                        break;
                    case TanSuatBaoCao.HangNam:
                        AddPeriod(items, string.Format("Năm {0}", request.Year), frequency, new DateTime(request.Year, 1, 1), new DateTime(request.Year, 12, 31), request.DueDayOffset, today);
                        break;
                }
            }

            foreach (var item in items)
            {
                item.AlreadyExists = PeriodExists(item.LoaiKyBaoCao, item.TuNgay, item.DenNgay);
            }

            return items
                .OrderBy(x => x.TuNgay)
                .ThenBy(x => GetFrequencyOrder(x.LoaiKyBaoCao))
                .ToList();
        }

        public ReportingPeriodScheduleResultViewModel GenerateSchedule(ReportingPeriodScheduleDto dto, DateTime now)
        {
            return GenerateSchedule(new ReportingPeriodScheduleRequestViewModel
            {
                Year = dto.Year,
                SelectedFrequencyValues = dto.SelectedFrequencyValues,
                DueDayOffset = dto.DueDayOffset,
                DefaultStatus = dto.DefaultStatus
            }, now);
        }

        public ReportingPeriodScheduleResultViewModel GenerateSchedule(ReportingPeriodScheduleRequestViewModel request, DateTime now)
        {
            var preview = BuildSchedulePreview(request, now);
            var result = new ReportingPeriodScheduleResultViewModel
            {
                TotalPreviewed = preview.Count,
                SkippedExistingCount = preview.Count(x => x.AlreadyExists)
            };

            foreach (var item in preview.Where(x => !x.AlreadyExists))
            {
                if (PeriodExists(item.LoaiKyBaoCao, item.TuNgay, item.DenNgay))
                {
                    result.SkippedExistingCount++;
                    continue;
                }

                InsertPeriod(item);
                result.CreatedCount++;
            }

            result.OpenedCount = OpenDuePeriods(now);
            DropdownCache.Remove("dropdown:periods");
            return result;
        }

        public int OpenDuePeriods(DateTime now)
        {
            const string sql = @"
UPDATE dbo.KyBaoCao
SET TrangThai=@Mo, NgayCapNhat=GETDATE()
WHERE TrangThai=@Nhap AND TuNgay <= @Today";

            var openedCount = Execute(sql,
                Param("@Mo", (byte)TrangThaiKyBaoCao.Mo),
                Param("@Nhap", (byte)TrangThaiKyBaoCao.Nhap),
                Param("@Today", now.Date));
            if (openedCount > 0)
            {
                DropdownCache.Remove("dropdown:periods");
            }

            return openedCount;
        }

        private static void ValidateScheduleRequest(ReportingPeriodScheduleRequestViewModel request)
        {
            if (request == null)
            {
                throw new InvalidOperationException("Vui lòng nhập thông tin tạo lịch kỳ báo cáo.");
            }

            if (request.Year < 2000 || request.Year > 2100)
            {
                throw new InvalidOperationException("Năm phải nằm trong khoảng 2000 đến 2100.");
            }

            if (request.DueDayOffset < 0 || request.DueDayOffset > 365)
            {
                throw new InvalidOperationException("Số ngày hạn nộp phải từ 0 đến 365.");
            }

            if (!GetSelectedFrequencies(request).Any())
            {
                throw new InvalidOperationException("Vui lòng chọn ít nhất một loại kỳ báo cáo.");
            }
        }

        private static IList<TanSuatBaoCao> GetSelectedFrequencies(ReportingPeriodScheduleRequestViewModel request)
        {
            if (request.SelectedFrequencyValues == null)
            {
                return new List<TanSuatBaoCao>();
            }

            return request.SelectedFrequencyValues
                .Where(x => Enum.IsDefined(typeof(TanSuatBaoCao), (byte)x))
                .Select(x => (TanSuatBaoCao)(byte)x)
                .Where(x => SchedulableFrequencies.Contains(x))
                .Distinct()
                .OrderBy(GetFrequencyOrder)
                .ToList();
        }

        private static void AddPeriod(
            IList<ReportingPeriodSchedulePreviewItemViewModel> items,
            string name,
            TanSuatBaoCao frequency,
            DateTime start,
            DateTime end,
            int dueDayOffset,
            DateTime today)
        {
            if (end.Date < today)
            {
                return;
            }

            items.Add(new ReportingPeriodSchedulePreviewItemViewModel
            {
                TenKyBaoCao = name,
                LoaiKyBaoCao = frequency,
                LoaiKyBaoCaoText = FormatFrequencyForSchedule(frequency),
                TuNgay = start,
                DenNgay = end,
                HanNop = end.AddDays(dueDayOffset),
                TrangThai = start.Date <= today ? TrangThaiKyBaoCao.Mo : TrangThaiKyBaoCao.Nhap
            });
        }

        private bool PeriodExists(TanSuatBaoCao frequency, DateTime start, DateTime end)
        {
            const string sql = @"
SELECT COUNT(*)
FROM dbo.KyBaoCao
WHERE LoaiKyBaoCao=@LoaiKyBaoCao AND TuNgay=@TuNgay AND DenNgay=@DenNgay";

            return Convert.ToInt32(Scalar(sql,
                Param("@LoaiKyBaoCao", (byte)frequency),
                Param("@TuNgay", start.Date),
                Param("@DenNgay", end.Date))) > 0;
        }

        private void InsertPeriod(ReportingPeriodSchedulePreviewItemViewModel item)
        {
            Execute(@"INSERT INTO dbo.KyBaoCao(TenKyBaoCao, LoaiKyBaoCao, TuNgay, DenNgay, HanNop, TrangThai)
VALUES(@TenKyBaoCao, @LoaiKyBaoCao, @TuNgay, @DenNgay, @HanNop, @TrangThai)",
                Param("@TenKyBaoCao", item.TenKyBaoCao),
                Param("@LoaiKyBaoCao", (byte)item.LoaiKyBaoCao),
                Param("@TuNgay", item.TuNgay.Date),
                Param("@DenNgay", item.DenNgay.Date),
                Param("@HanNop", item.HanNop.Date),
                Param("@TrangThai", (byte)item.TrangThai));
        }

        private static int GetFrequencyOrder(TanSuatBaoCao frequency)
        {
            switch (frequency)
            {
                case TanSuatBaoCao.HangNgay: return 5;
                case TanSuatBaoCao.HangThang: return 10;
                case TanSuatBaoCao.HangQuy: return 20;
                case TanSuatBaoCao.SauThang: return 30;
                case TanSuatBaoCao.ChinThang: return 40;
                case TanSuatBaoCao.HangNam: return 50;
                default: return 100;
            }
        }

        private static string FormatFrequencyForSchedule(TanSuatBaoCao frequency)
        {
            switch (frequency)
            {
                case TanSuatBaoCao.HangNgay: return "Hàng ngày";
                case TanSuatBaoCao.HangThang: return "Hàng tháng";
                case TanSuatBaoCao.HangQuy: return "Hàng quý";
                case TanSuatBaoCao.SauThang: return "6 tháng";
                case TanSuatBaoCao.ChinThang: return "9 tháng";
                case TanSuatBaoCao.HangNam: return "Hàng năm";
                default: return frequency.ToString();
            }
        }

        private static string ToRomanQuarter(int quarter)
        {
            switch (quarter)
            {
                case 1: return "I";
                case 2: return "II";
                case 3: return "III";
                case 4: return "IV";
                default: return quarter.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}
