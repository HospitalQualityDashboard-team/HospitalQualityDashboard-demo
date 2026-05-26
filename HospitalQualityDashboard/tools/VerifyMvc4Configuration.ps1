param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

$ErrorActionPreference = "Stop"

function Assert-Equal {
    param(
        [string]$Name,
        [object]$Actual,
        [object]$Expected
    )

    if ($Actual -ne $Expected) {
        throw "$Name expected '$Expected' but was '$Actual'."
    }

    Write-Host "[PASS] $Name = $Actual"
}

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

function Assert-NotContains {
    param(
        [string]$Name,
        [string]$Text,
        [string]$Forbidden
    )

    if ($Text -like "*$Forbidden*") {
        throw "$Name still contains forbidden MVC5 text '$Forbidden'."
    }

    Write-Host "[PASS] $Name does not contain $Forbidden"
}

$packagesPath = Join-Path $ProjectRoot "packages.config"
$projectPath = Join-Path $ProjectRoot "HospitalQualityDashboard.csproj"
$webConfigPath = Join-Path $ProjectRoot "Web.config"
$viewsConfigPath = Join-Path $ProjectRoot "Views\Web.config"

[xml]$packages = Get-Content -Raw -Encoding UTF8 $packagesPath
[xml]$project = Get-Content -Raw -Encoding UTF8 $projectPath
[xml]$webConfig = Get-Content -Raw -Encoding UTF8 $webConfigPath
[xml]$viewsConfig = Get-Content -Raw -Encoding UTF8 $viewsConfigPath

$packageById = @{}
$packages.packages.package | ForEach-Object { $packageById[$_.id] = $_ }

Assert-Equal "TargetFrameworkVersion" $project.Project.PropertyGroup[0].TargetFrameworkVersion "v4.7.2"
Assert-Equal "Microsoft.AspNet.Mvc package" $packageById["Microsoft.AspNet.Mvc"].version "4.0.40804"
Assert-Equal "Microsoft.AspNet.Razor package" $packageById["Microsoft.AspNet.Razor"].version "2.0.30506"
Assert-Equal "Microsoft.AspNet.WebPages package" $packageById["Microsoft.AspNet.WebPages"].version "2.0.30506"
Assert-Equal "webpages:Version" ($webConfig.configuration.appSettings.add | Where-Object { $_.key -eq "webpages:Version" }).value "2.0.0.0"

$projectText = Get-Content -Raw -Encoding UTF8 $projectPath
$webConfigText = Get-Content -Raw -Encoding UTF8 $webConfigPath
$viewsConfigText = Get-Content -Raw -Encoding UTF8 $viewsConfigPath

Assert-Contains "Project MVC reference" $projectText "System.Web.Mvc, Version=4.0.0.1"
Assert-Contains "Project Razor reference" $projectText "System.Web.Razor, Version=2.0.0.0"
Assert-Contains "Project WebPages reference" $projectText "System.Web.WebPages, Version=2.0.0.0"
Assert-Contains "Views MVC host" $viewsConfigText "System.Web.Mvc, Version=4.0.0.1"
Assert-Contains "Views Razor config" $viewsConfigText "System.Web.WebPages.Razor, Version=2.0.0.0"
Assert-Contains "Web.config MVC redirect" $webConfigText "newVersion=`"4.0.0.1`""

Assert-NotContains "Project file" $projectText "Microsoft.AspNet.Mvc.5.2.9"
Assert-NotContains "Project file" $projectText "Microsoft.AspNet.Razor.3.2.9"
Assert-NotContains "Project file" $projectText "Microsoft.AspNet.WebPages.3.2.9"
Assert-NotContains "Web.config" $webConfigText "newVersion=`"5.2.9.0`""
Assert-NotContains "Views Web.config" $viewsConfigText "Version=5.2.9.0"
Assert-NotContains "Views Web.config" $viewsConfigText "Version=3.0.0.0"

Write-Host "[PASS] MVC4 configuration verified."
