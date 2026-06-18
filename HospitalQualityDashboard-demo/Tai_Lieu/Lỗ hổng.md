# Báo cáo lỗ hổng bảo mật

> Cập nhật tài liệu ngày 17/06/2026: một số phát hiện trong báo cáo này đã được giảm nhẹ bởi các thay đổi sau ngày rà soát ban đầu. Cụ thể, export CSV đã được chuyển sang Excel `.xlsx`, một số thao tác DB nhiều bước đã dùng transaction, `Web.config` đã có `httpOnlyCookies`, `sessionState timeout="30"` và `cookieSameSite="Lax"`, và Dashboard export chi tiết đã có audit `LichSuXuatBaoCao`. Các mục bên dưới vẫn nên được kiểm tra lại trên code hiện tại trước khi đóng hẳn.

Ngày kiểm tra: 02/06/2026  
Phạm vi: toàn bộ dự án `HospitalQualityDashboard` trong repository hiện tại.  
Phương pháp: đọc mã nguồn, rà soát theo bề mặt runtime, đối chiếu controller, service, schema SQL, view Razor, cấu hình web và các luồng import/export. Không triển khai khai thác động vì dự án cần IIS Express/LocalDB và dữ liệu thật để chạy đầy đủ; các kết luận dưới đây được xác thực bằng truy vết tĩnh từ điểm vào đến sink/điểm kiểm soát.

## Tóm tắt

| Mức độ | Số lượng | Nhóm lỗi chính |
|---|---:|---|
| Cao | 3 | IDOR/ghi sai phạm vi báo cáo, mật khẩu dự đoán được, session không bị thu hồi khi tài khoản bị khóa |
| Trung bình | 6 | Rò rỉ dữ liệu nhân viên, brute force đăng nhập, session/cookie chưa harden, CSV formula injection, DoS khi import file nén/XML lớn, thao tác DB nhiều bước thiếu transaction |
| Thấp | 2 | POST đọc dữ liệu thiếu anti-forgery, debug production/config hardening |

## 1. User có thể sửa hoặc tạo báo cáo ngoài phạm vi hợp lệ

Mức độ: Cao  
Loại: IDOR/BOLA, broken object-level authorization, mass assignment qua hidden fields  
CWE liên quan: CWE-639, CWE-862, CWE-915  
Vị trí:

- `Controllers/ReportController.cs:90-100`
- `Services/ReportDashboardServices.cs:128-168`
- `Views/Report/Edit.cshtml:122-126`
- `App_Data/Sql/001_CreateSchema.sql:130`

### Bằng chứng

Form nhập báo cáo đưa các trường quan trọng vào hidden fields: `BaoCaoId`, `KyBaoCaoId`, `KhoaPhongId`, `ChiSoChatLuongId`, `PhanCongChiSoId` tại `Views/Report/Edit.cshtml:122-126`. Ở `ReportController.Edit(ReportEntryViewModel model)`, server chỉ kiểm tra:

```csharp
var gate = EnsureUserDepartment(model.KhoaPhongId);
```

tại `Controllers/ReportController.cs:97`, tức là kiểm tra dựa trên `KhoaPhongId` do client gửi lên. Sau đó controller gọi `_service.SaveDraft(model, CurrentTaiKhoanId.Value)` tại `Controllers/ReportController.cs:100`.

Trong `ReportService.SaveDraft`, nếu `BaoCaoId > 0`, service chỉ kiểm tra bản ghi có trạng thái `Nhap`:

```csharp
SELECT COUNT(*) FROM dbo.BaoCao WHERE BaoCaoId=@Id AND TrangThai=@Nhap
```

tại `Services/ReportDashboardServices.cs:156`. Điều kiện này không kiểm tra `BaoCaoId` có thuộc `KhoaPhongId` của user hiện tại hay không. Sau đó chi tiết báo cáo được cập nhật theo `BaoCaoId` tại `Services/ReportDashboardServices.cs:167`.

