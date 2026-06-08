# Hồ Sơ Bối Cảnh Dự Án

Tài liệu này là bản ghi nhớ kỹ thuật và nghiệp vụ tổng quan của dự án `HospitalQualityDashboard`. File được dùng để giúp lập trình viên, người kiểm thử, người viết báo cáo và trợ lý AI hiểu đúng hiện trạng dự án trước khi chỉnh sửa.

## 1. Tổng Quan

**Tên dự án:** HospitalQualityDashboard

**Mục tiêu:** xây dựng hệ thống quản lý chỉ số chất lượng bệnh viện, hỗ trợ Admin quản trị danh mục và hỗ trợ các khoa/phòng nhập, gửi, theo dõi số liệu báo cáo định kỳ.

**Bài toán chính:**

- Quản lý danh mục khoa/phòng, nhân viên và tài khoản đăng nhập.
- Quản lý danh mục chỉ số chất lượng bệnh viện, bao gồm định nghĩa, công thức, nguồn số liệu, tần suất báo cáo và mục tiêu.
- Phân công chỉ số cho một hoặc nhiều khoa/phòng phụ trách.
- Cho phép User khoa/phòng nhập số liệu theo kỳ báo cáo, lưu nháp và gửi báo cáo.
- Cho phép Admin theo dõi tiến độ, khóa hoặc xóa báo cáo khi cần.
- Hiển thị dashboard tiến độ báo cáo theo phạm vi quyền hạn.
- Gửi và theo dõi thông báo cho các khoa/phòng.
- Import dữ liệu từ Excel/Word để giảm thao tác nhập tay.

## 2. Công Nghệ

- Framework: ASP.NET MVC 4 trên .NET Framework 4.7.2.
- Ngôn ngữ: C#.
- View engine: Razor `.cshtml`.
- Cơ sở dữ liệu: SQL Server LocalDB.
- Truy cập dữ liệu: ADO.NET thuần qua `SqlConnection`, `SqlCommand`, `SqlDataReader`.
- Không dùng Entity Framework.
- Frontend: Bootstrap, CSS tùy biến trong `Content/Site.css`, JavaScript, Chart.js cho dashboard.
- Môi trường chạy phổ biến: IIS Express, ví dụ `localhost:44387` hoặc port IIS Express khác khi chạy local.

Connection string chính nằm trong `Web.config`:

