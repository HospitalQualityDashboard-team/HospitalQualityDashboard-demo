// Mục đích: định nghĩa bộ lọc tra cứu nhật ký thao tác hệ thống.
using System;

namespace HospitalQualityDashboardDemo.Models.DTOs
{
    public class SystemLogQueryDto
    {
        public string AccountKeyword { get; set; }
        public string Module { get; set; }
        public string LogAction { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
