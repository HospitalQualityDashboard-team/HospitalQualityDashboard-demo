# Root Controller Thin Redirect Cleanup — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert legacy root business-logic controllers to thin redirect wrappers that forward requests to Area controllers, performing the cleanup in 4 phases with verification gates.

**Architecture:**
- Root controllers keep only session guard (from `PageController`) and `RedirectToAction` to `area = "Admin"` or `area = "User"`.
- Area controllers already hold full business logic via shared Services + DTOs.
- No service behavior changes — only root controller code changes, view removals, doc updates.
- Each phase must pass MSBuild + verification scripts before proceeding.

**Tech Stack:** ASP.NET MVC 4, C#, Razor, ADO.NET

---

## File Map

### Files modified (per phase)

**Phase 1 — Admin-only:**
- `Controllers/AssignmentController.cs` — full rewrite to redirect-only
- `Controllers/DepartmentController.cs` — full rewrite to redirect-only
- `Controllers/EmployeeController.cs` — full rewrite to redirect-only
- `Controllers/ReportingPeriodController.cs` — full rewrite to redirect-only

**Phase 2 — Dual-role read:**
- `Controllers/DashboardController.cs` — rewrite to role-switch redirect
- `Controllers/IndicatorController.cs` — rewrite to role-switch redirect

**Phase 3 — Dual-role write:**
- `Controllers/ReportController.cs` — rewrite to role-switch redirect
- `Controllers/NotificationController.cs` — rewrite to role-switch redirect
- `Controllers/ExportController.cs` — rewrite to role-switch redirect

**Phase 4 — Root views + docs:**
- Delete `Views/Assignment/`
- Delete `Views/Department/`
- Delete `Views/Employee/`
- Delete `Views/ReportingPeriod/`
- Delete `Views/Report/`
- Delete `Views/Dashboard/`
- Delete `Views/Indicator/`
- Delete `Views/Notification/`
- Modify `PROJECT_CONTEXT.md` — update architecture section
- Modify `implementation-notes.md` — add cleanup entry

### Files NOT modified
- `Controllers/AccountController.cs` — still needed for login/logout
- `Controllers/HomeController.cs` — landing pages
- `Controllers/PageController.cs` — base class, stays
- All `Areas/` controllers — already hold the logic
- All `Services/` — unchanged
- All `Models/` — unchanged
- `Views/Shared/_Layout.cshtml` — keep, still used by Account/Home
- `Views/Account/` and `Views/Home/` — keep

---

## Phase 1: Admin-Only Redirect Controllers

### Task 1: Rewrite `Controllers/AssignmentController.cs`

**Files:**
- Modify: `Controllers/AssignmentController.cs` — full replace

- [ ] **Step 1: Replace file with redirect-only version**

```csharp
// Muc dich: chuyen huong Admin sang Area de quan ly phan cong chi so chat luong.
using System.Web.Mvc;

namespace HospitalQualityDashboard.Controllers
{
    public class AssignmentController : PageController
    {
        public ActionResult Index(
            int? khoaPhongId,
            int? chiSoId,
            string trangThai,
            string trangThaiPhanCong,
            string search,
            string viewMode,
            int page = 1)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Index", "Assignment", new
            {
                area = "Admin",
                khoaPhongId,
                chiSoId,
                trangThai,
                trangThaiPhanCong,
                search,
                viewMode,
                page
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Assign(AssignmentViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Assign", "Assignment", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Deactivate(int id, string viewMode, int? khoaPhongId, int? chiSoId, string trangThai, string search, int page = 1)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Deactivate", "Assignment", new
            {
                area = "Admin",
                id,
                viewMode,
                khoaPhongId,
                chiSoId,
                trangThai,
                search,
                page
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Activate(int id, string viewMode, int? khoaPhongId, int? chiSoId, string trangThai, string search, int page = 1)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Activate", "Assignment", new
            {
                area = "Admin",
                id,
                viewMode,
                khoaPhongId,
                chiSoId,
                trangThai,
                search,
                page
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SyncFromIndicators(string viewMode)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("SyncFromIndicators", "Assignment", new { area = "Admin", viewMode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, string viewMode, int? khoaPhongId, int? chiSoId, string trangThai, string search, int page = 1)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Delete", "Assignment", new
            {
                area = "Admin",
                id,
                viewMode,
                khoaPhongId,
                chiSoId,
                trangThai,
                search,
                page
            });
        }

        [HttpPost]
        public JsonResult Preview(int[] departmentIds, int[] indicatorIds)
        {
            var admin = RequireAdmin();
            if (admin != null) return Json(new { error = "Unauthorized" });
            return RedirectToAction("Preview", "Assignment", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BulkDeactivate(int[] ids, string viewMode)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("BulkDeactivate", "Assignment", new { area = "Admin", ids, viewMode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BulkActivate(int[] ids, string viewMode)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("BulkActivate", "Assignment", new { area = "Admin", ids, viewMode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BulkDelete(int[] ids, string viewMode)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("BulkDelete", "Assignment", new { area = "Admin", ids, viewMode });
        }
    }
}
```

