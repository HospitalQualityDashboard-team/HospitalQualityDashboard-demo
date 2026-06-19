$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$enumPath = Join-Path $root 'Models\Enums\SystemEnums.cs'
$builderPath = Join-Path $root 'Services\IndicatorWarningMessageBuilder.cs'

if (-not (Test-Path $builderPath)) {
    throw 'IndicatorWarningMessageBuilder.cs is missing.'
}

$builderSource = Get-Content -Raw -Encoding UTF8 $builderPath
$builderSource = $builderSource -replace '(?m)^using HospitalQualityDashboardDemo\.Models\.Enums;\r?\n', ''
$builderSource = $builderSource -replace '(?m)^using System;\r?\n', ''
$source = "using System;`r`nusing HospitalQualityDashboardDemo.Models.Enums;`r`n" +
          (Get-Content -Raw -Encoding UTF8 $enumPath) + [Environment]::NewLine +
          $builderSource
Add-Type -TypeDefinition $source -Language CSharp

$builder = [HospitalQualityDashboardDemo.Services.IndicatorWarningMessageBuilder]
$fromBase64 = {
    param([string]$Value)
    [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($Value))
}
$indicatorCode = &$fromBase64 'Q1MwNTAw'
$indicatorName = &$fromBase64 'Q2jhu4kgc+G7kSB0ZXN0'
$expectedTitle = &$fromBase64 'Q+G6o25oIGLDoW8gY2jhu4kgc+G7kSBDUzA1MDAgLSBDaOG7iSBz4buRIHRlc3Q='
$cases = @(
    @{
        Now = [datetime]'2026-06-19T23:30:00'
        Due = [datetime]'2026-06-30T23:59:00'
        Type = 'NhacHan'
        Body = &$fromBase64 'Q2jhu4kgc+G7kSBDUzA1MDAgLSBDaOG7iSBz4buRIHRlc3Q6IEPDsm4gMTEgbmfDoHkgxJHhur9uIGjhuqFuIG7hu5lwLiBWdWkgbMOybmcgY2h14bqpbiBi4buLIHbDoCBu4buZcCDEkcO6bmcgaOG6oW4u'
    },
    @{
        Now = [datetime]'2026-06-30T00:01:00'
        Due = [datetime]'2026-06-30T23:59:00'
        Type = 'HanNopHomNay'
        Body = &$fromBase64 'Q2jhu4kgc+G7kSBDUzA1MDAgLSBDaOG7iSBz4buRIHRlc3Q6IEjDtG0gbmF5IGzDoCBo4bqhbiBu4buZcC4gVnVpIGzDsm5nIG7hu5lwIHRyxrDhu5tjIGtoaSBo4bq/dCBuZ8OgeS4='
    },
    @{
        Now = [datetime]'2026-07-03T08:00:00'
        Due = [datetime]'2026-06-30T23:59:00'
        Type = 'QuaHan'
        Body = &$fromBase64 'Q2jhu4kgc+G7kSBDUzA1MDAgLSBDaOG7iSBz4buRIHRlc3Q6IMSQw6MgcXXDoSBo4bqhbiBu4buZcCAzIG5nw6B5LiBWdWkgbMOybmcgY+G6rXAgbmjhuq10IG5nYXkgxJHhu4MgdHLDoW5oIOG6o25oIGjGsOG7n25nIMSR4bq/biB0aeG6v24gxJHhu5ku'
    }
)

foreach ($case in $cases) {
    $result = $builder::Build($indicatorCode, $indicatorName, $case.Due, $case.Now)
    if ($result.Title -ne $expectedTitle) {
        throw "Unexpected title: $($result.Title)"
    }
    if ($result.Body -ne $case.Body) {
        throw "Unexpected body: $($result.Body)"
    }
    if ([string]$result.NotificationType -ne $case.Type) {
        throw "Unexpected type: $($result.NotificationType)"
    }
}

Write-Host 'Indicator warning message verification passed.'
