// Mục đích: cung cấp một điểm đọc và kiểm tra connection string dùng chung cho tầng truy cập dữ liệu.
using System;
using System.Configuration;

namespace HospitalQualityDashboardDemo.Services
{
    internal static class DatabaseConfiguration
    {
        public const string ConnectionName = "HospitalQualityConnection";

        // Đọc connection string bắt buộc và báo lỗi khi cấu hình bị thiếu.
        public static string GetConnectionString()
        {
            var settings = ConfigurationManager.ConnectionStrings[ConnectionName];
            if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                throw new InvalidOperationException("Missing connection string: " + ConnectionName + ".");
            }

            return settings.ConnectionString;
        }
    }
}
