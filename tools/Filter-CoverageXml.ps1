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

# Visual Studio coverage XML stores assemblies under module nodes. The element name casing differs by
# producer -- 'dotnet-coverage merge -f xml' writes lowercase <module>, while some Visual Studio
# exports write <Module> -- and XPath local-name() is case-sensitive, so match case-insensitively.
# Matching only one spelling yields an empty node set, which silently writes an unfiltered copy of
# the input and reports success.
$modules = @($xml.SelectNodes(
    "//*[translate(local-name(),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='module']"))

if ($modules.Count -eq 0) {
    throw "No <module> elements found in '$InputPath'. Expected a Visual Studio code coverage XML export, for example from 'dotnet-coverage merge <files> -o coverage.xml -f xml'."
}

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

$remove = @($remove)

if ($remove.Count -eq $modules.Count) {
    throw "No module in '$InputPath' matched '$AssemblyName'. Found: $(($modules | ForEach-Object { $_.GetAttribute('name') }) -join ', ')"
}

foreach ($node in $remove) {
    [void]$node.ParentNode.RemoveChild($node)
}

$xml.Save($OutputPath)

$retained = $modules.Count - $remove.Count
Write-Host "Retained $retained of $($modules.Count) module(s) matching '$AssemblyName':"
foreach ($module in $modules) {
    if ($remove -notcontains $module) {
        Write-Host "  $($module.GetAttribute('name'))"
    }
}

Write-Host "Wrote filtered coverage file:"
Write-Host $OutputPath