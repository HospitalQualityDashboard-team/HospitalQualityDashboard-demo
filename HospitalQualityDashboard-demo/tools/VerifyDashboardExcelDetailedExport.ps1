$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$exportService = Join-Path $root 'Services\NotificationExportServices.cs'
$excelService = Join-Path $root 'Services\ExcelImportExportService.cs'
$dashboardView = Join-Path $root 'Areas\Admin\Views\Dashboard\Index.cshtml'

$exportText = Get-Content -Raw -Path $exportService
$excelText = Get-Content -Raw -Path $excelService
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
    'db_export_SoBaoCaoDatMucTieuNam',
    'db_export_SoBaoCaoDanhGiaMucTieuNam',
    'db_export_TyLeDatMucTieuNam'
)

foreach ($token in $requiredViewTokens) {
    if ($viewText -notmatch [regex]::Escape($token)) {
        throw "Missing dashboard export option token: $token"
    }
}

Write-Host 'Dashboard detailed Excel export verification passed.'
