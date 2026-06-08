# Hướng dẫn làm việc trong repository

## Cấu trúc dự án và module

Repository này là ứng dụng web ASP.NET MVC 4 chạy trên .NET Framework 4.7.2. Code server chính nằm trong `Controllers/`, `Models/`, `Services/` và các Area `Areas/Admin`, `Areas/User`. Cấu hình khởi động MVC nằm trong `App_Start/`, entry point của ứng dụng là `Global.asax` và `Global.asax.cs`. Tài nguyên tĩnh nằm trong `Content/` cho CSS và `Scripts/` cho thư viện JavaScript. Tài liệu nghiệp vụ hệ thống chỉ số chất lượng bệnh viện nằm trong `Tai_Lieu/`. `App_Data/` dành cho dữ liệu ứng dụng local; không commit file database sinh ra hoặc dữ liệu riêng tư nếu không có yêu cầu rõ ràng.

## Lệnh build, kiểm thử và phát triển

Restore NuGet packages trước khi build:

```powershell
nuget restore HospitalQualityDashboard.csproj -PackagesDirectory ..\packages
```

Build project bằng MSBuild:

```powershell
msbuild HospitalQualityDashboard.csproj /p:Configuration=Debug
```

Build kèm kiểm tra Razor view:

```powershell
msbuild HospitalQualityDashboard.csproj /p:Configuration=Debug /p:MvcBuildViews=true
```

Khi phát triển local, mở `HospitalQualityDashboard.csproj` bằng Visual Studio và chạy bằng IIS Express. Project được cấu hình cho IIS Express với SSL port `44387`.

## Quy ước code và đặt tên

Dùng quy ước C#: PascalCase cho class, controller, action method, view model và public property; camelCase cho biến local và parameter. Tên controller kết thúc bằng `Controller`, ví dụ `HomeController`. Razor view phải khớp với tên action và nằm đúng thư mục Area/View tương ứng. Giữ indent 4 spaces cho C# và Razor. Controller nên nhỏ gọn, chỉ điều phối request; logic dùng lại đặt trong service hoặc model phù hợp.

## Hướng dẫn kiểm thử

Hiện chưa có test project riêng. Khi bổ sung test, tạo project như `HospitalQualityDashboard.Tests` và đặt tên rõ ràng theo mẫu `ControllerName_ActionName_ExpectedBehavior`. Ưu tiên test phân quyền, chuyển trạng thái báo cáo, tính toán chỉ số và validate import. Chạy đầy đủ test và script verify trước khi mở pull request.

## Quy ước commit và pull request

Dùng commit message ngắn gọn ở dạng mệnh lệnh, ví dụ `Add indicator assignment model` hoặc `Fix report status validation`. Pull request nên có tóm tắt thay đổi, module bị ảnh hưởng, bước kiểm thử thủ công, ảnh chụp màn hình nếu thay đổi UI và link đến issue/yêu cầu liên quan nếu có.

## Bảo mật và cấu hình

Không commit credential thật, connection string nhạy cảm, file upload bằng chứng hoặc dữ liệu nhạy cảm của bệnh viện. Cấu hình theo môi trường nên đặt trong transform file như `Web.Debug.config` và `Web.Release.config`. Luôn validate file upload/import và enforce quyền Admin/User ở server-side, không chỉ ẩn/hiện trên Razor view.
