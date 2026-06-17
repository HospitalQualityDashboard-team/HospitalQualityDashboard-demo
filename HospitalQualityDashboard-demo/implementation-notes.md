# Ghi Chú Triển Khai

Tài liệu này ghi lại các thay đổi kỹ thuật, quyết định thiết kế và lưu ý vận hành của dự án `HospitalQualityDashboard-demo`.

## 2026-06-16

### Sua xuat Excel thieu muc tieu chi so

- Khi xuat Excel dashboard, cot muc tieu uu tien muc tieu dung nam ky bao cao.
- Neu chi so chua co muc tieu dung nam, he thong fallback sang muc tieu gan nhat truoc nam bao cao; neu khong co thi lay muc tieu moi nhat cua chi so.
- Dong bo logic tinh dat/chua dat khi luu bao cao voi logic hien thi muc tieu trong file Excel de tranh cot muc tieu trong nhung van bi danh gia sai.

## 2026-06-15

### Cập nhật tài liệu hướng dẫn sử dụng

- Viết lại `README.md` ở root repo thành hướng dẫn sử dụng chi tiết cho developer và người vận hành.
- Bổ sung hướng dẫn tạo `ConnectionStrings.config` từ `ConnectionStrings.example.config`.
- Ghi rõ `ConnectionStrings.config` là file local secret, không được commit.
- Bổ sung quy trình chạy bằng Visual Studio/IIS Express, build bằng MSBuild và kiểm tra Razor bằng `aspnet_compiler`.
- Cập nhật luồng sử dụng Admin/User, import/export, phân trang, cache dropdown, dashboard tối ưu và index Azure SQL.
- Bổ sung mục troubleshooting cho các lỗi thường gặp: thiếu connection string, thiếu bảng, timeout Azure SQL, contributor Claude trên GitHub.

### Bảo vệ connection string thật

- Thêm `HospitalQualityDashboard-demo/ConnectionStrings.config` vào `.gitignore`.
- Bỏ tracking `ConnectionStrings.config` khỏi Git nhưng giữ file local trên máy dev.
- Thêm `HospitalQualityDashboard-demo/ConnectionStrings.example.config` không chứa password thật.
- Cập nhật `.csproj` để track file example thay vì file config thật.
- Lưu ý vận hành: nếu password đã từng xuất hiện trong Git history hoặc trên GitHub, cần rotate password Azure SQL và rewrite history nếu muốn xóa khỏi lịch sử remote.

### Tối ưu tốc độ web khi chạy với Azure SQL

- Session revalidation được throttle 5 phút thay vì gọi `AuthService.GetAuthenticatedUser()` ở mọi request.
- Không tự chạy `OpenDuePeriods` khi mở Dashboard/Report; chỉ giữ action thủ công.
- Thêm `CommandTimeout` mặc định 30 giây trong `DbServiceBase`; dashboard dùng 60 giây.
- Thêm cache dropdown 5 phút cho khoa/phòng, chỉ số, kỳ báo cáo.
- Dashboard dùng query tổng hợp/CTE để giảm DB round-trip.
- Nhân viên, báo cáo, thông báo dùng phân trang server-side mặc định 20 dòng/trang.
- Thêm `App_Data/Sql/002_PerformanceIndexes.sql` để tạo các index đọc chính bằng `IF NOT EXISTS`.

## 2026-06-10

### Cleanup file dư thừa

Đã có kế hoạch dọn dẹp. Hiện trạng thực tế:

**Đã xóa:**
- `bin/`, `obj/` — build artifact
- `packages/` NuGet trùng (các version `.0`, MVC5, Razor3, WebPages3)
- `.csproj.user`

**Còn tồn đọng:**
- `.claude/worktrees/`, `.superpowers/brainstorm/` — workspace tạm chưa xóa
- `docs/superpowers/` — plan/spec cũ chưa xóa (cả root và project)
- `Filters/` — thư mục rỗng
- `Content/` CSS không dùng, `Scripts/` JS không dùng — vẫn còn trong `.csproj`

**Cần làm tiếp:** Chạy cleanup script triệt để để xóa các mục tồn đọng.

## 2026-06-09

### 1. Cho phép User chỉnh sửa thông tin cá nhân trên Profile

Trang Profile (`/Account/Profile`) trước đây chỉ hiển thị thông tin readonly và form đổi mật khẩu. Đã bổ sung form chỉnh sửa thông tin cá nhân.

**Các file thay đổi:**

- `Models/DTOs/EmployeeDtos.cs` — Thêm `ProfileUpdateDto` với các trường: HoTen, NgaySinh, GioiTinh, ChucVu, Email, SoDienThoai.
- `Services/AuthService.cs` — Thêm `UpdateProfile(int taiKhoanId, ProfileUpdateDto dto)`: lấy NhanVienId từ TaiKhoan, UPDATE dbo.NhanVien với các trường trên. Thêm `GetNhanVienId(int taiKhoanId)` helper.
- `Controllers/AccountController.cs` — Thêm action `[HttpPost] UpdateProfile(ProfileUpdateDto model)`: kiểm tra auth, validate ModelState, gọi `_authService.UpdateProfile`, set `TempData["SuccessMessage"]` khi thành công, return về Profile. Nếu ModelState lỗi: reload profile và giữ nguyên dữ liệu vừa nhập.
- `Views/Account/Profile.cshtml` — Thêm card **Cập nhật thông tin cá nhân** bên dưới card **Thông tin người dùng** hiện tại ở cột trái. Form gồm: Họ tên (required), Ngày sinh (date picker), Giới tính, Chức vụ, Email, Số điện thoại. Nút submit **Cập nhật thông tin**.

**Kiến trúc:**
- AuthService tự quản lý connection (không kế thừa DbServiceBase), dùng `ExecuteNonQuery` riêng.
- Khi tài khoản chưa liên kết nhân viên (NhanVienId null), throw `InvalidOperationException` — Controller bắt và hiển thị lỗi trên form.

### 2. Fix lỗi WebGrease NullReferenceException khi minify Bootstrap 5

Lỗi: NullReferenceException tại `_Layout.cshtml:line 99` khi `@Scripts.Render("~/bundles/bootstrap")` gọi WebGrease JsMinify parse JS của Bootstrap 5.2 (syntax ES6+ không tương thích WebGrease 1.6.0 ~2014).

**Sửa:** Đổi `ScriptBundle` / `StyleBundle` thành `Bundle` cho các file `.min.*` trong `App_Start/BundleConfig.cs`:
- `~/bundles/jquery` (jquery-3.7.0.min.js)
- `~/bundles/jqueryval` (jquery.validate.min.js + jquery.validate.unobtrusive.min.js)
- `~/bundles/bootstrap` (bootstrap.bundle.min.js)
- `~/Content/css` (bootstrap.min.css + site.css)

Giữ `ScriptBundle` cho `~/bundles/modernizr` (modernizr-2.8.3.js — dev file, JS cũ, không minified).

## 2026-06-08

### 1. Root controller cleanup — chuyển sang Area redirect wrappers

