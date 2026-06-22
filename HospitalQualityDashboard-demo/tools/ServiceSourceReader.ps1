function Get-ServiceSource {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Root,

        [Parameter(Mandatory = $true)]
        [string[]]$Patterns
    )

    $files = foreach ($pattern in $Patterns) {
        Get-ChildItem -Path (Join-Path $Root $pattern) -File | Sort-Object FullName
    }

    if (-not $files) {
        throw "No service source files matched: $($Patterns -join ', ')"
    }

    return ($files | ForEach-Object {
        [System.IO.File]::ReadAllText($_.FullName, [System.Text.Encoding]::UTF8)
    }) -join [Environment]::NewLine
}
