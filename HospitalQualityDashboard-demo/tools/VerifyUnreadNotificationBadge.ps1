$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ServiceSourceReader.ps1')

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$service = Get-ServiceSource -Root $root -Patterns 'Services\Notifications\NotificationService.cs'
$userBase = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Areas\User\Controllers\UserBaseController.cs')
$controller = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Areas\User\Controllers\NotificationController.cs')
$layout = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Views\Shared\_Layout.cshtml')
$view = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Areas\User\Views\Notification\Index.cshtml')
$css = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Content\Site.css')

$checks = @(
    @{ Source = $service; Token = 'public int CountUnreadForUser(int accountId)' },
    @{ Source = $service; Token = 'COUNT(*) FROM dbo.ThongBaoNguoiNhan' },
    @{ Source = $service; Token = 'TaiKhoanId=@TaiKhoanId AND DaDoc=0' },
    @{ Source = $service; Token = 'GetPreviewForAccount(int accountId, int maxItems = 8)' },
    @{ Source = $service; Token = 'MarkAllAsReadForAccount(int accountId)' },
    @{ Source = $userBase; Token = 'ViewBag.UnreadNotificationCount' },
    @{ Source = $userBase; Token = 'ViewBag.NotificationPreview' },
    @{ Source = $userBase; Token = 'notificationPreview.UnreadCount' },
    @{ Source = $layout; Token = 'notification-icon-button' },
    @{ Source = $layout; Token = 'notification-dropdown-badge' },
    @{ Source = $layout; Token = 'unreadNotificationCount > 0' },
    @{ Source = $layout; Token = 'unreadNotificationDisplay' },
    @{ Source = $layout; Token = 'data-notification-unread-count' },
    @{ Source = $layout; Token = 'data-notification-read-form' },
    @{ Source = $layout; Token = 'markAllAsRead()' },
    @{ Source = $layout; Token = 'clearUnreadState()' },
    @{ Source = $view; Token = 'if (!item.DaDoc)' },
    @{ Source = $view; Token = 'Html.BeginForm("MarkAsRead"' },
    @{ Source = $view; Token = 'page = Model.Page' },
    @{ Source = $controller; Token = 'public ActionResult MarkAsRead(int id, int page = 1)' },
    @{ Source = $controller; Token = 'public ActionResult MarkAllAsRead()' },
    @{ Source = $controller; Token = 'page = NormalizePage(page)' },
    @{ Source = $css; Token = '.notification-dropdown-badge' }
)

foreach ($check in $checks) {
    if ($check.Source -notmatch [regex]::Escape($check.Token)) {
        throw "Unread notification badge contract is missing: $($check.Token)"
    }
}

Write-Host 'Unread notification badge verification passed.'
