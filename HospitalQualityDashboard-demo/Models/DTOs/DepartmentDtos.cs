namespace HospitalQualityDashboardDemo.Models.DTOs
{
    public class DepartmentSaveDto
    {
        public int KhoaPhongId { get; set; }
        public int IdKhoaPhongNguon { get; set; }
        public string TenKhoaPhong { get; set; }
        public bool Used { get; set; }
        public string GhiChu { get; set; }
    }

    public class DepartmentImportDto
    {
        public System.Web.HttpPostedFileBase File { get; set; }
        public int UserId { get; set; }
    }
}
