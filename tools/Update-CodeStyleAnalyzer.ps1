# ---------------------------------------------------------------------------------------------------------------
# Update-CodeStyleAnalyzer.ps1
#
# Single-command refresh of the in-repo Bodu.CodeStyle.XmlDocumentation analyzer. Wraps the three steps you would
# otherwise run by hand after changing anything under Bodu.CodeStyle/:
#
#   1. Pack the analyzer into ./local-packages and evict NuGet's global cache for it
#      (delegated to bld/pack-codestyle-analyzer.ps1 — the same pack the bash + cmd entry points call).
#   2. Force-restore a consumer (solution or project) so it re-extracts the freshly-packed 1.0.0 payload.
#   3. Build the consumer so Roslyn loads the new analyzer and reports against your source.
#
# The analyzer is always packed in Release (that is the configuration committed to local-packages/ and used by
# CI); -Configuration governs only the consumer restore/build. The pack runs under the SDK 8 pin in
# Bodu.CodeStyle/global.json, while the consumer build runs under the repo-root SDK 10 pin — each dotnet
# invocation resolves its own SDK from its working directory, so the two never collide.
#
# Examples:
#   pwsh ./tools/Update-CodeStyleAnalyzer.ps1
#   pwsh ./tools/Update-CodeStyleAnalyzer.ps1 -Target Bodu.Core/src/Bodu.Core.csproj
#   pwsh ./tools/Update-CodeStyleAnalyzer.ps1 -SkipBuild
# ---------------------------------------------------------------------------------------------------------------

[CmdletBinding()]
param(
    # Solution or project to force-restore (and build) against the freshly-packed analyzer. Resolved relative to
    # the repository root; defaults to the full solution so every consumer picks up the new payload.
    [string] $Target = 'bodu.slnx',

    # Configuration used for the consumer restore/build. Does not affect the analyzer pack, which is always Release.
    [string] $Configuration = 'Release',

    # Pack and force-restore only; skip the consumer build. Useful for a fast refresh while iterating.
    [switch] $SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$repoRoot = Split-Path -Parent $scriptDir
$packScript = Join-Path (Join-Path $repoRoot 'bld') 'pack-codestyle-analyzer.ps1'

if (-not (Test-Path $packScript)) {
    throw "Pack script not found at '$packScript'. Run this from a full repository checkout."
}

# 1. Pack the analyzer into local-packages/ and evict the NuGet global cache. The pack script throws on a pack
#    failure, so a terminating error here propagates straight out; wrap it only to add call-site context.
Write-Host '==> [1/3] Packing Bodu.CodeStyle.XmlDocumentation into local-packages/ ...' -ForegroundColor Cyan
try {
    & $packScript
}
catch {
    throw "Packing the analyzer failed: $($_.Exception.Message)"
}

# Run the consumer steps from the repository root so the root global.json (SDK 10) governs them, independent of
# the SDK 8 pin used while packing.
Push-Location $repoRoot
try {
    # 2. Force-restore: the analyzer version is pinned at 1.0.0, and step 1 deleted its global-cache entry, so a
    #    plain restore could reuse stale metadata. --force re-extracts the new payload.
    Write-Host "==> [2/3] Restoring '$Target' (--force) ..." -ForegroundColor Cyan
    dotnet restore $Target --force
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore '$Target' failed with exit code $LASTEXITCODE."
    }

    # 3. Build so Roslyn loads the new analyzer. --no-restore avoids a redundant restore after step 2.
    if ($SkipBuild) {
        Write-Host '==> [3/3] Skipping build (-SkipBuild).' -ForegroundColor Yellow
    }
    else {
        Write-Host "==> [3/3] Building '$Target' ($Configuration) ..." -ForegroundColor Cyan
        dotnet build $Target --configuration $Configuration --no-restore
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet build '$Target' failed with exit code $LASTEXITCODE."
        }
    }
}
finally {
    Pop-Location
}

Write-Host ''
Write-Host 'Done. Bodu.CodeStyle.XmlDocumentation was repacked into local-packages/ and the target restored' -ForegroundColor Green
Write-Host 'against it. Commit local-packages/Bodu.CodeStyle.XmlDocumentation.1.0.0.nupkg alongside your' -ForegroundColor Green
Write-Host 'Bodu.CodeStyle/ source changes so a fresh clone restores the same analyzer.' -ForegroundColor Green
