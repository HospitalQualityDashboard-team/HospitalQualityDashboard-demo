$ErrorActionPreference = 'Stop'

$projectRoot = Resolve-Path (Join-Path $PSScriptRoot '..')

function Read-ProjectFile {
    param([string] $RelativePath)
    return Get-Content -Path (Join-Path $projectRoot $RelativePath) -Raw
}

function Assert-Contains {
    param(
        [string] $Content,
        [string] $Pattern,
        [string] $Message
    )

    if ($Content -notmatch $Pattern) {
        throw $Message
    }
}

function Assert-NotContains {
    param(
        [string] $Content,
        [string] $Pattern,
        [string] $Message
    )

    if ($Content -match $Pattern) {
        throw $Message
    }
}

$userReportController = Read-ProjectFile 'Areas\User\Controllers\ReportController.cs'
$adminReportController = Read-ProjectFile 'Areas\Admin\Controllers\ReportController.cs'
$reportService = Read-ProjectFile 'Services\ReportDashboardServices.cs'
$bootstrapper = Read-ProjectFile 'Services\DatabaseBootstrapper.cs'
$globalAsax = Read-ProjectFile 'Global.asax.cs'
$webConfig = Read-ProjectFile 'Web.config'
$schema = Read-ProjectFile 'App_Data\Sql\001_CreateSchema.sql'
$userNotificationController = Read-ProjectFile 'Areas\User\Controllers\NotificationController.cs'
$adminNotificationController = Read-ProjectFile 'Areas\Admin\Controllers\NotificationController.cs'
$periodController = Read-ProjectFile 'Controllers\ReportingPeriodController.cs'
$pageController = Read-ProjectFile 'Controllers\PageController.cs'
$authService = Read-ProjectFile 'Services\AuthService.cs'
$excelService = Read-ProjectFile 'Services\ExcelImportExportService.cs'

Assert-Contains $userReportController 'var departmentId = existingReport == null \? model\.KhoaPhongId : existingReport\.KhoaPhongId' 'User POST Report/Edit must reload the existing report before authorization.'
Assert-Contains $userReportController 'EnsureUserDepartment\(departmentId\)' 'User POST Report/Edit must authorize against the persisted report department.'
Assert-Contains $reportService 'model\.KhoaPhongId = existingReport\.KhoaPhongId' 'SaveDraft must ignore client-supplied department fields for existing reports.'

Assert-NotContains $globalAsax 'DatabaseBootstrapper\.BootstrapIfDebug\(\);' 'Application startup must not seed a known admin account automatically.'
Assert-Contains $bootstrapper 'BootstrapIfExplicitlyEnabled' 'Database bootstrap must require an explicit local-only switch.'
Assert-Contains $bootstrapper 'HospitalQualityBootstrapEnabled' 'Database bootstrap must be gated by configuration.'
Assert-NotContains $schema "N'admin'" 'Base schema must not seed a known admin username.'
Assert-NotContains $schema 'Admin@123' 'Base schema must not document or seed the default admin password.'
Assert-NotContains $webConfig 'debug="true"' 'Base Web.config must not enable debug compilation by default.'

Assert-Contains $adminReportController 'Index\(int\? kyBaoCaoId, int\? khoaPhongId, int\? chiSoChatLuongId\)' 'Admin Report Index must be GET-only (redirect from root).'
Assert-Contains $userReportController 'Index\(int\? kyBaoCaoId, int\? chiSoChatLuongId\)' 'User Report Index must be GET-only (redirect from root).'

Assert-Contains $userNotificationController 'public ActionResult Index\(\)\s*\{\s*return View\(_service\.GetForUser\(CurrentTaiKhoanId\.Value, false\)\);\s*\}' 'User Notification GET Index must be read-only.'
Assert-Contains $adminNotificationController 'public ActionResult Index\(\)\s*\{\s*return View\(_service\.GetForUser\(CurrentTaiKhoanId\.Value, true\)\);\s*\}' 'Admin Notification GET Index must be read-only.'
Assert-Contains $userNotificationController 'public ActionResult Details\(int id\)[\s\S]*?return View\(new NotificationDetailViewModel' 'User Notification GET Details must remain a read-only detail renderer.'
Assert-Contains $adminNotificationController 'public ActionResult Details\(int id\)[\s\S]*?return View\(new NotificationDetailViewModel' 'Admin Notification GET Details must remain a read-only detail renderer.'
Assert-Contains $periodController 'public ActionResult Index\(\)\s*\{\s*var admin = RequireAdmin\(\);[\s\S]*?return RedirectToAction\("Index", "ReportingPeriod", new \{ area = "Admin" \}\);\s*\}' 'ReportingPeriod root GET Index must be a read-only redirect to the Admin Area.'
Assert-Contains $adminNotificationController 'public ActionResult OpenDuePeriodsAndRunAutomation\(\)' 'State-changing notification automation must be POST-only.'
Assert-Contains $userNotificationController 'public ActionResult MarkDetailAsRead\(int id\)' 'Marking notification details read must be POST-only.'
Assert-Contains $periodController 'public ActionResult OpenDuePeriods\(\)' 'Opening due periods from period pages must be POST-only.'

Assert-Contains $pageController 'RevalidateCurrentSession\(\)' 'Protected requests must revalidate the current account status.'
Assert-Contains $authService 'public AuthenticatedUser GetAuthenticatedUser\(int taiKhoanId\)' 'AuthService must expose current-account revalidation.'
Assert-Contains $authService 'LockoutUntil' 'Login must support temporary lockout.'
Assert-Contains $authService 'FailedLoginCount' 'Login must track failed attempts.'
Assert-Contains $authService 'RecordFailedLogin' 'Login failures must update throttling state.'
Assert-Contains $authService 'ResetFailedLogin' 'Successful login must reset throttling state.'
Assert-Contains $schema 'FailedLoginCount' 'Schema must include failed login counter.'
Assert-Contains $schema 'LockoutUntil' 'Schema must include lockout deadline.'

Assert-Contains $excelService 'NeutralizeCsvFormula' 'CSV export must neutralize spreadsheet formulas.'
Assert-Contains $excelService 'MaxImportBytes' 'Import must enforce upload size limits.'
Assert-Contains $excelService 'MaxImportRows' 'Import must enforce row count limits.'
Assert-Contains $excelService 'ValidateZipEntry' 'Office import must validate ZIP entry limits.'

Write-Host 'Security hardening verification passed.'
