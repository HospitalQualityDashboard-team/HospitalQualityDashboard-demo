$ErrorActionPreference = 'Stop'

$projectRoot = Resolve-Path (Join-Path $PSScriptRoot '..')

function Read-ProjectFile {
    param([string] $RelativePath)
    $path = Join-Path $projectRoot $RelativePath
    if (-not (Test-Path $path)) {
        return ''
    }

    return Get-Content -Path $path -Raw
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

$controller = Read-ProjectFile 'Controllers\ReportingPeriodController.cs'
$dashboardController = Read-ProjectFile 'Controllers\DashboardController.cs'
$reportController = Read-ProjectFile 'Controllers\ReportController.cs'
$notificationController = Read-ProjectFile 'Controllers\NotificationController.cs'
$globalAsax = Read-ProjectFile 'Global.asax.cs'
$viewModels = Read-ProjectFile 'Models\ViewModels\AppViewModels.cs'
$periodServices = Read-ProjectFile 'Services\ReportingPeriodServices.cs'
$indexView = Read-ProjectFile 'Views\ReportingPeriod\Index.cshtml'
$scheduleView = Read-ProjectFile 'Views\ReportingPeriod\GenerateSchedule.cshtml'
$projectFile = Read-ProjectFile 'HospitalQualityDashboard.csproj'

Assert-Contains $viewModels 'class ReportingPeriodScheduleRequestViewModel' 'Schedule request view model must exist.'
Assert-Contains $viewModels 'int Year' 'Schedule request must expose Year.'
Assert-Contains $viewModels 'int\[\] SelectedFrequencyValues' 'Schedule request must expose selected frequencies.'
Assert-Contains $viewModels 'int DueDayOffset' 'Schedule request must expose due-day offset.'
Assert-Contains $viewModels 'DueDayOffset = 0' 'Automatic schedule must default report deadline to the period end date.'
Assert-Contains $viewModels 'IList<ReportingPeriodSchedulePreviewItemViewModel> PreviewItems' 'Schedule request must carry preview rows.'
Assert-Contains $viewModels 'class ReportingPeriodSchedulePreviewItemViewModel' 'Schedule preview item view model must exist.'
Assert-Contains $viewModels 'bool AlreadyExists' 'Schedule preview item must mark duplicate periods.'
Assert-Contains $viewModels 'string TrangThaiText' 'Schedule preview item must expose Vietnamese status text.'
Assert-Contains $viewModels 'class ReportingPeriodScheduleResultViewModel' 'Schedule result view model must exist.'

Assert-Contains $periodServices 'class ReportingPeriodScheduleService' 'Schedule service must be separated from basic period CRUD.'
Assert-Contains $periodServices 'BuildSchedulePreview\(ReportingPeriodScheduleRequestViewModel request, DateTime now\)' 'Schedule service must expose preview generation.'
Assert-Contains $periodServices 'GenerateSchedule\(ReportingPeriodScheduleRequestViewModel request, DateTime now\)' 'Schedule service must expose bulk generation.'
Assert-Contains $periodServices 'OpenDuePeriods\(DateTime now\)' 'Schedule service must expose auto-open behavior.'
Assert-Contains $periodServices 'TuNgay\s*<=\s*@Today' 'Auto-open must open periods whose start date has arrived.'
Assert-Contains $periodServices 'TrangThai=@Mo' 'Auto-open must set due periods to Mo.'
Assert-Contains $periodServices 'LoaiKyBaoCao=@LoaiKyBaoCao\s+AND\s+TuNgay=@TuNgay\s+AND\s+DenNgay=@DenNgay' 'Duplicate detection must use frequency plus date range.'
Assert-Contains $periodServices 'TanSuatBaoCao\.HangNgay' 'Schedule service must support daily periods.'
Assert-Contains $periodServices 'TanSuatBaoCao\.HangThang' 'Schedule service must support monthly periods.'
Assert-Contains $periodServices 'TanSuatBaoCao\.HangQuy' 'Schedule service must support quarterly periods.'
Assert-Contains $periodServices 'TanSuatBaoCao\.SauThang' 'Schedule service must support six-month periods.'
Assert-Contains $periodServices 'TanSuatBaoCao\.ChinThang' 'Schedule service must support nine-month periods.'
Assert-Contains $periodServices 'TanSuatBaoCao\.HangNam' 'Schedule service must support yearly periods.'
Assert-Contains $periodServices 'end\.Date\s*<\s*today' 'Schedule preview must skip periods that ended before today.'
Assert-Contains $periodServices 'request\.DueDayOffset = 0' 'Schedule service must enforce deadline equals the period end date.'
Assert-NotContains $periodServices 'AddPeriod\(.*KhiPhatSinh' 'Schedule service must not create event-based periods.'
Assert-NotContains $periodServices 'AddPeriod\(.*TruocSauKhiThucHien' 'Schedule service must not create before/after implementation periods.'

Assert-Contains $controller 'ActionResult GenerateSchedule\(' 'ReportingPeriodController must expose GET GenerateSchedule.'
Assert-Contains $controller 'ActionResult PreviewSchedule\(ReportingPeriodScheduleRequestViewModel model\)' 'ReportingPeriodController must expose POST PreviewSchedule.'
Assert-Contains $controller 'ActionResult CreateSchedule\(ReportingPeriodScheduleRequestViewModel model\)' 'ReportingPeriodController must expose POST CreateSchedule.'
Assert-Contains $controller 'OpenDuePeriods\(DateTime\.Now\)' 'ReportingPeriodController must auto-open due periods.'

Assert-Contains $dashboardController 'ReportingPeriodScheduleService' 'Dashboard must invoke reporting period auto-open service.'
Assert-Contains $reportController 'ReportingPeriodScheduleService' 'Report page must invoke reporting period auto-open service.'
Assert-Contains $notificationController 'ReportingPeriodScheduleService' 'Notification automation path must invoke reporting period auto-open service.'
Assert-Contains $globalAsax 'ReportingPeriodScheduleService' 'Application_Start must invoke reporting period auto-open service.'

Assert-Contains $indexView 'FormatPeriodStatus' 'Reporting period index must render Vietnamese status text.'
Assert-Contains $scheduleView 'schedule-page-title' 'Schedule view must have the expected title.'
Assert-Contains $scheduleView 'daily-period-option' 'Schedule view must display daily schedule option.'
Assert-Contains $scheduleView 'expired-period-note' 'Schedule view must explain that expired periods are skipped.'
Assert-Contains $scheduleView 'deadline-end-of-period-note' 'Schedule view must explain that deadline is the period close time.'
Assert-Contains $scheduleView 'PreviewSchedule' 'Schedule view must post preview requests.'
Assert-Contains $scheduleView 'CreateSchedule' 'Schedule view must post create requests.'
Assert-Contains $scheduleView 'existing-period-badge' 'Schedule preview must show existing rows.'
Assert-Contains $scheduleView 'new-period-badge' 'Schedule preview must show new rows.'
Assert-Contains $scheduleView 'status-text-note' 'Schedule view must display Vietnamese status labels.'

Assert-Contains $projectFile 'Views\\ReportingPeriod\\GenerateSchedule\.cshtml' 'Project file must include the schedule view.'

Write-Host 'Reporting period schedule verification passed.'
