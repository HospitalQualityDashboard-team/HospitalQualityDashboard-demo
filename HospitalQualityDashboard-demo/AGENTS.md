# Hướng dẫn làm việc trong repository

Tài liệu này dành cho developer và agent khi sửa `HospitalQualityDashboard-demo`. Ưu tiên đọc cùng `README.md`, `PROJECT_CONTEXT.md` và `PROJECT_STRUCTURE.md` trước khi thay đổi module nghiệp vụ lớn.

## 1. Tổng quan repo

Đây là ứng dụng ASP.NET MVC 4 chạy trên .NET Framework 4.7.2. Code server nằm trong:

- `Controllers/`: Home, Account, base controller và endpoint bảo trì.
- `Areas/Admin/`: chức năng quản trị toàn viện.
- `Areas/User/`: chức năng khoa/phòng.
- `Models/`: enum, entity, DTO, ViewModel.
- `Services/`: nghiệp vụ, truy cập SQL, import/export, notification, dashboard.
- `App_Data/Sql/`: schema và migration SQL.
- `Views/`, `Content/`, `Scripts/`: Razor, CSS và JavaScript.
- `tools/`: script PowerShell verify.

Không sửa trực tiếp file trong `bin/`, `obj/`, `.vs/` hoặc package restore artifact.

## 2. Lệnh build và chạy local

Restore NuGet từ root repo:

```powershell
nuget restore .\HospitalQualityDashboard-demo\HospitalQualityDashboard-demo.csproj -PackagesDirectory .\packages
```

Build từ root repo:

```powershell
msbuild .\HospitalQualityDashboard-demo\HospitalQualityDashboard-demo.csproj /p:Configuration=Debug /p:Platform=AnyCPU /m
```

Build kèm Razor view:

```powershell
msbuild .\HospitalQualityDashboard-demo\HospitalQualityDashboard-demo.csproj /p:Configuration=Debug /p:MvcBuildViews=true
```

