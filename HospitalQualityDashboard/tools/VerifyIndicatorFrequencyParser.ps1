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
$tryParseFrequencies = $serviceType.GetMethod('TryParseFrequencies', $bindingFlags)
$formatFrequency = $serviceType.GetMethod('FormatFrequency', $bindingFlags)

if ($null -eq $tryParseFrequencies) {
    throw 'Could not find TryParseFrequencies.'
}

if ($null -eq $formatFrequency) {
    throw 'Could not find FormatFrequency.'
}

function ConvertFrom-UnicodeEscape {
    param([string] $Value)
    return [System.Text.RegularExpressions.Regex]::Unescape($Value)
}

$quarterlyCases = @(
    (ConvertFrom-UnicodeEscape 'H\u00e0ng qu\u00fd'),
    (ConvertFrom-UnicodeEscape 'M\u1ed7i qu\u00fd.'),
    (ConvertFrom-UnicodeEscape 'Theo qu\u00fd'),
    (ConvertFrom-UnicodeEscape 'Khoa KSNK b\u00e1o c\u00e1o k\u1ebft qu\u1ea3 h\u00e0ng qu\u00fd v\u1ec1 ph\u00f2ng \u0111i\u1ec1u d\u01b0\u1ee1ng')
)

foreach ($case in $quarterlyCases) {
    $args = @($case, $null)
    $parsed = [bool]$tryParseFrequencies.Invoke($null, $args)
    if (-not $parsed) {
        throw "Expected '$case' to parse as a report frequency."
    }

    $frequencies = $args[1]
    if (-not $frequencies.Contains([HospitalQualityDashboard.Models.Enums.TanSuatBaoCao]::HangQuy)) {
        throw "Expected '$case' to include HangQuy."
    }
}

$label = [string]$formatFrequency.Invoke($null, @([HospitalQualityDashboard.Models.Enums.TanSuatBaoCao]::HangQuy))
if ($label -ne 'Hang quy') {
    throw "Expected HangQuy display label to be 'Hang quy', got '$label'."
}

Write-Host 'Indicator frequency parser verification passed.'
