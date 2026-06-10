$ErrorActionPreference = 'Stop'

$projectRoot = Resolve-Path (Join-Path $PSScriptRoot '..')

function Read-ProjectFile {
    param([string] $RelativePath)
    $path = Join-Path $projectRoot $RelativePath
    if (-not (Test-Path $path)) {
        return ''
    }

    return Get-Content -Path $path -Raw
}

function Assert-FileExists {
    param(
        [string] $RelativePath,
        [string] $Message
    )

    if (-not (Test-Path (Join-Path $projectRoot $RelativePath))) {
        throw $Message
    }
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

$projectFile = Read-ProjectFile 'HospitalQualityDashboard.csproj'
$accountController = Read-ProjectFile 'Controllers\AccountController.cs'
$adminRegistration = Read-ProjectFile 'Areas\Admin\AdminAreaRegistration.cs'
$userRegistration = Read-ProjectFile 'Areas\User\UserAreaRegistration.cs'
$adminBase = Read-ProjectFile 'Areas\Admin\Controllers\AdminBaseController.cs'
$userBase = Read-ProjectFile 'Areas\User\Controllers\UserBaseController.cs'
$adminLayout = Read-ProjectFile 'Areas\Admin\Views\Shared\_AdminLayout.cshtml'
$userLayout = Read-ProjectFile 'Areas\User\Views\Shared\_UserLayout.cshtml'
$departmentDtos = Read-ProjectFile 'Models\DTOs\DepartmentDtos.cs'
$employeeDtos = Read-ProjectFile 'Models\DTOs\EmployeeDtos.cs'
$indicatorDtos = Read-ProjectFile 'Models\DTOs\IndicatorDtos.cs'
$assignmentDtos = Read-ProjectFile 'Models\DTOs\AssignmentDtos.cs'
$periodDtos = Read-ProjectFile 'Models\DTOs\ReportingPeriodDtos.cs'
$reportDtos = Read-ProjectFile 'Models\DTOs\ReportDtos.cs'
$notificationDtos = Read-ProjectFile 'Models\DTOs\NotificationDtos.cs'
$exportDtos = Read-ProjectFile 'Models\DTOs\ExportDtos.cs'
$departmentService = Read-ProjectFile 'Services\ManagementServices.cs'
$indicatorService = Read-ProjectFile 'Services\IndicatorServices.cs'
$reportService = Read-ProjectFile 'Services\ReportDashboardServices.cs'
$notificationService = Read-ProjectFile 'Services\NotificationExportServices.cs'

Assert-FileExists 'Areas\Admin\AdminAreaRegistration.cs' 'Admin area registration must exist.'
Assert-FileExists 'Areas\User\UserAreaRegistration.cs' 'User area registration must exist.'
Assert-FileExists 'Areas\Admin\Controllers\AdminBaseController.cs' 'Admin base controller must exist.'
Assert-FileExists 'Areas\User\Controllers\UserBaseController.cs' 'User base controller must exist.'
Assert-FileExists 'Areas\Admin\Views\Shared\_AdminLayout.cshtml' 'Admin layout must exist.'
Assert-FileExists 'Areas\User\Views\Shared\_UserLayout.cshtml' 'User layout must exist.'

Assert-Contains $adminRegistration 'Admin/\{controller\}/\{action\}/\{id\}' 'Admin area must use the Admin route prefix.'
Assert-Contains $userRegistration 'User/\{controller\}/\{action\}/\{id\}' 'User area must use the User route prefix.'
Assert-Contains $adminBase 'RequireAdmin\(\)' 'Admin area must enforce Admin access through its base controller.'
Assert-Contains $userBase 'CurrentLoaiTaiKhoan != LoaiTaiKhoan\.User' 'User area must enforce User access through its base controller.'
Assert-Contains $userBase 'CurrentKhoaPhongId\.HasValue' 'User area must require a department-scoped user.'

Assert-Contains $accountController 'new \{ area = "Admin" \}' 'Admin login must redirect to the Admin area.'
Assert-Contains $accountController 'new \{ area = "User" \}' 'User login must redirect to the User area.'
Assert-Contains $adminLayout 'area = "Admin"' 'Admin layout links must target the Admin area.'
Assert-Contains $userLayout 'area = "User"' 'User layout links must target the User area.'

Assert-Contains $departmentDtos 'class DepartmentSaveDto' 'DepartmentSaveDto must exist.'
Assert-Contains $employeeDtos 'class EmployeeSaveDto' 'EmployeeSaveDto must exist.'
Assert-Contains $employeeDtos 'class CreateUserAccountDto' 'CreateUserAccountDto must exist.'
Assert-Contains $indicatorDtos 'class IndicatorSaveDto' 'IndicatorSaveDto must exist.'
Assert-Contains $assignmentDtos 'class AssignmentQueryDto' 'AssignmentQueryDto must exist.'
Assert-Contains $assignmentDtos 'class AssignmentCommandDto' 'AssignmentCommandDto must exist.'
Assert-Contains $periodDtos 'class ReportingPeriodSaveDto' 'ReportingPeriodSaveDto must exist.'
Assert-Contains $periodDtos 'class ReportingPeriodScheduleDto' 'ReportingPeriodScheduleDto must exist.'
Assert-Contains $reportDtos 'class ReportListQueryDto' 'ReportListQueryDto must exist.'
Assert-Contains $reportDtos 'class ReportDraftDto' 'ReportDraftDto must exist.'
Assert-Contains $notificationDtos 'class NotificationSendDto' 'NotificationSendDto must exist.'
Assert-Contains $exportDtos 'class ReportExportQueryDto' 'ReportExportQueryDto must exist.'

Assert-Contains $departmentService 'Save\(DepartmentSaveDto dto\)' 'DepartmentService must accept DepartmentSaveDto for saves.'
Assert-Contains $departmentService 'Save\(EmployeeSaveDto dto\)' 'EmployeeService must accept EmployeeSaveDto for saves.'
Assert-Contains $departmentService 'CreateUserAccount\(CreateUserAccountDto dto\)' 'EmployeeService must accept CreateUserAccountDto.'
Assert-Contains $indicatorService 'Save\(IndicatorSaveDto dto\)' 'IndicatorService must accept IndicatorSaveDto for saves.'
Assert-Contains $indicatorService 'Assign\(AssignmentCommandDto dto\)' 'AssignmentService must accept AssignmentCommandDto.'
Assert-Contains $indicatorService 'Save\(ReportingPeriodSaveDto dto\)' 'ReportingPeriodService must accept ReportingPeriodSaveDto.'
Assert-Contains $indicatorService 'GenerateSchedule\(ReportingPeriodScheduleDto dto, DateTime now\)' 'Schedule service must accept ReportingPeriodScheduleDto.'
Assert-Contains $reportService 'GetAll\(ReportListQueryDto dto\)' 'ReportService must accept ReportListQueryDto.'
Assert-Contains $reportService 'SaveDraft\(ReportDraftDto dto, int userId\)' 'ReportService must accept ReportDraftDto.'
Assert-Contains $notificationService 'SendManual\(NotificationSendDto dto, int userId\)' 'NotificationService must accept NotificationSendDto.'

Assert-Contains $projectFile 'Areas\\Admin\\AdminAreaRegistration\.cs' 'Project must include AdminAreaRegistration.'
Assert-Contains $projectFile 'Areas\\User\\UserAreaRegistration\.cs' 'Project must include UserAreaRegistration.'
Assert-Contains $projectFile 'Models\\DTOs\\DepartmentDtos\.cs' 'Project must include DTO files.'
Assert-Contains $projectFile 'tools\\VerifyAreasAndDtos\.ps1' 'Project must include the Areas/DTO verification script.'

Write-Host 'Areas and DTO verification passed.'