Nếu `msbuild` không có trong PATH, dùng MSBuild của Visual Studio, ví dụ:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard-demo\HospitalQualityDashboard-demo.csproj /p:Configuration=Debug /p:Platform=AnyCPU /m
```

Chạy local bằng Visual Studio/IIS Express. Project thường chạy ở:

```text
https://localhost:44387/
```

## 3. Cấu hình và secret

- `ConnectionStrings.config` là file local secret, không commit.
- Khi setup môi trường mới, copy `ConnectionStrings.example.config` thành `ConnectionStrings.config`.
- Không đưa password, token, connection string thật, dump database, file bệnh án hoặc dữ liệu nhạy cảm vào Git, issue, PR, README hoặc ảnh chụp màn hình.
- `Web.config` đang dùng `configSource="ConnectionStrings.config"`; nếu thiếu file sẽ lỗi `Missing connection string: HospitalQualityConnection`.
- `HospitalQualityMaintenanceToken` bảo vệ endpoint `POST /Maintenance/RunReportingPeriodAutomation`; không để token thật trong tài liệu hoặc log.

## 4. Quy ước code

- C# dùng PascalCase cho class, controller, action, enum, public property; camelCase cho biến local và parameter.
- Controller kết thúc bằng `Controller`; Razor view phải khớp action và nằm đúng thư mục Area/View.
- Giữ indent 4 spaces cho C# và Razor.
- Controller chỉ điều phối request, kiểm quyền/scope và map dữ liệu; nghiệp vụ đặt trong service.
- Service truy cập database qua `DbServiceBase`; dùng parameterized SQL, không nối chuỗi input người dùng vào SQL.
- Các thao tác nhiều bước cần transaction qua helper có sẵn.
- DTO nằm trong `Models/DTOs`; ViewModel nằm trong `Models/ViewModels`; entity/enum dùng chung nằm trong `Models/Entities` và `Models/Enums`.
- Với class `partial`, đặt phần mở rộng cạnh module hiện có và giữ tên class nhất quán.
- Khi thêm file C#/view/static asset, kiểm tra `.csproj` kiểu cũ đã include file đúng chưa.

## 5. Quy ước nghiệp vụ

- Admin có scope toàn viện; User luôn bị khóa theo `KhoaPhongId` trong session.
- Không dựa vào ẩn/hiện nút trong Razor để bảo mật; phải kiểm quyền ở controller/service.
- Dashboard, Report, Export và Notification phải áp dụng scope giống nhau giữa màn hình và file xuất.
- Chỉ số đã ngừng triển khai không được tính cho kỳ ngoài khoảng hiệu lực; dùng logic/hàm liên quan `LichSuTrienKhaiChiSo` và `fn_ChiSoDuocTrienKhaiTrongKy`.
- Khi chỉnh trạng thái báo cáo/kỳ, rà enum, SQL, label giao diện, filter, export và notification.
- Khi chỉnh import, giữ giới hạn file/dòng và kiểm tra ZIP Office để giảm rủi ro file độc hại.

## 6. Database và migration

Các script SQL hiện có:

- `001_CreateSchema.sql`: schema ban đầu.
- `002_PerformanceIndexes.sql`: index hiệu năng.
- `003_AddExportHistory.sql`: audit xuất Dashboard Excel.
- `004_AddIndicatorWarning.sql`: cảnh báo theo chỉ số và chống gửi trùng.
- `005_AddIndicatorDeploymentHistory.sql`: vòng đời triển khai chỉ số.

Quy ước khi sửa SQL:

- Script migration phải idempotent nếu có thể chạy lại.
- Không phá dữ liệu hiện có khi nâng cấp database cũ.
- Nếu sửa Dashboard/Report/Export liên quan hiệu lực chỉ số, rà script `005`.
- Nếu sửa cảnh báo chỉ số hoặc notification dedup, rà script `004`.
- Nếu sửa audit/export Dashboard chi tiết, rà script `003`.
- Nếu sửa schema lõi, cập nhật `001` và migration bổ sung tương ứng.

## 7. Kiểm thử và verify

Hiện chưa có test project riêng. Sau khi sửa code, chọn mức kiểm tra theo rủi ro:

- Build project bằng MSBuild.
- Compile Razor nếu sửa `.cshtml`, layout, ViewModel hoặc route.
- Chạy script verify liên quan trong `HospitalQualityDashboard-demo/tools/` khi app local/database/dữ liệu mẫu đã sẵn sàng.
- Kiểm tra thủ công cả Admin và User nếu thay đổi scope, Dashboard, Report, Export hoặc Notification.

Một số script verify thường dùng:

```text
VerifyDashboardAdminSummary.ps1
VerifyDashboardExcelDetailedExport.ps1
VerifyDashboardPeriodComparison.ps1
VerifyIndicatorDeploymentLifecycle.ps1
VerifyManagementPaging.ps1
VerifyNotificationDropdown.ps1
VerifyReportDetailModal.ps1
VerifyReportingPeriodMaintenance.ps1
VerifySqlMigrations.ps1
VerifySystemLogPage.ps1
VerifyUnreadNotificationBadge.ps1
```

## 8. Quy ước tài liệu

- Khi thay đổi hành vi người dùng, cập nhật `README.md`.
- Khi thay đổi kiến trúc, module, scope dữ liệu, database, bảo mật hoặc vận hành, cập nhật `PROJECT_CONTEXT.md`.
- Khi thêm/đổi/xóa folder hoặc file tự viết, cập nhật `PROJECT_STRUCTURE.md`.
- Khi thay đổi quy trình làm việc, build, verify, secret hoặc convention, cập nhật `AGENTS.md`.
- Khi thay đổi nghiệp vụ chi tiết, cập nhật `TAI_LIEU_NGHIEP_VU.md` hoặc tài liệu trong `Tai_Lieu/` nếu phù hợp.

## 9. Commit và pull request

Commit message nên ngắn, rõ, dạng mệnh lệnh hoặc conventional commit:

```text
docs: update project business documentation
fix: apply user scope to dashboard export
perf: optimize report dashboard query
```

Pull request nên có:

- Tóm tắt thay đổi.
- Module bị ảnh hưởng.
- Các bước đã kiểm thử.
- Ảnh chụp nếu thay đổi UI.
- Ghi chú migration/config nếu có.

Không thêm trailer `Co-Authored-By` nếu không muốn GitHub hiển thị thêm contributor.
