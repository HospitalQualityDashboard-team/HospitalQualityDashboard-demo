$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ServiceSourceReader.ps1')

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$migrationPath = Join-Path $root 'App_Data\Sql\005_AddIndicatorDeploymentHistory.sql'
$schemaPath = Join-Path $root 'App_Data\Sql\001_CreateSchema.sql'
$projectPath = Join-Path $root 'HospitalQualityDashboard-demo.csproj'

if (-not (Test-Path $migrationPath)) {
    throw 'Indicator deployment migration 005_AddIndicatorDeploymentHistory.sql is missing.'
}

$migration = Get-Content -Raw -Path $migrationPath
$schema = Get-Content -Raw -Path $schemaPath
$project = Get-Content -Raw -Path $projectPath
$bootstrapper = Get-Content -Raw -Path (Join-Path $root 'Services\Infrastructure\Database\DatabaseBootstrapper.cs')
$global = Get-Content -Raw -Path (Join-Path $root 'Global.asax.cs')
$indicatorController = Get-Content -Raw -Path (Join-Path $root 'Areas\Admin\Controllers\IndicatorController.cs')
$indicatorIndex = Get-Content -Raw -Encoding UTF8 -Path (Join-Path $root 'Areas\Admin\Views\Indicator\Index.cshtml')
$indicatorEdit = Get-Content -Raw -Encoding UTF8 -Path (Join-Path $root 'Areas\Admin\Views\Indicator\Edit.cshtml')
$indicatorService = Get-ServiceSource -Root $root -Patterns 'Services\Indicators\IndicatorService*.cs'
$reportService = Get-ServiceSource -Root $root -Patterns 'Services\Reports\ReportService.cs'
$dashboardService = Get-ServiceSource -Root $root -Patterns 'Services\Dashboards\DashboardService*.cs'
$dashboardExportService = Get-ServiceSource -Root $root -Patterns 'Services\Dashboards\Export\DashboardExcelExportService*.cs'
$notificationService = Get-ServiceSource -Root $root -Patterns 'Services\Notifications\NotificationAutomationService.cs'
$exportService = Get-ServiceSource -Root $root -Patterns 'Services\Exports\ExportService.cs'

$checks = @(
    @{ Source = $migration; Token = 'CREATE TABLE dbo.LichSuTrienKhaiChiSo' },
    @{ Source = $migration; Token = 'fn_ChiSoDuocTrienKhaiTrongKy' },
    @{ Source = $migration; Token = 'TuNgayApDung' },
    @{ Source = $migration; Token = 'DenNgayApDung' },
    @{ Source = $migration; Token = 'NguoiKetThucId' },
    @{ Source = $migration; Token = 'WHERE cs.DangHoatDong = 1' },
    @{ Source = $schema; Token = 'CREATE TABLE dbo.LichSuTrienKhaiChiSo' },
    @{ Source = $schema; Token = 'fn_ChiSoDuocTrienKhaiTrongKy' },
    @{ Source = $project; Token = 'App_Data\Sql\005_AddIndicatorDeploymentHistory.sql' },
    @{ Source = $project; Token = 'tools\VerifyIndicatorDeploymentLifecycle.ps1' },
    @{ Source = $bootstrapper; Token = '005_AddIndicatorDeploymentHistory.sql' },
    @{ Source = $bootstrapper; Token = 'EnsureIndicatorDeploymentLifecycle' },
    @{ Source = $global; Token = 'DatabaseBootstrapper.EnsureIndicatorDeploymentLifecycle();' },
    @{ Source = $indicatorController; Token = 'public ActionResult StopDeployment' },
    @{ Source = $indicatorController; Token = 'public ActionResult Deploy' },
    @{ Source = $indicatorService; Token = 'StopDeployment(int id, int userId)' },
    @{ Source = $indicatorService; Token = 'Deploy(int id, int userId)' },
    @{ Source = $indicatorService; Token = 'EnsureDeploymentHistory' },
    @{ Source = $indicatorService; Token = 'LogIndicatorDeploymentAction' },
    @{ Source = $indicatorIndex; Token = 'StopDeployment' },
    @{ Source = $indicatorIndex; Token = 'Deploy' },
    @{ Source = $indicatorIndex; Token = 'status-active' },
    @{ Source = $indicatorIndex; Token = 'status-muted' },
    @{ Source = $indicatorEdit; Token = 'DangHoatDong' },
    @{ Source = $indicatorEdit; Token = 'type="hidden"' },
    @{ Source = $reportService; Token = 'dbo.fn_ChiSoDuocTrienKhaiTrongKy' },
    @{ Source = $dashboardService; Token = 'dbo.fn_ChiSoDuocTrienKhaiTrongKy' },
    @{ Source = $dashboardExportService; Token = 'dbo.fn_ChiSoDuocTrienKhaiTrongKy' },
    @{ Source = $notificationService; Token = 'dbo.fn_ChiSoDuocTrienKhaiTrongKy' },
    @{ Source = $exportService; Token = 'dbo.fn_ChiSoDuocTrienKhaiTrongKy' }
)

foreach ($check in $checks) {
    if ($check.Source -notmatch [regex]::Escape($check.Token)) {
        throw "Indicator deployment lifecycle contract is missing: $($check.Token)"
    }
}

if ($dashboardService -notmatch [regex]::Escape('Param("@DraftPeriodStatus", (byte)TrangThaiKyBaoCao.Nhap)')) {
    throw 'Dashboard deployment queries must pass @DraftPeriodStatus when filtering deployed slots.'
}

if ($indicatorIndex -match [regex]::Escape('"Lock"') -or $indicatorIndex -match [regex]::Escape('"Unlock"')) {
    throw 'Indicator index must replace old lock/unlock wording with deployment wording.'
}

Write-Host 'Indicator deployment lifecycle structural verification passed.'
