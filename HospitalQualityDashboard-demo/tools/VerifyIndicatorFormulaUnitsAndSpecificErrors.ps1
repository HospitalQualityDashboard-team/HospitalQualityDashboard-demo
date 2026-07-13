$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$solutionRoot = Resolve-Path (Join-Path $root '..')
$enumPath = Join-Path $root 'Models\Enums\SystemEnums.cs'
$helperPath = Join-Path $root 'Services\Indicators\IndicatorFormulaDisplay.cs'
$indicatorViewPath = Join-Path $root 'Areas\Admin\Views\Indicator\Edit.cshtml'
$indicatorControllerPath = Join-Path $root 'Areas\Admin\Controllers\IndicatorController.cs'
$departmentControllerPath = Join-Path $root 'Areas\Admin\Controllers\DepartmentController.cs'
$departmentServicePath = Join-Path $root 'Services\Departments\DepartmentService.cs'
$indicatorServicePath = Join-Path $root 'Services\Indicators\IndicatorService.cs'

foreach ($path in @($enumPath, $helperPath, $indicatorViewPath, $indicatorControllerPath, $departmentControllerPath, $departmentServicePath, $indicatorServicePath)) {
    if (-not (Test-Path $path)) {
        throw "Missing expected file: $path"
    }
}

$fromBase64 = {
    param([string]$Value)
    [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($Value))
}

$helperSource = Get-Content -Raw -Encoding UTF8 $helperPath
$helperSource = $helperSource -replace '(?m)^using .+;\r?\n', ''
$source = "using System;`r`nusing System.Collections.Generic;`r`nusing System.Globalization;`r`nusing System.Linq;`r`nusing System.Web.Script.Serialization;`r`nusing HospitalQualityDashboardDemo.Models.Enums;`r`n" +
          (Get-Content -Raw -Encoding UTF8 $enumPath) + [Environment]::NewLine +
          $helperSource
Add-Type -TypeDefinition $source -Language CSharp -ReferencedAssemblies @('System.Web.Extensions')

$display = [HospitalQualityDashboardDemo.Services.IndicatorFormulaDisplay]
$formula = [HospitalQualityDashboardDemo.Models.Enums.LoaiCongThuc]
$expectedUnits = @(
    @{ Formula = $formula::TyLe; Unit = '%' },
    @{ Formula = $formula::TySo; Unit = (&$fromBase64 'dOG7tyBz4buR') },
    @{ Formula = $formula::SoLuong; Unit = (&$fromBase64 'c+G7kSBsxrDhu6NuZw==') },
    @{ Formula = $formula::ThoiGianTrungBinh; Unit = (&$fromBase64 'dGjhu51pIGdpYW4=') },
    @{ Formula = $formula::DiemTrungBinh; Unit = (&$fromBase64 'xJFp4buDbQ==') },
    @{ Formula = $formula::GiaTriTrucTiep; Unit = (&$fromBase64 'Z2nDoSB0cuG7iw==') }
)

foreach ($case in $expectedUnits) {
    $actual = $display::GetUnit($case.Formula)
    if ($actual -ne $case.Unit) {
        throw "Unexpected unit for $($case.Formula): '$actual'"
    }
}

$indicatorView = Get-Content -Raw -Encoding UTF8 $indicatorViewPath
if ($indicatorView -match 'TextBoxFor\(m => m\.DonViTinh') {
    throw 'Indicator form still renders DonViTinh as a free text box.'
}
foreach ($required in @('HiddenFor(m => m.DonViTinh)', 'data-formula-units', 'indicator-unit-display')) {
    if ($indicatorView -notmatch [regex]::Escape($required)) {
        throw "Indicator form is missing required marker: $required"
    }
}

$indicatorController = Get-Content -Raw -Encoding UTF8 $indicatorControllerPath
$indicatorDuplicateMessage = &$fromBase64 'TcOjIGNo4buJIHPhu5EgxJHDoyB04buTbiB04bqhaSwgdnVpIGzDsm5nIG5o4bqtcCBtw6Mga2jDoWMu'
if ($indicatorController -notmatch ('ModelState\.AddModelError\("MaChiSo", "' + [regex]::Escape($indicatorDuplicateMessage) + '"\)')) {
    throw 'Indicator duplicate code validation message is missing.'
}

