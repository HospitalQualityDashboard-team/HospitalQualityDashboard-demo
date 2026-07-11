$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$dtoPath = Join-Path $root 'Models\DTOs\ExportDtos.cs'
$builderPath = Join-Path $root 'Services\Dashboards\DashboardComparisonBuilder.cs'
$progressBuilderPath = Join-Path $root 'Services\Dashboards\DashboardProgressComparisonBuilder.cs'

if (-not (Test-Path $builderPath)) {
    throw "Missing DashboardComparisonBuilder: $builderPath"
}

if (-not (Test-Path $progressBuilderPath)) {
    throw "Missing DashboardProgressComparisonBuilder: $progressBuilderPath"
}

Add-Type -Path $dtoPath, $builderPath, $progressBuilderPath

function Assert-Equal($expected, $actual, [string]$message) {
    if ($expected -ne $actual) {
        throw "$message. Expected '$expected' but got '$actual'."
    }
}

function ConvertFrom-Utf8Bytes([int[]]$bytes) {
    return [System.Text.Encoding]::UTF8.GetString([byte[]]$bytes)
}

function New-Period([int]$id, [int]$frequency, [datetime]$startDate, [int]$status = 3) {
    $period = New-Object HospitalQualityDashboardDemo.Models.DTOs.DashboardComparisonPeriodDto
    $period.KyBaoCaoId = $id
    $period.TenKyBaoCao = "Ky $id"
    $period.TanSuat = $frequency
    $period.TuNgay = $startDate
    $period.TrangThai = $status
    return $period
}

$periods = New-Object 'System.Collections.Generic.List[HospitalQualityDashboardDemo.Models.DTOs.DashboardComparisonPeriodDto]'
$periods.Add((New-Period 6 3 ([datetime]'2026-06-01')))
$periods.Add((New-Period 5 3 ([datetime]'2026-05-01')))
$periods.Add((New-Period 4 3 ([datetime]'2026-04-01')))

$ordered = [HospitalQualityDashboardDemo.Services.DashboardComparisonBuilder]::ValidateAndOrderPeriods(
    6,
    [int[]](5, 4, 5),
    $periods)

Assert-Equal 3 $ordered.Count 'Primary plus two unique comparison periods should be returned'
Assert-Equal 6 $ordered[0].KyBaoCaoId 'Primary period should be first'
Assert-Equal 5 $ordered[1].KyBaoCaoId 'Newest comparison period should follow primary'
Assert-Equal 4 $ordered[2].KyBaoCaoId 'Older comparison period should be last'

$invalidFrequency = New-Object 'System.Collections.Generic.List[HospitalQualityDashboardDemo.Models.DTOs.DashboardComparisonPeriodDto]'
$invalidFrequency.Add((New-Period 6 3 ([datetime]'2026-06-01')))
$invalidFrequency.Add((New-Period 2 4 ([datetime]'2026-01-01')))

$frequencyRejected = $false
try {
    [HospitalQualityDashboardDemo.Services.DashboardComparisonBuilder]::ValidateAndOrderPeriods(6, [int[]](2), $invalidFrequency) | Out-Null
} catch {
    $frequencyRejected = $_.Exception.InnerException -is [System.InvalidOperationException]
}
Assert-Equal $true $frequencyRejected 'A comparison period with another frequency should be rejected'

$tooManyPeriods = New-Object 'System.Collections.Generic.List[HospitalQualityDashboardDemo.Models.DTOs.DashboardComparisonPeriodDto]'
$tooManyPeriods.Add((New-Period 20 3 ([datetime]'2026-12-01')))
$tooManyIds = New-Object 'System.Collections.Generic.List[int]'
for ($id = 1; $id -le 12; $id++) {
    $tooManyPeriods.Add((New-Period $id 3 ([datetime]'2025-01-01').AddMonths($id)))
    $tooManyIds.Add($id)
}
$limitRejected = $false
try {
    [HospitalQualityDashboardDemo.Services.DashboardComparisonBuilder]::ValidateAndOrderPeriods(20, $tooManyIds, $tooManyPeriods) | Out-Null
} catch {
    $limitRejected = $_.Exception.InnerException -is [System.InvalidOperationException]
}
Assert-Equal $true $limitRejected 'More than eleven comparison periods should be rejected'