Hoàn tất chuyển các root controller từ chứa full business logic sang thin redirect wrapper theo kiến trúc Areas.

**Phạm vi 4 Phase:**

- **Phase 1 (Admin-only):** `AssignmentController`, `DepartmentController`, `EmployeeController`, `ReportingPeriodController` → redirect to `area = "Admin"`.
- **Phase 2 (Dual-role read):** `DashboardController`, `IndicatorController` → switch bằng `IsAdmin` rồi redirect vào Admin hoặc User Area.
- **Phase 3 (Dual-role write):** `ReportController`, `NotificationController`, `ExportController` → role-switch redirect, giữ 410 cho Approve/Reject.
- **Phase 4 (Cleanup):** Xoá 8 thư mục root views; xoá 49 Content items khỏi `.csproj`; cập nhật `PROJECT_CONTEXT.md`.

**Kết quả:**
- Tất cả business logic đã chuyển xuống `Areas/Admin/Controllers/` và `Areas/User/Controllers/`.
- Root controllers chỉ còn session guard + redirect.
- Các file bug fix kèm theo: sửa `@using` trong Area layouts, sửa `JsonResult→ActionResult` ở Preview, set `Web.config debug=false`, cập nhật `VerifySecurityHardening.ps1` guardrail cho kiến trúc mới.
- Build 0 error, 0 warning, `MvcBuildViews=true` pass.
- Tổng cộng 4 commits: Phase 1 → Phase 2 → Phase 3 → Phase 4.

### 2. Dọn dẹp file dư thừa sau Areas migration

- Xoá `Views/Home/About.cshtml`, `Views/Home/Contact.cshtml`, `Views/Account/ChangePassword.cshtml`, `Filters/RequireLoginAttribute.cs`.
- Đơn giản hóa `HomeController` (bỏ `[RequireLogin]`, xóa About/Contact).
- Xoá thư mục output build: `bin_unit/`, `obj/`, `obj_unit/`, `.csproj.user` (khỏi working tree).

### 3. Chuẩn hóa giao diện và thông báo tiếng Việt có dấu

- Việt hóa layout Admin/User, thông báo controller/service, AGENTS.md.
- Cập nhật `README.md` phản ánh cấu trúc Areas.
- Cập nhật `PROJECT_CONTEXT.md` mô tả HomeController đúng hiện trạng.

## 2026-06-06

### 1. Chuẩn hóa chú thích code tiếng Việt

- Bổ sung comment đầu file bằng tiếng Việt có dấu theo mẫu `Mục đích:` cho các file code tự viết.
- Phạm vi gồm `Controllers`, `Services`, `Models`, `Filters`, `App_Start`, `Global.asax.cs`, `Properties/AssemblyInfo.cs` và Razor view trong `Views`.
- Không chỉnh thư viện bên thứ ba như Bootstrap, jQuery, Modernizr hoặc file minified.
- Chuyển các comment nội bộ còn không dấu hoặc tiếng Anh trong phần code tự viết sang tiếng Việt có dấu.

### 2. Bổ sung hướng dẫn Inspect và Storage

- Cập nhật tài liệu để hướng dẫn kiểm tra `Application` > `Storage` > `Cookies` trong DevTools.
- Ghi rõ cookie cần kiểm tra là `ASP.NET_SessionId`.
- Làm rõ các giá trị đăng nhập như `TaiKhoanId`, `LoaiTaiKhoan`, `KhoaPhongId` được lưu ở server-side `Session`, không hiển thị trực tiếp trong `localStorage` hoặc `sessionStorage`.
- Ghi nhận hiện trạng `Web.config` chưa khai báo `sessionState timeout`, nên timeout server-side dùng mặc định ASP.NET khoảng 20 phút không hoạt động.