- [ ] **Step 2: Build to verify compilation**

Run:
```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU
```
Expected: Build succeeded, 0 errors, 0 warnings.

### Task 2: Rewrite `Controllers/DepartmentController.cs`

**Files:**
- Modify: `Controllers/DepartmentController.cs` — full replace

- [ ] **Step 1: Replace with redirect-only version**

```csharp
// Muc dich: chuyen huong Admin sang Area de quan ly khoa/phong.
using System.Web.Mvc;

namespace HospitalQualityDashboard.Controllers
{
    public class DepartmentController : PageController
    {
        public ActionResult Index(string search)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Index", "Department", new { area = "Admin", search });
        }

        public ActionResult Create()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Create", "Department", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(KhoaPhongViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Create", "Department", new { area = "Admin" });
        }

        public ActionResult Edit(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Edit", "Department", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(KhoaPhongViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Edit", "Department", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Lock(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Lock", "Department", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Unlock(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Unlock", "Department", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Delete", "Department", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Import(ImportFileViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Import", "Department", new { area = "Admin" });
        }
    }
}
```

- [ ] **Step 2: Build to verify compilation**

Run:
```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU
```
Expected: Build succeeded.

### Task 3: Rewrite `Controllers/EmployeeController.cs`

**Files:**
- Modify: `Controllers/EmployeeController.cs` — full replace

- [ ] **Step 1: Replace with redirect-only version**

```csharp
// Muc dich: chuyen huong Admin sang Area de quan ly nhan vien.
using System.Web.Mvc;

namespace HospitalQualityDashboard.Controllers
{
    public class EmployeeController : PageController
    {
        public ActionResult Index(int? khoaPhongId)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Index", "Employee", new { area = "Admin", khoaPhongId });
        }

        public ActionResult Create()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Create", "Employee", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(NhanVienViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Create", "Employee", new { area = "Admin" });
        }

        public ActionResult Edit(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Edit", "Employee", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(NhanVienViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Edit", "Employee", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Lock(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Lock", "Employee", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Unlock(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Unlock", "Employee", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Delete", "Employee", new { area = "Admin", id });
        }

        public ActionResult CreateAccount(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("CreateAccount", "Employee", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateAccount(CreateUserAccountViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("CreateAccount", "Employee", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Import(ImportFileViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Import", "Employee", new { area = "Admin" });
        }
    }
}
```

- [ ] **Step 2: Build to verify compilation**

Run:
```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU
```
Expected: Build succeeded.

### Task 4: Rewrite `Controllers/ReportingPeriodController.cs`

**Files:**
- Modify: `Controllers/ReportingPeriodController.cs` — full replace

- [ ] **Step 1: Replace with redirect-only version**