Nếu tạo báo cáo mới, service tin `KyBaoCaoId`, `ChiSoChatLuongId` và `PhanCongChiSoId` từ model. Khi `PhanCongChiSoId > 0`, service dùng trực tiếp giá trị đó để insert `BaoCao` tại `Services/ReportDashboardServices.cs:145-151`. Schema chỉ có foreign key riêng lẻ cho `PhanCongChiSoId`; không có ràng buộc composite bảo đảm `PhanCongChiSoId` khớp với cùng `KhoaPhongId` và `ChiSoChatLuongId`.

### Luồng khai thác

1. User đăng nhập hợp lệ và mở một form nhập báo cáo để lấy anti-forgery token.
2. User POST `/Report/Edit` nhưng sửa hidden fields.
3. Trường hợp sửa dữ liệu phòng khác: đặt `BaoCaoId` là ID bản nháp của phòng khác, nhưng đặt `KhoaPhongId` là khoa của mình. Controller cho qua vì kiểm tra `KhoaPhongId` từ request.
4. Trường hợp tạo dữ liệu sai phạm vi: đặt `KyBaoCaoId`, `ChiSoChatLuongId` hoặc `PhanCongChiSoId` không tương ứng với phân công/kỳ hợp lệ.
5. Service cập nhật hoặc insert dữ liệu dựa trên các ID đã bị sửa.

### Tác động

User có thể sửa dữ liệu nháp của khoa/phòng khác nếu đoán hoặc biết `BaoCaoId`. User cũng có thể tạo báo cáo không khớp phân công, không khớp kỳ hoặc không khớp tần suất. Đây là lỗi trực tiếp ảnh hưởng tính toàn vẹn dữ liệu báo cáo chất lượng bệnh viện.

### Khuyến nghị

- Không tin hidden fields cho các ID kiểm soát quyền.
- Khi `BaoCaoId > 0`, load bản ghi từ DB rồi kiểm tra `report.KhoaPhongId == CurrentKhoaPhongId` trước khi cập nhật.
- Khi tạo mới, server phải tự xác định `KhoaPhongId` từ session, tự xác định phân công hợp lệ từ DB, và kiểm tra kỳ đang mở, tần suất khớp, chỉ số đang hoạt động, phân công đang hoạt động.
- Không nhận `PhanCongChiSoId` từ client; nếu cần, chỉ dùng để đối chiếu với bản ghi server đã truy vấn.
- Thêm test cho các trường hợp: sửa `BaoCaoId` phòng khác, sửa `KhoaPhongId` hidden, tạo report với `PhanCongChiSoId` không khớp, tạo report cho kỳ đóng/sai tần suất.

## 2. Import nhân viên tự tạo mật khẩu có thể dự đoán được

Mức độ: Cao  
Loại: weak default credential  
CWE liên quan: CWE-521, CWE-798 theo nghĩa mật khẩu mặc định/dự đoán được  
Vị trí:

- `Services/ManagementServices.cs:339-349`
- `App_Data/Sql/001_CreateSchema.sql:31-43`
- `Controllers/AccountController.cs:61-82`

### Bằng chứng

Khi import nhân viên, nếu chưa có tài khoản, hệ thống tạo tài khoản mới với:

```csharp
Param("@TenDangNhap", model.MaNhanVien),
Param("@MatKhauHash", PasswordHasher.Hash(model.MaNhanVien))
```

tại `Services/ManagementServices.cs:347-348`. Tài khoản được bật ngay (`DangHoatDong = 1`) tại `Services/ManagementServices.cs:345-346`.

Schema `TaiKhoan` tại `App_Data/Sql/001_CreateSchema.sql:31-43` không có cột đánh dấu phải đổi mật khẩu lần đầu. Luồng đăng nhập trong `AccountController.cs:61-82` chỉ xác thực rồi set session, không bắt đổi mật khẩu đối với tài khoản mới import.

### Luồng khai thác

Nếu file import có nhân viên `MaNhanVien = NV001`, hệ thống tạo user `NV001` với mật khẩu ban đầu cũng là `NV001`. Người biết hoặc đoán được mã nhân viên có thể đăng nhập nếu tài khoản chưa đổi mật khẩu.

### Tác động

