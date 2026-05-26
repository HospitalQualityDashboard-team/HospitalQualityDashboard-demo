# Indicator Import Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add bulk import for quality indicators, support optional targets and auto-generated indicator codes, and fix Excel parsing for department import files.

**Architecture:** Keep the existing ASP.NET MVC 5 and ADO.NET service pattern. Fix `ExcelImportExportService` as the shared import boundary, add import orchestration inside `IndicatorService`, and expose it through `IndicatorController` plus the indicator index view.

**Tech Stack:** ASP.NET MVC 5, .NET Framework 4.7.2, ADO.NET, SQL Server LocalDB, Razor, PowerShell verification script.

---

### Task 1: Excel Parser Regression Coverage

**Files:**
- Create: `HospitalQualityDashboard/tools/VerifyExcelParser.ps1`
- Modify: `HospitalQualityDashboard/Services/ExcelImportExportService.cs`

- [ ] Write a PowerShell verification script that compiles `ExcelImportExportService.cs`, creates an in-memory `.xlsx` where headers sit in `A1` and `C1`, and asserts that `TENKHOAPHONG` is read from column `C`.
- [ ] Run `powershell -ExecutionPolicy Bypass -File HospitalQualityDashboard/tools/VerifyExcelParser.ps1` and confirm it fails before the parser fix.
- [ ] Change `ReadXlsx` to preserve the real column reference for each header instead of compressing headers into contiguous indexes.
- [ ] Add `inlineStr` support in `ReadCellValue`.
- [ ] Re-run the script and confirm it passes.

### Task 2: Indicator Import Service

**Files:**
- Modify: `HospitalQualityDashboard/Models/Enums/SystemEnums.cs`
- Modify: `HospitalQualityDashboard/Models/ViewModels/AppViewModels.cs`
- Modify: `HospitalQualityDashboard/Services/IndicatorPeriodServices.cs`

- [ ] Add `ChinThang` to `TanSuatBaoCao`.
- [ ] Add an index view model for indicator import results.
- [ ] Add `IndicatorService.Import(file, userId)` that accepts rows with optional `MaChiSo`, optional `SoThuTu`, optional target fields, and multi-department `KhoaPhongQuanLy`.
- [ ] Generate new indicator codes after insert from `ChiSoChatLuongId` using `CS0001` format when `MaChiSo` is absent.
- [ ] Resolve existing rows by `MaChiSo` first, then exact `TenChiSo`.
- [ ] Parse frequency text including `Mỗi tháng`, `Hàng tháng`, `3 tháng`, `6 tháng`, `9 tháng`, and `12 tháng`.
- [ ] Upsert active assignments in `PhanCongChiSo` for all resolved departments.
- [ ] Log import summary in `LichSuImport`.

### Task 3: MVC Entry Point

**Files:**
- Modify: `HospitalQualityDashboard/Controllers/IndicatorController.cs`
- Modify: `HospitalQualityDashboard/Views/Indicator/Index.cshtml`

- [ ] Return the new index view model from `IndicatorController.Index`.
- [ ] Add `IndicatorController.Import` POST action guarded by admin permission and anti-forgery validation.
- [ ] Add an upload form and import result summary to the indicator index view.
- [ ] Keep existing create/edit/lock/unlock behavior unchanged.

### Task 4: Verification

**Files:**
- Verify only.

- [ ] Run `powershell -ExecutionPolicy Bypass -File HospitalQualityDashboard/tools/VerifyExcelParser.ps1`.
- [ ] Run `dotnet msbuild HospitalQualityDashboard/HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU`.
- [ ] If CLI build is blocked by missing MVC WebApplication targets, report that exact limitation and the parser verification result.
