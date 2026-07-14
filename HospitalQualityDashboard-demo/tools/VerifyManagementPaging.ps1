$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ServiceSourceReader.ps1')

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$viewModelPath = Join-Path $root 'Models\ViewModels\AppViewModels.cs'
$departmentControllerPath = Join-Path $root 'Areas\Admin\Controllers\DepartmentController.cs'
$departmentViewPath = Join-Path $root 'Areas\Admin\Views\Department\Index.cshtml'
$employeeControllerPath = Join-Path $root 'Areas\Admin\Controllers\EmployeeController.cs'
$assignmentControllerPath = Join-Path $root 'Areas\Admin\Controllers\AssignmentController.cs'
$periodControllerPath = Join-Path $root 'Areas\Admin\Controllers\ReportingPeriodController.cs'
$periodViewPath = Join-Path $root 'Areas\Admin\Views\ReportingPeriod\Index.cshtml'
$adminIndicatorControllerPath = Join-Path $root 'Areas\Admin\Controllers\IndicatorController.cs'
$userIndicatorControllerPath = Join-Path $root 'Areas\User\Controllers\IndicatorController.cs'
$indicatorViewPath = Join-Path $root 'Areas\Admin\Views\Indicator\Index.cshtml'
$adminReportControllerPath = Join-Path $root 'Areas\Admin\Controllers\ReportController.cs'
$userReportControllerPath = Join-Path $root 'Areas\User\Controllers\ReportController.cs'
$adminNotificationControllerPath = Join-Path $root 'Areas\Admin\Controllers\NotificationController.cs'
$userNotificationControllerPath = Join-Path $root 'Areas\User\Controllers\NotificationController.cs'
$adminNotificationViewPath = Join-Path $root 'Areas\Admin\Views\Notification\Index.cshtml'
$userNotificationViewPath = Join-Path $root 'Areas\User\Views\Notification\Index.cshtml'

$viewModel = Get-Content -Raw -Path $viewModelPath
$departmentController = Get-Content -Raw -Path $departmentControllerPath
$departmentService = Get-ServiceSource -Root $root -Patterns 'Services\Departments\DepartmentService.cs'
$departmentView = Get-Content -Raw -Path $departmentViewPath
$employeeController = Get-Content -Raw -Path $employeeControllerPath
$assignmentController = Get-Content -Raw -Path $assignmentControllerPath
$periodController = Get-Content -Raw -Path $periodControllerPath
$periodService = Get-ServiceSource -Root $root -Patterns 'Services\ReportingPeriods\ReportingPeriodService.cs'
$periodView = Get-Content -Raw -Path $periodViewPath
$adminIndicatorController = Get-Content -Raw -Path $adminIndicatorControllerPath
$userIndicatorController = Get-Content -Raw -Path $userIndicatorControllerPath
$indicatorService = Get-ServiceSource -Root $root -Patterns 'Services\Indicators\IndicatorService*.cs'
$indicatorView = Get-Content -Raw -Path $indicatorViewPath
$adminReportController = Get-Content -Raw -Path $adminReportControllerPath
$userReportController = Get-Content -Raw -Path $userReportControllerPath
$adminNotificationController = Get-Content -Raw -Path $adminNotificationControllerPath
$userNotificationController = Get-Content -Raw -Path $userNotificationControllerPath
$adminNotificationView = Get-Content -Raw -Path $adminNotificationViewPath
$userNotificationView = Get-Content -Raw -Path $userNotificationViewPath

foreach ($token in @('KyBaoCaoIndexViewModel', 'PageSize', 'TotalItems', 'TotalPages')) {
    if ($viewModel -notmatch $token) {
        throw "Paging view models must expose $token."
    }
}

