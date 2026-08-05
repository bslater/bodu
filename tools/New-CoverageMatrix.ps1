<#
.SYNOPSIS
    Builds the per-package coverage matrix from a merged Cobertura report.

.DESCRIPTION
    Reads the merged Cobertura report produced by bld/merge-coverage.sh and computes the per-package
    line and branch coverage that docs/articles/coverage-baseline.md publishes.

    The percentages are computed here rather than taken from ReportGenerator's summary, because three
    corrections documented in docs/articles/code-coverage.md have to be applied first and none of
    them is expressible as a report filter:

      1. Phantom rows. Coverage is keyed by source path, so a report collected across - or merged
         over - a commit that moved or renamed files carries 0% rows for paths that no longer exist,
         dragging every aggregate down. Any row whose file does not resolve on disk is discarded and
         counted.

      2. Shared-source attribution. Bodu.Text.Serialization/shared/** is compiled directly into
         Bodu.Text.Toml, Bodu.Text.Bencode and Bodu.Text.Yaml, so those lines appear three times
         under three assemblies. They are reported once as a synthetic unit whose coverage is the
         union across the three hosts, and subtracted from the three host packages so the solution
         total does not count them three times.

      3. Hardware-gated paths. The JIT selects exactly one of the AVX-512 or scalar implementations
         per process, so a single run cannot cover both. Files that the collecting host could not
         possibly execute are reported as n/a and excluded from the denominator rather than as 0%.

.PARAMETER ReportPath
    Merged Cobertura report. Default: artifacts/coverage/report/Cobertura.xml.

.PARAMETER ReportDirectory
    Directory holding the collection manifests (environment*.json). Default: the report's directory.

.PARAMETER OutputPath
    Markdown matrix to write. Default: docs/articles/coverage-baseline.md.

.PARAMETER ThresholdPath
    Per-package ratchet floors. Default: bld/coverage-thresholds.json.

.PARAMETER Grace
    Percentage points a package may fall below its floor before -Check fails. Absorbs run-to-run
    drift from parallel execution. Default: 0.5.

.PARAMETER Check
    Compare against the ratchet floors and exit non-zero on a regression. Writes no matrix.

.EXAMPLE
    pwsh tools/New-CoverageMatrix.ps1

.EXAMPLE
    pwsh tools/New-CoverageMatrix.ps1 -Check
#>
[CmdletBinding()]
param(
    [string]$ReportPath,
    [string]$ReportDirectory,
    [string]$OutputPath,
    [string]$ThresholdPath,
    [double]$Grace = 0.5,
    [switch]$Check
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot

if (-not $ReportPath)      { $ReportPath = Join-Path $repoRoot 'artifacts/coverage/report/Cobertura.xml' }
if (-not $ReportDirectory) { $ReportDirectory = Split-Path -Parent $ReportPath }
if (-not $OutputPath)      { $OutputPath = Join-Path $repoRoot 'docs/articles/coverage-baseline.md' }
if (-not $ThresholdPath)   { $ThresholdPath = Join-Path $repoRoot 'bld/coverage-thresholds.json' }

if (-not (Test-Path -LiteralPath $ReportPath)) {
    throw "Merged report not found at '$ReportPath'. Run bld/collect-coverage.sh then bld/merge-coverage.sh first."
}

# The synthetic unit for source compiled into more than one assembly, and the assemblies it is
# compiled into. Keep these in step with the Compile Include globs in the three host csproj files.
$SharedSourcePrefix   = 'Bodu.Text.Serialization/shared/'
$SharedSourceUnit     = 'Bodu.Text.Serialization (shared source)'
$SharedSourceHosts    = @('Bodu.Text.Toml', 'Bodu.Text.Bencode', 'Bodu.Text.Yaml')

# ---------------------------------------------------------------------------------------------------------------
# Collection manifests
# ---------------------------------------------------------------------------------------------------------------
$manifests = @(Get-ChildItem -Path $ReportDirectory -Filter 'environment*.json' -ErrorAction SilentlyContinue |
    ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json })

$avx512 = 'unknown'
if ($manifests.Count -gt 0) {
    # Any shard that saw AVX-512 means the intrinsic paths were reachable in this collection.
    if ($manifests | Where-Object { $_.avx512Supported -eq 'true' }) { $avx512 = 'true' }
    elseif ($manifests | Where-Object { $_.avx512Supported -eq 'false' }) { $avx512 = 'false' }
}

$commit = if ($manifests.Count -gt 0 -and $manifests[0].commit) { $manifests[0].commit } else { 'unknown' }

# ---------------------------------------------------------------------------------------------------------------
# Packable package inventory
#
# Enumerated recursively: the five regional calendar data packs live a level deeper, under
# Bodu.Globalization.Calendar.Data/, than the other projects.
# ---------------------------------------------------------------------------------------------------------------
$packableProjects = Get-ChildItem -Path $repoRoot -Filter '*.csproj' -Recurse -File |
    Where-Object {
        $rel = $_.FullName.Substring($repoRoot.Length + 1).Replace('\', '/')
        $rel -match '/src/' -and
        $rel -notmatch '^(Bodu\.CodeStyle|samples|tools)/'
    } |
    Where-Object { (Get-Content -LiteralPath $_.FullName -Raw) -notmatch '<IsPackable>\s*false\s*</IsPackable>' }

$packableNames = @($packableProjects | ForEach-Object { $_.BaseName } | Sort-Object -Unique)

# Package status (Stable / Preview) as published by the package matrix.
$status = @{}
$packageMatrixPath = Join-Path $repoRoot 'docs/docs/package-matrix.md'
if (Test-Path -LiteralPath $packageMatrixPath) {
    foreach ($line in Get-Content -LiteralPath $packageMatrixPath) {
        if ($line -match '`(Bodu\.[A-Za-z0-9.]+)`' ) {
            $name = $Matches[1]
            if ($line -match '\|\s*(Stable|Preview)\s*\|' -and -not $status.ContainsKey($name)) {
                $status[$name] = $Matches[1]
            }
        }
    }
}

# ---------------------------------------------------------------------------------------------------------------
# Parse the merged report
#
# Lines are unioned across every assembly that reports them: a source file exercised by more than one
# test assembly must be credited once, not counted once per contributor.
# ---------------------------------------------------------------------------------------------------------------
[xml]$report = Get-Content -LiteralPath $ReportPath -Raw

$sourceRoots = @($report.coverage.sources.source)

# file -> @{ Package; Lines = @{ number = hits }; Branches = @{ number = @(covered, total) } }
$files = @{}
$phantom = [System.Collections.Generic.List[string]]::new()
$resolved = @{}

function Resolve-SourceFile([string]$filename) {
    if ($resolved.ContainsKey($filename)) { return $resolved[$filename] }

    $candidate = $null
    if ([IO.Path]::IsPathRooted($filename) -and (Test-Path -LiteralPath $filename)) {
        $candidate = $filename
    }
    else {
        foreach ($root in $sourceRoots) {
            $joined = Join-Path $root $filename
            if (Test-Path -LiteralPath $joined) { $candidate = $joined; break }
        }
    }

    $resolved[$filename] = $candidate
    return $candidate
}

foreach ($package in $report.coverage.packages.package) {
    $packageName = $package.name

    foreach ($class in $package.classes.class) {
        $filename = $class.filename
        if (-not $filename) { continue }

        $full = Resolve-SourceFile $filename
        if (-not $full) {
            # Phantom: a path recorded by the collector that no longer exists on disk. Counting it
            # would import a 0% row for code that has moved, not code that is untested.
            if (-not $phantom.Contains($filename)) { $phantom.Add($filename) }
            continue
        }

        $relative = (Resolve-Path -LiteralPath $full).Path.Substring($repoRoot.Length).TrimStart('/', '\').Replace('\', '/')

        if (-not $files.ContainsKey($relative)) {
            $files[$relative] = @{
                Package  = $packageName
                Lines    = @{}
                Branches = @{}
            }
        }

        $entry = $files[$relative]

        foreach ($line in $class.lines.line) {
            $number = [int]$line.number
            $hits = [int]$line.hits

            if (-not $entry.Lines.ContainsKey($number) -or $entry.Lines[$number] -lt $hits) {
                $entry.Lines[$number] = $hits
            }

            if ($line.branch -eq 'True' -and $line.'condition-coverage' -match '\((\d+)/(\d+)\)') {
                $covered = [int]$Matches[1]
                $total = [int]$Matches[2]
                if (-not $entry.Branches.ContainsKey($number) -or $entry.Branches[$number][0] -lt $covered) {
                    $entry.Branches[$number] = @($covered, $total)
                }
            }
        }
    }
}

# ---------------------------------------------------------------------------------------------------------------
# Reassign shared source to its synthetic unit
# ---------------------------------------------------------------------------------------------------------------
foreach ($relative in @($files.Keys)) {
    if ($relative.StartsWith($SharedSourcePrefix, [StringComparison]::Ordinal)) {
        $files[$relative].Package = $SharedSourceUnit
    }
}

# ---------------------------------------------------------------------------------------------------------------
# Hardware-gated files
#
# When the collecting host lacks AVX-512 the intrinsic implementations cannot execute, so they are
# reported as n/a. Bodu.Security.Cryptography.Simd.Test forces the SIMD feature switch off in its own
# process, so when it is part of the collection the scalar fallbacks are covered regardless.
# ---------------------------------------------------------------------------------------------------------------
$hardwareGated = @()
if ($avx512 -ne 'true') {
    $hardwareGated = @($files.Keys | Where-Object { $_ -like '*.Avx512.cs' })
}

# ---------------------------------------------------------------------------------------------------------------
# Aggregate per package
# ---------------------------------------------------------------------------------------------------------------
$aggregate = @{}

foreach ($relative in $files.Keys) {
    if ($hardwareGated -contains $relative) { continue }

    $entry = $files[$relative]
    $name = $entry.Package

    if (-not $aggregate.ContainsKey($name)) {
        $aggregate[$name] = [ordered]@{
            CoveredLines    = 0
            TotalLines      = 0
            CoveredBranches = 0
            TotalBranches   = 0
        }
    }

    $bucket = $aggregate[$name]
    $bucket.TotalLines += $entry.Lines.Count
    $bucket.CoveredLines += @($entry.Lines.Values | Where-Object { $_ -gt 0 }).Count

    foreach ($pair in $entry.Branches.Values) {
        $bucket.CoveredBranches += $pair[0]
        $bucket.TotalBranches += $pair[1]
    }
}

function Get-Rate([int]$covered, [int]$total) {
    if ($total -le 0) { return $null }
    return [math]::Round(100.0 * $covered / $total, 1)
}

$rows = foreach ($name in ($aggregate.Keys + $packableNames | Sort-Object -Unique)) {
    if ($name -eq 'Bodu.Text.Formats') { continue }   # meta-package: no source, no coverage to report

    $bucket = $aggregate[$name]

    [pscustomobject]@{
        Package      = $name
        Status       = if ($status.ContainsKey($name)) { $status[$name] } elseif ($name -eq $SharedSourceUnit) { 'Stable' } else { '' }
        Collected    = [bool]$bucket
        CoveredLines = if ($bucket) { $bucket.CoveredLines } else { 0 }
        TotalLines   = if ($bucket) { $bucket.TotalLines } else { 0 }
        LineRate     = if ($bucket) { Get-Rate $bucket.CoveredLines $bucket.TotalLines } else { $null }
        BranchRate   = if ($bucket) { Get-Rate $bucket.CoveredBranches $bucket.TotalBranches } else { $null }
    }
}

$rows = @($rows | Sort-Object Package)

# ---------------------------------------------------------------------------------------------------------------
# -Check: the ratchet
# ---------------------------------------------------------------------------------------------------------------
if ($Check) {
    if (-not (Test-Path -LiteralPath $ThresholdPath)) {
        Write-Host "No threshold file at '$ThresholdPath'; nothing to enforce yet."
        exit 0
    }

    $thresholds = Get-Content -LiteralPath $ThresholdPath -Raw | ConvertFrom-Json
    $failures = [System.Collections.Generic.List[string]]::new()

    foreach ($row in $rows) {
        if (-not $row.Collected) { continue }

        $floor = $thresholds.PSObject.Properties[$row.Package]
        if (-not $floor) { continue }

        foreach ($metric in @('line', 'branch')) {
            $expected = $floor.Value.PSObject.Properties[$metric]
            if (-not $expected) { continue }

            $actual = if ($metric -eq 'line') { $row.LineRate } else { $row.BranchRate }
            if ($null -eq $actual) { continue }

            if ($actual -lt ($expected.Value - $Grace)) {
                $failures.Add(("{0}: {1} coverage {2}% is below its floor of {3}% (grace {4}pp)" -f
                    $row.Package, $metric, $actual, $expected.Value, $Grace))
            }
        }
    }

    if ($phantom.Count -gt 0) {
        Write-Warning "$($phantom.Count) phantom row(s) discarded - the report spans a commit that moved or renamed source files. Re-collect on a clean checkout."
        foreach ($p in $phantom) { Write-Warning "  $p" }
    }

    if ($failures.Count -gt 0) {
        Write-Host "Coverage regression detected:" -ForegroundColor Red
        foreach ($f in $failures) { Write-Host "  $f" -ForegroundColor Red }
        exit 1
    }

    Write-Host "Coverage ratchet satisfied for $(@($rows | Where-Object Collected).Count) collected package(s)."
    exit 0
}

# ---------------------------------------------------------------------------------------------------------------
# Emit the matrix
# ---------------------------------------------------------------------------------------------------------------
$totalCovered = ($rows | Measure-Object -Property CoveredLines -Sum).Sum
$totalLines = ($rows | Measure-Object -Property TotalLines -Sum).Sum
$overall = Get-Rate $totalCovered $totalLines

$sb = [System.Text.StringBuilder]::new()

[void]$sb.AppendLine('# Coverage baseline')
[void]$sb.AppendLine()
[void]$sb.AppendLine('Per-package line and branch coverage for every packable Bodu package, computed from a merged')
[void]$sb.AppendLine('Cobertura report. Regenerate with `pwsh tools/New-CoverageMatrix.ps1`; do not hand-edit.')
[void]$sb.AppendLine()
[void]$sb.AppendLine('Legend: `—` = not part of this collection · `n/a` = excluded by design (see [Code coverage strategy](code-coverage.md)).')
[void]$sb.AppendLine()
[void]$sb.AppendLine('| Package | Status | Line % | Branch % | Covered / total lines |')
[void]$sb.AppendLine('|---|---|--:|--:|--:|')

foreach ($row in $rows) {
    $line = if ($null -ne $row.LineRate) { "$($row.LineRate)%" } else { '—' }
    $branch = if ($null -ne $row.BranchRate) { "$($row.BranchRate)%" } else { '—' }
    $lines = if ($row.Collected) { "$($row.CoveredLines) / $($row.TotalLines)" } else { '—' }
    [void]$sb.AppendLine("| ``$($row.Package)`` | $($row.Status) | $line | $branch | $lines |")
}

[void]$sb.AppendLine()
[void]$sb.AppendLine("**Overall:** $overall% ($totalCovered / $totalLines lines across $(@($rows | Where-Object Collected).Count) collected package(s)).")
[void]$sb.AppendLine()
[void]$sb.AppendLine('## Baseline evidence')
[void]$sb.AppendLine()
[void]$sb.AppendLine('```bash')
[void]$sb.AppendLine('bld/collect-coverage.sh')
[void]$sb.AppendLine('bld/merge-coverage.sh')
[void]$sb.AppendLine('pwsh tools/New-CoverageMatrix.ps1')
[void]$sb.AppendLine('```')
[void]$sb.AppendLine()
[void]$sb.AppendLine("- Commit: ``$commit``")
[void]$sb.AppendLine("- ``Avx512F.IsSupported`` on the collecting host: ``$avx512``")
[void]$sb.AppendLine("- Phantom rows discarded: $($phantom.Count)")

if ($hardwareGated.Count -gt 0) {
    [void]$sb.AppendLine()
    [void]$sb.AppendLine('### Hardware-gated files excluded from the denominator')
    [void]$sb.AppendLine()
    [void]$sb.AppendLine('The collecting host does not support AVX-512, so the JIT could not select these')
    [void]$sb.AppendLine('implementations. They are exercised by the standard known-answer suites on capable')
    [void]$sb.AppendLine('hardware and are **not** a missing-test gap.')
    [void]$sb.AppendLine()
    foreach ($file in ($hardwareGated | Sort-Object)) { [void]$sb.AppendLine("- ``$file``") }
}

if ($phantom.Count -gt 0) {
    [void]$sb.AppendLine()
    [void]$sb.AppendLine('### Phantom rows discarded')
    [void]$sb.AppendLine()
    [void]$sb.AppendLine('These paths were recorded by the collector but do not exist on disk, which means the report')
    [void]$sb.AppendLine('spans a commit that moved or renamed source files. Re-collect on a clean checkout.')
    [void]$sb.AppendLine()
    foreach ($p in ($phantom | Sort-Object)) { [void]$sb.AppendLine("- ``$p``") }
}

$outputDirectory = Split-Path -Parent $OutputPath
if ($outputDirectory -and -not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

Set-Content -LiteralPath $OutputPath -Value $sb.ToString() -NoNewline

Write-Host "Wrote coverage matrix: $OutputPath"
Write-Host "  packages reported : $($rows.Count) ($(@($rows | Where-Object Collected).Count) collected)"
Write-Host "  overall line rate : $overall%"
Write-Host "  phantom discarded : $($phantom.Count)"
Write-Host "  hardware-gated    : $($hardwareGated.Count)"