$primary = New-Object HospitalQualityDashboardDemo.Models.DTOs.DashboardIndicatorPeriodValueDto
$primary.KyBaoCaoId = 6
$primary.KhoaPhongId = 10
$primary.ChiSoChatLuongId = 20
$primary.KetQua = [decimal]98
$primary.IsExpected = $true
$primary.IsSubmitted = $true
$primary.DatMucTieu = $true

$previous = New-Object HospitalQualityDashboardDemo.Models.DTOs.DashboardIndicatorPeriodValueDto
$previous.KyBaoCaoId = 5
$previous.KhoaPhongId = 10
$previous.ChiSoChatLuongId = 20
$previous.KetQua = [decimal]95
$previous.IsExpected = $true
$previous.IsSubmitted = $true
$previous.DatMucTieu = $false

$comparison = [HospitalQualityDashboardDemo.Services.DashboardComparisonBuilder]::CompareIndicator($primary, $previous)
Assert-Equal ([decimal]3) $comparison.ChenhLech 'Indicator delta should be primary minus comparison'
$improvedText = ConvertFrom-Utf8Bytes @(67, 225, 186, 163, 105, 32, 116, 104, 105, 225, 187, 135, 110)
Assert-Equal $improvedText $comparison.XuHuong 'Moving from unmet to met target should improve'

$missingPrimary = New-Object HospitalQualityDashboardDemo.Models.DTOs.DashboardIndicatorPeriodValueDto
$missingPrimary.IsExpected = $true
$missingPrimary.IsSubmitted = $false
$missingComparison = [HospitalQualityDashboardDemo.Services.DashboardComparisonBuilder]::CompareIndicator($missingPrimary, $previous)
$notSubmittedText = ConvertFrom-Utf8Bytes @(67, 104, 198, 176, 97, 32, 110, 225, 187, 153, 112)
Assert-Equal $notSubmittedText $missingComparison.TrangThaiKyChinh 'Expected but unsubmitted data should not be treated as zero'

$dueDate = [datetime]'2026-06-30'
$todayBeforeDue = [datetime]'2026-06-20'
$todayAfterDue = [datetime]'2026-07-01'
$onTime = [HospitalQualityDashboardDemo.Services.DashboardProgressComparisonBuilder]::Classify([datetime]'2026-06-30', $dueDate, $todayAfterDue)
$late = [HospitalQualityDashboardDemo.Services.DashboardProgressComparisonBuilder]::Classify([datetime]'2026-07-01', $dueDate, $todayAfterDue)
$missing = [HospitalQualityDashboardDemo.Services.DashboardProgressComparisonBuilder]::Classify($null, $dueDate, $todayBeforeDue)
$overdue = [HospitalQualityDashboardDemo.Services.DashboardProgressComparisonBuilder]::Classify($null, $dueDate, $todayAfterDue)
Assert-Equal 'OnTime' $onTime 'Submission on due date should be on time'
Assert-Equal 'Late' $late 'Submission after due date should be late'
Assert-Equal 'Missing' $missing 'Unsubmitted work before due date should be missing'
Assert-Equal 'OverdueMissing' $overdue 'Unsubmitted work after due date should be overdue'
Assert-Equal 'Better' ([HospitalQualityDashboardDemo.Services.DashboardProgressComparisonBuilder]::Compare('OnTime', 'Late')) 'On-time should improve over late'
Assert-Equal 'Worse' ([HospitalQualityDashboardDemo.Services.DashboardProgressComparisonBuilder]::Compare('Late', 'OnTime')) 'Late should be worse than on-time'
Assert-Equal 'Insufficient' ([HospitalQualityDashboardDemo.Services.DashboardProgressComparisonBuilder]::Compare('Missing', 'OnTime')) 'An open missing submission should not be judged early'

