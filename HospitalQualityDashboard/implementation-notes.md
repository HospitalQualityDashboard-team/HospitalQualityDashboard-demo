# Ghi chú triển khai

## 2026-05-20

- Tiếp tục triển khai trên nhánh `develop` theo kế hoạch đã chốt, sau đó tạo nhánh feature cho từng cụm thay đổi.
- Giữ hướng truy cập dữ liệu bằng ADO.NET vì dự án hiện có `AuthService` dùng `SqlConnection` và chưa có EF6 package.
- Build bằng `dotnet msbuild HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU` đang bị chặn do thiếu `Microsoft.WebApplication.targets` trong SDK CLI. Cần xác minh lại bằng Visual Studio/IIS Express trên máy có workload ASP.NET MVC.
- V1 loại trừ upload minh chứng và workflow duyệt/trả lại, nên các bảng/action/view liên quan không được thêm vào.
- Theo yêu cầu mới, các file và thư mục trong `Controllers`, `Models`, `Services`, `Views` được đặt tên tiếng Anh. Tên bảng, cột, entity và property domain vẫn giữ theo schema hiện có để không phải viết migration đổi tên toàn hệ thống.
- Lệnh `dotnet add HospitalQualityDashboard\HospitalQualityDashboard.csproj package ClosedXML --version 0.95.4` không chạy được vì project MVC5 cần `Microsoft.WebApplication.targets` để tạo dependency graph. Tạm thời `ExcelImportExportService` dùng parser `.xlsx` tối thiểu dựa trên OpenXML/ZIP có sẵn trong .NET Framework và export CSV mở được bằng Excel; nên thay bằng ClosedXML trong Visual Studio khi NuGet restore hoạt động.
- Dashboard dùng Chart.js qua CDN để tránh ghi thêm asset thủ công khi chưa có pipeline package frontend.