Đây là rủi ro chiếm quyền tài khoản user ở các khoa/phòng, đặc biệt vì mã nhân viên thường không phải bí mật và có thể xuất hiện trong danh sách nhân sự, email nội bộ hoặc file import.

### Khuyến nghị

- Không dùng `MaNhanVien` làm mật khẩu.
- Tạo mật khẩu ngẫu nhiên đủ mạnh hoặc luồng kích hoạt tài khoản bằng token một lần.
- Thêm cột `MustChangePassword` hoặc `PasswordResetRequired`, bắt đổi mật khẩu trước khi vào hệ thống.
- Ghi log và thông báo rõ khi tài khoản được tạo tự động.
- Cân nhắc không tự tạo tài khoản hàng loạt nếu chưa có quy trình cấp phát mật khẩu an toàn.

## 3. Tài khoản bị khóa hoặc đổi quyền vẫn giữ session cũ

Mức độ: Cao  
Loại: broken session invalidation / stale authorization  
CWE liên quan: CWE-613, CWE-285  
Vị trí:

- `Services/AuthService.cs:48-83`
- `Services/SessionUserAccessor.cs:54-58`
- `Controllers/PageController.cs:49-57`

### Bằng chứng

Khi đăng nhập, `AuthService` có kiểm tra `DangHoatDong` và trạng thái nhân viên tại `Services/AuthService.cs:48-83`. Nhưng sau khi đăng nhập, role và khoa/phòng được lưu cố định vào session tại `Services/SessionUserAccessor.cs:54-58`.

`PageController.OnActionExecuting` chỉ kiểm tra session có `TaiKhoanId` hay không:

```csharp
if (!SessionUserAccessor.IsAuthenticated(Session))
```

tại `Controllers/PageController.cs:51`. Không có bước tái kiểm tra `TaiKhoan.DangHoatDong`, `NhanVien.DangHoatDong`, `LoaiTaiKhoan`, hoặc `KhoaPhongId` từ DB trên các request nhạy cảm.

### Luồng khai thác

1. User đăng nhập hợp lệ.
2. Admin khóa tài khoản, khóa nhân viên, đổi role, hoặc chuyển user sang khoa/phòng khác.
3. Session cũ vẫn chứa `TaiKhoanId`, `LoaiTaiKhoan`, `KhoaPhongId` cũ.
4. User tiếp tục gọi các controller kế thừa `PageController` cho đến khi session hết hạn hoặc logout.

### Tác động

Tài khoản đã bị thu hồi vẫn có thể tiếp tục thao tác. Nếu session bị chiếm trước khi khóa tài khoản, việc khóa tài khoản không chặn ngay phiên đang hoạt động.

### Khuyến nghị

- Trên mỗi request nhạy cảm, tái kiểm tra trạng thái tài khoản/nhân viên hoặc dùng security stamp/session version.
- Khi admin khóa tài khoản hoặc đổi quyền/khoa phòng, tăng `SecurityStamp`/`SessionVersion` trong DB để tất cả session cũ bị vô hiệu.
- Không chỉ lưu role/khoa trong session; phải coi session là cache có thể hết hạn khi DB thay đổi.
- Thêm test: đăng nhập user, khóa user, gọi lại dashboard/report/export phải bị chặn.

## 4. User có thể export danh sách nhân viên dù màn hình nhân viên là Admin-only

Mức độ: Trung bình  
Loại: inconsistent access control / thông tin cá nhân nội bộ  
CWE liên quan: CWE-200, CWE-862  
Vị trí:

- `Controllers/EmployeeController.cs:12-15`
- `Controllers/ExportController.cs:17-19`
- `Services/NotificationExportServices.cs:375-386`

### Bằng chứng

Màn hình nhân viên yêu cầu admin tại `EmployeeController.Index`, dòng `Controllers/EmployeeController.cs:12-15`. Tuy nhiên endpoint export:

```csharp
public ActionResult Employees(int? khoaPhongId)
{
    return File(_service.ExportEmployees(khoaPhongId, IsAdmin, CurrentKhoaPhongId), "text/csv", "nhan-vien.csv");
}
```

