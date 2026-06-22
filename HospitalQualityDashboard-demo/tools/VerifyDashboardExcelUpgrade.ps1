$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ServiceSourceReader.ps1')

$root = Resolve-Path (Join-Path $PSScriptRoot '..')

$paths = @{
    ExportDtos = Join-Path $root 'Models\DTOs\ExportDtos.cs'
    ViewModels = Join-Path $root 'Models\ViewModels\AppViewModels.cs'
    AdminExportController = Join-Path $root 'Areas\Admin\Controllers\ExportController.cs'
    UserExportController = Join-Path $root 'Areas\User\Controllers\ExportController.cs'
    AdminDashboardView = Join-Path $root 'Areas\Admin\Views\Dashboard\Index.cshtml'
    UserDashboardView = Join-Path $root 'Areas\User\Views\Dashboard\Index.cshtml'
    ExportService = Join-Path $root 'Services\Dashboards\Export'
    SqlScript = Join-Path $root 'App_Data\Sql\003_AddExportHistory.sql'
    PackagesConfig = Join-Path $root 'packages.config'
    Csproj = Join-Path $root 'HospitalQualityDashboard-demo.csproj'
}

foreach ($entry in $paths.GetEnumerator()) {
    if (-not (Test-Path $entry.Value)) {
        throw "Missing required file for Dashboard Excel upgrade: $($entry.Key) -> $($entry.Value)"
    }
}

$dashboardExportSource = Get-ServiceSource -Root $root -Patterns 'Services\Dashboards\Export\DashboardExcelExportService*.cs'

$checks = @(
    @{ Path = $paths.ExportDtos; Tokens = @('DashboardExcelExportQueryDto', 'ExportUserContextDto', 'TrangThaiNhapLieu', 'TrangThaiDuyet', 'DatMucTieu') },
    @{ Path = $paths.ViewModels; Tokens = @('DashboardExcelDetailRow', 'DashboardDepartmentSummaryRow', 'DashboardMissingIndicatorRow', 'DashboardReviewHistoryRow', 'ExportHistoryViewModel') },
    @{ Path = $paths.ExportService; Source = $dashboardExportSource; Tokens = @('class DashboardExcelExportService', 'BuildDashboardExcel', 'TongQuan', 'ChiTietChiSo', 'TheoKhoaPhong', 'ChiSoChuaNhap', 'ChiSoChuaDat', 'LichSuDuyet', 'LichSuXuatBaoCao', 'ClosedXML.Excel') },
    @{ Path = $paths.AdminExportController; Tokens = @('Dashboard(DashboardExcelExportQueryDto query)', 'BuildDashboardExcel', 'ExportUserContextDto') },
    @{ Path = $paths.UserExportController; Tokens = @('Dashboard(DashboardExcelExportQueryDto query)', 'CurrentKhoaPhongId', 'BuildDashboardExcel') },
    @{ Path = $paths.AdminDashboardView; Tokens = @('NamBaoCao', 'KyBaoCaoId', 'KhoaPhongId', 'LinhVuc', 'TrangThaiNhapLieu', 'TrangThaiDuyet', 'DatMucTieu', 'Dashboard", "Export"') },
    @{ Path = $paths.UserDashboardView; Tokens = @('NamBaoCao', 'KyBaoCaoId', 'LinhVuc', 'TrangThaiNhapLieu', 'TrangThaiDuyet', 'DatMucTieu', 'Dashboard", "Export"') },
    @{ Path = $paths.SqlScript; Tokens = @('CREATE TABLE dbo.LichSuXuatBaoCao', 'NguoiDungId', 'LoaiBaoCao', 'BoLoc', 'TenFile', 'SoDongDuLieu', 'DiaChiIP') },
    @{ Path = $paths.PackagesConfig; Tokens = @('ClosedXML', 'DocumentFormat.OpenXml', 'ExcelNumberFormat', 'SixLabors.Fonts', 'System.Memory', 'System.Buffers', 'System.Numerics.Vectors', 'System.Runtime.CompilerServices.Unsafe') },
    @{ Path = $paths.Csproj; Tokens = @('DashboardExcelExportService.cs', 'ClosedXML.dll', 'DocumentFormat.OpenXml.dll', 'System.Memory.dll', 'System.Buffers.dll', 'System.Numerics.Vectors.dll', 'System.Runtime.CompilerServices.Unsafe.dll') }
)

foreach ($check in $checks) {
    $text = if ($check.ContainsKey('Source')) { $check.Source } else { Get-Content -Raw -Path $check.Path }
    foreach ($token in $check.Tokens) {
        if ($text -notmatch [regex]::Escape($token)) {
            throw "Missing token '$token' in $($check.Path)"
        }
    }
}

Write-Host 'Dashboard Excel upgrade structural verification passed.'
