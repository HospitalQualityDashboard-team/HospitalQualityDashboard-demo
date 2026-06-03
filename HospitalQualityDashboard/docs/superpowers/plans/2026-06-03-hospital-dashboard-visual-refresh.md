# Hospital Dashboard Visual Refresh Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the approved sidebar-based healthcare admin design system for the MVC Razor app and apply it to the highest-value management screens.

**Architecture:** Keep the existing ASP.NET MVC 4 structure. Update the shared Razor layout to provide a role-aware sidebar shell, then centralize visual language in `Content/Site.css` so existing views can adopt consistent page headers, filter panels, tables, KPI cards, badges, forms, and responsive behavior without new frontend dependencies.

**Tech Stack:** ASP.NET MVC 4, Razor `.cshtml`, Bootstrap, jQuery, Chart.js, custom CSS in `Content/Site.css`, PowerShell verification scripts, MSBuild.

---

## File Structure

- Modify `HospitalQualityDashboard/Views/Shared/_Layout.cshtml`: replace top-only navbar with the approved left-sidebar app shell, preserve role-aware menu visibility, logout form, profile link, Bootstrap bundles, and `RenderBody`.
- Modify `HospitalQualityDashboard/Content/Site.css`: define design tokens and shared layout/component classes for sidebar, topbar, page headers, panels, KPI cards, filter panels, tables, badges, forms, alerts, empty states, responsive rules, and compatibility with existing Bootstrap markup.
- Modify `HospitalQualityDashboard/Views/Dashboard/Index.cshtml`: align the overview screen with the concept: page header, KPI strip, task panel, Chart.js panel, and department progress table/list.
- Modify `HospitalQualityDashboard/Views/Report/Index.cshtml`: keep current report workflow and forms, but align page header, active-period cards, filter panel, status badges, and report table.
- Modify `HospitalQualityDashboard/Views/Department/Index.cshtml`: use shared management-list layout, action bar, filter panel, import panel, table, and empty/import states.
- Modify `HospitalQualityDashboard/Views/Employee/Index.cshtml`: same shared management-list pattern as departments with department filter, import panel, account action, and table.
- Modify `HospitalQualityDashboard/Views/Indicator/Index.cshtml`: use shared management-list pattern, Admin import/export actions, table density, status badges, and empty/import states.
- Modify `HospitalQualityDashboard/Views/Notification/Index.cshtml`: convert table into inbox-style management surface using shared panels, unread styling, type badges, and Admin actions.
- Modify `HospitalQualityDashboard/Views/ReportingPeriod/Index.cshtml`: update list page to shared table/actions/status pattern.
- Modify `HospitalQualityDashboard/Views/ReportingPeriod/GenerateSchedule.cshtml`: update schedule wizard/preview to shared form-panel and preview-table pattern.
- Leave `HospitalQualityDashboard/Views/Assignment/Index.cshtml` behavior intact in the first pass; normalize only outer spacing via CSS compatibility unless a later task explicitly scopes deeper Assignment cleanup.
- Use existing verification scripts under `HospitalQualityDashboard/tools/` and MSBuild. No new test project is required for this visual-only pass.

## Task 1: Baseline And Guard Rails

**Files:**
- Read: `HospitalQualityDashboard/Views/Shared/_Layout.cshtml`
- Read: `HospitalQualityDashboard/Content/Site.css`
- Read: `HospitalQualityDashboard/Views/Dashboard/Index.cshtml`
- Read: `HospitalQualityDashboard/Views/Report/Index.cshtml`
- Read: `HospitalQualityDashboard/Views/Department/Index.cshtml`
- Read: `HospitalQualityDashboard/Views/Employee/Index.cshtml`
- Read: `HospitalQualityDashboard/Views/Indicator/Index.cshtml`
- Read: `HospitalQualityDashboard/Views/Notification/Index.cshtml`
- Read: `HospitalQualityDashboard/Views/ReportingPeriod/Index.cshtml`
- Read: `HospitalQualityDashboard/Views/ReportingPeriod/GenerateSchedule.cshtml`

- [ ] **Step 1: Capture the exact current status of scoped files**

Run:

```powershell
git status --short -- HospitalQualityDashboard/Views/Shared/_Layout.cshtml HospitalQualityDashboard/Content/Site.css HospitalQualityDashboard/Views/Dashboard/Index.cshtml HospitalQualityDashboard/Views/Report/Index.cshtml HospitalQualityDashboard/Views/Department/Index.cshtml HospitalQualityDashboard/Views/Employee/Index.cshtml HospitalQualityDashboard/Views/Indicator/Index.cshtml HospitalQualityDashboard/Views/Notification/Index.cshtml HospitalQualityDashboard/Views/ReportingPeriod/Index.cshtml HospitalQualityDashboard/Views/ReportingPeriod/GenerateSchedule.cshtml
```

Expected: report any pre-existing modified files before editing. Do not revert them. Work with the current file contents.

