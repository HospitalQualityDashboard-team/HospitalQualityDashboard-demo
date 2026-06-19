// Mục đích: tạo nội dung cảnh báo chỉ số theo ngày hạn nộp.
using HospitalQualityDashboardDemo.Models.Enums;
using System;

namespace HospitalQualityDashboardDemo.Services
{
    public sealed class IndicatorWarningMessage
    {
        public LoaiThongBao NotificationType { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
    }

    public static class IndicatorWarningMessageBuilder
    {
        public static IndicatorWarningMessage Build(
            string indicatorCode,
            string indicatorName,
            DateTime dueDate,
            DateTime warningDate)
        {
            var daysUntilDue = (dueDate.Date - warningDate.Date).Days;
            var title = string.Format(
                "Cảnh báo chỉ số {0} - {1}",
                indicatorCode,
                indicatorName);

            if (daysUntilDue > 0)
            {
                return Create(
                    LoaiThongBao.NhacHan,
                    title,
                    string.Format(
                        "Chỉ số {0} - {1}: Còn {2} ngày đến hạn nộp. Vui lòng chuẩn bị và nộp đúng hạn.",
                        indicatorCode,
                        indicatorName,
                        daysUntilDue));
            }

            if (daysUntilDue == 0)
            {
                return Create(
                    LoaiThongBao.HanNopHomNay,
                    title,
                    string.Format(
                        "Chỉ số {0} - {1}: Hôm nay là hạn nộp. Vui lòng nộp trước khi hết ngày.",
                        indicatorCode,
                        indicatorName));
            }

            return Create(
                LoaiThongBao.QuaHan,
                title,
                string.Format(
                    "Chỉ số {0} - {1}: Đã quá hạn nộp {2} ngày. Vui lòng cập nhật ngay để tránh ảnh hưởng đến tiến độ.",
                    indicatorCode,
                    indicatorName,
                    Math.Abs(daysUntilDue)));
        }

        private static IndicatorWarningMessage Create(
            LoaiThongBao type,
            string title,
            string body)
        {
            return new IndicatorWarningMessage
            {
                NotificationType = type,
                Title = title,
                Body = body
            };
        }
    }
}
