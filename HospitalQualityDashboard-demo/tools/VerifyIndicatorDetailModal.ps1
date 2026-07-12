$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$adminControllerPath = Join-Path $root 'Areas\Admin\Controllers\IndicatorController.cs'
$userControllerPath = Join-Path $root 'Areas\User\Controllers\IndicatorController.cs'
$adminViewPath = Join-Path $root 'Areas\Admin\Views\Indicator\Index.cshtml'
$userViewPath = Join-Path $root 'Areas\User\Views\Indicator\Index.cshtml'
$modalPath = Join-Path $root 'Views\Shared\_IndicatorDetailModal.cshtml'
$contentPath = Join-Path $root 'Views\Shared\_IndicatorDetailContent.cshtml'
$siteCssPath = Join-Path $root 'Content\Site.css'
$projectPath = Join-Path $root 'HospitalQualityDashboard-demo.csproj'

foreach ($path in @($modalPath, $contentPath)) {
    if (-not (Test-Path $path)) {
        throw "Missing indicator detail partial: $path"
    }
}

$adminController = Get-Content -Raw -Path $adminControllerPath
$userController = Get-Content -Raw -Path $userControllerPath
$adminView = Get-Content -Raw -Path $adminViewPath
$userView = Get-Content -Raw -Path $userViewPath
$modal = Get-Content -Raw -Path $modalPath
$content = Get-Content -Raw -Path $contentPath
$siteCss = Get-Content -Raw -Path $siteCssPath
$project = Get-Content -Raw -Path $projectPath

foreach ($controller in @($adminController, $userController)) {
    if ($controller -notmatch 'ActionResult DetailsPartial\(int id\)') {
        throw 'Both Indicator controllers must expose DetailsPartial(int id).'
    }

    if ($controller -notmatch 'PartialView\([^\r\n]*_IndicatorDetailContent[^\r\n]*,\s*model\)') {
        throw 'DetailsPartial must render the shared indicator detail content partial.'
    }
}

if ($userController -notmatch 'IsAssigned\(id, CurrentKhoaPhongId\.Value\)') {
    throw 'User DetailsPartial must enforce the same assigned-indicator authorization as Details.'
}

foreach ($view in @($adminView, $userView)) {
    if ($view -notmatch 'Html\.Partial\("_IndicatorDetailModal"\)') {
        throw 'Both indicator index views must render the shared indicator detail modal exactly once.'
    }

    foreach ($token in @(
        'data-bs-toggle="modal"',
        'data-bs-target="#indicatorDetailModal"',
        'data-indicator-detail-url=',
        'href="@Url.Action("Details"'
    )) {
        if ($view -notmatch [regex]::Escape($token)) {
            throw "Indicator view must contain modal trigger token: $token"
        }
    }

    if ($view -match 'ActionLink\("Chi tiết", "Details"') {
        throw 'Indicator Chi tiet must no longer use ActionLink navigation as the primary behavior.'
    }
}

foreach ($token in @(
    'id="indicatorDetailModal"',
    'indicator-detail-modal',
    'data-indicator-detail-content',
    'data-indicator-detail-loading',
    'data-indicator-detail-error',
    'data-indicator-detail-retry',
    'data-bs-dismiss="modal"'
)) {
    if ($modal -notmatch [regex]::Escape($token)) {
        throw "Indicator detail modal partial must contain $token."
    }
}

foreach ($token in @(
    'indicator-detail-grid',
    'indicator-detail-section',
    'MaChiSo',
    'TenChiSo',
    'DinhNghia',
    'TanSuatBaoCaoText',
    'PhuongPhapTinh',
    'TuSoMoTa',
    'MauSoMoTa',
    'NguonSoLieu',
    'MucTieuHienThi'
)) {
    if ($content -notmatch [regex]::Escape($token)) {
        throw "Indicator detail content partial must contain $token."
    }
}

foreach ($token in @(
    'indicator-detail-modal',
    'indicator-detail-grid',
    'indicator-detail-section',
    'indicator-detail-loading',
    'body.indicator-detail-modal-open .app-shell'
)) {
    if ($siteCss -notmatch [regex]::Escape($token)) {
        throw "Site.css must contain indicator modal styling token $token."
    }
}

foreach ($token in @(
    'Views\Shared\_IndicatorDetailModal.cshtml',
    'Views\Shared\_IndicatorDetailContent.cshtml',
    'tools\VerifyIndicatorDetailModal.ps1'
)) {
    if ($project -notmatch [regex]::Escape($token)) {
        throw "Project file must include $token."
    }
}

Write-Host 'Indicator detail modal verification passed.'