### 3. Kiểm tra đã chạy

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard-demo\HospitalQualityDashboard-demo.csproj /p:Configuration=Debug /p:Platform=AnyCPU /v:minimal
```

Kết quả:

- MSBuild Debug: passed.
- Không còn comment `Purpose:` trong phạm vi code tự viết.
- Các comment đầu file đã chuyển sang `Mục đích:`.

## 2026-05-21

### 1. Chuẩn hóa tài liệu dự án

- Viết lại `PROJECT_CONTEXT.md` bằng UTF-8 sạch, tiếng Việt có dấu, thay cho bản cũ bị lỗi encoding.
- Tạo mới `TAI_LIEU_NGHIEP_VU.md` để mô tả business và nghiệp vụ chi tiết của hệ thống.
- Cập nhật `implementation-notes.md` thành nhật ký triển khai rõ ràng hơn.
- Ghi nhận hiện trạng kiến trúc: ASP.NET MVC 4, .NET Framework 4.7.2, SQL Server LocalDB, ADO.NET thuần, Razor view.

### 2. Phân quyền Admin/User

- Hệ thống có 2 vai trò chính:
  - `Admin = 1`: quản trị toàn hệ thống.
  - `User = 2`: tài khoản khoa/phòng.
- User bắt buộc gắn với `KhoaPhongId`.
- `PageController` tiếp tục là lớp kiểm soát session và phân quyền phía server.
- `_Layout.cshtml` đã được cập nhật để menu phân biệt theo `Session["LoaiTaiKhoan"]`.
- User không còn thấy các menu Admin-only như:
  - `Khoa/phòng`
  - `Nhân viên`
  - `Chỉ số`
  - `Phân công`
  - `Kỳ báo cáo`
- User chỉ thấy các mục phù hợp:
  - `Tổng quan`
  - `Báo cáo của tôi`
  - `Thông báo`
  - `Đổi mật khẩu`
  - `Đăng xuất`
- Các nút Admin-only trong view cũng đã được ẩn với User, gồm `Xuất CSV`, `Khóa`, `Xóa`, `Gửi thông báo`.

### 3. Quan hệ Chỉ số - Khoa/phòng

- Xác định lại quan hệ nghiệp vụ giữa chỉ số và khoa/phòng là nhiều-nhiều.
- Một chỉ số có thể thuộc nhiều khoa/phòng.
- Một khoa/phòng có thể phụ trách nhiều chỉ số.
- Bảng trung gian là `PhanCongChiSo`.
- Database có unique constraint `(ChiSoChatLuongId, KhoaPhongId)` để tránh trùng phân công.
- Bổ sung migration `004_AddAssignmentUniqueConstraint.sql` để xử lý dữ liệu trùng và thêm constraint.

### 4. Phân công nhiều khoa/phòng từ dữ liệu chỉ số

- Bổ sung xử lý nhận diện nhiều khoa/phòng trong trường `ThuThapTongHop`.
- Ví dụ dữ liệu:

```text
P. KHTH thu thập số liệu
P. TCCB tổng hợp số liệu
```

- Hệ thống cần tạo 2 phân công cho cùng chỉ số:
  - Chỉ số - P. KHTH.
  - Chỉ số - P. TCCB.
- `AssignmentService.SyncFromIndicatorSources` được dùng để đồng bộ phân công từ nguồn chỉ số.
- Màn hình `Assignment` hỗ trợ chọn nhiều khoa/phòng và nhiều chỉ số.

### 5. Tần suất báo cáo nhiều giá trị

- Thiết kế ban đầu chỉ lưu một tần suất trong `ChiSoChatLuong.TanSuatBaoCao`.
- Nghiệp vụ thực tế có chỉ số chứa nhiều tần suất, ví dụ:

```text
Mỗi quý, 6 tháng, 12 tháng.
```

- Bổ sung bảng `ChiSoTanSuatBaoCao` để lưu nhiều tần suất cho một chỉ số.
- Bổ sung migration `003_AddIndicatorFrequencies.sql`.
- `IndicatorService.EnsureIndicatorFrequencyTable` đảm bảo bảng phụ tồn tại khi chạy nghiệp vụ liên quan.

### 6. Nhận diện tần suất quý

- Parser tần suất đã được cập nhật để nhận diện các biến thể:
  - `Quý`
  - `Hàng quý`
  - `Mỗi quý`
  - `Theo quý`
  - `3 tháng`
  - `Ba tháng`
- Tên hiển thị của `HangQuy` được chuẩn hóa theo nghiệp vụ là `Hàng quý`.
- Script kiểm tra: `tools/VerifyIndicatorFrequencyParser.ps1`.

### 7. Báo cáo và Dashboard

- `ReportEntryViewModel` bổ sung `PhanCongChiSoId` để báo cáo giữ được liên kết với phân công cụ thể.
- `ReportDashboardServices.GetAssignedForUser` lọc chỉ số theo:
  - khoa/phòng của User;
  - trạng thái phân công đang hoạt động;
  - tần suất phù hợp với kỳ báo cáo.
- `SaveDraft` lưu `PhanCongChiSoId` khi tạo báo cáo mới.
- Dashboard được điều chỉnh theo quyền:
  - Admin thấy tổng quan toàn viện.
  - User thấy số chỉ số được phân công và tiến độ của khoa/phòng mình.

### 8. Giao diện

- `Content/Site.css` được tối ưu theo hướng hiện đại, rõ ràng và dễ đọc hơn.
- Các view chính đã được chuyển sang tiếng Việt có dấu.
- Các form, bảng, nút và dashboard card đã được thống nhất phong cách.
- Một lỗi encoding do ghi file bằng PowerShell đã được phát hiện và xử lý bằng cách khôi phục nội dung UTF-8 đúng.

### 9. Import Excel/Word

- `ExcelImportExportService` xử lý các tình huống Excel thường gặp:
  - ô trống bị lược bỏ trong XML;
  - chuỗi dạng shared string;
  - chuỗi dạng inline string.
- Parser import chỉ số tiếp tục chịu trách nhiệm map dữ liệu nghiệp vụ từ Word/Excel sang `ChiSoViewModel`.
- Script kiểm tra:
  - `tools/VerifyExcelParser.ps1`
  - `tools/VerifyIndicatorDepartmentAssignmentParser.ps1`
  - `tools/VerifyIndicatorFrequencyParser.ps1`

### 10. Kiểm tra đã chạy

- `tools/VerifyExcelParser.ps1`: kiểm tra parser Excel.
- `tools/VerifyIndicatorFrequencyParser.ps1`: kiểm tra parser tần suất.
- `tools/VerifyIndicatorDepartmentAssignmentParser.ps1`: kiểm tra nhận diện nhiều khoa/phòng.
- Build Razor view bằng MSBuild với `MvcBuildViews=true`.

Lệnh build đã dùng:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard-demo\HospitalQualityDashboard-demo.csproj /p:Configuration=Debug /p:Platform=AnyCPU /p:MvcBuildViews=true
```

### 11. Quyết định thiết kế quan trọng

- **Giữ ADO.NET thuần** để phù hợp với kiến trúc và hiệu năng tối đa của codebase hiện tại.
- **Không chỉ ẩn menu để phân quyền**: Controller và Service vẫn thực hiện kiểm tra quyền chặt chẽ trên Server-side đối với mọi yêu cầu dữ liệu.
- **Chỉ số - Khoa/phòng là quan hệ nhiều-nhiều**: Bản ghi trung gian lưu trữ độc lập trạng thái hoạt động giúp kiểm soát linh hoạt.
- **Một chỉ số có thể có nhiều tần suất**: Tận dụng bảng phụ `ChiSoTanSuatBaoCao` để liên kết thay vì lưu danh sách chuỗi thô.
- **Parser import linh hoạt**: Sử dụng các phương pháp chuẩn hóa chuỗi tiếng Việt để tự động liên kết các khoa/phòng và tần suất bất kể cách viết tắt hay không đồng nhất trong dữ liệu nguồn.
- **Cơ chế sinh "kỳ báo cáo chi tiết" (Reporting Slots) động**:
  - *Trade-off (Đánh đổi)*: Thay vì ghi đè trước hàng ngàn bản ghi trống vào bảng `dbo.BaoCao` mỗi khi Admin tạo kỳ báo cáo (tốn bộ nhớ DB và gây mất đồng bộ nếu thay đổi phân công sau này), hệ thống dùng câu lệnh `LEFT JOIN` động giữa phân công chỉ số và báo cáo thực tế. Điều này mang lại sự linh hoạt tuyệt đối và tiết kiệm tài nguyên lưu trữ tối đa.
- **Duy trì bộ lọc và trang (State Preservation)**:
  - *Giải pháp*: Các thao tác sửa đổi trạng thái phân công sẽ chuyển đổi các tham số tìm kiếm và số trang hiện tại qua Route Values của RedirectAction để đảm bảo Admin không bị đẩy về trang đầu hoặc mất từ khóa tìm kiếm khi thao tác.

## 2026-05-23

Hệ thống đã trải qua một đợt nâng cấp nghiệp vụ quan trọng nhằm cải thiện trải nghiệm người dùng, sửa các lỗi vận hành sai và thắt chặt kiểm soát dữ liệu biểu mẫu.

### 1. Phân trang chỉ số & Sửa lỗi logic Tạm dừng phân công

- **Phân trang & Thống kê động**:
  - Tích hợp tính năng phân trang hiển thị 20 chỉ số/trang ở danh sách phân công.
  - Bổ sung 5 thẻ thống kê động (Tổng số chỉ số, Đã phân công, Chưa phân công, Tổng phân công, Đã tạm dừng) để giúp Admin có cái nhìn trực quan nhất. Card "Chưa phân công" giúp phát hiện ngay các chỉ số chưa được gán trách nhiệm.
- **Khắc phục lỗi Tạm dừng phân công**:
  - *Vấn đề*: Trước đây, khi click "Tạm dừng", hệ thống hiểu sai và ẩn mất khoa phòng khỏi chỉ số, khiến Admin không thể kích hoạt lại.
  - *Khắc phục*: Sửa đổi logic `bool isUnassigned = indAssignments.Count == 0;` trong phương thức `GetAllIndicatorGroups` thuộc `Services/IndicatorServices.cs`. Chỉ số chỉ được coi là "Chưa phân công" khi nó thực sự chưa được gán cho bất kỳ khoa phòng nào. Phân công bị tạm dừng (`DangHoatDong = 0`) vẫn hiển thị bình thường trong bảng với nhãn trạng thái màu cam và cung cấp tuỳ chọn **"Kích hoạt lại"**.
