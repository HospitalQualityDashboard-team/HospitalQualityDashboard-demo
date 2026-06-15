
# Danh sách chức năng và cấu trúc dự án HospitalQualityDashboard-demo

## 1. Danh sách chức năng và tên nhánh tương ứng

| Tên chức năng | Tên nhánh |
|---|---|
| Đăng nhập Admin/User | feature/login-system |
| Quản lý tài khoản người dùng | feature/account-management |
| Quản lý khoa/phòng | feature/department-management |
| Quản lý nhân viên | feature/employee-management |
| Quản lý danh mục chỉ số chất lượng (kèm xóa chỉ số) | feature/indicator-management |
| Phân công khoa/phòng phụ trách chỉ số | feature/assignment-management |
| Quản lý kỳ báo cáo | feature/reporting-period-management |
| Quản lý báo cáo | feature/report-management |
| Quản lý thông báo (thủ công + tự động) | feature/notification-management |
| Dashboard Admin và thống kê (biểu đồ Chart.js) | feature/admin-dashboard |
| Dashboard User và thống kê | feature/user-dashboard |
| Xuất Excel từ Dashboard (chọn cột, lọc theo kỳ báo cáo và tần suất chỉ số) | feature/dashboard-excel-export |
| Xem chỉ số được phân công (User) | feature/user-indicator-view |
| Nhập số liệu báo cáo (User) | feature/user-report-entry |
| Lưu nháp báo cáo (User) | feature/user-draft-report |
| Gửi báo cáo (User) | feature/user-submit-report |
| Xem trạng thái báo cáo (User) | feature/user-report-status |
| Xem thông báo (User) | feature/user-notification-view |
| Đổi mật khẩu | feature/change-password |
| Cập nhật thông tin profile | feature/update-profile |
| Xuất báo cáo Excel cho User | feature/user-export-reports |
| Khởi tạo CSDL tự động (DatabaseBootstrapper) | feature/database-bootstrap |
| BaseController kiểm tra session và phân quyền (PageController) | feature/session-management |
| Tính toán chỉ số tự động (IndicatorCalculationService) | feature/indicator-calculation |
| Thông báo tự động (NotificationAutomationService) | feature/auto-notification |

---

## 2. Chi tiết chức năng từng màn hình

### 2.1 Đăng nhập
- **Màn hình**: `Views/Account/AdminLogin.cshtml`, `Views/Account/UserLogin.cshtml`
- **Chức năng chính**:
  - Người dùng nhập tên đăng nhập và mật khẩu
  - Hệ thống xác thực và phân quyền (Admin/User)
  - Lưu thông tin vào Session (TaiKhoanId, TenDangNhap, LoaiTaiKhoan, KhoaPhongId,...)
  - Chuyển hướng đến Dashboard tương ứng

---

### 2.2 Admin - Quản lý tài khoản người dùng
- **Màn hình**: `Areas/Admin/Views/Employee/Index.cshtml`, `Areas/Admin/Views/Employee/CreateAccount.cshtml`
- **Chức năng chính**:
  - Xem danh sách tất cả tài khoản (Admin và User)
  - Thêm tài khoản mới
  - Sửa thông tin tài khoản (tất cả các trường)
  - Khóa/mở khóa tài khoản
  - Reset mật khẩu
  - Gán nhân viên và khoa/phòng cho tài khoản
  - Chọn loại tài khoản (Admin/User)

---

### 2.3 Admin - Quản lý khoa/phòng
- **Màn hình**: `Areas/Admin/Views/Department/Index.cshtml`, `Areas/Admin/Views/Department/Edit.cshtml`
- **Chức năng chính**:
  - Xem danh sách tất cả khoa/phòng
  - Thêm khoa/phòng mới
  - Sửa thông tin khoa/phòng
  - Khóa/mở khóa khoa/phòng
  - Import danh sách khoa/phòng từ file Excel
  - Xuất danh sách khoa/phòng ra file Excel

