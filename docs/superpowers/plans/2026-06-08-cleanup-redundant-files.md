# Dọn dẹp file dư thừa sau khi chuyển Areas

> **Mục tiêu:** xóa các file không còn được dùng sau khi các root controller đã chuyển thành redirect wrapper và logic nghiệp vụ đã nằm trong Areas.

## Kết quả scan

### Nhóm 1: Có thể xóa an toàn

| File | Lý do |
|---|---|
| `Views/Home/About.cshtml` | Template ASP.NET MVC mặc định, không link vào từ đâu |
| `Views/Home/Contact.cshtml` | Template ASP.NET MVC mặc định, không link vào từ đâu |
| `Views/Account/ChangePassword.cshtml` | `AccountController.ChangePassword` GET redirect về Profile ngay, view không bao giờ render |
| `Filters/RequireLoginAttribute.cs` | Chỉ được `HomeController` dùng `[RequireLogin]`, nhưng Home/Index là landing page public. Filter redirect đến `Account/Login` (không tồn tại, route đó redirect về UserLogin). **Filter không dùng thực tế.** |

### Nhóm 2: File cần giữ

| File | Lý do |
|---|---|
| `Views/Account/AdminLogin.cshtml` | Login Admin |
| `Views/Account/UserLogin.cshtml` | Login User |
| `Views/Account/Profile.cshtml` | Hồ sơ + đổi mật khẩu |
| `Views/Home/Index.cshtml` | Landing page, link vào từ layout brand |
| `Views/Shared/Error.cshtml` | Error page |
| `Views/Shared/_Layout.cshtml` | Layout cho Account/Home pages |
| `Views/_ViewStart.cshtml` | MVC convention |
| `Views/Web.config` | MVC convention |

### Nhóm 3: Có thể clean

| File | Vấn đề |
|---|---|
| `Controllers/HomeController.cs` | `About()` và `Contact()` action không được dùng. `[RequireLogin]` trỏ đến route không tồn tại. Cần đơn giản hoá: bỏ `[RequireLogin]`, xóa 2 action thừa. |

### Nhóm 4: Đã xác nhận snapshot

- 8 thư mục root views đã xoá (Phase 4)
- `csproj` đã sạch content references cũ
- `AccountController` redirect Area đúng sau login

## Các bước thực hiện

### Task 1: Xóa Views dư thừa

- [ ] **Xóa `Views/Home/About.cshtml`**
- [ ] **Xóa `Views/Home/Contact.cshtml`**
- [ ] **Xóa `Views/Account/ChangePassword.cshtml`**
- [ ] **Xóa `Filters/RequireLoginAttribute.cs`**
- [ ] **Xóa khỏi `.csproj` các `<Content Include=...>` tương ứng**
- [ ] **Build kiểm tra** với `MvcBuildViews=true`

### Task 2: Đơn giản hóa HomeController

- [ ] **Sửa `Controllers/HomeController.cs`**: bỏ `[RequireLogin]`, xóa `About()` và `Contact()` actions, dùng `Controller` thay vì `[RequireLogin]` (giữ nguyên `using` cần thiết)

### Task 3: Verification

- [ ] **Build + scripts pass**: MSBuild 0 error, `VerifyAreasAndDtos.ps1` pass, `VerifySecurityHardening.ps1` pass