- **Kích hoạt & Xử lý bộ lọc**:
  - Thêm hành động `Activate` vào `AssignmentController` để bật lại phân công bị tạm dừng.
  - Khắc phục lỗi lọc Khoa/Phòng không hoạt động bằng cách sửa đổi câu truy vấn SQL trong các phương thức `GetDepartmentsCount`, `GetIndicatorsCount`, và `GetAllIndicatorGroups` để nhận diện chính xác `FilterKhoaPhongId` và `FilterChiSoId`.

### 2. AJAX Preview & Bulk Actions cho Phân công hàng loạt

- **Bảng xem trước AJAX**:
  - Khi Admin chọn nhiều chỉ số và nhiều khoa/phòng trên biểu mẫu phân công, hệ thống sẽ thực hiện gọi AJAX đến phương thức `Preview` của `AssignmentController` để kiểm tra.
  - Bảng xem trước trực quan sẽ hiển thị trạng thái `✅ Mới` đối với các phân công chưa tồn tại và `⚠️ Đã tồn tại` đối với phân công đã có để Admin nắm rõ trước khi nhấn nút xác nhận tạo.
- **Thanh công cụ hàng loạt (Bulk Action Floating Bar)**:
  - Thiết kế thanh công cụ nổi mượt mà ở chân trang danh sách. Khi Admin tích chọn các checkbox phân công, thanh công cụ sẽ tự động xuất hiện.
  - Cung cấp các nút thao tác hàng loạt gồm: *Tạm dừng hàng loạt*, *Kích hoạt hàng loạt*, và *Xóa hàng loạt* (các Action tương ứng trong `AssignmentController`: `BulkDeactivate`, `BulkActivate`, `BulkDelete`).

### 3. Ràng buộc & Xác thực biểu mẫu Tạo/Sửa chỉ số chất lượng

- **Tái cấu trúc biểu mẫu trực quan**:
  - Biểu mẫu `Indicator/Edit.cshtml` được quy hoạch thành 4 phân khu chính: *1. Thông tin cơ bản*, *2. Cấu hình báo cáo*, *3. Mô tả định nghĩa & Phương pháp tính*, và *4. Giá trị mục tiêu*.
- **Xác định các trường bắt buộc và tùy chọn**:
  - Gắn nhãn dấu sao đỏ `*` cạnh các trường bắt buộc (Mã chỉ số, Tên chỉ số, Tần suất báo cáo, Loại công thức). Gắn nhãn phụ màu xám `(Không bắt buộc)` cạnh các trường tùy chọn để điều hướng rõ ràng cho Admin.
- **Xác thực đa lớp**:
  - *Client-side Validation*: Tích hợp bundle `@Scripts.Render("~/bundles/jqueryval")` chứa thư viện `jquery.validate.unobtrusive` để tự động kiểm tra và báo lỗi tức thì (chữ đỏ dưới ô nhập) nếu người dùng bỏ trống trường bắt buộc mà không cần reload trang.
  - *Server-side Validation*: Sửa phương thức `Save` của `IndicatorController` để kiểm tra `ModelState.IsValid`. Nếu không hợp lệ, biểu mẫu sẽ được trả lại kèm danh sách chi tiết lỗi hiển thị trong hộp thoại thông báo màu đỏ Bootstrap nổi bật ở đầu trang biểu mẫu. Đồng thời bắt buộc trường tần suất chọn qua `SelectedTanSuatBaoCaoValues` phải có ít nhất một giá trị được chọn.

### 4. Lọc thông minh Kỳ báo cáo cho vai trò User khoa/phòng

- **Tự động ẩn các kỳ báo cáo không liên quan**:
  - *Vấn đề*: Khi hệ thống có nhiều kỳ báo cáo thuộc các tần suất khác nhau cùng mở, User cấp khoa/phòng thường bị bối rối và có thể nhập nhầm dữ liệu vào kỳ không thuộc trách nhiệm của mình.
  - *Giải pháp*: Tại `ReportController.Index`, khi tài khoản User thường đăng nhập, hệ thống tự động gọi hàm `_periods.GetFrequenciesForDepartment(CurrentKhoaPhongId)` để lấy danh sách tất cả các tần suất chỉ số hoạt động mà khoa phòng đó được phân công (tổng hợp từ tần suất mặc định và bảng `ChiSoTanSuatBaoCao`).
  - Giao diện **"Kỳ báo cáo đang mở"** sẽ tự động lọc và **chỉ hiển thị những kỳ có tần suất tương thích**. Các kỳ khác tự động ẩn đi hoàn toàn để tinh gọn giao diện, tránh nhầm lẫn nhập liệu. Admin vẫn có quyền xem toàn bộ các kỳ để phục vụ mục tiêu giám sát.

### 5. Việc nên làm tiếp

- Triển khai viết các bài viết/tài liệu hướng dẫn sử dụng nhanh dành cho Admin và User khoa/phòng dựa trên các cải tiến này.
- Bổ sung unit test cho các phương thức tính toán tần suất của khoa phòng tại `GetFrequenciesForDepartment`.
- Xem xét bổ sung thêm cơ chế nhắc hạn báo cáo tự động bằng cách kết hợp thông báo nội bộ và ngày hạn nộp (`HanNop`) của kỳ.

## 2026-05-26

Đợt triển khai này điều chỉnh quy trình báo cáo theo phạm vi nghiệp vụ hiện tại và nâng cấp thông báo nội bộ để User nhìn thấy việc cần xử lý ngay khi đăng nhập hoặc khi click vào thông báo.

### 1. Chốt lại luồng báo cáo hiện tại

- Dự án hiện chỉ quản lý chỉ số và tiến độ báo cáo, chưa triển khai luồng xác thực đúng/sai số liệu bởi khoa/phòng.
- Admin không duyệt và không trả lại báo cáo trong luồng chính.
- Các route `Approve` và `Reject` bị chặn để tránh thao tác ngoài phạm vi nghiệp vụ hiện tại.
- Admin không được sửa hoặc gửi báo cáo thay User.
- Admin chỉ nhìn thấy báo cáo đã gửi, gửi trễ hoặc đã khóa; báo cáo `Nhap` của User không hiển thị trên danh sách Admin.
- View báo cáo hiển thị nút theo đúng vai trò:
  - User thấy `Sửa` khi báo cáo còn `Nhap`.
  - User/Admin chỉ `Xem` khi báo cáo đã gửi, gửi trễ hoặc đã khóa.
  - Admin không thấy nút `Duyệt`, `Trả lại`, `Gửi`.

### 2. Định nghĩa lại trạng thái `QuaHan`

Quy tắc đã chốt:

```text
Đã báo cáo = DaGui + QuaHan + DaKhoa
```

