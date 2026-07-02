$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ServiceSourceReader.ps1')

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$migrationPath = Join-Path $root 'App_Data\Sql\004_AddIndicatorWarning.sql'
$schema = Get-Content -Raw (Join-Path $root 'App_Data\Sql\001_CreateSchema.sql')
$enums = Get-Content -Raw (Join-Path $root 'Models\Enums\SystemEnums.cs')
$viewModels = Get-Content -Raw (Join-Path $root 'Models\ViewModels\AppViewModels.cs')
$notificationDtos = Get-Content -Raw (Join-Path $root 'Models\DTOs\NotificationDtos.cs')
$notificationService = Get-ServiceSource -Root $root -Patterns 'Services\Notifications\NotificationAutomationService.cs'
$messageBuilder = Get-ServiceSource -Root $root -Patterns 'Services\Notifications\IndicatorWarningMessageBuilder.cs'
$dashboardService = Get-ServiceSource -Root $root -Patterns 'Services\Dashboards\DashboardService*.cs'
$adminController = Get-Content -Raw (Join-Path $root 'Areas\Admin\Controllers\DashboardController.cs')
$userController = Get-Content -Raw (Join-Path $root 'Areas\User\Controllers\DashboardController.cs')
$userNotificationController = Get-Content -Raw (Join-Path $root 'Areas\User\Controllers\NotificationController.cs')
$view = Get-Content -Raw (Join-Path $root 'Areas\Admin\Views\Dashboard\Index.cshtml')
$userNotificationIndex = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Areas\User\Views\Notification\Index.cshtml')
$userNotificationDetails = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Areas\User\Views\Notification\Details.cshtml')
$siteCss = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Content\Site.css')

if (-not (Test-Path $migrationPath)) {
    throw 'Indicator warning migration 004_AddIndicatorWarning.sql is missing.'
}

$migration = Get-Content -Raw $migrationPath
$checks = @(
    @{ Source = $migration; Token = "COL_LENGTH('dbo.ThongBao', 'ChiSoChatLuongId')" },
    @{ Source = $migration; Token = "COL_LENGTH('dbo.ThongBaoTuDongLog', 'ChiSoChatLuongId')" },
    @{ Source = $migration; Token = 'CHECK (LoaiThongBao IN (1, 2, 3, 4, 5, 6, 7))' },
    @{ Source = $schema; Token = 'ChiSoChatLuongId INT NULL' },
    @{ Source = $schema; Token = 'CHECK (LoaiThongBao IN (1, 2, 3, 4, 5, 6, 7))' },
    @{ Source = $enums; Token = 'enum IndicatorWarningResult' },
    @{ Source = $enums; Token = 'HanNopHomNay = 7' },
    @{ Source = $notificationDtos; Token = 'public class IndicatorWarningTargetDto' },
    @{ Source = $notificationDtos; Token = 'public class IndicatorWarningBatchResultDto' },
    @{ Source = $viewModels; Token = 'public bool HasWarningToday' },
    @{ Source = $notificationService; Token = 'new[] { 10, 7, 3, 1, 0 }' },
    @{ Source = $notificationService; Token = 'SendIndicatorWarning' },
    @{ Source = $notificationService; Token = 'SendIndicatorWarnings' },
    @{ Source = $notificationService; Token = 'IndicatorWarningBatchResultDto' },
    @{ Source = $notificationService; Token = "COL_LENGTH('dbo.ThongBao', 'ChiSoChatLuongId')" },
    @{ Source = $notificationService; Token = 'CK_ThongBao_LoaiThongBao' },
    @{ Source = $notificationService; Token = 'CHECK (LoaiThongBao IN (1, 2, 3, 4, 5, 6, 7))' },
    @{ Source = $notificationService; Token = 'indicator-warning:{0}:{1}:{2}:{3:yyyyMMdd}' },
    @{ Source = $notificationService; Token = '@DaDuyet' },
    @{ Source = $notificationService; Token = 'IndicatorWarningMessageBuilder.Build(' },
    @{ Source = $notificationService; Token = 'warning.NotificationType' },
    @{ Source = $notificationService; Token = 'warning.Title' },
    @{ Source = $notificationService; Token = 'warning.Body' },
    @{ Source = $messageBuilder; Token = 'dueDate.Date - warningDate.Date' },
    @{ Source = $dashboardService; Token = '@ChiSoChatLuongId IS NULL OR cs.ChiSoChatLuongId = @ChiSoChatLuongId' },
    @{ Source = $dashboardService; Token = 'warningLog.DedupKey = CONCAT(' },
    @{ Source = $dashboardService; Token = "'indicator-warning:'" },
    @{ Source = $adminController; Token = 'public ActionResult WarnIndicator' },
    @{ Source = $adminController; Token = 'public ActionResult WarnIndicators' },
    @{ Source = $adminController; Token = 'ParseWarningTargets' },
    @{ Source = $adminController; Token = '[ValidateAntiForgeryToken]' },
    @{ Source = $adminController; Token = '_maintenance.Run' },
    @{ Source = $userController; Token = '_maintenance.Run' },
    @{ Source = $userNotificationController; Token = 'notification.ChiSoChatLuongId' },
    @{ Source = $view; Token = 'item.HasWarningToday' },
    @{ Source = $view; Token = 'item.CanSendWarning' },
    @{ Source = $view; Token = 'action="@Url.Action("WarnIndicator", "Dashboard")"' },
    @{ Source = $view; Token = 'action="@Url.Action("WarnIndicators", "Dashboard")"' },
    @{ Source = $view; Token = 'data-warning-bulk-form' },
    @{ Source = $view; Token = 'data-warning-select-all' },
    @{ Source = $view; Token = 'name="warningTargets"' },
    @{ Source = $userNotificationIndex; Token = 'notification-item-due-soon' },
    @{ Source = $userNotificationIndex; Token = 'notification-item-due-today' },
    @{ Source = $userNotificationIndex; Token = 'notification-item-overdue' },
    @{ Source = $userNotificationDetails; Token = 'notification-detail-' },
    @{ Source = $siteCss; Token = '--app-urgent:' },
    @{ Source = $siteCss; Token = '.notification-item.notification-item-due-soon' },
    @{ Source = $siteCss; Token = '.notification-item.notification-item-due-today' },
    @{ Source = $siteCss; Token = '.notification-item.notification-item-overdue' }
)

foreach ($check in $checks) {
    if ($check.Source -notmatch [regex]::Escape($check.Token)) {
        throw "Indicator warning contract is missing: $($check.Token)"
    }
}

Write-Host 'Indicator warning structural verification passed.'
