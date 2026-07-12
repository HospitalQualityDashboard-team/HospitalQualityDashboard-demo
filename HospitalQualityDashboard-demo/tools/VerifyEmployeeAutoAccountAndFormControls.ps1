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

$controllerPath = Join-Path $ProjectRoot "Areas\Admin\Controllers\EmployeeController.cs"
$servicePath = Join-Path $ProjectRoot "Services\Employees\EmployeeService.cs"
$viewPath = Join-Path $ProjectRoot "Areas\Admin\Views\Employee\Edit.cshtml"
$cssPath = Join-Path $ProjectRoot "Content\Site.css"
$scriptPath = Join-Path $ProjectRoot "Scripts\date-input.js"

$controller = Get-Content -Raw $controllerPath
$service = Get-Content -Raw $servicePath
$view = Get-Content -Raw $viewPath
$css = Get-Content -Raw $cssPath
$script = Get-Content -Raw $scriptPath

Assert-Contains $controller "IsUsernameExists\(model\.MaNhanVien\)" "Employee create must reject a duplicate account username before saving."
Assert-Contains $controller 'ModelState\.AddModelError\("MaNhanVien"' "Duplicate employee account username must be shown on the employee code field."
Assert-Contains $controller "NormalizeEmployeeBirthDate\(model\)" "Employee save must normalize dd/MM/yyyy birth dates before validation."
Assert-Contains $controller 'Request\.Form\["NgaySinh"\]' "Employee birth date normalization must read the raw posted value."
Assert-Contains $controller '"dd/MM/yyyy"' "Employee birth date normalization must accept dd/MM/yyyy."
Assert-Contains $controller 'ModelState\.Remove\("NgaySinh"\)' "Employee birth date normalization must clear model binder date errors after parsing."
Assert-Contains $controller 'ModelState\.AddModelError\("NgaySinh"' "Employee birth date normalization must show a clear validation error for invalid calendar dates."

Assert-Contains $service "ExecuteInTransaction\(\(conn,\s*trans\)\s*=>" "New employee save must run employee and account inserts in one transaction."
Assert-Contains $service "OUTPUT INSERTED\.NhanVienId" "New employee save must capture the inserted employee id."
Assert-Contains $service "INSERT INTO dbo\.TaiKhoan\(TenDangNhap,\s*MatKhauHash,\s*LoaiTaiKhoan,\s*NhanVienId,\s*KhoaPhongId,\s*DangHoatDong\)" "New employee save must create the linked user account."
Assert-Contains $service "PasswordHasher\.Hash\(model\.MaNhanVien\)" "Default account password must be the employee code, stored as a hash."

Assert-Contains $view "DropDownListFor\(\s*m => m\.GioiTinh" "Gender must be a select control."
Assert-Contains $view 'new SelectListItem\s*\{\s*Text = "Nam",\s*Value = "Nam"' "Gender select must include Nam."
Assert-Contains $view 'TextBox\(\s*"NgaySinh"' "Employee form must render a birth date input."
Assert-Contains $view 'ToString\("dd/MM/yyyy"\)' "Employee birth date must render as dd/MM/yyyy."
Assert-Contains $view 'data_date_input\s*=\s*""' "Employee birth date input must use the shared date formatter."
Assert-Contains $view "data_date_invalid_message" "Employee birth date input must provide a validation message."
Assert-Contains $view 'data-date-picker-trigger="EmployeeNgaySinhPicker"' "Employee birth date must have a calendar icon trigger."
Assert-Contains $view 'data-date-picker\s+data-date-display-id="NgaySinh"' "Employee birth date must keep a native date picker for calendar selection."
Assert-Contains $view '<svg viewBox="0 0 24 24"' "Employee birth date trigger must render a calendar icon."
Assert-Contains $view "data-department-picker" "Department field must use the searchable department picker."
Assert-NotContains $view "<datalist" "Department picker must not use native datalist because it shows too many departments."
Assert-NotContains $view 'list="KhoaPhongOptionsList"' "Department search input must not use native datalist."
Assert-Contains $view "data-department-menu" "Department picker must render a controlled menu."
Assert-Contains $view "report-filter-combobox" "Department picker must use the shared report filter combobox shell."
Assert-Contains $view "report-filter-options" "Department picker list must use the shared vertical dropdown style."
Assert-Contains $view "report-filter-option" "Department picker items must use the shared vertical option style."
Assert-Contains $view 'classList\.add\("is-open"\)' "Department picker must open through the shared combobox state class."
Assert-Contains $view "maxVisibleDepartments\s*=\s*5" "Department picker must limit visible departments to five."
Assert-Contains $view "\.slice\(0,\s*maxVisibleDepartments\)" "Department picker must render only the first five matching departments."
Assert-Contains $view "updateDepartmentSelection" "Department picker must map the selected text back to KhoaPhongId."
Assert-Contains $css "\.report-filter-options" "Department picker must reuse the shared report filter dropdown styling."
Assert-Contains $css "\.app-date-picker" "Birth date inputs must use shared date picker styling."
Assert-Contains $css "\.app-date-trigger" "Birth date calendar icon must use shared trigger styling."
Assert-Contains $script "formatDigits" "Shared date input script must format typed birth dates."
Assert-Contains $script "getDaysInMonth" "Shared date input script must validate real calendar days."
Assert-Contains $script "isLeapYear" "Shared date input script must validate leap years."
Assert-Contains $script "setCustomValidity" "Shared date input script must block invalid calendar dates in the browser."
Assert-Contains $script "data-date-picker-trigger" "Shared date input script must wire calendar icon triggers."

Write-Host "Employee auto-account and form control checks passed."