- `DaGui`: User gửi đúng hạn hoặc trước hạn.
- `QuaHan`: User đã gửi báo cáo nhưng gửi sau `KyBaoCao.HanNop`.
- `DaKhoa`: Admin đã khóa/chốt báo cáo.
- Slot chưa gửi sau hạn không tự tạo bản ghi `BaoCao` trạng thái `QuaHan`; nó chỉ xuất hiện trong truy vấn thiếu báo cáo của dashboard và thông báo.

### 3. Dashboard User hiển thị cảnh báo sau đăng nhập

- `DashboardService.GetMissingReportsForDepartment` truy vấn các chỉ số còn thiếu dựa trên kỳ đang mở, phân công đang hoạt động, tần suất phù hợp và trạng thái báo cáo đã nộp.
- Dashboard User hiển thị cảnh báo nếu có chỉ số chưa báo cáo.
- Cảnh báo phân biệt:
  - gần đến hạn;
  - quá hạn chưa nộp;
  - chưa báo cáo nhưng chưa gần hạn.
- User có thể mở chi tiết và bấm link nhập báo cáo nhanh theo từng chỉ số.

### 4. Thông báo tự động

- Bổ sung các loại thông báo:
  - `KyBaoCaoMo`
  - `NhacHan`
  - `QuaHan`
  - `TongHopAdmin`
- `NotificationAutomationService.Run(DateTime now)` có thể chạy nhiều lần.
- `ThongBaoTuDongLog.DedupKey` chống tạo thông báo trùng.
- Admin có nút “Chạy kiểm tra thông báo tự động”.
- Khi Admin vào Dashboard hoặc trang Thông báo, service có thể được gọi nhẹ nhờ cơ chế chống trùng.

### 5. Click thông báo để xem chi tiết kỳ báo cáo

- `NotificationViewModel` bổ sung `KyBaoCaoId` và `BaoCaoId`.
- `NotificationDetailViewModel` chứa thông báo và danh sách `MissingReportAlertViewModel`.
- `NotificationService.GetForUser` đọc thêm `KyBaoCaoId`, `BaoCaoId`.
- `NotificationService.GetDetailForUser` kiểm tra quyền xem chi tiết:
  - Admin có thể xem thông báo.
  - User chỉ xem được thông báo có dòng nhận trong `ThongBaoNguoiNhan`.
- `NotificationController.Details(int id)`:
  - lấy thông báo theo quyền;
  - đánh dấu đã đọc khi User mở chi tiết;
  - nếu thông báo gắn kỳ báo cáo, truy vấn các chỉ số còn thiếu của khoa/phòng trong kỳ đó;
  - nếu loại thông báo là `QuaHan`, chỉ lấy các chỉ số quá hạn chưa nộp.
- `Views/Notification/Index.cshtml` đổi hành động sang `Xem chi tiết`; tiêu đề và nội dung thông báo cũng là link.
- `Views/Notification/Details.cshtml` hiển thị nội dung thông báo, trạng thái, ngày tạo, danh sách chỉ số còn thiếu và nút `Nhập báo cáo`.

### 6. File chính đã chỉnh

- `Controllers/ReportController.cs`
- `Controllers/NotificationController.cs`
- `Services/ReportDashboardServices.cs`
- `Services/NotificationExportServices.cs`
- `Models/ViewModels/AppViewModels.cs`
- `Models/Enums/SystemEnums.cs`
- `Views/Report/Index.cshtml`
- `Views/Report/Edit.cshtml`
- `Views/Dashboard/Index.cshtml`
- `Views/Notification/Index.cshtml`
- `Views/Notification/Details.cshtml`
- `App_Data/Sql/006_AddNotificationAutomationLog.sql`
- `tools/VerifyReportWorkflowAndNotifications.ps1`

### 7. Kiểm tra đã chạy

```powershell
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard-demo\tools\VerifyReportWorkflowAndNotifications.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard-demo\tools\VerifyExcelParser.ps1
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard-demo\HospitalQualityDashboard-demo.csproj /p:Configuration=Debug /p:Platform=AnyCPU /p:MvcBuildViews=true
```

Kết quả:

- `VerifyReportWorkflowAndNotifications.ps1`: passed.
- `VerifyExcelParser.ps1`: passed.
- MSBuild với `MvcBuildViews=true`: build succeeded, 0 warning, 0 error.

### 8. Lưu ý kiểm thử thủ công

- Khi truy cập `localhost:44387/Notification` bằng browser nội bộ mà chưa đăng nhập, hệ thống chuyển về `Account/UserLogin`. Đây là đúng cơ chế bảo vệ session.
- Cần đăng nhập bằng tài khoản User có thông báo tự động gắn `KyBaoCaoId` để kiểm tra thao tác click thông báo và xem danh sách chỉ số quá hạn chưa nộp trên giao diện thật.

## 2026-05-28

Đợt cập nhật này hoàn thiện logic import chỉ số từ file `Phân chia các chỉ số dựa theo đơn vị thu thập và tổng hợp.docx`, tập trung vào 2 vấn đề: nhận diện đúng loại công thức và tự gán `DonViTinh` khi file Word nguồn không có cột đơn vị tính.

### 1. Suy luận loại công thức khi import DOCX

- `IndicatorService.BuildIndicatorFromRow` không còn mặc định chỉ chia `GiaTriTrucTiep`/`TyLe` theo việc có mẫu số hay không.
- Bổ sung `InferFormulaType(ChiSoViewModel model)` để suy luận từ:
  - tên chỉ số;
  - phương pháp tính;
  - tử số;
  - mẫu số.
- Tên chỉ số được chuẩn hóa bằng cách bỏ số thứ tự đầu dòng như `1.`, `8.`, `10.` trước khi nhận diện.
- Các cụm `Tỷ lệ`, `Tỷ suất`, `Công suất`, `Hiệu suất` được nhận diện là `TyLe`.
- Các cụm `Tỷ số` được nhận diện là `TySo`.
- Các chỉ số bắt đầu bằng `Số lượng`, `Số ca`, `Số lượt` được nhận diện là `SoLuong` trước khi xét chữ `điểm`, để chỉ số `Số lượng các điểm tiếp nối...` không bị nhầm sang `DiemTrungBinh`.

### 2. Suy luận đơn vị tính khi import DOCX

- Bổ sung `InferUnit(ChiSoViewModel model)`.
- Nếu file import đã có `DonViTinh`, hệ thống giữ nguyên giá trị trong file.
- Nếu file import thiếu `DonViTinh`, hệ thống tự gán theo rule chi tiết:
  - `%` cho nhóm tỷ lệ, tỷ suất, công suất, hiệu suất;
  - đơn vị tỷ số cụ thể như `bác sĩ/giường bệnh`, `điều dưỡng/giường bệnh`, `bác sĩ/điều dưỡng`, `dược sĩ/giường bệnh`, `nhân viên dinh dưỡng/giường bệnh`, `bác sĩ có chứng chỉ/phẫu thuật viên`;
  - `giờ`, `phút`, `ngày` cho nhóm thời gian;
  - `người`, `báo cáo`, `ca`, `lượt`, `buồng`, `điểm tiếp nối`, `cầu thang` cho nhóm số lượng;
  - `mức độ` cho chỉ số `Vi tính hóa quản lý trang thiết bị y tế khối nội`.