---

### 2.4 Admin - Quản lý nhân viên
- **Màn hình**: `Areas/Admin/Views/Employee/Index.cshtml`, `Areas/Admin/Views/Employee/Edit.cshtml`
- **Chức năng chính**:
  - Xem danh sách nhân viên toàn viện
  - Lọc nhân viên theo khoa/phòng
  - Thêm nhân viên mới
  - Sửa thông tin nhân viên (tất cả các trường)
  - Khóa/mở khóa nhân viên
  - Import nhân viên từ file Excel
  - Xuất danh sách nhân viên ra file Excel

---

### 2.5 Admin - Quản lý danh mục chỉ số chất lượng
- **Màn hình**: `Areas/Admin/Views/Indicator/Index.cshtml`, `Areas/Admin/Views/Indicator/Edit.cshtml`, `Areas/Admin/Views/Indicator/Details.cshtml`
- **Chức năng chính**:
  - Xem danh sách tất cả chỉ số chất lượng
  - Thêm chỉ số mới (tên, mã, định nghĩa, phương pháp tính, tử số, mẫu số, nguồn số liệu, tần suất báo cáo, mục tiêu năm, thu thập và tổng hợp số liệu,...)
  - Sửa thông tin chỉ số
  - Khóa/mở khóa chỉ số
  - Xóa chỉ số (nếu không có ràng buộc dữ liệu)
  - Xem chi tiết thông tin chỉ số
  - Import chỉ số từ file Excel/Docx
    - Hệ thống tự nhận diện dựa trên "Thu thập và tổng hợp số liệu" để phân công chỉ số cho khoa/phòng
  - Cấu hình tần suất báo cáo (hàng ngày, hàng tuần, hàng tháng, hàng quý, 6 tháng, 9 tháng, 12 tháng, hàng năm, Khi phát sinh, Trước và sau khi thực hiện)

---

### 2.6 Admin - Phân công khoa/phòng phụ trách chỉ số
- **Màn hình**: `Areas/Admin/Views/Assignment/Index.cshtml`
- **Chức năng chính**:
  - Xem danh sách phân công chỉ số theo khoa/phòng
  - Phân công 1 hoặc nhiều chỉ số cho 1 khoa/phòng
  - Sửa phân công hiện tại
  - Hủy phân công chỉ số cho khoa/phòng
  - Lọc theo khoa/phòng hoặc chỉ số
  - Xuất danh sách phân công ra file Excel

---

### 2.7 Admin - Quản lý kỳ báo cáo
- **Màn hình**: `Areas/Admin/Views/ReportingPeriod/Index.cshtml`, `Areas/Admin/Views/ReportingPeriod/Edit.cshtml`, `Areas/Admin/Views/ReportingPeriod/GenerateSchedule.cshtml`
- **Chức năng chính**:
  - Xem danh sách các kỳ báo cáo
  - Tạo kỳ báo cáo thủ công
  - Sửa thông tin kỳ báo cáo (thời gian, hạn nộp)
  - Mở/khóa kỳ báo cáo
  - Tạo lịch kỳ báo cáo tự động theo năm (chọn loại kỳ, xem trước trước khi tạo)
  - Theo dõi tiến độ nộp báo cáo của các khoa/phòng

---

### 2.8 Admin - Quản lý báo cáo
- **Màn hình**: `Areas/Admin/Views/Report/Index.cshtml`, `Areas/Admin/Views/Report/Nhap.cshtml`
- **Chức năng chính**:
  - Xem danh sách báo cáo theo kỳ, khoa/phòng, chỉ số
  - Xem chi tiết báo cáo (tử số, mẫu số, kết quả, ghi chú)
  - Lọc báo cáo theo nhiều tiêu chí
  - *Lưu ý: Chưa cần chức năng duyệt/trả lại báo cáo*

---

