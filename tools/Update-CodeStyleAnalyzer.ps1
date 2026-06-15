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
# When the target is a solution, benchmark projects (and anything else matching -ExcludeProjectPattern) are
# dropped via a temporary solution filter (.slnf): they do not exercise the XML-documentation analyzer and only
# slow the refresh. Pass -ExcludeProjectPattern @() to restore/build the whole solution unfiltered.
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
#   pwsh ./tools/Update-CodeStyleAnalyzer.ps1 -ExcludeProjectPattern @()        # build everything, benchmarks included
# ---------------------------------------------------------------------------------------------------------------

[CmdletBinding()]
param(
    # Solution or project to force-restore (and build) against the freshly-packed analyzer. Resolved relative to
    # the repository root; defaults to the full solution so every consumer picks up the new payload.
    [string] $Target = 'bodu.slnx',

    # Configuration used for the consumer restore/build. Does not affect the analyzer pack, which is always Release.
    [string] $Configuration = 'Release',

    # Regex patterns tested against each project path when the target is a solution; matches are excluded from the
    # restore/build via a temporary solution filter. Defaults to benchmark projects (any '/bench/' path segment).
    # Ignored when the target is a single project. Pass @() to disable filtering.
    [string[]] $ExcludeProjectPattern = @('[\\/]bench[\\/]'),

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
    $resolvedTarget = (Resolve-Path -LiteralPath $Target -ErrorAction Stop).Path
    $isSolution = $resolvedTarget -match '\.slnx?$'
    $effectiveTarget = $resolvedTarget
    $tempFilter = $null

    # When targeting a solution, drop excluded projects (benchmarks by default) by generating a temporary solution
    # filter alongside the solution. A .slnf restores/builds only the listed projects, so the analyzer refresh
    # skips harnesses that never run it. Single-project targets need no filtering.
    if ($isSolution -and $ExcludeProjectPattern -and @($ExcludeProjectPattern).Count -gt 0) {
        $listOutput = dotnet sln $resolvedTarget list
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet sln '$Target' list failed with exit code $LASTEXITCODE."
        }

        $allProjects = @($listOutput | ForEach-Object { $_.Trim() } | Where-Object { $_ -match '\.(cs|vb|fs)proj$' })
        $kept = [System.Collections.Generic.List[string]]::new()
        $excluded = [System.Collections.Generic.List[string]]::new()

        foreach ($project in $allProjects) {
            $isExcluded = $false
            foreach ($pattern in $ExcludeProjectPattern) {
                if ($project -match $pattern) {
                    $isExcluded = $true
                    break
                }
            }

            if ($isExcluded) { $excluded.Add($project) } else { $kept.Add($project) }
        }

        if ($kept.Count -eq 0) {
            throw "Every project in '$Target' matched -ExcludeProjectPattern; nothing left to restore or build."
        }

        if ($excluded.Count -gt 0) {
            $solutionDir = Split-Path -Parent $resolvedTarget
            $solutionLeaf = Split-Path -Leaf $resolvedTarget
            $tempFilter = Join-Path $solutionDir '_codestyle-refresh.slnf'

            $filter = [ordered]@{
                solution = [ordered]@{
                    path     = $solutionLeaf
                    projects = @($kept)
                }
            }
            $filter | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $tempFilter -Encoding utf8
            $effectiveTarget = $tempFilter

            Write-Host ("    Excluding {0} benchmark/excluded project(s); restoring + building {1} of {2} projects via a solution filter." -f $excluded.Count, $kept.Count, $allProjects.Count) -ForegroundColor DarkGray
        }
    }

    try {
        # 2. Force-restore: the analyzer version is pinned at 1.0.0, and step 1 deleted its global-cache entry, so a
        #    plain restore could reuse stale metadata. --force re-extracts the new payload.
        Write-Host "==> [2/3] Restoring '$Target' (--force) ..." -ForegroundColor Cyan
        dotnet restore $effectiveTarget --force
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet restore '$Target' failed with exit code $LASTEXITCODE."
        }

        # 3. Build so Roslyn loads the new analyzer. --no-restore avoids a redundant restore after step 2.
        if ($SkipBuild) {
            Write-Host '==> [3/3] Skipping build (-SkipBuild).' -ForegroundColor Yellow
        }
        else {
            Write-Host "==> [3/3] Building '$Target' ($Configuration) ..." -ForegroundColor Cyan
            dotnet build $effectiveTarget --configuration $Configuration --no-restore
            if ($LASTEXITCODE -ne 0) {
                throw "dotnet build '$Target' failed with exit code $LASTEXITCODE."
            }
        }
    }
    finally {
        if ($tempFilter -and (Test-Path -LiteralPath $tempFilter)) {
            Remove-Item -LiteralPath $tempFilter -Force
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
