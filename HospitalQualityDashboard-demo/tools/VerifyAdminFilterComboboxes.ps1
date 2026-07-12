param(
    [string]$ProjectRoot = (Resolve-Path "$PSScriptRoot\..").Path
)

$ErrorActionPreference = "Stop"

function Assert-Contains {
    param(
        [string]$Content,
        [string]$Pattern,
        [string]$Message
    )

    if ($Content -notmatch $Pattern) {
        throw $Message
    }
}

function Assert-NotContains {
    param(
        [string]$Content,
        [string]$Pattern,
        [string]$Message
    )

    if ($Content -match $Pattern) {
        throw $Message
    }
}

$bundlePath = Join-Path $ProjectRoot "App_Start\BundleConfig.cs"
$projectPath = Join-Path $ProjectRoot "HospitalQualityDashboard-demo.csproj"
$scriptPath = Join-Path $ProjectRoot "Scripts\filter-combobox.js"
$reportPath = Join-Path $ProjectRoot "Areas\Admin\Views\Report\Index.cshtml"
$assignmentPath = Join-Path $ProjectRoot "Areas\Admin\Views\Assignment\Index.cshtml"
$departmentPath = Join-Path $ProjectRoot "Areas\Admin\Views\Department\Index.cshtml"
$employeePath = Join-Path $ProjectRoot "Areas\Admin\Views\Employee\Index.cshtml"
$departmentControllerPath = Join-Path $ProjectRoot "Areas\Admin\Controllers\DepartmentController.cs"
$viewModelPath = Join-Path $ProjectRoot "Models\ViewModels\AppViewModels.cs"

$bundle = Get-Content -Raw $bundlePath
$project = Get-Content -Raw $projectPath
$script = if (Test-Path $scriptPath) { Get-Content -Raw $scriptPath } else { "" }
$report = Get-Content -Raw $reportPath
$assignment = Get-Content -Raw $assignmentPath
$department = Get-Content -Raw $departmentPath
$employee = Get-Content -Raw $employeePath
$departmentController = Get-Content -Raw $departmentControllerPath
$viewModels = Get-Content -Raw $viewModelPath

Assert-Contains $project "Scripts\\filter-combobox\.js" "Project file must include the shared filter combobox script."
Assert-Contains $bundle "~/Scripts/filter-combobox\.js" "The shared filter combobox script must be included in the common JavaScript bundle."
Assert-Contains $script "select\[data-filter-combobox\]" "Shared combobox script must enhance select filters."
Assert-Contains $script "\[data-filter-text-combobox\]" "Shared combobox script must support text filters with suggestions."
Assert-Contains $script "report-filter-combobox" "Shared combobox script must render the same shell as the report filters."
Assert-Contains $script "report-filter-options" "Shared combobox script must render a vertical dropdown list."
Assert-Contains $script "report-filter-combobox-clear" "Shared combobox script must render the clear button."

Assert-Contains $report "data_filter_combobox\s*=\s*""true""" "Report filters must still opt in to the shared combobox."

Assert-Contains $assignment "data_filter_combobox\s*=\s*""true""" "Assignment select filters must use searchable comboboxes."
Assert-Contains $assignment "data_filter_placeholder\s*=\s*""[^""]*khoa/phòng""" "Assignment department filter must have the expected placeholder."
Assert-Contains $assignment "data_filter_placeholder\s*=\s*""[^""]*chỉ số""" "Assignment indicator filter must have the expected placeholder."
Assert-Contains $assignment "name\s*=\s*""trangThai""[^>]*class\s*=\s*""[^""]*form-select" "Assignment status filter must remain a native select."
Assert-NotContains $assignment "name\s*=\s*""trangThai""[^>]*data-filter-combobox" "Assignment status filter must remain a native select."
Assert-NotContains $assignment "name\s*=\s*""search""" "Assignment filter form must not include the general search box."
Assert-NotContains $assignment "name\s*=\s*""trangThaiPhanCong""[^>]*data-filter-combobox" "Assignment indicator assignment-status filter must remain a native select."

Assert-Contains $employee "data_filter_combobox\s*=\s*""true""" "Employee department filter must use a searchable combobox."
Assert-Contains $employee "KhoaPhongId[\s\S]*data_filter_placeholder\s*=" "Employee department filter must have the expected placeholder."

Assert-Contains $department "data_filter_text_combobox\s*=\s*""true""" "Department search must use the text suggestion combobox."
Assert-Contains $department "data_filter_options_source\s*=\s*""DepartmentSearchOptions""" "Department search must expose suggestion options."
Assert-Contains $departmentController "KhoaPhongOptions\s*=\s*_service\.GetOptions\(\)" "Department controller must provide department options for search suggestions."
Assert-Contains $viewModels "class KhoaPhongIndexViewModel[\s\S]*IList<SelectListItem>\s+KhoaPhongOptions" "Department index view model must expose department options."

Write-Host "Admin filter combobox checks passed."
