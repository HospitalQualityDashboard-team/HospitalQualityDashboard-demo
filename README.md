# HospitalQualityDashboard-demo

HospitalQualityDashboard-demo là hệ thống quản lý bộ chỉ số chất lượng bệnh viện viết bằng ASP.NET MVC 4 trên .NET Framework 4.7.2. Ứng dụng hỗ trợ Admin quản lý danh mục, import dữ liệu từ Excel/Word, phân công chỉ số cho khoa/phòng, mở kỳ báo cáo, theo dõi tiến độ, gửi thông báo; đồng thời hỗ trợ User khoa/phòng nhập và gửi báo cáo định kỳ.

Tài liệu liên quan:

- [`PROJECT_STRUCTURE.md`](PROJECT_STRUCTURE.md): giải thích chi tiết chức năng của từng folder, từng file code tự viết và quan hệ giữa các module.
- `HospitalQualityDashboard-demo/PROJECT_CONTEXT.md`: bối cảnh kỹ thuật và nghiệp vụ.
- `HospitalQualityDashboard-demo/TAI_LIEU_NGHIEP_VU.md`: mô tả nghiệp vụ tổng hợp.
- `HospitalQualityDashboard-demo/implementation-notes.md`: nhật ký thay đổi kỹ thuật.
- `HospitalQualityDashboard-demo/Tai_Lieu/`: tài liệu BA, phân tích thiết kế, file mẫu import.

## 1. Công nghệ

- ASP.NET MVC 4.
- .NET Framework 4.7.2.
- C# và Razor `.cshtml`.
- Azure SQL / SQL Server qua ADO.NET thuần.
- Bootstrap, jQuery, jQuery Validate, Chart.js.
- NuGet packages trong `HospitalQualityDashboard-demo/packages.config`.

## 2. Chức năng chính

### Admin

- Dashboard tổng quan toàn viện theo từng slot cần nộp `(kỳ, khoa/phòng, chỉ số)`, bảo đảm tổng cần nộp bằng đã báo cáo cộng còn thiếu. Admin có thể bấm bốn thẻ tổng quan để xem danh sách chi tiết theo bộ lọc tần suất hiện tại; báo cáo đã nộp hiển thị thêm Đạt, Chưa đạt hoặc Chưa đánh giá.
- Dashboard tự kiểm tra nhắc hạn ở các mốc 10, 7, 3, 1 và 0 ngày. Admin có thể gửi cảnh báo riêng cho từng chỉ số chưa nộp, tối đa một lần mỗi ngày.
- Quản lý khoa/phòng.
- Quản lý nhân viên.
- Tạo tài khoản User từ nhân viên.
- Quản lý chỉ số chất lượng.
- Triển khai hoặc ngừng triển khai chỉ số theo vòng đời áp dụng; các kỳ đã bắt đầu vẫn được giữ lịch sử, còn kỳ mới chỉ tính các chỉ số đang có hiệu lực theo tần suất.
- Import chỉ số từ Excel/Word.
- Import khoa/phòng từ Excel.
- Import nhân viên từ Excel.
- Phân công chỉ số cho khoa/phòng.
- Tạo kỳ báo cáo thủ công hoặc tự động theo năm/tần suất.
- Mở kỳ báo cáo đến hạn bằng action thủ công.
- Xem danh sách báo cáo đã gửi/quá hạn/đã khóa.
- Khóa hoặc xóa báo cáo theo nghiệp vụ.
- Gửi thông báo thủ công.
- Chạy kiểm tra thông báo tự động.
- Xuất Excel cho dữ liệu cần theo dõi.

### User khoa/phòng