- [ ] **Step 2: Run existing visual/report verification before edits if practical**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyVisualRefresh.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyReportWorkflowAndNotifications.ps1
```

Expected: record whether each script passes, fails, or is unavailable in the final task notes. If a script fails before edits, preserve the failure output as baseline context.

- [ ] **Step 3: Locate MSBuild**

Run:

```powershell
$candidates = @(
  "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe",
  "C:\Program Files\Microsoft Visual Studio\17\Community\MSBuild\Current\Bin\MSBuild.exe",
  "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
)
$candidates | Where-Object { Test-Path $_ }
```

Expected: at least one MSBuild path. If none exists, use Visual Studio Developer PowerShell or report that MVC build verification is blocked by missing MSBuild.

## Task 2: Shared Sidebar Shell

**Files:**
- Modify: `HospitalQualityDashboard/Views/Shared/_Layout.cshtml`
- Modify: `HospitalQualityDashboard/Content/Site.css`

- [ ] **Step 1: Replace layout body shell**

In `Views/Shared/_Layout.cshtml`, keep the existing Razor session variables and `navClass` helper, then replace the `<div class="app-shell">...</div>` body wrapper with this structure:

```cshtml
<div class="app-shell">
    <aside class="app-sidebar" id="appSidebar">
        <div class="app-sidebar-brand">
            @Html.ActionLink("HQ", "Index", "Home", new { area = "" }, new { @class = "app-brand-mark", aria_label = "Trang chủ" })
            <div class="app-brand-copy">
                <span class="app-brand-title">Chất lượng bệnh viện</span>
                <span class="app-brand-subtitle">Hospital Quality Dashboard</span>
            </div>
        </div>

        <nav class="app-sidebar-nav" aria-label="Điều hướng chính">
            @if (isAuthenticated)
            {
                @Html.ActionLink("Tổng quan", "Index", "Dashboard", new { area = "" }, new { @class = navClass("Dashboard", "Index") })
                if (isAdmin)
                {
                    @Html.ActionLink("Khoa/phòng", "Index", "Department", new { area = "" }, new { @class = navClass("Department", "") })
                    @Html.ActionLink("Nhân viên", "Index", "Employee", new { area = "" }, new { @class = navClass("Employee", "") })
                    @Html.ActionLink("Chỉ số", "Index", "Indicator", new { area = "" }, new { @class = navClass("Indicator", "") })
                    @Html.ActionLink("Phân công", "Index", "Assignment", new { area = "" }, new { @class = navClass("Assignment", "") })
                    @Html.ActionLink("Kỳ báo cáo", "Index", "ReportingPeriod", new { area = "" }, new { @class = navClass("ReportingPeriod", "") })
                    @Html.ActionLink("Báo cáo", "Index", "Report", new { area = "" }, new { @class = navClass("Report", "") })
                    @Html.ActionLink("Thông báo", "Index", "Notification", new { area = "" }, new { @class = navClass("Notification", "") })
                }
                else if (isUser)
                {
                    @Html.ActionLink("Chỉ số", "Index", "Indicator", new { area = "" }, new { @class = navClass("Indicator", "") })
                    @Html.ActionLink("Báo cáo của tôi", "Index", "Report", new { area = "" }, new { @class = navClass("Report", "") })
                    @Html.ActionLink("Thông báo", "Index", "Notification", new { area = "" }, new { @class = navClass("Notification", "") })
                }
            }
        </nav>
    </aside>

    <div class="app-content">
        <header class="app-topbar">
            <button type="button" class="app-sidebar-toggle" data-app-sidebar-toggle aria-controls="appSidebar" aria-expanded="false" aria-label="Mở menu">
                <span></span><span></span><span></span>
            </button>
            <div class="app-topbar-title">@ViewBag.Title</div>
            <div class="app-topbar-actions">
                @if (!isAuthenticated)
                {
                    @Html.ActionLink("Admin", "AdminLogin", "Account", new { area = "" }, new { @class = navClass("Account", "AdminLogin") })
                    @Html.ActionLink("User", "UserLogin", "Account", new { area = "" }, new { @class = navClass("Account", "UserLogin") })
                }
                else
                {
                    <span class="app-role-chip">@currentRole</span>
                    @Html.ActionLink("Hồ sơ", "Profile", "Account", new { area = "" }, new { @class = navClass("Account", "Profile") })
                    using (Html.BeginForm("Logout", "Account", FormMethod.Post, new { @class = "d-inline" }))
                    {
                        @Html.AntiForgeryToken()
                        <button type="submit" class="btn btn-link app-topbar-link">Đăng xuất</button>
                    }
                }
            </div>
        </header>

        <main class="app-main body-content">
            @RenderBody()
        </main>

        <footer class="app-footer">
            <span>&copy; @DateTime.Now.Year - Bảng điều khiển chất lượng bệnh viện</span>
        </footer>
    </div>
</div>
```

- [ ] **Step 2: Add sidebar toggle script before `</body>`**

Still in `_Layout.cshtml`, after `@RenderSection("scripts", required: false)`, add:

```html
<script>
    (function () {
        var toggle = document.querySelector('[data-app-sidebar-toggle]');
        var shell = document.querySelector('.app-shell');
        if (!toggle || !shell) return;
        toggle.addEventListener('click', function () {
            var isOpen = shell.classList.toggle('is-sidebar-open');
            toggle.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
        });
    })();
</script>
```

- [ ] **Step 3: Replace old navbar CSS with sidebar shell CSS**

In `Content/Site.css`, replace the existing `.app-navbar`, `.app-nav-*`, and top-navbar responsive blocks with sidebar definitions. Preserve existing dashboard/report/table classes until later tasks modify them.

Add this shell CSS near the top after global element rules:

```css
.app-shell {
    min-height: 100vh;
    display: grid;
    grid-template-columns: 264px minmax(0, 1fr);
}

.app-sidebar {
    position: sticky;
    top: 0;
    height: 100vh;
    display: flex;
    flex-direction: column;
    padding: 18px 14px;
    background: var(--app-surface);
    border-right: 1px solid var(--app-border);
    box-shadow: 8px 0 26px rgba(20, 32, 51, .06);
    z-index: 1040;
}

