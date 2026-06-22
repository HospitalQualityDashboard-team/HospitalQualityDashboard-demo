# Hồ Sơ Bối Cảnh Dự Án

Tài liệu này là bản ghi nhớ kỹ thuật và nghiệp vụ tổng quan của dự án `HospitalQualityDashboard-demo`. File được dùng để giúp lập trình viên, người kiểm thử, người viết báo cáo và trợ lý AI hiểu đúng hiện trạng dự án trước khi chỉnh sửa.

## 1. Tổng Quan

**Tên dự án:** HospitalQualityDashboard-demo

**Mục tiêu:** xây dựng hệ thống quản lý chỉ số chất lượng bệnh viện, hỗ trợ Admin quản trị danh mục và hỗ trợ các khoa/phòng nhập, gửi, theo dõi số liệu báo cáo định kỳ. User có thể tự chỉnh sửa thông tin cá nhân (họ tên, email, SĐT, chức vụ, giới tính, ngày sinh) trên trang Profile.

**Bài toán chính:**

- Quản lý danh mục khoa/phòng, nhân viên và tài khoản đăng nhập.
- Quản lý danh mục chỉ số chất lượng bệnh viện, bao gồm định nghĩa, công thức, nguồn số liệu, tần suất báo cáo và mục tiêu.
- Phân công chỉ số cho một hoặc nhiều khoa/phòng phụ trách.
- Cho phép User khoa/phòng nhập số liệu theo kỳ báo cáo, lưu nháp và gửi báo cáo.
- Cho phép User tự cập nhật thông tin cá nhân (họ tên, email, SĐT, chức vụ, giới tính, ngày sinh) trên trang Profile — dữ liệu được ghi vào bảng NhanVien thông qua AuthService.UpdateProfile.
- Cho phép Admin theo dõi tiến độ, khóa hoặc xóa báo cáo khi cần.
- Hiển thị dashboard tiến độ báo cáo theo phạm vi quyền hạn.
- Gửi và theo dõi thông báo cho các khoa/phòng.
- Import dữ liệu từ Excel/Word để giảm thao tác nhập tay.

## 2. Công Nghệ

- Framework: ASP.NET MVC 4 trên .NET Framework 4.7.2.
- Ngôn ngữ: C#.
- View engine: Razor `.cshtml`.
- Cơ sở dữ liệu: Azure SQL.
- Truy cập dữ liệu: ADO.NET thuần qua `SqlConnection`, `SqlCommand`, `SqlDataReader`.
- Không dùng Entity Framework.
- Frontend: Bootstrap, CSS tùy biến trong `Content/Site.css`, JavaScript, Chart.js cho dashboard.
- Môi trường chạy phổ biến: IIS Express, ví dụ `localhost:44387` hoặc port IIS Express khác khi chạy local.

Connection string chính nằm trong `ConnectionStrings.config`; `Web.config` chỉ tham chiếu file này:

```xml
<connectionStrings configSource="ConnectionStrings.config" />
```

## 3. Cấu Trúc Thư Mục

```text
HospitalQualityDashboard-demo/                      # Thư mục gốc của project
├── App_Data/
│   └── Sql/
│       ├── 001_CreateSchema.sql                # Schema đầy đủ cho database mới
│       ├── 002_PerformanceIndexes.sql           # Index hiệu năng idempotent
│       ├── 003_AddExportHistory.sql             # Audit xuất Dashboard chi tiết
│       └── 004_AddIndicatorWarning.sql          # Cảnh báo theo chỉ số và dedup
├── App_Start/                                # Cấu hình MVC khởi động
│   ├── BundleConfig.cs                       # Bundle CSS/JS
│   ├── FilterConfig.cs                       # Global filter
│   └── RouteConfig.cs                        # Route MVC
├── Areas/                                    # Phân vùng theo vai trò
│   ├── Admin/                                # ===== ADMIN AREA =====
│   │   ├── Controllers/                      # 10 controllers
│   │   │   ├── AdminBaseController.cs        # Base cho Admin controllers
│   │   │   ├── AssignmentController.cs       # Phân công chỉ số
│   │   │   ├── DashboardController.cs        # Dashboard quản trị
│   │   │   ├── DepartmentController.cs       # Khoa/phòng
│   │   │   ├── EmployeeController.cs         # Nhân viên
│   │   │   ├── ExportController.cs           # Xuất dữ liệu
│   │   │   ├── IndicatorController.cs        # Chỉ số chất lượng
│   │   │   ├── NotificationController.cs     # Thông báo
│   │   │   ├── ReportController.cs           # Báo cáo
│   │   │   └── ReportingPeriodController.cs  # Kỳ báo cáo
│   │   ├── Views/                            # 24 views + Web.config + _ViewStart
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
│   │   │   │   ├── GenerateSchedule.cshtml   # Tạo lịch tự động
│   │   │   │   └── Index.cshtml
│   │   │   ├── Shared/
│   │   │   │   └── _AdminLayout.cshtml       # Layout Admin
│   │   │   ├── Web.config
│   │   │   └── _ViewStart.cshtml
│   │   └── AdminAreaRegistration.cs
│   └── User/                                 # ===== USER AREA =====
│       ├── Controllers/                      # 6 controllers
│       │   ├── DashboardController.cs         # Dashboard khoa/phòng
│       │   ├── ExportController.cs            # Xuất báo cáo Excel (.xlsx)
│       │   ├── IndicatorController.cs         # Xem chỉ số
│       │   ├── NotificationController.cs      # Xem thông báo
│       │   ├── ReportController.cs            # Nhập/gửi báo cáo
│       │   └── UserBaseController.cs          # Base cho User controllers
│       ├── Views/                             # 12 views + Web.config + _ViewStart
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
│       │   │   └── _UserLayout.cshtml         # Layout User
│       │   ├── Web.config
│       │   └── _ViewStart.cshtml
│       └── UserAreaRegistration.cs
├── Content/                                   # CSS
│   ├── bootstrap.css                          # Bootstrap 5 (Bundle CSS chính)
│   ├── bootstrap.min.css                      # Minified (có trong .csproj)
│   ├── bootstrap.css.map
│   └── Site.css                               # Style tùy biến
├── Controllers/                               # Root controllers
│   ├── AccountController.cs                   # Login/logout, profile (giữ nguyên logic)
│   ├── HomeController.cs                      # Landing page
│   └── PageController.cs                      # Lớp base: session guard, phân quyền
├── Models/
│   ├── DTOs/                                  # Data Transfer Objects
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
├── Scripts/                                   # JavaScript
│   ├── jquery-3.7.0.js / .min.js             # jQuery 3.7.0
│   ├── jquery-3.7.0.slim.js / .min.js
│   ├── jquery.validate.js / .min.js           # jQuery Validation
│   ├── jquery.validate.unobtrusive.js / .min.js
│   ├── modernizr-2.8.3.js                     # Modernizr
│   ├── bootstrap.js / .min.js                 # Bootstrap 5 JS
│   ├── bootstrap.bundle.js / .min.js
│   ├── bootstrap.esm.js / .min.js
│   └── *.map                                  # Source maps
├── Services/
│   ├── Authentication/                        # Xác thực, mật khẩu và session
│   ├── Common/                                # Helper dùng chung
│   ├── Dashboards/                            # Dashboard, so sánh kỳ và Excel export
│   │   └── Export/
│   ├── Departments/                           # Quản lý khoa/phòng
│   ├── Employees/                             # Quản lý nhân viên
│   ├── Excel/                                 # Đọc/ghi CSV, XLSX và DOCX
│   ├── Exports/                               # Điều phối các luồng export
│   ├── Indicators/                            # Chỉ số, import và phân công
│   ├── Infrastructure/
│   │   ├── Caching/                           # Cache dropdown
│   │   └── Database/                          # Cấu hình, bootstrap và DbServiceBase
│   ├── Notifications/                         # Thông báo, automation và cảnh báo chỉ số
│   ├── ReportingPeriods/                      # Kỳ báo cáo và sinh lịch
│   └── Reports/                               # Báo cáo và tính toán chỉ số
├── Tai_Lieu/                                  # Tài liệu nghiệp vụ và file nguồn
│   ├── Danh_sach_nhan_vien_mau_Benh_vien_Ung_Buou.xlsx
│   ├── DM_KHOA_PHONG.xlsx
│   ├── Lỗ hổng.md
│   ├── Phân chia các chỉ số dựa theo đơn vị thu thập và tổng hợp.docx
│   ├── Phân công chỉ số.xlsx
│   ├── Phan Tich Thiet Ke He Thong Chi Tiet.md
│   ├── Phieu-Theo-doi-Tien-do-TTTN-Tuan7.docx
│   ├── Phieu-Theo-doi-Tien-do-TTTN-Tuan8.docx
│   ├── SRS_HeThongDauThauBenhVien.docx
│   └── Định nghĩa(55 chí số) _55.docx
├── Views/                                     # Root views
│   ├── Account/
│   │   ├── AdminLogin.cshtml                  # Đăng nhập Admin
│   │   ├── Profile.cshtml                     # Hồ sơ + đổi mật khẩu
│   │   └── UserLogin.cshtml                   # Đăng nhập User
│   ├── Home/
│   │   └── Index.cshtml                       # Landing page
│   ├── Shared/
│   │   ├── Error.cshtml                       # Trang lỗi
│   │   └── _Layout.cshtml                     # Layout chính
│   ├── Web.config
│   └── _ViewStart.cshtml
├── AGENTS.md                                  # Hướng dẫn làm việc
├── Global.asax                                # Entry point
├── Global.asax.cs                             # Application_Start + tự mở kỳ
├── HospitalQualityDashboard-demo.csproj
├── implementation-notes.md                    # Nhật ký triển khai
├── packages.config                            # NuGet packages
├── PROJECT_CONTEXT.md                         # File này
├── TAI_LIEU_NGHIEP_VU.md                      # Tài liệu nghiệp vụ
├── Web.config                                 # Cấu hình chính
├── Web.Debug.config                           # Transform Debug
└── Web.Release.config                         # Transform Release
```

