$ErrorActionPreference = 'Stop'

$projectRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$packageRoot = Resolve-Path (Join-Path $projectRoot '..\packages')

$references = @(
    'System.dll',
    'System.Core.dll',
    'System.Data.dll',
    'System.Configuration.dll',
    'System.ComponentModel.DataAnnotations.dll',
    'System.Web.dll',
    'System.Xml.dll',
    'System.Xml.Linq.dll',
    'System.IO.Compression.dll',
    'System.IO.Compression.FileSystem.dll',
    (Join-Path $packageRoot 'Microsoft.AspNet.Mvc.5.2.9\lib\net45\System.Web.Mvc.dll')
)

$sources = @(
    (Join-Path $projectRoot 'Models\Enums\SystemEnums.cs'),
    (Join-Path $projectRoot 'Models\Entities\CoreEntities.cs'),
    (Join-Path $projectRoot 'Models\ViewModels\AppViewModels.cs'),
    (Join-Path $projectRoot 'Services\DbServiceBase.cs'),
    (Join-Path $projectRoot 'Services\ExcelImportExportService.cs'),
    (Join-Path $projectRoot 'Services\IndicatorServices.cs')
)

Add-Type -ReferencedAssemblies $references -Path $sources

$serviceType = [HospitalQualityDashboard.Services.IndicatorService]
$bindingFlags = [System.Reflection.BindingFlags]'NonPublic, Static'
$normalizeKey = $serviceType.GetMethod('NormalizeKey', $bindingFlags)
$resolveDepartmentIds = $serviceType.GetMethod('ResolveDepartmentIds', $bindingFlags)
$departmentLookupType = $serviceType.GetNestedType('DepartmentLookup', [System.Reflection.BindingFlags]'NonPublic')

if ($null -eq $normalizeKey) {
    throw 'Could not find NormalizeKey.'
}

if ($null -eq $resolveDepartmentIds) {
    throw 'Could not find ResolveDepartmentIds.'
}

if ($null -eq $departmentLookupType) {
    throw 'Could not find DepartmentLookup.'
}

function New-DepartmentLookup {
    param(
        [int] $Id,
        [int] $SourceId,
        [string] $Name
    )

    $lookup = [System.Activator]::CreateInstance($departmentLookupType, $true)
    $departmentLookupType.GetProperty('KhoaPhongId').SetValue($lookup, $Id, $null)
    $departmentLookupType.GetProperty('IdKhoaPhongNguon').SetValue($lookup, $SourceId, $null)
    $departmentLookupType.GetProperty('TenKhoaPhong').SetValue($lookup, $Name, $null)
    $normalizedName = [string]$normalizeKey.Invoke($null, @($Name))
    $departmentLookupType.GetProperty('NormalizedName').SetValue($lookup, $normalizedName, $null)
    return $lookup
}

$departmentListType = [System.Collections.Generic.List``1].MakeGenericType($departmentLookupType)
$departments = [System.Activator]::CreateInstance($departmentListType)
$departments.Add((New-DepartmentLookup 10 10 'Ke hoach tong hop'))
$departments.Add((New-DepartmentLookup 20 20 'To chuc can bo'))
$departments.Add((New-DepartmentLookup 30 30 'Kiem soat nhiem khuan'))

$sourceText = "P. KHTH thu thap so lieu P. TCCB tong hop so lieu"
$resolvedIds = $resolveDepartmentIds.Invoke($null, @($sourceText, $departments))

if ($resolvedIds.Count -ne 2) {
    throw "Expected 2 departments from '$sourceText', got $($resolvedIds.Count)."
}

if (-not $resolvedIds.Contains(10) -or -not $resolvedIds.Contains(20)) {
    throw "Expected KHTH and TCCB ids from '$sourceText'."
}

Write-Host 'Indicator department assignment parser verification passed.'
