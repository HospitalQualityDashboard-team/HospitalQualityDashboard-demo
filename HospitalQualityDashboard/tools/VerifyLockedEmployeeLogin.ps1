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

$servicePath = Join-Path $ProjectRoot "Services\ManagementServices.cs"
$authServicePath = Join-Path $ProjectRoot "Services\AuthService.cs"
$accountControllerPath = Join-Path $ProjectRoot "Controllers\AccountController.cs"

$serviceText = Get-Content -Raw -Encoding UTF8 $servicePath
$authServiceText = Get-Content -Raw -Encoding UTF8 $authServicePath
$accountControllerText = Get-Content -Raw -Encoding UTF8 $accountControllerPath
$lockedMessage = -join ([char[]]@(0x0054,0x00E0,0x0069,0x0020,0x006B,0x0068,0x006F,0x1EA3,0x006E,0x0020,0x0063,0x1EE7,0x0061,0x0020,0x0062,0x1EA1,0x006E,0x0020,0x0111,0x00E3,0x0020,0x0062,0x1ECB,0x0020,0x006B,0x0068,0x00F3,0x0061))

Assert-Contains "EmployeeService SetActive" $serviceText "UPDATE tk SET DangHoatDong = @Active"
Assert-Contains "EmployeeService SetActive" $serviceText "tk.NhanVienId = nv.NhanVienId OR tk.TenDangNhap = nv.MaNhanVien"
Assert-Contains "AuthenticatedUser" $authServiceText "public bool IsLocked { get; set; }"
Assert-Contains "AuthService account status query" $authServiceText "tk.DangHoatDong AS TaiKhoanDangHoatDong"
Assert-Contains "AuthService employee status query" $authServiceText "nv.DangHoatDong AS NhanVienDangHoatDong"
Assert-Contains "AccountController locked login branch" $accountControllerText "user.IsLocked"
Assert-Contains "AccountController locked login message" $accountControllerText $lockedMessage

Write-Host "[PASS] Locked employee login behavior verified."
