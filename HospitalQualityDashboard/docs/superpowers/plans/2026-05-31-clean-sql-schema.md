# Clean SQL Schema Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the scattered SQL migration files with one clean database bootstrap script for a fresh SQL Server database.

**Architecture:** Keep database creation in `DatabaseBootstrapper`, but run a single full schema script when the base table is missing. The SQL script owns table creation, constraints, indexes, and the default admin seed.

**Tech Stack:** ASP.NET MVC4, .NET Framework 4.7.2, ADO.NET, SQL Server LocalDB.

---

### Task 1: Consolidate SQL

**Files:**
- Modify: `App_Data/Sql/001_CreateSchema.sql`
- Delete: `App_Data/Sql/002_SeedAdmin.sql`
- Delete: `App_Data/Sql/003_AddIndicatorFrequencies.sql`
- Delete: `App_Data/Sql/004_AddAssignmentUniqueConstraint.sql`
- Delete: `App_Data/Sql/005_AddApprovalAndRejection.sql`
- Delete: `App_Data/Sql/006_AddNotificationAutomationLog.sql`

- [ ] Replace `001_CreateSchema.sql` with a complete fresh-database schema.
- [ ] Include `BaoCao.YKienPhanHoi` directly in `dbo.BaoCao`.
- [ ] Set `CK_BaoCao_TrangThai` to `TrangThai IN (1, 2, 3, 4, 5, 6)`.
- [ ] Keep `ChiSoTanSuatBaoCao`, `ThongBaoTuDongLog`, unique constraints, foreign keys, and useful indexes in the base schema.
- [ ] Seed the default `admin` account at the end of the script.

### Task 2: Simplify Bootstrapper

**Files:**
- Modify: `Services/DatabaseBootstrapper.cs`

- [ ] Run only `001_CreateSchema.sql` when `dbo.KhoaPhong` does not exist.
- [ ] Remove old migration-specific branching for `003` through `006`.
- [ ] Remove unused approval-script detection code.

### Task 3: Project And Verification

**Files:**
- Modify: `HospitalQualityDashboard.csproj`
- Modify: `tools/VerifyDatabaseBootstrapper.ps1`

- [ ] Keep only `App_Data\Sql\001_CreateSchema.sql` as SQL content in the project file.
- [ ] Update bootstrapper verification to assert the single-script flow.
- [ ] Run `powershell -ExecutionPolicy Bypass -File .\tools\VerifyDatabaseBootstrapper.ps1`.
- [ ] Run the existing report workflow verification script if it still applies.
- [ ] Build the project with MSBuild when available.
