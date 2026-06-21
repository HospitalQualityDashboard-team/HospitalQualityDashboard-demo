# Hướng dẫn làm việc trong repository

## Cấu trúc dự án và module

Repository này là ứng dụng web ASP.NET MVC 4 chạy trên .NET Framework 4.7.2. Code server chính nằm trong `Controllers/` (Account, Home, PageController base), `Areas/Admin/`, `Areas/User/`, `Models/`, `Services/`. Cấu hình khởi động MVC nằm trong `App_Start/`, entry point của ứng dụng là `Global.asax` và `Global.asax.cs`. Tài nguyên tĩnh nằm trong `Content/` (bootstrap.css, Site.css) và `Scripts/` (jQuery, Bootstrap, jQuery Validate). Tài liệu nghiệp vụ hệ thống chỉ số chất lượng bệnh viện nằm trong `Tai_Lieu/`. `App_Data/` dành cho dữ liệu ứng dụng local; không commit file database sinh ra hoặc dữ liệu riêng tư nếu không có yêu cầu rõ ràng.

## Lệnh build, kiểm thử và phát triển

Restore NuGet packages trước khi build:

```powershell
nuget restore HospitalQualityDashboard-demo.csproj -PackagesDirectory ..\packages
```

Build project bằng MSBuild:

```powershell
msbuild HospitalQualityDashboard-demo.csproj /p:Configuration=Debug
```

Build kèm kiểm tra Razor view:

```powershell
msbuild HospitalQualityDashboard-demo.csproj /p:Configuration=Debug /p:MvcBuildViews=true
```

Khi phát triển local, mở `HospitalQualityDashboard-demo.csproj` bằng Visual Studio và chạy bằng IIS Express. Project được cấu hình cho IIS Express với SSL port `44387`.

Các script verify bổ sung nằm trong `tools/` và kiểm tra các luồng như tổng hợp/chi tiết Dashboard, Dashboard Excel, so sánh nhiều kỳ, cảnh báo chỉ số, badge thông báo chưa đọc, phân trang quản lý, thứ tự nhân viên, thời gian/kết quả báo cáo và audit điều hướng sau khi gửi báo cáo. Hiện thư mục có 12 script `Verify*.ps1`; chỉ chạy chúng khi app local, database và dữ liệu mẫu đã sẵn sàng.

## Quy ước code và đặt tên

Dùng quy ước C#: PascalCase cho class, controller, action method, view model và public property; camelCase cho biến local và parameter. Tên controller kết thúc bằng `Controller`, ví dụ `HomeController`. Razor view phải khớp với tên action và nằm đúng thư mục Area/View tương ứng. Giữ indent 4 spaces cho C# và Razor. Controller nên nhỏ gọn, chỉ điều phối request; logic dùng lại đặt trong service hoặc model phù hợp.

## Hướng dẫn kiểm thử

Hiện chưa có test project riêng. Khi bổ sung test, tạo project như `HospitalQualityDashboard.Tests` và đặt tên rõ ràng theo mẫu `ControllerName_ActionName_ExpectedBehavior`. Ưu tiên test phân quyền, chuyển trạng thái báo cáo, tính toán chỉ số và validate import. Chạy đầy đủ test và script verify trước khi mở pull request.

## Quy ước commit và pull request

Dùng commit message ngắn gọn ở dạng mệnh lệnh, ví dụ `Add indicator assignment model` hoặc `Fix report status validation`. Pull request nên có tóm tắt thay đổi, module bị ảnh hưởng, bước kiểm thử thủ công, ảnh chụp màn hình nếu thay đổi UI và link đến issue/yêu cầu liên quan nếu có.

## Bảo mật và cấu hình

Không commit credential thật, connection string nhạy cảm, file upload bằng chứng hoặc dữ liệu nhạy cảm của bệnh viện. Cấu hình theo môi trường nên đặt trong transform file như `Web.Debug.config` và `Web.Release.config`. Luôn validate file upload/import và enforce quyền Admin/User ở server-side, không chỉ ẩn/hiện trên Razor view.

`ConnectionStrings.config` là file local secret và đã được đưa vào `.gitignore`. Khi setup môi trường mới, copy `ConnectionStrings.example.config` thành `ConnectionStrings.config` rồi điền thông tin Azure SQL thật trên máy local hoặc môi trường deploy. Chỉ commit file example, không commit file config thật hoặc ảnh chụp có password.

Khi thay đổi hành vi vận hành, cấu hình, database, import/export hoặc hiệu năng, cập nhật `README.md` trước, sau đó bổ sung ngắn gọn vào `PROJECT_CONTEXT.md` hoặc `implementation-notes.md` nếu thay đổi ảnh hưởng người phát triển/người vận hành.

Hiện project có bốn script SQL trong `App_Data/Sql/`: `001_CreateSchema.sql`, `002_PerformanceIndexes.sql`, `003_AddExportHistory.sql` và `004_AddIndicatorWarning.sql`. Nếu chỉnh export Dashboard chi tiết hoặc audit lịch sử xuất, cập nhật script `003`; nếu chỉnh cảnh báo theo chỉ số hoặc cơ chế chống gửi trùng, cập nhật script `004` và phần hướng dẫn vận hành liên quan.

`Web.config` đặt session timeout 30 phút, cookie `HttpOnly` và `SameSite=Lax`; transform Release bắt buộc cookie HTTPS. Không hạ các thiết lập này khi sửa cấu hình môi trường.
