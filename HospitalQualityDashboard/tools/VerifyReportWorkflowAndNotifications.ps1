$ErrorActionPreference = 'Stop'

$projectRoot = Resolve-Path (Join-Path $PSScriptRoot '..')

function Read-ProjectFile {
    param([string] $RelativePath)
    return Get-Content -Path (Join-Path $projectRoot $RelativePath) -Raw
}

function Assert-Contains {
    param(
        [string] $Content,
        [string] $Pattern,
        [string] $Message
    )

    if ($Content -notmatch $Pattern) {
        throw $Message
    }
}

function Assert-NotContains {
    param(
        [string] $Content,
        [string] $Pattern,
        [string] $Message
    )

    if ($Content -match $Pattern) {
        throw $Message
    }
}

$enums = Read-ProjectFile 'Models\Enums\SystemEnums.cs'
$reports = Read-ProjectFile 'Services\ReportDashboardServices.cs'
$notifications = Read-ProjectFile 'Services\NotificationExportServices.cs'
$reportController = Read-ProjectFile 'Controllers\ReportController.cs'
$notificationController = Read-ProjectFile 'Controllers\NotificationController.cs'
$reportIndex = Read-ProjectFile 'Views\Report\Index.cshtml'
$reportEdit = Read-ProjectFile 'Views\Report\Edit.cshtml'
$notificationIndex = Read-ProjectFile 'Views\Notification\Index.cshtml'
$notificationDetails = Read-ProjectFile 'Views\Notification\Details.cshtml'
$dashboardView = Read-ProjectFile 'Views\Dashboard\Index.cshtml'
$viewModels = Read-ProjectFile 'Models\ViewModels\AppViewModels.cs'
$schema = Read-ProjectFile 'App_Data\Sql\001_CreateSchema.sql'
$projectFile = Read-ProjectFile 'HospitalQualityDashboard.csproj'

Assert-Contains $reports 'bc\.TrangThai\s+IN\s*\(@DaGuiStatus,\s*@QuaHanStatus,\s*@DaKhoaStatus\)' 'Admin report list must only include submitted, late-submitted, and locked reports.'
Assert-Contains $reports 'CASE\s+WHEN\s+GETDATE\(\)\s*>\s*ky\.HanNop\s+THEN\s+@QuaHan' 'Submit must mark late submissions as QuaHan.'
Assert-Contains $reports 'WHERE\s+BaoCaoId=@Id\s+AND\s+TrangThai=@Nhap' 'SaveDraft must only allow editing draft reports.'
Assert-NotContains $reports 'status\s*!=\s*\(byte\)TrangThaiBaoCao\.TraLai' 'SaveDraft must no longer allow editing returned reports.'
Assert-Contains $reports 'TrangThai\s+IN\s*\(@DaGui,\s*@QuaHan\)' 'Lock must allow both DaGui and QuaHan reports.'
Assert-Contains $reports 'TrangThai IN \(2,3,4\)' 'Dashboard submitted count must include DaGui, QuaHan, and DaKhoa.'
Assert-Contains $reports 'bc\.TrangThai=@QuaHanStatus' 'Dashboard late-submitted count must count actual QuaHan reports.'

Assert-Contains $reportController 'if\s*\(IsAdmin\)\s*\{\s*return new HttpUnauthorizedResult\(\);\s*\}' 'Admin must be blocked from POSTing report edits.'
Assert-Contains $reportController 'if\s*\(IsAdmin\)\s*\{\s*return new HttpUnauthorizedResult\(\);\s*\}' 'Admin must be blocked from submitting reports.'
Assert-Contains $reportController 'return new HttpStatusCodeResult\(410' 'Approve and Reject routes must be disabled.'

Assert-Contains $reportIndex 'Model\.IsAdmin\s*\?\s*"Xem"' 'Admin action label must be Xem.'
Assert-NotContains $reportIndex '>Duy' 'Report list must not show approval action.'
Assert-NotContains $reportIndex '>Trả lại<' 'Report list must not show reject action.'
Assert-NotContains $reportIndex 'promptRejectReason' 'Report list must remove reject prompt script.'

Assert-Contains $reportEdit 'bool isAdmin = ViewBag\.IsAdmin == true;' 'Report edit view must know whether current viewer is Admin.'
Assert-Contains $reportEdit 'bool isEditable = !isAdmin &&' 'Report edit view must be readonly for Admin.'
Assert-Contains $reportEdit 'Báo cáo này chỉ được xem, không thể chỉnh sửa số liệu\.' 'Readonly message must match the current workflow.'

