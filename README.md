# HospitalQualityDashboard

Hệ thống quản lý bộ chỉ số chất lượng bệnh viện bằng ASP.NET MVC 4. Ứng dụng hỗ trợ Admin quản lý danh mục, import dữ liệu từ Excel/Word, phân công chỉ số cho khoa/phòng, mở kỳ báo cáo, theo dõi tiến độ và hỗ trợ User khoa/phòng nhập báo cáo định kỳ.

## 1. Công Nghệ

- ASP.NET MVC 4 trên .NET Framework 4.7.2.
- C# và Razor `.cshtml`.
- SQL Server LocalDB qua ADO.NET thuần.
- Bootstrap, jQuery, jQuery Validate, Chart.js.
- NuGet packages khai báo trong `HospitalQualityDashboard/packages.config`.

## 2. Cấu Trúc Chính

```text
HospitalQualityDashboard/
├── App_Data/Sql/              # Script tạo schema, seed admin, migration bổ sung
├── Controllers/               # MVC controllers
├── Models/                    # Entity, enum, view model
├── Services/                  # Nghiệp vụ và ADO.NET data access
├── Views/                     # Razor views
├── Tai_Lieu/                  # Tài liệu nghiệp vụ, BA, SDD và file nguồn
├── tools/                     # Script verify/test nhanh
├── Web.config                 # Connection string và cấu hình ASP.NET
├── PROJECT_CONTEXT.md         # Bối cảnh kỹ thuật/nghiệp vụ
├── TAI_LIEU_NGHIEP_VU.md      # Tài liệu nghiệp vụ tổng hợp
└── implementation-notes.md    # Nhật ký triển khai
```

## 3. Yêu Cầu Môi Trường

Cài đặt:

- Windows.
- Visual Studio có workload ASP.NET/.NET Framework.
- .NET Framework 4.7.2 Developer Pack hoặc Targeting Pack.
- SQL Server LocalDB.
- PowerShell 5+.
- NuGet restore khả dụng trong Visual Studio hoặc MSBuild.

MSBuild thường nằm tại:

```powershell
C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe
```

Nếu máy dùng Visual Studio khác phiên bản, thay đường dẫn MSBuild tương ứng.

## 4. Cấu Hình Database

Connection string mặc định trong `HospitalQualityDashboard/Web.config`:

```xml
<add name="HospitalQualityConnection"
     connectionString="Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=HospitalQualityDashboard;Integrated Security=True;MultipleActiveResultSets=True"
     providerName="System.Data.SqlClient" />
```

Khi chạy ở Debug, `Global.asax.cs` gọi `DatabaseBootstrapper.BootstrapIfDebug()`. Bootstrapper sẽ:

1. Kiểm tra database `HospitalQualityDashboard`.
2. Tạo database nếu chưa tồn tại.
3. Chạy các script trong `HospitalQualityDashboard/App_Data/Sql`.
4. Seed tài khoản Admin mặc định.

Tài khoản Admin mặc định:

```text
Tên đăng nhập: admin
Mật khẩu: Admin@123
```

Nên đổi mật khẩu sau khi đăng nhập lần đầu nếu dùng dữ liệu thật.

## 5. Chạy Dự Án Bằng Visual Studio

1. Mở thư mục repo hoặc mở project `HospitalQualityDashboard/HospitalQualityDashboard.csproj`.
2. Restore NuGet packages nếu Visual Studio chưa tự restore.
3. Chọn project `HospitalQualityDashboard` làm startup project.
4. Chạy bằng IIS Express.
5. Mở:

```text
https://localhost:44387/
```

Một số route hữu ích:

```text
https://localhost:44387/Account/AdminLogin
https://localhost:44387/Account/UserLogin
https://localhost:44387/Dashboard
https://localhost:44387/Indicator
https://localhost:44387/Assignment
https://localhost:44387/Report
```

## 6. Build Bằng Command Line

Từ thư mục gốc repo:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug
```

Build kèm kiểm tra Razor view:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:MvcBuildViews=true
```

Nếu IIS Express hoặc Visual Studio đang giữ file trong `bin/obj` và build báo lỗi access denied, dùng output riêng để verify code mà không cần tắt app:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:BaseIntermediateOutputPath=obj_unit\ /p:OutputPath=bin_unit\
```

Build Razor view với output riêng:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:MvcBuildViews=true /p:BaseIntermediateOutputPath=obj_unit\ /p:OutputPath=bin_unit\
```

## 7. Dữ Liệu Nguồn Và Import

Các file tài liệu/nghiệp vụ nằm trong:

