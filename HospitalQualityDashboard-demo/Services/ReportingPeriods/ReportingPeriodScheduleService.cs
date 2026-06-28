// Mục đích: sinh lịch kỳ báo cáo theo tần suất và mở kỳ phù hợp với phân công chỉ số.
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

        // Xử lý chức năng lịch kỳ báo cáo của method CreateDefaultRequest, giữ logic nghiệp vụ tập trung trong tầng phù hợp.
        public ReportingPeriodScheduleRequestViewModel CreateDefaultRequest()
        {
            return PopulateOptions(new ReportingPeriodScheduleRequestViewModel());
        }

        // Nạp các tùy chọn cần thiết vào model của màn hình tạo lịch.
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

        // Dựng cấu trúc dữ liệu lịch kỳ báo cáo từ input đã lọc để tái sử dụng cho truy vấn, view hoặc xuất file.
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

        // Sinh các kỳ báo cáo theo tần suất và khoảng thời gian được yêu cầu.
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

        // Sinh các kỳ báo cáo theo tần suất và khoảng thời gian được yêu cầu.
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

        // Mở các kỳ báo cáo đến hạn theo lịch để khoa/phòng bắt đầu nhập số liệu.
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

        // Kiểm tra cấu hình sinh kỳ báo cáo để tránh tạo lịch thiếu tần suất, sai ngày hoặc không có khoa/phòng.
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

        // Xử lý chức năng lịch kỳ báo cáo của method GetSelectedFrequencies, giữ logic nghiệp vụ tập trung trong tầng phù hợp.
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

        // Xử lý chức năng lịch kỳ báo cáo của method AddPeriod, giữ logic nghiệp vụ tập trung trong tầng phù hợp.
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

        // Kiểm tra bản ghi hoặc đối tượng cần dùng đã tồn tại hay chưa.
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

        // Xử lý chức năng lịch kỳ báo cáo của method InsertPeriod, giữ logic nghiệp vụ tập trung trong tầng phù hợp.
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

        // Xử lý chức năng lịch kỳ báo cáo của method GetFrequencyOrder, giữ logic nghiệp vụ tập trung trong tầng phù hợp.
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

        // Định dạng giá trị theo quy ước hiển thị của lịch kỳ báo cáo.
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

        // Chuyển số quý sang chữ số La Mã dùng trong tên kỳ báo cáo.
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
