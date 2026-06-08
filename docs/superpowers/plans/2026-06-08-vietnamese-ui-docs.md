# Vietnamese UI and Docs Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Chuẩn hóa giao diện, thông báo người dùng và tài liệu chính sang tiếng Việt có dấu mà không đổi business logic hoặc identifier kỹ thuật.

**Architecture:** Thực hiện theo 4 phase: layout/account/home, Admin/User views, controller messages, docs chính. Chỉ sửa text hiển thị và nội dung docs; không đổi route action/controller name, enum, SQL schema, class/method names.

**Tech Stack:** ASP.NET MVC 4, Razor `.cshtml`, C#, Markdown, MSBuild.

---

## File Map

### Sửa giao diện `.cshtml`

- `HospitalQualityDashboard/Views/Shared/_Layout.cshtml`
- `HospitalQualityDashboard/Views/Home/Index.cshtml`
- `HospitalQualityDashboard/Views/Account/AdminLogin.cshtml`
- `HospitalQualityDashboard/Views/Account/UserLogin.cshtml`
- `HospitalQualityDashboard/Views/Account/Profile.cshtml`
- `HospitalQualityDashboard/Views/Shared/Error.cshtml`
- `HospitalQualityDashboard/Areas/Admin/Views/**/*.cshtml`
- `HospitalQualityDashboard/Areas/User/Views/**/*.cshtml`

### Sửa message trong `.cs`

- `HospitalQualityDashboard/Controllers/*.cs`
- `HospitalQualityDashboard/Areas/Admin/Controllers/*.cs`
- `HospitalQualityDashboard/Areas/User/Controllers/*.cs`
- Chỉ sửa string người dùng thấy: `TempData`, `ModelState.AddModelError`, `HttpStatusCodeResult`, `HttpUnauthorizedResult` message.

### Sửa docs chính `.md`

- `README.md`
- `HospitalQualityDashboard/AGENTS.md`
- `HospitalQualityDashboard/PROJECT_CONTEXT.md`
- `HospitalQualityDashboard/TAI_LIEU_NGHIEP_VU.md`
- `HospitalQualityDashboard/implementation-notes.md`

### Không sửa

- `packages/`, `bin/`, `obj/`
- `docs/superpowers/**` cũ ngoài plan/spec mới này
- `HospitalQualityDashboard/docs/superpowers/**` lịch sử
- Tên class/method/namespace/route/action/enum/table/column

---

## Task 1: Việt hóa layout, account, home, error views

**Files:**
- Modify: `HospitalQualityDashboard/Views/Shared/_Layout.cshtml`
- Modify: `HospitalQualityDashboard/Views/Home/Index.cshtml`
- Modify: `HospitalQualityDashboard/Views/Account/AdminLogin.cshtml`
- Modify: `HospitalQualityDashboard/Views/Account/UserLogin.cshtml`
- Modify: `HospitalQualityDashboard/Views/Account/Profile.cshtml`
- Modify: `HospitalQualityDashboard/Views/Shared/Error.cshtml`

- [ ] **Step 1: Scan text còn không dấu/tiếng Anh**

Run:
```bash
grep -RInE "Admin workspace|User workspace|Tong quan|Khoa/phong|Nhan vien|Chi so|Phan cong|Ky bao cao|Bao cao|Thong bao|Dang xuat|Trang chu|Login|Logout|Profile|Error|Home" HospitalQualityDashboard/Views HospitalQualityDashboard/Areas/*/Views/Shared
```

Expected: liệt kê các dòng cần sửa, không sửa route/action names trong `ActionLink`.

- [ ] **Step 2: Update root `_Layout.cshtml` visible text only**

Keep route values unchanged. Translate only visible strings/ARIA/comment text:

- `Mở menu` giữ nguyên nếu đã có dấu.
- `Hồ sơ`, `Đăng xuất`, `Trang chủ`, `Bảng điều khiển chất lượng bệnh viện` phải có dấu.
- If there is `using (Html.BeginForm(...))` without `@using`, keep current working syntax.