### 2.9 Admin - Quản lý thông báo
- **Màn hình**: `Areas/Admin/Views/Notification/Index.cshtml`, `Areas/Admin/Views/Notification/Create.cshtml`, `Areas/Admin/Views/Notification/Details.cshtml`
- **Chức năng chính**:
  - Xem danh sách các thông báo đã tạo
  - Tạo và gửi thông báo thủ công đến 1 hoặc nhiều khoa/phòng
  - Xem chi tiết thông báo và trạng thái đọc của người dùng
  - **Thông báo tự động** (`NotificationAutomationService`):
    - Tự động gửi thông báo khi mở kỳ báo cáo mới
    - Tự động nhắc hạn nộp báo cáo
    - Tự động thông báo khi quá hạn

---

### 2.10 Admin - Dashboard và thống kê
- **Màn hình**: `Areas/Admin/Views/Dashboard/Index.cshtml`
- **Chức năng chính**:
  - Xem tổng quan số lượng chỉ số, số báo cáo đã nộp/chưa nộp/quá hạn
  - Thống kê theo khoa/phòng
  - Thống kê theo chỉ số
  - Hiển thị biểu đồ cột (Chart.js) so sánh tiến độ các khoa/phòng
  - **Lọc theo Tần suất báo cáo** (dropdown Hàng tháng, Hàng quý, 6 tháng, Hàng năm, Tất cả) tự động cập nhật thống kê, biểu đồ và danh sách khoa/phòng
  - Hiển thị các cột tiến độ nâng cao: Số báo cáo lưu nháp, Số báo cáo còn thiếu, Xếp loại hoàn thành tổng quan và tiến độ chi tiết từng tần suất chính kèm Badge màu tương ứng (Xuất sắc, Khá, Trung bình, Yếu, N/A)
  - **Xuất file Excel (.xlsx)** (qua `Areas/Admin/Controllers/ExportController.cs`):
    - Xuất danh sách khoa/phòng (Excel)
    - Xuất danh sách nhân viên (Excel, có lọc theo khoa)
    - Xuất danh sách chỉ số chất lượng (Excel)
    - Xuất phân công chỉ số (Excel, có lọc theo khoa/chỉ số/trạng thái, chọn cột)
    - Xuất báo cáo (Excel, có lọc theo kỳ/khoa/chỉ số)
    - **Xuất Excel Tiến độ Dashboard** (hỗ trợ lọc theo Tần suất và tùy chọn bật/tắt các cột xuất nâng cao: Nháp, Còn thiếu, Xếp loại tổng thể, Tiến độ & Xếp loại Hàng tháng/Hàng quý/Hàng năm)

---

### 2.11 User - Dashboard và thống kê
- **Màn hình**: `Areas/User/Views/Dashboard/Index.cshtml`
- **Chức năng chính**:
  - Xem tổng quan báo cáo của khoa/phòng mình
  - Thống kê các chỉ số được phân công
  - Xem tiến độ nộp báo cáo
  - Cảnh báo chỉ số quá hạn/sắp đến hạn
  - Hiển thị biểu đồ cột tiến độ

---

### 2.12 User - Xem chỉ số được phân công
- **Màn hình**: `Areas/User/Views/Indicator/Index.cshtml`, `Areas/User/Views/Indicator/Details.cshtml`
- **Chức năng chính**:
  - Xem danh sách các chỉ số được phân công cho khoa/phòng của mình
  - Xem chi tiết thông tin chỉ số (định nghĩa, phương pháp tính, tử số, mẫu số, nguồn số liệu)
  - Lọc chỉ số theo kỳ báo cáo

---