```xml
<connectionStrings>
  <add name="HospitalQualityConnection"
       connectionString="Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=HospitalQualityDashboard;Integrated Security=True;MultipleActiveResultSets=True"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

## 3. Cấu Trúc Thư Mục

```text
HospitalQualityDashboard/
├── App_Data/
│   └── Sql/
│       ├── 001_CreateSchema.sql
│       ├── 002_CreateSchema_Revised.sql
│       ├── 003_AddIndicatorFrequencies.sql
│       ├── 004_AddAssignmentUniqueConstraint.sql
│       ├── 005_AddApprovalAndRejection.sql
│       └── 006_AddNotificationAutomationLog.sql
├── App_Start/
│   ├── BundleConfig.cs
│   ├── FilterConfig.cs
│   └── RouteConfig.cs
├── Areas/
│   ├── Admin/
│   │   ├── Controllers/      # Controller cho Admin workflows
│   │   ├── Views/            # View cho Admin workflows
│   │   └── AdminAreaRegistration.cs
│   └── User/
│       ├── Controllers/      # Controller cho User khoa/phòng
│       ├── Views/            # View cho User khoa/phòng
│       └── UserAreaRegistration.cs
├── Content/
│   └── Site.css
├── Controllers/
│   ├── AccountController.cs  # Login/logout (giữ nguyên)
│   ├── HomeController.cs     # Landing page (giữ nguyên)
│   └── PageController.cs     # Lớp base, session guard
├── Models/
│   ├── Entities/CoreEntities.cs
│   ├── Enums/SystemEnums.cs
│   ├── DTOs/                 # Data Transfer Objects
│   └── ViewModels/
│       ├── AppViewModels.cs
│       └── AuthViewModels.cs
├── Services/
│   ├── AuthService.cs
│   ├── DbServiceBase.cs
│   ├── ExcelImportExportService.cs
│   ├── IndicatorPeriodServices.cs
│   ├── ManagementServices.cs
│   ├── NotificationExportServices.cs
│   ├── PasswordHasher.cs
│   ├── ReportDashboardServices.cs
│   └── SessionUserAccessor.cs
├── Views/
│   ├── Account/
│   ├── Home/
│   └── Shared/
├── tools/
│   ├── VerifyExcelParser.ps1
│   ├── VerifyAssignmentExcelExport.ps1
│   ├── VerifyIndicatorFormulaImport.ps1
│   ├── VerifyReportWorkflowAndNotifications.ps1
│   ├── VerifyIndicatorDepartmentAssignmentParser.ps1
│   └── VerifyIndicatorFrequencyParser.ps1
├── implementation-notes.md
├── PROJECT_CONTEXT.md
└── TAI_LIEU_NGHIEP_VU.md
```

## 4. Kiến Trúc Ứng Dụng

### 4.1. Controller

Các controller kế thừa `PageController` để dùng chung cơ chế session và phân quyền.
Sau đợt tái cấu trúc tháng 06/2026, các root controller (`Controllers/`) chỉ đóng vai trò chuyển hướng (redirect wrapper) tới Area controller tương ứng. Logic nghiệp vụ và render view được thực hiện trong `Areas/Admin/Controllers/` và `Areas/User/Controllers/`.

- `AccountController`: đăng nhập Admin/User riêng biệt, đăng xuất, đổi mật khẩu (giữ nguyên root).
- `HomeController`: trang giới thiệu và liên hệ (giữ nguyên root).
- `PageController`: lớp cơ sở, cung cấp session guard, `RequireAdmin()`, `EnsureUserDepartment()`.
- Các root controller khác: tất cả đều là redirect wrapper, chuyển hướng sang Area.
- `Areas/Admin/Controllers/`: chứa logic quản trị cho khoa/phòng, nhân viên, chỉ số, phân công, kỳ báo cáo, báo cáo, thông báo, xuất dữ liệu và dashboard..
- `Areas/User/Controllers/`: chứa logic cho User khoa/phòng: dashboard, báo cáo, chỉ số (xem), thông báo, xuất báo cáo.

### 4.2. Service

Các service chứa nghiệp vụ và truy cập database trực tiếp qua ADO.NET.

- `DbServiceBase`: lớp cơ sở cung cấp `Query`, `Scalar`, `Execute`, `Param` và helper đọc dữ liệu.
- `AuthService`: xác thực tài khoản, đổi mật khẩu, cập nhật lần đăng nhập cuối.
- `SessionUserAccessor`: chuẩn hóa các key session như `TaiKhoanId`, `LoaiTaiKhoan`, `KhoaPhongId`.
- `ManagementServices`: nghiệp vụ khoa/phòng và nhân viên.
- `ExcelImportExportService`: đọc Excel, xử lý shared string, inline string và ô trống bị Excel lược bỏ trong XML.
- `IndicatorPeriodServices`: nghiệp vụ chỉ số, import chỉ số, parser tần suất, parser khoa/phòng, suy luận loại công thức/đơn vị tính, phân công và kỳ báo cáo.
- `ReportDashboardServices`: nhập báo cáo, tính kết quả, gửi/khóa/xóa báo cáo, lấy dữ liệu dashboard.
- `NotificationExportServices`: thông báo, thông báo tự động, chống gửi trùng và xuất dữ liệu.
- `PasswordHasher`: hash/verify mật khẩu bằng PBKDF2.

### 4.3. Quy ước chú thích code

Các file code tự viết đã có chú thích tiếng Việt có dấu ở đầu file theo mẫu `Mục đích:` để mô tả vai trò chính của file. Quy ước này áp dụng cho controller, service, model, filter, cấu hình MVC và Razor view nội bộ.

Không thêm chú thích vào thư viện bên thứ ba như Bootstrap, jQuery, Modernizr hoặc file minified. Khi bổ sung code mới, comment nên ưu tiên giải thích mục đích nghiệp vụ, điều kiện bảo mật hoặc lý do xử lý đặc biệt; không cần mô tả lại thao tác hiển nhiên của từng dòng.

## 5. Phân Quyền

Hệ thống có 2 loại tài khoản:

- `Admin = 1`: quản trị toàn hệ thống.
- `User = 2`: tài khoản khoa/phòng, bắt buộc gắn với một `KhoaPhongId`.

Quy tắc chính:

- Người chưa đăng nhập bị chuyển về trang đăng nhập User.
- Admin có thể truy cập các trang quản trị: khoa/phòng, nhân viên, chỉ số, phân công, kỳ báo cáo, báo cáo, thông báo, export.
- User chỉ nhìn thấy các trang phù hợp: tổng quan, báo cáo của khoa/phòng mình, thông báo, đổi mật khẩu.
- Các controller vẫn kiểm tra quyền ở server bằng `RequireAdmin()` và `EnsureUserDepartment()`, không chỉ dựa vào ẩn/hiện menu.
- `_Layout.cshtml` đã phân biệt menu theo `Session["LoaiTaiKhoan"]` để User không còn thấy link Admin như `Chỉ số`, `Phân công`, `Kỳ báo cáo`.

## 6. Cơ Sở Dữ Liệu Chính

### 6.1. Danh mục tổ chức

- `KhoaPhong`: danh mục khoa/phòng, có mã nguồn `IdKhoaPhongNguon`, tên khoa/phòng và trạng thái sử dụng.
- `NhanVien`: nhân viên thuộc khoa/phòng, có mã nhân viên, họ tên, ngày sinh, giới tính, chức vụ, email, số điện thoại.
- `TaiKhoan`: tài khoản đăng nhập. User bắt buộc có `KhoaPhongId` nhờ constraint `CK_TaiKhoan_UserHasKhoaPhong`.

### 6.2. Chỉ số chất lượng

- `ChiSoChatLuong`: lưu thông tin chỉ số, định nghĩa, lĩnh vực áp dụng, khía cạnh/thành tố chất lượng, nguồn số liệu, phương pháp tính, công thức, đơn vị tính, tần suất mặc định.
- `ChiSoTanSuatBaoCao`: bảng phụ cho phép một chỉ số có nhiều tần suất báo cáo. Đây là phần mở rộng quan trọng so với thiết kế ban đầu chỉ có một tần suất trong `ChiSoChatLuong`.
- `ChiSoMucTieu`: lưu mục tiêu theo năm, toán tử so sánh và giá trị mục tiêu.

### 6.3. Phân công và báo cáo

- `PhanCongChiSo`: bảng trung gian thể hiện quan hệ nhiều-nhiều giữa `ChiSoChatLuong` và `KhoaPhong`.
- `KyBaoCao`: kỳ báo cáo theo tháng/quý/6 tháng/năm hoặc loại kỳ khác, có ngày bắt đầu, ngày kết thúc, hạn nộp và trạng thái.
- `BaoCao`: bản ghi báo cáo theo kỳ, khoa/phòng, chỉ số và phân công cụ thể.
- `BaoCaoChiTiet`: dữ liệu nhập chi tiết gồm tử số, mẫu số, giá trị nhập trực tiếp, kết quả tính, đạt mục tiêu hay không, ghi chú.

### 6.4. Thông báo và nhật ký

- `ThongBao`: thông báo do hệ thống hoặc Admin tạo.
- `ThongBaoNguoiNhan`: danh sách người nhận và trạng thái đã đọc.
- `ThongBaoTuDongLog`: log chống gửi trùng cho thông báo tự động theo `DedupKey`.
- `LichSuImport`: lưu lịch sử import ở mức tổng hợp.
- `NhatKyHeThong`: lưu nhật ký thao tác hệ thống.

## 7. Quan Hệ Nghiệp Vụ Quan Trọng

### 7.1. Chỉ số và khoa/phòng là quan hệ n-n

Một chỉ số có thể do nhiều khoa/phòng cùng thu thập, tổng hợp hoặc phối hợp báo cáo. Một khoa/phòng cũng có thể phụ trách nhiều chỉ số. Vì vậy quan hệ đúng là:

```text
ChiSoChatLuong 1-n PhanCongChiSo n-1 KhoaPhong
```

Ví dụ: chỉ số có trường “Thu thập và tổng hợp số liệu” gồm:

```text
P. KHTH thu thập số liệu
P. TCCB tổng hợp số liệu
```

Hệ thống cần tạo 2 phân công cho cùng một chỉ số:

- Chỉ số A - P. KHTH.
- Chỉ số A - P. TCCB.

Bảng `PhanCongChiSo` có unique constraint `(ChiSoChatLuongId, KhoaPhongId)` để tránh trùng phân công.

### 7.2. Một chỉ số có thể có nhiều tần suất

Một số chỉ số có tần suất mô tả dạng “Mỗi quý, 6 tháng, 12 tháng”. Thiết kế mới dùng bảng `ChiSoTanSuatBaoCao` để lưu nhiều dòng tần suất cho cùng một chỉ số.

Các biến thể tiếng Việt đang được nhận diện là tần suất quý:

- `Hàng quý`
- `Mỗi quý`
- `Theo quý`
- `Quý`
- Các mô tả chứa chu kỳ 3 tháng phù hợp.

## 8. Luồng Nghiệp Vụ Chính

### 8.1. Đăng nhập

1. Người dùng chọn trang đăng nhập Admin hoặc User.
2. `AuthService.Authenticate` kiểm tra tên đăng nhập, mật khẩu, trạng thái hoạt động.
3. `AccountController` đối chiếu đúng `LoaiTaiKhoan` với trang đăng nhập.
4. `SessionUserAccessor` ghi session.
5. Người dùng được chuyển đến Dashboard.

Kiểm tra session trên trình duyệt thực hiện qua DevTools:

- Tab `Application` > `Storage` > `Cookies` cho biết cookie `ASP.NET_SessionId`.
- Cookie chỉ lưu mã session; dữ liệu đăng nhập thực tế nằm phía server trong `Session`.
- `Web.config` hiện chưa khai báo `sessionState timeout`, nên timeout server-side dùng mặc định ASP.NET khoảng 20 phút không hoạt động.
- Sau logout, `SessionUserAccessor.ClearLoginSession` gọi `Clear()` và `Abandon()`, sau đó truy cập trang protected phải quay lại login.

### 8.2. Import khoa/phòng và nhân viên

1. Admin chọn file Excel.
2. `ExcelImportExportService` đọc workbook.
3. Service kiểm tra dòng, map cột, validate dữ liệu.
4. Dữ liệu hợp lệ được insert/update.
5. Lỗi được gom vào `ImportResultViewModel` để hiển thị.

### 8.3. Import chỉ số

1. Admin upload file Excel/Word chứa danh mục chỉ số.
2. Parser đọc từng dòng/bảng và map vào `ChiSoViewModel`.
3. Hệ thống phân tích:
   - mã chỉ số;
   - tên chỉ số;
   - định nghĩa;
   - nguồn số liệu;
   - thu thập/tổng hợp số liệu;
   - tần suất báo cáo;
   - phương pháp tính;
   - tử số/mẫu số;
   - loại công thức;
   - đơn vị tính;
   - mục tiêu.
4. Hệ thống nhận diện khoa/phòng từ trường `ThuThapTongHop`.
5. Hệ thống lưu chỉ số, tần suất, mục tiêu và phân công tương ứng.

Với file DOCX không có cột `Đơn vị tính`, ví dụ `Phân chia các chỉ số dựa theo đơn vị thu thập và tổng hợp.docx`, hệ thống tự suy luận `DonViTinh` sau khi đã xác định `LoaiCongThuc`. Giá trị `DonViTinh` có sẵn trong file import luôn được ưu tiên. Các rule chính:

- `Tỷ lệ`, `Tỷ suất`, `Công suất`, `Hiệu suất` -> `%`.
- `Tỷ số` -> đơn vị tỷ số cụ thể như `bác sĩ/giường bệnh`, `điều dưỡng/giường bệnh`, `bác sĩ/điều dưỡng`.
- Chỉ số thời gian -> `giờ`, `phút`, `ngày`.
- Chỉ số số lượng -> `người`, `báo cáo`, `ca`, `lượt`, `buồng`, `điểm tiếp nối`, `cầu thang`.
- `Vi tính hóa quản lý trang thiết bị y tế khối nội` -> `mức độ`.

Tên chỉ số được bỏ số thứ tự đầu dòng như `8.` hoặc `10.` trước khi nhận diện. Case `Số lượng các điểm tiếp nối...` được ưu tiên là `SoLuong`, không bị nhầm sang `DiemTrungBinh`.

### 8.4. Phân công chỉ số

Admin có 2 cách phân công:

- Chọn nhiều khoa/phòng và nhiều chỉ số trên màn hình `Assignment`.
- Bấm đồng bộ từ dữ liệu chỉ số để hệ thống đọc lại trường `ThuThapTongHop` và tạo các phân công còn thiếu.

Mục tiêu của luồng này là đảm bảo các chỉ số có nhiều khoa/phòng liên quan được gắn đủ phân công, thay vì chỉ gắn 1 khoa/phòng.

### 8.5. Tạo kỳ báo cáo

Admin tạo kỳ báo cáo với loại kỳ, thời gian bắt đầu/kết thúc, hạn nộp. Khi User nhập báo cáo cho kỳ, hệ thống chỉ hiển thị các chỉ số được phân công cho khoa/phòng của User và có tần suất phù hợp với loại kỳ báo cáo.

### 8.6. User nhập và gửi báo cáo

1. User vào `Báo cáo của tôi`.
2. Chọn kỳ báo cáo.
3. Hệ thống liệt kê các chỉ số được phân công cho khoa/phòng của User.
4. User nhập tử số/mẫu số hoặc giá trị trực tiếp tùy `LoaiCongThuc`.
5. Hệ thống tính `KetQua`, so sánh mục tiêu nếu có và lưu chi tiết.
6. User gửi báo cáo. Nếu gửi trước hoặc đúng hạn, trạng thái chuyển từ `Nhap` sang `DaGui`; nếu gửi sau `KyBaoCao.HanNop`, trạng thái chuyển sang `QuaHan`.

### 8.7. Admin theo dõi và xử lý báo cáo

Admin xem danh sách báo cáo toàn viện, lọc theo kỳ, khoa/phòng, chỉ số. Danh sách Admin chỉ gồm các báo cáo `DaGui`, `QuaHan`, `DaKhoa`; báo cáo `Nhap` của User không hiển thị cho Admin.

Admin có thể:

- xem chi tiết báo cáo ở chế độ chỉ đọc;
- khóa báo cáo đã gửi đúng hạn hoặc gửi trễ;
- xóa báo cáo nếu cần xử lý dữ liệu sai trong quá trình vận hành thử;
- xuất dữ liệu.

Các route duyệt/trả lại báo cáo không còn thuộc luồng chính và bị chặn. Admin cũng không được submit hoặc sửa số liệu thay User.

### 8.8. Dashboard

Dashboard hiển thị theo quyền:

- Admin thấy thống kê toàn viện và tiến độ theo từng khoa/phòng.
- User thấy số chỉ số được phân công, số đã gửi, còn thiếu, quá hạn của khoa/phòng mình.
- User thấy cảnh báo ngay sau khi đăng nhập nếu có chỉ số chưa báo cáo, gần đến hạn hoặc đã quá hạn chưa nộp. Cảnh báo có danh sách chi tiết và link nhập báo cáo nhanh.

Quy tắc thống kê quan trọng:

```text
Đã báo cáo = DaGui + QuaHan + DaKhoa
```

`QuaHan` trong `BaoCao` nghĩa là đã gửi trễ, không phải slot chưa gửi sau hạn.

### 8.9. Thông báo tự động và chi tiết thông báo

Thông báo nội bộ hiện có 2 nhóm:

- Thông báo thủ công do Admin gửi.
- Thông báo tự động do `NotificationAutomationService` tạo.

Các loại thông báo tự động chính:

- `KyBaoCaoMo`: nhắc kỳ báo cáo đã mở.
- `NhacHan`: nhắc các khoa/phòng còn thiếu báo cáo trước hạn hoặc đúng ngày hạn.
- `QuaHan`: cảnh báo còn chỉ số quá hạn chưa nộp.
- `TongHopAdmin`: tổng hợp tiến độ hằng ngày cho Admin.

Khi User bấm vào một thông báo có gắn `KyBaoCaoId`, `NotificationController.Details` mở trang chi tiết và truy vấn danh sách chỉ số còn thiếu của khoa/phòng trong kỳ đó. Với thông báo `QuaHan`, danh sách chỉ lấy các chỉ số đã qua hạn nhưng chưa có báo cáo ở trạng thái `DaGui`, `QuaHan` hoặc `DaKhoa`.

Trang chi tiết thông báo hiển thị form POST có anti-forgery để User đánh dấu thông báo là đã đọc; GET chi tiết chỉ đọc dữ liệu.

## 9. Giao Diện

Giao diện đã được cải tiến theo hướng hiện đại, dùng tiếng Việt có dấu. Các điểm chính:

- Navigation tối, rõ vai trò.
- Card thống kê dashboard.
- Bảng dữ liệu dễ đọc, có spacing và trạng thái.
- Form nhập liệu dùng nhãn tiếng Việt có dấu.
- Menu phân quyền theo Admin/User.
- Các nút gây 401 cho User đã được ẩn ở giao diện User, ví dụ `Chỉ số`, `Phân công`, `Xuất CSV`, `Khóa`, `Xóa`, `Gửi thông báo`.

## 10. Kiểm Thử Và Công Cụ Xác Minh

Các script trong `tools/` hỗ trợ kiểm thử nhanh các parser quan trọng:

- `VerifyExcelParser.ps1`: kiểm tra parser Excel xử lý đúng shared strings, inline strings và ô trống.
- `VerifyAssignmentExcelExport.ps1`: kiểm tra xuất Excel trên trang phân công.
- `VerifyIndicatorFormulaImport.ps1`: kiểm tra import DOCX, alias nhãn tiếng Việt, suy luận loại công thức và đơn vị tính.
- `VerifyReportWorkflowAndNotifications.ps1`: kiểm tra luồng báo cáo, dashboard cảnh báo thiếu báo cáo, thông báo tự động và trang chi tiết thông báo.
- `VerifyIndicatorFrequencyParser.ps1`: kiểm tra nhận diện tần suất, đặc biệt các biến thể quý.
- `VerifyIndicatorDepartmentAssignmentParser.ps1`: kiểm tra nhận diện nhiều khoa/phòng trong trường thu thập/tổng hợp số liệu.

Lệnh build kiểm tra Razor view:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU /p:MvcBuildViews=true
```

