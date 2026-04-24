param(
    [Parameter(Mandatory)]
    [string]$InputPath,

    [Parameter(Mandatory)]
    [string]$AssemblyName,

    [string]$OutputPath = $null
)

$ErrorActionPreference = 'Stop'

if (-not $OutputPath) {
    $baseName = [IO.Path]::GetFileNameWithoutExtension($InputPath)
    $dir = [IO.Path]::GetDirectoryName($InputPath)
    $OutputPath = Join-Path $dir "$baseName.$AssemblyName.coveragexml"
}

[xml]$xml = Get-Content -LiteralPath $InputPath -Raw

# Visual Studio coverage XML usually stores assemblies/modules under Module nodes.
$modules = @($xml.SelectNodes("//*[local-name()='Module']"))

$remove = foreach ($module in $modules) {
    $moduleName =
        $module.GetAttribute('name')

    if (-not $moduleName) {
        $moduleName = $module.ModuleName
    }

    if ($moduleName -notlike "*$AssemblyName*") {
        $module
    }
}

foreach ($node in $remove) {
    [void]$node.ParentNode.RemoveChild($node)
}

$xml.Save($OutputPath)

Write-Host "Wrote filtered coverage file:"
Write-Host $OutputPath