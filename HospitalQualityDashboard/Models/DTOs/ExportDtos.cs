namespace HospitalQualityDashboard.Models.DTOs
{
    public class EmployeeExportQueryDto
    {
        public int? DepartmentId { get; set; }
        public bool IsAdmin { get; set; }
        public int? CurrentDepartmentId { get; set; }
    }

    public class ReportExportQueryDto
    {
        public int? PeriodId { get; set; }
        public int? DepartmentId { get; set; }
        public int? IndicatorId { get; set; }
        public bool IsAdmin { get; set; }
        public int? CurrentDepartmentId { get; set; }
    }

    public class AssignmentExportQueryDto
    {
        public int? DepartmentId { get; set; }
        public int? IndicatorId { get; set; }
        public string Status { get; set; }
        public string AssignmentStatus { get; set; }
        public string Search { get; set; }
        public string[] Columns { get; set; }
    }
}