## 11. Các Quyết Định Thiết Kế Đã Chốt

- **Dùng ADO.NET thuần** thay vì Entity Framework để giữ dự án đơn giản, hiệu năng tối đa và phù hợp với hiện trạng ban đầu.
- **User bắt buộc gắn với khoa/phòng** và bị giới hạn nghiêm ngặt phạm vi truy cập dữ liệu ở mức Server-side (không chỉ ẩn/hiện ở Client-side).
- **Chỉ số - Khoa/phòng là quan hệ nhiều-nhiều** thông qua bảng trung gian `PhanCongChiSo` hỗ trợ thuộc tính trạng thái hoạt động độc lập.
- **Một chỉ số có thể cấu hình nhiều tần suất báo cáo** thông qua bảng phụ `ChiSoTanSuatBaoCao`, cho phép linh hoạt ghép nối tần suất nghiệp vụ thực tế.
- **Import DOCX không phụ thuộc tuyệt đối vào cột đơn vị tính**: Nếu file chỉ số không có `DonViTinh`, hệ thống suy luận đơn vị từ tên chỉ số và loại công thức. Nếu file có `DonViTinh`, giá trị file được ưu tiên để tránh ghi đè dữ liệu chủ động của Admin.
- **Sinh "kỳ báo cáo chi tiết" (Reporting Slots) động bằng cơ chế LEFT JOIN**: Hệ thống không ghi trước các dòng trống xuống cơ sở dữ liệu khi Admin tạo một kỳ báo cáo mới. Thay vào đó, khi User truy cập, hệ thống sử dụng truy vấn `LEFT JOIN` giữa bảng phân công chỉ số của khoa đó (`PhanCongChiSo`) và bảng báo cáo thực tế (`dbo.BaoCao`).
  - *Lợi ích*: Tiết kiệm dung lượng lưu trữ tối đa, tránh dư thừa dữ liệu. Đồng thời giúp cập nhật danh sách ngay lập tức khi Admin thay đổi phân công chỉ số mà không cần thao tác đồng bộ phức tạp.
  - *Giải thích số lượng slots*: Số lượng "slot" báo cáo hiển thị chính xác tương ứng với số lượng chỉ số đang hoạt động của khoa đó có tần suất trùng với tần suất của kỳ báo cáo. Ví dụ, nếu khoa chỉ phụ trách 3 chỉ số có tần suất 6 tháng, thì trong kỳ báo cáo 6 tháng hệ thống sẽ hiển thị đúng 3 slot tương ứng.
