$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$servicePath = Join-Path $root 'Services\ReportDashboardServices.cs'
$viewModelPath = Join-Path $root 'Models\ViewModels\AppViewModels.cs'
$viewPath = Join-Path $root 'Areas\Admin\Views\Dashboard\Index.cshtml'

$service = Get-Content -Raw -Path $servicePath
$viewModel = Get-Content -Raw -Path $viewModelPath
$view = Get-Content -Raw -Path $viewPath

$requiredServiceTokens = @(
    'ExpectedSlots AS',
    'CompletedSlots AS',
    'TongBaoCaoCanNop',
    '@DaDuyetStatus',
    '@DraftPeriodStatus',
    '@Today',
    'es.HanNop < @Today'
)

foreach ($token in $requiredServiceTokens) {
    if ($service -notmatch [regex]::Escape($token)) {
        throw "Dashboard summary is missing required slot token: $token"
    }
}

$optimizedSummaryMatch = [regex]::Match(
    $service,
    'private void ApplyOptimizedDashboardSummary[\s\S]+?private IList<DepartmentProgressViewModel> GetOptimizedDepartmentProgress')
if (-not $optimizedSummaryMatch.Success) {
    throw 'Could not locate ApplyOptimizedDashboardSummary for regression verification.'
}

if ($optimizedSummaryMatch.Value -match [regex]::Escape('model.BaoCaoThieu = model.ChiSoDuocPhanCong - model.BaoCaoDaGui')) {
    throw 'Dashboard summary still subtracts report rows from active assignments.'
}

if ($viewModel -notmatch 'public\s+int\s+TongBaoCaoCanNop\s*\{\s*get;\s*set;\s*\}') {
    throw 'DashboardViewModel is missing TongBaoCaoCanNop.'
}

$requiredViewTokens = @(
    'Model.TongBaoCaoCanNop',
    'totalMetricLabel',
    'Math.Round',
    '0.##'
)

foreach ($token in $requiredViewTokens) {
    if ($view -notmatch [regex]::Escape($token)) {
        throw "Admin Dashboard view is missing required summary token: $token"
    }
}

Write-Host 'Dashboard Admin slot summary structural verification passed.'