```csharp
// Muc dich: chuyen huong Admin sang Area de quan ly ky bao cao.
using System.Web.Mvc;

namespace HospitalQualityDashboard.Controllers
{
    public class ReportingPeriodController : PageController
    {
        public ActionResult Index()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Index", "ReportingPeriod", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OpenDuePeriods()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("OpenDuePeriods", "ReportingPeriod", new { area = "Admin" });
        }

        public ActionResult GenerateSchedule()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("GenerateSchedule", "ReportingPeriod", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PreviewSchedule(ReportingPeriodScheduleRequestViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("PreviewSchedule", "ReportingPeriod", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateSchedule(ReportingPeriodScheduleRequestViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("CreateSchedule", "ReportingPeriod", new { area = "Admin" });
        }

        public ActionResult Create()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Create", "ReportingPeriod", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(KyBaoCaoViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Create", "ReportingPeriod", new { area = "Admin" });
        }

        public ActionResult Edit(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Edit", "ReportingPeriod", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(KyBaoCaoViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Edit", "ReportingPeriod", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Open(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Open", "ReportingPeriod", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Lock(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Lock", "ReportingPeriod", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Delete", "ReportingPeriod", new { area = "Admin", id });
        }
    }
}
```

- [ ] **Step 2: Build to verify compilation**

Run:
```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU
```
Expected: Build succeeded.

### Task 5: Phase 1 integration verification

- [ ] **Step 1: Build with MVC views**

Run:
```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU /p:MvcBuildViews=true
```
Expected: Build succeeded, 0 errors, 0 warnings.

- [ ] **Step 2: Run verification scripts**

Run:
```powershell
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyAreasAndDtos.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifySecurityHardening.ps1
```
Expected: Both scripts pass.

- [ ] **Step 3: Git commit Phase 1**

```bash
git add .
git commit -m "refactor: convert Admin-only root controllers to thin redirect wrappers

Convert Assignment, Department, Employee, and ReportingPeriod
root controllers to redirect-only wrappers that forward all actions
to their Admin Area equivalents.

Phase 1 of the root controller cleanup plan.
No service behavior changes.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Phase 2: Dual-Role Read-Only Redirect Controllers

### Task 6: Rewrite `Controllers/DashboardController.cs`

**Files:**
- Modify: `Controllers/DashboardController.cs` — full replace

- [ ] **Step 1: Replace with role-switch redirect version**

```csharp
// Muc dich: dieu huong dashboard theo vai tro Admin/User sang Area tuong ung.
using System.Web.Mvc;

namespace HospitalQualityDashboard.Controllers
{
    public class DashboardController : PageController
    {
        public ActionResult Index()
        {
            if (IsAdmin)
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            else
            {
                return RedirectToAction("Index", "Dashboard", new { area = "User" });
            }
        }
    }
}
```

- [ ] **Step 2: Build to verify compilation**

Run:
```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU
```
Expected: Build succeeded.

### Task 7: Rewrite `Controllers/IndicatorController.cs`

**Files:**
- Modify: `Controllers/IndicatorController.cs` — full replace

- [ ] **Step 1: Replace with role-switch redirect version**

```csharp
// Muc dich: dieu huong qua ly chi so chat luong theo vai tro sang Area tuong ung.
using System.Web.Mvc;

namespace HospitalQualityDashboard.Controllers
{
    public class IndicatorController : PageController
    {
        public ActionResult Index()
        {
            if (IsAdmin)
            {
                return RedirectToAction("Index", "Indicator", new { area = "Admin" });
            }
            else
            {
                return RedirectToAction("Index", "Indicator", new { area = "User" });
            }
        }

        public ActionResult Details(int id)
        {
            if (IsAdmin)
            {
                return RedirectToAction("Details", "Indicator", new { area = "Admin", id });
            }
            else
            {
                return RedirectToAction("Details", "Indicator", new { area = "User", id });
            }
        }

        public ActionResult Create()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Create", "Indicator", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ChiSoViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Create", "Indicator", new { area = "Admin" });
        }

        public ActionResult Edit(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Edit", "Indicator", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ChiSoViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Edit", "Indicator", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Lock(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Lock", "Indicator", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Unlock(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Unlock", "Indicator", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Delete", "Indicator", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Import(ImportFileViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Import", "Indicator", new { area = "Admin" });
        }
    }
}
```

- [ ] **Step 2: Build to verify compilation**

Run:
```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU
```
Expected: Build succeeded.

### Task 8: Phase 2 integration verification

- [ ] **Step 1: Build with MVC views**

Run:
```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU /p:MvcBuildViews=true
```
Expected: Build succeeded.

- [ ] **Step 2: Run verification scripts**

Run:
```powershell
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyAreasAndDtos.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifySecurityHardening.ps1
```
Expected: Both scripts pass.

- [ ] **Step 3: Git commit Phase 2**

```bash
git add .
git commit -m "refactor: convert Dashboard and Indicator root controllers to redirect wrappers

