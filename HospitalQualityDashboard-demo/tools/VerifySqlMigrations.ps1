$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$sqlRoot = Join-Path $root 'App_Data\Sql'

$schema = Get-Content -Raw -Path (Join-Path $sqlRoot '001_CreateSchema.sql')
$indexes = Get-Content -Raw -Path (Join-Path $sqlRoot '002_PerformanceIndexes.sql')
$exportHistory = Get-Content -Raw -Path (Join-Path $sqlRoot '003_AddExportHistory.sql')
$indicatorDeployment = Get-Content -Raw -Path (Join-Path $sqlRoot '005_AddIndicatorDeploymentHistory.sql')
$bootstrapper = Get-Content -Raw -Path (Join-Path $root 'Services\Infrastructure\Database\DatabaseBootstrapper.cs')
$indicatorService = Get-Content -Raw -Path (Join-Path $root 'Services\Indicators\IndicatorService.cs')

if ($bootstrapper -notmatch [regex]::Escape('RunOptionalScript(connection, scriptDirectory, "003_AddExportHistory.sql")')) {
    throw 'Database bootstrapper must run 003_AddExportHistory.sql so export-history indexes are applied.'
}

$duplicateIndexTokens = @(
    'IX_PhanCongChiSo_DepartmentActiveIndicator',
    'IX_ChiSoTanSuatBaoCao_IndicatorFrequency',
    'IX_ThongBaoNguoiNhan_AccountRead'
)

foreach ($token in $duplicateIndexTokens) {
    if ($indexes -match [regex]::Escape($token)) {
        throw "002_PerformanceIndexes.sql still creates duplicate index: $token"
    }
}

$requiredIndexTokens = @(
    'IX_BaoCao_PeriodDeptIndicatorStatus',
    'IX_NhanVien_DepartmentName'
)

foreach ($token in $requiredIndexTokens) {
    if ($indexes -notmatch [regex]::Escape($token)) {
        throw "002_PerformanceIndexes.sql is missing useful index: $token"
    }
}

$constraintTokens = @(
    'FK_LichSuTrienKhaiChiSo_ChiSo',
    'FK_LichSuTrienKhaiChiSo_NguoiTao',
    'FK_LichSuTrienKhaiChiSo_NguoiKetThuc',
    'CK_LichSuTrienKhaiChiSo_TanSuat',
    'CK_LichSuTrienKhaiChiSo_DateRange'
)

foreach ($token in $constraintTokens) {
    $pattern = 'ADD\s+CONSTRAINT\s+' + [regex]::Escape($token)
    if ($indicatorDeployment -notmatch $pattern) {
        throw "005_AddIndicatorDeploymentHistory.sql must add constraint outside create-table-only path: $token"
    }
}

if ($indicatorService -notmatch [regex]::Escape('DELETE FROM dbo.LichSuTrienKhaiChiSo WHERE ChiSoChatLuongId=@Id')) {
    throw 'Indicator delete must remove deployment history before deleting ChiSoChatLuong.'
}

if ($schema -notmatch [regex]::Escape('CREATE TABLE dbo.LichSuTrienKhaiChiSo')) {
    throw '001_CreateSchema.sql must include deployment history table for fresh databases.'
}

if ($exportHistory -notmatch [regex]::Escape('IX_LichSuXuatBaoCao_NgayXuat')) {
    throw '003_AddExportHistory.sql must keep export-history date index.'
}

Write-Host 'SQL migration structural verification passed.'