tại `Controllers/ExportController.cs:17-19` không gọi `RequireAdmin()`. Service có giới hạn user thường về `CurrentKhoaPhongId`, nhưng vẫn xuất `MaNhanVien`, `HoTen`, `KhoaPhong`, `Email`, `SoDienThoai` tại `Services/NotificationExportServices.cs:375-386`.

### Luồng khai thác

User thường gọi trực tiếp `GET /Export/Employees` và tải danh sách nhân viên của khoa/phòng mình, dù không thể vào màn hình quản lý nhân viên.

### Tác động

Nếu yêu cầu nghiệp vụ chỉ cho Admin xem danh sách nhân viên, đây là rò rỉ dữ liệu cá nhân nội bộ và bypass chính sách phân quyền.

### Khuyến nghị

- Nếu export nhân viên chỉ dành cho Admin, thêm `RequireAdmin()` trong `ExportController.Employees`.
- Nếu user được phép export danh bạ khoa mình, cần ghi rõ trong nghiệp vụ và giảm dữ liệu xuất ra theo nguyên tắc tối thiểu.
- Thêm test phân quyền cho từng endpoint export, không chỉ test màn hình UI.

## 5. Không có lockout hoặc rate limit cho đăng nhập

Mức độ: Trung bình  
Loại: brute force / credential stuffing  
CWE liên quan: CWE-307  
Vị trí:

- `Controllers/AccountController.cs:36-61`
- `Services/AuthService.cs:38-89`
- `App_Data/Sql/001_CreateSchema.sql:33-40`

### Bằng chứng

`AdminLogin` và `UserLogin` đều gọi `_authService.Authenticate(model.TenDangNhap, model.MatKhau)` tại `Controllers/AccountController.cs:61`. `AuthService.Authenticate` kiểm tra username/password nhưng không ghi nhận số lần sai. `UpdateLastLogin` tại `Services/AuthService.cs:89-92` chỉ chạy khi đăng nhập thành công. Schema `TaiKhoan` không có cột đếm lần sai, thời điểm khóa tạm, hoặc audit đăng nhập thất bại.

### Luồng khai thác

Attacker có thể lặp POST tới `/Account/AdminLogin` hoặc `/Account/UserLogin`. Anti-forgery token làm tăng chi phí tự động hóa nhưng không phải rate limit; attacker vẫn có thể lấy token mới từ form trước mỗi lần thử.

### Tác động

Tăng nguy cơ dò mật khẩu, đặc biệt khi tồn tại mật khẩu ban đầu có thể dự đoán được từ import nhân viên.

### Khuyến nghị

- Thêm đếm đăng nhập sai theo username và IP.
- Khóa tạm hoặc backoff sau nhiều lần sai.
- Ghi audit đăng nhập thất bại.
- Cân nhắc CAPTCHA sau ngưỡng rủi ro, nhưng không thay thế lockout/backoff server-side.

## 6. Session/cookie chưa được harden rõ ràng và không rotate session khi login

Mức độ: Trung bình  
Loại: session fixation / cookie hardening  
CWE liên quan: CWE-384, CWE-614, CWE-1275  
Vị trí:

- `Controllers/AccountController.cs:80`
- `Services/SessionUserAccessor.cs:47-58`
- `Web.config:16-19`

### Bằng chứng

Sau khi đăng nhập thành công, controller gọi `SessionUserAccessor.SetLoginSession(Session, user)` tại `Controllers/AccountController.cs:80`, ghi thông tin đăng nhập vào session hiện tại. Không thấy bước clear/abandon session cũ hoặc rotate session id trước khi set quyền.

`Web.config` chỉ có `globalization`, `compilation debug="true"` và `httpRuntime targetFramework="4.7.2"` tại `Web.config:16-19`. Không thấy cấu hình rõ `httpCookies requireSSL`, `httpOnlyCookies`, `sameSite`, `sessionState cookieSameSite`, timeout, hoặc cơ chế bắt buộc HTTPS cho session cookie.

### Tác động

Nếu session id bị cố định hoặc bị lộ trước đăng nhập, rủi ro chiếm phiên tăng. Nếu triển khai qua HTTP hoặc thiếu cookie flags, session cookie dễ bị đánh cắp hơn.

