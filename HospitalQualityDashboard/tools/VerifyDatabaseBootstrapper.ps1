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
Assert-Contains $bootstrapper '001_CreateSchema\.sql' 'Bootstrapper must run the consolidated schema script.'

if ($bootstrapper -match '00[2-6]_.*\.sql') {
    throw 'Bootstrapper must not reference removed migration SQL files.'
}

Assert-Contains $project '<Content Include="App_Data\\Sql\\001_CreateSchema.sql" />' 'HospitalQualityDashboard.csproj must include the consolidated schema script.'

if ($project -match 'App_Data\\Sql\\00[2-6]_.*\.sql') {
    throw 'HospitalQualityDashboard.csproj must not include removed migration SQL files.'
}

$schemaPath = Join-Path $ProjectRoot 'App_Data\Sql\001_CreateSchema.sql'
if (-not (Test-Path $schemaPath)) {
    throw 'App_Data\Sql\001_CreateSchema.sql must exist.'
}

$schema = Get-Content -Raw $schemaPath
Assert-Contains $schema 'YKienPhanHoi NVARCHAR\(MAX\) NULL' 'Consolidated schema must include BaoCao.YKienPhanHoi.'
Assert-Contains $schema 'CK_BaoCao_TrangThai CHECK \(TrangThai IN \(1, 2, 3, 4, 5, 6\)\)' 'Consolidated schema must allow report statuses 1 through 6.'
Assert-Contains $schema 'CREATE TABLE dbo\.ChiSoTanSuatBaoCao' 'Consolidated schema must create ChiSoTanSuatBaoCao.'
Assert-Contains $schema 'CREATE TABLE dbo\.ThongBaoTuDongLog' 'Consolidated schema must create ThongBaoTuDongLog.'
Assert-Contains $schema 'UQ_KyBaoCao_Loai_TuNgay_DenNgay' 'Consolidated schema must prevent duplicate reporting periods.'
Assert-Contains $schema "N'admin'" 'Consolidated schema must seed the default admin account.'

$sqlFiles = Get-ChildItem (Join-Path $ProjectRoot 'App_Data\Sql') -Filter '*.sql'
if ($sqlFiles.Count -ne 1 -or $sqlFiles[0].Name -ne '001_CreateSchema.sql') {
    throw 'App_Data\Sql must contain only 001_CreateSchema.sql.'
}

Write-Host 'Database bootstrapper verification passed.'
