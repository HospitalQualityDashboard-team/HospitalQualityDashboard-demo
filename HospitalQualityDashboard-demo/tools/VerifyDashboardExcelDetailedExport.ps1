$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ServiceSourceReader.ps1')

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$dashboardView = Join-Path $root 'Areas\Admin\Views\Dashboard\Index.cshtml'

$exportText = Get-ServiceSource -Root $root -Patterns 'Services\Exports\ExportService.cs'
$excelText = Get-ServiceSource -Root $root -Patterns @('Services\Excel\ExcelImportExportService*.cs', 'Services\Excel\ExcelWorksheetExport.cs')
$viewText = Get-Content -Raw -Path $dashboardView

$requiredExportTokens = @(
    'CreateXlsxWorkbook',
    'TongHopTienDo',
    'ChiTietSoLieu',
    'QueryDashboardReportDetails',
    'GiaTriNhap',
    'KetQua',
    'MucTieuNam',
    'DatMucTieuText',
    'TyLeDatMucTieuNam',
    'SoBaoCaoDatMucTieuNam',
    'SoBaoCaoDanhGiaMucTieuNam'
)

foreach ($token in $requiredExportTokens) {
    if ($exportText -notmatch [regex]::Escape($token)) {
        throw "Missing dashboard detailed export token: $token"
    }
}

$requiredExcelTokens = @(
    'ExcelWorksheetExport',
    'CreateXlsxWorkbook',
    'workbook.xml.rels',
    'BuildWorkbookRelationshipsXml',
    'sheet{0}.xml'
)

foreach ($token in $requiredExcelTokens) {
    if ($excelText -notmatch [regex]::Escape($token)) {
        throw "Missing multi-sheet Excel support token: $token"
    }
}

$requiredViewTokens = @(
    'dashboardExportModal',
    'enable-dashboard-comparison',
    'dashboard-comparison-periods',
    'ComparisonPeriodIds'
)

foreach ($token in $requiredViewTokens) {
    if ($viewText -notmatch [regex]::Escape($token)) {
        throw "Missing dashboard export modal token: $token"
    }
}

$removedViewTokens = @(
    'dashboard-export-column',
    'dashboard-export-select-all',
    'dashboard-export-clear',
    'db_export_'
)

foreach ($token in $removedViewTokens) {
    if ($viewText -match [regex]::Escape($token)) {
        throw "Dashboard export still contains removed column selector token: $token"
    }
}

Write-Host 'Dashboard detailed Excel export verification passed.'
