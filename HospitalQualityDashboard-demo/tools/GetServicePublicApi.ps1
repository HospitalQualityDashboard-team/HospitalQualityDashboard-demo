param(
    [Parameter(Mandatory = $true)]
    [string]$AssemblyPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'

$assemblyPath = (Resolve-Path $AssemblyPath).Path
$outputPath = [System.IO.Path]::GetFullPath($OutputPath)
$assemblyDirectory = Split-Path $assemblyPath -Parent

$resolver = [ResolveEventHandler] {
    param($sender, $eventArgs)

    $name = (New-Object System.Reflection.AssemblyName($eventArgs.Name)).Name + '.dll'
    $candidate = Join-Path $assemblyDirectory $name
    if (Test-Path $candidate) {
        return [System.Reflection.Assembly]::LoadFrom($candidate)
    }

    return $null
}

[AppDomain]::CurrentDomain.add_AssemblyResolve($resolver)

try {
    $assembly = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
    $bindingFlags = [System.Reflection.BindingFlags]'Public,Instance,Static,DeclaredOnly'
    $lines = New-Object 'System.Collections.Generic.List[string]'

    $types = $assembly.GetExportedTypes() |
        Where-Object { $_.Namespace -eq 'HospitalQualityDashboardDemo.Services' } |
        Sort-Object FullName

    foreach ($type in $types) {
        $lines.Add("TYPE $($type.FullName)")

        foreach ($constructor in ($type.GetConstructors($bindingFlags) | Sort-Object { $_.ToString() })) {
            $parameters = ($constructor.GetParameters() | ForEach-Object { $_.ParameterType.FullName }) -join ','
            $lines.Add("CTOR $($type.FullName)($parameters)")
        }

        foreach ($property in ($type.GetProperties($bindingFlags) | Sort-Object Name)) {
            $lines.Add("PROPERTY $($type.FullName).$($property.Name):$($property.PropertyType.FullName)")
        }

        foreach ($method in ($type.GetMethods($bindingFlags) |
            Where-Object { -not $_.IsSpecialName } |
            Sort-Object Name, { $_.ToString() })) {
            $parameters = ($method.GetParameters() | ForEach-Object {
                $modifier = if ($_.IsOut) { 'out ' } elseif ($_.ParameterType.IsByRef) { 'ref ' } else { '' }
                $modifier + $_.ParameterType.FullName
            }) -join ','
            $lines.Add("METHOD $($type.FullName).$($method.Name)($parameters):$($method.ReturnType.FullName)")
        }
    }

    $outputDirectory = Split-Path $outputPath -Parent
    if (-not (Test-Path $outputDirectory)) {
        New-Item -ItemType Directory -Path $outputDirectory | Out-Null
    }

    [System.IO.File]::WriteAllLines($outputPath, $lines, (New-Object System.Text.UTF8Encoding($false)))
    Write-Host "Exported $($lines.Count) public service API signatures to $outputPath"
}
finally {
    [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolver)
}
