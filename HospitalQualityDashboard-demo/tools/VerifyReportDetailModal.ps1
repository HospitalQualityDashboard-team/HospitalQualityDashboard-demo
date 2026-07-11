$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$adminReportIndexPath = Join-Path $root 'Areas\Admin\Views\Report\Index.cshtml'
$userReportIndexPath = Join-Path $root 'Areas\User\Views\Report\Index.cshtml'
$partialPath = Join-Path $root 'Views\Shared\_ReportDetailModal.cshtml'
$siteCssPath = Join-Path $root 'Content\Site.css'
$projectPath = Join-Path $root 'HospitalQualityDashboard-demo.csproj'

$adminReportIndex = Get-Content -Raw -Path $adminReportIndexPath
$userReportIndex = Get-Content -Raw -Path $userReportIndexPath
$siteCss = Get-Content -Raw -Path $siteCssPath
$project = Get-Content -Raw -Path $projectPath

if (-not (Test-Path $partialPath)) {
    throw 'Shared report detail modal partial is missing.'
}

$partial = Get-Content -Raw -Path $partialPath

foreach ($token in @(
    'id="reportDetailModal"',
    'id="reportRejectModal"',
    'data-bs-dismiss="modal"',
    'btn-close',
    'data-report-field="indicator"',
    'data-report-field="period"',
    'data-report-field="department"',
    'data-report-field="formula"',
    'data-report-field="numerator"',
    'data-report-field="denominator"',
    'data-report-field="inputValue"',
    'data-report-field="result"',
    'data-report-field="target"',
    'data-report-field="submittedAt"',
    'data-report-field="submittedBy"',
    'data-report-field="status"',
    'data-report-field="note"',
    'data-report-field="feedback"'
)) {
    if ($partial -notmatch [regex]::Escape($token)) {
        throw "Report detail modal partial must contain $token."
    }
}

foreach ($token in @(
    'data-report-reject-form',
    'name="yKienPhanHoi"',
    'required',
    'data-report-reject-field="indicator"',
    'data-report-reject-field="period"',
    'data-report-reject-field="department"'
)) {
    if ($partial -notmatch [regex]::Escape($token)) {
        throw "Report reject modal partial must contain $token."
    }
}

if ($project -notmatch 'Views\\Shared\\_ReportDetailModal\.cshtml') {
    throw 'Project file must register the shared report detail modal partial.'
}

foreach ($view in @($adminReportIndex, $userReportIndex)) {
    if ($view -notmatch 'Html\.Partial\("_ReportDetailModal"\)') {
        throw 'Both report index views must render the shared report detail modal exactly once.'
    }

    foreach ($token in @(
        'data-bs-toggle="modal"',
        'data-bs-target="#reportDetailModal"',
        'data-report-id=',
        'data-report-period=',
        'data-report-department=',
        'data-report-indicator-code=',
        'data-report-indicator-name=',
        'data-report-formula=',
        'data-report-numerator=',
        'data-report-denominator=',
        'data-report-input-value=',
        'data-report-result=',
        'data-report-target=',
        'data-report-submitted-at=',
        'data-report-submitted-by=',
        'data-report-status=',
        'data-report-note=',
        'data-report-feedback='
    )) {
        if ($view -notmatch [regex]::Escape($token)) {
            throw "Report view must pass $token to the modal trigger."
        }
    }
}

if ($adminReportIndex -match 'ActionLink\(detailLabel, "Edit"') {
    throw 'Admin view button must not use Edit navigation for report detail viewing.'
}

if ($adminReportIndex -match 'showReject') {
    throw 'Admin reject flow must no longer navigate to Edit with showReject.'
}

if ($adminReportIndex -notmatch 'data-bs-target="#reportRejectModal"') {
    throw 'Admin reject button must open the reject modal.'
}

foreach ($token in @(
    'data-report-reject-action=',
    'data-report-reject-period=',
    'data-report-reject-department=',
    'data-report-reject-indicator='
)) {
    if ($adminReportIndex -notmatch [regex]::Escape($token)) {
        throw "Admin reject button must pass $token to the reject modal."
    }
}

if ($adminReportIndex -notmatch 'report-table-compact') {
    throw 'Admin report table must use the compact table class.'
}

foreach ($removedHeader in @('Khoa/phòng', 'Ngày giờ nộp', 'Người nộp')) {
    if ($adminReportIndex -match [regex]::Escape("<th>$removedHeader</th>")) {
        throw "Admin report table must not render removed header $removedHeader."
    }
}

if ($adminReportIndex -notmatch '<th></th>') {
    throw 'Admin report table must keep an untitled actions column.'
}

if ($userReportIndex -notmatch 'ActionLink\("Sửa", "Edit"') {
    throw 'User draft/returned reports must still navigate to Edit with a Sua button.'
}

if ($userReportIndex -match 'ActionLink\(detailLabel, "Edit"') {
    throw 'User readonly Xem button must not share Edit navigation with Sua.'
}

foreach ($token in @(
    'data-filter-combobox',
    'report-filter-combobox',
    'report-filter-clear',
    'ActionLink("Xóa lọc", "Index", "Report"'
)) {
    if ($adminReportIndex -notmatch [regex]::Escape($token)) {
        throw "Admin report filters must contain $token."
    }
}

foreach ($token in @(
    'cdn.jsdelivr.net/npm/select2',
    '.select2(',
    'select2-container'
)) {
    if ($adminReportIndex -match [regex]::Escape($token)) {
        throw "Admin report filters must not use Select2 token $token because it renders a second search box."
    }
}

foreach ($token in @(
    'KyBaoCaoId',
    'KhoaPhongId',
    'ChiSoChatLuongId'
)) {
    if ($adminReportIndex -notmatch "$token[^\r\n]+data_filter_combobox") {
        throw "Admin filter $token must be enhanced by the inline combobox."
    }
}

foreach ($token in @(
    'report-detail-modal',
    'report-reject-modal',
    'body.report-detail-modal-open .app-shell',
    'body.report-reject-modal-open .app-shell',
    'backdrop-filter: blur',
    'report-table-compact'
)) {
    if ($siteCss -notmatch [regex]::Escape($token)) {
        throw "Site.css must contain report modal/compact table styling token $token."
    }
}

Write-Host 'Report detail modal verification passed.'
