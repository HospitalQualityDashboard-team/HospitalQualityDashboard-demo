using System;

namespace HospitalQualityDashboardDemo.Services
{
    public static class DashboardProgressComparisonBuilder
    {
        public const string OnTime = "OnTime";
        public const string Late = "Late";
        public const string Missing = "Missing";
        public const string OverdueMissing = "OverdueMissing";

        public static string Classify(DateTime? submittedAt, DateTime dueDate, DateTime today)
        {
            if (submittedAt.HasValue)
            {
                return submittedAt.Value.Date <= dueDate.Date ? OnTime : Late;
            }

            return today.Date > dueDate.Date ? OverdueMissing : Missing;
        }

        public static string Compare(string primaryStatus, string previousStatus)
        {
            if (string.IsNullOrWhiteSpace(primaryStatus) || string.IsNullOrWhiteSpace(previousStatus))
            {
                return "Insufficient";
            }

            if (primaryStatus == Missing)
            {
                return "Insufficient";
            }

            var primaryRank = GetRank(primaryStatus);
            var previousRank = GetRank(previousStatus);
            if (primaryRank == 0 || previousRank == 0)
            {
                return "Insufficient";
            }

            if (primaryRank > previousRank) return "Better";
            if (primaryRank < previousRank) return "Worse";
            return "Unchanged";
        }

        public static string FormatStatus(string status)
        {
            switch (status)
            {
                case OnTime: return "Dung han";
                case Late: return "Nop tre";
                case Missing: return "Chua nop";
                case OverdueMissing: return "Qua han chua nop";
                default: return "Khong xac dinh";
            }
        }

        private static int GetRank(string status)
        {
            switch (status)
            {
                case OnTime: return 4;
                case Late: return 3;
                case Missing: return 2;
                case OverdueMissing: return 1;
                default: return 0;
            }
        }
    }
}
