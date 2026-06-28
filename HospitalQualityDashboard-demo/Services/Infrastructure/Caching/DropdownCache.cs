// Mục đích: cache ngắn hạn dữ liệu dropdown dùng chung để giảm số lượt truy vấn Azure SQL.
using System;
using System.Web;
using System.Web.Caching;

namespace HospitalQualityDashboardDemo.Services
{
    internal static class DropdownCache
    {
        private static readonly TimeSpan Duration = TimeSpan.FromMinutes(5);

        // Lấy dữ liệu dropdown từ cache nếu còn hạn; khi cache trống mới gọi factory để truy vấn database.
        public static T GetOrAdd<T>(string key, Func<T> factory) where T : class
        {
            var cache = HttpRuntime.Cache;
            var value = cache == null ? null : cache[key] as T;
            if (value != null)
            {
                return value;
            }

            value = factory();
            if (cache != null)
            {
                // Hết hạn tuyệt đối giúp danh mục được làm mới định kỳ dù có truy cập liên tục.
                cache.Insert(key, value, null, DateTime.UtcNow.Add(Duration), Cache.NoSlidingExpiration);
            }

            return value;
        }

        // Loại bỏ dữ liệu đã chọn khỏi cache dữ liệu danh mục.
        public static void Remove(string key)
        {
            var cache = HttpRuntime.Cache;
            if (cache != null)
            {
                cache.Remove(key);
            }
        }
    }
}
