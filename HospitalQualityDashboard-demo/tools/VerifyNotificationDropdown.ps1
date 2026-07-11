$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ServiceSourceReader.ps1')

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$layout = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Views\Shared\_Layout.cshtml')
$css = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Content\Site.css')
$service = Get-ServiceSource -Root $root -Patterns 'Services\Notifications\NotificationService.cs'
$adminBase = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Areas\Admin\Controllers\AdminBaseController.cs')
$userBase = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Areas\User\Controllers\UserBaseController.cs')
$adminController = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Areas\Admin\Controllers\NotificationController.cs')
$userController = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Areas\User\Controllers\NotificationController.cs')
$viewModels = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Models\ViewModels\AppViewModels.cs')

$importantHeading = 'Quan tr' + [char]0x1ECD + 'ng'
$otherHeading = 'C' + [char]0x00E1 + 'c th' + [char]0x00F4 + 'ng b' + [char]0x00E1 + 'o kh' + [char]0x00E1 + 'c'
$viewAllLabel = 'Xem t' + [char]0x1EA5 + 't c' + [char]0x1EA3
$sendNotificationLabel = 'G' + [char]0x1EED + 'i th' + [char]0x00F4 + 'ng b' + [char]0x00E1 + 'o'

$checks = @(
    @{ Source = $viewModels; Token = 'public class NotificationPreviewViewModel' },
    @{ Source = $viewModels; Token = 'public IList<NotificationViewModel> ImportantItems' },
    @{ Source = $viewModels; Token = 'public IList<NotificationViewModel> OtherItems' },
    @{ Source = $service; Token = 'GetPreviewForAccount(int accountId, int maxItems = 8)' },
    @{ Source = $service; Token = 'MarkAllAsReadForAccount(int accountId)' },
    @{ Source = $service; Token = 'FROM dbo.ThongBaoNguoiNhan tbn' },
    @{ Source = $service; Token = 'WHERE tbn.TaiKhoanId = @TaiKhoanId' },
    @{ Source = $service; Token = 'WHERE TaiKhoanId=@TaiKhoanId AND DaDoc=0' },
    @{ Source = $service; Token = 'IsImportantPreviewNotification' },
    @{ Source = $adminBase; Token = 'ViewBag.NotificationPreview' },
    @{ Source = $adminBase; Token = 'ViewBag.UnreadNotificationCount' },
    @{ Source = $userBase; Token = 'ViewBag.NotificationPreview' },
    @{ Source = $userBase; Token = 'ViewBag.UnreadNotificationCount' },
    @{ Source = $layout; Token = 'notification-icon-button' },
    @{ Source = $layout; Token = 'notification-bell-icon' },
    @{ Source = $layout; Token = 'notification-dropdown' },
    @{ Source = $layout; Token = 'notification-dropdown-badge' },
    @{ Source = $layout; Token = '99+' },
    @{ Source = $layout; Token = $importantHeading },
    @{ Source = $layout; Token = $otherHeading },
    @{ Source = $layout; Token = $viewAllLabel },
    @{ Source = $layout; Token = $sendNotificationLabel },
    @{ Source = $layout; Token = 'data-notification-toggle' },
    @{ Source = $layout; Token = 'data-notification-panel' },
    @{ Source = $layout; Token = 'MarkAllAsRead' },
    @{ Source = $layout; Token = 'data-notification-read-form' },
    @{ Source = $layout; Token = 'data-notification-unread-count' },
    @{ Source = $layout; Token = 'data-notification-unread-summary' },
    @{ Source = $layout; Token = 'markAllAsRead()' },
    @{ Source = $layout; Token = 'XMLHttpRequest' },
    @{ Source = $layout; Token = 'clearUnreadState()' },
    @{ Source = $layout; Token = "event.key === 'Escape'" },
    @{ Source = $css; Token = '.notification-topbar' },
    @{ Source = $css; Token = '.notification-dropdown' },
    @{ Source = $css; Token = '.notification-dropdown-badge' },
    @{ Source = $css; Token = '@media (max-width: 576px)' },
    @{ Source = $adminController; Token = 'public ActionResult MarkDetailAsRead(int id)' },
    @{ Source = $adminController; Token = 'public ActionResult MarkAllAsRead()' },
    @{ Source = $adminController; Token = 'return Json(new { success = true, unreadCount = 0 })' },
    @{ Source = $userController; Token = 'public ActionResult MarkAllAsRead()' },
    @{ Source = $userController; Token = 'return Json(new { success = true, unreadCount = 0 })' }
)

foreach ($check in $checks) {
    if ($check.Source -notmatch [regex]::Escape($check.Token)) {
        throw "Notification dropdown contract is missing: $($check.Token)"
    }
}

Write-Host 'Notification dropdown verification passed.'