```text
HospitalQualityDashboard/Tai_Lieu/
```

Các chức năng import chính:

- Khoa/phòng từ Excel.
- Nhân viên từ Excel.
- Chỉ số từ Excel/Word.

Import chỉ số hiện hỗ trợ:

- Đọc bảng chỉ số từ DOCX.
- Map nhãn tiếng Việt sang field hệ thống.
- Nhận diện nhiều tần suất báo cáo.
- Nhận diện nhiều khoa/phòng từ `ThuThapTongHop`.
- Suy luận `LoaiCongThuc` khi file không khai báo rõ.
- Suy luận `DonViTinh` khi file không có cột đơn vị tính.

Với file `Phân chia các chỉ số dựa theo đơn vị thu thập và tổng hợp.docx`, hệ thống tự gán đủ đơn vị tính cho 55 chỉ số khi import mới. Dữ liệu đã import trước khi có logic này cần import lại hoặc chạy cập nhật bổ sung để điền `DonViTinh`.

## 8. Test Và Verification

Các script test nhanh nằm trong:

```text
HospitalQualityDashboard/tools/
```

Chạy từng script từ thư mục gốc repo.

### 8.1. Verify cấu hình MVC và database bootstrap

```powershell
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyMvc4Configuration.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyDatabaseBootstrapper.ps1
```

Mục tiêu:

- Kiểm tra project đang dùng MVC 4, Razor 2, WebPages 2.
- Kiểm tra `DatabaseBootstrapper` được gọi ở `Global.asax.cs`.
- Kiểm tra bootstrap đọc đúng connection string và chạy SQL scripts.

### 8.2. Verify parser Excel/Word và import chỉ số

```powershell
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyExcelParser.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyIndicatorFrequencyParser.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyIndicatorDepartmentAssignmentParser.ps1
```

Mục tiêu:

- Excel parser xử lý đúng shared string, inline string và ô trống.
- Parser nhận diện đúng tần suất báo cáo, đặc biệt các biến thể quý.
- Parser nhận diện nhiều khoa/phòng trong trường thu thập/tổng hợp số liệu.

Verify import công thức và đơn vị tính cần assembly đã build. Nếu dùng build mặc định:

```powershell
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyIndicatorFormulaImport.ps1
```

Nếu dùng output riêng `bin_unit`:

```powershell
$env:HQD_APP_ASSEMBLY = (Resolve-Path ".\HospitalQualityDashboard\bin_unit\HospitalQualityDashboard.dll")
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyIndicatorFormulaImport.ps1
Remove-Item Env:HQD_APP_ASSEMBLY
```

Mục tiêu:

- Kiểm tra alias nhãn DOCX như `Lý do chọn lựa`, `Thu nhập và tổng hợp số liệu`.
- Kiểm tra suy luận `LoaiCongThuc`.
- Kiểm tra suy luận `DonViTinh`.
- Kiểm tra file có sẵn `DonViTinh` thì không bị override.

### 8.3. Verify phân công và xuất Excel

```powershell
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyAssignmentExcelExport.ps1
```

Mục tiêu:

- Kiểm tra export Excel trên trang Phân công.
- Kiểm tra workbook xuất tiếng Việt và đúng các cột được chọn.

### 8.4. Verify tài khoản, đăng nhập và nút nhân viên

```powershell
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyEmployeeAccountButton.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyLockedEmployeeLogin.ps1
```

Mục tiêu:

- Kiểm tra luồng tạo tài khoản từ nhân viên.
- Kiểm tra tài khoản bị khóa không đăng nhập được.

### 8.5. Verify workflow báo cáo, thông báo và audit

```powershell
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyReportWorkflowAndNotifications.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyReportAuditLog.ps1
```

Mục tiêu:

- Kiểm tra luồng User lưu nháp/gửi báo cáo.
- Kiểm tra Admin chỉ xem/khóa/xóa theo phạm vi hiện tại.
- Kiểm tra dashboard cảnh báo thiếu báo cáo.
- Kiểm tra thông báo tự động và chi tiết thông báo.
- Kiểm tra ghi audit log các thao tác báo cáo.

### 8.6. Chạy full verification suite

Sau khi build project, có thể chạy lần lượt:

```powershell
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyMvc4Configuration.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyDatabaseBootstrapper.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyExcelParser.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyIndicatorFrequencyParser.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyIndicatorDepartmentAssignmentParser.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyIndicatorFormulaImport.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyAssignmentExcelExport.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyEmployeeAccountButton.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyLockedEmployeeLogin.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyReportWorkflowAndNotifications.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyReportAuditLog.ps1
```

