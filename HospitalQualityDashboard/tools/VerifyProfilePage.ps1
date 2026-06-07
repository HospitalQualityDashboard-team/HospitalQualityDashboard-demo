param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Stop"

function Assert-FileContains {
    param(
        [string]$Path,
        [string]$Pattern,
        [string]$Message
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Missing file: $Path"
    }

    $content = Get-Content -Raw -Encoding UTF8 -LiteralPath $Path
    if ($content -notmatch $Pattern) {
        throw $Message
    }
}

$accountController = Join-Path $ProjectRoot "Controllers\AccountController.cs"
$authService = Join-Path $ProjectRoot "Services\AuthService.cs"
$authViewModels = Join-Path $ProjectRoot "Models\ViewModels\AuthViewModels.cs"
$profileView = Join-Path $ProjectRoot "Views\Account\Profile.cshtml"
$layoutView = Join-Path $ProjectRoot "Views\Shared\_Layout.cshtml"
$csproj = Join-Path $ProjectRoot "HospitalQualityDashboard.csproj"

Assert-FileContains $authViewModels "class\s+UserProfileViewModel" "UserProfileViewModel is required."
Assert-FileContains $authViewModels "ChangePasswordViewModel\s+ChangePassword" "Profile view model must embed ChangePasswordViewModel."
Assert-FileContains $authService "GetUserProfile\s*\(" "AuthService.GetUserProfile is required."
Assert-FileContains $authService "LEFT\s+JOIN\s+dbo\.NhanVien" "Profile query must include imported employee data."
Assert-FileContains $authService "LEFT\s+JOIN\s+dbo\.KhoaPhong" "Profile query must include department data."
Assert-FileContains $accountController "ActionResult\s+Profile\s*\(" "AccountController must expose GET Profile."
Assert-FileContains $accountController "ActionResult\s+Profile\s*\(\s*UserProfileViewModel\s+model\s*\)" "AccountController must expose POST Profile for password change."
Assert-FileContains $profileView "UserProfileViewModel" "Profile view must be strongly typed."
Assert-FileContains $profileView "MaNhanVien|Mã nhân viên" "Profile view must display imported employee code."
Assert-FileContains $profileView "HoTen|Họ tên" "Profile view must display imported full name."
Assert-FileContains $profileView "BeginForm\(""Profile"",\s*""Account""" "Password form must post to Account/Profile."
Assert-FileContains $layoutView "Profile"",\s*""Account""" "Navigation must link authenticated users to Profile."
Assert-FileContains $csproj "Views\\Account\\Profile\.cshtml" "Project file must include Views\\Account\\Profile.cshtml."

Write-Host "Profile page verification passed."
