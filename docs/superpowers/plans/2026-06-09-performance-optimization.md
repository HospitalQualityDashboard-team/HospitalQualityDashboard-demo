# Tối ưu tốc độ hệ thống — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Giảm thời gian load mỗi trang từ 3-5 giây xuống dưới 1 giây bằng cách loại bỏ các query dư thừa, thêm index, tối ưu bundle và thêm caching.

**Architecture:** Chia làm 2 phase riêng biệt. Phase 1 — Backend: loại bỏ session revalidation query mỗi request, loại bỏ `EnsureLoginThrottleColumns` dư thừa, thêm SQL index, cache danh mục ít thay đổi. Phase 2 — Frontend: dùng minified bundle trong Debug, nén bundle, thêm Cache-Control, giảm số lượng connection mở mỗi trang.

**Tech Stack:** ASP.NET MVC 4, .NET Framework 4.7.2, C#, SQL Server LocalDB, ADO.NET, Bootstrap, jQuery.

---

## File Map

### Phase 1 — Backend

| File | Thay đổi |
|---|---|
| `Controllers/PageController.cs` | Bỏ `GetAuthenticatedUser` mỗi request, chỉ validate session local |
| `Services/AuthService.cs` | Bỏ `EnsureLoginThrottleColumns` khỏi `GetAuthenticatedUser`, thêm static flag cho DDL |
| `Services/DbServiceBase.cs` | Thêm cache static cho danh mục ít thay đổi (CacheAside pattern) |
| `App_Data/Sql/007_PerformanceIndexes.sql` | Migration thêm index cho query hay dùng |

### Phase 2 — Frontend + Connection

| File | Thay đổi |
|---|---|
| `App_Start/BundleConfig.cs` | Đổi `ScriptBundle` thành Bundle với CDN fallback, dùng `.min` files |
| `Web.config` | Thêm `clientCache` cho static resources |
| `Services/DbServiceBase.cs` | Dùng một connection/shared connection? Giữ nguyên — mỗi query một connection là ổn cho LocalDB |

---

## Phase 1: Backend Optimization

### Task 1: Loại bỏ session revalidation query mỗi request

**Vấn đề:** `PageController.OnActionExecuting` và `RevalidateCurrentSession` gọi `AuthService.GetAuthenticatedUser(taiKhoanId)` — một SQL query JOIN 3 bảng — trên **mọi request đến controller kế thừa PageController**. Trong khi session vẫn còn hiệu lực, dữ liệu user không thay đổi giữa các request.

**Sửa:** Bỏ `RevalidateCurrentSession` khỏi `OnActionExecuting`. Chỉ kiểm tra session local (`IsAuthenticated` + `CurrentTaiKhoanId.HasValue`). Giữ `RevalidateCurrentSession` cho các action quan trọng (Profile, ChangePassword) nếu cần nhưng không gọi mỗi request.

**Files:**
- Modify: `Controllers/PageController.cs`

- [ ] **Step 1: Sửa `OnActionExecuting` bỏ revalidation query**

```csharp
protected override void OnActionExecuting(ActionExecutingContext filterContext)
{
    if (!SessionUserAccessor.IsAuthenticated(Session))
    {
        filterContext.Result = RedirectToAction("UserLogin", "Account", new { area = "" });
        return;
    }

    // Không gọi RevalidateCurrentSession ở đây — session vẫn valid trong timeout ASP.NET.
    // Thay vào đó, controller con tự gọi nếu cần kiểm tra active/lock.
    base.OnActionExecuting(filterContext);
}
```

- [ ] **Step 2: Giữ `RevalidateCurrentSession` và `ClearLoginSession` — chỉ gọi từ action cần thiết**

Giữ nguyên code 2 method này trong `PageController`, nhưng không gọi tự động trong `OnActionExecuting`. Ai cần active check thì tự gọi.

- [ ] **Step 3: Build kiểm tra**

Chạy:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add .
git commit -m "perf: loại bỏ session revalidation SQL query mỗi request

- OnActionExecuting không còn gọi GetAuthenticatedUser
- Giữ RevalidateCurrentSession cho action cần kiểm tra active/lock
- Request nhanh hơn ~50-200ms tùy độ trễ DB

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: Loại bỏ `EnsureLoginThrottleColumns` khỏi `GetAuthenticatedUser`

**Vấn đề:** `GetAuthenticatedUser` gọi `EnsureLoginThrottleColumns()` mỗi lần. Method này chạy `IF COL_LENGTH(...) ALTER TABLE ...` — DDL statement tốn thời gian, đặc biệt trên LocalDB mỗi lần mở connection mới. DDL này chỉ cần chạy một lần sau khi deploy.

