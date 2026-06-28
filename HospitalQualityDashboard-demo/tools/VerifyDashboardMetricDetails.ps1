$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ServiceSourceReader.ps1')

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$viewModel = Get-Content -Raw (Join-Path $root 'Models\ViewModels\AppViewModels.cs')
$service = Get-ServiceSource -Root $root -Patterns 'Services\Dashboards\DashboardService*.cs'
$adminView = Get-Content -Raw (Join-Path $root 'Areas\Admin\Views\Dashboard\Index.cshtml')
$userView = Get-Content -Raw (Join-Path $root 'Areas\User\Views\Dashboard\Index.cshtml')
$css = Get-Content -Raw (Join-Path $root 'Content\Site.css')

$checks = @(
    @{ Source = $viewModel; Token = 'class DashboardMetricDetailViewModel' },
    @{ Source = $viewModel; Token = 'IList<DashboardMetricDetailViewModel> MetricDetails' },
    @{ Source = $service; Token = 'GetAdminMetricDetails' },
    @{ Source = $service; Token = 'GetUserMetricDetails' },
    @{ Source = $service; Token = 'model.MetricDetails = GetUserMetricDetails(departmentId.Value, tanSuatFilter)' },
    @{ Source = $service; Token = 'pc.KhoaPhongId = @KhoaPhongId' },
    @{ Source = $service; Token = 'ct.DatMucTieu' },
    @{ Source = $service; Token = 'IsOverdueMissing' },
    @{ Source = $adminView; Token = 'id="dashboardMetricDetailModal"' },
    @{ Source = $adminView; Token = 'data-metric="submitted"' },
    @{ Source = $adminView; Token = 'data-metric-groups' },
    @{ Source = $adminView; Token = 'show.bs.modal' },
    @{ Source = $userView; Token = 'id="dashboardMetricDetailModal"' },
    @{ Source = $userView; Token = 'data-metric="submitted"' },
    @{ Source = $userView; Token = 'data-metric-groups' },
    @{ Source = $userView; Token = 'show.bs.modal' },
    @{ Source = $css; Token = '.metric-card-button' }
)

foreach ($check in $checks) {
    if ($check.Source -notmatch [regex]::Escape($check.Token)) {
        throw "Dashboard metric detail contract is missing: $($check.Token)"
    }
}

Write-Host 'Dashboard metric detail structural verification passed.'
