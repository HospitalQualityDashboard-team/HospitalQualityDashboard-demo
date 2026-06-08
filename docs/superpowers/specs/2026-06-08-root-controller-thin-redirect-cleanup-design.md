# Root Controller Thin Redirect Cleanup Design

## Context

HospitalQualityDashboard is an ASP.NET MVC 4 application that now contains role-specific MVC Areas:

- `Areas/Admin` for Admin workflows.
- `Areas/User` for department User workflows.
- Shared `Services` and `Models/DTOs` continue to hold actual business logic and data contracts.

The cleanup goal is to remove business logic from legacy root controllers while preserving backward-compatible root routes. No shared service behavior change is intended.

## Selected Approach

Use phased migration to convert legacy root controllers into thin redirect controllers.

A request to a legacy route such as `/Report/Index` should still work, but the root controller should only decide the current role and redirect to the appropriate Area route:

```text
Root route -> thin root controller -> Admin/User Area controller -> shared service
```

This keeps old bookmarks and links working while moving executable workflows to Area controllers.

## Architecture

### Root controllers

Root controllers should contain only:

- authentication/session gate inherited from `PageController`;
- role selection when a workflow exists in both Admin and User areas;
- route parameter preservation;
- `RedirectToAction` into `area = "Admin"` or `area = "User"`.

Root controllers should not call business services directly after migration.

### Area controllers

Area controllers remain the executable workflow layer:

- `Areas/Admin/Controllers/*Controller.cs` handles Admin workflows.
- `Areas/User/Controllers/*Controller.cs` handles User workflows.

They may call shared services and use DTOs/ViewModels.

### Shared services

Shared services keep their current behavior and should not be modified as part of this cleanup unless a compile error or redirect compatibility issue requires a minimal signature adjustment. The expected cleanup should not change SQL, data access, import parsing, reporting calculation, notification automation, or dashboard behavior.

## Migration Phases

### Phase 1: Admin-only root controllers

Convert these root controllers to Admin redirect-only wrappers:

- `Controllers/AssignmentController.cs`
- `Controllers/DepartmentController.cs`
- `Controllers/EmployeeController.cs`
- `Controllers/ReportingPeriodController.cs`

Each action should call `RequireAdmin()` where appropriate, preserve route/query parameters, and redirect to the Admin Area equivalent.

### Phase 2: Dual-role read controllers

Convert root controllers whose main entry points select Admin/User destination:

- `Controllers/DashboardController.cs`
- `Controllers/IndicatorController.cs`

Admin users redirect to `area = "Admin"`; department users redirect to `area = "User"`.

### Phase 3: Dual-role write/export controllers

Convert more sensitive dual-role controllers after Phase 1 and Phase 2 pass:

- `Controllers/ReportController.cs`
- `Controllers/NotificationController.cs`
- `Controllers/ExportController.cs`

POST actions should preserve anti-forgery protection at the Area controller layer. Root POST actions can redirect to Area actions with route parameters, but should not execute business logic.

### Phase 4: Root view cleanup and documentation

After each corresponding controller phase passes, remove root views that are no longer rendered by any root controller. Keep root views needed by Account/Home/shared error pages.

Update documentation to describe:

- Area controllers as the active workflow layer;
- root controllers as backward-compatible redirect wrappers;
- shared services as unchanged business/data access layer.

## Verification Criteria Per Phase

A phase is complete only when all applicable checks pass:

1. Project builds successfully.
2. MVC view compilation passes with `MvcBuildViews=true`.
3. Existing verification scripts for Areas/DTOs and security hardening pass.
4. Root controllers in the phase no longer instantiate or call business services.
5. Redirect route parameters are preserved for list filters, paging, and identifiers.
6. Removed root views are not referenced by remaining root controllers.

## Risks and Mitigations

### Route parameter loss

Some root actions accept filters such as period, department, indicator, status, search text, and page. Redirect wrappers must pass those values through unchanged.

### Dual-role routing mistakes

For controllers with both Admin and User versions, root wrappers must choose destination by `IsAdmin`. User redirects must never expose Admin Area routes.

### POST redirect behavior

POST requests redirected with HTTP 302 become GET requests in browsers. This is acceptable only when the root POST action is used as a compatibility shim and the actual form posts already target Area routes. If existing root forms still POST to root routes, their views should not be removed until form targets are verified.

### Root layout links

`Views/Shared/_Layout.cshtml` should not keep links that route users back into legacy root workflows. It should point to Area routes for logged-in Admin/User users.

## Out of Scope

- Changing shared service behavior.
- Changing SQL schema or migrations.
- Reworking authentication beyond existing session/role checks.
- Adding new business workflows.
- Removing `AccountController`, `HomeController`, or `PageController`.