$departmentController = Get-Content -Raw -Encoding UTF8 $departmentControllerPath
$departmentDuplicateMessage = &$fromBase64 'TcOjIGtob2EvcGjDsm5nIG5ndeG7k24gxJHDoyB04buTbiB04bqhaSwgdnVpIGzDsm5nIG5o4bqtcCBtw6Mga2jDoWMu'
if ($departmentController -notmatch ('ModelState\.AddModelError\("IdKhoaPhongNguon", "' + [regex]::Escape($departmentDuplicateMessage) + '"\)')) {
    throw 'Department duplicate source id validation message is missing.'
}

$departmentService = Get-Content -Raw -Encoding UTF8 $departmentServicePath
foreach ($message in @(
    (&$fromBase64 'S2jDtG5nIHRo4buDIHjDs2EgdsOsIGtob2EvcGjDsm5nIMSRYW5nIGPDsyBuaMOibiB2acOqbi4gVnVpIGzDsm5nIGtow7NhIGtob2EvcGjDsm5nIHRoYXkgdsOsIHjDs2Eu'),
    (&$fromBase64 'S2jDtG5nIHRo4buDIHjDs2EgdsOsIGtob2EvcGjDsm5nIMSRYW5nIGPDsyB0w6BpIGtob+G6o24gbmfGsOG7nWkgZMO5bmcu'),
    (&$fromBase64 'S2jDtG5nIHRo4buDIHjDs2EgdsOsIGtob2EvcGjDsm5nIMSRYW5nIMSRxrDhu6NjIHBow6JuIGPDtG5nIGNo4buJIHPhu5EuIFZ1aSBsw7JuZyB04bqhbSBk4burbmcgcGjDom4gY8O0bmcgaG/hurdjIGtow7NhIGtob2EvcGjDsm5nLg=='),
    (&$fromBase64 'S2jDtG5nIHRo4buDIHjDs2EgdsOsIGtob2EvcGjDsm5nIMSRw6MgcGjDoXQgc2luaCBiw6FvIGPDoW8uIFZ1aSBsw7JuZyBraMOzYSBraG9hL3Bow7JuZyB0aGF5IHbDrCB4w7NhLg=='),
    (&$fromBase64 'S2jDtG5nIHRo4buDIHjDs2EgdsOsIGtob2EvcGjDsm5nIMSRYW5nIMSRxrDhu6NjIGfhuq9uIGzDoG0gxJHGoW4gduG7iyB0aHUgdGjhuq1wL3Thu5VuZyBo4bujcCBjaOG7iSBz4buRLg==')
)) {
    if ($departmentService -notmatch [regex]::Escape($message)) {
        throw "Department delete message missing: $message"
    }
}

$indicatorService = Get-Content -Raw -Encoding UTF8 $indicatorServicePath
foreach ($message in @(
    (&$fromBase64 'S2jDtG5nIHRo4buDIHjDs2EgdsOsIGNo4buJIHPhu5EgxJHDoyBwaMOhdCBzaW5oIGLDoW8gY8Ohby4gVnVpIGzDsm5nIG5n4burbmcgdHJp4buDbiBraGFpIHRoYXkgdsOsIHjDs2Eu'),
    (&$fromBase64 'S2jDtG5nIHRo4buDIHjDs2EgdsOsIGNo4buJIHPhu5EgxJFhbmcgxJHGsOG7o2MgcGjDom4gY8O0bmcgY2hvIGtob2EvcGjDsm5nLg=='),
    (&$fromBase64 'S2jDtG5nIHRo4buDIHjDs2EgdsOsIGNo4buJIHPhu5EgxJHDoyBjw7MgdGjDtG5nIGLDoW8gaG/hurdjIGPhuqNuaCBiw6FvIGxpw6puIHF1YW4u')
)) {
    if ($indicatorService -notmatch [regex]::Escape($message)) {
        throw "Indicator delete message missing: $message"
    }
}

Write-Host 'Indicator formula units and specific error verification passed.'
