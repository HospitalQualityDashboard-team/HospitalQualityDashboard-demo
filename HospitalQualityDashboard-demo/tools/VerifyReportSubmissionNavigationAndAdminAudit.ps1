$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$userControllerPath = Join-Path $root 'Areas\User\Controllers\ReportController.cs'
$userEditViewPath = Join-Path $root 'Areas\User\Views\Report\Edit.cshtml'
$adminReportViewPath = Join-Path $root 'Areas\Admin\Views\Report\Index.cshtml'
$viewModelPath = Join-Path $root 'Models\ViewModels\AppViewModels.cs'
$reportServicePath = Join-Path $root 'Services\ReportDashboardServices.cs'

$userController = Get-Content -Raw -Path $userControllerPath
$userEditView = Get-Content -Raw -Path $userEditViewPath
$adminReportView = Get-Content -Raw -Path $adminReportViewPath
$viewModel = Get-Content -Raw -Path $viewModelPath
$reportService = Get-Content -Raw -Path $reportServicePath

if ($userEditView -notmatch '"Nhap", new \{ kyBaoCaoId = Model\.KyBaoCaoId \}') {
    throw 'Report edit Back button must return to Nhap for the current KyBaoCaoId.'
}

if ($userController -notmatch 'RedirectToAction\("Nhap", new \{ kyBaoCaoId = model\.KyBaoCaoId \}\)') {
    throw 'Submitting from the report edit form must redirect to Nhap for the submitted KyBaoCaoId.'
}

if ($userController -notmatch 'RedirectToAction\("Nhap", new \{ kyBaoCaoId = report\.KyBaoCaoId \}\)') {
    throw 'Submitting from the report list must redirect to Nhap for the submitted KyBaoCaoId.'
}

foreach ($token in @('NgayGui', 'TenNguoiGui')) {
    if ($viewModel -notmatch $token) {
        throw "ReportEntryViewModel must expose $token."
    }

    if ($reportService -notmatch $token) {
        throw "Report service must load $token for report rows."
    }

    if ($adminReportView -notmatch $token) {
        throw "Admin report history must render $token."
    }
}

Write-Host 'Report submission navigation and admin audit verification passed.'