**Sửa:** Kiểm tra column bằng static flag `_loginThrottleEnsured` để chỉ chạy DDL đúng một lần trong vòng đời AppDomain. Đưa `EnsureLoginThrottleColumns` ra khỏi `GetAuthenticatedUser`.

**Files:**
- Modify: `Services/AuthService.cs`

- [ ] **Step 1: Thêm static flag cho `EnsureLoginThrottleColumns`**

```csharp
private static bool _loginThrottleEnsured;
private static readonly object _loginThrottleLock = new object();

private void EnsureLoginThrottleColumns()
{
    if (_loginThrottleEnsured) return;
    lock (_loginThrottleLock)
    {
        if (_loginThrottleEnsured) return;
        ExecuteNonQuery(@"
IF COL_LENGTH('dbo.TaiKhoan', 'FailedLoginCount') IS NULL
BEGIN
    ALTER TABLE dbo.TaiKhoan ADD FailedLoginCount INT NOT NULL CONSTRAINT DF_TaiKhoan_FailedLoginCount_Runtime DEFAULT (0);
END;
IF COL_LENGTH('dbo.TaiKhoan', 'LockoutUntil') IS NULL
BEGIN
    ALTER TABLE dbo.TaiKhoan ADD LockoutUntil DATETIME NULL;
END;");
        _loginThrottleEnsured = true;
    }
}
```

- [ ] **Step 2: Bỏ `EnsureLoginThrottleColumns` khỏi đầu `GetAuthenticatedUser`**

Xóa dòng:

```csharp
EnsureLoginThrottleColumns();
```

khỏi method `GetAuthenticatedUser(int taiKhoanId)`. Giữ nguyên trong `Authenticate`, `RecordFailedLogin`, `ResetFailedLogin` — chúng có thể được gọi trước khi flag được set.

- [ ] **Step 3: Build kiểm tra**

Chạy:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add .
git commit -m "perf: EnsureLoginThrottleColumns chỉ chạy 1 lần trong AppDomain

- Thêm static flag _loginThrottleEnsured + lock
- Bỏ EnsureLoginThrottleColumns khỏi GetAuthenticatedUser (gọi nhiều nhất)
- Tránh DDL check trên mỗi request

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3: Thêm SQL index cho query thường dùng

**Vấn đề:** Các query `LIKE %keyword%`, `ORDER BY`, `JOIN` trên bảng lớn có thể gây table scan. Cần index cho các cột thường xuất hiện trong WHERE/JOIN/ORDER BY.

**Phân tích index cần tạo (dựa trên code hiện tại):**

1. `NhanVien.KhoaPhongId` — Join với `KhoaPhong.KhoaPhongId` (EmployeeService.GetAll)
2. `TaiKhoan.NhanVienId` — Join với NhanVien (AuthService, EmployeeService)
3. `TaiKhoan.KhoaPhongId` — Join với KhoaPhong (AuthService)
4. `PhanCongChiSo.KhoaPhongId` — Filter theo khoa
5. `PhanCongChiSo.ChiSoChatLuongId` — Join với ChiSoChatLuong
6. `BaoCao.KhoaPhongId + KyBaoCaoId` — Composite cho dashboard missing reports

**Files:**
- Create: `HospitalQualityDashboard/App_Data/Sql/007_PerformanceIndexes.sql`

- [ ] **Step 1: Tạo migration file**

