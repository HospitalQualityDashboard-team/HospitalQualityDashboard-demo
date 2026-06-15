using System;
using System.Configuration;

namespace HospitalQualityDashboardDemo.Services
{
    internal static class DatabaseConfiguration
    {
        public const string ConnectionName = "HospitalQualityConnection";

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