Sau đó chạy build Razor view:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:MvcBuildViews=true
```

## 9. Checklist Test Thủ Công Trên Trình Duyệt

### 9.1. Admin

1. Đăng nhập tại `/Account/AdminLogin` bằng `admin / Admin@123`.
2. Vào Dashboard, kiểm tra thống kê tổng quan.
3. Vào Khoa/phòng, tạo/sửa/tạm dừng một khoa phòng thử nghiệm.
4. Vào Nhân viên, tạo nhân viên và tạo tài khoản User từ nhân viên.
5. Vào Chỉ số, tạo chỉ số mới và kiểm tra validation bắt buộc.
6. Import file chỉ số Word/Excel, kiểm tra `LoaiCongThuc`, `DonViTinh`, tần suất và trường `ThuThapTongHop`.
7. Vào Phân công, chọn nhiều khoa/phòng và nhiều chỉ số, kiểm tra preview AJAX.
8. Kiểm tra tạm dừng, kích hoạt, xóa đơn lẻ và thao tác hàng loạt.
9. Bấm xuất Excel ở trang Phân công, chọn một số cột và kiểm tra file tải về.
10. Tạo kỳ báo cáo đang mở với tần suất phù hợp.
11. Vào Báo cáo, kiểm tra Admin chỉ thấy báo cáo đã gửi/gửi trễ/đã khóa, không thấy bản nháp của User.
12. Vào Thông báo, gửi thông báo thủ công hoặc chạy kiểm tra thông báo tự động.

### 9.2. User khoa/phòng

1. Đăng nhập tại `/Account/UserLogin` bằng tài khoản User đã tạo.
2. Kiểm tra menu chỉ còn Tổng quan, Báo cáo của tôi, Thông báo, Đổi mật khẩu, Đăng xuất.
3. Kiểm tra Dashboard chỉ hiển thị dữ liệu thuộc khoa/phòng của User.
4. Vào Báo cáo của tôi, kiểm tra kỳ báo cáo được lọc theo tần suất chỉ số của khoa/phòng.
5. Nhập báo cáo dạng tỷ lệ/tỷ số, kiểm tra mẫu số bằng 0 bị chặn.
6. Nhập báo cáo dạng số lượng/thời gian/giá trị trực tiếp.
7. Lưu nháp, đăng xuất/đăng nhập lại và kiểm tra dữ liệu nháp còn tồn tại.
8. Gửi báo cáo, kiểm tra sau khi gửi giao diện chuyển sang chỉ đọc.
9. Nếu gửi sau hạn, kiểm tra trạng thái là `QuaHan`.
10. Mở thông báo có link chi tiết, kiểm tra danh sách chỉ số còn thiếu và trạng thái đã đọc.

## 10. Lỗi Thường Gặp

### Không kết nối được LocalDB

Kiểm tra SQL Server LocalDB đã cài và chạy:

```powershell
sqllocaldb info
sqllocaldb start MSSQLLocalDB
```

Nếu dùng SQL Server khác, sửa connection string trong `Web.config`.

### Build lỗi access denied ở `bin` hoặc `obj`

Thường do IIS Express hoặc Visual Studio đang giữ DLL. Có 2 cách:

- Dừng IIS Express/Visual Studio rồi build lại.
- Dùng output riêng `obj_unit/bin_unit` như mục build command line.

### VerifyIndicatorFormulaImport báo thiếu DLL

Build project trước khi chạy script:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug
```

Nếu build bằng `bin_unit`, set `HQD_APP_ASSEMBLY` như mục 8.2.

### Import DOCX xong dữ liệu cũ vẫn thiếu đơn vị

Logic mới chỉ áp dụng khi import/chạy build model từ dòng import. Các bản ghi đã import trước đó cần import lại hoặc cập nhật lại dữ liệu trong database.

## 11. Tài Liệu Tham Khảo Trong Repo

- `HospitalQualityDashboard/PROJECT_CONTEXT.md`: bối cảnh tổng quan.
- `HospitalQualityDashboard/TAI_LIEU_NGHIEP_VU.md`: nghiệp vụ hệ thống.
- `HospitalQualityDashboard/implementation-notes.md`: nhật ký triển khai.
- `HospitalQualityDashboard/Tai_Lieu/Phan Tich Thiet Ke He Thong Chi Tiet.md`: tài liệu phân tích thiết kế.
- `HospitalQualityDashboard/Tai_Lieu/Yeu Cau Nghiep Vu BA.md`: tài liệu yêu cầu BA.
