param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

$ErrorActionPreference = "Stop"

function Assert-Contains {
    param(
        [string]$Name,
        [string]$Text,
        [string]$Expected
    )

    if ($Text -notlike "*$Expected*") {
        throw "$Name does not contain expected text '$Expected'."
    }

    Write-Host "[PASS] $Name contains $Expected"
}

$reportServicePath = Join-Path $ProjectRoot "Services\ReportDashboardServices.cs"
$reportServiceText = Get-Content -Raw -Encoding UTF8 $reportServicePath

Assert-Contains "ReportService audit insert" $reportServiceText "INSERT INTO dbo.NhatKyHeThong"
Assert-Contains "ReportService create draft audit" $reportServiceText '"TaoNhap"'
Assert-Contains "ReportService edit draft audit" $reportServiceText '"SuaNhap"'
Assert-Contains "ReportService submit audit" $reportServiceText '"GuiBaoCao"'
Assert-Contains "ReportService before snapshot" $reportServiceText "GetReportDetailSnapshot"
Assert-Contains "ReportService after snapshot" $reportServiceText "BuildReportDetailSnapshot"
Assert-Contains "ReportService audit actor" $reportServiceText "TaiKhoanId"

Write-Host "[PASS] Report audit logging verified."
