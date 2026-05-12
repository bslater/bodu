# -------------------------------------------------------------------------------------------------
# Remove-StaleRemoteBranches.ps1
#
# Lists stale remote branches, groups them by prefix, and optionally deletes selected groups.
#
# Default behaviour is safe:
#   - fetches and prunes origin
#   - only includes branches merged into origin/main
#   - excludes protected branch names
#   - dry-run unless -Delete is supplied
# -------------------------------------------------------------------------------------------------

[CmdletBinding()]
param(
    [string] $Remote = "origin",

    [string] $Base = "main",

    [int] $OlderThanDays = 30,

    [switch] $IncludeUnmerged,

    [switch] $SkipFetch,

    [switch] $Delete,

    [string[]] $ProtectedBranches = @(
        "main",
        "master",
        "develop",
        "development",
        "dev",
        "trunk",
        "production",
        "staging",
        "release"
    )
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Invoke-Git {
    param(
        [Parameter(Mandatory)]
        [string[]] $Arguments,

        [switch] $AllowFailure
    )

    $output = & git @Arguments 2>&1
    $exitCode = $LASTEXITCODE

    if ($exitCode -ne 0 -and -not $AllowFailure) {
        throw "git $($Arguments -join ' ') failed with exit code $exitCode.`n$output"
    }

    return @($output)
}

function Test-ProtectedBranch {
    param(
        [Parameter(Mandatory)]
        [string] $BranchName
    )

    foreach ($protected in $ProtectedBranches) {
        if ($BranchName -eq $protected) {
            return $true
        }

        if ($BranchName.StartsWith("$protected/", [StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    return $false
}

function ConvertTo-Selection {
    param(
        [Parameter(Mandatory)]
        [string] $InputText,

        [Parameter(Mandatory)]
        [int] $Maximum
    )

    $selected = [System.Collections.Generic.HashSet[int]]::new()

    if ($InputText.Trim().Equals("all", [StringComparison]::OrdinalIgnoreCase)) {
        1..$Maximum | ForEach-Object { [void] $selected.Add($_) }
        return $selected.ToArray() | Sort-Object
    }

    foreach ($part in $InputText.Split(",", [System.StringSplitOptions]::RemoveEmptyEntries)) {
        $token = $part.Trim()

        if ($token -match "^\d+$") {
            $value = [int] $token

            if ($value -lt 1 -or $value -gt $Maximum) {
                throw "Selection '$value' is outside the valid range 1..$Maximum."
            }

            [void] $selected.Add($value)
            continue
        }

        if ($token -match "^(\d+)-(\d+)$") {
            $start = [int] $Matches[1]
            $end = [int] $Matches[2]

            if ($start -gt $end) {
                throw "Invalid range '$token'."
            }

            if ($start -lt 1 -or $end -gt $Maximum) {
                throw "Selection '$token' is outside the valid range 1..$Maximum."
            }

            $start..$end | ForEach-Object { [void] $selected.Add($_) }
            continue
        }

        throw "Invalid selection token '$token'. Use examples like 1,3,5-7 or all."
    }

    return $selected.ToArray() | Sort-Object
}

Invoke-Git @("rev-parse", "--is-inside-work-tree") | Out-Null

if (-not $SkipFetch) {
    Write-Host "Fetching and pruning '$Remote'..."
    Invoke-Git @("fetch", $Remote, "--prune") | Out-Null
}

$baseRef = "refs/remotes/$Remote/$Base"
$baseExists = Invoke-Git @("show-ref", "--verify", "--quiet", $baseRef) -AllowFailure

if ($LASTEXITCODE -ne 0) {
    $headRef = Invoke-Git @("symbolic-ref", "--short", "refs/remotes/$Remote/HEAD") -AllowFailure |
        Select-Object -First 1

    if (-not [string]::IsNullOrWhiteSpace($headRef) -and $headRef.StartsWith("$Remote/")) {
        $Base = $headRef.Substring($Remote.Length + 1)
        $baseRef = "refs/remotes/$Remote/$Base"
        Write-Host "Base branch '$Remote/$Base' inferred from '$Remote/HEAD'."
    } else {
        throw "Could not find '$Remote/$Base'. Pass -Base <branch> explicitly."
    }
}

$mergedRefs = Invoke-Git @("branch", "-r", "--merged", "$Remote/$Base") |
    ForEach-Object { $_.Trim() } |
    Where-Object { $_ -and $_ -ne "$Remote/HEAD" }

$mergedSet = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($ref in $mergedRefs) {
    [void] $mergedSet.Add($ref)
}

$separator = [char] 0x1f
$format = "%(refname:short)%x1f%(committerdate:iso8601-strict)%x1f%(authorname)%x1f%(objectname:short)%x1f%(contents:subject)"

$rawRefs = Invoke-Git @(
    "for-each-ref",
    "refs/remotes/$Remote",
    "--sort=committerdate",
    "--format=$format"
)

$now = Get-Date

$branches = foreach ($line in $rawRefs) {
    if ([string]::IsNullOrWhiteSpace($line)) {
        continue
    }

    $parts = $line.Split($separator, 5)

    if ($parts.Count -lt 5) {
        continue
    }

    $remoteBranch = $parts[0]

    if ($remoteBranch -eq "$Remote/HEAD") {
        continue
    }

    if (-not $remoteBranch.StartsWith("$Remote/")) {
        continue
    }

    $branchName = $remoteBranch.Substring($Remote.Length + 1)

    if (Test-ProtectedBranch $branchName) {
        continue
    }

    $date = [datetimeoffset]::Parse($parts[1]).LocalDateTime
    $ageDays = [int] [Math]::Floor(($now - $date).TotalDays)
    $isMerged = $mergedSet.Contains($remoteBranch)

    if (-not $IncludeUnmerged -and -not $isMerged) {
        continue
    }

    if ($ageDays -lt $OlderThanDays) {
        continue
    }

    $prefix = if ($branchName.Contains("/")) {
        $branchName.Split("/", 2)[0]
    } else {
        "(none)"
    }

    [pscustomobject] @{
        RemoteBranch = $remoteBranch
        BranchName   = $branchName
        Prefix       = $prefix
        LastCommit   = $date
        AgeDays      = $ageDays
        Merged       = $isMerged
        Author       = $parts[2]
        Commit       = $parts[3]
        Subject      = $parts[4]
    }
}

$branches = @($branches)

if ($branches.Count -eq 0) {
    Write-Host "No stale remote branches matched the current filters."
    Write-Host "Remote: $Remote"
    Write-Host "Base: $Remote/$Base"
    Write-Host "OlderThanDays: $OlderThanDays"
    Write-Host "IncludeUnmerged: $IncludeUnmerged"
    exit 0
}

Write-Host ""
Write-Host "Candidate remote branches:"
Write-Host "Remote:          $Remote"
Write-Host "Base:            $Remote/$Base"
Write-Host "OlderThanDays:   $OlderThanDays"
Write-Host "IncludeUnmerged: $IncludeUnmerged"
Write-Host "Delete mode:     $Delete"
Write-Host ""

$groups = @(
    $branches |
        Group-Object Prefix |
        Sort-Object Name |
        ForEach-Object {
            $items = @($_.Group)

            [pscustomobject] @{
                Id         = 0
                Prefix     = $_.Name
                Count      = $items.Count
                Merged     = ($items | Where-Object Merged).Count
                Unmerged   = ($items | Where-Object { -not $_.Merged }).Count
                OldestDays = ($items | Measure-Object AgeDays -Maximum).Maximum
                NewestDays = ($items | Measure-Object AgeDays -Minimum).Minimum
            }
        }
)

for ($i = 0; $i -lt $groups.Count; $i++) {
    $groups[$i].Id = $i + 1
}

$groups | Format-Table Id, Prefix, Count, Merged, Unmerged, OldestDays, NewestDays -AutoSize

Write-Host ""
Write-Host "Enter group numbers to select for deletion."
Write-Host "Examples: 1,3,5-7 or all"
Write-Host "Press Enter to cancel."
Write-Host ""

$selectionText = Read-Host "Groups"

if ([string]::IsNullOrWhiteSpace($selectionText)) {
    Write-Host "Cancelled."
    exit 0
}

$selectedIds = ConvertTo-Selection -InputText $selectionText -Maximum $groups.Count
$selectedPrefixes = $groups |
    Where-Object { $selectedIds -contains $_.Id } |
    Select-Object -ExpandProperty Prefix

$selectedBranches = @(
    $branches |
        Where-Object { $selectedPrefixes -contains $_.Prefix } |
        Sort-Object Prefix, LastCommit, BranchName
)

Write-Host ""
Write-Host "Selected branches:"
Write-Host ""

$selectedBranches |
    Select-Object BranchName, Prefix, AgeDays, Merged, LastCommit, Author, Commit, Subject |
    Format-Table -AutoSize

Write-Host ""
Write-Host "Branches selected: $($selectedBranches.Count)"

if (-not $Delete) {
    Write-Host ""
    Write-Host "Dry run only. No remote branches were deleted."
    Write-Host "To delete the selected branches, rerun the script with -Delete."
    Write-Host ""
    Write-Host "Equivalent commands:"
    foreach ($branch in $selectedBranches) {
        Write-Host "git push $Remote --delete `"$($branch.BranchName)`""
    }

    exit 0
}

Write-Host ""
Write-Host "WARNING: This will delete $($selectedBranches.Count) branch(es) from remote '$Remote'."
Write-Host "Type DELETE to continue."
$confirmation = Read-Host "Confirm"

if ($confirmation -ne "DELETE") {
    Write-Host "Cancelled."
    exit 0
}

foreach ($branch in $selectedBranches) {
    Write-Host "Deleting $($branch.RemoteBranch)..."

    try {
        Invoke-Git @("push", $Remote, "--delete", $branch.BranchName) | Out-Host
    } catch {
        Write-Warning "Failed to delete $($branch.RemoteBranch): $_"
    }
}

Write-Host ""
Write-Host "Done."