- **Duy trì trạng thái giao diện (State Preservation)**: Khi Admin thực hiện các hành động cập nhật phân công (Kích hoạt, Tạm dừng, Xóa), hệ thống bảo lưu toàn bộ tham số tìm kiếm, lọc khoa phòng, bộ lọc trạng thái và trang phân trang hiện tại thông qua các tham số Route Values truyền trực tiếp trong Action Redirect.
- **Tài liệu dự án dùng UTF-8 sạch** và viết hoàn toàn bằng tiếng Việt có dấu.
- **Phạm vi báo cáo hiện tại không có duyệt/trả lại**: Admin theo dõi, xem, khóa và xóa báo cáo; User chịu trách nhiệm nhập/gửi. Các trạng thái cũ phục vụ tương thích dữ liệu nhưng không xuất hiện trong luồng chính.
- **Phân biệt báo cáo gửi trễ và slot quá hạn chưa nộp**: `BaoCao.QuaHan` chỉ dùng cho báo cáo đã gửi sau hạn; slot chưa gửi sau hạn được tính bằng truy vấn thiếu báo cáo và dùng cho dashboard/thông báo.
- **Thông báo tự động phải idempotent**: `ThongBaoTuDongLog.DedupKey` chống tạo trùng khi service được gọi nhiều lần từ Dashboard, trang thông báo hoặc tác vụ nền sau này.

