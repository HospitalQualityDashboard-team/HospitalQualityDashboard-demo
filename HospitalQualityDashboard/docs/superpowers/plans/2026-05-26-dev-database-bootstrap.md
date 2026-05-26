# Dev Database Bootstrap Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make IIS Express Debug startup create and initialize the configured SQL Server database for local developers.

**Architecture:** Add a focused `DatabaseBootstrapper` startup service that reads `HospitalQualityConnection`, creates the target database through `master`, then runs the existing SQL scripts from `App_Data/Sql`. `Global.asax.cs` calls it before normal MVC registration, and the bootstrapper exits immediately unless ASP.NET debugging is enabled.

**Tech Stack:** ASP.NET MVC 4, .NET Framework 4.7.2, ADO.NET `System.Data.SqlClient`, SQL Server/LocalDB, PowerShell verification.

---

## File Structure

- Create `HospitalQualityDashboard/Services/DatabaseBootstrapper.cs`
  - Owns all dev-only database bootstrap behavior.
  - Does not change normal service query behavior.
- Modify `HospitalQualityDashboard/Global.asax.cs`
  - Calls `DatabaseBootstrapper.BootstrapIfDebug()` at application startup.
- Modify `HospitalQualityDashboard/HospitalQualityDashboard.csproj`
  - Includes the new C# source file in the legacy MVC project.
- Create `HospitalQualityDashboard/tools/VerifyDatabaseBootstrapper.ps1`
  - Static verification for the startup hook and important bootstrap safety checks.

### Task 1: Add Static Verification Script

**Files:**
- Create: `HospitalQualityDashboard/tools/VerifyDatabaseBootstrapper.ps1`

- [ ] **Step 1: Create the failing verification script**

Create `HospitalQualityDashboard/tools/VerifyDatabaseBootstrapper.ps1` with this content:

```powershell
param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
)

$ErrorActionPreference = 'Stop'

function Assert-Contains {
    param(
        [string]$Content,
        [string]$Pattern,
        [string]$Message
    )

    if ($Content -notmatch $Pattern) {
        throw $Message
    }
}

$bootstrapperPath = Join-Path $ProjectRoot 'Services\DatabaseBootstrapper.cs'
$globalPath = Join-Path $ProjectRoot 'Global.asax.cs'
$projectPath = Join-Path $ProjectRoot 'HospitalQualityDashboard.csproj'

if (-not (Test-Path $bootstrapperPath)) {
    throw 'Services\DatabaseBootstrapper.cs must exist.'
}

$bootstrapper = Get-Content -Raw $bootstrapperPath
$global = Get-Content -Raw $globalPath
$project = Get-Content -Raw $projectPath

Assert-Contains $global 'DatabaseBootstrapper\.BootstrapIfDebug\(\);' 'Global.asax.cs must call DatabaseBootstrapper.BootstrapIfDebug().'
Assert-Contains $project '<Compile Include="Services\\DatabaseBootstrapper.cs" />' 'HospitalQualityDashboard.csproj must compile Services\DatabaseBootstrapper.cs.'
Assert-Contains $bootstrapper 'HttpContext\.Current\.IsDebuggingEnabled' 'Bootstrapper must be gated by ASP.NET debug mode.'
Assert-Contains $bootstrapper 'ConfigurationManager\.ConnectionStrings\["HospitalQualityConnection"\]' 'Bootstrapper must use HospitalQualityConnection.'
Assert-Contains $bootstrapper 'SqlConnectionStringBuilder' 'Bootstrapper must parse connection strings with SqlConnectionStringBuilder.'
Assert-Contains $bootstrapper 'InitialCatalog = "master"' 'Bootstrapper must connect to master before creating the target database.'
Assert-Contains $bootstrapper 'CREATE DATABASE' 'Bootstrapper must create the configured database when missing.'
Assert-Contains $bootstrapper 'ObjectExists\(connection, "dbo\.KhoaPhong"\)' 'Bootstrapper must use dbo.KhoaPhong as the base schema marker.'
Assert-Contains $bootstrapper 'SplitSqlBatches' 'Bootstrapper must split SQL scripts into GO-delimited batches.'
Assert-Contains $bootstrapper '005_AddApprovalAndRejection\.sql' 'Bootstrapper must handle the approval/rejection schema update.'

Write-Host 'Database bootstrapper verification passed.'
```

- [ ] **Step 2: Run verification to confirm it fails before implementation**

Run from `HospitalQualityDashboard` web project directory:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\VerifyDatabaseBootstrapper.ps1
```

Expected: fails with `Services\DatabaseBootstrapper.cs must exist.`

- [ ] **Step 3: Commit verification script**

Run:

```powershell
git add HospitalQualityDashboard/tools/VerifyDatabaseBootstrapper.ps1
git commit -m "Add database bootstrapper verification"
```

Expected: commit succeeds.

### Task 2: Add Database Bootstrapper Service

**Files:**
- Create: `HospitalQualityDashboard/Services/DatabaseBootstrapper.cs`

- [ ] **Step 1: Create `DatabaseBootstrapper.cs`**

Create `HospitalQualityDashboard/Services/DatabaseBootstrapper.cs` with this complete content:

```csharp
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;

