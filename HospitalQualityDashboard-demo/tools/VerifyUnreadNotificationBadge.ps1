$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$service = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Services\NotificationExportServices.cs')
$userBase = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Areas\User\Controllers\UserBaseController.cs')
$controller = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Areas\User\Controllers\NotificationController.cs')
$layout = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Views\Shared\_Layout.cshtml')
$view = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Areas\User\Views\Notification\Index.cshtml')
$css = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'Content\Site.css')

$checks = @(
    @{ Source = $service; Token = 'public int CountUnreadForUser(int accountId)' },
    @{ Source = $service; Token = 'COUNT(*) FROM dbo.ThongBaoNguoiNhan' },
    @{ Source = $service; Token = 'TaiKhoanId=@TaiKhoanId AND DaDoc=0' },
    @{ Source = $userBase; Token = 'ViewBag.UnreadNotificationCount' },
    @{ Source = $userBase; Token = 'CountUnreadForUser(CurrentTaiKhoanId.Value)' },
    @{ Source = $layout; Token = 'notification-nav-badge' },
    @{ Source = $layout; Token = 'unreadNotificationCount > 0' },
    @{ Source = $view; Token = 'if (!item.DaDoc)' },
    @{ Source = $view; Token = 'Html.BeginForm("MarkAsRead"' },
    @{ Source = $view; Token = 'page = Model.Page' },
    @{ Source = $controller; Token = 'public ActionResult MarkAsRead(int id, int page = 1)' },
    @{ Source = $controller; Token = 'page = NormalizePage(page)' },
    @{ Source = $css; Token = '.notification-nav-badge' }
)

foreach ($check in $checks) {
    if ($check.Source -notmatch [regex]::Escape($check.Token)) {
        throw "Unread notification badge contract is missing: $($check.Token)"
    }
}

Write-Host 'Unread notification badge verification passed.'
