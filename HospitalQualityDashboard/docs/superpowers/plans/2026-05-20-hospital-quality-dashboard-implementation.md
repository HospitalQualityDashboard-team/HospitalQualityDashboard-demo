# Hospital Quality Dashboard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build an ASP.NET MVC5 application for managing hospital quality indicators, department assignments, reporting periods, data entry, dashboards, and exports.

**Architecture:** Use a classic ASP.NET MVC5 structure with controllers, Razor views, SQL Server tables, and service classes for database access/business rules. All secured controllers must inherit from a shared `PageController`, which centralizes Session access, login checks, Admin/User role checks, and department-scope filtering.

**Tech Stack:** ASP.NET MVC5, .NET Framework 4.7.2, SQL Server, ADO.NET or EF6 if the package is available, Bootstrap 5, Chart.js, Excel import/export library when available.

---

## Git Workflow Rules

- Work starts from `main`, create `develop`, then create one branch per feature from `develop`.
- Branch naming format: `feature/<function-name>`, for example `feature/login`, `feature/page-controller`, `feature/department-management`.
- Commit only completed logical units. Do not commit unrelated changes together.
- Commit message format:
  - `feat: add login feature`
  - `feat: add page controller session management`
  - `feat: add department import feature`
  - `fix: correct department import validation`
  - `docs: add project implementation plan`
- After each feature commit, merge into `develop` using `--no-ff`.
- Keep `main` stable. Merge `develop` into `main` only after v1 verification.

## Scope Decisions

- V1 has only two account types: `Admin` and `User`.
- V1 does not include file evidence upload. Do not create `TepMinhChung`, upload views, or upload storage paths.
- V1 does not include approval/review workflow. Do not create `Duyet`, `TraLai`, `NguoiDuyetId`, `NgayDuyet`, or `LyDoTraLai`.
- Report status flow is only: `Nhap` -> `DaGui` -> `DaKhoa`; `QuaHan` is derived or marked when deadline passes.
- Admin manages master data and views all reports. User only sees and edits reports for their own `KhoaPhongId`.

## Planned File Structure

- `Controllers/PageController.cs`: base controller for Session, login checks, role checks, and department scope helpers.
- `Controllers/AccountController.cs`: login, logout, change password.
- `Controllers/KhoaPhongController.cs`: department CRUD and Excel import.
- `Controllers/NhanVienController.cs`: employee CRUD/import.
- `Controllers/ChiSoChatLuongController.cs`: indicator catalogue CRUD.
- `Controllers/PhanCongChiSoController.cs`: assign indicators to departments.
- `Controllers/KyBaoCaoController.cs`: reporting period management.
- `Controllers/BaoCaoController.cs`: User data entry and Admin report listing.
- `Controllers/DashboardController.cs`: Admin/User dashboard.
- `Models/Entities/*.cs`: database entities or POCOs.
- `Models/ViewModels/*.cs`: form/list/dashboard view models.
- `Services/*.cs`: authentication, import, indicator calculation, report state changes, dashboard queries.
- `App_Data/Sql/*.sql`: schema and seed scripts.
- `Views/<ControllerName>/*.cshtml`: Razor views per module.

## Database Design

### Core Tables

- `KhoaPhong`: `KhoaPhongId`, `IdKhoaPhongNguon`, `TenKhoaPhong`, `Used`, `GhiChu`, `NgayTao`, `NgayCapNhat`.
  - Unique: `IdKhoaPhongNguon`.
  - Import columns must match `DM_KHOA_PHONG.xlsx`: `ID`, `IDKHOAPHONG`, `TENKHOAPHONG`, `USED`.
- `NhanVien`: `NhanVienId`, `MaNhanVien`, `HoTen`, `NgaySinh`, `GioiTinh`, `ChucVu`, `Email`, `SoDienThoai`, `KhoaPhongId`, `DangHoatDong`.
- `TaiKhoan`: `TaiKhoanId`, `TenDangNhap`, `MatKhauHash`, `LoaiTaiKhoan`, `NhanVienId`, `KhoaPhongId`, `DangHoatDong`, `LanDangNhapCuoi`.
  - `LoaiTaiKhoan`: `1 = Admin`, `2 = User`.
  - User accounts must have `KhoaPhongId`.
- `ChiSoChatLuong`: stores indicator definition, formula description, numerator/denominator description, data source, reporting frequency, formula type, and active status.
- `ChiSoMucTieu`: target value per indicator/year.
- `PhanCongChiSo`: assigns active indicators to departments.
- `KyBaoCao`: period name, period type, date range, deadline, status.
- `BaoCao`: one report per `KyBaoCaoId + KhoaPhongId + ChiSoChatLuongId`.
- `BaoCaoChiTiet`: numerator, denominator, direct value, computed result, target pass/fail, note.
- `ThongBao`, `ThongBaoNguoiNhan`: manual and system notifications.
- `LichSuImport`, `LichSuImportChiTiet`: import history and row errors.
- `NhatKyHeThong`: important user actions.

