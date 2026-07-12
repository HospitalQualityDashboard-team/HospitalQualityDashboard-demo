$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ServiceSourceReader.ps1')

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$viewModel = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Models\ViewModels\AppViewModels.cs')
$service = Get-ServiceSource -Root $root -Patterns 'Services\Dashboards\DashboardService*.cs'
$adminView = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Areas\Admin\Views\Dashboard\Index.cshtml')
$userView = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Areas\User\Views\Dashboard\Index.cshtml')
$css = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Content\Site.css')

$checks = @(
    @{ Source = $viewModel; Token = 'public DateTime TuNgay { get; set; }' },
    @{ Source = $viewModel; Token = 'public DateTime DenNgay { get; set; }' },
    @{ Source = $viewModel; Token = 'public TanSuatBaoCao LoaiKyBaoCao { get; set; }' },
    @{ Source = $service; Token = 'LoaiKyBaoCao = (TanSuatBaoCao)Convert.ToByte(reader["LoaiKyBaoCao"])' },
    @{ Source = $service; Token = 'TuNgay = reader.GetDateTime(reader.GetOrdinal("TuNgay"))' },
    @{ Source = $service; Token = 'DenNgay = reader.GetDateTime(reader.GetOrdinal("DenNgay"))' },
    @{ Source = $adminView; Token = 'data-report-period-filter' },
    @{ Source = $adminView; Token = 'data-period-id="@item.KyBaoCaoId"' },
    @{ Source = $adminView; Token = 'data-period-frequency="@((int)item.LoaiKyBaoCao)"' },
    @{ Source = $adminView; Token = 'data-period-start="@item.TuNgay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)"' },
    @{ Source = $adminView; Token = 'data-period-end="@item.DenNgay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)"' },
    @{ Source = $adminView; Token = 'var metricPageSize = 10;' },
    @{ Source = $adminView; Token = 'selectCurrentMonthlyPeriod' },
    @{ Source = $adminView; Token = 'data-metric-pagination' },
    @{ Source = $userView; Token = 'data-report-period-filter' },
    @{ Source = $userView; Token = 'data-period-id="@item.KyBaoCaoId"' },
    @{ Source = $userView; Token = 'data-period-frequency="@((int)item.LoaiKyBaoCao)"' },
    @{ Source = $userView; Token = 'data-period-start="@item.TuNgay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)"' },
    @{ Source = $userView; Token = 'data-period-end="@item.DenNgay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)"' },
    @{ Source = $userView; Token = 'var metricPageSize = 10;' },
    @{ Source = $userView; Token = 'selectCurrentMonthlyPeriod' },
    @{ Source = $userView; Token = 'data-metric-pagination' },
    @{ Source = $css; Token = '.metric-pagination' }
)

foreach ($check in $checks) {
    if ($check.Source -notmatch [regex]::Escape($check.Token)) {
        throw "Dashboard metric period paging contract is missing: $($check.Token)"
    }
}

Write-Host 'Dashboard metric period filter and paging structural verification passed.'