```sql
-- 007: Thêm index cho các query thường dùng để giảm table scan

-- Index cho join employee → department
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_NhanVien_KhoaPhongId')
    CREATE NONCLUSTERED INDEX IX_NhanVien_KhoaPhongId ON dbo.NhanVien(KhoaPhongId) INCLUDE(MaNhanVien, HoTen, DangHoatDong);

-- Index cho join account → employee, account → department
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TaiKhoan_NhanVienId')
    CREATE NONCLUSTERED INDEX IX_TaiKhoan_NhanVienId ON dbo.TaiKhoan(NhanVienId) INCLUDE(TenDangNhap, LoaiTaiKhoan, KhoaPhongId, DangHoatDong);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TaiKhoan_KhoaPhongId')
    CREATE NONCLUSTERED INDEX IX_TaiKhoan_KhoaPhongId ON dbo.TaiKhoan(KhoaPhongId) WHERE KhoaPhongId IS NOT NULL;

-- Index cho phân công chỉ số
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PhanCongChiSo_KhoaPhongId')
    CREATE NONCLUSTERED INDEX IX_PhanCongChiSo_KhoaPhongId ON dbo.PhanCongChiSo(KhoaPhongId) INCLUDE(ChiSoChatLuongId, DangHoatDong);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PhanCongChiSo_ChiSoChatLuongId')
    CREATE NONCLUSTERED INDEX IX_PhanCongChiSo_ChiSoChatLuongId ON dbo.PhanCongChiSo(ChiSoChatLuongId) INCLUDE(KhoaPhongId, DangHoatDong);

-- Index composite cho dashboard báo cáo thiếu
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BaoCao_KhoaPhong_Ky')
    CREATE NONCLUSTERED INDEX IX_BaoCao_KhoaPhong_Ky ON dbo.BaoCao(KhoaPhongId, KyBaoCaoId) INCLUDE(ChiSoChatLuongId, TrangThai);

-- Index cho tần suất chỉ số
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChiSoTanSuatBaoCao_ChiSoChatLuongId')
    CREATE NONCLUSTERED INDEX IX_ChiSoTanSuatBaoCao_ChiSoChatLuongId ON dbo. ChiSoTanSuatBaoCao(ChiSoChatLuongId);

-- Index cho thông báo người nhận
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ThongBaoNguoiNhan_TaiKhoanId')
    CREATE NONCLUSTERED INDEX IX_ThongBaoNguoiNhan_TaiKhoanId ON dbo.ThongBaoNguoiNhan(TaiKhoanId) INCLUDE(DaDoc);
```

- [ ] **Step 2: Chạy script verify**

Kiểm tra script tồn tại và không có lỗi cú pháp:

```powershell
$dbName = "HospitalQualityDashboard"
$sql = Get-Content ".\HospitalQualityDashboard\App_Data\Sql\007_PerformanceIndexes.sql" -Raw
Write-Host "Script OK, length: $($sql.Length) chars"
```

- [ ] **Step 3: Build**

Chạy:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add .
git commit -m "perf: thêm SQL index cho query thường dùng

- Index cho NhanVien.KhoaPhongId, TaiKhoan.NhanVienId, TaiKhoan.KhoaPhongId
- Index cho PhanCongChiSo, BaoCao composite
- Index cho ChiSoTanSuatBaoCao, ThongBaoNguoiNhan
- Migration 007_PerformanceIndexes.sql

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 4: Cache tĩnh cho danh mục ít thay đổi

**Vấn đề:** Danh sách khoa/phòng (`GetOptions`), loại tần suất, enum text được query/format lại mỗi lần load trang phân công, chỉ số, báo cáo...

**Sửa:** Thêm `LazyCache` pattern trong `DbServiceBase` hoặc trong service cụ thể. Dùng `MemoryCache` (System.Runtime.Caching) có sẵn trong .NET Framework với expiration 5 phút.

**Files:**
- Modify: `Services/ManagementServices.cs` — `GetOptions()` cache 5 phút
- Modify: `Services/IndicatorPeriodServices.cs` — cache danh sách frequency options

- [ ] **Step 1: Thêm helper cache trong `DbServiceBase`**

Thêm method helper:

```csharp
using System.Runtime.Caching;

// Lớp cơ sở cache cho dữ liệu danh mục ít thay đổi
protected static class LocalCache
{
    private static readonly MemoryCache Cache = MemoryCache.Default;

    public static T GetOrAdd<T>(string key, Func<T> factory, int seconds = 300)
    {
        var value = Cache.Get(key);
        if (value != null) return (T)value;

        value = factory();
        Cache.Set(key, value, DateTimeOffset.Now.AddSeconds(seconds));
        return (T)value;
    }

    public static void Invalidate(string key)
    {
        Cache.Remove(key);
    }
}
```

- [ ] **Step 2: Cache `DepartmentService.GetOptions()`**

```csharp
public IList<SelectListItem> GetOptions()
{
    return LocalCache.GetOrAdd("DepartmentOptions", () =>
        GetAll(null, false)
            .Select(x => new SelectListItem { Value = x.KhoaPhongId.ToString(), Text = x.TenKhoaPhong })
            .ToList()
    );
}
```

- [ ] **Step 3: Build kiểm tra**

Chạy:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add .
git commit -m "perf: thêm MemoryCache cho danh mục ít thay đổi

- LocalCache helper trong DbServiceBase với expiration 5 phút
- Cache DepartmentService.GetOptions — giảm query danh sách khoa/phòng
- Tránh gọi DB mỗi lần render dropdown

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Phase 2: Frontend Optimization

### Task 5: Bundle — dùng minified files trong Debug và Production

**Vấn đề:** `BundleConfig.cs` dùng `{version}.js` và `bootstrap.js` (không minified) ngay cả khi build Debug. File non-minified lớn gấp 3 lần so với minified.