- [ ] **Step 3: Update Account/Login/Profile views**

Translate visible headings/labels/buttons/messages to Vietnamese with diacritics:

Examples:
```text
Ten dang nhap -> Tên đăng nhập
Mat khau -> Mật khẩu
Dang nhap -> Đăng nhập
Ho so -> Hồ sơ
Doi mat khau -> Đổi mật khẩu
Mat khau hien tai -> Mật khẩu hiện tại
Mat khau moi -> Mật khẩu mới
Nhap lai mat khau moi -> Nhập lại mật khẩu mới
```

Do not rename model properties such as `TenDangNhap`, `MatKhau`.

- [ ] **Step 4: Update Home/Index and Error visible text**

Ensure landing page is fully Vietnamese with diacritics. Error page should say:

```text
Đã xảy ra lỗi
Vui lòng thử lại hoặc liên hệ quản trị hệ thống nếu lỗi tiếp tục xảy ra.
```

- [ ] **Step 5: Build views**

Run:
```bash
cd "D:/Hoc_Tap/Thuc_Tap/HospitalQualityDashboard" && powershell.exe -NoProfile -Command "& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU /p:MvcBuildViews=true /v:minimal"
```

Expected: build succeeds.

---

## Task 2: Việt hóa Admin Area views

**Files:**
- Modify: `HospitalQualityDashboard/Areas/Admin/Views/**/*.cshtml`

- [ ] **Step 1: Scan Admin Area views**

Run:
```bash
grep -RInE "Tong quan|Khoa/phong|Nhan vien|Chi so|Phan cong|Ky bao cao|Bao cao|Thong bao|Dang xuat|Trang chu|Admin workspace|Dieu huong|Ho so|Da |Vui long|Sua|Xoa|Khoa|Mo|Them|Gui|Nhap|Chi tiet|Export|Search|Filter|Status|Create|Edit|Delete" HospitalQualityDashboard/Areas/Admin/Views --include="*.cshtml"
```

Expected: output lines are reviewed one by one.

- [ ] **Step 2: Update Admin layout**

In `Areas/Admin/Views/Shared/_AdminLayout.cshtml`, translate visible text:

```text
Admin workspace -> Không gian quản trị
Dieu huong Admin -> Điều hướng quản trị
Tong quan -> Tổng quan
Khoa/phong -> Khoa/phòng
Nhan vien -> Nhân viên
Chi so -> Chỉ số
Phan cong -> Phân công
Ky bao cao -> Kỳ báo cáo
Bao cao -> Báo cáo
Thong bao -> Thông báo
Ho so -> Hồ sơ
Dang xuat -> Đăng xuất
Trang chu Admin -> Trang chủ quản trị
```

- [ ] **Step 3: Update Admin workflow views**

Translate visible text in:
- Assignment views
- Dashboard view
- Department views
- Employee views
- Indicator views
- Notification views
- Report views
- ReportingPeriod views

Keep route strings in `ActionLink`, `Url.Action`, `BeginForm` unchanged.

- [ ] **Step 4: Build views**

Run MSBuild with `MvcBuildViews=true` as in Task 1.

Expected: build succeeds.

---

## Task 3: Việt hóa User Area views

**Files:**
- Modify: `HospitalQualityDashboard/Areas/User/Views/**/*.cshtml`

- [ ] **Step 1: Scan User Area views**

Run:
```bash
grep -RInE "User workspace|Tong quan|Chi so|Bao cao|Thong bao|Dang xuat|Ho so|Trang chu|Nhap|Gui|Sua|Chi tiet|Vui long|Khong|Da |Export|Status|Search|Filter" HospitalQualityDashboard/Areas/User/Views --include="*.cshtml"
```

- [ ] **Step 2: Update User layout**

In `Areas/User/Views/Shared/_UserLayout.cshtml`, translate visible text:

```text
User workspace -> Không gian khoa/phòng
Dieu huong User -> Điều hướng người dùng
Tong quan -> Tổng quan
Chi so -> Chỉ số
Bao cao cua toi -> Báo cáo của tôi
Thong bao -> Thông báo
Ho so -> Hồ sơ
Dang xuat -> Đăng xuất
Trang chu User -> Trang chủ người dùng
```

- [ ] **Step 3: Update User workflow views**

Translate visible text in:
- Dashboard
- Indicator
- Notification
- Report

Keep route strings unchanged.

- [ ] **Step 4: Build views**

Run MSBuild with `MvcBuildViews=true`.

Expected: build succeeds.

---

## Task 4: Việt hóa controller user-facing messages

**Files:**
- Modify: `HospitalQualityDashboard/Controllers/*.cs`
- Modify: `HospitalQualityDashboard/Areas/Admin/Controllers/*.cs`
- Modify: `HospitalQualityDashboard/Areas/User/Controllers/*.cs`

- [ ] **Step 1: Scan target strings**

Run:
```bash
grep -RInE '"(Da |Vui long|Thieu|Ky bao cao|Ban khong|Approval workflow is disabled|Unauthorized|Ten dang nhap|Mat khau|Da chay|Da tao|Da mo|Da dong bo|Da phan cong|khong phai|khong dung|chua mo|khong phu hop)' HospitalQualityDashboard/Controllers HospitalQualityDashboard/Areas/Admin/Controllers HospitalQualityDashboard/Areas/User/Controllers --include="*.cs"
```

- [ ] **Step 2: Translate user-facing messages**

Use these exact replacements where appropriate:

```text
Da phan cong chi so thanh cong! -> Đã phân công chỉ số thành công!
Da dong bo {n} phan cong tu du lieu chi so. -> Đã đồng bộ {n} phân công từ dữ liệu chỉ số.
Vui long nhap tieu de va noi dung. -> Vui lòng nhập tiêu đề và nội dung.
Da chay kiem tra thong bao tu dong. -> Đã chạy kiểm tra thông báo tự động.
Approval workflow is disabled. -> Quy trình duyệt báo cáo hiện không được sử dụng.
Da mo {0} ky bao cao den ngay bat dau. -> Đã mở {0} kỳ báo cáo đến ngày bắt đầu.
Da tao {0} ky bao cao moi, bo qua {1} ky da ton tai, tu mo {2} ky den ngay bat dau. -> Đã tạo {0} kỳ báo cáo mới, bỏ qua {1} kỳ đã tồn tại, tự mở {2} kỳ đến ngày bắt đầu.
Ban khong co quyen xem chi tiet chi so nay. -> Bạn không có quyền xem chi tiết chỉ số này.
Thieu thong tin ky bao cao hoac chi so. -> Thiếu thông tin kỳ báo cáo hoặc chỉ số.
Ky bao cao chua mo hoac khong phu hop voi phan cong cua khoa/phong. -> Kỳ báo cáo chưa mở hoặc không phù hợp với phân công của khoa/phòng.
Tai khoan nay khong phai tai khoan Admin. -> Tài khoản này không phải tài khoản Admin.
Tai khoan nay khong phai tai khoan User. -> Tài khoản này không phải tài khoản User.
Ten dang nhap hoac mat khau khong dung. -> Tên đăng nhập hoặc mật khẩu không đúng.
Mat khau hien tai khong dung. -> Mật khẩu hiện tại không đúng.
Doi mat khau thanh cong. -> Đổi mật khẩu thành công.
```

Do not translate filenames, content types, route/action names, or DTO names.

- [ ] **Step 3: Build**

Run MSBuild Debug.

Expected: build succeeds.

---

## Task 5: Việt hóa tài liệu dự án chính

**Files:**
- Modify: `README.md`
- Modify: `HospitalQualityDashboard/AGENTS.md`
- Modify: `HospitalQualityDashboard/PROJECT_CONTEXT.md`
- Modify: `HospitalQualityDashboard/TAI_LIEU_NGHIEP_VU.md`
- Modify: `HospitalQualityDashboard/implementation-notes.md`