### Required Constraints

- `KhoaPhong.IdKhoaPhongNguon` unique.
- `NhanVien.MaNhanVien` unique.
- `TaiKhoan.TenDangNhap` unique.
- `ChiSoChatLuong.MaChiSo` unique.
- `BaoCao` unique by `KyBaoCaoId`, `KhoaPhongId`, `ChiSoChatLuongId`.
- `BaoCaoChiTiet.BaoCaoId` unique.
- `KyBaoCao.TuNgay <= KyBaoCao.DenNgay`.
- `KyBaoCao.HanNop >= KyBaoCao.DenNgay`.
- No foreign key or table for evidence files in v1.

## Task 1: Database Foundation

**Branch:** `feature/database-foundation`  
**Commit:** `feat: add database foundation`

- [ ] Create `Models/Enums/SystemEnums.cs` with enums for account type, formula type, reporting frequency, period status, report status, notification type, import type.
- [ ] Create database entity/POCO classes for the core tables listed above.
- [ ] Create `App_Data/Sql/001_CreateSchema.sql` with SQL Server table definitions and constraints.
- [ ] Exclude evidence-file schema entirely.
- [ ] Run build command:

```powershell
dotnet msbuild HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU
```

Expected in a full Visual Studio environment: build succeeds. If this environment lacks `Microsoft.WebApplication.targets`, record that as a toolchain blocker and verify in Visual Studio.

## Task 2: PageController Session Foundation

**Branch:** `feature/page-controller`  
**Commit:** `feat: add page controller session management`

- [ ] Create `Controllers/PageController.cs`.
- [ ] Add properties: `CurrentTaiKhoanId`, `CurrentTenDangNhap`, `CurrentLoaiTaiKhoan`, `CurrentNhanVienId`, `CurrentKhoaPhongId`, `CurrentTenKhoaPhong`, `IsAdmin`, `IsUser`.
- [ ] Override `OnActionExecuting` to redirect unauthenticated users to `Account/Login`.
- [ ] Add protected methods:
  - `RequireAdmin()`: returns `HttpUnauthorizedResult` when current user is not Admin.
  - `EnsureUserDepartment(int khoaPhongId)`: blocks User access to other departments.
  - `SetLoginSession(AuthenticatedUser user)`: writes all required Session keys.
  - `ClearLoginSession()`: clears login Session.
- [ ] Update all secured controllers to inherit `PageController`.
- [ ] Keep `AccountController` as a normal `Controller` for login GET/POST, but use `PageController` helper logic where appropriate after login.

## Task 3: Login Feature

**Branch:** `feature/login`  
**Commit:** `feat: add login feature`

- [ ] Create `AccountController` with `Login`, `Logout`, and `ChangePassword`.
- [ ] Create `LoginViewModel` and `ChangePasswordViewModel`.
- [ ] Create `PasswordHasher` using PBKDF2.
- [ ] Create `AuthService` to load active accounts from `TaiKhoan`, verify password, update last login, and change password.
- [ ] Store required Session values exactly as documented: `TaiKhoanId`, `TenDangNhap`, `LoaiTaiKhoan`, `NhanVienId`, `KhoaPhongId`, `TenKhoaPhong`.
- [ ] Update `_Layout.cshtml` to show login/logout/change password links.
- [ ] Add seed SQL for one Admin account after generating a password hash.

## Task 4: Department Management

**Branch:** `feature/department-management`  
**Commit:** `feat: add department management feature`

- [ ] Create department list, create, edit, lock/unlock pages.
- [ ] Create Excel import service for `DM_KHOA_PHONG.xlsx`.
- [ ] Validate exact columns: `ID`, `IDKHOAPHONG`, `TENKHOAPHONG`, `USED`.
- [ ] Reject rows where `IDKHOAPHONG` is empty, not positive integer, or duplicated in the file.
- [ ] Reject rows where `TENKHOAPHONG` is empty.
- [ ] Reject rows where `USED` is not `0` or `1`.
- [ ] Upsert by `IDKHOAPHONG`: existing rows update name/status; new rows insert.
- [ ] Treat `USED = 0` as inactive, never delete old data.

## Task 5: Employee Management

**Branch:** `feature/employee-management`  
**Commit:** `feat: add employee management feature`

- [ ] Create employee list, create, edit, lock/unlock pages.
- [ ] Filter employees by department.
- [ ] Import employees for whole hospital or selected department.
- [ ] Validate `MaNhanVien`, `HoTen`, and department reference.
- [ ] Allow Admin to create a User account from an employee.

