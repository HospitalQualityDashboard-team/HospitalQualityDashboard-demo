$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$dashboardViews = @(
    (Join-Path $root 'Areas\Admin\Views\Dashboard\Index.cshtml'),
    (Join-Path $root 'Areas\User\Views\Dashboard\Index.cshtml')
)

foreach ($viewPath in $dashboardViews) {
    $view = Get-Content -Raw -Path $viewPath
    $viewName = Split-Path $viewPath -Leaf

    $forbiddenTokens = @(
        '.dashboard-hero::after',
        '.metric-card::before',
        '.metric-card::after',
        'radial-gradient',
        'filter: saturate'
    )

    foreach ($token in $forbiddenTokens) {
        if ($view -match [regex]::Escape($token)) {
            throw "$viewName still contains decorative card token: $token"
        }
    }

    $requiredPatterns = @(
        '\.metric-card\.is-accent\s*\{\s*background:\s*#146c78;',
        '\.metric-card\.is-success\s*\{\s*background:\s*#22A06B;',
        '\.metric-card\.is-warning\s*\{\s*background:\s*#E5484D;',
        '\.metric-card\.is-danger\s*\{\s*background:\s*#E5484D;',
        '\.dashboard-summary-strip\s+\.summary-success\s*\{\s*background:\s*#22A06B\s*!important;',
        '\.dashboard-summary-strip\s+\.summary-danger\s*\{\s*background:\s*#E5484D\s*!important;',
        '\.dashboard-summary-strip\s+\.summary-info\s*\{\s*background:\s*#146c78\s*!important;',
        '\.metric-value\s*\{[\s\S]*?color:\s*#fff;',
        '\.metric-caption\s*\{[\s\S]*?color:\s*rgba\(255,255,255,\.88\)\s*!important;'
    )

    foreach ($pattern in $requiredPatterns) {
        if ($view -notmatch $pattern) {
            throw "$viewName is missing colored card pattern: $pattern"
        }
    }
}

Write-Host 'Dashboard simple card structural verification passed.'
