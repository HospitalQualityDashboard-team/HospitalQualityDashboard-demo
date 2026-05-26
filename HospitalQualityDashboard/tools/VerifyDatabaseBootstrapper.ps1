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
Assert-Contains $bootstrapper 'ConnectionName = "HospitalQualityConnection"' 'Bootstrapper must define HospitalQualityConnection as the configured connection name.'
Assert-Contains $bootstrapper 'ConfigurationManager\.ConnectionStrings\[ConnectionName\]' 'Bootstrapper must read the configured HospitalQualityConnection connection string.'
Assert-Contains $bootstrapper 'SqlConnectionStringBuilder' 'Bootstrapper must parse connection strings with SqlConnectionStringBuilder.'
Assert-Contains $bootstrapper 'InitialCatalog = "master"' 'Bootstrapper must connect to master before creating the target database.'
Assert-Contains $bootstrapper 'CREATE DATABASE' 'Bootstrapper must create the configured database when missing.'
Assert-Contains $bootstrapper 'ObjectExists\(connection, "dbo\.KhoaPhong"\)' 'Bootstrapper must use dbo.KhoaPhong as the base schema marker.'
Assert-Contains $bootstrapper 'SplitSqlBatches' 'Bootstrapper must split SQL scripts into GO-delimited batches.'
Assert-Contains $bootstrapper '005_AddApprovalAndRejection\.sql' 'Bootstrapper must handle the approval/rejection schema update.'

Write-Host 'Database bootstrapper verification passed.'
