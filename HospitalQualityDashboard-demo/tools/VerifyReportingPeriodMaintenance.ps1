$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot

function Assert-FileExists {
    param(
        [string]$Path,
        [string]$Message
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw $Message
    }
}

function Read-Text {
    param([string]$RelativePath)
    $path = Join-Path $projectRoot $RelativePath
    Assert-FileExists $path "Missing expected file: $RelativePath"
    return Get-Content -Raw -LiteralPath $path
}

function Assert-Contains {
    param(
        [string]$Text,
        [string]$Pattern,
        [string]$Message
    )

    if ($Text -notmatch $Pattern) {
        throw $Message
    }
}

function Assert-Order {
    param(
        [string]$Text,
        [string[]]$Needles,
        [string]$Message
    )

    $lastIndex = -1
    foreach ($needle in $Needles) {
        $index = $Text.IndexOf($needle, [System.StringComparison]::Ordinal)
        if ($index -lt 0 -or $index -le $lastIndex) {
            throw $Message
        }

        $lastIndex = $index
    }
}

$schedule = Read-Text "Services\ReportingPeriods\ReportingPeriodScheduleService.cs"
Assert-Contains $schedule "public\s+int\s+CloseOverduePeriods\s*\(\s*DateTime\s+now\s*\)" "CloseOverduePeriods(DateTime now) must exist."
Assert-Contains $schedule "WHERE\s+TrangThai=@Mo\s+AND\s+HanNop\s+<\s+@Today" "CloseOverduePeriods must only close open periods whose HanNop is before today."
Assert-Contains $schedule "Param\(""@Khoa"",\s*\(byte\)TrangThaiKyBaoCao\.Khoa\)" "CloseOverduePeriods must set periods to Khoa."

$maintenance = Read-Text "Services\ReportingPeriods\ReportingPeriodMaintenanceService.cs"
Assert-Contains $maintenance "public\s+ReportingPeriodMaintenanceResultDto\s+Run\s*\(\s*DateTime\s+now\s*\)" "Maintenance service must expose Run(DateTime now)."
Assert-Order $maintenance @("OpenDuePeriods(now)", "_automation.Run(now)", "CloseOverduePeriods(now)") "Maintenance service must open due periods, run notifications, then close overdue periods."
Assert-Contains $maintenance "NotificationAutomationRan\s*=\s*true" "Maintenance result must report notification automation ran."

$dto = Read-Text "Models\DTOs\ReportingPeriodDtos.cs"
Assert-Contains $dto "class\s+ReportingPeriodMaintenanceResultDto" "ReportingPeriodMaintenanceResultDto must be defined."
Assert-Contains $dto "OpenedCount" "Maintenance result must include OpenedCount."
Assert-Contains $dto "ClosedCount" "Maintenance result must include ClosedCount."
Assert-Contains $dto "NotificationAutomationRan" "Maintenance result must include NotificationAutomationRan."
Assert-Contains $dto "RanAt" "Maintenance result must include RanAt."

$adminDashboard = Read-Text "Areas\Admin\Controllers\DashboardController.cs"
Assert-Contains $adminDashboard "ReportingPeriodMaintenanceService" "Admin dashboard must use ReportingPeriodMaintenanceService."
Assert-Contains $adminDashboard "RunReportingPeriodMaintenance" "Admin dashboard must run reporting-period maintenance."

$userDashboard = Read-Text "Areas\User\Controllers\DashboardController.cs"
Assert-Contains $userDashboard "ReportingPeriodMaintenanceService" "User dashboard must use ReportingPeriodMaintenanceService."
Assert-Contains $userDashboard "RunReportingPeriodMaintenance" "User dashboard must run reporting-period maintenance."

$notificationController = Read-Text "Areas\Admin\Controllers\NotificationController.cs"
Assert-Contains $notificationController "ReportingPeriodMaintenanceService" "Notification controller must use the maintenance service."
Assert-Contains $notificationController "OpenedCount" "Manual automation result must mention opened periods."
Assert-Contains $notificationController "ClosedCount" "Manual automation result must mention closed periods."

$maintenanceController = Read-Text "Controllers\MaintenanceController.cs"
Assert-Contains $maintenanceController "\[HttpPost\]" "Maintenance endpoint must only accept POST."
Assert-Contains $maintenanceController "X-Maintenance-Token" "Maintenance endpoint must accept the scheduler token header."
Assert-Contains $maintenanceController "HospitalQualityMaintenanceToken" "Maintenance endpoint must read the configured token."
Assert-Contains $maintenanceController "HttpStatusCodeResult\(403\)" "Maintenance endpoint must reject missing or invalid tokens with 403."
Assert-Contains $maintenanceController "openedCount" "Maintenance endpoint JSON must include openedCount."
Assert-Contains $maintenanceController "closedCount" "Maintenance endpoint JSON must include closedCount."
Assert-Contains $maintenanceController "ranAt" "Maintenance endpoint JSON must include ranAt."

$webConfig = Read-Text "Web.config"
Assert-Contains $webConfig "HospitalQualityMaintenanceToken" "Web.config must declare HospitalQualityMaintenanceToken."

$project = Read-Text "HospitalQualityDashboard-demo.csproj"
Assert-Contains $project "Controllers\\MaintenanceController\.cs" "Project file must compile MaintenanceController."
Assert-Contains $project "Services\\ReportingPeriods\\ReportingPeriodMaintenanceService\.cs" "Project file must compile ReportingPeriodMaintenanceService."
Assert-Contains $project "tools\\VerifyReportingPeriodMaintenance\.ps1" "Project file must include the verification script."

Write-Host "Reporting period maintenance verification passed."