### 2.13 User - Nhập và quản lý báo cáo
- **Màn hình**: `Areas/User/Views/Report/Index.cshtml`, `Areas/User/Views/Report/Nhap.cshtml`, `Areas/User/Views/Report/Edit.cshtml`
- **Chức năng chính**:
  - Chọn kỳ báo cáo để nhập số liệu
  - Nhập tử số, mẫu số hoặc giá trị trực tiếp
  - Tự động tính kết quả
  - Nhập ghi chú
  - Lưu báo cáo ở trạng thái nháp
  - Gửi báo cáo
  - Xem trạng thái báo cáo (nháp, đã gửi, quá hạn)
  - **Xuất báo cáo Excel** (qua `Areas/User/Controllers/ExportController.cs`, lọc theo kỳ/chỉ số)
  - *Lưu ý: Chưa cần chức năng sửa báo cáo bị trả lại và đính kèm file minh chứng*

---

### 2.14 User - Xem thông báo
- **Màn hình**: `Areas/User/Views/Notification/Index.cshtml`, `Areas/User/Views/Notification/Details.cshtml`
- **Chức năng chính**:
  - Xem danh sách các thông báo từ Admin hoặc hệ thống
  - Xem chi tiết thông báo
  - Đánh dấu thông báo đã đọc

---

### 2.15 Đổi mật khẩu
- **Màn hình**: `Views/Account/Profile.cshtml`
- **Chức năng chính**:
  - Người dùng nhập mật khẩu cũ và mật khẩu mới
  - Hệ thống xác thực và cập nhật mật khẩu

---

### 2.16 Cập nhật thông tin profile
- **Màn hình**: `Views/Account/Profile.cshtml`
- **Chức năng chính**:
  - **User**: Chỉ có thể sửa các trường: Ngày sinh, Email, Số điện thoại, Giới tính
  - **Admin**: Có thể sửa tất cả các trường thông tin cho User và cho chính mình

---

### 2.17 Hệ thống - Quản lý phiên đăng nhập (PageController)
- **Màn hình**: `Controllers/PageController.cs` (abstract base class)
- **Chức năng chính**:
  - Kiểm tra session tự động trước mỗi action
  - Re-validate tài khoản (kiểm tra tài khoản còn hoạt động không)
  - Cung cấp các property: `CurrentTaiKhoanId`, `CurrentTenDangNhap`, `CurrentLoaiTaiKhoan`, `CurrentNhanVienId`, `CurrentKhoaPhongId`, `CurrentTenKhoaPhong`
  - Hỗ trợ kiểm tra quyền: `RequireAdmin()`, `EnsureUserDepartment()`

### 2.18 Hệ thống - Khởi tạo CSDL tự động (DatabaseBootstrapper)
- **File**: `Services/DatabaseBootstrapper.cs`
- **Chức năng chính**:
  - Tự động tạo CSDL và các bảng khi ứng dụng khởi chạy lần đầu
  - Tạo tài khoản Admin mặc định
  - Import dữ liệu mẫu (khoa/phòng, nhân viên, chỉ số)

### 2.19 Hệ thống - Tính toán chỉ số tự động
- **File**: `Services/ReportDashboardServices.cs` — `IndicatorCalculationService`
- **Chức năng chính**:
  - Tự động tính kết quả dựa trên loại công thức (tỷ lệ, số lượng, giá trị trực tiếp, điểm trung bình, tỷ số)
  - So sánh kết quả với mục tiêu năm (> , >=, <, <=, =)
  - Đánh giá đạt/không đạt mục tiêu

### 2.20 Hệ thống - Thông báo tự động
- **File**: `Services/NotificationExportServices.cs` — `NotificationAutomationService`
- **Chức năng chính**:
  - Tự động gửi thông báo đến các tài khoản User khi Admin mở kỳ báo cáo mới
  - Tự động gửi thông báo nhắc hạn cho các khoa/phòng có chỉ số sắp đến hạn nộp
  - Tự động gửi thông báo quá hạn
  - Ghi nhận lịch sử gửi thông báo tự động

### 2.21 Hệ thống - Kiểm thử
- Các script PowerShell xác minh tạm trong `tools/` đã được xóa khỏi repository vì chỉ còn file rỗng/placeholder.
- Kiểm tra hiện tại thực hiện bằng build project, build Razor view và checklist test thủ công.