$integrationChecks = @(
    @{ Path = $dtoPath; Tokens = @('ComparisonPeriodIds', 'DashboardComparisonPeriodDto', 'DashboardAnalysisQueryDto', 'StatusPeriodId', 'ProgressStatus') },
    @{ Path = Join-Path $root 'Models\ViewModels\AppViewModels.cs'; Tokens = @('public int? StatusPeriodId', 'public string ProgressStatus') },
    @{ Path = Join-Path $root 'Services\Dashboards\DashboardProgressComparisonService.cs'; Tokens = @('GetComparison', 'CurrentDepartmentId', 'StatusPeriodId', 'ProgressStatus', 'FilterRowsByStatus') },
    @{ Path = Join-Path $root 'Areas\Admin\Controllers\DashboardController.cs'; Tokens = @('ActionResult PeriodComparison', 'ActionResult Comparison') },
    @{ Path = Join-Path $root 'Services\Dashboards\Export\DashboardExcelExportService.Comparison.cs'; Tokens = @('SoSanhTongQuan', 'SoSanhChiSo', 'SubmittedLate', 'OverdueMissing', 'DashboardProgressComparisonBuilder.Compare', 'ProgressStatus') },
    @{ Path = Join-Path $root 'Areas\Admin\Views\Dashboard\PeriodComparison.cshtml'; Tokens = @('_DashboardComparison.cshtml', 'data-dashboard-pane="comparison"', 'dashboard-analysis.js') },
    @{ Path = Join-Path $root 'Views\Shared\_DashboardComparison.cshtml'; Tokens = @('StatusPeriodId', 'ProgressStatus', 'comparisonStatusDetailModal', 'data-auto-open', 'modal-xl', 'modal-dialog-scrollable', 'comparison-detail-table', 'comparison-doughnut-chart') },
    @{ Path = Join-Path $root 'Scripts\dashboard-analysis.js'; Tokens = @('[data-analysis-export]', 'new URLSearchParams(new FormData(form))', "parameters.delete('Page')", 'window.location.href = exportUrl', "type: 'doughnut'", 'onClick', 'statusCodes', 'StatusPeriodId', 'ProgressStatus', 'bootstrap.Modal', 'hidden.bs.modal', 'data-analysis-page') },
    @{ Path = Join-Path $root 'Views\Shared\_Layout.cshtml'; Tokens = @('So sánh kỳ báo cáo', 'PeriodComparison') },
    @{ Path = Join-Path $root 'HospitalQualityDashboard-demo.csproj'; Tokens = @('DashboardProgressComparisonBuilder.cs', 'DashboardProgressComparisonService.cs', 'Areas\Admin\Views\Dashboard\PeriodComparison.cshtml', 'VerifyDashboardPeriodComparison.ps1') }
)

foreach ($check in $integrationChecks) {
    $text = Get-Content -Raw -Path $check.Path
    foreach ($token in $check.Tokens) {
        if ($text -notmatch [regex]::Escape($token)) {
            throw "Missing comparison integration token '$token' in $($check.Path)"
        }
    }
}

$comparisonPartial = Get-Content -Raw -Path (Join-Path $root 'Views\Shared\_DashboardComparison.cshtml')
if ($comparisonPartial -match [regex]::Escape('ComparisonPeriodIds = Model.ComparisonPeriodIds')) {
    throw 'Comparison export link must not build a static array route value; it should serialize the current form values on click.'
}
if ($comparisonPartial -match [regex]::Escape('@if (selectedStatusPeriod')) {
    throw 'Nested selectedStatusPeriod filter is already inside a Razor code block; use if (...) without @ to avoid HttpParseException.'
}
if ($comparisonPartial -match [regex]::Escape('comparison-detail-section')) {
    throw 'Status detail table must be rendered in a modal, not inline as comparison-detail-section.'
}

$adminComparisonPage = Get-Content -Raw -Path (Join-Path $root 'Areas\Admin\Views\Dashboard\PeriodComparison.cshtml')
$forbiddenAdminComparisonTokens = @('dashboard-hero', 'dashboard-summary-strip', 'metric-grid')
foreach ($token in $forbiddenAdminComparisonTokens) {
    if ($adminComparisonPage -match [regex]::Escape($token)) {
        throw "Admin period comparison page must not include overview token '$token'."
    }
}
if ($adminComparisonPage -match [regex]::Escape('.comparison-detail-section')) {
    throw 'Admin period comparison styles must not include inline comparison-detail-section.'
}
$adminModalStyleTokens = @('.comparison-status-modal', '.comparison-modal-table-wrap', '.comparison-detail-table')
foreach ($token in $adminModalStyleTokens) {
    if ($adminComparisonPage -notmatch [regex]::Escape($token)) {
        throw "Admin period comparison page must include modal style token '$token'."
    }
}