### Khuyến nghị

- Rotate session id sau đăng nhập thành công.
- Cấu hình cookie chỉ HTTPS, HttpOnly, SameSite phù hợp.
- Bắt buộc HTTPS ở môi trường thật.
- Thiết lập session timeout rõ ràng và thu hồi session khi đổi mật khẩu/khóa tài khoản.

## 7. CSV formula injection trong các file export CSV

Mức độ: Trung bình  
Loại: CSV/Excel formula injection  
CWE liên quan: CWE-1236  
Vị trí:

- `Services/ExcelImportExportService.cs:54-62`
- `Services/ExcelImportExportService.cs:457-460`
- `Services/NotificationExportServices.cs:364-404`
- `Controllers/ExportController.cs:14-49`

### Bằng chứng

`CreateCsv` ghi từng giá trị qua `Escape` tại `Services/ExcelImportExportService.cs:54-62`. Hàm `Escape` tại `Services/ExcelImportExportService.cs:457-460` chỉ bọc giá trị bằng dấu nháy kép và escape dấu nháy kép:

```csharp
return "\"" + value.Replace("\"", "\"\"") + "\"";
```

Cách này đúng cú pháp CSV nhưng không ngăn Excel diễn giải công thức nếu ô bắt đầu bằng `=`, `+`, `-`, `@`, tab hoặc ký tự điều khiển.

Các endpoint export CSV gồm khoa/phòng, nhân viên, chỉ số và báo cáo tại `ExportController.cs:14-49`.

### Luồng khai thác

Nếu dữ liệu nguồn hoặc file import chứa giá trị như `=WEBSERVICE(...)` trong tên khoa/phòng, tên nhân viên, email, tên chỉ số hoặc trường text được export, khi người dùng mở CSV bằng Excel công thức có thể được Excel thực thi.

### Tác động

Có thể dẫn tới gọi URL ngoài, rò rỉ dữ liệu cục bộ trong bảng tính, hoặc lừa operator thực hiện hành động nguy hiểm trong Excel. Khả năng khai thác phụ thuộc vào ai có quyền ghi dữ liệu được export và việc operator mở file bằng Excel.

### Khuyến nghị

- Khi export CSV, nếu giá trị bắt đầu bằng `=`, `+`, `-`, `@`, tab hoặc CR/LF nguy hiểm, prefix bằng dấu nháy đơn hoặc khoảng trắng an toàn theo chính sách đã chọn.
- Áp dụng cho tất cả export CSV, không chỉ một endpoint.
- Thêm test với payload `=1+1`, `+cmd`, `@SUM(1,1)` để bảo đảm ô không được Excel hiểu là công thức.

## 8. Import `.xlsx`/`.docx` có thể gây DoS do đọc ZIP/XML không giới hạn

Mức độ: Trung bình  
Loại: resource exhaustion / parser DoS  
CWE liên quan: CWE-400, CWE-409  
Vị trí:

- `Services/ExcelImportExportService.cs:17-35`
- `Services/ExcelImportExportService.cs:142-166`
- `Services/ExcelImportExportService.cs:205-234`
- `Services/ExcelImportExportService.cs:391-392`
- `Web.config:19`

### Bằng chứng

Import chỉ kiểm tra file khác null và `ContentLength > 0` tại `ExcelImportExportService.cs:17-24` và `38-45`, sau đó dựa trên extension `.csv`, `.xlsx`, `.docx`. Không thấy giới hạn kích thước riêng, giới hạn số dòng, giới hạn số cell, giới hạn số entry ZIP, hoặc giới hạn kích thước XML sau giải nén.

Với `.xlsx`, code mở `ZipArchive`, load `sheet1.xml` bằng `XDocument.Load`, rồi materialize rows/cells bằng `.ToList()` tại `ExcelImportExportService.cs:142-166`. Shared strings cũng được load toàn bộ tại `ExcelImportExportService.cs:391-392`. Với `.docx`, `word/document.xml` được load toàn bộ tại `ExcelImportExportService.cs:205-234`.

