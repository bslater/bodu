<#
.SYNOPSIS
    Raises the per-package coverage ratchet floors from a measured run.

.DESCRIPTION
    Reads the machine-readable package rows emitted by tools/New-CoverageMatrix.ps1 and updates
    bld/coverage-thresholds.json.

    The ratchet only ever moves up. A floor is raised when the measured figure clears it by more
    than the grace band, and is left untouched otherwise - a run that comes in low never lowers a
    floor, because that would let a regression silently become the new normal. Enforcing direction
    rather than an absolute number is deliberate: 100% is not the target.

    Floors are recorded slightly below the measured figure so that ordinary run-to-run drift - the
    suite runs with MaxCpuCount=0, and the timing-sensitive threading and concurrent-collection
    suites genuinely vary - does not turn the next run red.

    Packages absent from the run keep their existing floors, so collecting a subset (a PR-scoped
    run, or a single shard) can never erase the rest of the ratchet.

.PARAMETER RowsPath
    Package rows from New-CoverageMatrix.ps1. Default: artifacts/coverage/report/packages.json.

.PARAMETER ThresholdPath
    Floors to update. Default: bld/coverage-thresholds.json.

.PARAMETER Grace
    Percentage points subtracted from the measured figure when recording a floor. Default: 0.5.

.PARAMETER WhatIf
    Report what would change without writing.

.EXAMPLE
    pwsh tools/Update-CoverageThresholds.ps1

.EXAMPLE
    pwsh tools/Update-CoverageThresholds.ps1 -WhatIf
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$RowsPath,
    [string]$ThresholdPath,
    [double]$Grace = 0.5
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot

if (-not $RowsPath)      { $RowsPath = Join-Path $repoRoot 'artifacts/coverage/report/packages.json' }
if (-not $ThresholdPath) { $ThresholdPath = Join-Path $repoRoot 'bld/coverage-thresholds.json' }

if (-not (Test-Path -LiteralPath $RowsPath)) {
    throw "Package rows not found at '$RowsPath'. Run bld/merge-coverage.sh then tools/New-CoverageMatrix.ps1 first."
}

$rows = @(Get-Content -LiteralPath $RowsPath -Raw | ConvertFrom-Json)

$existing = [ordered]@{}
if (Test-Path -LiteralPath $ThresholdPath) {
    $loaded = Get-Content -LiteralPath $ThresholdPath -Raw | ConvertFrom-Json
    foreach ($property in $loaded.PSObject.Properties) {
        $existing[$property.Name] = [ordered]@{
            line   = $property.Value.line
            branch = $property.Value.branch
        }
    }
}

$raised = [System.Collections.Generic.List[string]]::new()
$held = 0

foreach ($row in ($rows | Sort-Object Package)) {
    if (-not $row.Collected) { continue }

    if (-not $existing.Contains($row.Package)) {
        $existing[$row.Package] = [ordered]@{ line = 0.0; branch = 0.0 }
    }

    $entry = $existing[$row.Package]

    foreach ($metric in @('line', 'branch')) {
        $measured = if ($metric -eq 'line') { $row.LineRate } else { $row.BranchRate }
        if ($null -eq $measured) { continue }

        $candidate = [math]::Round([math]::Max(0.0, $measured - $Grace), 1)
        $current = [double]$entry[$metric]

        if ($candidate -gt $current) {
            $raised.Add(("{0} {1}: {2}% -> {3}% (measured {4}%)" -f $row.Package, $metric, $current, $candidate, $measured))
            $entry[$metric] = $candidate
        }
        else {
            $held++
        }
    }
}

# Stable key order keeps the committed file's diffs to the floors that actually moved.
$ordered = [ordered]@{}
foreach ($key in ($existing.Keys | Sort-Object)) { $ordered[$key] = $existing[$key] }

$json = $ordered | ConvertTo-Json -Depth 3

if ($PSCmdlet.ShouldProcess($ThresholdPath, "Update $($raised.Count) coverage floor(s)")) {
    $directory = Split-Path -Parent $ThresholdPath
    if ($directory -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    Set-Content -LiteralPath $ThresholdPath -Value $json
}

if ($raised.Count -gt 0) {
    Write-Host "Raised $($raised.Count) floor(s):"
    foreach ($entry in $raised) { Write-Host "  $entry" }
}
else {
    Write-Host "No floor raised; the ratchet is unchanged."
}

Write-Host "$held metric(s) held at or below their existing floor (never lowered)."
Write-Host "Thresholds: $ThresholdPath"
