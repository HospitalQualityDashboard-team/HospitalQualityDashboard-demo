$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$viewPath = Join-Path $root 'Views\Account\Profile.cshtml'
$controllerPath = Join-Path $root 'Controllers\AccountController.cs'
$bundlePath = Join-Path $root 'App_Start\BundleConfig.cs'
$scriptPath = Join-Path $root 'Scripts\date-input.js'
$cssPath = Join-Path $root 'Content\Site.css'
$projectPath = Join-Path $root 'HospitalQualityDashboard-demo.csproj'

foreach ($path in @($viewPath, $controllerPath, $bundlePath, $scriptPath, $cssPath, $projectPath)) {
    if (-not (Test-Path $path)) {
        throw "Required profile update file is missing: $path"
    }
}

$view = Get-Content -Raw -Encoding UTF8 -Path $viewPath
$controller = Get-Content -Raw -Encoding UTF8 -Path $controllerPath
$bundle = Get-Content -Raw -Encoding UTF8 -Path $bundlePath
$script = Get-Content -Raw -Encoding UTF8 -Path $scriptPath
$css = Get-Content -Raw -Encoding UTF8 -Path $cssPath
$project = Get-Content -Raw -Encoding UTF8 -Path $projectPath

foreach ($token in @(
    'ToString("dd/MM/yyyy")',
    'placeholder = "dd/MM/yyyy"',
    'inputmode = "numeric"',
    'pattern = "\\d{1,2}/\\d{1,2}/\\d{4}"',
    'maxlength = "10"',
    'data_date_input = ""',
    'data_date_picker_id = "ProfileNgaySinhPicker"',
    'data_date_invalid_message =',
    'data-date-picker-trigger="ProfileNgaySinhPicker"',
    'data-date-picker data-date-display-id="NgaySinh"',
    '<svg viewBox="0 0 24 24"',
    'ValidationMessage("NgaySinh"',
    'DropDownList("GioiTinh"',
    'new SelectListItem { Text = "Nam", Value = "Nam"',
    'Selected = string.Equals(Model.GioiTinh,'
)) {
    if ($view -notmatch [regex]::Escape($token)) {
        throw "Profile view must contain token: $token"
    }
}

foreach ($token in @(
    'type = "date"',
    'TextBox("GioiTinh"',
    '>Lịch<'
)) {
    if ($view -match [regex]::Escape($token)) {
        throw "Profile view must not keep old control token: $token"
    }
}

foreach ($token in @(
    'NormalizeProfileUpdateDate(model);',
    'Request.Form["NgaySinh"]',
    '"dd/MM/yyyy"',
    '"d/M/yyyy"',
    '"yyyy-MM-dd"',
    'DateTime.TryParseExact(',
    'CultureInfo.GetCultureInfo("vi-VN")',
    'ModelState.Remove("NgaySinh")',
    'ModelState.AddModelError("NgaySinh"'
)) {
    if ($controller -notmatch [regex]::Escape($token)) {
        throw "AccountController must parse profile birth date with token: $token"
    }
}

foreach ($token in @(
    '~/Scripts/date-input.js'
)) {
    if ($bundle -notmatch [regex]::Escape($token)) {
        throw "BundleConfig must include date input script token: $token"
    }
}

foreach ($token in @(
    'formatDigits',
    'data-date-input',
    'data-date-picker',
    'data-date-picker-trigger',
    'getDaysInMonth',
    'isLeapYear',
    'validateInput',
    'setCustomValidity',
    'getInvalidMessage',
    'showPicker'
)) {
    if ($script -notmatch [regex]::Escape($token)) {
        throw "date-input.js must contain token: $token"
    }
}

foreach ($token in @(
    '.app-date-picker',
    '.app-date-input',
    '.app-date-trigger',
    '.app-date-native',
    'padding-right: 42px'
)) {
    if ($css -notmatch [regex]::Escape($token)) {
        throw "Site.css must contain app date picker token: $token"
    }
}

if ($project -notmatch [regex]::Escape('tools\VerifyProfileUpdate.ps1')) {
    throw 'Project file must include tools\VerifyProfileUpdate.ps1.'
}

if ($project -notmatch [regex]::Escape('Scripts\date-input.js')) {
    throw 'Project file must include Scripts\date-input.js.'
}

Write-Host 'Profile update verification passed.'