.app-sidebar-brand {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 0 8px 18px;
    border-bottom: 1px solid var(--app-border);
}

.app-sidebar-nav {
    display: grid;
    gap: 5px;
    padding: 16px 0;
}

.app-sidebar-nav .nav-link,
.app-topbar-actions .nav-link,
.app-topbar-link {
    display: inline-flex;
    align-items: center;
    min-height: 38px;
    padding: .52rem .72rem;
    color: #465468;
    border: 1px solid transparent;
    border-radius: 7px;
    font-size: .92rem;
    font-weight: 750;
    line-height: 1.1;
    text-decoration: none;
    white-space: nowrap;
}

.app-sidebar-nav .nav-link {
    position: relative;
    width: 100%;
    justify-content: flex-start;
}

.app-sidebar-nav .nav-link:hover,
.app-sidebar-nav .nav-link:focus,
.app-topbar-actions .nav-link:hover,
.app-topbar-actions .nav-link:focus,
.app-topbar-link:hover,
.app-topbar-link:focus {
    color: var(--app-primary-dark);
    background: var(--app-primary-soft);
    border-color: rgba(20, 108, 120, .14);
}

.app-sidebar-nav .nav-link.active {
    color: var(--app-primary-dark);
    background: var(--app-primary-soft);
    border-color: rgba(20, 108, 120, .18);
}

.app-sidebar-nav .nav-link.active::before {
    content: "";
    position: absolute;
    left: -6px;
    top: 8px;
    bottom: 8px;
    width: 3px;
    border-radius: 999px;
    background: var(--app-primary);
}

.app-content {
    min-width: 0;
    display: flex;
    flex-direction: column;
}

.app-topbar {
    position: sticky;
    top: 0;
    z-index: 1030;
    min-height: 60px;
    display: flex;
    align-items: center;
    gap: 14px;
    padding: 0 24px;
    background: rgba(255, 255, 255, .92);
    border-bottom: 1px solid rgba(189, 203, 215, .82);
    backdrop-filter: blur(14px);
}

.app-topbar-title {
    min-width: 0;
    color: var(--app-muted);
    font-size: .9rem;
    font-weight: 800;
}

.app-topbar-actions {
    margin-left: auto;
    display: flex;
    align-items: center;
    gap: 8px;
    flex-wrap: wrap;
}

.app-sidebar-toggle {
    display: none;
    width: 40px;
    height: 38px;
    align-items: center;
    justify-content: center;
    flex-direction: column;
    gap: 4px;
    border: 1px solid var(--app-border);
    border-radius: 8px;
    background: var(--app-surface);
}

.app-sidebar-toggle span {
    width: 18px;
    height: 2px;
    background: var(--app-text);
    border-radius: 999px;
}

.app-main {
    flex: 1;
    width: 100%;
    max-width: 1480px;
    margin: 0 auto;
    padding: 28px 24px 36px;
}

.app-footer {
    max-width: 1480px;
    width: 100%;
    margin: 0 auto;
    padding: 16px 24px;
    color: var(--app-muted);
    border-top: 1px solid var(--app-border);
    font-size: .88rem;
}
```

- [ ] **Step 4: Add responsive sidebar CSS**

Add this under the existing media queries, replacing any old navbar-specific mobile rules:

```css
@media (max-width: 992px) {
    .app-shell {
        grid-template-columns: 1fr;
    }

    .app-sidebar {
        position: fixed;
        left: 0;
        top: 0;
        bottom: 0;
        width: 264px;
        transform: translateX(-100%);
        transition: transform .24s cubic-bezier(.16, 1, .3, 1);
    }

    .app-shell.is-sidebar-open .app-sidebar {
        transform: translateX(0);
    }

    .app-sidebar-toggle {
        display: inline-flex;
    }

    .app-topbar {
        padding: 0 16px;
    }

    .app-topbar-actions {
        gap: 6px;
    }
}

@media (max-width: 768px) {
    .app-main {
        padding: 20px 14px 28px;
    }

    .app-topbar-title {
        display: none;
    }

    .app-topbar-actions .nav-link,
    .app-topbar-link {
        min-height: 34px;
        padding: .42rem .55rem;
        font-size: .84rem;
    }
}
```

- [ ] **Step 5: Build-check layout syntax**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:MvcBuildViews=true
```

Expected: build succeeds. If using a different MSBuild path from Task 1, substitute that path.

- [ ] **Step 6: Commit shell**

Run:

```powershell
git add -- HospitalQualityDashboard/Views/Shared/_Layout.cshtml HospitalQualityDashboard/Content/Site.css
git commit -m "Implement sidebar application shell"
```

Expected: commit includes only the shared layout and CSS shell changes.

## Task 3: Shared Component System

**Files:**
- Modify: `HospitalQualityDashboard/Content/Site.css`

- [ ] **Step 1: Normalize shared action/filter/table classes**

Add or update these CSS blocks in `Site.css`. Keep existing class names used by views so current markup continues to work:

