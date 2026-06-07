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

## 7. Quy Ước Chú Thích Code

Các file code tự viết của dự án đã được bổ sung chú thích tiếng Việt có dấu ở đầu file theo mẫu `Mục đích:`. Quy ước này giúp người đọc nhanh chóng hiểu vai trò của từng controller, service, view model, Razor view hoặc file cấu hình trước khi đi vào chi tiết.

Phạm vi chú thích:

- Có chú thích: `Controllers`, `Services`, `Models`, `Filters`, `App_Start`, `Global.asax.cs`, `Properties/AssemblyInfo.cs` và các Razor view tự viết trong `Views`.
- Không chú thích vào thư viện bên thứ ba như Bootstrap, jQuery, Modernizr hoặc file minified.
- Comment trong code ưu tiên giải thích vai trò, luồng nghiệp vụ hoặc lý do xử lý; tránh mô tả lại từng dòng code hiển nhiên.

## 8. Dữ Liệu Nguồn Và Import

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

## 9. Test Và Verification

Các script test nhanh nằm trong:

```text
HospitalQualityDashboard/tools/
```

Chạy từng script từ thư mục gốc repo.

### 9.1. Verify cấu hình MVC và database bootstrap

```powershell
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyMvc4Configuration.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyDatabaseBootstrapper.ps1
```

Mục tiêu:

- Kiểm tra project đang dùng MVC 4, Razor 2, WebPages 2.
- Kiểm tra `DatabaseBootstrapper` được gọi ở `Global.asax.cs`.
- Kiểm tra bootstrap đọc đúng connection string và chạy SQL scripts.

### 9.2. Verify parser Excel/Word và import chỉ số

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

### 9.3. Verify phân công và xuất Excel

```powershell
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyAssignmentExcelExport.ps1
```

Mục tiêu:

- Kiểm tra export Excel trên trang Phân công.
- Kiểm tra workbook xuất tiếng Việt và đúng các cột được chọn.

### 9.4. Verify tài khoản, đăng nhập và nút nhân viên

```powershell
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyEmployeeAccountButton.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyLockedEmployeeLogin.ps1
```

Mục tiêu:

- Kiểm tra luồng tạo tài khoản từ nhân viên.
- Kiểm tra tài khoản bị khóa không đăng nhập được.

### 9.5. Verify workflow báo cáo, thông báo và audit

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

### 9.6. Chạy full verification suite

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

## 10. Checklist Test Thủ Công Trên Trình Duyệt

### 10.1. Admin

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

### 10.2. User khoa/phòng

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

### 10.3. Inspect, Storage và session

Khi cần kiểm tra trạng thái đăng nhập trên trình duyệt:

1. Chạy app bằng IIS Express, thường tại `https://localhost:44387/`.
2. Mở DevTools bằng `F12` hoặc `Ctrl + Shift + I`.
3. Vào tab `Application` > `Storage` > `Cookies` > domain local của app.
4. Kiểm tra cookie `ASP.NET_SessionId`. Cookie này chỉ chứa mã session; các giá trị như `TaiKhoanId`, `LoaiTaiKhoan`, `KhoaPhongId` được lưu phía server qua `SessionUserAccessor`.
5. Nếu cột `Expires / Max-Age` hiển thị `Session`, cookie sẽ hết khi đóng phiên trình duyệt. Timeout server-side hiện chưa khai báo rõ trong `Web.config`, nên ASP.NET dùng mặc định khoảng 20 phút không hoạt động.
6. Sau logout, truy cập lại trang cần đăng nhập như `/Dashboard`; hệ thống phải chuyển về trang login.

## 11. Lỗi Thường Gặp

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

Nếu build bằng `bin_unit`, set `HQD_APP_ASSEMBLY` như mục 9.2.

### Import DOCX xong dữ liệu cũ vẫn thiếu đơn vị

Logic mới chỉ áp dụng khi import/chạy build model từ dòng import. Các bản ghi đã import trước đó cần import lại hoặc cập nhật lại dữ liệu trong database.

## 12. Tài Liệu Tham Khảo Trong Repo

- `HospitalQualityDashboard/PROJECT_CONTEXT.md`: bối cảnh tổng quan.
- `HospitalQualityDashboard/TAI_LIEU_NGHIEP_VU.md`: nghiệp vụ hệ thống.
- `HospitalQualityDashboard/implementation-notes.md`: nhật ký triển khai.
- `HospitalQualityDashboard/Tai_Lieu/Phan Tich Thiet Ke He Thong Chi Tiet.md`: tài liệu phân tích thiết kế.
- `HospitalQualityDashboard/Tai_Lieu/Yeu Cau Nghiep Vu BA.md`: tài liệu yêu cầu BA.

## 13. Cập Nhật Chức Năng Tạo Lịch Kỳ Báo Cáo Tự Động

Từ ngày 30/05/2026, hệ thống bổ sung chức năng **Tạo lịch tự động** cho module Kỳ báo cáo. Chức năng này giúp Admin không phải tạo từng kỳ thủ công, nhưng vẫn giữ nguyên nguyên tắc dữ liệu sạch: hệ thống chỉ tạo các dòng `KyBaoCao`, không tạo trước các dòng `BaoCao` rỗng.

### 13.1. Luồng sử dụng cho Admin

1. Admin đăng nhập và vào menu **Kỳ báo cáo**.
2. Bấm **Tạo lịch tự động**.
3. Chọn năm cần tạo lịch.
4. Chọn một hoặc nhiều loại kỳ:
   - Hàng ngày.
   - Hàng tháng.
   - Hàng quý.
   - 6 tháng.
   - 9 tháng.
   - Hàng năm.
