$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ServiceSourceReader.ps1')

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$userControllerPath = Join-Path $root 'Areas\User\Controllers\ReportController.cs'
$userIndexViewPath = Join-Path $root 'Areas\User\Views\Report\Index.cshtml'
$userEditViewPath = Join-Path $root 'Areas\User\Views\Report\Edit.cshtml'
$viewModelPath = Join-Path $root 'Models\ViewModels\AppViewModels.cs'

function Read-Utf8File([string]$Path) {
    return [System.Text.Encoding]::UTF8.GetString([System.IO.File]::ReadAllBytes($Path))
}

$userController = Read-Utf8File $userControllerPath
$userIndexView = Read-Utf8File $userIndexViewPath
$userEditView = Read-Utf8File $userEditViewPath
$viewModel = Read-Utf8File $viewModelPath
$reportService = Get-ServiceSource -Root $root -Patterns 'Services\Reports\ReportService.cs'

$lockedMessage = -join @(
    [char]0x004b, [char]0x1ef3, [char]0x0020,
    [char]0x0062, [char]0x00e1, [char]0x006f, [char]0x0020,
    [char]0x0063, [char]0x00e1, [char]0x006f, [char]0x0020,
    [char]0x0111, [char]0x00e3, [char]0x0020,
    [char]0x006b, [char]0x0068, [char]0x00f3, [char]0x0061, [char]0x002c, [char]0x0020,
    [char]0x006b, [char]0x0068, [char]0x00f4, [char]0x006e, [char]0x0067, [char]0x0020,
    [char]0x0074, [char]0x0068, [char]0x1ec3, [char]0x0020,
    [char]0x0067, [char]0x1eed, [char]0x0069, [char]0x0020,
    [char]0x0062, [char]0x00e1, [char]0x006f, [char]0x0020,
    [char]0x0063, [char]0x00e1, [char]0x006f, [char]0x0020,
    [char]0x006c, [char]0x1ea1, [char]0x0069, [char]0x002e
)

if ($userController -notmatch [regex]::Escape($lockedMessage)) {
    throw 'User report controller must use the locked-period business message.'
}

if ($userController -match 'HttpStatusCodeResult\(403,') {
    throw 'User report controller must not render the raw IIS 403 page for closed reporting periods.'
}

if ($viewModel -notmatch 'TrangThaiKyBaoCao') {
    throw 'ReportEntryViewModel must expose the reporting period status.'
}

if ($reportService -notmatch 'TrangThaiKyBaoCao') {
    throw 'ReportService must load the reporting period status for report rows.'
}

if ($userEditView -notmatch 'periodLockedMessage') {
    throw 'User report edit view must render the locked-period message from the controller.'
}

if ($userEditView -notmatch 'TrangThaiKyBaoCao\.Mo') {
    throw 'User report edit view must disable editing when the reporting period is not open.'
}

if ($userIndexView -notmatch 'TempData\["Error"\]') {
    throw 'User report index view must render business errors redirected from report actions.'
}

Write-Host 'User report locked return message verification passed.'