foreach ($entry in @(
    @{ Name = 'Department'; Source = $departmentController; Pattern = 'private const int DefaultPageSize = 10;' },
    @{ Name = 'Employee'; Source = $employeeController; Pattern = 'private const int DefaultPageSize = 10;' },
    @{ Name = 'Admin Indicator'; Source = $adminIndicatorController; Pattern = 'private const int DefaultPageSize = 10;' },
    @{ Name = 'User Indicator'; Source = $userIndicatorController; Pattern = 'private const int DefaultPageSize = 10;' },
    @{ Name = 'Assignment'; Source = $assignmentController; Pattern = 'private const int PageSize = 10;' },
    @{ Name = 'ReportingPeriod'; Source = $periodController; Pattern = 'private const int DefaultPageSize = 10;' },
    @{ Name = 'Admin Report'; Source = $adminReportController; Pattern = 'private const int DefaultPageSize = 10;' },
    @{ Name = 'User Report'; Source = $userReportController; Pattern = 'private const int DefaultPageSize = 10;' },
    @{ Name = 'Admin Notification'; Source = $adminNotificationController; Pattern = 'private const int DefaultPageSize = 10;' },
    @{ Name = 'User Notification'; Source = $userNotificationController; Pattern = 'private const int DefaultPageSize = 10;' }
)) {
    if ($entry.Source -notmatch [regex]::Escape($entry.Pattern)) {
        throw "$($entry.Name) list page size must be 10."
    }
}

if ($viewModel -notmatch 'KhoaPhongIndexViewModel[\s\S]*PageSize[\s\S]*TotalItems[\s\S]*TotalPages') {
    throw 'Department view model must expose paging metadata.'
}

if ($departmentController -notmatch 'ActionResult Index\(string search, string statusFilter = "all", int page = 1\)') {
    throw 'Department Index must accept a page parameter.'
}

if ($departmentController -notmatch 'GetAll\(search, statusFilter, page, DefaultPageSize, out totalItems\)') {
    throw 'Department Index must request one page from the service.'
}

if ($departmentService -notmatch 'GetAll\(string search, int page, int pageSize, out int totalItems, bool includeInactive = true\)') {
    throw 'DepartmentService must provide a paged GetAll overload.'
}

foreach ($token in @('Model.TotalPages > 1', 'Trang @Model.Page / @Model.TotalPages', 'page = Model.Page + 1', 'search = Model.Search')) {
    if ($departmentView -notmatch [regex]::Escape($token)) {
        throw "Department view must render paging token: $token"
    }
}

if ($periodController -notmatch 'ActionResult Index\(int page = 1\)') {
    throw 'ReportingPeriod Index must accept a page parameter.'
}

if ($periodController -notmatch 'GetAll\(page, DefaultPageSize, out totalItems\)') {
    throw 'ReportingPeriod Index must request one page from the service.'
}

if ($periodService -notmatch 'GetAll\(int page, int pageSize, out int totalItems\)') {
    throw 'ReportingPeriodService must provide a paged GetAll overload.'
}

foreach ($token in @('Model.TotalPages > 1', 'Trang @Model.Page / @Model.TotalPages', 'page = Model.Page + 1')) {
    if ($periodView -notmatch [regex]::Escape($token)) {
        throw "ReportingPeriod view must render paging token: $token"
    }
}

foreach ($controller in @($adminIndicatorController, $userIndicatorController)) {
    if ($controller -notmatch 'ActionResult Index\(int page = 1\)') {
        throw 'Indicator Index actions must accept a page parameter.'
    }

    if ($controller -notmatch 'GetAll\(.*page, DefaultPageSize, out totalItems\)') {
        throw 'Indicator Index actions must request one page from the service.'
    }
}

if ($indicatorService -notmatch 'GetAll\(bool includeInactive, int\? filterKhoaPhongId, int page, int pageSize, out int totalItems\)') {
    throw 'IndicatorService must provide a paged GetAll overload.'
}

foreach ($token in @('Model.TotalPages > 1', 'Trang @Model.Page / @Model.TotalPages', 'page = Model.Page + 1')) {
    if ($indicatorView -notmatch [regex]::Escape($token)) {
        throw "Indicator view must render paging token: $token"
    }
}

foreach ($controller in @($adminNotificationController, $userNotificationController)) {
    if ($controller -notmatch 'ActionResult Index\(int page = 1\)') {
        throw 'Notification Index actions must accept a page parameter.'
    }

    if ($controller -notmatch 'TotalPages = GetTotalPages\(totalItems, DefaultPageSize\)') {
        throw 'Notification Index actions must populate TotalPages.'
    }
}

foreach ($view in @($adminNotificationView, $userNotificationView)) {
    foreach ($token in @('Model.TotalPages > 1', 'Trang @Model.Page / @Model.TotalPages', 'page = Model.Page + 1')) {
        if ($view -notmatch [regex]::Escape($token)) {
            throw "Notification views must render paging token: $token"
        }
    }
}

Write-Host 'Management paging verification passed.'
