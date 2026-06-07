$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$layout = Get-Content -Raw -LiteralPath (Join-Path $root 'Views\Shared\_Layout.cshtml')
$dashboard = Get-Content -Raw -LiteralPath (Join-Path $root 'Views\Dashboard\Index.cshtml')
$siteCss = Get-Content -Raw -LiteralPath (Join-Path $root 'Content\Site.css')

function Assert-Contains {
    param(
        [string]$Content,
        [string]$Pattern,
        [string]$Message
    )

    if ($Content -notmatch $Pattern) {
        throw $Message
    }
}

Assert-Contains $layout 'app-navbar' 'Layout must use the refreshed app navigation shell.'
Assert-Contains $layout 'app-main' 'Layout must wrap pages in the refreshed app main container.'
Assert-Contains $layout 'app-brand-mark' 'Layout must render the compact brand mark.'
Assert-Contains $layout 'currentController' 'Layout must compute the active navigation context.'

Assert-Contains $dashboard 'dashboard-hero' 'Dashboard must render the refreshed operational header.'
Assert-Contains $dashboard 'metric-grid' 'Dashboard must render the KPI grid.'
Assert-Contains $dashboard 'dashboard-grid' 'Dashboard must use the refreshed dashboard content grid.'
Assert-Contains $dashboard 'progress-track' 'Dashboard must render progress bars for department progress.'
Assert-Contains $dashboard 'missing-work-panel' 'Dashboard must render the missing report action panel for users.'
Assert-Contains $dashboard 'completionRate' 'Dashboard must compute completion rate from the current model.'

Assert-Contains $siteCss '\.app-navbar' 'Site CSS must style the refreshed navigation.'
Assert-Contains $siteCss '\.dashboard-hero' 'Site CSS must style the dashboard header.'
Assert-Contains $siteCss '\.metric-grid' 'Site CSS must style the KPI grid.'
Assert-Contains $siteCss '\.dashboard-grid' 'Site CSS must style the dashboard content grid.'
Assert-Contains $siteCss '\.progress-track' 'Site CSS must style department progress bars.'
Assert-Contains $siteCss '\.status-pill' 'Site CSS must define shared status chips.'

Write-Host 'Visual refresh structure checks passed.'
