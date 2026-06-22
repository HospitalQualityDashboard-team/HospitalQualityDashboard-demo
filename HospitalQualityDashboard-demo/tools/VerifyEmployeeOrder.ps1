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

    $servicePath = Join-Path $ProjectRoot "Services\Employees\EmployeeService.cs"
$viewPath = Join-Path $ProjectRoot "Areas\Admin\Views\Employee\Index.cshtml"

$service = Get-Content -Raw $servicePath
$view = Get-Content -Raw $viewPath

Assert-Contains $service "ORDER BY\s+nv\.NhanVienId" "EmployeeService must order employees by NhanVienId so the list follows sequence order."
Assert-Contains $view "<th>STT</th>" "Employee index must display an STT column."
Assert-Contains $view "Model\.PageSize" "Employee STT must use PageSize so numbering continues across pages."
Assert-Contains $view "@stt" "Employee index must render the current STT value."
Assert-Contains $view "stt\+\+" "Employee index must increment STT for each row."

Write-Host "Employee order and STT checks passed."