### Luồng khai thác

Admin import một file `.xlsx` hoặc `.docx` độc hại có kích thước request còn nằm trong giới hạn, nhưng nội dung ZIP bung ra thành XML rất lớn hoặc có rất nhiều shared strings/row/cell. Worker ASP.NET phải giải nén, parse XML và giữ danh sách lớn trong bộ nhớ.

### Tác động

Có thể làm treo request, tăng CPU/RAM, hoặc làm gián đoạn worker process. Do endpoint import là Admin-only, đây không phải DoS ẩn danh, nhưng vẫn là rủi ro khi admin xử lý file nguồn không tin cậy.

### Khuyến nghị

- Cấu hình giới hạn upload rõ ràng trong `httpRuntime maxRequestLength` và IIS/request filtering.
- Kiểm tra `ContentLength` trước khi parse.
- Giới hạn số dòng, số cell, số shared strings, số bảng DOCX, kích thước XML sau giải nén, và tổng uncompressed size.
- Dùng parser streaming khi có thể thay vì `XDocument.Load` toàn bộ.
- Bắt lỗi parser và trả thông báo an toàn thay vì để request/worker cạn tài nguyên.

## 9. Thao tác xóa/cập nhật nhiều bước thiếu transaction

Mức độ: Trung bình đến thấp  
Loại: mất toàn vẹn dữ liệu khi lỗi giữa chừng  
CWE liên quan: CWE-667 theo nghĩa thiếu kiểm soát nhất quán giao dịch  
Vị trí:

- `Services/ReportDashboardServices.cs:246-250`
- `Services/IndicatorServices.cs:139-149` (IndicatorService.Save)
- `Services/IndicatorServices.cs:392-395` (IndicatorService xử lý tần suất)
- `Services/DbServiceBase.cs:62-69`

### Bằng chứng

`ReportService.Delete` xóa `BaoCaoChiTiet`, sau đó xóa `BaoCao` bằng hai lệnh riêng tại `Services/ReportDashboardServices.cs:246-250`. `DbServiceBase.Execute` mở connection và thực thi từng lệnh riêng tại `Services/DbServiceBase.cs:62-69`; không có transaction bao quanh.

Tương tự, `IndicatorService.Delete` và cập nhật tần suất chỉ số có nhiều bước xóa/insert riêng. Nếu một bước sau lỗi, các bước trước đã commit.

### Tác động

Admin-only, nên đây không phải leo quyền trực tiếp. Tuy nhiên có thể làm mất dữ liệu chi tiết hoặc làm dữ liệu chỉ số/tần suất không nhất quán nếu có lỗi FK, race condition hoặc lỗi giữa chừng.

### Khuyến nghị

- Dùng `SqlTransaction` cho các thao tác nhiều bước.
- Với xóa báo cáo, kiểm tra FK liên quan trước hoặc dùng transaction để rollback khi bước sau lỗi.
- Thêm test mô phỏng lỗi giữa chừng để bảo đảm dữ liệu không bị mất một phần.

## 10. POST admin JSON thiếu anti-forgery token

Mức độ: Thấp  
Loại: thiếu nhất quán CSRF protection  
CWE liên quan: CWE-352  
Vị trí:

- `Controllers/AssignmentController.cs:145-151`
- `Views/Assignment/Index.cshtml:592-599`

### Bằng chứng

`AssignmentController.Preview` là `[HttpPost]` nhưng không có `[ValidateAntiForgeryToken]` tại `Controllers/AssignmentController.cs:145-151`. JavaScript gọi endpoint này bằng `fetch` và chỉ gửi `Content-Type: application/x-www-form-urlencoded` tại `Views/Assignment/Index.cshtml:592-599`, không gửi token.

### Tác động

Endpoint này chỉ preview dữ liệu, không thay đổi trạng thái. Same Origin Policy thường chặn attacker đọc response. Vì vậy mức độ thấp. Tuy nhiên đây vẫn là bề mặt admin thiếu nhất quán với các POST còn lại.

### Khuyến nghị

