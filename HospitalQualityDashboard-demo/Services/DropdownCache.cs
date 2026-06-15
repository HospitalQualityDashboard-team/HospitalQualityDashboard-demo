using System;
using System.Web;
using System.Web.Caching;

namespace HospitalQualityDashboardDemo.Services
{
    internal static class DropdownCache
    {
        private static readonly TimeSpan Duration = TimeSpan.FromMinutes(5);

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
                cache.Insert(key, value, null, DateTime.UtcNow.Add(Duration), Cache.NoSlidingExpiration);
            }

            return value;
        }

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
