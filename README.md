# HospitalQualityDashboard-demo

`HospitalQualityDashboard-demo` là ứng dụng web ASP.NET MVC dùng để quản lý bộ chỉ số chất lượng bệnh viện. Hệ thống giúp phòng quản lý chất lượng/Admin thiết lập danh mục, phân công chỉ số cho khoa/phòng, mở kỳ báo cáo, theo dõi tiến độ nộp số liệu, nhắc hạn và xuất Excel; đồng thời giúp User của từng khoa/phòng nhập, lưu nháp, gửi và theo dõi báo cáo định kỳ trong phạm vi của mình.

## Chức năng chính

### Admin

- Quản lý danh mục khoa/phòng, nhân viên và tài khoản User.
- Quản lý định nghĩa chỉ số chất lượng: mã chỉ số, tên, định nghĩa, công thức, đơn vị tính, nguồn số liệu, tần suất và mục tiêu.
- Import dữ liệu từ Excel/Word cho khoa/phòng, nhân viên, chỉ số và phân công.
- Triển khai hoặc ngừng triển khai chỉ số theo vòng đời áp dụng; các kỳ đã có dữ liệu vẫn giữ lịch sử.
- Phân công một hoặc nhiều chỉ số cho một hoặc nhiều khoa/phòng, có preview và thao tác hàng loạt.
- Tạo kỳ báo cáo thủ công hoặc sinh lịch theo năm/tần suất; mở/khóa/xóa kỳ theo nghiệp vụ.
- Theo dõi Dashboard toàn viện theo slot cần nộp `(kỳ, khoa/phòng, chỉ số)`, gồm đã nộp, còn thiếu, quá hạn, đạt/chưa đạt mục tiêu.
- So sánh tiến độ giữa tối đa 12 kỳ cùng tần suất và xem xu hướng báo cáo.
- Gửi thông báo thủ công, chạy automation nhắc hạn/quá hạn và cảnh báo chỉ số chưa nộp.
- Xem lịch sử hệ thống và lịch sử xuất Dashboard Excel.
- Xuất Excel cho dashboard, báo cáo, nhân viên, phân công và các danh sách nghiệp vụ hỗ trợ export.

### User khoa/phòng

- Đăng nhập bằng tài khoản do Admin tạo từ hồ sơ nhân viên.
- Xem Dashboard chỉ trong phạm vi khoa/phòng của tài khoản.
- Xem các chỉ số được phân công và kỳ báo cáo đang mở.
- Nhập số liệu theo công thức chỉ số, lưu nháp và gửi báo cáo.
- Xem thông báo, nhắc hạn, báo cáo còn thiếu/quá hạn.
- Xuất báo cáo và Dashboard chi tiết theo scope khoa/phòng.
- Cập nhật hồ sơ cá nhân và đổi mật khẩu.

## Công nghệ

- ASP.NET MVC 4 trên .NET Framework 4.7.2.
- C# và Razor `.cshtml`.
- SQL Server/Azure SQL qua ADO.NET thuần, không dùng Entity Framework.
- Bootstrap 5, jQuery 3.7, jQuery Validate, Chart.js.
- ClosedXML/OpenXML cho import/export Excel và đọc bảng Word `.docx`.
- Visual Studio/IIS Express cho phát triển local.

## Tài liệu chính

- [PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md): bản đồ folder/file và trách nhiệm từng module.
- [HospitalQualityDashboard-demo/PROJECT_CONTEXT.md](HospitalQualityDashboard-demo/PROJECT_CONTEXT.md): bối cảnh nghiệp vụ, kiến trúc và vận hành.
- [HospitalQualityDashboard-demo/AGENTS.md](HospitalQualityDashboard-demo/AGENTS.md): quy ước code, build, kiểm thử và bảo mật.
- `HospitalQualityDashboard-demo/TAI_LIEU_NGHIEP_VU.md`: tài liệu nghiệp vụ chi tiết.
- `HospitalQualityDashboard-demo/Tai_Lieu/`: tài liệu BA/SDD, file mẫu import và tài liệu nguồn.

## Cấu trúc nhanh

```text
HospitalQualityDashboard-demo/
├── README.md
├── PROJECT_STRUCTURE.md
├── HospitalQualityDashboard-demo.slnx
└── HospitalQualityDashboard-demo/
    ├── App_Data/Sql/                 Script schema/migration SQL
    ├── App_Start/                    Route, filter, bundle MVC
    ├── Areas/Admin/                  Màn hình và endpoint Admin
    ├── Areas/User/                   Màn hình và endpoint User khoa/phòng
    ├── Content/                      CSS và hình ảnh
    ├── Controllers/                  Home, Account, base auth, maintenance
    ├── Models/                       Entity, enum, DTO, ViewModel
    ├── Scripts/                      JavaScript vendor và dashboard-analysis.js
    ├── Services/                     Nghiệp vụ và truy cập database
    ├── Tai_Lieu/                     Tài liệu nghiệp vụ/file mẫu
    ├── tools/                        Script PowerShell verify
    ├── Views/                        View root và partial dùng chung
    ├── AGENTS.md
    ├── PROJECT_CONTEXT.md
    ├── TAI_LIEU_NGHIEP_VU.md
    ├── Web.config
    └── ConnectionStrings.example.config
```

## Cấu hình local

File cấu hình thật `HospitalQualityDashboard-demo/ConnectionStrings.config` không được commit vì chứa credential. Khi setup máy mới, tạo file này từ mẫu:

```powershell
Copy-Item .\HospitalQualityDashboard-demo\ConnectionStrings.example.config .\HospitalQualityDashboard-demo\ConnectionStrings.config
```

