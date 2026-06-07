$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

function Read-ProjectFile {
    param([string]$RelativePath)
    return Get-Content -Raw -LiteralPath (Join-Path $root $RelativePath)
}

function Assert-Contains {
    param(
        [string]$Content,
        [string]$Pattern,
        [string]$Message
    )

    if ($Content -notmatch $Pattern) {
        throw $Message
    }
}

$reportController = Read-ProjectFile 'Controllers\ReportController.cs'
$reportService = Read-ProjectFile 'Services\ReportDashboardServices.cs'
$periodService = Read-ProjectFile 'Services\IndicatorPeriodServices.cs'
$reportIndex = Read-ProjectFile 'Views\Report\Index.cshtml'
$siteCss = Read-ProjectFile 'Content\Site.css'

Assert-Contains $reportController 'BuildUserPeriodOptions\(activePeriods\)' 'User report dropdown must be built from open periods only.'
Assert-Contains $reportController 'EnsureOpenPeriodForUser\(kyBaoCaoId\)' 'Report entry route must guard against unopened periods.'
Assert-Contains $reportController 'EnsureOpenPeriodForUser\(model\.KyBaoCaoId\)' 'Report save route must guard against posting to unopened periods.'
Assert-Contains $reportController 'EnsureOpenPeriodForUser\(report\.KyBaoCaoId\)' 'Report submit route must guard against submitting unopened periods.'
Assert-Contains $reportController 'kyBaoCaoId = null;' 'Invalid User period filters must be cleared instead of querying unopened periods.'

Assert-Contains $periodService 'public bool IsOpenForDepartment\(int periodId, int departmentId\)' 'Reporting period service must expose a department-aware open-period check.'
Assert-Contains $periodService 'ky\.TrangThai=@Mo' 'Open-period check must require period status Mo.'
Assert-Contains $periodService 'cst\.TanSuatBaoCao = ky\.LoaiKyBaoCao' 'Open-period check must respect department indicator frequency.'

Assert-Contains $reportService 'ky\.TrangThai <> @DraftPeriodStatus' 'User report history must exclude periods that have not opened.'
Assert-Contains $reportService 'Param\("@DraftPeriodStatus", \(byte\)TrangThaiKyBaoCao\.Nhap\)' 'User report history must pass the draft-period status parameter.'
Assert-Contains $reportService 'WHERE pc\.DangHoatDong = 1 AND pc\.KhoaPhongId = @KhoaPhongId\s+AND ky\.TrangThai = @Mo' 'Assigned report query must only return open periods.'

Assert-Contains $reportIndex 'report-open-grid' 'Report page must use the refreshed open-period grid.'
Assert-Contains $reportIndex 'filter-panel' 'Report page must use the refreshed filter panel.'
Assert-Contains $reportIndex 'empty-state' 'Report page must render an empty state when no reports match.'
Assert-Contains $siteCss '\.report-open-grid' 'Site CSS must style the refreshed report open-period grid.'
Assert-Contains $siteCss '\.period-card' 'Site CSS must style modern report period cards.'
Assert-Contains $siteCss '\.filter-panel' 'Site CSS must style modern report filters.'

Write-Host 'Report period visibility checks passed.'