```css
.page-header {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 16px;
    margin-bottom: 18px;
}

.page-header h1 {
    margin: 0;
    color: var(--app-text);
    font-size: 1.75rem;
    font-weight: 850;
    line-height: 1.16;
}

.page-header p {
    max-width: 780px;
    margin: 6px 0 0;
    color: var(--app-muted);
}

.page-actions,
.action-bar {
    display: flex;
    align-items: center;
    gap: 8px;
    flex-wrap: wrap;
    margin-bottom: 14px;
}

.dashboard-panel,
.management-panel,
.filter-panel,
.import-panel,
.missing-work-panel {
    background: var(--app-surface);
    border: 1px solid var(--app-border);
    border-radius: 8px;
    box-shadow: var(--app-shadow-soft);
}

.management-panel,
.import-panel {
    padding: 16px;
    margin-bottom: 16px;
}

.filter-panel {
    padding: 14px;
    margin-bottom: 16px;
}

.filter-form {
    display: grid;
    grid-template-columns: repeat(4, minmax(180px, 1fr));
    gap: 12px;
    align-items: end;
}

.filter-field {
    display: grid;
    gap: 6px;
}

.filter-field label {
    color: var(--app-muted);
    font-size: .78rem;
    font-weight: 850;
    text-transform: uppercase;
}

.filter-actions {
    display: flex;
    gap: 8px;
    justify-content: flex-end;
    flex-wrap: wrap;
}

.table {
    margin-bottom: 0;
    background: var(--app-surface);
    border: 1px solid var(--app-border);
    border-radius: 8px;
    box-shadow: var(--app-shadow-soft);
}

.table thead th {
    color: #344054;
    background: var(--app-surface-soft);
    border-bottom: 1px solid var(--app-border);
    font-size: .78rem;
    font-weight: 850;
    text-transform: uppercase;
}

.table td,
.table th {
    vertical-align: middle;
    padding: .72rem .85rem;
}

.table tbody tr:hover {
    background: #eff7f8;
}

.table-actions {
    white-space: nowrap;
}

.table-actions form,
.table-actions .btn {
    margin-left: 4px;
}
```

- [ ] **Step 2: Normalize status and empty states**

Ensure these status classes exist and use color plus readable text:

```css
.status-pill {
    display: inline-flex;
    align-items: center;
    min-height: 26px;
    padding: 3px 10px;
    border-radius: 999px;
    font-size: .82rem;
    font-weight: 800;
    white-space: nowrap;
}

.status-active,
.status-success,
.status-open,
.status-read {
    color: var(--app-success);
    background: var(--app-success-soft);
}

.status-muted,
.status-draft,
.status-locked {
    color: var(--app-muted);
    background: #edf1f5;
}

.status-warning,
.status-unread {
    color: var(--app-warning);
    background: var(--app-warning-soft);
}

.status-danger,
.status-overdue {
    color: var(--app-danger);
    background: var(--app-danger-soft);
}

.empty-state {
    padding: 24px;
    color: var(--app-muted);
    background: var(--app-surface-soft);
    border: 1px dashed var(--app-border-strong);
    border-radius: 8px;
    text-align: center;
}
```

- [ ] **Step 3: Normalize form and button polish**

Keep Bootstrap behavior but enforce project styling:

```css
.form-control,
.form-select {
    max-width: none;
    border-color: #cbd6e2;
    border-radius: 7px;
}

.form-control:focus,
.form-select:focus {
    border-color: var(--app-primary);
    box-shadow: 0 0 0 .2rem rgba(20, 108, 120, .14);
}

.btn {
    border-radius: 7px;
    font-weight: 750;
}

.btn-primary {
    background: var(--app-primary);
    border-color: var(--app-primary);
}

.btn-primary:hover,
.btn-primary:focus {
    background: var(--app-primary-dark);
    border-color: var(--app-primary-dark);
}

.btn-outline-primary {
    color: var(--app-primary);
    border-color: var(--app-primary);
}

.btn-outline-primary:hover,
.btn-outline-primary:focus {
    color: #ffffff;
    background: var(--app-primary);
    border-color: var(--app-primary);
}
```

- [ ] **Step 4: Add responsive component rules**

Add:

```css
@media (max-width: 992px) {
    .filter-form {
        grid-template-columns: repeat(2, minmax(0, 1fr));
    }

    .filter-actions {
        justify-content: flex-start;
    }
}

@media (max-width: 768px) {
    .page-header {
        display: block;
    }

    .page-actions,
    .action-bar {
        margin-top: 12px;
    }

    .filter-form {
        grid-template-columns: 1fr;
    }

    .filter-actions .btn {
        width: 100%;
    }

    .table {
        display: block;
        overflow-x: auto;
        white-space: nowrap;
    }
}
```

- [ ] **Step 5: Run CSS grep sanity check**

Run:

```powershell
rg -n "app-navbar|app-nav-container|navbar-expand-xl|rounded-pill shadow-sm" HospitalQualityDashboard\Content\Site.css HospitalQualityDashboard\Views\Shared\_Layout.cshtml
```

Expected: no old shared top-navbar references in `_Layout.cshtml` or `Site.css`. Existing `rounded-pill shadow-sm` in `Assignment/Index.cshtml` is acceptable because Assignment deeper cleanup is out of first pass.

- [ ] **Step 6: Commit component CSS**

Run:

```powershell
git add -- HospitalQualityDashboard/Content/Site.css
git commit -m "Add shared dashboard component styling"
```

Expected: commit contains only `Site.css`.

## Task 4: Dashboard Overview Refresh

**Files:**
- Modify: `HospitalQualityDashboard/Views/Dashboard/Index.cshtml`
- Modify: `HospitalQualityDashboard/Content/Site.css`

- [ ] **Step 1: Update dashboard header and KPI model variables**

At the top of `Dashboard/Index.cshtml`, keep existing `progressItems`, `missingReports`, `completionRate`, and labels. Add:

```cshtml
var pendingLabel = Model.IsAdmin ? "Chờ nhập" : "Còn thiếu";
var taskPanelTitle = Model.IsAdmin ? "Việc cần theo dõi" : "Việc báo cáo cần xử lý";
```

- [ ] **Step 2: Replace the dashboard hero with a compact page header**

Replace `<section class="dashboard-hero">...</section>` with:

```cshtml
<div class="page-header dashboard-page-header">
    <div>
        <span class="dashboard-role">@roleLabel</span>
        <h1>Trung tâm điều hành chất lượng</h1>
        <p>Theo dõi tiến độ nộp báo cáo, nhận diện chỉ số còn thiếu và kiểm soát rủi ro quá hạn theo khoa/phòng.</p>
    </div>
    <div class="dashboard-summary-strip" aria-label="Tóm tắt tiến độ">
        <div class="dashboard-summary-item">
            <span>Tỷ lệ hoàn tất</span>
            <strong>@completionRate%</strong>
        </div>
        <div class="dashboard-summary-item">
            <span>Cần xử lý</span>
            <strong>@urgentCount</strong>
        </div>
        <div class="dashboard-summary-item">
            <span>Khoa/phòng</span>
            <strong>@progressItems.Count</strong>
        </div>
    </div>
</div>
```

- [ ] **Step 3: Update metric grid labels**

Keep the four existing metric cards, but change the third card label to use `@pendingLabel`:

```cshtml
<span class="metric-label">@pendingLabel</span>
```

- [ ] **Step 4: Add task panel for Admin and preserve User missing-report behavior**

For Admin, insert a `missing-work-panel` before `metric-grid`:

```cshtml
@if (Model.IsAdmin)
{
    <section class="missing-work-panel">
        <div class="missing-work-summary">
            <div>
                <h2>@taskPanelTitle</h2>
                <p>Có @Model.BaoCaoThieu báo cáo còn thiếu và @Model.BaoCaoQuaHan báo cáo quá hạn trong phạm vi hệ thống.</p>
                <div class="status-stack">
                    <span class="status-pill status-warning">@Model.BaoCaoThieu chờ nhập</span>
                    <span class="status-pill status-danger">@Model.BaoCaoQuaHan quá hạn</span>
                </div>
            </div>
            @Html.ActionLink("Xem báo cáo", "Index", "Report", null, new { @class = "btn btn-outline-primary btn-sm" })
        </div>
    </section>
}
```

Keep the current User-only missing report table under `@if (!Model.IsAdmin)` unchanged except for heading variable if desired.

- [ ] **Step 5: Convert progress list into table-like rows**

Keep existing chart panel. In the right-side progress panel, preserve the current `.progress-list` loop but ensure each row displays department, count, progress track, and percent. Use:

```cshtml
<div class="progress-row">
    <div>
        <div class="progress-name">@item.TenKhoaPhong</div>
        <div class="progress-meta">@item.DaGui/@item.Tong báo cáo</div>
    </div>
    <div class="progress-track" aria-label="@item.TenKhoaPhong đạt @percent%">
        <div class="progress-fill" style="width: @(percent)%"></div>
    </div>
    <div class="progress-number">@percent%</div>
</div>
```

- [ ] **Step 6: Update dashboard CSS density**

In `Site.css`, remove large hero-specific visual weight from `.dashboard-hero` and prefer `.dashboard-page-header`. Add:

```css
.dashboard-page-header {
    align-items: stretch;
}

.dashboard-summary-strip {
    display: grid;
    grid-template-columns: repeat(3, minmax(112px, 1fr));
    gap: 10px;
    min-width: 360px;
}
```

Keep existing metric/grid/chart/progress CSS, adjusting only if the rendered page overflows.

- [ ] **Step 7: Verify dashboard syntax**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:MvcBuildViews=true
```

Expected: build succeeds.

- [ ] **Step 8: Commit dashboard**

Run:

```powershell
git add -- HospitalQualityDashboard/Views/Dashboard/Index.cshtml HospitalQualityDashboard/Content/Site.css
git commit -m "Refresh dashboard overview layout"
```

Expected: commit includes dashboard and any needed CSS.

## Task 5: Management List Pages

**Files:**
- Modify: `HospitalQualityDashboard/Views/Department/Index.cshtml`
- Modify: `HospitalQualityDashboard/Views/Employee/Index.cshtml`
- Modify: `HospitalQualityDashboard/Views/Indicator/Index.cshtml`
- Modify: `HospitalQualityDashboard/Content/Site.css`

- [ ] **Step 1: Update Department page action and filter structure**

In `Department/Index.cshtml`, replace the action `<div class="mb-3">` with:

```cshtml
<div class="action-bar">
    @Html.ActionLink("Thêm khoa/phòng", "Create", null, new { @class = "btn btn-primary" })
    @Html.ActionLink("Xuất CSV", "Departments", "Export", null, new { @class = "btn btn-outline-secondary" })
</div>
```

Replace the search form wrapper with:

```cshtml
<section class="filter-panel">
    @using (Html.BeginForm("Index", "Department", FormMethod.Get, new { @class = "filter-form" }))
    {
        <div class="filter-field">
            <label for="Search">Tìm kiếm</label>
            @Html.TextBoxFor(m => m.Search, new { @class = "form-control", placeholder = "Tìm tên hoặc mã khoa/phòng" })
        </div>
        <div class="filter-actions">
            <button class="btn btn-outline-primary" type="submit">Lọc</button>
        </div>
    }
