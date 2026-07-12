$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$controllerPath = Join-Path $root 'Areas\Admin\Controllers\SystemLogController.cs'
$servicePath = Join-Path $root 'Services\SystemLogs\SystemLogService.cs'
$viewPath = Join-Path $root 'Areas\Admin\Views\SystemLog\Index.cshtml'
$viewModelPath = Join-Path $root 'Models\ViewModels\AppViewModels.cs'
$dtoPath = Join-Path $root 'Models\DTOs\SystemLogDtos.cs'
$layoutPath = Join-Path $root 'Views\Shared\_Layout.cshtml'
$siteCssPath = Join-Path $root 'Content\Site.css'
$scriptPath = Join-Path $root 'Scripts\date-input.js'
$projectPath = Join-Path $root 'HospitalQualityDashboard-demo.csproj'

foreach ($path in @($controllerPath, $servicePath, $viewPath, $dtoPath, $scriptPath)) {
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
$siteCss = Get-Content -Raw -Encoding UTF8 -Path $siteCssPath
$script = Get-Content -Raw -Encoding UTF8 -Path $scriptPath
$project = Get-Content -Raw -Encoding UTF8 -Path $projectPath

foreach ($token in @(
    'public class SystemLogController : AdminBaseController',
    'ActionResult Index(SystemLogQueryDto query, int page = 1)',
    'private const int DefaultPageSize = 10;',
    'NormalizeDateFilters(query);',
    'GetVietnamLocalToday()',
    'DateTime.TryParseExact(',
    'rawValue.Trim()',
    '"dd-MM-yyyy"',
    '"dd/MM/yyyy"',
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

foreach ($token in @(
    'ToString("dd-MM-yyyy")',
    'ToString("yyyy-MM-dd")',
    'type="text"',
    'type="date"',
    'placeholder="dd-MM-yyyy"',
    'inputmode="numeric"',
    'pattern="\d{1,2}-\d{1,2}-\d{4}"',
    'value="@fromDateValue"',
    'value="@toDateValue"',
    'value="@fromDatePickerValue"',
    'value="@toDatePickerValue"',
    'data-date-input',
    'data-date-picker',
    'data-date-separator="-"',
    'data-date-invalid-message=',
    'data-date-picker-trigger',
    '<svg viewBox="0 0 24 24"'
)) {
    if ($view -notmatch [regex]::Escape($token)) {
        throw "SystemLog view must render dd-MM-yyyy date filters with native picker token: $token"
    }
}

foreach ($token in @(
    'placeholder="dd/mm/yyyy"',
    'pattern="\d{2}/\d{2}/\d{4}"',
    'readonly',
    'data-system-log-date-picker',
    'data-date-picker-display',
    '>Lịch<',
    'display.addEventListener("click"',
    '<script>'
)) {
    if ($view -match [regex]::Escape($token)) {
        throw "SystemLog date filters must not keep text-entry token: $token"
    }
}

foreach ($token in @(
    '.system-log-date-picker',
    '.system-log-date-display',
    '.system-log-date-native',
    '.system-log-date-trigger',
    'padding-right: 42px',
    'border-left: 1px solid var(--app-border)',
    'pointer-events: none'
)) {
    if ($siteCss -notmatch [regex]::Escape($token)) {
        throw "SystemLog date picker CSS must contain token: $token"
    }
}

foreach ($token in @(
    'data-date-separator',
    'getDaysInMonth',
    'setCustomValidity',
    'showPicker'
)) {
    if ($script -notmatch [regex]::Escape($token)) {
        throw "Shared date input script must contain token: $token"
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
