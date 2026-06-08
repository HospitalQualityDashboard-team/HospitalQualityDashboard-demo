namespace HospitalQualityDashboard.Models.DTOs
{
    public class AssignmentQueryDto
    {
        public int? KhoaPhongId { get; set; }
        public int? ChiSoId { get; set; }
        public string TrangThai { get; set; }
        public string TrangThaiPhanCong { get; set; }
        public string Search { get; set; }
        public string ViewMode { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class AssignmentCommandDto
    {
        public int[] DepartmentIds { get; set; }
        public int[] IndicatorIds { get; set; }
        public int CurrentUserId { get; set; }
    }

    public class AssignmentBulkActionDto
    {
        public int[] Ids { get; set; }
        public string ViewMode { get; set; }
    }
}