- Thêm `[ValidateAntiForgeryToken]` cho `Preview`.
- Gửi token trong body/header của `fetch`.
- Duy trì quy tắc: mọi POST authenticated đều có anti-forgery, kể cả POST chỉ đọc.

## 11. Cấu hình debug và cookie/security header chưa rõ cho production

Mức độ: Thấp  
Loại: security configuration hardening  
CWE liên quan: CWE-489, CWE-16  
Vị trí:

- `Web.config:18-19`

### Bằng chứng

`Web.config` đang có:

```xml
<compilation debug="true" targetFramework="4.7.2" />
<httpRuntime targetFramework="4.7.2" />
```

Không thấy cấu hình production rõ ràng cho custom errors, cookie flags, HSTS/HTTPS enforcement hoặc giới hạn request. Có thể các transform `Web.Release.config` xử lý một phần, nhưng file cấu hình gốc vẫn là rủi ro nếu được deploy trực tiếp.

### Tác động

Debug mode có thể làm lộ thông tin lỗi, giảm tối ưu và làm tăng bề mặt debug trong môi trường thật. Thiếu cấu hình cookie/request rõ ràng làm tăng rủi ro khi triển khai sai.

### Khuyến nghị

- Đảm bảo `Web.Release.config` luôn set `debug="false"`.
- Cấu hình custom errors phù hợp.
- Cấu hình cookie `requireSSL`, `httpOnly`, `sameSite`.
- Cấu hình giới hạn upload/request và bắt buộc HTTPS/HSTS ở môi trường production.

## Các bề mặt đã kiểm tra và không thấy lỗi khai thác trực tiếp

| Bề mặt | Kết quả |
|---|---|
| SQL injection trong ADO.NET | Không thấy SQL injection trực tiếp khai thác được. Phần lớn query dùng `SqlParameter`. Một số `IN (...)` được ghép bằng `string.Join` trên `int[]`; model binding hạn chế SQLi nhưng vẫn nên parameter hóa từng phần tử và giới hạn số lượng ID. |
| Path traversal khi upload | Không thấy lưu `file.FileName` xuống filesystem. File upload được đọc từ `InputStream`; tên file chủ yếu lưu DB qua parameter. |
| Export XLSX phân công | Dữ liệu được ghi dạng `inlineStr` và danh sách cột được whitelist; chưa thấy công thức Excel được ghi dưới dạng formula trong XLSX. |
| Notification detail | Service dùng `GetDetailForUser(id, CurrentTaiKhoanId, IsAdmin)`; chưa thấy user đọc notification của tài khoản khác qua controller hiện tại. |
| Controller nghiệp vụ | Phần lớn controller kế thừa `PageController` hoặc dùng `RequireAdmin`. Rủi ro còn lại là dự án chưa có global auth filter, nên controller mới có thể quên kế thừa base controller. |

## Ưu tiên khắc phục

1. Sửa luồng `Report/Edit` và `ReportService.SaveDraft`: không tin hidden ID, reload object từ DB, kiểm tra department/assignment/period/status trên server.
2. Đổi quy trình tạo tài khoản import: mật khẩu ngẫu nhiên hoặc token kích hoạt, bắt đổi mật khẩu lần đầu.
3. Thêm cơ chế thu hồi session khi tài khoản bị khóa/đổi quyền/đổi khoa.
4. Thêm lockout/rate limit và audit đăng nhập thất bại.
5. Chốt chính sách export nhân viên: Admin-only hoặc user được phép nhưng phải giảm dữ liệu.
6. Kiểm tra lại rủi ro formula injection trên export Excel `.xlsx`; mục CSV cũ không còn đúng nguyên trạng vì hệ thống đã bỏ CSV.
7. Giới hạn upload và parser cho `.xlsx`/`.docx`.
8. Kiểm tra lại toàn bộ thao tác DB nhiều bước; một số luồng chính đã có transaction nhưng chưa nên giả định đã bao phủ 100%.
9. Hardening cookie/session/production config, đặc biệt `requireSSL`, custom errors và HSTS cho môi trường production.
10. Thêm anti-forgery cho mọi POST, bao gồm endpoint preview.