- Nếu không match rule chi tiết, fallback theo loại công thức:
  - `TyLe` -> `%`
  - `TySo` -> `tỷ số`
  - `SoLuong` -> `số lượng`
  - `ThoiGianTrungBinh` -> `thời gian`
  - `DiemTrungBinh` -> `điểm`
  - `GiaTriTrucTiep` -> `giá trị`

### 3. Kiểm tra đã chạy

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard-demo\HospitalQualityDashboard-demo.csproj /p:Configuration=Debug /p:BaseIntermediateOutputPath=obj_unit\ /p:OutputPath=bin_unit\
$env:HQD_APP_ASSEMBLY = (Resolve-Path '.\HospitalQualityDashboard-demo\bin_unit\HospitalQualityDashboardDemo.dll'); powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard-demo\tools\VerifyIndicatorFormulaImport.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard-demo\tools\VerifyExcelParser.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard-demo\tools\VerifyAssignmentExcelExport.ps1
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard-demo\HospitalQualityDashboard-demo.csproj /p:Configuration=Debug /p:MvcBuildViews=true /p:BaseIntermediateOutputPath=obj_unit\ /p:OutputPath=bin_unit\
```

Kết quả:

- Build Debug bằng output riêng `bin_unit`: passed.
- `MvcBuildViews=true`: passed.
- `VerifyIndicatorFormulaImport.ps1`: passed.
- `VerifyExcelParser.ps1`: passed.
- `VerifyAssignmentExcelExport.ps1`: passed.
- Probe trực tiếp file DOCX nguồn đọc được `55/55` chỉ số, `0` chỉ số thiếu `DonViTinh`.

### 4. Lưu ý vận hành

- Các bản ghi đã import trước ngày cập nhật sẽ không tự có `DonViTinh`. Cần import lại file DOCX hoặc chạy cập nhật dữ liệu nếu muốn điền đơn vị cho dữ liệu cũ.
- Trong lúc kiểm thử, build mặc định vào `bin/obj` bị khóa bởi process local đang chạy, nên dùng output riêng `obj_unit/bin_unit` để xác minh code mới mà không cần tắt localhost.

## 2026-05-30

Đợt cập nhật này triển khai chức năng **Tạo lịch kỳ báo cáo tự động** cho Admin và điều chỉnh quy ước hạn nộp theo ngày kết thúc kỳ.

### 1. Mục tiêu kỹ thuật

- Cho phép Admin tạo hàng loạt kỳ báo cáo theo năm.
- Có màn hình preview trước khi ghi database.
- Chống tạo trùng kỳ theo `LoaiKyBaoCao + TuNgay + DenNgay`.
- Tự chuyển kỳ tương lai từ `Nhap` sang `Mo` khi tới ngày bắt đầu.
- Không tạo trước bản ghi `BaoCao` rỗng.
- Giữ cơ chế User chỉ thấy kỳ `Mo` có tần suất khớp chỉ số được phân công.
- Thêm loại kỳ **Hàng ngày** vào chức năng tạo lịch.
- Thống nhất hạn nộp tự động bằng ngày kết thúc kỳ, hiểu là 23:59 của ngày đó.

### 2. Thay đổi ViewModel

`Models/ViewModels/AppViewModels.cs` bổ sung nhóm ViewModel cho tạo lịch:

- `ReportingPeriodScheduleViewModel`: dữ liệu form Admin nhập.
- `ReportingPeriodSchedulePreviewItemViewModel`: từng dòng preview.
- Các trường chính gồm năm, danh sách loại kỳ được chọn, trạng thái mặc định, danh sách preview và thống kê số dòng sẽ tạo mới/đã tồn tại.

Form vẫn giữ `DueDayOffset` ở mức model để tương thích code cũ, nhưng giao diện hiện tại không dùng offset hạn nộp nữa. Hạn nộp của kỳ tự động được tính bằng `DenNgay`.

### 3. Thay đổi service

Tách `Services/IndicatorPeriodServices.cs` thành `IndicatorServices.cs` (IndicatorService, AssignmentService) và `ReportingPeriodServices.cs` (ReportingPeriodService, ReportingPeriodScheduleService).

Các trách nhiệm chính:

- Sinh danh sách kỳ theo năm và tần suất.
- Sinh kỳ hàng ngày từ 01/01 đến 31/12, sau đó lọc bỏ kỳ đã kết thúc trước hôm nay.
- Sinh kỳ tháng, quý, 6 tháng, 9 tháng và năm theo mốc ngày cố định.
- Không sinh `KhiPhatSinh` và `TruocSauKhiThucHien`.
- Kiểm tra kỳ đã tồn tại trước khi tạo.
- Xác định trạng thái dự kiến:
  - `Mo` nếu `TuNgay <= today`;
  - `Nhap` nếu `TuNgay > today`.
- Tạo các kỳ chưa tồn tại.
- Tự mở kỳ đã tới ngày bắt đầu bằng cách cập nhật `TrangThai = Mo` cho các kỳ `Nhap` có `TuNgay <= today`.

Quy tắc bỏ qua kỳ cũ:

```text
Nếu DenNgay < today thì không đưa kỳ vào preview và không tạo mới.
```

Ví dụ ngày 30/05/2026:

- Hàng ngày bắt đầu từ Ngày 30/05/2026.
- Hàng tháng bỏ qua Tháng 01-04/2026.
- Tháng 05/2026 vẫn được giữ vì `DenNgay = 31/05/2026`.

### 4. Thay đổi controller

`Controllers/ReportingPeriodController.cs` bổ sung:

- `GenerateSchedule`: hiển thị form tạo lịch.
- `PreviewSchedule`: nhận form, gọi service sinh preview và trả lại màn hình.
- `CreateSchedule`: tạo các kỳ chưa tồn tại từ cấu hình đã preview.

Controller cũng gọi hàm tự mở kỳ trước khi hiển thị danh sách để đảm bảo Admin thấy trạng thái mới nhất.

Các controller khác gọi tự mở kỳ ở điểm vào nghiệp vụ:

- `DashboardController`.
- `ReportController`.
- `NotificationController`.

`Global.asax.cs` gọi tự mở kỳ khi ứng dụng khởi động. Đây là cơ chế nhẹ, không cần Windows Service, Hangfire hay scheduler ngoài.

### 5. Thay đổi giao diện

`Views/ReportingPeriod/Index.cshtml`:

- Thêm nút **Tạo lịch tự động**.
- Hiển thị trạng thái kỳ bằng tiếng Việt: **Nhập**, **Mở**, **Khóa**.

`Views/ReportingPeriod/GenerateSchedule.cshtml`:

- Form chọn năm.
- Form chọn loại kỳ báo cáo.
- Hỗ trợ các lựa chọn: Hàng ngày, Hàng tháng, Hàng quý, 6 tháng, 9 tháng, Hàng năm.
- Không hiển thị lựa chọn Khi phát sinh và Trước/sau khi thực hiện.
- Hiển thị ghi chú `00:00` cho ngày mở và `23:59` cho ngày đóng/hạn nộp.
- Preview danh sách kỳ dự kiến.
- Badge **Sẽ tạo mới** và **Đã tồn tại**.
- Nút **Tạo các kỳ chưa tồn tại** chỉ dùng sau khi đã preview.

### 6. Điều chỉnh hạn nộp và báo cáo trễ

Quy ước nghiệp vụ mới:

```text
HanNop = DenNgay
HanNop hiển thị là 23:59 của DenNgay
```

Vì database lưu kiểu ngày, code không lưu giờ `23:59` vật lý. Thay vào đó:

- UI hiển thị `23:59` cạnh ngày hạn nộp để người dùng hiểu hạn cuối.
- `ReportDashboardServices.cs` đánh `QuaHan` khi `CAST(GETDATE() AS date) > HanNop`.
- Nếu User gửi trong cùng ngày `HanNop`, kể cả sau giờ hành chính, vẫn được xem là đúng hạn theo quy ước ngày.
- Nếu User gửi từ ngày hôm sau trở đi, báo cáo chuyển `QuaHan`.

Các nơi hiển thị hạn nộp đã được cập nhật để thống nhất:

- Dashboard cảnh báo.
- Danh sách báo cáo.
- Chi tiết thông báo.
- Nội dung thông báo tự động.

### 7. Quy tắc User thấy kỳ phù hợp

Không thay đổi thiết kế dữ liệu động trước đó. User chỉ thấy kỳ khi:

- kỳ `Mo`;
- loại kỳ khớp tần suất của chỉ số đang hoạt động;
- chỉ số được phân công cho khoa/phòng của User;
- phân công đang hoạt động.

Admin vẫn xem được toàn bộ kỳ để quản trị.

### 8. Không tạo `BaoCao` rỗng

Khi tạo lịch tự động, hệ thống chỉ insert vào `KyBaoCao`. Không insert vào:

- `BaoCao`;
- `BaoCaoChiTiet`.

`BaoCao` chỉ được tạo khi User thực hiện một trong các thao tác:

- **Lưu nháp**.
- **Gửi báo cáo**.

Điều này giúp tránh dữ liệu rác và đảm bảo thay đổi phân công chỉ số có hiệu lực ngay trong danh sách cần báo cáo.

### 9. File chính đã chỉnh

- `Controllers/DashboardController.cs`
- `Controllers/NotificationController.cs`
- `Controllers/ReportController.cs`
- `Controllers/ReportingPeriodController.cs`
- `Global.asax.cs`
- `HospitalQualityDashboard-demo.csproj`
- `Models/ViewModels/AppViewModels.cs`
- `Services/IndicatorServices.cs`
- `Services/NotificationExportServices.cs`
- `Services/ReportDashboardServices.cs`
- `Views/Dashboard/Index.cshtml`
- `Views/Notification/Details.cshtml`
- `Views/Report/Index.cshtml`
- `Views/ReportingPeriod/GenerateSchedule.cshtml`
- `Views/ReportingPeriod/Index.cshtml`
- `tools/VerifyReportingPeriodSchedule.ps1`
- `tools/VerifyReportWorkflowAndNotifications.ps1`

### 10. Kiểm tra tự động

Script mới:

```powershell
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard-demo\tools\VerifyReportingPeriodSchedule.ps1
```

Script này kiểm tra các điểm quan trọng:

- tồn tại ViewModel tạo lịch;
- tồn tại màn hình `GenerateSchedule`;
- controller có action preview và tạo lịch;
- service có logic tạo lịch, chống trùng, tự mở kỳ;
- hỗ trợ `HangNgay`, `HangThang`, `HangQuy`, `SauThang`, `ChinThang`, `HangNam`;
- không tạo lịch tự động cho loại phụ thuộc sự kiện;
- bỏ qua kỳ có `DenNgay < today`;
- hạn nộp bằng `DenNgay`;
- UI hiển thị quy ước 00:00/23:59.

Các lệnh nên chạy sau khi sửa:

```powershell
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard-demo\tools\VerifyReportingPeriodSchedule.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard-demo\tools\VerifyReportWorkflowAndNotifications.ps1
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard-demo\HospitalQualityDashboard-demo.csproj /p:Configuration=Debug /p:MvcBuildViews=true
```

### 11. Lưu ý vận hành

- Nếu Admin muốn nhập bù dữ liệu các tháng đã kết thúc, cần tạo kỳ thủ công hoặc thay đổi quy tắc bỏ qua kỳ cũ.
- Nếu sau này bệnh viện muốn “hạn nộp sau ngày kết thúc kỳ N ngày”, cần đưa lại trường offset lên UI và rà soát dashboard/thông báo.
- Nếu muốn đóng kỳ tự động sau hạn nộp, cần tách rõ khái niệm `Mo` để nhập trễ và `Khoa` để không cho nhập nữa. Hiện tại chưa tự khóa kỳ vì hệ thống vẫn hỗ trợ gửi trễ.

---

## 2026-06-13

### Cải tiến Tầng DB, Định dạng Xuất Excel, Nút Xuất Excel Dashboard & Fix Lỗi Profile

### 12. Nội dung cải tiến & Khắc phục lỗi

#### A. Sửa lỗi cập nhật hồ sơ cá nhân (Profile Update Bug)
- **Vấn đề**: Khi cập nhật hồ sơ cá nhân (`/Account/Profile`), nếu có lỗi validation hoặc lỗi cập nhật, dữ liệu form bị mất hoặc controller không có action POST phù hợp.
- **Giải pháp**:
  - Khôi phục action POST `UpdateProfile` trong `AccountController.cs`, import thêm `using System;` để xử lý ngoại lệ `InvalidOperationException`.
  - Triển khai phương thức `UpdateProfile(int taiKhoanId, ProfileUpdateDto dto)` và `GetNhanVienId` trong `AuthService.cs` sử dụng SQL Update ADO.NET thuần.
  - Khi validation thất bại hoặc xảy ra lỗi, giữ lại thông tin người dùng vừa nhập bằng cách gán đè dữ liệu form vào view model trước khi trả về `View("Profile", profile)`.

#### B. Đóng gói SqlTransaction cho các tác vụ thay đổi dữ liệu nhiều bước
- **Lý do**: Đảm bảo tính toàn vẹn dữ liệu (Atomicity) khi thực hiện import hoặc chỉnh sửa dữ liệu nhiều bước.
- **Thực hiện**:
  - Bổ sung helper `ExecuteInTransaction` và overloads `Execute`/`Scalar` nhận `SqlConnection` và `SqlTransaction` trong `DbServiceBase.cs`.
  - Bọc giao dịch `SqlTransaction` cho các nghiệp vụ ghi chép dữ liệu phức tạp:
    - **Báo cáo**: `SaveDraft` (Lưu nháp) và `Delete` (Xóa báo cáo) trong `ReportDashboardServices.cs`.
    - **Chỉ số**: `Save`, `Delete` và `Import` chỉ số trong `IndicatorServices.cs`.
    - **Danh mục & Nhân sự**: `Import` khoa phòng và `Import` nhân viên (bao gồm tạo tài khoản mặc định) trong `ManagementServices.cs`.

#### C. Chuyển đổi toàn bộ định dạng xuất dữ liệu từ CSV sang Excel (.xlsx)
- **Vấn đề**: File CSV gặp lỗi font tiếng Việt hiển thị không chuẩn và kém chuyên nghiệp.
- **Giải pháp**:
  - Thay đổi toàn bộ các hàm xuất dữ liệu danh mục khoa/phòng, nhân viên, chỉ số và báo cáo trong `ExportService` (`NotificationExportServices.cs`) sang gọi `_excel.CreateXlsx` thay vì `CreateCsv`.
  - Điều chỉnh MIME Type trả về thành `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` và đổi phần mở rộng file thành `.xlsx` trong `ExportController.cs` (Admin, User).
  - Cập nhật nhãn của tất cả các nút bấm trong view từ "Xuất CSV" thành "Xuất Excel" (`Areas/Admin/Views/Department/Index.cshtml`, `Employee/Index.cshtml`, `Indicator/Index.cshtml`, `Report/Index.cshtml`, `Areas/User/Views/Indicator/Index.cshtml`, `Report/Index.cshtml`).

#### D. Tính năng xuất Excel tiến độ từ Dashboard Admin
- **Giải pháp**:
  - Thêm nút "Xuất Excel" cạnh tiêu đề "Bảng tiến độ" tại trang Dashboard Admin.
  - Tạo modal Bootstrap `#dashboardExportModal` cho phép Admin tùy chọn các cột xuất: *Khoa / Phòng, Số báo cáo đã gửi, Tổng số chỉ số cần nộp, Tỷ lệ hoàn tất (%)*.
  - Thêm action `DashboardProgress` vào `ExportController` ở root và Area Admin, gọi `ExportDashboardProgress` trong `ExportService` để tạo động file Excel và trả về file `tien-do-khoa-phong.xlsx`.

