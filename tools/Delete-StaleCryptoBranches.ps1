# ---------------------------------------------------------------------------------------------------------------
# Delete-StaleCryptoBranches.ps1
#
# Deletes the 28 unmerged crypto/hashing branches identified during the systematic branch assessment
# captured in PRs #200, #201, #202, and #204 (closed). Each branch falls into one of three categories:
#
#   - GHOST     14 branches whose squash-merged work already lives on master (zero diff vs origin/master).
#   - STALE     12 predecessor branches whose work landed verbatim via #200, #201, or #202.
#   - DEGRADES   1 branch (improve-hashing-crypto-extension-docs) whose 30 doc commits would revert
#                the IDisposable / lifecycle additions made by #200 — see PR #204 for the full analysis.
#   - UTILITY    1 transient branch (claude/consolidate-hashing-crypto) created to drive #200; redundant.
#
# Usage:
#   pwsh ./tools/Delete-StaleCryptoBranches.ps1               # dry-run; lists what would happen
#   pwsh ./tools/Delete-StaleCryptoBranches.ps1 -Confirm      # actually delete (requires gh auth)
#   pwsh ./tools/Delete-StaleCryptoBranches.ps1 -Confirm -Skip 'hashing-coverage','refactor-block-hash-base-types'
#
# Requires:
#   - PowerShell 7+
#   - gh CLI authenticated against bslater/bodu with branch:write scope
# ---------------------------------------------------------------------------------------------------------------

[CmdletBinding()]
param(
    [string]$Owner = 'bslater',

    [string]$Repo = 'bodu',

    # Pass -Confirm to actually delete branches. Without this the script is dry-run only.
    [switch]$Confirm,

    # Optional: branch names (without the 'origin/' prefix) to skip even if they appear in the
    # canonical lists below. Useful if you want to keep one or two for follow-up work.
    [string[]]$Skip = @()
)

$ErrorActionPreference = 'Stop'

# ── Branch lists ────────────────────────────────────────────────────────────────────────────────

# 14 ghost branches: zero diff against current master (work already squash-merged via earlier PRs).
$Ghost = @(
    'claude/add-blake-keyed-property-q9NiR',
    'claude/add-noncrypto-hash-extensions-WfGK7',
    'claude/add-transform-mode-kats-wtLc2',
    'claude/blake3-deferred-final-WDQjE',
    'claude/consolidate-merkle-blake-JHdAJ',
    'claude/fix-crypto-tests-FUIy2',
    'claude/hashing-todo-jNUhB',
    'claude/implement-aead-foundations-OzEAf',
    'claude/refactor-skein-tests-Dgfe7',
    'claude/refresh-crypto-docs-gW4f7',
    'claude/standardize-cipher-tests-3atem',
    'claude/update-hash-algorithm-docs-gugl8',
    'hashing-coverage',
    'refactor-block-hash-base-types'
)

# 12 stale predecessor branches: work landed via #200, #201, or #202.
$Stale = @(
    'claude/empty-final-block-allowed',
    'claude/fix-aead-extension-method-ambiguity',
    'claude/fix-aead-nonce-test-ocb',
    'claude/fix-gcm-nonce-in-extensions-tests',
    'claude/cryptohelpers-tryfill-perbyte-rejection',
    'claude/test-hash-extensions-ZRsPG',
    'claude/mode-transform-disposal',
    'claude/aead-tier3-single-use',
    'claude/aead-tier2-tests',
    'claude/test-crypto-edge-cases-bLCr3',
    'claude/scope-aead-extension-tests-to-extensions',
    'claude/cubehash-dispose-fields'
)

# 1 branch that would degrade master if merged (PR #204 documents the analysis).
$Degrades = @(
    'claude/improve-hashing-crypto-extension-docs'
)

# 1 utility branch created to land #200; redundant once #200 merged.
$Utility = @(
    'claude/consolidate-hashing-crypto'
)

# ── Helpers ─────────────────────────────────────────────────────────────────────────────────────