## Task 6: Indicator Catalogue

**Branch:** `feature/indicator-catalog`  
**Commit:** `feat: add indicator catalog feature`

- [ ] Create indicator list, create, edit, detail, lock/unlock pages.
- [ ] Store all fields from `danh_sach_chuc_nang_admin_user.md`.
- [ ] Support formula types: percentage, count, average time, average score, direct value, ratio.
- [ ] Store targets in `ChiSoMucTieu` by year.
- [ ] Import initial indicator data only after reviewing the “55 indicators” document because extracted content may contain more than 55 items.

## Task 7: Indicator Assignment

**Branch:** `feature/indicator-assignment`  
**Commit:** `feat: add indicator assignment feature`

- [ ] Create assignment pages by department and by indicator.
- [ ] Allow Admin to assign one or many indicators to one department.
- [ ] Deactivate assignments instead of deleting.
- [ ] User queries must only return active assignments for the user’s `KhoaPhongId`.

## Task 8: Reporting Periods

**Branch:** `feature/reporting-periods`  
**Commit:** `feat: add reporting period feature`

- [ ] Create reporting period list, create, edit, open, lock pages.
- [ ] Validate date range and deadline.
- [ ] Support monthly, quarterly, six-month, yearly, daily, weekly, and event-based periods.
- [ ] Show completion progress per period.

## Task 9: Report Entry Without Approval Workflow

**Branch:** `feature/report-entry`  
**Commit:** `feat: add report entry feature`

- [ ] User sees assigned indicators for their own department.
- [ ] User creates or edits reports only while status is `Nhap`.
- [ ] User submits a report, changing status from `Nhap` to `DaGui`.
- [ ] Admin can view all submitted reports and lock reports, changing status to `DaKhoa`.
- [ ] System marks or displays `QuaHan` when `HanNop` has passed and no submitted report exists.
- [ ] Do not implement approve/return actions in v1.
- [ ] Do not implement file evidence upload in v1.

## Task 10: Indicator Calculation

**Branch:** `feature/indicator-calculation`  
**Commit:** `feat: add indicator calculation service`

- [ ] Create calculation service for `BaoCaoChiTiet`.
- [ ] Percentage: `TuSo / MauSo * 100`, reject `MauSo = 0`.
- [ ] Count/direct value: `KetQua = GiaTriNhap`.
- [ ] Average time and ratio: `KetQua = TuSo / MauSo`, reject `MauSo = 0`.
- [ ] Compare result with yearly target using `ToanTuSoSanh` and set `DatMucTieu`.

## Task 11: Notifications

**Branch:** `feature/notifications`  
**Commit:** `feat: add notification feature`

- [ ] Admin can send manual notifications to one or more departments.
- [ ] System can create due-soon, overdue, submitted, and locked-report notifications.
- [ ] User can view notifications and mark them as read.
- [ ] No approval/return notification type in v1.

## Task 12: Dashboard

**Branch:** `feature/dashboard`  
**Commit:** `feat: add dashboard feature`

- [ ] Admin dashboard shows total indicators, submitted reports, missing reports, overdue reports, and department progress.
- [ ] User dashboard shows assigned indicators and report states for their department.
- [ ] Use Chart.js for basic charts.
- [ ] Filters: period, department, indicator.

## Task 13: Excel Export

**Branch:** `feature/excel-export`  
**Commit:** `feat: add excel export feature`

- [ ] Export departments, employees, indicator catalogue, period reports, department reports, and hospital summary reports.
- [ ] Admin can export all data.
- [ ] User can export only reports for their own department.
- [ ] PDF export is not required for v1.

## Task 14: Final Integration

**Branch:** `develop` then `main` after verification  
**Commit:** merge commits only

- [ ] Run schema script on local SQL Server.
- [ ] Seed Admin account and baseline departments.
- [ ] Run the app in Visual Studio/IIS Express.
- [ ] Verify login, department import, indicator assignment, report entry, dashboard, and export.
- [ ] Merge `develop` into `main` only after v1 acceptance.

## Acceptance Checklist

- [ ] Every secured controller except login inherits from `PageController`.
- [ ] User cannot access data for another department.
- [ ] Admin can manage all master data.
- [ ] Department import follows `DM_KHOA_PHONG.xlsx` rules.
- [ ] Reports do not require approval in v1.
- [ ] No file evidence table, controller action, view, or upload directory exists in v1.
- [ ] Git history uses `develop` and `feature/*` branches with focused commits.
- [ ] Build is verified in Visual Studio if CLI MSBuild lacks WebApplication targets.