> **Ghi chú:** Thư mục `Filters/` và các root redirect controller cũ đã được xóa. `Content/` giữ CSS Bootstrap chính, bản minified và source map; `Scripts/` vẫn giữ các biến thể bundle/esm/slim và source map có trong project.
>
> **Cấp thư mục gốc repo (`D:\Hoc_Tap\Thuc_Tap\HospitalQualityDashboard-demo\`):** chứa `README.md`, `HospitalQualityDashboard-demo.slnx`, `.gitignore`, thư mục local `.worktrees/`, project ASP.NET và `packages/` NuGet.

## 4. Kiến Trúc Ứng Dụng

### 4.1. Controller

Các controller kế thừa `PageController` để dùng chung cơ chế session và phân quyền.
Sau đợt tái cấu trúc tháng 06/2026, các redirect wrapper ở root đã được xóa. Logic nghiệp vụ và render view nằm trong `Areas/Admin/Controllers/` và `Areas/User/Controllers/`; root chỉ giữ đăng nhập/hồ sơ, trang Home và lớp cơ sở phân quyền.

- `AccountController`: đăng nhập Admin/User riêng biệt, đăng xuất, đổi mật khẩu (giữ nguyên root).
- `HomeController`: trang landing page root, điều hướng người dùng theo trạng thái đăng nhập.
- `PageController`: lớp cơ sở, cung cấp session guard, `RequireAdmin()`, `EnsureUserDepartment()`.
- `Areas/Admin/Controllers/`: chứa logic quản trị cho khoa/phòng, nhân viên, chỉ số, phân công, kỳ báo cáo, báo cáo, thông báo, xuất dữ liệu và dashboard..
- `Areas/User/Controllers/`: chứa logic cho User khoa/phòng: dashboard, báo cáo, chỉ số (xem), thông báo, xuất báo cáo.

### 4.2. Service

Các service chứa nghiệp vụ và truy cập database trực tiếp qua ADO.NET. Tất cả các file đều nằm trong thư mục `Services/`:

| File | Vai trò chính |
|---|---|
| `DbServiceBase.cs` | Lớp cơ sở: `Query`, `Scalar`, `Execute`, `Param`, helper đọc dữ liệu |
| `AuthService.cs` | Xác thực tài khoản, đổi mật khẩu, cập nhật lần đăng nhập cuối, update profile |
| `DashboardExcelExportService.cs` | Xuất Dashboard nhiều sheet, áp dụng phạm vi theo vai trò và ghi `LichSuXuatBaoCao` |
| `DatabaseConfiguration.cs` | Cấu hình tên connection string dùng chung cho các service |
| `DatabaseBootstrapper.cs` | Khởi tạo CSDL và chạy script SQL tự động khi enabled |
| `DropdownCache.cs` | Cache tùy chọn khoa/phòng, chỉ số và kỳ báo cáo trong 5 phút |
| `ExcelImportExportService.cs` | Đọc Excel, xử lý shared string, inline string, ô trống bị Excel lược bỏ |
| `FrequencyHelper.cs` | Chuẩn hóa và đối chiếu tần suất giữa chỉ số với kỳ báo cáo |
| `IndicatorWarningMessageBuilder.cs` | Tạo nội dung cảnh báo trước hạn, đúng hạn và quá hạn theo ngày Việt Nam |
| `IndicatorServices.cs` | Nghiệp vụ chỉ số: CRUD, import, parser tần suất/khoa phòng, suy luận công thức/đơn vị, phân công (AssignmentService) |
| `ManagementServices.cs` | Nghiệp vụ khoa/phòng và nhân viên |
| `NotificationExportServices.cs` | Thông báo thủ công, thông báo tự động (NotificationAutomationService), chống gửi trùng, xuất dữ liệu XLSX |
| `PasswordHasher.cs` | Hash/verify mật khẩu bằng PBKDF2 |
| `ReportDashboardServices.cs` | Nhập báo cáo, tính kết quả (IndicatorCalculationService), gửi/khóa/xóa, dashboard (DashboardService), cảnh báo |
| `ReportingPeriodServices.cs` | Kỳ báo cáo (ReportingPeriodService), tạo lịch tự động (ReportingPeriodScheduleService), tự mở kỳ |
| `SessionUserAccessor.cs` | Chuẩn hóa các key session: `TaiKhoanId`, `LoaiTaiKhoan`, `KhoaPhongId` |

### 4.3. Quy ước chú thích code

Các file code tự viết đã có chú thích tiếng Việt có dấu ở đầu file theo mẫu `Mục đích:` để mô tả vai trò chính của file. Quy ước này áp dụng cho controller, service, model, filter, cấu hình MVC và Razor view nội bộ.

Không thêm chú thích vào thư viện bên thứ ba như Bootstrap, jQuery, Modernizr hoặc file minified. Khi bổ sung code mới, comment nên ưu tiên giải thích mục đích nghiệp vụ, điều kiện bảo mật hoặc lý do xử lý đặc biệt; không cần mô tả lại thao tác hiển nhiên của từng dòng.

## 5. Phân Quyền

Hệ thống có 2 loại tài khoản:

- `Admin = 1`: quản trị toàn hệ thống.
- `User = 2`: tài khoản khoa/phòng, bắt buộc gắn với một `KhoaPhongId`.

Quy tắc chính:

- Người chưa đăng nhập bị chuyển về trang đăng nhập User.
- Admin có thể truy cập các trang quản trị: khoa/phòng, nhân viên, chỉ số, phân công, kỳ báo cáo, báo cáo, thông báo, export.
- User chỉ nhìn thấy các trang phù hợp: tổng quan, báo cáo của khoa/phòng mình, thông báo, đổi mật khẩu.
- Các controller vẫn kiểm tra quyền ở server bằng `RequireAdmin()` và `EnsureUserDepartment()`, không chỉ dựa vào ẩn/hiện menu.
- `_Layout.cshtml` đã phân biệt menu theo `Session["LoaiTaiKhoan"]` để User không còn thấy link Admin như `Chỉ số`, `Phân công`, `Kỳ báo cáo`.

## 6. Cơ Sở Dữ Liệu Chính

### 6.1. Danh mục tổ chức

- `KhoaPhong`: danh mục khoa/phòng, có mã nguồn `IdKhoaPhongNguon`, tên khoa/phòng và trạng thái sử dụng.
- `NhanVien`: nhân viên thuộc khoa/phòng, có mã nhân viên, họ tên, ngày sinh, giới tính, chức vụ, email, số điện thoại.
- `TaiKhoan`: tài khoản đăng nhập. User bắt buộc có `KhoaPhongId` nhờ constraint `CK_TaiKhoan_UserHasKhoaPhong`.

### 6.2. Chỉ số chất lượng

- `ChiSoChatLuong`: lưu thông tin chỉ số, định nghĩa, lĩnh vực áp dụng, khía cạnh/thành tố chất lượng, nguồn số liệu, phương pháp tính, công thức, đơn vị tính, tần suất mặc định.
- `ChiSoTanSuatBaoCao`: bảng phụ cho phép một chỉ số có nhiều tần suất báo cáo. Đây là phần mở rộng quan trọng so với thiết kế ban đầu chỉ có một tần suất trong `ChiSoChatLuong`.
- `ChiSoMucTieu`: lưu mục tiêu theo năm, toán tử so sánh và giá trị mục tiêu.

### 6.3. Phân công và báo cáo

- `PhanCongChiSo`: bảng trung gian thể hiện quan hệ nhiều-nhiều giữa `ChiSoChatLuong` và `KhoaPhong`.
- `KyBaoCao`: kỳ báo cáo theo tháng/quý/6 tháng/năm hoặc loại kỳ khác, có ngày bắt đầu, ngày kết thúc, hạn nộp và trạng thái.
- `BaoCao`: bản ghi báo cáo theo kỳ, khoa/phòng, chỉ số và phân công cụ thể.
- `BaoCaoChiTiet`: dữ liệu nhập chi tiết gồm tử số, mẫu số, giá trị nhập trực tiếp, kết quả tính, đạt mục tiêu hay không, ghi chú.

### 6.4. Thông báo và nhật ký

- `ThongBao`: thông báo do hệ thống hoặc Admin tạo.
- `ThongBaoNguoiNhan`: danh sách người nhận và trạng thái đã đọc.
- `ThongBaoTuDongLog`: log chống gửi trùng cho thông báo tự động theo `DedupKey`.
- `LichSuImport`: lưu lịch sử import ở mức tổng hợp.
- `NhatKyHeThong`: lưu nhật ký thao tác hệ thống.

## 7. Quan Hệ Nghiệp Vụ Quan Trọng

### 7.1. Chỉ số và khoa/phòng là quan hệ n-n

Một chỉ số có thể do nhiều khoa/phòng cùng thu thập, tổng hợp hoặc phối hợp báo cáo. Một khoa/phòng cũng có thể phụ trách nhiều chỉ số. Vì vậy quan hệ đúng là:

```text
ChiSoChatLuong 1-n PhanCongChiSo n-1 KhoaPhong
```

Ví dụ: chỉ số có trường “Thu thập và tổng hợp số liệu” gồm:

```text
P. KHTH thu thập số liệu
P. TCCB tổng hợp số liệu
```

Hệ thống cần tạo 2 phân công cho cùng một chỉ số:

- Chỉ số A - P. KHTH.
- Chỉ số A - P. TCCB.

Bảng `PhanCongChiSo` có unique constraint `(ChiSoChatLuongId, KhoaPhongId)` để tránh trùng phân công.

### 7.2. Một chỉ số có thể có nhiều tần suất

Một số chỉ số có tần suất mô tả dạng “Mỗi quý, 6 tháng, 12 tháng”. Thiết kế mới dùng bảng `ChiSoTanSuatBaoCao` để lưu nhiều dòng tần suất cho cùng một chỉ số.

Các biến thể tiếng Việt đang được nhận diện là tần suất quý:

- `Hàng quý`
- `Mỗi quý`
- `Theo quý`
- `Quý`
- Các mô tả chứa chu kỳ 3 tháng phù hợp.

## 8. Luồng Nghiệp Vụ Chính

### 8.1. Đăng nhập

1. Người dùng chọn trang đăng nhập Admin hoặc User.
2. `AuthService.Authenticate` kiểm tra tên đăng nhập, mật khẩu, trạng thái hoạt động.
3. `AccountController` đối chiếu đúng `LoaiTaiKhoan` với trang đăng nhập.
4. `SessionUserAccessor` ghi session.
5. Người dùng được chuyển đến Dashboard.

Kiểm tra session trên trình duyệt thực hiện qua DevTools:

- Tab `Application` > `Storage` > `Cookies` cho biết cookie `ASP.NET_SessionId`.
- Cookie chỉ lưu mã session; dữ liệu đăng nhập thực tế nằm phía server trong `Session`.
- `Web.config` đặt `sessionState timeout="30"`, nên session server-side hết hạn sau 30 phút không hoạt động.
- Sau logout, `SessionUserAccessor.ClearLoginSession` gọi `Clear()` và `Abandon()`, sau đó truy cập trang protected phải quay lại login.

### 8.2. Import khoa/phòng và nhân viên

1. Admin chọn file Excel.
2. `ExcelImportExportService` đọc workbook.
3. Service kiểm tra dòng, map cột, validate dữ liệu.
4. Dữ liệu hợp lệ được insert/update.
5. Lỗi được gom vào `ImportResultViewModel` để hiển thị.

### 8.3. Import chỉ số

1. Admin upload file Excel/Word chứa danh mục chỉ số.
2. Parser đọc từng dòng/bảng và map vào `ChiSoViewModel`.
3. Hệ thống phân tích:
   - mã chỉ số;
   - tên chỉ số;
   - định nghĩa;
   - nguồn số liệu;
   - thu thập/tổng hợp số liệu;
   - tần suất báo cáo;
   - phương pháp tính;
   - tử số/mẫu số;
   - loại công thức;
   - đơn vị tính;
   - mục tiêu.
4. Hệ thống nhận diện khoa/phòng từ trường `ThuThapTongHop`.
5. Hệ thống lưu chỉ số, tần suất, mục tiêu và phân công tương ứng.

Với file DOCX không có cột `Đơn vị tính`, ví dụ `Phân chia các chỉ số dựa theo đơn vị thu thập và tổng hợp.docx`, hệ thống tự suy luận `DonViTinh` sau khi đã xác định `LoaiCongThuc`. Giá trị `DonViTinh` có sẵn trong file import luôn được ưu tiên. Các rule chính:

- `Tỷ lệ`, `Tỷ suất`, `Công suất`, `Hiệu suất` -> `%`.
- `Tỷ số` -> đơn vị tỷ số cụ thể như `bác sĩ/giường bệnh`, `điều dưỡng/giường bệnh`, `bác sĩ/điều dưỡng`.
- Chỉ số thời gian -> `giờ`, `phút`, `ngày`.
- Chỉ số số lượng -> `người`, `báo cáo`, `ca`, `lượt`, `buồng`, `điểm tiếp nối`, `cầu thang`.
- `Vi tính hóa quản lý trang thiết bị y tế khối nội` -> `mức độ`.

Tên chỉ số được bỏ số thứ tự đầu dòng như `8.` hoặc `10.` trước khi nhận diện. Case `Số lượng các điểm tiếp nối...` được ưu tiên là `SoLuong`, không bị nhầm sang `DiemTrungBinh`.

### 8.4. Phân công chỉ số

Admin có 2 cách phân công:

- Chọn nhiều khoa/phòng và nhiều chỉ số trên màn hình `Assignment`.
- Bấm đồng bộ từ dữ liệu chỉ số để hệ thống đọc lại trường `ThuThapTongHop` và tạo các phân công còn thiếu.

Mục tiêu của luồng này là đảm bảo các chỉ số có nhiều khoa/phòng liên quan được gắn đủ phân công, thay vì chỉ gắn 1 khoa/phòng.

### 8.5. Tạo kỳ báo cáo

Admin tạo kỳ báo cáo với loại kỳ, thời gian bắt đầu/kết thúc, hạn nộp. Khi User nhập báo cáo cho kỳ, hệ thống chỉ hiển thị các chỉ số được phân công cho khoa/phòng của User và có tần suất phù hợp với loại kỳ báo cáo.

### 8.6. User nhập và gửi báo cáo

1. User vào `Báo cáo của tôi`.
2. Chọn kỳ báo cáo.
3. Hệ thống liệt kê các chỉ số được phân công cho khoa/phòng của User.
4. User nhập tử số/mẫu số hoặc giá trị trực tiếp tùy `LoaiCongThuc`.
5. Hệ thống tính `KetQua`, so sánh mục tiêu nếu có và lưu chi tiết.
6. User gửi báo cáo. Nếu gửi trước hoặc đúng hạn, trạng thái chuyển từ `Nhap` sang `DaGui`; nếu gửi sau `KyBaoCao.HanNop`, trạng thái chuyển sang `QuaHan`.

### 8.7. Admin theo dõi và xử lý báo cáo

Admin xem danh sách báo cáo toàn viện, lọc theo kỳ, khoa/phòng, chỉ số. Luồng danh sách thao tác chính gồm `DaGui`, `QuaHan`, `DaKhoa`; báo cáo `Nhap` của User không hiển thị. Dữ liệu `DaDuyet` cũ vẫn được Dashboard và dịch vụ thông báo xem là đã nộp để không làm sai thống kê.

Admin có thể:

- xem chi tiết báo cáo ở chế độ chỉ đọc;
- khóa báo cáo đã gửi đúng hạn hoặc gửi trễ;
- xóa báo cáo nếu cần xử lý dữ liệu sai trong quá trình vận hành thử;
- xuất dữ liệu.

Các route duyệt/trả lại báo cáo không còn thuộc luồng chính và bị chặn. Admin cũng không được submit hoặc sửa số liệu thay User.

### 8.8. Dashboard

Dashboard hiển thị theo quyền:

- Admin thấy thống kê toàn viện và tiến độ theo từng khoa/phòng.
- Cụm thẻ tổng quan Admin tính theo slot duy nhất `(KyBaoCaoId, KhoaPhongId, ChiSoChatLuongId)` của kỳ không nháp, phân công/chỉ số đang hoạt động và tần suất phù hợp. Tổng cần nộp luôn bằng đã báo cáo cộng còn thiếu.
- Admin có thể bấm từng thẻ để mở modal chi tiết: Tổng cần nộp hiển thị mọi slot, Đã báo cáo hiển thị các báo cáo hợp lệ kèm Đạt/Chưa đạt/Chưa đánh giá, Còn thiếu gồm slot chưa nộp và bản nháp, còn Quá hạn là tập con chưa nộp có `HanNop` trước ngày hiện tại.
- Modal Dashboard Admin có nút cảnh báo cho từng slot chưa nộp. Mỗi slot chỉ gửi tối đa một cảnh báo thủ công trong ngày; thông báo liên kết trực tiếp với chỉ số để User mở đúng dòng cần xử lý.
- User thấy số chỉ số được phân công, số đã gửi, còn thiếu, quá hạn của khoa/phòng mình.
- User thấy cảnh báo ngay sau khi đăng nhập nếu có chỉ số chưa báo cáo, gần đến hạn hoặc đã quá hạn chưa nộp. Cảnh báo có danh sách chi tiết và link nhập báo cáo nhanh.

Quy tắc thống kê quan trọng:

```text
Đã báo cáo = DaGui + QuaHan + DaKhoa + DaDuyet
```

`QuaHan` trong `BaoCao` nghĩa là đã gửi trễ. Thẻ `Quá hạn` trên Dashboard Admin chỉ đếm slot đã qua `HanNop` nhưng chưa có báo cáo hợp lệ, không đếm báo cáo đã gửi trễ.

### 8.9. Thông báo tự động và chi tiết thông báo

Thông báo nội bộ hiện có 2 nhóm:

- Thông báo thủ công do Admin gửi.
- Thông báo tự động do `NotificationAutomationService` tạo.
- Nhắc hạn tự động chạy ở các mốc 10, 7, 3, 1 và 0 ngày trước hạn khi Dashboard được mở; dedup log ngăn tạo thông báo trùng.

Các loại thông báo tự động chính:

- `KyBaoCaoMo`: nhắc kỳ báo cáo đã mở.
- `NhacHan`: nhắc các khoa/phòng còn thiếu báo cáo trước hạn hoặc đúng ngày hạn.
- `QuaHan`: cảnh báo còn chỉ số quá hạn chưa nộp.
- `TongHopAdmin`: tổng hợp tiến độ hằng ngày cho Admin.

Khi User bấm vào một thông báo có gắn `KyBaoCaoId`, `NotificationController.Details` mở trang chi tiết và truy vấn danh sách chỉ số còn thiếu của khoa/phòng trong kỳ đó. Với thông báo `QuaHan`, danh sách chỉ lấy các chỉ số đã qua hạn nhưng chưa có báo cáo ở trạng thái `DaGui`, `QuaHan`, `DaKhoa` hoặc `DaDuyet`.

Trang chi tiết thông báo hiển thị form POST có anti-forgery để User đánh dấu thông báo là đã đọc; GET chi tiết chỉ đọc dữ liệu.

## 9. Giao Diện

Giao diện đã được cải tiến theo hướng hiện đại, dùng tiếng Việt có dấu. Các điểm chính:

- Navigation tối, rõ vai trò.
- Card thống kê dashboard.
- Bảng dữ liệu dễ đọc, có spacing và trạng thái.
- Form nhập liệu dùng nhãn tiếng Việt có dấu.
- Menu phân quyền theo Admin/User.
- Các nút gây 401 cho User đã được ẩn ở giao diện User, ví dụ `Chỉ số`, `Phân công`, `Xuất Excel`, `Khóa`, `Xóa`, `Gửi thông báo`.

## 10. Kiểm Thử Và Công Cụ Xác Minh

Các script PowerShell xác minh tạm trong `tools/` đã được xóa khỏi repository vì chỉ còn file rỗng/placeholder.

Lệnh build kiểm tra Razor view:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard-demo\HospitalQualityDashboard-demo.csproj /p:Configuration=Debug /p:Platform=AnyCPU /p:MvcBuildViews=true
```

## 11. Các Quyết Định Thiết Kế Đã Chốt

- **Dùng ADO.NET thuần** thay vì Entity Framework để giữ dự án đơn giản, hiệu năng tối đa và phù hợp với hiện trạng ban đầu.
- **User bắt buộc gắn với khoa/phòng** và bị giới hạn nghiêm ngặt phạm vi truy cập dữ liệu ở mức Server-side (không chỉ ẩn/hiện ở Client-side).
- **Chỉ số - Khoa/phòng là quan hệ nhiều-nhiều** thông qua bảng trung gian `PhanCongChiSo` hỗ trợ thuộc tính trạng thái hoạt động độc lập.
- **Một chỉ số có thể cấu hình nhiều tần suất báo cáo** thông qua bảng phụ `ChiSoTanSuatBaoCao`, cho phép linh hoạt ghép nối tần suất nghiệp vụ thực tế.
- **Import DOCX không phụ thuộc tuyệt đối vào cột đơn vị tính**: Nếu file chỉ số không có `DonViTinh`, hệ thống suy luận đơn vị từ tên chỉ số và loại công thức. Nếu file có `DonViTinh`, giá trị file được ưu tiên để tránh ghi đè dữ liệu chủ động của Admin.
- **Sinh "kỳ báo cáo chi tiết" (Reporting Slots) động bằng cơ chế LEFT JOIN**: Hệ thống không ghi trước các dòng trống xuống cơ sở dữ liệu khi Admin tạo một kỳ báo cáo mới. Thay vào đó, khi User truy cập, hệ thống sử dụng truy vấn `LEFT JOIN` giữa bảng phân công chỉ số của khoa đó (`PhanCongChiSo`) và bảng báo cáo thực tế (`dbo.BaoCao`).
  - *Lợi ích*: Tiết kiệm dung lượng lưu trữ tối đa, tránh dư thừa dữ liệu. Đồng thời giúp cập nhật danh sách ngay lập tức khi Admin thay đổi phân công chỉ số mà không cần thao tác đồng bộ phức tạp.
  - *Giải thích số lượng slots*: Số lượng "slot" báo cáo hiển thị chính xác tương ứng với số lượng chỉ số đang hoạt động của khoa đó có tần suất trùng với tần suất của kỳ báo cáo. Ví dụ, nếu khoa chỉ phụ trách 3 chỉ số có tần suất 6 tháng, thì trong kỳ báo cáo 6 tháng hệ thống sẽ hiển thị đúng 3 slot tương ứng.
- **Duy trì trạng thái giao diện (State Preservation)**: Khi Admin thực hiện các hành động cập nhật phân công (Kích hoạt, Tạm dừng, Xóa), hệ thống bảo lưu toàn bộ tham số tìm kiếm, lọc khoa phòng, bộ lọc trạng thái và trang phân trang hiện tại thông qua các tham số Route Values truyền trực tiếp trong Action Redirect.
- **Tài liệu dự án dùng UTF-8 sạch** và viết hoàn toàn bằng tiếng Việt có dấu.
- **Phạm vi báo cáo hiện tại không có duyệt/trả lại**: Admin theo dõi, xem, khóa và xóa báo cáo; User chịu trách nhiệm nhập/gửi. Các trạng thái cũ phục vụ tương thích dữ liệu nhưng không xuất hiện trong luồng chính.
- **Phân biệt báo cáo gửi trễ và slot quá hạn chưa nộp**: `BaoCao.QuaHan` chỉ dùng cho báo cáo đã gửi sau hạn; slot chưa gửi sau hạn được tính bằng truy vấn thiếu báo cáo và dùng cho dashboard/thông báo.
- **Thông báo tự động phải idempotent**: `ThongBaoTuDongLog.DedupKey` chống tạo trùng khi service được gọi nhiều lần từ Dashboard, trang thông báo hoặc tác vụ nền sau này.

## 12. Các Cải Tiến Nghiệp Vụ Mới Nhất (Ngày 23/05/2026)

Hệ thống đã được nâng cấp toàn diện với các giải pháp kỹ thuật hiện đại:
1. **Phân trang & Lọc nâng cao cho Phân công**:
   - Giao diện Phân công được tích hợp phân trang 20 chỉ số/trang kèm 5 thẻ thống kê động (Tổng số chỉ số, Đã phân công, Chưa phân công, Tổng phân công, Đã tạm dừng).
   - Card "Chưa phân công" giúp Admin theo dõi các chỉ số bị bỏ quên.
   - Sửa lỗi logic khi click "Tạm dừng": Phân công bị tạm dừng chỉ chuyển trạng thái `DangHoatDong = 0` chứ không bị xóa khỏi chỉ số, khoa/phòng vẫn được giữ lại với trạng thái cảnh báo màu cam rõ ràng.
2. **AJAX Preview & Bulk Actions**:
   - Khi chọn phân công hàng loạt, hệ thống gọi AJAX đến phương thức `Preview` hiển thị bảng xem trước trạng thái phân công là `✅ Mới` (sẽ được tạo) hay `⚠️ Đã tồn tại` (sẽ bỏ qua) để tránh trùng lặp.
   - Hỗ trợ chọn nhiều dòng qua checkbox và thực hiện kích hoạt/tạm dừng/xóa hàng loạt thông qua thanh công cụ nổi (Bulk Action Floating Bar).
3. **Xác thực biểu mẫu Chỉ số đa lớp**:
   - Biểu mẫu tạo mới/sửa chỉ số được chia thành 4 khu vực trực quan rõ ràng.
   - Các trường bắt buộc được gắn nhãn dấu sao đỏ `*`, các trường không bắt buộc được gắn nhãn phụ màu xám `(Không bắt buộc)`.
   - Tích hợp `jquery.validate.unobtrusive` ở Client-side cho phép thông báo lỗi tức thời mà không cần reload trang. Ở Server-side, `ModelState.IsValid` được kiểm tra chặt chẽ và hiển thị hộp thoại cảnh báo Bootstrap chi tiết ở đầu trang nếu có lỗi gửi lên.
4. **Bộ lọc thông minh kỳ báo cáo cho User thường**:
   - Khi tài khoản khoa/phòng đăng nhập, hệ thống tự động kiểm tra các tần suất của các chỉ số mà khoa đó đang phụ trách (bao gồm tần suất mặc định và tần suất trong bảng `ChiSoTanSuatBaoCao`).
   - Danh sách "Kỳ báo cáo đang mở" sẽ được lọc để chỉ hiển thị các kỳ báo cáo có tần suất tương thích với tần suất của khoa phòng đó, giúp giao diện người dùng gọn gàng, tránh nhầm lẫn nhập liệu.

## 13. Hướng Phát Triển Tiếp Theo

- Bổ sung luồng duyệt/trả lại báo cáo đầy đủ nếu bệnh viện cần quy trình phê duyệt nhiều bước.
- Bổ sung trang quản lý tài khoản riêng nếu Admin cần CRUD tài khoản độc lập ngoài chức năng tạo tài khoản từ nhân viên.
- Bổ sung audit log đầy đủ cho thao tác sửa/xóa/khóa báo cáo.
- Bổ sung test tự động ở mức service cho parser import và nghiệp vụ báo cáo.
- Chuẩn hóa export Excel định dạng đẹp hơn thay vì chỉ CSV.
- Tích hợp kênh gửi thông báo ngoài hệ thống như email, SMS hoặc Zalo nếu bệnh viện cần nhắc việc ngoài web app.

## 14. Cập Nhật Ngày 26/05/2026

Đợt cập nhật này chốt lại quy trình báo cáo và nâng cấp thông báo theo phạm vi hiện tại của dự án:

- Admin không thấy báo cáo nháp của User và không sửa số liệu báo cáo.
- User chỉ sửa báo cáo khi còn `Nhap`; sau khi gửi thì chỉ xem.
- Khi gửi báo cáo sau hạn, service tự đặt trạng thái `QuaHan`.
- Admin có thể khóa cả báo cáo `DaGui` và `QuaHan`.
- Dashboard User hiển thị cảnh báo ngay khi đăng nhập nếu còn chỉ số chưa báo cáo, gần hạn hoặc quá hạn chưa nộp.
- Danh sách thông báo có link “Xem chi tiết”; tiêu đề/nội dung thông báo cũng có thể click.
- Trang `Views/Notification/Details.cshtml` hiển thị nội dung thông báo và danh sách chỉ số còn thiếu của kỳ báo cáo liên quan.
- Với thông báo `QuaHan`, trang chi tiết chỉ hiển thị các chỉ số quá hạn chưa nộp thuộc kỳ đó.
- Khi User mở chi tiết thông báo, hệ thống chỉ hiển thị nội dung; thao tác đánh dấu đã đọc dùng POST riêng có anti-forgery.

## 15. Cập Nhật Ngày 28/05/2026

Đợt cập nhật này hoàn thiện import chỉ số từ file DOCX nguồn:

- `IndicatorService.BuildIndicatorFromRow` gọi `InferFormulaType` khi file thiếu `LoaiCongThuc`.
- `InferFormulaType` bỏ số thứ tự đầu tên chỉ số trước khi nhận diện, nhận diện thêm `Tỷ suất`, `Công suất`, `Hiệu suất`, và ưu tiên `SoLuong` cho các chỉ số bắt đầu bằng `Số lượng`, `Số ca`, `Số lượt`.
- `InferUnit` tự gán `DonViTinh` khi file import không có đơn vị tính.
- Các đơn vị chi tiết đã hỗ trợ gồm `%`, các đơn vị tỷ số cụ thể, `giờ`, `phút`, `ngày`, `người`, `báo cáo`, `ca`, `lượt`, `buồng`, `điểm tiếp nối`, `cầu thang`, `mức độ`.
- Probe trên file `Phân chia các chỉ số dựa theo đơn vị thu thập và tổng hợp.docx` đọc được 55/55 chỉ số và 0 chỉ số thiếu `DonViTinh`.
- Dữ liệu cũ đã import trước khi cập nhật cần import lại hoặc chạy cập nhật bổ sung để điền `DonViTinh`.

## 16. Cập Nhật Ngày 30/05/2026 - Tạo Lịch Kỳ Báo Cáo Tự Động

Đợt cập nhật này bổ sung chức năng tạo lịch kỳ báo cáo tự động theo năm cho Admin. Mục tiêu là giảm thao tác tạo kỳ thủ công, nhưng vẫn giữ nguyên mô hình dữ liệu động của hệ thống: chỉ tạo `KyBaoCao`, không tạo sẵn `BaoCao` rỗng cho từng khoa/phòng và từng chỉ số.

### 16.1. Phạm vi chức năng

- Admin vào màn hình **Kỳ báo cáo** và bấm **Tạo lịch tự động**.
- Admin chọn năm cần tạo lịch.
- Admin chọn một hoặc nhiều loại kỳ cần sinh.
- Hệ thống hiển thị preview trước khi lưu, gồm cả kỳ sẽ tạo mới và kỳ đã tồn tại.
- Khi Admin xác nhận, hệ thống chỉ tạo các kỳ chưa tồn tại.
- User không thấy màn hình tạo lịch tự động.
- User chỉ thấy các kỳ đang **Mở** có tần suất khớp với chỉ số đang được phân công cho khoa/phòng của mình.

### 16.2. Các loại kỳ được hỗ trợ

| Loại kỳ nghiệp vụ | Enum trong code | Số kỳ tối đa trong một năm | Ghi chú |
|---|---|---:|---|
| Hàng ngày | `HangNgay` | 365 hoặc 366 | Mỗi ngày là một kỳ riêng. |
| Hàng tháng | `HangThang` | 12 | Mỗi tháng là một kỳ. |
| Hàng quý | `HangQuy` | 4 | Quý I, II, III, IV. |
| 6 tháng | `SauThang` | 2 | 6 tháng đầu năm và 6 tháng cuối năm. |
| 9 tháng | `ChinThang` | 1 | Từ 01/01 đến 30/09. |
| Hàng năm | `HangNam` | 1 | Từ 01/01 đến 31/12. |

Không sinh lịch tự động cho:

- `KhiPhatSinh`.
- `TruocSauKhiThucHien`.

Hai loại này phụ thuộc sự kiện nghiệp vụ thực tế, không phù hợp để tạo sẵn theo lịch năm.

### 16.3. Quy ước thời gian 00:00 và 23:59

Database hiện lưu `TuNgay`, `DenNgay`, `HanNop` theo kiểu ngày. Vì vậy hệ thống dùng quy ước nghiệp vụ sau:

- `TuNgay` = thời điểm mở kỳ lúc **00:00** của ngày bắt đầu.
- `DenNgay` = thời điểm đóng kỳ lúc **23:59** của ngày kết thúc.
- `HanNop` = hạn nộp cuối cùng lúc **23:59** của ngày kết thúc.

Ví dụ vận hành:

| Loại kỳ | Tên kỳ | Mở lúc | Đóng lúc | Hạn nộp cuối cùng |
|---|---|---|---|---|
| Hàng ngày | Ngày 30/05/2026 | 30/05/2026 00:00 | 30/05/2026 23:59 | 30/05/2026 23:59 |
| Hàng tháng | Tháng 06/2026 | 01/06/2026 00:00 | 30/06/2026 23:59 | 30/06/2026 23:59 |
| Hàng quý | Quý II/2026 | 01/04/2026 00:00 | 30/06/2026 23:59 | 30/06/2026 23:59 |
| 6 tháng | 6 tháng cuối năm 2026 | 01/07/2026 00:00 | 31/12/2026 23:59 | 31/12/2026 23:59 |
| 9 tháng | 9 tháng năm 2026 | 01/01/2026 00:00 | 30/09/2026 23:59 | 30/09/2026 23:59 |
| Hàng năm | Năm 2026 | 01/01/2026 00:00 | 31/12/2026 23:59 | 31/12/2026 23:59 |

Do database chỉ lưu ngày, phần `23:59` được thể hiện ở giao diện và trong tài liệu để người dùng hiểu đúng hạn cuối. Khi User gửi báo cáo, hệ thống đánh trễ khi **ngày hiện tại lớn hơn ngày hạn nộp**, không đánh trễ trong chính ngày `HanNop`.

### 16.4. Quy tắc bỏ qua kỳ đã kết thúc

Khi tạo lịch cho năm hiện tại, hệ thống không sinh các kỳ đã kết thúc trước hôm nay. Điều này tránh tình trạng ngày 30/05/2026 nhưng preview vẫn đề xuất tạo các kỳ như Tháng 01/2026 hoặc Tháng 02/2026.

Ví dụ nếu hôm nay là **30/05/2026**:

- Lịch hàng ngày bắt đầu từ Ngày 30/05/2026.
- Lịch hàng tháng bỏ qua Tháng 01, 02, 03, 04/2026.
- Tháng 05/2026 vẫn được preview vì kỳ này kết thúc ngày 31/05/2026.
- Tháng 06/2026 trở đi là kỳ tương lai.
- Kỳ 9 tháng năm 2026 vẫn được preview vì kết thúc ngày 30/09/2026.
- Kỳ năm 2026 vẫn được preview vì kết thúc ngày 31/12/2026.

Nếu Admin tạo lịch cho năm tương lai, hệ thống preview toàn bộ kỳ của năm đó. Nếu Admin tạo lịch cho năm đã qua, các kỳ đã kết thúc trước hôm nay sẽ không được đề xuất tạo mới.

### 16.5. Quy tắc trạng thái

Enum trong code vẫn giữ nguyên để không phá dữ liệu và luồng hiện có:

| Enum | Hiển thị | Ý nghĩa |
|---|---|---|
| `Nhap` | Nhập | Kỳ đã có trong hệ thống nhưng chưa tới ngày bắt đầu. |
| `Mo` | Mở | Kỳ đã tới ngày bắt đầu và User có thể nhập/gửi báo cáo. |
| `Khoa` | Khóa | Kỳ đã bị khóa, User không tiếp tục nhập/sửa. |

Khi tạo lịch:

- Nếu `TuNgay <= hôm nay`, kỳ được tạo ở trạng thái `Mo`.
- Nếu `TuNgay > hôm nay`, kỳ được tạo ở trạng thái `Nhap`.
- Các kỳ `Nhap` sẽ tự chuyển sang `Mo` khi tới ngày bắt đầu.

Hàm tự mở kỳ được gọi ở các điểm nhẹ:

- khi ứng dụng khởi động;
- khi truy cập Dashboard;
- khi truy cập Báo cáo;
- khi truy cập Kỳ báo cáo;
- khi truy cập Thông báo.

Giai đoạn này chưa dùng background scheduler phức tạp vì nhu cầu chỉ cần tự đồng bộ trạng thái theo ngày.

### 16.6. Chống trùng và không tạo báo cáo rỗng

Hệ thống chống trùng kỳ theo bộ khóa nghiệp vụ:

```text
LoaiKyBaoCao + TuNgay + DenNgay
```

Nếu kỳ đã tồn tại:

- preview hiển thị là **Đã tồn tại**;
- khi bấm tạo, hệ thống bỏ qua dòng đó;
- không cập nhật đè trạng thái, tên kỳ hoặc hạn nộp của kỳ cũ.

Chức năng này không sinh dòng `BaoCao` rỗng. Danh sách User cần báo cáo vẫn được tính động bằng truy vấn kết hợp:

- kỳ báo cáo đang `Mo`;
- tần suất kỳ báo cáo;
- chỉ số đang hoạt động;
- bảng tần suất phụ `ChiSoTanSuatBaoCao`;
- phân công chỉ số đang hoạt động;
- khoa/phòng của User;
- bản ghi `BaoCao` thực tế nếu đã lưu nháp hoặc gửi.

Nhờ vậy, nếu Admin thay đổi phân công sau khi tạo kỳ, danh sách cần báo cáo của User vẫn cập nhật đúng mà không cần tạo/xóa lại các báo cáo rỗng.

### 16.7. File kỹ thuật chính

- `Models/ViewModels/AppViewModels.cs`: chứa ViewModel tạo lịch và preview.
- `Services/ReportingPeriods/ReportingPeriodScheduleService.cs`: logic sinh kỳ, chống trùng, bỏ qua kỳ cũ và tự mở kỳ.
- `Areas/Admin/Controllers/ReportingPeriodController.cs`: thêm action `GenerateSchedule`, `PreviewSchedule`, `CreateSchedule`.
- `Areas/Admin/Views/ReportingPeriod/GenerateSchedule.cshtml`: màn hình Admin chọn năm, loại kỳ và xem preview.
- `Areas/Admin/Views/ReportingPeriod/Index.cshtml`: thêm nút **Tạo lịch tự động**.
- `Areas/Admin/Controllers/ReportingPeriodController.cs` và `Areas/Admin/Controllers/NotificationController.cs`: có endpoint POST có anti-forgery để Admin mở kỳ đến hạn hoặc chạy automation thủ công; các trang GET chính giữ nguyên read-only.
- `Global.asax.cs`: gọi tự mở kỳ khi ứng dụng khởi động.
- `Services/Reports/ReportService.cs`: xác định báo cáo trễ theo ngày hạn nộp, phù hợp quy ước hạn cuối 23:59.
- Kiểm tra chức năng tạo lịch tự động bằng build Razor view và checklist thủ công.

### 16.8. Kiểm thử cần giữ

Các kịch bản tối thiểu:

- Tạo lịch hàng ngày cho ngày hiện tại sinh kỳ hôm nay ở trạng thái **Mở**.
- Tạo lịch hàng tháng cho năm 2026 tại ngày 30/05/2026 bỏ qua Tháng 01-04/2026, giữ Tháng 05/2026 trở đi.
- Tạo lịch quý sinh đúng các quý còn hiệu lực.
- Tạo lịch 6 tháng sinh đúng kỳ còn hiệu lực.
- Tạo lịch 9 tháng sinh một kỳ từ 01/01 đến 30/09 nếu kỳ chưa kết thúc.
- Tạo lịch năm sinh một kỳ từ 01/01 đến 31/12 nếu kỳ chưa kết thúc.
- Chạy tạo lịch lần hai không tạo trùng.
- Kỳ tương lai là `Nhap`.
- Kỳ tới ngày bắt đầu tự chuyển sang `Mo`.
- User chỉ thấy kỳ `Mo` khớp tần suất chỉ số được phân công.
- Admin vẫn xem được toàn bộ kỳ báo cáo.

Lệnh kiểm tra:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard-demo\HospitalQualityDashboard-demo.csproj /p:Configuration=Debug /p:MvcBuildViews=true
```

## 17. Ghi Chú Về Dọn Dẹp File Dư Thừa (Ngày 10/06/2026)

Mục này lưu lịch sử dọn dẹp ngày 10/06/2026. Trạng thái được đối chiếu lại ngày 19/06/2026:

**Các mục đã xóa:**
- `bin/`, `obj/` — build artifact, có thể rebuild lại.
- `packages/` NuGet trùng — các bản `.0`, MVC5, Razor3, WebPages3 không được `.csproj` tham chiếu.
- `HospitalQualityDashboard-demo.csproj.user` — file cấu hình VS cá nhân.

**Các mục local hoặc tài nguyên phụ còn giữ:**

| Mục | Vị trí | Trạng thái |
|---|---|---|
| `.worktrees/` | Root repo | Thư mục local còn tồn tại; không thuộc mã ứng dụng. |
| `Content/` | CSS Bootstrap chính, minified và source map | Đang được project quản lý. |
| `Scripts/` | `bootstrap.bundle.*`, `bootstrap.esm.*`, `jquery-*.slim.*`, `*.map` | Đang được project quản lý; có thể rà soát riêng nếu cần giảm dung lượng. |

**Lưu ý:** Không xóa `.worktrees/`, `Content/` hoặc `Scripts/` chỉ dựa trên danh sách này; phải kiểm tra đăng ký Git worktree, `.csproj` và `BundleConfig` trước.

## 18. Cập Nhật Ngày 13/06/2026 - Cải tiến Tầng CSDL, Xuất Excel & Sửa Lỗi Hồ Sơ

Đợt cập nhật này cải tiến tầng CSDL bằng cách áp dụng giao dịch (Transaction), chuyển đổi định dạng xuất báo cáo từ CSV sang Excel (.xlsx), bổ sung tính năng chọn cột xuất Excel trên Dashboard Admin, sửa lỗi cập nhật hồ sơ cá nhân và dọn dẹp các thư mục tạm.

### 18.1. Sửa lỗi cập nhật hồ sơ cá nhân (Profile Update Bug)
- **Hành vi**: Khôi phục lại action POST `UpdateProfile` trong `AccountController.cs` và phương thức `UpdateProfile` trong `AuthService.cs`.
- **Bảo lưu dữ liệu**: Khi validation thất bại hoặc xảy ra ngoại lệ `InvalidOperationException` (như tài khoản chưa liên kết nhân viên), hệ thống nạp lại profile của user nhưng giữ nguyên dữ liệu vừa điền trên form để hiển thị lỗi mà không làm mất thông tin nhập liệu.
- **Tập tin chính**:
  - [AccountController.cs](file:///d:/Hoc_Tap/Thuc_Tap/HospitalQualityDashboard-demo/HospitalQualityDashboard-demo/Controllers/AccountController.cs): Cập nhật logic xử lý lỗi và lưu trạng thái form.
  - [AuthService.cs](file:///d:/Hoc_Tap/Thuc_Tap/HospitalQualityDashboard-demo/HospitalQualityDashboard-demo/Services/AuthService.cs): Bổ sung phương thức `UpdateProfile` và helper `GetNhanVienId`.

### 18.2. Áp dụng SqlTransaction cho các tác vụ nhiều bước
- **Mục tiêu**: Đảm bảo tính toàn vẹn dữ liệu (Atomicity), rollback toàn bộ nếu có bất cứ lỗi nào xảy ra trong quá trình cập nhật hoặc import dữ liệu nhiều bảng.
- **Cơ sở hạ tầng**: Bổ sung helper `ExecuteInTransaction` trong [DbServiceBase.cs](file:///d:/Hoc_Tap/Thuc_Tap/HospitalQualityDashboard-demo/HospitalQualityDashboard-demo/Services/DbServiceBase.cs) cùng các overload nhận `SqlConnection` và `SqlTransaction` để tái sử dụng.
- **Nghiệp vụ áp dụng**:
  - **Báo cáo**: Lưu nháp (`SaveDraft`) và Xóa (`Delete`) trong `Services/Reports/ReportService.cs`.
  - **Chỉ số**: Thêm/Sửa (`Save`), Xóa (`Delete`) và `Import` trong các partial tại `Services/Indicators/IndicatorService*.cs`.
  - **Danh mục**: `Import` khoa/phòng trong `Services/Departments/DepartmentService.cs` và nhân viên trong `Services/Employees/EmployeeService.cs`.

### 18.3. Thay thế CSV bằng Excel (.xlsx) & Thêm xuất Excel Dashboard
- **Chuyển đổi định dạng**: Thay đổi tất cả tính năng xuất dữ liệu (Khoa phòng, Nhân viên, Chỉ số, Báo cáo) từ định dạng CSV sang định dạng Excel thực tế (.xlsx) qua hàm `CreateXlsx` tự viết (sẵn có trong hệ thống), đổi MIME type thành `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`. Việc này giải quyết triệt để lỗi hiển thị font tiếng Việt có dấu.
- **Nhãn giao diện**: Thay đổi toàn bộ nút bấm từ "Xuất CSV" thành "Xuất Excel" trong tất cả các View của cả phân hệ Admin và User.
- **Xuất Excel Dashboard**:
  - Thêm nút "Xuất Excel" tại Bảng tiến độ Dashboard Admin.
  - Hiển thị modal `#dashboardExportModal` cho phép Admin chọn các cột: *Khoa / Phòng, Số báo cáo đã gửi, Tổng số chỉ số cần nộp, Tỷ lệ hoàn tất (%)*.
  - Xuất động dữ liệu tiến độ theo các cột được chọn thành file `tien-do-khoa-phong.xlsx`.
- **Tập tin chính**:
  - `Services/Exports/ExportService.cs`: Cập nhật `ExportService` và bổ sung `ExportDashboardProgress`.
  - [ExportController.cs (Admin)](file:///d:/Hoc_Tap/Thuc_Tap/HospitalQualityDashboard-demo/HospitalQualityDashboard-demo/Areas/Admin/Controllers/ExportController.cs), [ExportController.cs (User)](file:///d:/Hoc_Tap/Thuc_Tap/HospitalQualityDashboard-demo/HospitalQualityDashboard-demo/Areas/User/Controllers/ExportController.cs), [ExportController.cs (Root)](file:///d:/Hoc_Tap/Thuc_Tap/HospitalQualityDashboard-demo/HospitalQualityDashboard-demo/Controllers/ExportController.cs): Thay đổi MIME type, phần mở rộng `.xlsx` và thêm các action xử lý xuất Excel Dashboard.
  - [Index.cshtml (Admin Dashboard)](file:///d:/Hoc_Tap/Thuc_Tap/HospitalQualityDashboard-demo/HospitalQualityDashboard-demo/Areas/Admin/Views/Dashboard/Index.cshtml): Bổ sung nút bấm và modal chọn cột.

### 18.4. Dọn dẹp Repository
- Đã thực hiện xóa triệt để các thư mục dư thừa và tạm thời: `.claude/worktrees/`, `.superpowers/`, thư mục `docs/` ở root và `HospitalQualityDashboard-demo/docs/` để làm sạch repository.

### 18.5. Kiểm thử xác minh tự động
- Thêm các tập lệnh kiểm tra chất lượng mã nguồn:
  - Script verify tạm cho Profile/Dashboard Excel đã được xóa vì chỉ còn placeholder rỗng.
- Chạy kiểm thử thành công qua [run_tests.ps1](file:///C:/Users/maiva/.gemini/antigravity/brain/8f49f584-1d44-40b1-a5dd-9b390c10fa7d/scratch/run_tests.ps1).

## 19. Cập Nhật Ngày 13/06/2026 - Bộ Lọc Tần Suất Báo Cáo, Xếp Loại Hoàn Thành & Thống Kê Nâng Cao trên Dashboard & Excel

Đợt cập nhật này bổ sung bộ lọc tần suất báo cáo (Tháng, Quý, 6 tháng, Năm), cơ chế xếp loại hoàn thành dựa trên tỷ lệ và các cột thống kê tiến độ chuyên sâu cho Dashboard của Admin cùng tệp Excel xuất ra.

### 19.1. Bộ lọc Tần suất báo cáo trên Dashboard & Excel
- **Hành vi**: Thêm Dropdown chọn tần suất báo cáo trên thanh công cụ của Bảng tiến độ tại trang Dashboard Admin. Khi Admin thay đổi tần suất (ví dụ: chỉ chọn tần suất "6 tháng"), hệ thống sẽ:
  - Lọc lại toàn bộ số liệu thống kê (Tổng chỉ số, Đã báo cáo, Còn thiếu, Quá hạn) chỉ tính dựa trên các chỉ số thuộc tần suất được lọc.
  - Cập nhật lại biểu đồ cột tiến độ Chart.js.
  - Tích hợp hidden input lưu tần suất đang lọc vào modal xuất Excel. Khi bấm xuất, tệp Excel `tien-do-khoa-phong.xlsx` tải về sẽ chỉ chứa dữ liệu tiến độ của các chỉ số khớp với tần suất lọc.

### 19.2. Cơ chế Xếp loại Hoàn thành
- **Quy tắc xếp loại**:
  - **Xuất sắc**: Tỷ lệ hoàn tất >= 90%
  - **Khá**: 70% <= Tỷ lệ hoàn tất < 90%
  - **Trung bình**: 50% <= Tỷ lệ hoàn tất < 70%
  - **Yếu**: Tỷ lệ hoàn tất < 50%
  - **N/A**: Nếu tổng số chỉ số được giao bằng 0.
- **Badge màu sắc**: Giao diện hiển thị nhãn xếp loại bằng các thẻ Badge màu sắc Bootstrap tương ứng (`bg-success`, `bg-info`, `bg-warning`, `bg-danger`, `bg-secondary`).

### 19.3. Thống kê nâng cao & Tiến độ chi tiết
  - **Bảng tiến độ & Xuất Excel**: Bổ sung các cột thống kê nâng cao: 

  - Số báo cáo lưu nháp (`LuuNhap`), Số báo cáo còn thiếu (`ConThieu`), Xếp loại tổng thể (`XepLoai`).
  - Chi tiết tiến độ nộp báo cáo (dưới dạng chuỗi "đã nộp/tổng") và Xếp loại hoàn thành của các tần suất Hàng tháng, Hàng quý, Hàng năm.
- **Tối ưu hóa Database Access**: Sử dụng cơ chế truy vấn con (Subquery) cho từng khoa/phòng, đếm chính xác số lượng chỉ số và báo cáo theo từng tần suất để tránh Cartesian product từ phép JOIN trực tiếp.

### 19.4. Tập tin chính đã chỉnh sửa
- [AppViewModels.cs](file:///d:/Hoc_Tap/Thuc_Tap/HospitalQualityDashboard-demo/HospitalQualityDashboard-demo/Models/ViewModels/AppViewModels.cs): Mở rộng `DepartmentProgressViewModel` và `DashboardViewModel`.
- `Services/Dashboards/DashboardService*.cs`: Nâng cấp `GetDashboard` và thêm helper `CalculateXepLoai`.
- `Services/Exports/ExportService.cs`: Cập nhật `ExportDashboardProgress` và các cột cấu hình `DashboardProgressExportColumns`.
- [DashboardController.cs (Admin)](file:///d:/Hoc_Tap/Thuc_Tap/HospitalQualityDashboard-demo/HospitalQualityDashboard-demo/Areas/Admin/Controllers/DashboardController.cs): Tiếp nhận tham số lọc `tanSuat`.
- [ExportController.cs (Admin)](file:///d:/Hoc_Tap/Thuc_Tap/HospitalQualityDashboard-demo/HospitalQualityDashboard-demo/Areas/Admin/Controllers/ExportController.cs) & [ExportController.cs (Root)](file:///d:/Hoc_Tap/Thuc_Tap/HospitalQualityDashboard-demo/HospitalQualityDashboard-demo/Controllers/ExportController.cs): Tiếp nhận và truyền tham số `tanSuat`.
- [Index.cshtml (Admin Dashboard)](file:///d:/Hoc_Tap/Thuc_Tap/HospitalQualityDashboard-demo/HospitalQualityDashboard-demo/Areas/Admin/Views/Dashboard/Index.cshtml): Thêm form lọc, hiển thị badge xếp loại và cập nhật cấu hình modal xuất Excel.

## 20. Cập Nhật Ngày 15/06/2026 - Hướng Dẫn Sử Dụng, Bảo Mật Cấu Hình & Tối Ưu Tốc Độ

### 20.1. Tài liệu sử dụng

README root đã được viết lại để trở thành tài liệu sử dụng chính của dự án. Khi cần hướng dẫn chạy dự án, cấu hình Azure SQL, build, kiểm thử thủ công, import/export hoặc xử lý lỗi thường gặp, ưu tiên đọc:

```text
README.md
```

README hiện bao gồm yêu cầu môi trường, cách tạo `ConnectionStrings.config`, cách chạy bằng IIS Express, cách build bằng MSBuild, route chính, quy trình Admin/User, import/export, lưu ý hiệu năng Azure SQL, checklist kiểm thử thủ công và troubleshooting.

### 20.2. Connection string và secret

`ConnectionStrings.config` là file local secret và không được commit. Repo chỉ commit:

```text
HospitalQualityDashboard-demo/ConnectionStrings.example.config
```

Khi clone repo hoặc setup máy mới, developer cần copy file example thành file thật:

```powershell
Copy-Item .\HospitalQualityDashboard-demo\ConnectionStrings.example.config .\HospitalQualityDashboard-demo\ConnectionStrings.config
```

Sau đó điền `Server`, `Initial Catalog`, `User ID`, `Password` theo môi trường dev/test. Nếu password thật đã từng được commit hoặc xuất hiện trên GitHub, cần rotate password Azure SQL và rewrite history nếu muốn xóa khỏi lịch sử remote.

### 20.3. Hiệu năng Azure SQL hiện tại

Các tối ưu đã được áp dụng:

- `PageController` không gọi `AuthService.GetAuthenticatedUser()` ở mọi request; session revalidate sau 5 phút hoặc khi thiếu dữ liệu bắt buộc.
- Dashboard/Report không tự chạy `OpenDuePeriods` khi mở trang; thao tác mở kỳ đến hạn được giữ ở action thủ công.
- `DbServiceBase` hỗ trợ `CommandTimeout`, mặc định 30 giây.
- `DashboardService` dùng timeout 60 giây.
- Dropdown khoa/phòng, chỉ số, kỳ báo cáo dùng query nhẹ và cache 5 phút.
- Dashboard dùng query tổng hợp/CTE để giảm DB round-trip.
- Nhân viên, báo cáo, thông báo dùng phân trang server-side mặc định 20 dòng/trang.
- `App_Data/Sql/002_PerformanceIndexes.sql` bổ sung index đọc chính bằng `IF NOT EXISTS`.

### 20.4. Lưu ý vận hành

- Bootstrap database mặc định nên tắt trên môi trường đang dùng thật.
- Chỉ bật bootstrap có kiểm soát khi cần tạo schema hoặc bổ sung index.
- Với database Azure SQL đã tồn tại, nên chạy `002_PerformanceIndexes.sql` trực tiếp hoặc bật bootstrap tạm thời ở môi trường dev/test.
- Import nhân viên vẫn là luồng đồng bộ trong request; nếu file lớn và Azure SQL chậm, nên chia file nhỏ hoặc tối ưu riêng bằng background job trong đợt khác.

## 21. Cập Nhật Ngày 17/06/2026 - Đồng Bộ Tài Liệu, Audit Xuất Excel Và Script Verify

### 21.1. Tài liệu Markdown

Các file Markdown chính đã được đồng bộ theo hiện trạng code:

- `README.md`: hướng dẫn chạy, vận hành, export, audit và kiểm thử.
- `AGENTS.md`: hướng dẫn làm việc cho agent/dev trong repo.
- `PROJECT_CONTEXT.md`: kiến trúc, ánh xạ thành phần kỹ thuật và trạng thái script verify.
- `TAI_LIEU_NGHIEP_VU.md`: phạm vi Admin/User, ma trận chức năng và nghiệp vụ xuất Dashboard chi tiết.
- `Tai_Lieu/Phan Tich Thiet Ke He Thong Chi Tiet.md`: bổ sung thiết kế audit export.
- `Tai_Lieu/Lỗ hổng.md`: ghi chú trạng thái các rủi ro đã giảm nhẹ hoặc cần kiểm tra lại.

### 21.2. Audit lịch sử xuất Excel Dashboard

`DashboardExcelExportService` tạo workbook Dashboard chi tiết và ghi lịch sử xuất vào `LichSuXuatBaoCao`. Script schema tương ứng là:

```text
App_Data/Sql/003_AddExportHistory.sql
```

Thông tin audit gồm người xuất, vai trò, khoa/phòng, bộ lọc, tên file, số dòng dữ liệu, thời điểm xuất và địa chỉ IP. Admin có thể xuất toàn viện theo filter; User bị giới hạn về `KhoaPhongId` của tài khoản đăng nhập.

### 21.3. Script verify hiện có

Thư mục `tools/` hiện có các script:

- `VerifyDashboardExcelDetailedExport.ps1`
- `VerifyDashboardExcelUpgrade.ps1`
- `VerifyDashboardPeriodComparison.ps1`
- `VerifyDashboardAdminSummary.ps1`
- `VerifyDashboardMetricDetails.ps1`
- `VerifyEmployeeOrder.ps1`
- `VerifyIndicatorWarningMessages.ps1`
- `VerifyIndicatorWarnings.ps1`
- `VerifyManagementPaging.ps1`
- `VerifyReportResultAndExcelTime.ps1`
- `VerifyReportSubmissionNavigationAndAdminAudit.ps1`
- `VerifyUnreadNotificationBadge.ps1`

Các script này không thay thế build/Razor compile/checklist thủ công, nhưng giúp kiểm tra nhanh những luồng nghiệp vụ đã từng phát sinh lỗi.