Route to Admin or User Area based on the current role.
Admin-only actions still use RequireAdmin before redirect.

Phase 2 of the root controller cleanup plan.
No service behavior changes.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Phase 3: Dual-Role Write/Export Redirect Controllers

### Task 9: Rewrite `Controllers/ExportController.cs`

**Files:**
- Modify: `Controllers/ExportController.cs` — full replace

- [ ] **Step 1: Replace with role-switch redirect version**

```csharp
// Muc dich: dieu huong xuat du lieu theo vai tro sang Area tuong ung.
using System.Web.Mvc;

namespace HospitalQualityDashboard.Controllers
{
    public class ExportController : PageController
    {
        public ActionResult Departments()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Departments", "Export", new { area = "Admin" });
        }

        public ActionResult Employees(int? khoaPhongId)
        {
            if (IsAdmin)
            {
                return RedirectToAction("Employees", "Export", new { area = "Admin", khoaPhongId });
            }
            else
            {
                return new HttpUnauthorizedResult();
            }
        }

        public ActionResult Indicators()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Indicators", "Export", new { area = "Admin" });
        }

        public ActionResult Assignments(
            int? khoaPhongId,
            int? chiSoId,
            string trangThai,
            string trangThaiPhanCong,
            string search,
            string[] columns)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Assignments", "Export", new
            {
                area = "Admin",
                khoaPhongId,
                chiSoId,
                trangThai,
                trangThaiPhanCong,
                search,
                columns
            });
        }

        public ActionResult Reports(int? kyBaoCaoId, int? khoaPhongId, int? chiSoChatLuongId)
        {
            if (IsAdmin)
            {
                return RedirectToAction("Reports", "Export", new { area = "Admin", kyBaoCaoId, khoaPhongId, chiSoChatLuongId });
            }
            else
            {
                return RedirectToAction("Reports", "Export", new { area = "User", kyBaoCaoId, chiSoChatLuongId });
            }
        }
    }
}
```

- [ ] **Step 2: Build to verify compilation**

Run:
```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU
```
Expected: Build succeeded.

### Task 10: Rewrite `Controllers/ReportController.cs`

**Files:**
- Modify: `Controllers/ReportController.cs` — full replace

- [ ] **Step 1: Replace with role-switch redirect version**

```csharp
// Muc dich: dieu huong qua trinh bao cao theo vai tro sang Area tuong ung.
using System.Web.Mvc;

namespace HospitalQualityDashboard.Controllers
{
    public class ReportController : PageController
    {
        public ActionResult Index(int? kyBaoCaoId, int? khoaPhongId, int? chiSoChatLuongId)
        {
            if (IsAdmin)
            {
                return RedirectToAction("Index", "Report", new { area = "Admin", kyBaoCaoId, khoaPhongId, chiSoChatLuongId });
            }
            else
            {
                return RedirectToAction("Index", "Report", new { area = "User", kyBaoCaoId, chiSoChatLuongId });
            }
        }

        public ActionResult Nhap(int kyBaoCaoId)
        {
            return RedirectToAction("Nhap", "Report", new { area = "User", kyBaoCaoId });
        }

        public ActionResult Edit(int? id, int? kyBaoCaoId, int? khoaPhongId, int? chiSoChatLuongId)
        {
            if (IsAdmin)
            {
                return RedirectToAction("Edit", "Report", new { area = "Admin", id });
            }
            else
            {
                return RedirectToAction("Edit", "Report", new { area = "User", id, kyBaoCaoId, chiSoChatLuongId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ReportEntryViewModel model)
        {
            return RedirectToAction("Edit", "Report", new { area = "User" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Submit(int id)
        {
            return RedirectToAction("Submit", "Report", new { area = "User", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(int id)
        {
            // Approval workflow is disabled — return 410 Gone
            return new HttpStatusCodeResult(410, "Approval workflow is disabled.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reject(int id, string yKienPhanHoi)
        {
            return new HttpStatusCodeResult(410, "Approval workflow is disabled.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Lock(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Lock", "Report", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Delete", "Report", new { area = "Admin", id });
        }
    }
}
```