</section>
```

- [ ] **Step 2: Wrap Department import in import panel**

Replace the import form wrapper with:

```cshtml
<section class="import-panel">
    @using (Html.BeginForm("Import", "Department", FormMethod.Post, new { enctype = "multipart/form-data" }))
    {
        @Html.AntiForgeryToken()
        <div class="input-group">
            <input type="file" name="File" class="form-control" />
            <button class="btn btn-outline-primary" type="submit">Import DM_KHOA_PHONG</button>
        </div>
    }
</section>
```

- [ ] **Step 3: Add Department empty state**

Wrap the table with:

```cshtml
@if (Model.Items != null && Model.Items.Any())
{
    <div class="table-responsive">
        <table class="table table-sm align-middle">
            ...
        </table>
    </div>
}
else
{
    <div class="empty-state">Chưa có khoa/phòng phù hợp với bộ lọc hiện tại.</div>
}
```

- [ ] **Step 4: Apply the same pattern to Employee**

In `Employee/Index.cshtml`, use:

```cshtml
<div class="action-bar">
    @Html.ActionLink("Thêm nhân viên", "Create", null, new { @class = "btn btn-primary" })
    @Html.ActionLink("Xuất CSV", "Employees", "Export", new { khoaPhongId = Model.KhoaPhongId }, new { @class = "btn btn-outline-secondary" })
</div>

<section class="filter-panel">
    @using (Html.BeginForm("Index", "Employee", FormMethod.Get, new { @class = "filter-form" }))
    {
        <div class="filter-field">
            <label for="KhoaPhongId">Khoa/phòng</label>
            @Html.DropDownListFor(m => m.KhoaPhongId, Model.KhoaPhongOptions, "Tất cả khoa/phòng", new { @class = "form-select" })
        </div>
        <div class="filter-actions">
            <button class="btn btn-outline-primary" type="submit">Lọc</button>
        </div>
    }
</section>
```

Wrap import form in `.import-panel` and table in `Model.Items != null && Model.Items.Any()` with empty state text:

```cshtml
<div class="empty-state">Chưa có nhân viên phù hợp với bộ lọc hiện tại.</div>
```

- [ ] **Step 5: Apply the same pattern to Indicator**

In `Indicator/Index.cshtml`, for Admin action/import section use:

```cshtml
<div class="action-bar">
    @Html.ActionLink("Thêm chỉ số", "Create", null, new { @class = "btn btn-primary" })
    @Html.ActionLink("Xuất CSV", "Indicators", "Export", null, new { @class = "btn btn-outline-secondary" })
</div>

<section class="import-panel">
    @using (Html.BeginForm("Import", "Indicator", FormMethod.Post, new { enctype = "multipart/form-data" }))
    {
        @Html.AntiForgeryToken()
        <div class="input-group">
            <input type="file" name="File" class="form-control" accept=".xlsx,.csv,.docx" />
            <button class="btn btn-outline-primary" type="submit">Import chỉ số</button>
        </div>
    }
</section>
```

Wrap table in an `Any()` guard and use empty state:

```cshtml
<div class="empty-state">Chưa có chỉ số chất lượng trong phạm vi hiển thị.</div>
```

- [ ] **Step 6: Ensure `System.Linq` is available where `Any()` is used**

Add this near the top of each modified view if it is missing:

```cshtml
@using System.Linq
```

- [ ] **Step 7: Build-check management pages**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:MvcBuildViews=true
```

Expected: build succeeds.

- [ ] **Step 8: Commit management list pages**

Run:

```powershell
git add -- HospitalQualityDashboard/Views/Department/Index.cshtml HospitalQualityDashboard/Views/Employee/Index.cshtml HospitalQualityDashboard/Views/Indicator/Index.cshtml HospitalQualityDashboard/Content/Site.css
git commit -m "Refresh management list pages"
```

Expected: commit includes only these list pages and any required CSS.

## Task 6: Reports And Reporting Periods

**Files:**
- Modify: `HospitalQualityDashboard/Views/Report/Index.cshtml`
- Modify: `HospitalQualityDashboard/Views/ReportingPeriod/Index.cshtml`
- Modify: `HospitalQualityDashboard/Views/ReportingPeriod/GenerateSchedule.cshtml`
- Modify: `HospitalQualityDashboard/Content/Site.css`

- [ ] **Step 1: Preserve Report workflow and tighten classes**

In `Report/Index.cshtml`, keep the current `report-page`, active-period cards, filter form, status switch, and action forms. Ensure the outer sections use:

```cshtml
<section class="dashboard-panel report-open-section">
...
<section class="filter-panel">
...
<section class="dashboard-panel">
```

If the current file already uses this structure, do not rewrite it.

- [ ] **Step 2: Add report table empty state and responsive wrapper if missing**

Ensure report table is wrapped exactly:

```cshtml
@if (reports.Any())
{
    <div class="table-responsive">
        <table class="table table-sm align-middle report-table">
            ...
        </table>
    </div>
}
else
{
    <div class="empty-state">Chưa có báo cáo phù hợp với bộ lọc hiện tại.</div>
}
```

- [ ] **Step 3: Update ReportingPeriod action bar**

In `ReportingPeriod/Index.cshtml`, replace the `<p>` action block with:

```cshtml
<div class="action-bar">
    @Html.ActionLink("Thêm kỳ báo cáo", "Create", null, new { @class = "btn btn-primary" })
    @Html.ActionLink("Tạo lịch tự động", "GenerateSchedule", null, new { @class = "btn btn-outline-primary" })
</div>
```

