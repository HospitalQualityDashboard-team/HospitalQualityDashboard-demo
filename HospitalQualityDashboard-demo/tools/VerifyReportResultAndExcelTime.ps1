$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ServiceSourceReader.ps1')

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$adminReportIndexPath = Join-Path $root 'Areas\Admin\Views\Report\Index.cshtml'
$userReportIndexPath = Join-Path $root 'Areas\User\Views\Report\Index.cshtml'
$userReportNhapPath = Join-Path $root 'Areas\User\Views\Report\Nhap.cshtml'

$reportService = Get-ServiceSource -Root $root -Patterns @('Services\Reports\IndicatorCalculationService.cs', 'Services\Reports\ReportService.cs')
$dashboardExport = Get-ServiceSource -Root $root -Patterns 'Services\Dashboards\Export\DashboardExcelExportService*.cs'
$notificationExport = Get-ServiceSource -Root $root -Patterns @('Services\Notifications\Notification*.cs', 'Services\Exports\ExportService.cs')
$adminReportIndex = Get-Content -Raw -Path $adminReportIndexPath
$userReportIndex = Get-Content -Raw -Path $userReportIndexPath
$userReportNhap = Get-Content -Raw -Path $userReportNhapPath

if ($reportService -notmatch 'MidpointRounding\.AwayFromZero') {
    throw 'Report result must round to 2 decimal places with explicit midpoint behavior.'
}

if ($reportService -notmatch 'GetVietnamLocalNow') {
    throw 'Report save/submit must use application-provided Vietnam local time instead of database GETDATE().'
}

if ($reportService -match 'UPDATE dbo\.BaoCao SET NgayCapNhat=GETDATE\(\)') {
    throw 'Report draft update still writes NgayCapNhat with GETDATE().'
}

if ($reportService -match 'NgayGui=GETDATE\(\)') {
    throw 'Report submit still writes NgayGui with GETDATE().'
}

if ($reportService -match 'NgayCapNhat=GETDATE\(\)') {
    throw 'Report updates must not write NgayCapNhat with GETDATE().'
}

if ($reportService -match 'DATEDIFF\(day, CAST\(GETDATE\(\) AS date\)') {
    throw 'Report missing/overdue checks must use Vietnam local date, not database GETDATE().'
}

if ($dashboardExport -notmatch 'FormatExcelDateTime') {
    throw 'Dashboard Excel export must format date/time values explicitly.'
}

if ($dashboardExport -notmatch 'dd/MM/yyyy HH:mm') {
    throw 'Dashboard Excel export must use 24-hour dd/MM/yyyy HH:mm format.'
}

if ($dashboardExport -notmatch 'FormatExcelDateTime\(x\.NgayNhap\)') {
    throw 'Dashboard Excel NgayNhap column must export a formatted text value.'
}

if ($dashboardExport -match 'DATEDIFF\(day, CAST\(GETDATE\(\) AS date\)') {
    throw 'Dashboard Excel missing-row checks must use Vietnam local date.'
}

if ($dashboardExport -match 'worksheet\.Cell\(7, 2\)\.Value = DateTime\.Now') {
    throw 'Dashboard Excel export metadata must use Vietnam local time.'
}

if ($dashboardExport -notmatch 'KhoaPhongId, NgayXuat\)') {
    throw 'Dashboard export history must persist explicit Vietnam local export time.'
}

if ($notificationExport -match 'KetQua", x => x\.KetQua\)') {
    throw 'Report Excel exports must not write raw KetQua decimals.'
}

if ($notificationExport -notmatch 'ToString\("0\.##"') {
    throw 'Report Excel exports must format KetQua with at most 2 decimal places.'
}

if ($notificationExport -match 'DatMucTieu", x => x\.DatMucTieu\)') {
    throw 'Report Excel exports must not write raw DatMucTieu booleans.'
}

if ($notificationExport -notmatch 'FormatDatMucTieu\(x\.DatMucTieu\)') {
    throw 'Report Excel exports must format DatMucTieu as Vietnamese text.'
}

if ($adminReportIndex -notmatch 'ToString\("0\.##"\)') {
    throw 'Admin report index view must display KetQua with at most 2 decimal places.'
}

foreach ($view in @($userReportIndex, $userReportNhap)) {
    if ($view -notmatch 'ToString\("0\.00"\)') {
        throw 'User report views must display KetQua with exactly 2 decimal places.'
    }
}

Write-Host 'Report result rounding and Excel time verification passed.'