5. Bấm **Xem trước** để hệ thống hiển thị danh sách kỳ dự kiến.
6. Kiểm tra các dòng **Sẽ tạo mới** và **Đã tồn tại**.
7. Bấm **Tạo các kỳ chưa tồn tại** để lưu các kỳ mới.

### 13.2. Quy tắc thời gian mở, đóng và hạn nộp

Hệ thống xem `TuNgay` là thời điểm mở kỳ lúc **00:00** của ngày bắt đầu, và xem `DenNgay`/`HanNop` là thời điểm đóng/hết hạn lúc **23:59** của ngày kết thúc.

Ví dụ:

| Loại kỳ | Tên kỳ | Mở lúc | Đóng lúc | Hạn nộp cuối cùng |
|---|---|---|---|---|
| Hàng ngày | Ngày 30/05/2026 | 30/05/2026 00:00 | 30/05/2026 23:59 | 30/05/2026 23:59 |
| Hàng tháng | Tháng 06/2026 | 01/06/2026 00:00 | 30/06/2026 23:59 | 30/06/2026 23:59 |
| Hàng quý | Quý II/2026 | 01/04/2026 00:00 | 30/06/2026 23:59 | 30/06/2026 23:59 |
| 6 tháng | 6 tháng cuối năm 2026 | 01/07/2026 00:00 | 31/12/2026 23:59 | 31/12/2026 23:59 |
| 9 tháng | 9 tháng năm 2026 | 01/01/2026 00:00 | 30/09/2026 23:59 | 30/09/2026 23:59 |
| Hàng năm | Năm 2026 | 01/01/2026 00:00 | 31/12/2026 23:59 | 31/12/2026 23:59 |

Vì cột `KyBaoCao.HanNop` trong database đang lưu kiểu `DATE`, thời điểm `23:59` được hiểu theo quy ước nghiệp vụ và hiển thị ở giao diện. Khi User gửi báo cáo, hệ thống so sánh theo ngày: chỉ khi ngày hiện tại lớn hơn ngày hạn nộp thì báo cáo mới bị đánh trạng thái `QuaHan`.

### 13.3. Quy tắc bỏ qua kỳ cũ

Khi tạo lịch cho năm hiện tại, hệ thống bỏ qua các kỳ đã kết thúc trước hôm nay.

Ví dụ nếu hôm nay là **30/05/2026**:

- Tạo lịch hàng ngày năm 2026 sẽ bắt đầu từ **Ngày 30/05/2026**, không tạo các ngày 01/01/2026 đến 29/05/2026.
- Tạo lịch hàng tháng năm 2026 sẽ không tạo Tháng 01, 02, 03, 04/2026 vì các kỳ này đã kết thúc.
- Tháng 05/2026 vẫn được đưa vào preview vì ngày 30/05/2026 vẫn nằm trong kỳ.
- Các kỳ tương lai như Tháng 06/2026, Quý III/2026 hoặc 6 tháng cuối năm 2026 được tạo ở trạng thái **Nhập**.

### 13.4. Quy tắc trạng thái kỳ

Giao diện hiển thị tiếng Việt:

| Enum trong code | Hiển thị | Ý nghĩa |
|---|---|---|
| `Nhap` | Nhập | Kỳ đã được tạo nhưng chưa tới ngày bắt đầu, User chưa nhập báo cáo. |
| `Mo` | Mở | Kỳ đã tới ngày bắt đầu, User có thể nhập và gửi báo cáo. |
| `Khoa` | Khóa | Kỳ đã bị khóa, User không tiếp tục nhập/sửa báo cáo. |

Khi tạo lịch tự động:

- Kỳ có `TuNgay <= hôm nay` được tạo hoặc tự chuyển sang **Mở**.
- Kỳ có `TuNgay > hôm nay` được tạo ở trạng thái **Nhập**.
- Hệ thống tự mở các kỳ đến ngày bắt đầu khi app khởi động hoặc khi người dùng truy cập Dashboard, Báo cáo, Kỳ báo cáo, Thông báo.

### 13.5. Nguyên tắc không tạo báo cáo rỗng

Chức năng tạo lịch tự động chỉ tạo dữ liệu trong bảng `KyBaoCao`. Hệ thống không tạo trước bản ghi trong bảng `BaoCao`.

Danh sách việc User cần báo cáo vẫn được tính động bằng cách kết hợp:

- kỳ báo cáo đang **Mở**;
- tần suất của kỳ;
- chỉ số đang hoạt động;
- phân công chỉ số đang hoạt động;
- khoa/phòng của User;
- báo cáo thực tế đã lưu/gửi hay chưa.

Chỉ khi User bấm **Lưu nháp** hoặc **Gửi báo cáo**, hệ thống mới tạo bản ghi `BaoCao` thật.

### 13.6. Kiểm tra chức năng tạo lịch

Script kiểm tra mới:

```powershell
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyReportingPeriodSchedule.ps1
```

Script này kiểm tra:

- có ViewModel tạo lịch tự động;
- có màn hình `Views/ReportingPeriod/GenerateSchedule.cshtml`;
- có action preview và tạo lịch trong `ReportingPeriodController`;
- có service tạo lịch và tự mở kỳ;
- có loại kỳ hàng ngày, hàng tháng, hàng quý, 6 tháng, 9 tháng, hàng năm;
- không tạo lịch cho `KhiPhatSinh` và `TruocSauKhiThucHien`;
- bỏ qua kỳ đã kết thúc trước hôm nay;
- hạn nộp mặc định bằng ngày kết thúc kỳ;
- UI hiển thị trạng thái tiếng Việt và ghi chú 00:00/23:59.

Nên chạy thêm:

```powershell
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyReportWorkflowAndNotifications.ps1
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:MvcBuildViews=true
```