namespace HospitalQualityDashboard.Services
{
    public static class DatabaseBootstrapper
    {
        private const string ConnectionName = "HospitalQualityConnection";
        private const int CommandTimeoutSeconds = 120;

        public static void BootstrapIfDebug()
        {
            if (!IsDebugMode())
            {
                return;
            }

            var connectionSettings = ConfigurationManager.ConnectionStrings[ConnectionName];
            if (connectionSettings == null || string.IsNullOrWhiteSpace(connectionSettings.ConnectionString))
            {
                throw new InvalidOperationException("Missing connection string: " + ConnectionName + ".");
            }

            var databaseConnection = new SqlConnectionStringBuilder(connectionSettings.ConnectionString);
            var databaseName = databaseConnection.InitialCatalog;
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new InvalidOperationException("Connection string " + ConnectionName + " must include Initial Catalog or Database.");
            }

            var scriptDirectory = HostingEnvironment.MapPath("~/App_Data/Sql");
            if (string.IsNullOrWhiteSpace(scriptDirectory) || !Directory.Exists(scriptDirectory))
            {
                throw new InvalidOperationException("SQL script directory was not found: ~/App_Data/Sql.");
            }

            EnsureDatabaseExists(databaseConnection, databaseName);
            RunSchemaScripts(databaseConnection.ConnectionString, scriptDirectory);
        }

        private static bool IsDebugMode()
        {
            return HttpContext.Current != null && HttpContext.Current.IsDebuggingEnabled;
        }

        private static void EnsureDatabaseExists(SqlConnectionStringBuilder databaseConnection, string databaseName)
        {
            var masterConnection = new SqlConnectionStringBuilder(databaseConnection.ConnectionString)
            {
                InitialCatalog = "master"
            };

            try
            {
                using (var connection = new SqlConnection(masterConnection.ConnectionString))
                {
                    connection.Open();
                    if (!DatabaseExists(connection, databaseName))
                    {
                        ExecuteNonQuery(connection, "CREATE DATABASE " + QuoteIdentifier(databaseName));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create or verify database '" + databaseName + "'. Check SQL Server name, login permissions, and whether the server is running.", ex);
            }
        }

        private static bool DatabaseExists(SqlConnection connection, string databaseName)
        {
            using (var command = new SqlCommand("SELECT DB_ID(@DatabaseName)", connection))
            {
                command.CommandTimeout = CommandTimeoutSeconds;
                command.Parameters.AddWithValue("@DatabaseName", databaseName);
                var result = command.ExecuteScalar();
                return result != DBNull.Value && result != null;
            }
        }

        private static void RunSchemaScripts(string connectionString, string scriptDirectory)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                if (!ObjectExists(connection, "dbo.KhoaPhong"))
                {
                    RunScript(connection, scriptDirectory, "001_CreateSchema.sql");
                }

                RunScript(connection, scriptDirectory, "002_SeedAdmin.sql");
                RunScript(connection, scriptDirectory, "003_AddIndicatorFrequencies.sql");
                RunScript(connection, scriptDirectory, "004_AddAssignmentUniqueConstraint.sql");

                if (ShouldRunApprovalScript(connection))
                {
                    RunScript(connection, scriptDirectory, "005_AddApprovalAndRejection.sql");
                }

                RunScript(connection, scriptDirectory, "006_AddNotificationAutomationLog.sql");
            }
        }

        private static bool ShouldRunApprovalScript(SqlConnection connection)
        {
            if (!ObjectExists(connection, "dbo.BaoCao"))
            {
                return false;
            }

            var hasFeedbackColumn = Convert.ToInt32(ExecuteScalar(connection, "SELECT CASE WHEN COL_LENGTH('dbo.BaoCao', 'YKienPhanHoi') IS NULL THEN 0 ELSE 1 END")) == 1;
            var constraintDefinition = ExecuteScalar(connection, @"
SELECT definition
FROM sys.check_constraints
WHERE name = N'CK_BaoCao_TrangThai'
  AND parent_object_id = OBJECT_ID(N'dbo.BaoCao')") as string;

            var hasUpdatedStatusConstraint = !string.IsNullOrWhiteSpace(constraintDefinition)
                && Regex.IsMatch(constraintDefinition, @"\b6\b");

            return !hasFeedbackColumn || !hasUpdatedStatusConstraint;
        }

        private static bool ObjectExists(SqlConnection connection, string objectName)
        {
            using (var command = new SqlCommand("SELECT OBJECT_ID(@ObjectName, N'U')", connection))
            {
                command.CommandTimeout = CommandTimeoutSeconds;
                command.Parameters.AddWithValue("@ObjectName", objectName);
                var result = command.ExecuteScalar();
                return result != DBNull.Value && result != null;
            }
        }

        private static void RunScript(SqlConnection connection, string scriptDirectory, string fileName)
        {
            var scriptPath = Path.Combine(scriptDirectory, fileName);
            if (!File.Exists(scriptPath))
            {
                throw new InvalidOperationException("Required SQL script was not found: " + scriptPath);
            }

            try
            {
                var script = File.ReadAllText(scriptPath, Encoding.UTF8);
                foreach (var batch in SplitSqlBatches(script))
                {
                    ExecuteNonQuery(connection, batch);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to execute SQL script: " + fileName + ".", ex);
            }
        }

        private static IEnumerable<string> SplitSqlBatches(string script)
        {
            var batch = new StringBuilder();
            using (var reader = new StringReader(script))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.Equals(line.Trim(), "GO", StringComparison.OrdinalIgnoreCase))
                    {
                        if (batch.Length > 0)
                        {
                            yield return batch.ToString();
                            batch.Clear();
                        }
                    }
                    else
                    {
                        batch.AppendLine(line);
                    }
                }
            }