- [ ] **Step 2: Build to verify compilation**

Run:
```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU
```
Expected: Build succeeded.

### Task 11: Rewrite `Controllers/NotificationController.cs`

**Files:**
- Modify: `Controllers/NotificationController.cs` — full replace

- [ ] **Step 1: Replace with role-switch redirect version**

```csharp
// Muc dich: dieu huong thong bao theo vai tro sang Area tuong ung.
using System.Web.Mvc;

namespace HospitalQualityDashboard.Controllers
{
    public class NotificationController : PageController
    {
        public ActionResult Index()
        {
            if (IsAdmin)
            {
                return RedirectToAction("Index", "Notification", new { area = "Admin" });
            }
            else
            {
                return RedirectToAction("Index", "Notification", new { area = "User" });
            }
        }

        public ActionResult Details(int id)
        {
            if (IsAdmin)
            {
                return RedirectToAction("Details", "Notification", new { area = "Admin", id });
            }
            else
            {
                return RedirectToAction("Details", "Notification", new { area = "User", id });
            }
        }

        public ActionResult Create()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Create", "Notification", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(NotificationViewModel model)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Create", "Notification", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkAsRead(int id)
        {
            if (IsAdmin)
            {
                return RedirectToAction("MarkAsRead", "Notification", new { area = "Admin", id });
            }
            else
            {
                return RedirectToAction("MarkAsRead", "Notification", new { area = "User", id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkDetailAsRead(int id)
        {
            if (IsAdmin)
            {
                return RedirectToAction("MarkDetailAsRead", "Notification", new { area = "Admin", id });
            }
            else
            {
                return RedirectToAction("MarkDetailAsRead", "Notification", new { area = "User", id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OpenDuePeriodsAndRunAutomation()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("OpenDuePeriodsAndRunAutomation", "Notification", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RunAutomation()
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("RunAutomation", "Notification", new { area = "Admin" });
        }
    }
}
```

- [ ] **Step 2: Build to verify compilation**

Run:
```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU
```
Expected: Build succeeded.

### Task 12: Phase 3 integration verification

- [ ] **Step 1: Build with MVC views**

Run:
```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU /p:MvcBuildViews=true
```
Expected: Build succeeded, 0 errors, 0 warnings.

- [ ] **Step 2: Run all verification scripts**

Run:
```powershell
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyAreasAndDtos.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifySecurityHardening.ps1
```
Expected: Both scripts pass.

- [ ] **Step 3: Git commit Phase 3**

