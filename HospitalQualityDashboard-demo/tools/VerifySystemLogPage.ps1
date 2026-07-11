$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$controllerPath = Join-Path $root 'Areas\Admin\Controllers\SystemLogController.cs'
$servicePath = Join-Path $root 'Services\SystemLogs\SystemLogService.cs'
$viewPath = Join-Path $root 'Areas\Admin\Views\SystemLog\Index.cshtml'
$viewModelPath = Join-Path $root 'Models\ViewModels\AppViewModels.cs'
$dtoPath = Join-Path $root 'Models\DTOs\SystemLogDtos.cs'
$layoutPath = Join-Path $root 'Views\Shared\_Layout.cshtml'
$projectPath = Join-Path $root 'HospitalQualityDashboard-demo.csproj'

foreach ($path in @($controllerPath, $servicePath, $viewPath, $dtoPath)) {
    if (-not (Test-Path $path)) {
        throw "Required SystemLog file is missing: $path"
    }
}

$controller = Get-Content -Raw -Encoding UTF8 -Path $controllerPath
$service = Get-Content -Raw -Encoding UTF8 -Path $servicePath
$view = Get-Content -Raw -Encoding UTF8 -Path $viewPath
$viewModel = Get-Content -Raw -Encoding UTF8 -Path $viewModelPath
$dto = Get-Content -Raw -Encoding UTF8 -Path $dtoPath
$layout = Get-Content -Raw -Encoding UTF8 -Path $layoutPath
$project = Get-Content -Raw -Encoding UTF8 -Path $projectPath

foreach ($token in @(
    'public class SystemLogController : AdminBaseController',
    'ActionResult Index(SystemLogQueryDto query, int page = 1)',
    'private const int DefaultPageSize = 10;',
    'new SystemLogIndexViewModel'
)) {
    if ($controller -notmatch [regex]::Escape($token)) {
        throw "SystemLogController must contain token: $token"
    }
}

foreach ($token in @(
    'public class SystemLogService : DbServiceBase',
    'FROM dbo.NhatKyHeThong',
    'LEFT JOIN dbo.TaiKhoan',
    'LEFT JOIN dbo.NhanVien',
    'ORDER BY logs.ThoiGian DESC, logs.NhatKyHeThongId DESC',
    'OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY',
    'GetModuleOptions',
    'GetActionOptions'
)) {
    if ($service -notmatch [regex]::Escape($token)) {
        throw "SystemLogService must contain token: $token"
    }
}

foreach ($token in @('AccountKeyword', 'Module', 'LogAction', 'FromDate', 'ToDate')) {
    if ($dto -notmatch $token) {
        throw "SystemLogQueryDto must expose $token."
    }

    if ($view -notmatch $token) {
        throw "SystemLog view must render filter $token."
    }
}

foreach ($token in @('SummarizeLogContent', 'title="@(item.NoiDung ?? "")"', 'system-log-description')) {
    if ($view -notmatch [regex]::Escape($token)) {
        throw "SystemLog view must summarize long descriptions and preserve full content with token: $token"
    }
}

if ($dto -match 'public string Action \{ get; set; \}') {
    throw 'SystemLogQueryDto must not expose Action because MVC binds route action=Index into that property.'
}

if ($service -match '@Action') {
    throw 'SystemLogService must use @LogAction, not @Action, to avoid route-action binding collisions.'
}

foreach ($token in @(
    'SystemLogRowViewModel',
    'SystemLogIndexViewModel',
    'IList<SystemLogRowViewModel>',
    'IList<SelectListItem>'
)) {
    if ($viewModel -notmatch [regex]::Escape($token)) {
        throw "SystemLog view models must contain token: $token"
    }
}

if ($layout -notmatch 'SystemLog') {
    throw 'Admin sidebar must link to SystemLog.'
}

foreach ($token in @(
    'Areas\Admin\Controllers\SystemLogController.cs',
    'Services\SystemLogs\SystemLogService.cs',
    'Models\DTOs\SystemLogDtos.cs',
    'Areas\Admin\Views\SystemLog\Index.cshtml',
    'tools\VerifySystemLogPage.ps1'
)) {
    if ($project -notmatch [regex]::Escape($token)) {
        throw "Project file must include $token."
    }
}

Write-Host 'SystemLog page verification passed.'