Assert-Contains $enums 'KyBaoCaoMo\s*=\s*2' 'Notification types must include KyBaoCaoMo.'
Assert-Contains $enums 'NhacHan\s*=\s*3' 'Notification types must include NhacHan.'
Assert-Contains $enums 'QuaHan\s*=\s*4' 'Notification types must include QuaHan.'
Assert-Contains $enums 'TongHopAdmin\s*=\s*5' 'Notification types must include TongHopAdmin.'
Assert-Contains $schema 'ThongBaoTuDongLog' 'Schema must define ThongBaoTuDongLog for deduplication.'
Assert-Contains $notifications 'class NotificationAutomationService' 'Notification automation service must exist.'
Assert-Contains $notifications 'public void Run\(DateTime now\)' 'Notification automation service must expose Run(DateTime now).'
Assert-Contains $notifications 'SendPeriodOpenedNotifications' 'Automation must send period-opened notifications.'
Assert-Contains $notifications 'SendDueReminderNotifications' 'Automation must send due reminder notifications.'
Assert-Contains $notifications 'SendOverdueNotifications' 'Automation must send overdue notifications.'
Assert-Contains $notifications 'SendAdminDailySummary' 'Automation must send admin daily summary notifications.'
Assert-Contains $notifications 'DedupKey' 'Automation must use a deduplication key.'
Assert-Contains $notificationController 'RunAutomation' 'Notification controller must expose an Admin trigger for automation.'
Assert-Contains $notificationController 'ActionResult Details\(int id\)' 'Notification controller must expose a detail page for each notification.'
Assert-Contains $notificationController 'GetDetailForUser\(id, CurrentTaiKhoanId\.Value, IsAdmin\)' 'Notification details must enforce recipient/admin visibility.'
Assert-Contains $notificationController 'MarkAsRead\(id, CurrentTaiKhoanId\.Value\)' 'Opening a notification detail must mark it as read for the current user.'
Assert-Contains $notificationController 'GetMissingReportsForDepartment\(CurrentKhoaPhongId\.Value, notification\.KyBaoCaoId\.Value, overdueOnly\)' 'Notification details must load missing indicators for the notification period.'
Assert-Contains $notifications 'KyBaoCaoId' 'Notification queries must keep KyBaoCaoId so details can show the related period.'
Assert-Contains $notifications 'BaoCaoId' 'Notification queries must keep BaoCaoId for report-specific notifications.'
Assert-Contains $notifications 'GetDetailForUser' 'Notification service must expose a permission-aware detail lookup.'

Assert-Contains $viewModels 'class MissingReportAlertViewModel' 'Dashboard must expose a view model for missing report details.'
Assert-Contains $viewModels 'class NotificationDetailViewModel' 'Notification detail view model must exist.'
Assert-Contains $viewModels 'IList<MissingReportAlertViewModel> MissingReports' 'Notification detail must carry missing report rows.'
Assert-Contains $viewModels 'int\? KyBaoCaoId' 'Notification view model must carry the related report period.'
Assert-Contains $viewModels 'int\? BaoCaoId' 'Notification view model must carry the related report when present.'
Assert-Contains $viewModels 'IList<MissingReportAlertViewModel> MissingReports' 'Dashboard must carry missing report details for User alerts.'
Assert-Contains $viewModels 'DueSoonReportCount' 'Dashboard must expose near-deadline missing report count.'
Assert-Contains $viewModels 'OverdueMissingReportCount' 'Dashboard must expose overdue-unsubmitted report count.'
Assert-Contains $reports 'GetMissingReportsForDepartment' 'Dashboard service must query missing reports for the current department.'
Assert-Contains $reports 'GetMissingReportsForDepartment\(int departmentId, int\? periodId, bool overdueOnly\)' 'Dashboard service must support period and overdue-only filters for notification details.'
Assert-Contains $reports '@KyBaoCaoId IS NULL OR ky\.KyBaoCaoId = @KyBaoCaoId' 'Missing report query must be filterable by report period.'
Assert-Contains $reports '@OverdueOnly = 0 OR DATEDIFF\(day, CAST\(GETDATE\(\) AS date\), ky\.HanNop\) < 0' 'Missing report query must be filterable to overdue-unsubmitted indicators.'
Assert-Contains $reports 'bc\.TrangThai IN \(@DaGui, @QuaHan, @DaKhoa\)' 'Missing report query must treat only submitted, late-submitted, and locked reports as reported.'
Assert-Contains $reports 'bc\.BaoCaoId IS NULL' 'Missing report query must keep drafts counted as not submitted.'
Assert-Contains $dashboardView 'missing-report-alert' 'Dashboard must show an immediate missing-report alert to User.'
Assert-Contains $dashboardView 'data-bs-target="#missingReportDetails"' 'Dashboard alert must reveal details on click.'
Assert-Contains $dashboardView 'Model\.MissingReports' 'Dashboard must render missing report detail rows.'
Assert-Contains $dashboardView 'ActionLink\(.+, "Edit", "Report", new \{ kyBaoCaoId = item\.KyBaoCaoId, chiSoChatLuongId = item\.ChiSoChatLuongId \}' 'Dashboard missing-report rows must provide a quick report entry action.'
Assert-Contains $notificationIndex 'ActionLink\("Xem chi.+", "Details"' 'Notification list must link each notification to its detail page.'
Assert-Contains $notificationIndex 'Url\.Action\("Details", new \{ id = item\.ThongBaoId \}\)' 'Notification title/body must be clickable.'
Assert-Contains $notificationDetails 'Model\.MissingReports' 'Notification details must render missing indicator rows.'
Assert-Contains $notificationDetails 'Qu.+h.+n ch.+a n.+p' 'Overdue notification details must label overdue-unsubmitted indicators.'
Assert-Contains $notificationDetails 'Nh.+p b.+o c.+o' 'Notification details must let users jump to report entry.'
Assert-Contains $projectFile 'Views\\Notification\\Details\.cshtml' 'Project file must include the notification detail view.'

Write-Host 'Report workflow and notification automation verification passed.'
