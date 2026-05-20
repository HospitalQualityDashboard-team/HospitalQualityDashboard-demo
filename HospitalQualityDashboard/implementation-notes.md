# Ghi chú triển khai

## 2026-05-20

- Tiếp tục triển khai trên nhánh `develop` theo kế hoạch đã chốt, sau đó tạo nhánh feature cho từng cụm thay đổi.
- Giữ hướng truy cập dữ liệu bằng ADO.NET vì dự án hiện có `AuthService` dùng `SqlConnection` và chưa có EF6 package.
- Build bằng `dotnet msbuild HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU` đang bị chặn do thiếu `Microsoft.WebApplication.targets` trong SDK CLI. Cần xác minh lại bằng Visual Studio/IIS Express trên máy có workload ASP.NET MVC.
- V1 loại trừ upload minh chứng và workflow duyệt/trả lại, nên các bảng/action/view liên quan không được thêm vào.