## 12. Các Cải Tiến Nghiệp Vụ Mới Nhất (Ngày 23/05/2026)

Hệ thống đã được nâng cấp toàn diện với các giải pháp kỹ thuật hiện đại:
1. **Phân trang & Lọc nâng cao cho Phân công**:
   - Giao diện Phân công được tích hợp phân trang 20 chỉ số/trang kèm 5 thẻ thống kê động (Tổng số chỉ số, Đã phân công, Chưa phân công, Tổng phân công, Đã tạm dừng).
   - Card "Chưa phân công" giúp Admin theo dõi các chỉ số bị bỏ quên.
   - Sửa lỗi logic khi click "Tạm dừng": Phân công bị tạm dừng chỉ chuyển trạng thái `DangHoatDong = 0` chứ không bị xóa khỏi chỉ số, khoa/phòng vẫn được giữ lại với trạng thái cảnh báo màu cam rõ ràng.
2. **AJAX Preview & Bulk Actions**:
   - Khi chọn phân công hàng loạt, hệ thống gọi AJAX đến phương thức `Preview` hiển thị bảng xem trước trạng thái phân công là `✅ Mới` (sẽ được tạo) hay `⚠️ Đã tồn tại` (sẽ bỏ qua) để tránh trùng lặp.
   - Hỗ trợ chọn nhiều dòng qua checkbox và thực hiện kích hoạt/tạm dừng/xóa hàng loạt thông qua thanh công cụ nổi (Bulk Action Floating Bar).