- Dashboard theo khoa/phòng đang đăng nhập.
- Bấm các thẻ tổng quan trên Dashboard User để xem danh sách chỉ số/báo cáo chi tiết, luôn bị giới hạn theo khoa/phòng của tài khoản đăng nhập.
- Xem chỉ số được phân công.
- Xem kỳ báo cáo đang mở phù hợp với tần suất chỉ số.
- Nhập số liệu báo cáo.
- Lưu nháp báo cáo.
- Gửi báo cáo.
- Xem thông báo, nhắc hạn, quá hạn.
- Xuất báo cáo và Dashboard chi tiết trong phạm vi khoa/phòng của mình.
- Cập nhật hồ sơ cá nhân.
- Đổi mật khẩu.

## 3. Cấu trúc thư mục

```text
HospitalQualityDashboard-demo/
├── HospitalQualityDashboard-demo.slnx
├── README.md
└── HospitalQualityDashboard-demo/
    ├── App_Data/
    │   └── Sql/
    │       ├── 001_CreateSchema.sql
    │       ├── 002_PerformanceIndexes.sql
    │       ├── 003_AddExportHistory.sql
    │       ├── 004_AddIndicatorWarning.sql
    │       └── 005_AddIndicatorDeploymentHistory.sql
    ├── App_Start/
    │   ├── BundleConfig.cs
    │   ├── FilterConfig.cs
    │   └── RouteConfig.cs
    ├── Areas/
    │   ├── Admin/
    │   │   ├── Controllers/
    │   │   └── Views/
    │   └── User/
    │       ├── Controllers/
    │       └── Views/
    ├── Content/
    ├── Controllers/
    ├── Models/
    │   ├── DTOs/
    │   ├── Entities/
    │   ├── Enums/
    │   └── ViewModels/
    ├── Scripts/
    ├── Services/
    ├── Tai_Lieu/
    ├── Views/
    ├── AGENTS.md
    ├── ConnectionStrings.example.config
    ├── Global.asax
    ├── HospitalQualityDashboard-demo.csproj
    ├── implementation-notes.md
    ├── packages.config
    ├── PROJECT_CONTEXT.md
    ├── TAI_LIEU_NGHIEP_VU.md
    └── Web.config
```

Các controller nghiệp vụ chính nằm trong `Areas/Admin` và `Areas/User`. Thư mục root `Controllers/` hiện chỉ còn `AccountController`, `HomeController` và `PageController`; các controller redirect tương thích cũ đã được xóa.

## 4. Yêu cầu môi trường

Cài đặt tối thiểu:

- Windows.
- Visual Studio có workload ASP.NET/.NET Framework.
- .NET Framework 4.7.2 Developer Pack hoặc Targeting Pack.
- IIS Express.
- Azure SQL hoặc SQL Server tương thích.
- PowerShell 5+.
- NuGet restore khả dụng trong Visual Studio hoặc dòng lệnh.

MSBuild thường nằm ở:

```powershell
C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe
```

Nếu máy dùng Visual Studio phiên bản khác, thay đường dẫn MSBuild tương ứng.

## 5. Cấu hình connection string

File cấu hình thật `HospitalQualityDashboard-demo/ConnectionStrings.config` không được commit lên Git vì có thể chứa mật khẩu database.

Repo chỉ commit file mẫu:

```text
HospitalQualityDashboard-demo/ConnectionStrings.example.config
```

### 5.1. Tạo file cấu hình local

Từ thư mục gốc repo, copy file mẫu:

```powershell
Copy-Item .\HospitalQualityDashboard-demo\ConnectionStrings.example.config .\HospitalQualityDashboard-demo\ConnectionStrings.config
```

Sau đó mở `HospitalQualityDashboard-demo/ConnectionStrings.config` và thay các giá trị:

```xml
<?xml version="1.0" encoding="utf-8"?>
<connectionStrings>
  <add name="HospitalQualityConnection"
       connectionString="Server=tcp:YOUR_SERVER.database.windows.net,1433;Initial Catalog=YOUR_DATABASE;Persist Security Info=False;User ID=YOUR_USER;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;ConnectRetryCount=3;ConnectRetryInterval=10;"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

Không đưa mật khẩu thật vào `ConnectionStrings.example.config`, README, issue, pull request hoặc ảnh chụp màn hình.

### 5.2. Web.config tham chiếu connection string

`Web.config` dùng:

```xml
<connectionStrings configSource="ConnectionStrings.config" />
```

Vì vậy khi chạy local phải có file `ConnectionStrings.config` nằm cùng cấp với `Web.config`.

### 5.3. Kiểm tra lỗi connection string

Nếu gặp lỗi:

```text
Missing connection string: HospitalQualityConnection.
```

Kiểm tra:

- File `HospitalQualityDashboard-demo/ConnectionStrings.config` đã tồn tại chưa.
- Tên connection string có đúng là `HospitalQualityConnection` không.
- XML có đúng root `<connectionStrings>` không.
- File có nằm đúng thư mục project, không phải thư mục repo root.

Nếu gặp lỗi login/timeout Azure SQL:

- Kiểm tra `Server`, `Initial Catalog`, `User ID`, `Password`.
- Kiểm tra firewall của Azure SQL đã cho IP máy dev truy cập chưa.
- Kiểm tra database đã tồn tại.
- Kiểm tra user SQL có quyền đọc/ghi schema ứng dụng.

## 6. Bootstrap database

Ứng dụng có `DatabaseBootstrapper` để chạy script SQL khi được bật cấu hình.

Các script chính:

- `App_Data/Sql/001_CreateSchema.sql`: tạo schema ban đầu.
- `App_Data/Sql/002_PerformanceIndexes.sql`: tạo index tối ưu hiệu năng, có `IF NOT EXISTS`.
- `App_Data/Sql/003_AddExportHistory.sql`: tạo bảng `LichSuXuatBaoCao` và index phục vụ audit lịch sử xuất Excel.
- `App_Data/Sql/004_AddIndicatorWarning.sql`: liên kết thông báo với chỉ số và bổ sung index chống gửi cảnh báo trùng.
- `App_Data/Sql/005_AddIndicatorDeploymentHistory.sql`: tạo bảng `LichSuTrienKhaiChiSo`, index và hàm `fn_ChiSoDuocTrienKhaiTrongKy` để lọc chỉ số theo vòng đời triển khai trong từng kỳ báo cáo.

Mặc định bootstrap nên tắt:

```xml
<add key="HospitalQualityBootstrapEnabled" value="false" />
<add key="HospitalQualityBootstrapCreateDatabase" value="false" />
```

Chỉ bật `HospitalQualityBootstrapEnabled=true` khi cần tạo schema hoặc bổ sung index trên môi trường dev/test. Sau khi chạy xong nên tắt lại.

Chỉ bật `HospitalQualityBootstrapCreateDatabase=true` khi tài khoản SQL có quyền tạo database và bạn thật sự muốn ứng dụng tự tạo database qua `master`.

## 7. Restore, build và chạy dự án

### 7.1. Mở bằng Visual Studio

1. Clone repo.
2. Tạo `ConnectionStrings.config` từ file mẫu.
3. Mở `HospitalQualityDashboard-demo.slnx` hoặc `HospitalQualityDashboard-demo/HospitalQualityDashboard-demo.csproj`.
4. Restore NuGet packages nếu Visual Studio chưa tự restore.
5. Chọn project `HospitalQualityDashboard-demo` làm startup project.
6. Chạy bằng IIS Express.

URL thường gặp:

```text
https://localhost:44387/
```

Port có thể khác tùy IIS Express local.

### 7.2. Build bằng command line

Từ thư mục gốc repo:

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

Nếu IIS Express đang giữ DLL, tắt IIS Express/Visual Studio rồi build lại.

## 8. Đường dẫn sử dụng chính

```text
/                           Trang Home
/Account/AdminLogin          Đăng nhập Admin
/Account/UserLogin           Đăng nhập User
/Account/Profile             Hồ sơ cá nhân
/Admin/Dashboard             Dashboard Admin
/Admin/Department            Khoa/phòng
/Admin/Employee              Nhân viên
/Admin/Indicator             Chỉ số chất lượng
/Admin/Assignment            Phân công chỉ số
/Admin/ReportingPeriod       Kỳ báo cáo
/Admin/Report                Báo cáo Admin
/Admin/Notification          Thông báo Admin
/User/Dashboard              Dashboard User
/User/Indicator              Chỉ số User được xem
/User/Report                 Báo cáo của khoa/phòng
/User/Notification           Thông báo User
```

Root controller như `/Dashboard`, `/Report`, `/Notification` vẫn redirect vào Area tương ứng để giữ tương thích, nhưng route chính nên dùng `/Admin/...` hoặc `/User/...`.

## 9. Quy trình sử dụng cho Admin

### 9.1. Chuẩn bị danh mục khoa/phòng

1. Đăng nhập Admin.
2. Vào `Admin > Khoa/phòng`.
3. Tạo khoa/phòng thủ công hoặc import Excel.
4. Kiểm tra trạng thái `Used` để đảm bảo khoa/phòng đang được dùng trong dropdown/filter.

### 9.2. Quản lý nhân viên

1. Vào `Admin > Nhân viên`.
2. Chọn khoa/phòng nếu muốn lọc.
3. Thêm nhân viên thủ công hoặc import Excel.
4. Với nhân viên chưa có tài khoản, bấm `Tạo tài khoản`.
5. Khóa/mở khóa nhân viên khi cần.

Danh sách nhân viên đã có phân trang server-side, mặc định 20 dòng/trang.

### 9.3. Quản lý chỉ số chất lượng

1. Vào `Admin > Chỉ số`.
2. Thêm/sửa chỉ số thủ công.
3. Import chỉ số từ Excel/Word nếu có file chuẩn.
4. Kiểm tra tần suất báo cáo, đơn vị tính, loại công thức, mục tiêu.
5. Dùng `Triển khai` hoặc `Ngừng triển khai` để điều chỉnh hiệu lực chỉ số. Hệ thống ghi lịch sử theo từng tần suất và dùng lịch sử này khi tính báo cáo, Dashboard, nhắc hạn và export.

Dropdown chỉ số dùng query nhẹ và cache ngắn hạn 5 phút để giảm tải Azure SQL.

### 9.4. Phân công chỉ số

1. Vào `Admin > Phân công`.
2. Chọn một hoặc nhiều khoa/phòng.
3. Chọn một hoặc nhiều chỉ số.
4. Xem preview để tránh phân công trùng.
5. Xác nhận phân công.
6. Có thể tạm dừng, kích hoạt, xóa hoặc thao tác hàng loạt.

### 9.5. Tạo và mở kỳ báo cáo

Có hai cách:

- Tạo thủ công từng kỳ trong `Admin > Kỳ báo cáo`.
- Tạo lịch tự động theo năm/tần suất trong màn hình tạo lịch.

Các trạng thái kỳ:

| Trạng thái | Ý nghĩa |
|---|---|
| Nhập | Kỳ đã tạo nhưng chưa mở cho User nhập. |
| Mở | User có thể nhập và gửi báo cáo. |
| Khóa | User không tiếp tục nhập/sửa báo cáo. |

Ứng dụng không tự chạy mở kỳ khi vào Dashboard/Report. Nếu cần mở các kỳ đã đến hạn, dùng action/nút thủ công trong module Kỳ báo cáo hoặc Thông báo.

### 9.6. Theo dõi báo cáo

1. Vào `Admin > Báo cáo`.
2. Lọc theo kỳ, khoa/phòng, chỉ số.
3. Xem báo cáo đã gửi, quá hạn, đã khóa.
4. Khóa báo cáo nếu cần chốt dữ liệu.
5. Xóa báo cáo nếu nghiệp vụ cho phép.

Danh sách báo cáo đã có phân trang server-side, mặc định 20 dòng/trang.

### 9.7. Gửi và kiểm tra thông báo

1. Vào `Admin > Thông báo`.
2. Gửi thông báo thủ công đến khoa/phòng.
3. Chạy kiểm tra tự động để tạo thông báo nhắc hạn/quá hạn.
4. Xem danh sách thông báo theo trang.

## 10. Quy trình sử dụng cho User khoa/phòng

### 10.1. Đăng nhập

1. Vào `/Account/UserLogin`.
2. Đăng nhập bằng tài khoản được Admin tạo từ nhân viên.
3. Kiểm tra menu chỉ hiển thị chức năng thuộc User.

### 10.2. Xem Dashboard

Dashboard User chỉ hiển thị dữ liệu trong phạm vi khoa/phòng của tài khoản đăng nhập:

- Chỉ số được phân công.
- Báo cáo đã gửi.
- Báo cáo còn thiếu.
- Báo cáo quá hạn.
- Danh sách cảnh báo thiếu báo cáo.

### 10.3. Nhập báo cáo

1. Vào `User > Báo cáo`.
2. Chọn kỳ báo cáo đang mở.
3. Bấm nhập báo cáo.
4. Nhập tử số/mẫu số hoặc giá trị trực tiếp tùy loại công thức.
5. Bấm `Lưu nháp` nếu chưa muốn gửi.
6. Bấm `Gửi` khi hoàn tất.

Sau khi gửi, báo cáo chuyển sang trạng thái đã gửi hoặc quá hạn tùy ngày gửi so với hạn nộp.

### 10.4. Xem thông báo

1. Vào `User > Thông báo`.
2. Mở chi tiết thông báo.
3. Với thông báo nhắc hạn/quá hạn, xem danh sách chỉ số còn thiếu nếu có.
4. Thông báo được đánh dấu đã đọc theo tài khoản.

Danh sách thông báo đã có phân trang server-side, mặc định 20 dòng/trang.

### 10.5. Cập nhật hồ sơ

1. Vào `/Account/Profile`.
2. Cập nhật họ tên, ngày sinh, giới tính, chức vụ, email, số điện thoại.
3. Đổi mật khẩu nếu cần.

## 11. Import dữ liệu

File mẫu/tài liệu nguồn nằm trong:

```text
HospitalQualityDashboard-demo/Tai_Lieu/
```

### 11.1. Import khoa/phòng

File cần có các cột hệ thống mong đợi như ID, IDKHOAPHONG, TENKHOAPHONG, USED. Sau import, danh sách khoa/phòng trong dropdown được xóa cache để cập nhật lại.

### 11.2. Import nhân viên

Import nhân viên hỗ trợ:

- Import theo khoa/phòng đang chọn.
- Import toàn viện nếu file có thông tin khoa/phòng.
- Tạo/cập nhật nhân viên theo mã nhân viên.
- Tạo tài khoản User mặc định nếu nhân viên chưa có tài khoản.

Lưu ý: import nhân viên có thể mất thời gian nếu file lớn hoặc Azure SQL phản hồi chậm. Chức năng này chưa được chuyển sang background job.

### 11.3. Import chỉ số

Import chỉ số hỗ trợ:

- Excel.
- Word `.docx` dạng bảng.
- Đọc tên chỉ số, mã chỉ số, định nghĩa, công thức, nguồn số liệu.
- Đọc nhiều tần suất báo cáo.
- Suy luận loại công thức khi file không khai báo rõ.
- Suy luận đơn vị tính khi file không có cột đơn vị tính.
- Phân công theo mô tả thu thập/tổng hợp nếu dữ liệu đủ rõ.

## 12. Export dữ liệu

Các màn hình Admin/User có nút xuất Excel tùy module:

- Nhân viên.
- Báo cáo.
- Phân công.
- Dashboard tiến độ (`DashboardProgress`): xuất bảng tổng hợp theo khoa/phòng, có thể chọn cột và lọc theo tần suất.
- Dashboard chi tiết (`Dashboard`): xuất workbook nhiều sheet gồm dữ liệu chi tiết, tổng hợp khoa/phòng, chỉ số còn thiếu, báo cáo chưa đạt mục tiêu và lịch sử duyệt/trả lại nếu có dữ liệu. Admin/User có thể bật `So sánh nhiều kỳ`, chọn một kỳ chính và tối đa 11 kỳ cũ hơn cùng tần suất; file bổ sung `SoSanhTongQuan` và `SoSanhChiSo`.
- Các danh sách nghiệp vụ khác nếu controller export hỗ trợ.

Khi export báo cáo, nên dùng filter trước để giảm dung lượng file và thời gian truy vấn. Cột `DatMucTieu` hiển thị nhãn `Đạt`, `Chưa đạt` hoặc `Chưa đánh giá` thay vì giá trị boolean `True/False`.

So sánh nhiều kỳ hỗ trợ tần suất tháng, quý, 6 tháng, 9 tháng và năm. Kỳ liền trước được chọn sẵn; các bộ lọc và phạm vi khoa/phòng được áp dụng giống nhau cho mọi kỳ. Sheet tổng quan tách `Nộp quá hạn` khỏi `Quá hạn chưa nộp`; sheet chi tiết không quy dữ liệu thiếu về `0` mà hiển thị trạng thái chưa nộp/không áp dụng/mới phát sinh/không còn phát sinh.

Mỗi lần xuất Dashboard chi tiết sẽ được ghi vào `LichSuXuatBaoCao` với người xuất, vai trò, bộ lọc, tên file, số dòng dữ liệu, thời gian xuất và địa chỉ IP. Nếu database đã tồn tại từ trước, cần chạy `App_Data/Sql/003_AddExportHistory.sql` hoặc bật bootstrap có kiểm soát để tạo bảng audit này.

## 13. Hiệu năng và vận hành Azure SQL

Các tối ưu hiện có:

- Session không gọi lại DB ở mọi request; revalidate sau 5 phút hoặc khi thiếu dữ liệu bắt buộc.
- Dropdown ít đổi có cache 5 phút.
- Query dropdown chỉ lấy cột cần thiết.
- Dashboard dùng query tổng hợp/CTE thay vì nhiều round-trip nhỏ.
- Danh sách nhân viên, báo cáo, thông báo có phân trang server-side.
- `DbServiceBase` có `CommandTimeout` mặc định 30 giây.
- Dashboard dùng timeout 60 giây.
- Không tự chạy bảo trì kỳ báo cáo khi mở Dashboard/Report.
- Script index hiệu năng nằm ở `App_Data/Sql/002_PerformanceIndexes.sql`.
- Script audit lịch sử xuất Excel nằm ở `App_Data/Sql/003_AddExportHistory.sql`.
- Script vòng đời triển khai chỉ số nằm ở `App_Data/Sql/005_AddIndicatorDeploymentHistory.sql`; các query báo cáo/dashboard/export dùng `fn_ChiSoDuocTrienKhaiTrongKy` để không tính chỉ số đã ngừng triển khai ngoài khoảng áp dụng.

Các index được đề xuất/tạo idempotent:

- `BaoCao(KyBaoCaoId, KhoaPhongId, ChiSoChatLuongId, TrangThai)`
- `PhanCongChiSo(KhoaPhongId, DangHoatDong, ChiSoChatLuongId)`
- `ChiSoTanSuatBaoCao(ChiSoChatLuongId, TanSuatBaoCao)`
- `ThongBaoNguoiNhan(TaiKhoanId, DaDoc)`
- `NhanVien(KhoaPhongId, HoTen)`

Nếu database Azure SQL đã tồn tại từ trước, hãy chạy script index trên database thật hoặc bật bootstrap có kiểm soát ở môi trường dev/test.

Nếu dùng chức năng xuất Dashboard chi tiết trên database cũ, hãy chạy thêm `003_AddExportHistory.sql` để tránh lỗi thiếu bảng `LichSuXuatBaoCao`.

Nếu dùng cảnh báo chỉ số trên Dashboard với database cũ, phải chạy `App_Data/Sql/004_AddIndicatorWarning.sql` trước khi khởi động tính năng.

Nếu dùng vòng đời triển khai/ngừng triển khai chỉ số trên database cũ, phải chạy `App_Data/Sql/005_AddIndicatorDeploymentHistory.sql` để tạo `LichSuTrienKhaiChiSo`, seed các chỉ số đang hoạt động và tạo hàm `fn_ChiSoDuocTrienKhaiTrongKy`.

## 14. Bảo mật

- Không commit `ConnectionStrings.config`.
- Không commit password, token, file dump database, file bệnh án hoặc dữ liệu nhạy cảm.
- Không đưa connection string thật vào ảnh chụp màn hình.
- Sau khi lộ password trong Git history, cần rotate password Azure SQL và rewrite history nếu muốn xóa khỏi lịch sử public/private remote.
- Phân quyền Admin/User phải kiểm tra ở server-side, không chỉ ẩn nút ở Razor.
- Session hết hạn sau 30 phút không hoạt động; cookie dùng `HttpOnly` và `SameSite=Lax`, còn Release transform bật `requireSSL`.
- Session được tái xác thực với database tối đa mỗi 5 phút; tài khoản bị khóa hoặc đổi quyền/khoa phòng sẽ bị cập nhật hoặc thu hồi phiên.
- Đăng nhập sai 5 lần khóa tạm tài khoản trong 15 phút.
- Import giới hạn file 5 MB, tối đa 10.000 dòng và kiểm tra tỷ lệ giải nén/nội dung ZIP Office.
- Mọi POST nghiệp vụ hiện có anti-forgery; dữ liệu xuất CSV được trung hòa tiền tố công thức và các luồng xuất chính dùng `.xlsx`.
- Khi rewrite Git history, dùng `--force-with-lease`, không dùng `--force` thường.

## 15. Kiểm tra thủ công sau khi thay đổi code

### Admin

1. Đăng nhập Admin.
2. Mở Dashboard, kiểm tra số liệu tổng quan.
3. Mở Nhân viên, kiểm tra filter và phân trang.
4. Mở Báo cáo, kiểm tra filter và phân trang.
5. Mở Thông báo, kiểm tra phân trang.
6. Tạo/sửa khoa phòng.
7. Tạo/sửa nhân viên.
8. Tạo tài khoản User.
9. Ngừng triển khai một chỉ số thử nghiệm rồi triển khai lại, kiểm tra danh sách chỉ số đổi nhãn đúng và Dashboard không tính chỉ số ngoài khoảng hiệu lực.
10. Import một file nhỏ để kiểm tra luồng import.
11. Tạo kỳ báo cáo.
12. Chạy mở kỳ báo cáo thủ công nếu cần.
13. Xuất Excel một danh sách có filter.
14. Xuất Dashboard chi tiết và kiểm tra file `.xlsx` có các sheet tổng hợp/chi tiết/còn thiếu; bật so sánh và kiểm tra thêm `SoSanhTongQuan`, `SoSanhChiSo`.
15. Nếu có quyền truy cập DB, kiểm tra `LichSuXuatBaoCao` ghi nhận lịch sử xuất.

### User

1. Đăng nhập User.
2. Mở Dashboard, kiểm tra chỉ thấy dữ liệu khoa/phòng của mình.
3. Mở Báo cáo, nhập nháp.
4. Gửi báo cáo.
5. Mở Thông báo và xem chi tiết.
6. Cập nhật hồ sơ cá nhân.
7. Đổi mật khẩu.
8. Xuất Dashboard/Báo cáo và xác nhận dữ liệu chỉ thuộc khoa/phòng của User.

### Script verify hiện có

Các script PowerShell trong `HospitalQualityDashboard-demo/tools/` dùng để kiểm tra nhanh các luồng đã từng sửa:

```text
VerifyDashboardAdminSummary.ps1
VerifyDashboardExcelDetailedExport.ps1
VerifyDashboardExcelUpgrade.ps1
VerifyDashboardPeriodComparison.ps1
VerifyDashboardMetricDetails.ps1
VerifyEmployeeOrder.ps1
VerifyIndicatorWarningMessages.ps1
VerifyIndicatorWarnings.ps1
VerifyIndicatorDeploymentLifecycle.ps1
VerifyManagementPaging.ps1
VerifyReportResultAndExcelTime.ps1
VerifyReportSubmissionNavigationAndAdminAudit.ps1
VerifyUnreadNotificationBadge.ps1
```

Các script này cần app local chạy được và có dữ liệu phù hợp; dùng chúng như kiểm tra bổ sung bên cạnh build, Razor compile và checklist thủ công.

## 16. Lỗi thường gặp

### Missing connection string

Thông báo:

```text
Missing connection string: HospitalQualityConnection.
```

Cách xử lý:

- Copy `ConnectionStrings.example.config` thành `ConnectionStrings.config`.
- Đảm bảo file nằm trong `HospitalQualityDashboard-demo/`.
- Đảm bảo connection name đúng là `HospitalQualityConnection`.

### Invalid object name

Ví dụ:

```text
Invalid object name 'dbo.KyBaoCao'
```

Cách xử lý:

- Database chưa có schema.
- Chạy `001_CreateSchema.sql` hoặc bật bootstrap có kiểm soát.
- Kiểm tra app đang trỏ đúng `Initial Catalog`.

### Execution Timeout Expired

Nguyên nhân thường gặp:

- Azure SQL phản hồi chậm.
- Thiếu index.
- Query trả quá nhiều dòng.
- Import file lớn chạy đồng bộ trong request.

Cách xử lý:

- Đảm bảo đã có `002_PerformanceIndexes.sql`.
- Dùng filter và phân trang.
- Kiểm tra firewall/region Azure SQL.
- Với import lớn, chia file nhỏ hơn hoặc tối ưu riêng luồng import.

### GitHub vẫn hiện contributor Claude

Nguyên nhân thường gặp:

- Commit message cũ có `Co-Authored-By: Claude ...`.
- Branch khác vẫn trỏ tới commit cũ.
- GitHub contributor graph còn cache.

Cách kiểm tra:

```powershell
git log --remotes --format="%H%n%B%n---END---" | Select-String -Pattern "Claude|noreply@anthropic.com"
```

Nếu không còn kết quả trên remote branches, thường chỉ cần chờ GitHub cập nhật cache.

## 17. Quy ước commit

Gợi ý format:

```text
feat: add reporting period schedule
fix: handle missing Azure SQL connection string
perf: optimize dashboard reads and paginated lists
docs: update project usage guide
chore: ignore local connection strings config
```

Không thêm trailer `Co-Authored-By` nếu không muốn GitHub hiển thị thêm contributor.
## So sánh tiến độ kỳ báo cáo

Dashboard Admin và User có ba tab: Tổng quan, So sánh kỳ báo cáo và Xu hướng. Chức năng phân tích số báo cáo đúng hạn, nộp trễ, chưa nộp và quá hạn chưa nộp giữa tối đa 12 kỳ cùng tần suất; không so sánh giá trị chuyên môn của chỉ số. Admin có thể lọc toàn viện hoặc từng khoa/phòng, còn User luôn bị giới hạn theo khoa/phòng trong session ở phía server.
