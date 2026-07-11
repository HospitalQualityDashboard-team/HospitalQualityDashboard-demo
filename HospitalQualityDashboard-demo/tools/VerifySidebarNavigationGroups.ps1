$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$layoutPath = Join-Path $root 'Views\Shared\_Layout.cshtml'
$cssPath = Join-Path $root 'Content\Site.css'

$layout = Get-Content -Raw -Encoding UTF8 -Path $layoutPath
$css = Get-Content -Raw -Encoding UTF8 -Path $cssPath

foreach ($token in @(
    'class="app-nav-section"',
    'class="app-nav-heading"'
)) {
    if ($layout -notmatch [regex]::Escape($token)) {
        throw "Sidebar layout must render navigation group token: $token"
    }
}

if (([regex]::Matches($layout, 'class="app-nav-heading"')).Count -lt 5) {
    throw 'Sidebar layout must render at least five grouped navigation headings.'
}

if ($layout -notmatch 'app-nav-heading">TRANG CH[\s\S]*ActionLink\("[^"]+", "Index", "Dashboard"') {
    throw 'Dashboard navigation must sit under the home group.'
}

foreach ($token in @(
    '.app-nav-section',
    '.app-nav-heading',
    '.app-nav-section + .app-nav-section'
)) {
    if ($css -notmatch [regex]::Escape($token)) {
        throw "Sidebar CSS must style navigation group token: $token"
    }
}

Write-Host 'Sidebar navigation group verification passed.'