3. **Xác thực biểu mẫu Chỉ số đa lớp**:
   - Biểu mẫu tạo mới/sửa chỉ số được chia thành 4 khu vực trực quan rõ ràng.
   - Các trường bắt buộc được gắn nhãn dấu sao đỏ `*`, các trường không bắt buộc được gắn nhãn phụ màu xám `(Không bắt buộc)`.
   - Tích hợp `jquery.validate.unobtrusive` ở Client-side cho phép thông báo lỗi tức thời mà không cần reload trang. Ở Server-side, `ModelState.IsValid` được kiểm tra chặt chẽ và hiển thị hộp thoại cảnh báo Bootstrap chi tiết ở đầu trang nếu có lỗi gửi lên.
4. **Bộ lọc thông minh kỳ báo cáo cho User thường**:
   - Khi tài khoản khoa/phòng đăng nhập, hệ thống tự động kiểm tra các tần suất của các chỉ số mà khoa đó đang phụ trách (bao gồm tần suất mặc định và tần suất trong bảng `ChiSoTanSuatBaoCao`).
   - Danh sách "Kỳ báo cáo đang mở" sẽ được lọc để chỉ hiển thị các kỳ báo cáo có tần suất tương thích với tần suất của khoa phòng đó, giúp giao diện người dùng gọn gàng, tránh nhầm lẫn nhập liệu.

## 13. Hướng Phát Triển Tiếp Theo

- Bổ sung luồng duyệt/trả lại báo cáo đầy đủ nếu bệnh viện cần quy trình phê duyệt nhiều bước.
- Bổ sung trang quản lý tài khoản riêng nếu Admin cần CRUD tài khoản độc lập ngoài chức năng tạo tài khoản từ nhân viên.
- Bổ sung audit log đầy đủ cho thao tác sửa/xóa/khóa báo cáo.
- Bổ sung test tự động ở mức service cho parser import và nghiệp vụ báo cáo.
- Chuẩn hóa export Excel định dạng đẹp hơn thay vì chỉ CSV.
- Tích hợp kênh gửi thông báo ngoài hệ thống như email, SMS hoặc Zalo nếu bệnh viện cần nhắc việc ngoài web app.

## 14. Cập Nhật Ngày 26/05/2026

Đợt cập nhật này chốt lại quy trình báo cáo và nâng cấp thông báo theo phạm vi hiện tại của dự án:

- Admin không thấy báo cáo nháp của User và không sửa số liệu báo cáo.
- User chỉ sửa báo cáo khi còn `Nhap`; sau khi gửi thì chỉ xem.
- Khi gửi báo cáo sau hạn, service tự đặt trạng thái `QuaHan`.
- Admin có thể khóa cả báo cáo `DaGui` và `QuaHan`.
- Dashboard User hiển thị cảnh báo ngay khi đăng nhập nếu còn chỉ số chưa báo cáo, gần hạn hoặc quá hạn chưa nộp.
- Danh sách thông báo có link “Xem chi tiết”; tiêu đề/nội dung thông báo cũng có thể click.
- Trang `Views/Notification/Details.cshtml` hiển thị nội dung thông báo và danh sách chỉ số còn thiếu của kỳ báo cáo liên quan.
- Với thông báo `QuaHan`, trang chi tiết chỉ hiển thị các chỉ số quá hạn chưa nộp thuộc kỳ đó.
- Khi User mở chi tiết thông báo, hệ thống chỉ hiển thị nội dung; thao tác đánh dấu đã đọc dùng POST riêng có anti-forgery.

## 15. Cập Nhật Ngày 28/05/2026

Đợt cập nhật này hoàn thiện import chỉ số từ file DOCX nguồn:

- `IndicatorService.BuildIndicatorFromRow` gọi `InferFormulaType` khi file thiếu `LoaiCongThuc`.
- `InferFormulaType` bỏ số thứ tự đầu tên chỉ số trước khi nhận diện, nhận diện thêm `Tỷ suất`, `Công suất`, `Hiệu suất`, và ưu tiên `SoLuong` cho các chỉ số bắt đầu bằng `Số lượng`, `Số ca`, `Số lượt`.
- `InferUnit` tự gán `DonViTinh` khi file import không có đơn vị tính.
- Các đơn vị chi tiết đã hỗ trợ gồm `%`, các đơn vị tỷ số cụ thể, `giờ`, `phút`, `ngày`, `người`, `báo cáo`, `ca`, `lượt`, `buồng`, `điểm tiếp nối`, `cầu thang`, `mức độ`.
- Probe trên file `Phân chia các chỉ số dựa theo đơn vị thu thập và tổng hợp.docx` đọc được 55/55 chỉ số và 0 chỉ số thiếu `DonViTinh`.
- Dữ liệu cũ đã import trước khi cập nhật cần import lại hoặc chạy cập nhật bổ sung để điền `DonViTinh`.