### 13. File đã chỉnh sửa và bổ sung

- `Controllers/AccountController.cs` (Sửa đổi)
- `Services/AuthService.cs` (Sửa đổi)
- `Services/DbServiceBase.cs` (Sửa đổi)
- `Services/IndicatorServices.cs` (Sửa đổi)
- `Services/ManagementServices.cs` (Sửa đổi)
- `Services/NotificationExportServices.cs` (Sửa đổi)
- `Areas/Admin/Controllers/ExportController.cs` (Sửa đổi)
- `Areas/User/Controllers/ExportController.cs` (Sửa đổi)
- `Controllers/ExportController.cs` (Sửa đổi)
- `Areas/Admin/Views/Dashboard/Index.cshtml` (Sửa đổi)
- `Areas/Admin/Views/Department/Index.cshtml`, `Employee/Index.cshtml`, `Indicator/Index.cshtml`, `Report/Index.cshtml` (Sửa đổi)
- `Areas/User/Views/Indicator/Index.cshtml`, `Report/Index.cshtml` (Sửa đổi)
- `tools/VerifyProfileUpdate.ps1` (Thêm mới)
- `tools/VerifyDashboardExcelExport.ps1` (Thêm mới)

### 14. Tập lệnh kiểm thử xác minh

Có thể chạy bộ script kiểm thử đầy đủ bằng PowerShell:
```powershell
powershell -ExecutionPolicy Bypass -File C:\Users\maiva\.gemini\antigravity\brain\8f49f584-1d44-40b1-a5dd-9b390c10fa7d\scratch\run_tests.ps1
```

