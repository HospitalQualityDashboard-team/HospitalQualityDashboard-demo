$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ServiceSourceReader.ps1')

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$viewModel = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Models\ViewModels\AppViewModels.cs')
$service = Get-ServiceSource -Root $root -Patterns 'Services\Dashboards\DashboardService*.cs'
$adminView = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Areas\Admin\Views\Dashboard\Index.cshtml')
$userView = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Areas\User\Views\Dashboard\Index.cshtml')
$css = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Content\Site.css')

$checks = @(
    @{ Source = $viewModel; Token = 'class DashboardMetricDetailViewModel' },
    @{ Source = $viewModel; Token = 'IList<DashboardMetricDetailViewModel> MetricDetails' },
    @{ Source = $viewModel; Token = 'public string MucTieu { get; set; }' },
    @{ Source = $viewModel; Token = 'public TrangThaiKyBaoCao TrangThaiKyBaoCao { get; set; }' },
    @{ Source = $viewModel; Token = 'public bool IsReportingPeriodLocked { get; set; }' },
    @{ Source = $viewModel; Token = 'public bool CanSendWarning { get; set; }' },
    @{ Source = $service; Token = 'GetAdminMetricDetails' },
    @{ Source = $service; Token = 'GetUserMetricDetails' },
    @{ Source = $service; Token = 'model.MetricDetails = GetUserMetricDetails(departmentId.Value, tanSuatFilter)' },
    @{ Source = $service; Token = 'pc.KhoaPhongId = @KhoaPhongId' },
    @{ Source = $service; Token = 'ky.TrangThai AS TrangThaiKyBaoCao' },
    @{ Source = $service; Token = 'TrangThaiKyBaoCao.Khoa' },
    @{ Source = $service; Token = 'CanSendWarning = !isSubmitted && !hasWarningToday && !isReportingPeriodLocked' },
    @{ Source = $service; Token = 'ct.DatMucTieu' },
    @{ Source = $service; Token = 'ChiSoMucTieu' },
    @{ Source = $service; Token = 'FormatMucTieu' },
    @{ Source = $service; Token = 'IsOverdueMissing' },
    @{ Source = $adminView; Token = 'id="dashboardMetricDetailModal"' },
    @{ Source = $adminView; Token = 'data-metric="submitted"' },
    @{ Source = $adminView; Token = 'data-metric-groups' },
    @{ Source = $adminView; Token = 'data-indicator-detail-panel' },
    @{ Source = $adminView; Token = 'data-indicator-detail-trigger' },
    @{ Source = $adminView; Token = 'data-detail-status' },
    @{ Source = $adminView; Token = 'data-detail-target' },
    @{ Source = $adminView; Token = 'data-indicator-detail-target' },
    @{ Source = $adminView; Token = 'status-period-locked' },
    @{ Source = $adminView; Token = 'data-detail-period-state' },
    @{ Source = $adminView; Token = 'data-period-state-filter' },
    @{ Source = $adminView; Token = 'data-period-state="@periodStateValue"' },
    @{ Source = $adminView; Token = 'value="locked"' },
    @{ Source = $adminView; Token = 'applyMetricFilters' },
    @{ Source = $adminView; Token = 'item.CanSendWarning' },
    @{ Source = $adminView; Token = 'setIndicatorDetail' },
    @{ Source = $adminView; Token = 'show.bs.modal' },
    @{ Source = $userView; Token = 'id="dashboardMetricDetailModal"' },
    @{ Source = $userView; Token = 'data-metric="submitted"' },
    @{ Source = $userView; Token = 'data-metric-groups' },
    @{ Source = $userView; Token = 'data-indicator-detail-panel' },
    @{ Source = $userView; Token = 'data-indicator-detail-trigger' },
    @{ Source = $userView; Token = 'data-detail-status' },
    @{ Source = $userView; Token = 'data-detail-target' },
    @{ Source = $userView; Token = 'data-indicator-detail-target' },
    @{ Source = $userView; Token = 'status-period-locked' },
    @{ Source = $userView; Token = 'data-period-lock-note' },
    @{ Source = $userView; Token = 'data-period-state-filter' },
    @{ Source = $userView; Token = 'data-period-state="@periodStateValue"' },
    @{ Source = $userView; Token = 'value="locked"' },
    @{ Source = $userView; Token = 'applyMetricFilters' },
    @{ Source = $userView; Token = 'setIndicatorDetail' },
    @{ Source = $userView; Token = 'show.bs.modal' },
    @{ Source = $css; Token = '.metric-card-button' }
)

foreach ($check in $checks) {
    if ($check.Source -notmatch [regex]::Escape($check.Token)) {
        throw "Dashboard metric detail contract is missing: $($check.Token)"
    }
}

Write-Host 'Dashboard metric detail structural verification passed.'