## 16. Cập Nhật Ngày 30/05/2026 - Tạo Lịch Kỳ Báo Cáo Tự Động

Đợt cập nhật này bổ sung chức năng tạo lịch kỳ báo cáo tự động theo năm cho Admin. Mục tiêu là giảm thao tác tạo kỳ thủ công, nhưng vẫn giữ nguyên mô hình dữ liệu động của hệ thống: chỉ tạo `KyBaoCao`, không tạo sẵn `BaoCao` rỗng cho từng khoa/phòng và từng chỉ số.

### 16.1. Phạm vi chức năng

- Admin vào màn hình **Kỳ báo cáo** và bấm **Tạo lịch tự động**.
- Admin chọn năm cần tạo lịch.
- Admin chọn một hoặc nhiều loại kỳ cần sinh.
- Hệ thống hiển thị preview trước khi lưu, gồm cả kỳ sẽ tạo mới và kỳ đã tồn tại.
- Khi Admin xác nhận, hệ thống chỉ tạo các kỳ chưa tồn tại.
- User không thấy màn hình tạo lịch tự động.
- User chỉ thấy các kỳ đang **Mở** có tần suất khớp với chỉ số đang được phân công cho khoa/phòng của mình.

### 16.2. Các loại kỳ được hỗ trợ

| Loại kỳ nghiệp vụ | Enum trong code | Số kỳ tối đa trong một năm | Ghi chú |
|---|---|---:|---|
| Hàng ngày | `HangNgay` | 365 hoặc 366 | Mỗi ngày là một kỳ riêng. |
| Hàng tháng | `HangThang` | 12 | Mỗi tháng là một kỳ. |
| Hàng quý | `HangQuy` | 4 | Quý I, II, III, IV. |
| 6 tháng | `SauThang` | 2 | 6 tháng đầu năm và 6 tháng cuối năm. |
| 9 tháng | `ChinThang` | 1 | Từ 01/01 đến 30/09. |
| Hàng năm | `HangNam` | 1 | Từ 01/01 đến 31/12. |

Không sinh lịch tự động cho:

- `KhiPhatSinh`.
- `TruocSauKhiThucHien`.

Hai loại này phụ thuộc sự kiện nghiệp vụ thực tế, không phù hợp để tạo sẵn theo lịch năm.

### 16.3. Quy ước thời gian 00:00 và 23:59

Database hiện lưu `TuNgay`, `DenNgay`, `HanNop` theo kiểu ngày. Vì vậy hệ thống dùng quy ước nghiệp vụ sau:

- `TuNgay` = thời điểm mở kỳ lúc **00:00** của ngày bắt đầu.
- `DenNgay` = thời điểm đóng kỳ lúc **23:59** của ngày kết thúc.
- `HanNop` = hạn nộp cuối cùng lúc **23:59** của ngày kết thúc.

Ví dụ vận hành:

| Loại kỳ | Tên kỳ | Mở lúc | Đóng lúc | Hạn nộp cuối cùng |
|---|---|---|---|---|
| Hàng ngày | Ngày 30/05/2026 | 30/05/2026 00:00 | 30/05/2026 23:59 | 30/05/2026 23:59 |
| Hàng tháng | Tháng 06/2026 | 01/06/2026 00:00 | 30/06/2026 23:59 | 30/06/2026 23:59 |
| Hàng quý | Quý II/2026 | 01/04/2026 00:00 | 30/06/2026 23:59 | 30/06/2026 23:59 |
| 6 tháng | 6 tháng cuối năm 2026 | 01/07/2026 00:00 | 31/12/2026 23:59 | 31/12/2026 23:59 |
| 9 tháng | 9 tháng năm 2026 | 01/01/2026 00:00 | 30/09/2026 23:59 | 30/09/2026 23:59 |
| Hàng năm | Năm 2026 | 01/01/2026 00:00 | 31/12/2026 23:59 | 31/12/2026 23:59 |

Do database chỉ lưu ngày, phần `23:59` được thể hiện ở giao diện và trong tài liệu để người dùng hiểu đúng hạn cuối. Khi User gửi báo cáo, hệ thống đánh trễ khi **ngày hiện tại lớn hơn ngày hạn nộp**, không đánh trễ trong chính ngày `HanNop`.

### 16.4. Quy tắc bỏ qua kỳ đã kết thúc

Khi tạo lịch cho năm hiện tại, hệ thống không sinh các kỳ đã kết thúc trước hôm nay. Điều này tránh tình trạng ngày 30/05/2026 nhưng preview vẫn đề xuất tạo các kỳ như Tháng 01/2026 hoặc Tháng 02/2026.

Ví dụ nếu hôm nay là **30/05/2026**:

- Lịch hàng ngày bắt đầu từ Ngày 30/05/2026.
- Lịch hàng tháng bỏ qua Tháng 01, 02, 03, 04/2026.
- Tháng 05/2026 vẫn được preview vì kỳ này kết thúc ngày 31/05/2026.
- Tháng 06/2026 trở đi là kỳ tương lai.
- Kỳ 9 tháng năm 2026 vẫn được preview vì kết thúc ngày 30/09/2026.
- Kỳ năm 2026 vẫn được preview vì kết thúc ngày 31/12/2026.

