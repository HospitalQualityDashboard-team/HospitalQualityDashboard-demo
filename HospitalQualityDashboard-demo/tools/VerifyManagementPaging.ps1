$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$viewModelPath = Join-Path $root 'Models\ViewModels\AppViewModels.cs'
$periodControllerPath = Join-Path $root 'Areas\Admin\Controllers\ReportingPeriodController.cs'
$periodServicePath = Join-Path $root 'Services\ReportingPeriodServices.cs'
$periodViewPath = Join-Path $root 'Areas\Admin\Views\ReportingPeriod\Index.cshtml'
$adminIndicatorControllerPath = Join-Path $root 'Areas\Admin\Controllers\IndicatorController.cs'
$userIndicatorControllerPath = Join-Path $root 'Areas\User\Controllers\IndicatorController.cs'
$indicatorServicePath = Join-Path $root 'Services\IndicatorServices.cs'
$indicatorViewPath = Join-Path $root 'Areas\Admin\Views\Indicator\Index.cshtml'
$adminNotificationControllerPath = Join-Path $root 'Areas\Admin\Controllers\NotificationController.cs'
$userNotificationControllerPath = Join-Path $root 'Areas\User\Controllers\NotificationController.cs'
$adminNotificationViewPath = Join-Path $root 'Areas\Admin\Views\Notification\Index.cshtml'
$userNotificationViewPath = Join-Path $root 'Areas\User\Views\Notification\Index.cshtml'

$viewModel = Get-Content -Raw -Path $viewModelPath
$periodController = Get-Content -Raw -Path $periodControllerPath
$periodService = Get-Content -Raw -Path $periodServicePath
$periodView = Get-Content -Raw -Path $periodViewPath
$adminIndicatorController = Get-Content -Raw -Path $adminIndicatorControllerPath
$userIndicatorController = Get-Content -Raw -Path $userIndicatorControllerPath
$indicatorService = Get-Content -Raw -Path $indicatorServicePath
$indicatorView = Get-Content -Raw -Path $indicatorViewPath
$adminNotificationController = Get-Content -Raw -Path $adminNotificationControllerPath
$userNotificationController = Get-Content -Raw -Path $userNotificationControllerPath
$adminNotificationView = Get-Content -Raw -Path $adminNotificationViewPath
$userNotificationView = Get-Content -Raw -Path $userNotificationViewPath

foreach ($token in @('KyBaoCaoIndexViewModel', 'PageSize', 'TotalItems', 'TotalPages')) {
    if ($viewModel -notmatch $token) {
        throw "Paging view models must expose $token."
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