- [ ] **Step 4: Use status pills in ReportingPeriod table**

Replace raw status cell:

```cshtml
<td>@FormatPeriodStatus(item.TrangThai)</td>
```

with:

```cshtml
<td>
    @if (item.TrangThai == TrangThaiKyBaoCao.Mo)
    {
        <span class="status-pill status-open">Mở</span>
    }
    else if (item.TrangThai == TrangThaiKyBaoCao.Khoa)
    {
        <span class="status-pill status-locked">Khóa</span>
    }
    else
    {
        <span class="status-pill status-draft">Nhập</span>
    }
</td>
```

- [ ] **Step 5: Wrap ReportingPeriod table with `Any()` guard**

Add `@using System.Linq` if missing and wrap the table:

```cshtml
@if (Model != null && Model.Any())
{
    <div class="table-responsive">
        <table class="table table-sm align-middle">
            ...
        </table>
    </div>
}
else
{
    <div class="empty-state">Chưa có kỳ báo cáo nào.</div>
}
```

- [ ] **Step 6: Update GenerateSchedule form into panel**

In `GenerateSchedule.cshtml`, wrap the preview form in:

```cshtml
<section class="management-panel">
    @using (Html.BeginForm("PreviewSchedule", "ReportingPeriod", FormMethod.Post))
    {
        ...
    }
</section>
```

Keep all existing hidden fields and validation messages.

- [ ] **Step 7: Update GenerateSchedule preview header and table**

When preview items exist, wrap preview table:

```cshtml
<section class="dashboard-panel">
    <div class="panel-header">
        <div>
            <h2>Danh sách kỳ dự kiến</h2>
            <p>Kiểm tra kỳ mới và kỳ đã tồn tại trước khi tạo lịch.</p>
        </div>
        @using (Html.BeginForm("CreateSchedule", "ReportingPeriod", FormMethod.Post, new { @class = "d-inline" }))
        {
            ...
            <button class="btn btn-success" type="submit">Tạo các kỳ chưa tồn tại</button>
        }
    </div>
    <div class="table-responsive">
        <table class="table table-sm align-middle">
            ...
        </table>
    </div>
</section>
```

- [ ] **Step 8: Build and workflow verification**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:MvcBuildViews=true
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyReportingPeriodSchedule.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyReportWorkflowAndNotifications.ps1
```

Expected: build succeeds. Scripts pass or any pre-existing baseline failures are unchanged and documented.

- [ ] **Step 9: Commit reports and periods**

Run:

```powershell
git add -- HospitalQualityDashboard/Views/Report/Index.cshtml HospitalQualityDashboard/Views/ReportingPeriod/Index.cshtml HospitalQualityDashboard/Views/ReportingPeriod/GenerateSchedule.cshtml HospitalQualityDashboard/Content/Site.css
git commit -m "Refresh report and period screens"
```

Expected: commit includes only report/period UI files and required CSS.

## Task 7: Notification Inbox

**Files:**
- Modify: `HospitalQualityDashboard/Views/Notification/Index.cshtml`
- Modify: `HospitalQualityDashboard/Content/Site.css`

- [ ] **Step 1: Add `System.Linq` and model list guard**

At top of `Notification/Index.cshtml`, add:

```cshtml
@using System.Linq
@{
    var notifications = (Model ?? Enumerable.Empty<HospitalQualityDashboard.Models.ViewModels.NotificationViewModel>()).ToList();
}
```

Keep the existing `isAdmin` variable.

- [ ] **Step 2: Move Admin actions into page header actions**

Update the page header to:

```cshtml
<div class="page-header">
    <div>
        <h1>Thông báo</h1>
        <p>Theo dõi, đọc và gửi thông báo cho các khoa/phòng.</p>
    </div>
    @if (isAdmin)
    {
        <div class="page-actions">
            @Html.ActionLink("Gửi thông báo", "Create", null, new { @class = "btn btn-primary" })
            @using (Html.BeginForm("RunAutomation", "Notification", FormMethod.Post, new { @class = "d-inline" }))
            {
                @Html.AntiForgeryToken()
                <button class="btn btn-outline-success" type="submit">Chạy kiểm tra tự động</button>
            }
        </div>
    }
</div>
```

- [ ] **Step 3: Convert table to inbox panel**

Replace the table block with:

```cshtml
@if (notifications.Any())
{
    <section class="dashboard-panel notification-inbox">
        <div class="table-responsive">
            <table class="table table-sm align-middle">
                <thead>
                    <tr>
                        <th>Thông báo</th>
                        <th>Loại</th>
                        <th>Ngày tạo</th>
                        <th>Trạng thái</th>
                        <th></th>
                    </tr>
                </thead>
                <tbody>
                @foreach (var item in notifications)
                {
                    var detailUrl = Url.Action("Details", new { id = item.ThongBaoId });
                    <tr class="@(item.DaDoc ? "" : "notification-unread-row")">
                        <td>
                            <strong>@Html.ActionLink(item.TieuDe, "Details", new { id = item.ThongBaoId }, new { @class = "text-dark text-decoration-none" })</strong><br />
                            <a href="@detailUrl" class="text-body text-decoration-none notification-preview">@item.NoiDung</a>
                        </td>
                        <td><span class="status-pill status-muted">@item.LoaiThongBao</span></td>
                        <td>@item.NgayTao.ToString("dd/MM/yyyy HH:mm")</td>
                        <td>
                            <span class="status-pill @(item.DaDoc ? "status-read" : "status-unread")">@(item.DaDoc ? "Đã đọc" : "Chưa đọc")</span>
                        </td>
                        <td class="text-end">
                            @Html.ActionLink("Xem chi tiết", "Details", new { id = item.ThongBaoId }, new { @class = "btn btn-sm btn-outline-primary" })
                        </td>
                    </tr>
                }
                </tbody>
            </table>
        </div>
    </section>
}
else
{
    <div class="empty-state">Chưa có thông báo nào.</div>
}
```

- [ ] **Step 4: Add inbox CSS**

In `Site.css`, add:

```css
.notification-inbox {
    padding: 0;
    overflow: hidden;
}