```
HospitalQualityDashboard-demo/
├── App_Data/
│   ├── Sql/
│   │   └── 001_CreateSchema.sql
├── App_Start/
│   ├── BundleConfig.cs
│   ├── FilterConfig.cs
│   └── RouteConfig.cs
├── Areas/
│   ├── Admin/
│   │   ├── Controllers/
│   │   │   ├── AdminBaseController.cs
│   │   │   ├── AssignmentController.cs
│   │   │   ├── DashboardController.cs
│   │   │   ├── DepartmentController.cs
│   │   │   ├── EmployeeController.cs
│   │   │   ├── ExportController.cs
│   │   │   ├── IndicatorController.cs
│   │   │   ├── NotificationController.cs
│   │   │   ├── ReportController.cs
│   │   │   └── ReportingPeriodController.cs
│   │   ├── Views/
│   │   │   ├── Assignment/
│   │   │   │   ├── Index.cshtml
│   │   │   │   ├── _DepartmentView.cshtml
│   │   │   │   ├── _IndicatorView.cshtml
│   │   │   │   └── _TableView.cshtml
│   │   │   ├── Dashboard/
│   │   │   │   └── Index.cshtml
│   │   │   ├── Department/
│   │   │   │   ├── Edit.cshtml
│   │   │   │   └── Index.cshtml
│   │   │   ├── Employee/
│   │   │   │   ├── CreateAccount.cshtml
│   │   │   │   ├── Edit.cshtml
│   │   │   │   └── Index.cshtml
│   │   │   ├── Indicator/
│   │   │   │   ├── Details.cshtml
│   │   │   │   ├── Edit.cshtml
│   │   │   │   └── Index.cshtml
│   │   │   ├── Notification/
│   │   │   │   ├── Create.cshtml
│   │   │   │   ├── Details.cshtml
│   │   │   │   └── Index.cshtml
│   │   │   ├── Report/
│   │   │   │   ├── Edit.cshtml
│   │   │   │   ├── Index.cshtml
│   │   │   │   └── Nhap.cshtml
│   │   │   ├── ReportingPeriod/
│   │   │   │   ├── Edit.cshtml
│   │   │   │   ├── GenerateSchedule.cshtml
│   │   │   │   └── Index.cshtml
│   │   │   ├── Shared/
│   │   │   │   └── _AdminLayout.cshtml
│   │   │   ├── Web.config
│   │   │   └── _ViewStart.cshtml
│   │   └── AdminAreaRegistration.cs
│   └── User/
│       ├── Controllers/
│       │   ├── DashboardController.cs
│       │   ├── IndicatorController.cs
│       │   ├── NotificationController.cs
│       │   ├── ReportController.cs
│       │   └── UserBaseController.cs
│       ├── Views/
│       │   ├── Dashboard/
│       │   │   └── Index.cshtml
│       │   ├── Indicator/
│       │   │   ├── Details.cshtml
│       │   │   └── Index.cshtml
│       │   ├── Notification/
│       │   │   ├── Details.cshtml
│       │   │   └── Index.cshtml
│       │   ├── Report/
│       │   │   ├── Edit.cshtml
│       │   │   ├── Index.cshtml
│       │   │   └── Nhap.cshtml
│       │   ├── Shared/
│       │   │   └── _UserLayout.cshtml
│       │   ├── Web.config
│       │   └── _ViewStart.cshtml
│       └── UserAreaRegistration.cs
├── Content/
│   ├── bootstrap.css
│   └── Site.css
├── Controllers/
│   ├── AccountController.cs
│   ├── AssignmentController.cs
│   ├── DashboardController.cs
│   ├── DepartmentController.cs
│   ├── EmployeeController.cs
│   ├── ExportController.cs
│   ├── HomeController.cs
│   ├── IndicatorController.cs
│   ├── NotificationController.cs
│   ├── PageController.cs
│   ├── ReportController.cs
│   └── ReportingPeriodController.cs
├── Models/
│   ├── DTOs/
│   │   ├── AssignmentDtos.cs
│   │   ├── DepartmentDtos.cs
│   │   ├── EmployeeDtos.cs
│   │   ├── ExportDtos.cs
│   │   ├── IndicatorDtos.cs
│   │   ├── NotificationDtos.cs
│   │   ├── ReportDtos.cs
│   │   └── ReportingPeriodDtos.cs
│   ├── Entities/
│   │   └── CoreEntities.cs
│   ├── Enums/
│   │   └── SystemEnums.cs
│   └── ViewModels/
│       ├── AppViewModels.cs
│       └── AuthViewModels.cs
├── Properties/
│   └── AssemblyInfo.cs
├── Scripts/
│   ├── jquery-3.7.0.js
│   ├── jquery-3.7.0.min.js
│   ├── jquery.validate.js
│   ├── jquery.validate.min.js
│   ├── jquery.validate.unobtrusive.js
│   ├── jquery.validate.unobtrusive.min.js
│   ├── modernizr-2.8.3.js
│   └── bootstrap.js
├── Services/
│   ├── AuthService.cs
│   ├── DatabaseBootstrapper.cs
│   ├── DbServiceBase.cs
│   ├── ExcelImportExportService.cs
│   ├── IndicatorServices.cs
│   ├── ReportingPeriodServices.cs
│   ├── ManagementServices.cs
│   ├── NotificationExportServices.cs
│   ├── PasswordHasher.cs
│   ├── ReportDashboardServices.cs
│   │   (chứa IndicatorCalculationService, ReportService, DashboardService)
│   └── SessionUserAccessor.cs
├── Tai_Lieu/
│   ├── BACKEND_TASKS.md
│   ├── DM_KHOA_PHONG.xlsx
│   ├── Danh_sach_nhan_vien_mau_Benh_vien_Ung_Buou.xlsx
│   ├── Lỗ hổng.md
│   ├── Phan Tich Thiet Ke He Thong Chi Tiet.md
│   ├── Phân chia các chỉ số dựa theo đơn vị thu thập và tổng hợp.docx
│   ├── Phân công chỉ số.xlsx
│   ├── Quy Định &amp; Hướng Dẫn Dành Cho Dev Team.md
│   ├── SRS_HeThongDauThauBenhVien.docx
│   ├── Yeu Cau Nghiep Vu BA.md
│   ├── danh_sach_chuc_nang_admin_user.md
│   └── Định nghĩa(55 chí số) _55.docx
├── Views/
│   ├── Account/
│   │   ├── AdminLogin.cshtml
│   │   ├── Profile.cshtml
│   │   └── UserLogin.cshtml
│   ├── Home/
│   │   └── Index.cshtml
│   ├── Shared/
│   │   ├── Error.cshtml
│   │   └── _Layout.cshtml
│   ├── Web.config
│   └── _ViewStart.cshtml
├── docs/                     # Con ton dong (superpowers plans/specs cu can xoa tiep)
├── cleanup.ps1              # Script don dep file du thua
├── implementation-notes.md
├── packages.config
├── AGENTS.md
├── Global.asax
├── Global.asax.cs
├── HospitalQualityDashboard-demo.csproj
├── PROJECT_CONTEXT.md
├── TAI_LIEU_NGHIEP_VU.md
├── Web.config
├── Web.Debug.config
└── Web.Release.config
```

---

## 4. Công nghệ sử dụng

- **Framework**: ASP.NET MVC 4
- **Ngôn ngữ**: C#
- **.NET Framework**: .NET Framework 4.7.2
- **Database**: SQL Server
- **CSS Framework**: Bootstrap 5
- **JavaScript Library**: jQuery 3.7.0