            if (batch.Length > 0)
            {
                yield return batch.ToString();
            }
        }

        private static object ExecuteScalar(SqlConnection connection, string sql)
        {
            using (var command = new SqlCommand(sql, connection))
            {
                command.CommandTimeout = CommandTimeoutSeconds;
                return command.ExecuteScalar();
            }
        }

        private static void ExecuteNonQuery(SqlConnection connection, string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return;
            }

            using (var command = new SqlCommand(sql, connection))
            {
                command.CommandTimeout = CommandTimeoutSeconds;
                command.ExecuteNonQuery();
            }
        }

        private static string QuoteIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier) || identifier.IndexOf('\0') >= 0)
            {
                throw new InvalidOperationException("Invalid SQL Server database name.");
            }

            return "[" + identifier.Replace("]", "]]") + "]";
        }
    }
}
```

- [ ] **Step 2: Commit bootstrapper service**

Run:

```powershell
git add HospitalQualityDashboard/Services/DatabaseBootstrapper.cs
git commit -m "Add dev database bootstrapper"
```

Expected: commit succeeds.

### Task 3: Wire Bootstrapper Into MVC Startup

**Files:**
- Modify: `HospitalQualityDashboard/Global.asax.cs`
- Modify: `HospitalQualityDashboard/HospitalQualityDashboard.csproj`

- [ ] **Step 1: Update `Global.asax.cs`**

Replace `HospitalQualityDashboard/Global.asax.cs` with:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            DatabaseBootstrapper.BootstrapIfDebug();

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
```

- [ ] **Step 2: Update `HospitalQualityDashboard.csproj`**

In `HospitalQualityDashboard/HospitalQualityDashboard.csproj`, add this compile include immediately after `Services\AuthService.cs`:

```xml
    <Compile Include="Services\DatabaseBootstrapper.cs" />
```

Expected surrounding block:

```xml
    <Compile Include="Services\AuthService.cs" />
    <Compile Include="Services\DatabaseBootstrapper.cs" />
    <Compile Include="Services\DbServiceBase.cs" />
```

- [ ] **Step 3: Run static verification**

Run from `HospitalQualityDashboard` web project directory:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\VerifyDatabaseBootstrapper.ps1
```

Expected: `Database bootstrapper verification passed.`

- [ ] **Step 4: Commit startup wiring**

Run:

```powershell
git add HospitalQualityDashboard/Global.asax.cs HospitalQualityDashboard/HospitalQualityDashboard.csproj
git commit -m "Wire dev database bootstrapper into startup"
```

Expected: commit succeeds.

### Task 4: Build And Manual SQL Verification

**Files:**
- Verify only.

- [ ] **Step 1: Restore packages if needed**

Run from repository root:

```powershell
nuget restore .\HospitalQualityDashboard\HospitalQualityDashboard.csproj -PackagesDirectory .\packages
```

Expected: packages restore or report that packages are already installed.

- [ ] **Step 2: Build the project**

Run from repository root:

```powershell
msbuild .\HospitalQualityDashboard\HospitalQualityDashboard.csproj /p:Configuration=Debug /p:Platform=AnyCPU
```

Expected: build succeeds with `0 Error(s)`.

- [ ] **Step 3: Verify a fresh database name manually**

Temporarily change the database name in `HospitalQualityDashboard/Web.config` from:

```xml
Initial Catalog=HospitalQualityDashboard;
```

to:

```xml
Initial Catalog=HospitalQualityDashboard_BootstrapSmoke;
```

Start IIS Express from Visual Studio or through the existing project launch profile. Expected:

- SQL Server creates database `HospitalQualityDashboard_BootstrapSmoke`.
- Tables such as `dbo.KhoaPhong`, `dbo.TaiKhoan`, and `dbo.BaoCao` exist.
- `dbo.TaiKhoan` contains the default `admin` account.
- Restarting IIS Express a second time does not fail.

- [ ] **Step 4: Revert only the temporary smoke-test connection string change**

Restore `HospitalQualityDashboard/Web.config` to:

```xml
Initial Catalog=HospitalQualityDashboard;
```

Expected: `git diff -- HospitalQualityDashboard/Web.config` is empty after reverting the smoke-test database name.

- [ ] **Step 5: Final status check**

Run:

```powershell
git status --short
```

Expected: only intended source, project, tool, spec, or plan files are changed.
