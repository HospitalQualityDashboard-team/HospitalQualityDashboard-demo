param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

$ErrorActionPreference = "Stop"

function Assert-Contains {
    param(
        [string]$Name,
        [string]$Text,
        [string]$Expected
    )

    if ($Text -notlike "*$Expected*") {
        throw "$Name does not contain expected text '$Expected'."
    }

    Write-Host "[PASS] $Name contains $Expected"
}

$viewModelPath = Join-Path $ProjectRoot "Models\ViewModels\AppViewModels.cs"
$servicePath = Join-Path $ProjectRoot "Services\ManagementServices.cs"
$viewPath = Join-Path $ProjectRoot "Views\Employee\Index.cshtml"
$controllerPath = Join-Path $ProjectRoot "Controllers\EmployeeController.cs"

$viewModelText = Get-Content -Raw -Encoding UTF8 $viewModelPath
$serviceText = Get-Content -Raw -Encoding UTF8 $servicePath
$viewText = Get-Content -Raw -Encoding UTF8 $viewPath
$controllerText = Get-Content -Raw -Encoding UTF8 $controllerPath

Assert-Contains "NhanVienViewModel" $viewModelText "public bool HasAccount { get; set; }"
Assert-Contains "EmployeeService GetAll/Get" $serviceText "AS HasAccount"
Assert-Contains "EmployeeService MapEmployee" $serviceText 'HasAccount = reader.GetBoolean(reader.GetOrdinal("HasAccount"))'
Assert-Contains "Employee index view" $viewText "if (!item.HasAccount)"
Assert-Contains "Employee CreateAccount GET guard" $controllerText "if (employee.HasAccount)"

Write-Host "[PASS] Employee account button behavior verified."