Sau đó điền connection string:

```xml
<?xml version="1.0" encoding="utf-8"?>
<connectionStrings>
  <add name="HospitalQualityConnection"
       connectionString="Server=tcp:YOUR_SERVER.database.windows.net,1433;Initial Catalog=YOUR_DATABASE;Persist Security Info=False;User ID=YOUR_USER;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;ConnectRetryCount=3;ConnectRetryInterval=10;"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

`Web.config` tham chiếu file này bằng:

```xml
<connectionStrings configSource="ConnectionStrings.config" />
```

## Chạy dự án

1. Cài Visual Studio có workload ASP.NET/.NET Framework và .NET Framework 4.7.2 Developer Pack/Targeting Pack.
2. Tạo `ConnectionStrings.config` từ file mẫu.
3. Mở `HospitalQualityDashboard-demo.slnx` hoặc `HospitalQualityDashboard-demo/HospitalQualityDashboard-demo.csproj`.
4. Restore NuGet packages nếu Visual Studio chưa tự restore.
5. Chọn project `HospitalQualityDashboard-demo` làm startup project.
6. Chạy bằng IIS Express.

URL local thường gặp:

```text
https://localhost:44387/
```

## Build bằng dòng lệnh

Từ root repo:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard-demo\HospitalQualityDashboard-demo.csproj /p:Configuration=Debug /p:Platform=AnyCPU /m
```

Kiểm tra Razor view:

```powershell
$target = Join-Path $env:TEMP "hqd-aspnet-compiled"
if (Test-Path $target) { Remove-Item -Recurse -Force $target }
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\aspnet_compiler.exe" -p ".\HospitalQualityDashboard-demo" -v / $target
if (Test-Path $target) { Remove-Item -Recurse -Force $target }
```

## Database và migration

Các script SQL nằm trong `HospitalQualityDashboard-demo/App_Data/Sql/`:

- `001_CreateSchema.sql`: tạo schema ban đầu.
- `002_PerformanceIndexes.sql`: index hiệu năng idempotent.
- `003_AddExportHistory.sql`: bảng audit lịch sử xuất báo cáo.
- `004_AddIndicatorWarning.sql`: liên kết thông báo với chỉ số và chống gửi cảnh báo trùng.
- `005_AddIndicatorDeploymentHistory.sql`: lịch sử triển khai chỉ số và hàm lọc chỉ số có hiệu lực theo kỳ.

`DatabaseBootstrapper` có thể chạy script khi bật cấu hình:

```xml
<add key="HospitalQualityBootstrapEnabled" value="true" />
<add key="HospitalQualityBootstrapCreateDatabase" value="false" />
```

Chỉ bật bootstrap có kiểm soát ở dev/test hoặc khi cần nâng cấp schema. Với môi trường thật hoặc database ổn định, nên tắt bootstrap sau khi hoàn tất migration.

## Route sử dụng chính

```text
/                                      Trang Home
/Account/AdminLogin                    Đăng nhập Admin
/Account/UserLogin                     Đăng nhập User
/Account/Profile                       Hồ sơ cá nhân
/Admin/Dashboard                       Dashboard Admin
/Admin/Department                      Khoa/phòng
/Admin/Employee                        Nhân viên
/Admin/Indicator                       Chỉ số chất lượng
/Admin/Assignment                      Phân công chỉ số
/Admin/ReportingPeriod                 Kỳ báo cáo
/Admin/Report                          Báo cáo toàn viện
/Admin/Notification                    Thông báo Admin
/Admin/SystemLog                       Nhật ký hệ thống
/User/Dashboard                        Dashboard khoa/phòng
/User/Indicator                        Chỉ số được phân công
/User/Report                           Báo cáo của khoa/phòng
/User/Notification                     Thông báo User
/Maintenance/RunReportingPeriodAutomation  Endpoint POST bảo trì, yêu cầu token
```

## Kiểm thử và verify

Hiện repo chưa có test project riêng. Khi sửa code, tối thiểu cần build project và compile Razor nếu thay đổi view. Các script kiểm tra bổ sung nằm trong `HospitalQualityDashboard-demo/tools/`, ví dụ:

- `VerifyDashboardAdminSummary.ps1`
- `VerifyDashboardExcelDetailedExport.ps1`
- `VerifyDashboardPeriodComparison.ps1`
- `VerifyIndicatorDeploymentLifecycle.ps1`
- `VerifyReportingPeriodMaintenance.ps1`
- `VerifyReportDetailModal.ps1`
- `VerifySystemLogPage.ps1`
- `VerifySqlMigrations.ps1`
- `VerifyUnreadNotificationBadge.ps1`

Các script này cần app local, database và dữ liệu mẫu phù hợp; dùng chúng như lớp kiểm tra bổ sung cho các luồng từng phát sinh lỗi.

## Bảo mật

- Không commit `ConnectionStrings.config`, password, token, dump database, file bệnh án hoặc dữ liệu nhạy cảm.
- Không đưa connection string thật vào README, issue, pull request hoặc ảnh chụp màn hình.
- Phân quyền Admin/User phải kiểm tra ở server-side, không chỉ ẩn nút trong Razor.
- Session hết hạn sau 30 phút không hoạt động; cookie dùng `HttpOnly` và `SameSite=Lax`, Release transform bật HTTPS cookie.
- Đăng nhập sai nhiều lần bị khóa tạm tài khoản.
- Import giới hạn kích thước/dòng và kiểm tra nội dung ZIP Office để giảm rủi ro file độc hại.