.notification-preview {
    display: inline-block;
    max-width: 720px;
    color: var(--app-muted) !important;
}

.notification-unread-row td:first-child {
    border-left: 3px solid var(--app-warning);
}
```

- [ ] **Step 5: Build-check notifications**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:MvcBuildViews=true
```

Expected: build succeeds.

- [ ] **Step 6: Commit notification inbox**

Run:

```powershell
git add -- HospitalQualityDashboard/Views/Notification/Index.cshtml HospitalQualityDashboard/Content/Site.css
git commit -m "Refresh notification inbox"
```

Expected: commit includes only notification index and CSS.

## Task 8: Final Verification And Visual QA

**Files:**
- Verify: all files modified in previous tasks.
- Reference: `HospitalQualityDashboard/docs/superpowers/concepts/2026-06-03-dashboard-sidebar-concept.png`

- [ ] **Step 1: Run full build with Razor view compilation**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:MvcBuildViews=true
```

Expected: build succeeds without Razor syntax errors.

- [ ] **Step 2: Run available project verification scripts**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyVisualRefresh.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyReportWorkflowAndNotifications.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyReportingPeriodSchedule.ps1
powershell -ExecutionPolicy Bypass -File .\HospitalQualityDashboard\tools\VerifyProfilePage.ps1
```

Expected: scripts pass. If a script fails because it was already failing in Task 1 baseline, document that as pre-existing.

- [ ] **Step 3: Manual browser verification in IIS Express or Visual Studio**

Start the MVC app in Visual Studio/IIS Express and verify these routes for both layout and role-aware navigation:

```text
/Dashboard
/Department
/Employee
/Indicator
/Report
/ReportingPeriod
/ReportingPeriod/GenerateSchedule
/Notification
/Account/Profile
```

Expected:

- Sidebar appears on desktop.
- Mobile toggle opens/closes sidebar around 375px width.
- Admin menu includes all Admin modules.
- User menu excludes Admin-only modules.
- Buttons and badges do not overflow.
- Data tables remain readable and horizontally scroll on mobile.
- Dashboard resembles the approved sidebar concept in structure, palette, density, typography, KPI treatment, table readability, and status badge behavior.

- [ ] **Step 4: Inspect generated CSS for forbidden visual patterns**

Run:

```powershell
rg -n "orb|blob|hero|linear-gradient\\([^)]*purple|font-size:\\s*clamp|letter-spacing:\\s*-" HospitalQualityDashboard\Content\Site.css
```

Expected: no decorative orb/blob patterns, no large marketing hero system, no viewport-scaled font sizes, and no negative letter spacing. Existing subtle teal/blue gradients for progress fills are acceptable.

- [ ] **Step 5: Confirm scoped git diff**

Run:

```powershell
git status --short
git diff --stat
```

Expected: visual refresh changes are limited to planned UI files. Do not stage unrelated pre-existing changes.

- [ ] **Step 6: Final commit if verification produced cleanup changes**

If Task 8 required small cleanup edits, run:

```powershell
git add -- HospitalQualityDashboard/Views/Shared/_Layout.cshtml HospitalQualityDashboard/Content/Site.css HospitalQualityDashboard/Views/Dashboard/Index.cshtml HospitalQualityDashboard/Views/Report/Index.cshtml HospitalQualityDashboard/Views/Department/Index.cshtml HospitalQualityDashboard/Views/Employee/Index.cshtml HospitalQualityDashboard/Views/Indicator/Index.cshtml HospitalQualityDashboard/Views/Notification/Index.cshtml HospitalQualityDashboard/Views/ReportingPeriod/Index.cshtml HospitalQualityDashboard/Views/ReportingPeriod/GenerateSchedule.cshtml
git commit -m "Polish dashboard visual refresh"
```

Expected: commit contains only cleanup needed after verification.

## Self-Review

- Spec coverage: The plan covers the selected scope B by establishing the shared sidebar layout and design system first, then applying it to dashboard, report, department, employee, indicator, notification, and reporting-period screens. Assignment deeper cleanup, auth screens, profile, details/edit forms, and error/Home/About/Contact are acknowledged as lower-priority follow-up surfaces rather than hidden scope.
- Placeholder scan: No task uses TBD/TODO/fill-in placeholders. Each code-changing step includes concrete markup/CSS or an exact preservation instruction.
- Type consistency: Razor snippets use existing model names and properties: `DashboardViewModel`, `ReportListViewModel`, `KhoaPhongIndexViewModel`, `NhanVienIndexViewModel`, `ChiSoIndexViewModel`, `KyBaoCaoViewModel`, `ReportingPeriodScheduleRequestViewModel`, and `NotificationViewModel`. Status snippets use existing enums `TrangThaiKyBaoCao` and existing view variables.
- Risk note: Several existing files appear modified before this plan. Implementation must avoid reverting user changes and must stage only the task-scoped files.