function Test-GhAvailable {
    $cmd = Get-Command gh -ErrorAction SilentlyContinue
    if (-not $cmd) {
        throw "gh CLI is not installed or not on PATH. See https://cli.github.com/."
    }

    $authStatus = & gh auth status --hostname github.com 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "gh is not authenticated against github.com. Run 'gh auth login' and retry. Output: $authStatus"
    }
}

function Test-BranchExists {
    param(
        [Parameter(Mandatory)] [string]$Branch
    )

    $encoded = [uri]::EscapeDataString($Branch)
    & gh api -H 'Accept: application/vnd.github+json' "repos/$Owner/$Repo/branches/$encoded" 2>$null | Out-Null
    return ($LASTEXITCODE -eq 0)
}

function Remove-RemoteBranch {
    param(
        [Parameter(Mandatory)] [string]$Branch
    )

    $encoded = [uri]::EscapeDataString($Branch)
    $ref = "heads/$encoded"

    & gh api `
        -X DELETE `
        -H 'Accept: application/vnd.github+json' `
        "repos/$Owner/$Repo/git/refs/$ref" 2>&1 | Out-Null

    return ($LASTEXITCODE -eq 0)
}

# ── Main ────────────────────────────────────────────────────────────────────────────────────────

$allBranches = @(
    @{ Category = 'GHOST';    Branches = $Ghost    },
    @{ Category = 'STALE';    Branches = $Stale    },
    @{ Category = 'DEGRADES'; Branches = $Degrades },
    @{ Category = 'UTILITY';  Branches = $Utility  }
)

$flat = $allBranches | ForEach-Object {
    $category = $_.Category
    $_.Branches | ForEach-Object {
        [PSCustomObject]@{
            Category = $category
            Name     = $_
        }
    }
}

$totalListed = $flat.Count
$skipped = $flat | Where-Object { $Skip -contains $_.Name }
$targets = $flat | Where-Object { $Skip -notcontains $_.Name }

Write-Host ""
Write-Host "Target repo: $Owner/$Repo"
Write-Host "Total branches listed: $totalListed"
Write-Host "Branches skipped via -Skip: $($skipped.Count)"
Write-Host "Branches to act on: $($targets.Count)"
Write-Host ""

if (-not $Confirm) {
    Write-Host "[DRY RUN — pass -Confirm to actually delete]" -ForegroundColor Yellow
    Write-Host ""
    $targets | Format-Table -AutoSize Category, Name
    return
}

Test-GhAvailable

$results = New-Object System.Collections.Generic.List[object]

foreach ($entry in $targets) {
    $branch = $entry.Name
    $category = $entry.Category

    Write-Host -NoNewline ("[{0,-8}] {1,-60} ... " -f $category, $branch)

    if (-not (Test-BranchExists -Branch $branch)) {
        Write-Host "skipped (does not exist on origin)" -ForegroundColor DarkGray
        $results.Add([PSCustomObject]@{ Branch = $branch; Status = 'NotFound' })
        continue
    }

    if (Remove-RemoteBranch -Branch $branch) {
        Write-Host "deleted" -ForegroundColor Green
        $results.Add([PSCustomObject]@{ Branch = $branch; Status = 'Deleted' })
    }
    else {
        Write-Host "FAILED" -ForegroundColor Red
        $results.Add([PSCustomObject]@{ Branch = $branch; Status = 'Failed' })
    }
}

Write-Host ""
Write-Host "── Summary ──────────────────────────────────────────────────────"
$results | Group-Object Status | ForEach-Object {
    Write-Host ("{0,-10} {1}" -f "$($_.Name):", $_.Count)
}
Write-Host ""

$failed = $results | Where-Object Status -eq 'Failed'
if ($failed) {
    Write-Host "The following branches could not be deleted:" -ForegroundColor Red
    $failed | ForEach-Object { Write-Host "  - $($_.Branch)" }
    exit 1
}