**Sửa:** Dùng `.min` files trong cả Debug và Production. BundleTransformationAttribute mặc định `js` không minify trong Debug — nhưng nếu dùng file `.min` trực tiếp thì không cần minify. Đổi bundle thành `Bundle` thay vì `ScriptBundle`.

**Files:**
- Modify: `App_Start/BundleConfig.cs`

- [ ] **Step 1: Sửa BundleConfig để dùng file .min**

```csharp
public static void RegisterBundles(BundleCollection bundles)
{
    bundles.UseCdn = true;

    // jQuery — dùng file .min
    bundles.Add(new ScriptBundle("~/bundles/jquery", "https://code.jquery.com/jquery-3.7.0.min.js").Include(
                "~/Scripts/jquery-3.7.0.min.js"));

    bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                "~/Scripts/jquery.validate.min.js",
                "~/Scripts/jquery.validate.unobtrusive.min.js"));

    // Modernizr — vẫn giữ debug-friendly
    bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                "~/Scripts/modernizr-2.8.3.js"));

    // Bootstrap — dùng file .min
    bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
              "~/Scripts/bootstrap.bundle.min.js"));

    // CSS — dùng file .min
    bundles.Add(new StyleBundle("~/Content/css").Include(
              "~/Content/bootstrap.min.css",
              "~/Content/site.css"));
}
```

- [ ] **Step 2: Bật BundleTable.EnableOptimizations trong Web.config transform hoặc BundleConfig**

Thêm dòng vào cuối `RegisterBundles`:

```csharp
#if !DEBUG
    BundleTable.EnableOptimizations = true;
#endif
```

Trong Debug, vẫn dùng file `.min` nhưng bundling gộp thành 1 request thay vì nhiều request.

- [ ] **Step 3: Build kiểm tra**

Chạy:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add .
git commit -m "perf: bundle dùng file .min và gộp CSS/JS

- ScriptBundle dùng jQuery, Bootstrap minified
- CSS dùng bootstrap.min.css
- Gộp bundle thành ít request hơn

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 6: Cache-Control cho static resources

**Vấn đề:** Các file CSS, JS, ảnh không có Cache-Control header trong Web.config khiến browser phải revalidate mỗi lần.

**Sửa:** Thêm `clientCache` trong `Web.config` cho static resources với cache 7 ngày.

**Files:**
- Modify: `HospitalQualityDashboard/Web.config`

- [ ] **Step 1: Thêm `clientCache` trong `<system.webServer>`**

```xml
<system.webServer>
  <staticContent>
    <clientCache cacheControlMode="UseMaxAge" cacheControlMaxAge="7.00:00:00" />
  </staticContent>
  <!-- ... các phần còn lại ... -->
</system.webServer>
```

Tìm dòng `<system.webServer>` hiện tại và thêm `<staticContent>` bên trong.

- [ ] **Step 2: Build kiểm tra**

Chạy:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU
```

Expected: Build succeeded (Web.config change không ảnh hưởng build).

- [ ] **Step 3: Commit**

```bash
git add .
git commit -m "perf: thêm Cache-Control 7 ngày cho static resources

- clientCache trong Web.config
- Giảm request revalidation từ browser

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 7: Verification tổng thể

**Files:**
- Tất cả file từ Task 1-6

- [ ] **Step 1: Full build + view compile**

Chạy:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU /p:MvcBuildViews=true
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 2: Chạy verification scripts**

```powershell
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyAreasAndDtos.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifySecurityHardening.ps1
```

Expected: Both pass.

- [ ] **Step 3: Commit cuối**

```bash
git add .
git commit -m "perf: verification và hoàn thiện tối ưu tốc độ

- Build + MvcBuildViews=true passed
- VerifyAreasAndDtos, VerifySecurityHardening passed
- Tổng hợp các thay đổi backend + frontend optimization

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Tổng kết tác động dự kiến

| Task | Tác động | Giảm thời gian dự kiến |
|---|---|---|
| 1. Bỏ session revalidation | -50~200ms mỗi request | ~100ms |
| 2. DDL chỉ chạy 1 lần | -20~100ms mỗi request đầu | ~50ms |
| 3. SQL index | -100~500ms cho trang có JOIN/WHERE nhiều | ~200ms |
| 4. MemoryCache | -20~100ms mỗi lần render dropdown | ~50ms |
| 5. Bundle min+gộp | từ 6 request CSS/JS → 2 request | ~500ms |
| 6. Cache-Control | Không revalidate static file | ~200ms |

**Dự kiến tổng: từ 3-5 giây xuống 0.5-1 giây mỗi trang.**