```bash
git add .
git commit -m "refactor: convert Report, Notification, Export root controllers to redirect wrappers

Route to Admin or User Area based on the current role.
Admin-only actions use RequireAdmin before redirect.
Approve/Reject return 410 (unchanged behavior).

Phase 3 of the root controller cleanup plan.
No service behavior changes.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Phase 4: Root View Cleanup and Documentation

### Task 13: Remove orphaned root Views

**Files:**
- Delete: `Views/Assignment/` directory (Index.cshtml, _DepartmentView.cshtml, _IndicatorView.cshtml, _TableView.cshtml)
- Delete: `Views/Department/` directory (Index.cshtml, Edit.cshtml)
- Delete: `Views/Employee/` directory (Index.cshtml, Edit.cshtml, CreateAccount.cshtml)
- Delete: `Views/ReportingPeriod/` directory (Index.cshtml, Edit.cshtml, GenerateSchedule.cshtml)
- Delete: `Views/Report/` directory (Index.cshtml, Edit.cshtml, Nhap.cshtml)
- Delete: `Views/Dashboard/` directory (Index.cshtml)
- Delete: `Views/Indicator/` directory (Index.cshtml, Edit.cshtml, Details.cshtml)
- Delete: `Views/Notification/` directory (Index.cshtml, Details.cshtml, Create.cshtml)

> **Note:** Only delete views whose corresponding root controller no longer calls `View()`. Views that are still referenced by AccountController, HomeController, or Error pages must be kept.

- [ ] **Step 1: Remove root view directories**

Run:
```powershell
Remove-Item -Recurse -Force .\HospitalQualityDashboard\Views\Assignment
Remove-Item -Recurse -Force .\HospitalQualityDashboard\Views\Department
Remove-Item -Recurse -Force .\HospitalQualityDashboard\Views\Employee
Remove-Item -Recurse -Force .\HospitalQualityDashboard\Views\ReportingPeriod
Remove-Item -Recurse -Force .\HospitalQualityDashboard\Views\Report
Remove-Item -Recurse -Force .\HospitalQualityDashboard\Views\Dashboard
Remove-Item -Recurse -Force .\HospitalQualityDashboard\Views\Indicator
Remove-Item -Recurse -Force .\HospitalQualityDashboard\Views\Notification
```
Expected: All 8 directories removed.

- [ ] **Step 2: Verify no remaining references to these view paths**

Run:
```powershell
# Check no root controllers still call View() pointing to these views
$controllers = Get-ChildItem .\HospitalQualityDashboard\Controllers\*.cs
foreach ($c in $controllers) {
    $content = Get-Content $c -Raw
    if ($content -match 'View\("(Assignment|Department|Employee|ReportingPeriod|Dashboard|Indicator|Notification|Report)')
    {
        Write-Host "WARNING: $($c.Name) still calls View('...')"
    }
}
Write-Host "Check complete."
```
Expected: No warnings. All root views now have their corresponding controllers using `RedirectToAction` instead of `View()`.

- [ ] **Step 3: Clean .csproj — remove `<Content Include=...>` entries for deleted root views**

Run:
```powershell
$csprojPath = ".\HospitalQualityDashboard\HospitalQualityDashboard.csproj"
$csproj = Get-Content $csprojPath
$removed = 0; $keptLines = @()
$patterns = @(
    'Views\\Assignment\\',
    'Views\\Department\\', 
    'Views\\Employee\\',
    'Views\\ReportingPeriod\\',
    'Views\\Report\\',
    'Views\\Dashboard\\',
    'Views\\Indicator\\',
    'Views\\Notification\\'
)
foreach ($line in $csproj) {
    $matched = $false
    foreach ($p in $patterns) {
        if ($line -match $p) { $matched = $true; $removed++; break }
    }
    if (-not $matched) { $keptLines += $line }
}
$keptLines | Set-Content $csprojPath
Write-Host "Removed $removed Content item(s) from csproj."
```
Expected: All root view Content lines removed. Build will not fail on missing files.

- [ ] **Step 4: Build with MVC views to confirm**

Run:
```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU /p:MvcBuildViews=true
```
Expected: Build succeeded, 0 errors, 0 warnings.

### Task 14: Update documentation

**Files:**
- Modify: `PROJECT_CONTEXT.md` — update architecture description
- Modify: `implementation-notes.md` — add cleanup entry

- [ ] **Step 1: Update `PROJECT_CONTEXT.md` section 4.1 (Controller)**

Replace section 4.1 to reflect that root controllers are now thin redirect wrappers pointing to Area controllers.

Apply edit to `PROJECT_CONTEXT.md` section 4.1. Replace the bullet "Controller" description.

- [ ] **Step 2: Add entry to `implementation-notes.md`**

Add a section under date `2026-06-08` describing the root controller cleanup with summary of what was done per phase.

- [ ] **Step 3: Build to verify**

Run:
```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU
```
Expected: Build succeeded.

### Task 15: Final integration verification

- [ ] **Step 1: Full build with view compilation**

Run:
```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU /p:MvcBuildViews=true
```
Expected: Build succeeded, 0 errors, 0 warnings.

- [ ] **Step 2: Run all verification scripts**

Run:
```powershell
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyAreasAndDtos.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifySecurityHardening.ps1
```
Expected: Both pass.

- [ ] **Step 3: Git commit Phase 4**

```bash
git add .
git commit -m "refactor: remove orphaned root views, update architecture docs

Remove Views/Assignment, Department, Employee, ReportingPeriod,
Report, Dashboard, Indicator, Notification — their controllers
now redirect to Areas and no longer render these views.

Update PROJECT_CONTEXT.md and implementation-notes.md.

Phase 4 (final) of the root controller cleanup plan.
No service behavior changes.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```
