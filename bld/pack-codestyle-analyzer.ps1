# ---------------------------------------------------------------------------------------------------------------
# Packs the in-repo Bodu.CodeStyle.XmlDocumentation analyzer into .\local-packages so the main
# library projects (referenced via bld\Bodu.props) can restore it through the bodu-local NuGet
# source declared in the repo-root NuGet.config.
#
# Run once after cloning the repo, and again whenever you change anything under Bodu.CodeStyle\
# that should be picked up by Bodu.Core and the other library projects.
#
# Because the analyzer keeps a constant 1.0.0 version, NuGet's global cache must be evicted so
# that consumer projects re-extract the freshly-packed contents on their next restore.
# ---------------------------------------------------------------------------------------------------------------

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$repoRoot = Split-Path -Parent $scriptDir
$codestyleDir = Join-Path $repoRoot 'Bodu.CodeStyle'
$outputDir = Join-Path $repoRoot 'local-packages'
$packageId = 'bodu.codestyle.xmldocumentation'

Write-Host '>> Building + packing the analyzer...' -ForegroundColor Cyan
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

Push-Location $codestyleDir
try {
    dotnet pack 'Bodu.CodeStyle.XmlDocumentation/Bodu.CodeStyle.XmlDocumentation.csproj' `
        --configuration Release `
        --output $outputDir
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet pack failed with exit code $LASTEXITCODE"
    }
}
finally {
    Pop-Location
}

Write-Host ">> Evicting NuGet global cache for $packageId so consumers re-extract the new payload..." -ForegroundColor Cyan
$nugetGlobal = $env:NUGET_PACKAGES
if (-not $nugetGlobal) {
    $nugetGlobal = Join-Path $env:USERPROFILE '.nuget\packages'
}
$packageCache = Join-Path $nugetGlobal $packageId
if (Test-Path $packageCache) {
    Remove-Item -Recurse -Force $packageCache
}
dotnet nuget locals http-cache --clear | Out-Null

Write-Host '>> Done. Consumer projects must re-restore (e.g. dotnet restore --force) on next build.' -ForegroundColor Green