- [ ] **Step 1: Scan docs chính**

Run:
```bash
grep -RInE "\b(Build|Test|Development Commands|Commit|Pull Request|Guidelines|Implementation|Plan|Goal|Architecture|Tech Stack|Task|Expected|Run|Status|Root|Controller|Area|Debug|Verify|Passed|Failed|Done|Error|Success)\b" README.md HospitalQualityDashboard/AGENTS.md HospitalQualityDashboard/PROJECT_CONTEXT.md HospitalQualityDashboard/TAI_LIEU_NGHIEP_VU.md HospitalQualityDashboard/implementation-notes.md
```

- [ ] **Step 2: Rewrite docs in Vietnamese with diacritics**

Keep command snippets and technical names unchanged, but translate prose/headings. Examples:

```text
Build, Test, and Development Commands -> Lệnh build, kiểm thử và phát triển
Commit & Pull Request Guidelines -> Quy ước commit và pull request
Implementation Plan -> Kế hoạch triển khai
Goal -> Mục tiêu
Architecture -> Kiến trúc
Expected -> Kết quả mong đợi
Run -> Chạy lệnh
```

- [ ] **Step 3: Do not edit package docs or historical superpowers docs**

Verify:
```bash
git diff --name-only | grep -E 'packages/|docs/superpowers/.+2026-06-08-root|HospitalQualityDashboard/docs/superpowers/' && echo "Unexpected docs touched" || echo "Docs scope OK"
```

Expected: no unexpected package/historical superpowers docs touched.

---

## Task 6: Final verification and commit

**Files:**
- All files from Tasks 1-5

- [ ] **Step 1: Build with MVC views**

Run:
```bash
cd "D:/Hoc_Tap/Thuc_Tap/HospitalQualityDashboard" && powershell.exe -NoProfile -Command "& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU /p:MvcBuildViews=true /v:minimal"
```

Expected: build succeeds.

- [ ] **Step 2: Run verification scripts**

Run:
```bash
cd "D:/Hoc_Tap/Thuc_Tap/HospitalQualityDashboard" && powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "& '.\HospitalQualityDashboard\tools\VerifyAreasAndDtos.ps1'"
cd "D:/Hoc_Tap/Thuc_Tap/HospitalQualityDashboard" && powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "& '.\HospitalQualityDashboard\tools\VerifySecurityHardening.ps1'"
```

Expected:
```text
Areas and DTO verification passed.
Security hardening verification passed.
```

- [ ] **Step 3: Final grep for obvious misses**

Run:
```bash
grep -RInE "Admin workspace|User workspace|Tong quan|Khoa/phong|Nhan vien|Chi so|Phan cong|Ky bao cao|Bao cao|Thong bao|Dang xuat|Ho so|Vui long|Da chay|Da tao|Da mo|Thieu thong tin|Approval workflow is disabled|Your application|Your contact" HospitalQualityDashboard/Areas HospitalQualityDashboard/Views HospitalQualityDashboard/Controllers README.md HospitalQualityDashboard/*.md --include="*.cshtml" --include="*.cs" --include="*.md"
```

Expected: no user-facing misses. Route/action names such as `Admin`, `User`, `Dashboard`, `Report` may remain when they are technical route/controller identifiers.

- [ ] **Step 4: Commit**

Run:
```bash
git add -A
git commit -m "refactor: chuẩn hóa giao diện và tài liệu tiếng Việt có dấu

Chuẩn hóa text giao diện, thông báo người dùng và tài liệu chính
sang tiếng Việt có dấu. Giữ nguyên route/action/controller/schema và
identifier kỹ thuật.

Verification:
- MSBuild Debug với MvcBuildViews=true passed
- VerifyAreasAndDtos.ps1 passed
- VerifySecurityHardening.ps1 passed

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```