$userController = Get-Content -Raw -Path (Join-Path $root 'Areas\User\Controllers\DashboardController.cs')
$forbiddenUserControllerTokens = @('ActionResult Comparison', 'PeriodComparison', 'NormalizeDashboardTab', 'model.Comparison', 'DashboardTab')
foreach ($token in $forbiddenUserControllerTokens) {
    if ($userController -match [regex]::Escape($token)) {
        throw "User dashboard controller must not include comparison token '$token'."
    }
}

$userDashboard = Get-Content -Raw -Path (Join-Path $root 'Areas\User\Views\Dashboard\Index.cshtml')
$forbiddenUserDashboardTokens = @('DashboardTab', 'comparison-doughnut-chart')
foreach ($token in $forbiddenUserDashboardTokens) {
    if ($userDashboard -match [regex]::Escape($token)) {
        throw "User dashboard page must not include comparison token '$token'."
    }
}
if ($userDashboard -match 'Model\.Comparison(?!Periods)') {
    throw "User dashboard page must not include comparison token 'Model.Comparison'."
}

$layout = Get-Content -Raw -Path (Join-Path $root 'Views\Shared\_Layout.cshtml')
$adminComparisonLinkPattern = 'ActionLink\(.*"PeriodComparison",\s*"Dashboard",\s*new\s*\{\s*area\s*=\s*"Admin"\s*\}'
if ($layout -notmatch $adminComparisonLinkPattern) {
    throw 'Admin sidebar must include a PeriodComparison link.'
}
$userComparisonLinkPattern = 'ActionLink\(.*"PeriodComparison",\s*"Dashboard",\s*new\s*\{\s*area\s*=\s*"User"\s*\}'
if ($layout -match $userComparisonLinkPattern) {
    throw 'User sidebar must not include a PeriodComparison link.'
}
$adminComparisonIndex = $layout.IndexOf('PeriodComparison')
$adminReportIndex = $layout.IndexOf('", "Report"')
$adminDepartmentIndex = $layout.IndexOf('", "Department"')
if ($adminComparisonIndex -lt 0 -or $adminReportIndex -lt 0 -or $adminDepartmentIndex -lt 0) {
    throw 'Could not locate Admin sidebar ordering anchors.'
}
if ($adminComparisonIndex -lt $adminReportIndex -or $adminComparisonIndex -gt $adminDepartmentIndex) {
    throw 'Admin PeriodComparison link must live in the THONG KE section, after Report and before Department.'
}

$removedTrendChecks = @(
    @{ Path = $dtoPath; Tokens = @('DashboardTrendQueryDto') },
    @{ Path = Join-Path $root 'Models\ViewModels\AppViewModels.cs'; Tokens = @('DashboardTrendViewModel', 'public DashboardTrendViewModel Trend') },
    @{ Path = Join-Path $root 'Services\Dashboards\DashboardProgressComparisonService.cs'; Tokens = @('GetTrend') },
    @{ Path = Join-Path $root 'Areas\Admin\Controllers\DashboardController.cs'; Tokens = @('ActionResult Trend', 'model.Trend', '"trend"') },
    @{ Path = Join-Path $root 'Areas\User\Controllers\DashboardController.cs'; Tokens = @('ActionResult Trend', 'model.Trend', '"trend"') },
    @{ Path = Join-Path $root 'Areas\Admin\Views\Dashboard\Index.cshtml'; Tokens = @('DashboardTab = "trend"', '_DashboardTrend.cshtml', 'dashboard-trend-chart', 'data-dashboard-trend-form') },
    @{ Path = Join-Path $root 'Areas\User\Views\Dashboard\Index.cshtml'; Tokens = @('DashboardTab = "trend"', '_DashboardTrend.cshtml', 'dashboard-trend-chart', 'data-dashboard-trend-form') },
    @{ Path = Join-Path $root 'Scripts\dashboard-analysis.js'; Tokens = @('dashboard-trend-chart', 'data-dashboard-trend-form') },
    @{ Path = Join-Path $root 'Content\Site.css'; Tokens = @('.trend-filter', '.trend-chart-panel') },
    @{ Path = Join-Path $root 'HospitalQualityDashboard-demo.csproj'; Tokens = @('_DashboardTrend.cshtml') }
)

foreach ($check in $removedTrendChecks) {
    $text = Get-Content -Raw -Path $check.Path
    foreach ($token in $check.Tokens) {
        if ($text -match [regex]::Escape($token)) {
            throw "Removed trend token '$token' still exists in $($check.Path)"
        }
    }
}

Write-Host 'Dashboard period comparison behavior verification passed.'