Nếu Admin tạo lịch cho năm tương lai, hệ thống preview toàn bộ kỳ của năm đó. Nếu Admin tạo lịch cho năm đã qua, các kỳ đã kết thúc trước hôm nay sẽ không được đề xuất tạo mới.

### 16.5. Quy tắc trạng thái

Enum trong code vẫn giữ nguyên để không phá dữ liệu và luồng hiện có:

| Enum | Hiển thị | Ý nghĩa |
|---|---|---|
| `Nhap` | Nhập | Kỳ đã có trong hệ thống nhưng chưa tới ngày bắt đầu. |
| `Mo` | Mở | Kỳ đã tới ngày bắt đầu và User có thể nhập/gửi báo cáo. |
| `Khoa` | Khóa | Kỳ đã bị khóa, User không tiếp tục nhập/sửa. |

Khi tạo lịch:

- Nếu `TuNgay <= hôm nay`, kỳ được tạo ở trạng thái `Mo`.
- Nếu `TuNgay > hôm nay`, kỳ được tạo ở trạng thái `Nhap`.
- Các kỳ `Nhap` sẽ tự chuyển sang `Mo` khi tới ngày bắt đầu.

Hàm tự mở kỳ được gọi ở các điểm nhẹ:

- khi ứng dụng khởi động;
- khi truy cập Dashboard;
- khi truy cập Báo cáo;
- khi truy cập Kỳ báo cáo;
- khi truy cập Thông báo.

Giai đoạn này chưa dùng background scheduler phức tạp vì nhu cầu chỉ cần tự đồng bộ trạng thái theo ngày.

### 16.6. Chống trùng và không tạo báo cáo rỗng

Hệ thống chống trùng kỳ theo bộ khóa nghiệp vụ:

```text
LoaiKyBaoCao + TuNgay + DenNgay
```

Nếu kỳ đã tồn tại:

- preview hiển thị là **Đã tồn tại**;
- khi bấm tạo, hệ thống bỏ qua dòng đó;
- không cập nhật đè trạng thái, tên kỳ hoặc hạn nộp của kỳ cũ.

Chức năng này không sinh dòng `BaoCao` rỗng. Danh sách User cần báo cáo vẫn được tính động bằng truy vấn kết hợp:

- kỳ báo cáo đang `Mo`;
- tần suất kỳ báo cáo;
- chỉ số đang hoạt động;
- bảng tần suất phụ `ChiSoTanSuatBaoCao`;
- phân công chỉ số đang hoạt động;
- khoa/phòng của User;
- bản ghi `BaoCao` thực tế nếu đã lưu nháp hoặc gửi.

Nhờ vậy, nếu Admin thay đổi phân công sau khi tạo kỳ, danh sách cần báo cáo của User vẫn cập nhật đúng mà không cần tạo/xóa lại các báo cáo rỗng.

### 16.7. File kỹ thuật chính

- `Models/ViewModels/AppViewModels.cs`: chứa ViewModel tạo lịch và preview.
- `Services/IndicatorPeriodServices.cs`: chứa `ReportingPeriodScheduleService`, logic sinh kỳ, chống trùng, bỏ qua kỳ cũ và tự mở kỳ.
- `Controllers/ReportingPeriodController.cs`: thêm action `GenerateSchedule`, `PreviewSchedule`, `CreateSchedule`.
- `Views/ReportingPeriod/GenerateSchedule.cshtml`: màn hình Admin chọn năm, loại kỳ và xem preview.
- `Views/ReportingPeriod/Index.cshtml`: thêm nút **Tạo lịch tự động**.
- `Controllers/ReportingPeriodController.cs` và `Controllers/NotificationController.cs`: có endpoint POST có anti-forgery để Admin mở kỳ đến hạn hoặc chạy automation thủ công; các trang GET chính giữ nguyên read-only.
- `Global.asax.cs`: gọi tự mở kỳ khi ứng dụng khởi động.
- `Services/ReportDashboardServices.cs`: xác định báo cáo trễ theo ngày hạn nộp, phù hợp quy ước hạn cuối 23:59.
- `tools/VerifyReportingPeriodSchedule.ps1`: script kiểm tra cấu trúc chức năng tạo lịch tự động.

### 16.8. Kiểm thử cần giữ

Các kịch bản tối thiểu:

- Tạo lịch hàng ngày cho ngày hiện tại sinh kỳ hôm nay ở trạng thái **Mở**.
- Tạo lịch hàng tháng cho năm 2026 tại ngày 30/05/2026 bỏ qua Tháng 01-04/2026, giữ Tháng 05/2026 trở đi.
- Tạo lịch quý sinh đúng các quý còn hiệu lực.
- Tạo lịch 6 tháng sinh đúng kỳ còn hiệu lực.
- Tạo lịch 9 tháng sinh một kỳ từ 01/01 đến 30/09 nếu kỳ chưa kết thúc.
- Tạo lịch năm sinh một kỳ từ 01/01 đến 31/12 nếu kỳ chưa kết thúc.
- Chạy tạo lịch lần hai không tạo trùng.
- Kỳ tương lai là `Nhap`.
- Kỳ tới ngày bắt đầu tự chuyển sang `Mo`.
- User chỉ thấy kỳ `Mo` khớp tần suất chỉ số được phân công.
- Admin vẫn xem được toàn bộ kỳ báo cáo.

Lệnh kiểm tra:

```powershell
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyReportingPeriodSchedule.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyReportWorkflowAndNotifications.ps1
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:MvcBuildViews=true
```
