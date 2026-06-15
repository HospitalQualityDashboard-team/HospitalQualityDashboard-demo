// Mục đích: gom view model cho màn hình nghiệp vụ và dữ liệu truyền sang Razor view.
using HospitalQualityDashboardDemo.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HospitalQualityDashboardDemo.Models.ViewModels
{
    public class KhoaPhongViewModel
    {
        public int KhoaPhongId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã khoa/phòng nguồn.")]
        [Display(Name = "Mã khoa/phòng nguồn")]
        public int IdKhoaPhongNguon { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên khoa/phòng.")]
        [Display(Name = "Tên khoa/phòng")]
        public string TenKhoaPhong { get; set; }

        public bool Used { get; set; }
        public string GhiChu { get; set; }
    }

    public class KhoaPhongIndexViewModel
    {
        public string Search { get; set; }
        public IList<KhoaPhongViewModel> Items { get; set; }
        public ImportResultViewModel ImportResult { get; set; }
    }

    // View model dùng cho form thêm/sửa và hiển thị một nhân viên.
    public class NhanVienViewModel
    {
        // Bằng 0 khi thêm mới, có giá trị khi sửa nhân viên.
        public int NhanVienId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã nhân viên.")]
        [Display(Name = "Mã nhân viên")]
        // Mã nhân viên bắt buộc và thường dùng làm tên đăng nhập mặc định.
        public string MaNhanVien { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [Display(Name = "Họ tên")]
        // Họ tên nhân viên bắt buộc.
        public string HoTen { get; set; }

        [Display(Name = "Ngày sinh")]
        // Thông tin cá nhân và liên hệ.
        public DateTime? NgaySinh { get; set; }
        [Display(Name = "Giới tính")]
        public string GioiTinh { get; set; }
        [Display(Name = "Chức vụ")]
        public string ChucVu { get; set; }
        public string Email { get; set; }
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn khoa/phòng.")]
        [Display(Name = "Khoa/phòng")]
        // Khoa/phòng của nhân viên, bắt buộc chọn trên form.
        public int KhoaPhongId { get; set; }

        // Dữ liệu bổ sung để hiển thị trên danh sách.
        public string TenKhoaPhong { get; set; }
        public bool DangHoatDong { get; set; }
        public bool HasAccount { get; set; }

        // Danh sách khoa/phòng để render dropdown.
        public IList<SelectListItem> KhoaPhongOptions { get; set; }
    }

    // View model cho màn hình danh sách nhân viên.
    public class NhanVienIndexViewModel
    {
        // Khoa/phòng đang được chọn để lọc danh sách.
        public int? KhoaPhongId { get; set; }

        // Dropdown khoa/phòng ở bộ lọc và form import.
        public IList<SelectListItem> KhoaPhongOptions { get; set; }

        // Danh sách nhân viên hiển thị trong bảng.
        public IList<NhanVienViewModel> Items { get; set; }

        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }

        // Kết quả sau khi import file nhân viên.
        public ImportResultViewModel ImportResult { get; set; }
    }

    // View model cho form tạo tài khoản User từ nhân viên.
    public class CreateUserAccountViewModel
    {
        // Id nhân viên sẽ được gán tài khoản.
        public int NhanVienId { get; set; }

        // Tên nhân viên để hiển thị trên form.
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [Display(Name = "Tên đăng nhập")]
        // Tên đăng nhập mới, bắt buộc và không được trùng.
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        [Display(Name = "Mật khẩu")]
        // Mật khẩu được hash trước khi lưu vào database.
        public string MatKhau { get; set; }
    }

    public class ChiSoViewModel
    {
        public int ChiSoChatLuongId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã chỉ số.")]
        [Display(Name = "Mã chỉ số")]
        public string MaChiSo { get; set; }

        [Display(Name = "Số thứ tự")]
        public int? SoThuTu { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên chỉ số.")]
        [Display(Name = "Tên chỉ số")]
        public string TenChiSo { get; set; }

        [Display(Name = "Định nghĩa")]
        public string DinhNghia { get; set; }
        [Display(Name = "Lĩnh vực áp dụng")]
        public string LinhVucApDung { get; set; }
        [Display(Name = "Khía cạnh chất lượng")]
        public string KhiaCanhChatLuong { get; set; }
        [Display(Name = "Thành tố chất lượng")]
        public string ThanhToChatLuong { get; set; }
        [Display(Name = "Lý do lựa chọn")]
        public string LyDoLuaChon { get; set; }
        [Display(Name = "Phương pháp tính")]
        public string PhuongPhapTinh { get; set; }
        [Display(Name = "Tử số")]
        public string TuSoMoTa { get; set; }
        [Display(Name = "Mẫu số")]
        public string MauSoMoTa { get; set; }
        [Display(Name = "Nguồn số liệu")]
        public string NguonSoLieu { get; set; }
        [Display(Name = "Thu thập và tổng hợp số liệu")]
        public string ThuThapTongHop { get; set; }
        public int? KhoaPhongThuThapId { get; set; }
        public int? KhoaPhongTongHopId { get; set; }
        [Display(Name = "Giá trị của số liệu")]
        public string GiaTriSoLieu { get; set; }
        public TanSuatBaoCao TanSuatBaoCao { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn ít nhất một tần suất báo cáo.")]
        [Display(Name = "Tần suất báo cáo")]
        public int[] SelectedTanSuatBaoCaoValues { get; set; }
        public IList<TanSuatBaoCao> TanSuatBaoCaos { get; set; }
        public string TanSuatBaoCaoText { get; set; }
        public IList<SelectListItem> TanSuatBaoCaoOptions { get; set; }
        [Display(Name = "Loại công thức")]
        public LoaiCongThuc LoaiCongThuc { get; set; }
        [Display(Name = "Đơn vị tính")]
        public string DonViTinh { get; set; }
        public bool DangHoatDong { get; set; }
        [Display(Name = "Năm mục tiêu")]
        public int? NamMucTieu { get; set; }
        [Display(Name = "Toán tử so sánh")]
        public string ToanTuSoSanh { get; set; }
        [Display(Name = "Giá trị mục tiêu")]
        public decimal? GiaTriMucTieu { get; set; }
        [Display(Name = "Mô tả mục tiêu")]
        public string MoTaMucTieu { get; set; }
    }

    public class ChiSoIndexViewModel
    {
        public IList<ChiSoViewModel> Items { get; set; }
        public ImportResultViewModel ImportResult { get; set; }
    }

    public class AssignmentViewModel
    {
        public int KhoaPhongId { get; set; }
        public int ChiSoChatLuongId { get; set; }
        public int[] SelectedKhoaPhongIds { get; set; }
        public int[] SelectedChiSoIds { get; set; }
        public IList<SelectListItem> KhoaPhongOptions { get; set; }
        public IList<SelectListItem> ChiSoOptions { get; set; }
        public IList<AssignmentItemViewModel> Items { get; set; }
        // Bộ lọc
        public int? FilterKhoaPhongId { get; set; }
        public int? FilterChiSoId { get; set; }
        public string FilterTrangThai { get; set; }
        public string FilterTrangThaiPhanCong { get; set; } // "all"/"assigned"/"unassigned"
        public string Search { get; set; }
        public string ViewMode { get; set; } // "table" | "byDepartment" | "byIndicator"
        // Phân trang
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; } // = 20
        // Thống kê
        public int TongChiSo { get; set; }
        public int SoChiSoDaPhanCong { get; set; }
        public int SoChiSoChuaPhanCong { get; set; }
        public int TongPhanCong { get; set; }
        public int SoTamDung { get; set; }
        // Nhóm cho chế độ xem
        public IList<DepartmentAssignmentGroup> DepartmentGroups { get; set; }
        public IList<IndicatorAssignmentGroup> IndicatorGroups { get; set; }
    }

    public class AssignmentItemViewModel
    {
        public int PhanCongChiSoId { get; set; }
        public string TenKhoaPhong { get; set; }
        public string MaChiSo { get; set; }
        public string TenChiSo { get; set; }
        public bool DangHoatDong { get; set; }
        public int KhoaPhongId { get; set; }
        public int ChiSoChatLuongId { get; set; }
        public string TanSuatBaoCaoText { get; set; }
        public string LoaiCongThucText { get; set; }
        public DateTime NgayTao { get; set; }
    }

    public class AssignmentExportRow
    {
        public int ChiSoChatLuongId { get; set; }
        public string TenChiSo { get; set; }
        public TanSuatBaoCao TanSuatBaoCao { get; set; }
        public string TanSuatBaoCaoText { get; set; }
        public string PhuongPhapTinh { get; set; }
        public string TuSoMoTa { get; set; }
        public string MauSoMoTa { get; set; }
        public string ThuThapTongHop { get; set; }
        public string TenKhoaPhong { get; set; }
    }

    public class DepartmentAssignmentGroup
    {
        public int KhoaPhongId { get; set; }
        public string TenKhoaPhong { get; set; }
        public int SoChiSo { get; set; }
        public int SoChiSoHoatDong { get; set; }
        public int SoChiSoTamDung { get; set; }
        public IList<AssignmentItemViewModel> Items { get; set; }
    }

    public class IndicatorAssignmentGroup
    {
        public int ChiSoChatLuongId { get; set; }
        public string MaChiSo { get; set; }
        public string TenChiSo { get; set; }
        public string TanSuatBaoCaoText { get; set; }
        public string LoaiCongThucText { get; set; }
        public int SoKhoaPhong { get; set; }
        public bool ChuaPhanCong { get; set; }  // true nếu chưa có khoa/phòng nào
        public IList<AssignmentItemViewModel> Items { get; set; }
    }

    public class PreviewAssignmentResultViewModel
    {
        private readonly IList<AssignmentPreviewItem> _items = new List<AssignmentPreviewItem>();

        public int Total => _items.Count;
        public int ExistingCount => _items.Count(x => x.DaTonTai);
        public int NewCount => Total - ExistingCount;
        public IList<AssignmentPreviewItem> Items => _items;

        public void Add(AssignmentPreviewItem item)
        {
            _items.Add(item);
        }
    }

    public class AssignmentPreviewItem
    {
        public string TenKhoaPhong { get; set; }
        public string MaChiSo { get; set; }
        public string TenChiSo { get; set; }
        public bool DaTonTai { get; set; }
    }

    public class KyBaoCaoViewModel
    {
        public int KyBaoCaoId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên kỳ báo cáo.")]
        public string TenKyBaoCao { get; set; }

        public TanSuatBaoCao LoaiKyBaoCao { get; set; }
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }
        public DateTime HanNop { get; set; }
        public TrangThaiKyBaoCao TrangThai { get; set; }
        public string TrangThaiText
        {
            get
            {
                switch (TrangThai)
                {
                    case TrangThaiKyBaoCao.Nhap: return "Nhập";
                    case TrangThaiKyBaoCao.Mo: return "Mở";
                    case TrangThaiKyBaoCao.Khoa: return "Khóa";
                    default: return TrangThai.ToString();
                }
            }
        }

        public int TongBaoCao { get; set; }
        public int DaGui { get; set; }
    }

    public class ReportingPeriodScheduleRequestViewModel
    {
        [Range(2000, 2100, ErrorMessage = "Năm phải nằm trong khoảng 2000 đến 2100.")]
        [Display(Name = "Năm")]
        public int Year { get; set; }

        [Display(Name = "Loại kỳ báo cáo")]
        public int[] SelectedFrequencyValues { get; set; }

        [Range(0, 365, ErrorMessage = "Số ngày hạn nộp phải từ 0 đến 365.")]
        [Display(Name = "Hạn nộp sau ngày kết thúc kỳ")]
        public int DueDayOffset { get; set; }

        [Display(Name = "Trạng thái mặc định")]
        public TrangThaiKyBaoCao DefaultStatus { get; set; }

        public IList<SelectListItem> FrequencyOptions { get; set; }
        public IList<SelectListItem> StatusOptions { get; set; }
        public IList<ReportingPeriodSchedulePreviewItemViewModel> PreviewItems { get; set; }
        public ReportingPeriodScheduleResultViewModel Result { get; set; }

        public ReportingPeriodScheduleRequestViewModel()
        {
            Year = DateTime.Today.Year;
            DueDayOffset = 0;
            DefaultStatus = TrangThaiKyBaoCao.Nhap;
            SelectedFrequencyValues = new[] { (int)TanSuatBaoCao.HangNgay, (int)TanSuatBaoCao.HangThang, (int)TanSuatBaoCao.HangQuy };
            PreviewItems = new List<ReportingPeriodSchedulePreviewItemViewModel>();
        }
    }

    public class ReportingPeriodSchedulePreviewItemViewModel
    {
        public string TenKyBaoCao { get; set; }
        public TanSuatBaoCao LoaiKyBaoCao { get; set; }
        public string LoaiKyBaoCaoText { get; set; }
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }
        public DateTime HanNop { get; set; }
        public TrangThaiKyBaoCao TrangThai { get; set; }
        public bool AlreadyExists { get; set; }

        public string TrangThaiText
        {
            get
            {
                switch (TrangThai)
                {
                    case TrangThaiKyBaoCao.Nhap: return "Nhập";
                    case TrangThaiKyBaoCao.Mo: return "Mở";
                    case TrangThaiKyBaoCao.Khoa: return "Khóa";
                    default: return TrangThai.ToString();
                }
            }
        }
    }

    public class ReportingPeriodScheduleResultViewModel
    {
        public int TotalPreviewed { get; set; }
        public int CreatedCount { get; set; }
        public int SkippedExistingCount { get; set; }
        public int OpenedCount { get; set; }
    }

    public class ReportEntryViewModel
    {
        public int BaoCaoId { get; set; }
        public int KyBaoCaoId { get; set; }
        public int KhoaPhongId { get; set; }
        public int ChiSoChatLuongId { get; set; }
        public int PhanCongChiSoId { get; set; }
        public string TenKyBaoCao { get; set; }
        public string TenKhoaPhong { get; set; }
        public string MaChiSo { get; set; }
        public string TenChiSo { get; set; }
        public LoaiCongThuc LoaiCongThuc { get; set; }
        public TrangThaiBaoCao TrangThai { get; set; }
        [Display(Name = "Tử số")]
        public decimal? TuSo { get; set; }
        [Display(Name = "Mẫu số")]
        public decimal? MauSo { get; set; }
        public decimal? GiaTriNhap { get; set; }
        public decimal? KetQua { get; set; }
        public bool? DatMucTieu { get; set; }
        [Display(Name = "Ghi chú")]
        public string GhiChu { get; set; }
        public string YKienPhanHoi { get; set; }
    }

    public class ReportListViewModel
    {
        public bool IsAdmin { get; set; }
        public int? KyBaoCaoId { get; set; }
        public int? KhoaPhongId { get; set; }
        public int? ChiSoChatLuongId { get; set; }
        public IList<SelectListItem> KyBaoCaoOptions { get; set; }
        public IList<SelectListItem> KhoaPhongOptions { get; set; }
        public IList<SelectListItem> ChiSoOptions { get; set; }
        public IList<ReportEntryViewModel> Items { get; set; }
        public IList<KyBaoCaoViewModel> ActivePeriods { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }

    public class NotificationViewModel
    {
        public int ThongBaoId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề.")]
        [Display(Name = "Tiêu đề")]
        public string TieuDe { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung.")]
        [Display(Name = "Nội dung")]
        public string NoiDung { get; set; }
        public LoaiThongBao LoaiThongBao { get; set; }
        public int? KyBaoCaoId { get; set; }
        public int? BaoCaoId { get; set; }
        public DateTime NgayTao { get; set; }
        public bool DaDoc { get; set; }
        public int[] SelectedKhoaPhongIds { get; set; }
        public IList<SelectListItem> KhoaPhongOptions { get; set; }
    }

    public class NotificationIndexViewModel
    {
        public IList<NotificationViewModel> Items { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }

    public class NotificationDetailViewModel
    {
        public NotificationViewModel Notification { get; set; }
        public IList<MissingReportAlertViewModel> MissingReports { get; set; }
    }

    public class DashboardViewModel
    {
        public bool IsAdmin { get; set; }
        public int TongChiSo { get; set; }
        public int BaoCaoDaGui { get; set; }
        public int BaoCaoThieu { get; set; }
        public int BaoCaoQuaHan { get; set; }
        public int ChiSoDuocPhanCong { get; set; }
        public int DueSoonReportCount { get; set; }
        public int OverdueMissingReportCount { get; set; }
        public IList<MissingReportAlertViewModel> MissingReports { get; set; }
        public IList<DepartmentProgressViewModel> DepartmentProgress { get; set; }
        public int? SelectedTanSuat { get; set; }
        public IList<SelectListItem> TanSuatOptions { get; set; }
    }

    public class MissingReportAlertViewModel
    {
        public int KyBaoCaoId { get; set; }
        public string TenKyBaoCao { get; set; }
        public DateTime HanNop { get; set; }
        public int ChiSoChatLuongId { get; set; }
        public int PhanCongChiSoId { get; set; }
        public string MaChiSo { get; set; }
        public string TenChiSo { get; set; }
        public int DaysUntilDue { get; set; }
        public bool IsOverdue { get; set; }
        public bool IsDueSoon { get; set; }
    }

    public class DepartmentProgressViewModel
    {
        public string TenKhoaPhong { get; set; }
        public int Tong { get; set; }
        public int DaGui { get; set; }
        public int LuuNhap { get; set; }
        public int ConThieu => Tong - DaGui;
        public string XepLoai { get; set; }

        // Chi tiết theo tần suất
        public int TongThang { get; set; }
        public int DaGuiThang { get; set; }
        public string TiendoHangThang => $"{DaGuiThang}/{TongThang}";
        public string XepLoaiThang { get; set; }

        public int TongQuy { get; set; }
        public int DaGuiQuy { get; set; }
        public string TiendoHangQuy => $"{DaGuiQuy}/{TongQuy}";
        public string XepLoaiQuy { get; set; }

        public int TongNam { get; set; }
        public int DaGuiNam { get; set; }
        public string TiendoHangNam => $"{DaGuiNam}/{TongNam}";
        public string XepLoaiNam { get; set; }
    }

    // Kết quả sau khi import dữ liệu từ file.
    public class ImportResultViewModel
    {
        // Tổng số dòng dữ liệu đọc được từ file.
        public int TongSoDong { get; set; }

        // Số dòng import thành công.
        public int SoDongThanhCong { get; set; }

        // Số dòng bị lỗi và bị bỏ qua.
        public int SoDongLoi { get; set; }

        // Danh sách lỗi chi tiết theo dòng.
        public IList<string> Errors { get; set; }

        public ImportResultViewModel()
        {
            // Khởi tạo sẵn để view/service có thể thêm lỗi trực tiếp.
            Errors = new List<string>();
        }
    }

    // File import và khoa/phòng đang chọn trên màn hình.
    public class ImportFileViewModel
    {
        // File người dùng upload.
        public HttpPostedFileBase File { get; set; }

        // Nếu có giá trị thì import vào khoa/phòng này.
        public int? KhoaPhongId { get; set; }
    }
}