### 15. Nâng cấp Bộ lọc Tần suất Báo cáo, Xếp loại Hoàn thành & Thống kê Nâng cao trên Dashboard & Excel

#### A. Tính năng & Logic Nghiệp vụ mới:
1. **Bộ lọc Tần suất Báo cáo**:
   - Thêm dropdown lọc tần suất báo cáo trên thanh công cụ của Bảng tiến độ Dashboard Admin (Tất cả, Hàng tháng, Hàng quý, 6 tháng, Hàng năm).
   - Khi thay đổi bộ lọc, toàn bộ số liệu thống kê (Tổng chỉ số, Đã báo cáo, Còn thiếu, Quá hạn), biểu đồ tiến độ Chart.js và danh sách khoa/phòng sẽ được lọc lại tương ứng.
   - Khi bấm "Xuất Excel" trong lúc đang lọc, tham số `tanSuat` được truyền vào controller và service để lọc dữ liệu xuất Excel đồng bộ với giao diện.
2. **Cơ chế Xếp loại Hoàn thành (Grading logic)**:
   - **Xuất sắc**: Tỷ lệ hoàn tất >= 90%
   - **Khá**: 70% <= Tỷ lệ hoàn tất < 90%
   - **Trung bình**: 50% <= Tỷ lệ hoàn tất < 70%
   - **Yếu**: Tỷ lệ hoàn tất < 50%
   - **N/A**: Nếu tổng số chỉ số được giao bằng 0.
3. **Thống kê nâng cao & Tiến độ chi tiết**:
   - Hiển thị và hỗ trợ xuất Excel các cột: Số báo cáo lưu nháp (`LuuNhap`), Số báo cáo còn thiếu (`ConThieu`), Xếp loại tổng thể (`XepLoai`).
   - Phân rã chi tiết tiến độ ("đã nộp/tổng") và Xếp loại hoàn thành theo từng loại tần suất chính (Hàng tháng, Hàng quý, Hàng năm).

#### B. Tối ưu hóa Database Access (ADO.NET):
- Tránh phép JOIN trực tiếp giữa bảng phân công `pc` và bảng tần suất chỉ số `ts` gây nhân đôi dòng (Cartesian product) đối với các chỉ số có nhiều tần suất.
- Chuyển sang sử dụng cơ chế truy vấn con (Subquery) được tối ưu hóa cho từng khoa/phòng, đếm chính xác số lượng chỉ số và báo cáo theo từng tần suất.

#### C. Các file đã chỉnh sửa:
- `Models/ViewModels/AppViewModels.cs` (Cập nhật `DepartmentProgressViewModel` và `DashboardViewModel`)
- `Services/ReportDashboardServices.cs` (Cập nhật `DashboardService.GetDashboard` hỗ trợ bộ lọc và SQL subqueries mới)
- `Services/NotificationExportServices.cs` (Cập nhật `ExportService.ExportDashboardProgress` và các cột cấu hình xuất Excel)
- `Areas/Admin/Controllers/DashboardController.cs` (Truyền tham số `tanSuat` vào action `Index`)
- `Areas/Admin/Controllers/ExportController.cs` (Truyền tham số `tanSuat` vào action `DashboardProgress`)
- `Controllers/ExportController.cs` (Root controller chuyển hướng kèm tham số `tanSuat`)
- `Areas/Admin/Views/Dashboard/Index.cshtml` (Cập nhật HTML dropdown lọc, bảng hiển thị cột mới, badge màu sắc và modal Excel export)

