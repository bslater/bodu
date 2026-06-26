<#
.SYNOPSIS
    Downloads the official yaml-test-suite conformance corpus and prepares the vendored
    Bodu.Text.Yaml/test/YamlTestCorpus tree that drives the CorpusKat-based regression run.

.DESCRIPTION
    Fetches a released yaml-test-suite data branch (or tag) as a source archive, then vendors the
    files the Bodu YAML conformance harness consumes for each test case:

        name        (upstream '===' description)
        in.yaml     (the input document)
        in.json     (the canonical JSON expectation, when published)
        error       (empty marker present for error cases)
        test.event  (the expected event stream)
        out.yaml    (the normalized re-emitted document, when published)
        emit.yaml   (the expected emitter output, when published)

    A "case" is any directory that contains an 'in.yaml'; its identifier is its path relative to the
    corpus root (for example '229Q' or 'SM9W/00'). The upstream LICENSE is vendored alongside.

    The script never clobbers the human-reviewed classification: every existing case id keeps its
    category in classification.tsv. A newly added case is written with the category 'Unclassified' so
    the C# governance suite (and a maintainer) resolve it deliberately; cases that disappear upstream
    are removed and reported. The 'kind' column (valid / valid-nojson / invalid) is recomputed from the
    vendored files. Finally provenance.json is rewritten with the source, the released branch/tag, the
    resolved commit SHA, the license, and the recomputed counts.

    This tool does not run the parser, so it does not decide SupportedPass / SupportedFail /
    UnsupportedFeatureRejected for new cases. Classifying them is a separate C# step.

.PARAMETER Ref
    The released yaml-test-suite branch or tag to vendor. Upstream advises pinning a 'data-YYYY-MM-DD'
    release rather than the volatile 'data' branch. Defaults to the newest known release.

.PARAMETER Source
    The yaml-test-suite repository, owner/name form. Defaults to 'yaml/yaml-test-suite'.

.PARAMETER CorpusRoot
    The vendored corpus directory to (re)build. Defaults to the checked-in
    Bodu.Text.Yaml/test/YamlTestCorpus.

.EXAMPLE
    pwsh ./tools/Update-YamlTestCorpus.ps1

.EXAMPLE
    pwsh ./tools/Update-YamlTestCorpus.ps1 -Ref data-2022-01-17

.NOTES
    Requires PowerShell 7+ and the 'tar' utility (present on Windows 10+, macOS, and Linux).
    Exits non-zero when any case remains 'Unclassified' so the gap is not silently shipped.
#>
#Requires -Version 7
[CmdletBinding()]
param(
    [string]$Ref = 'data-2022-01-17',
    [string]$Source = 'yaml/yaml-test-suite',
    [string]$CorpusRoot = (Join-Path $PSScriptRoot '..' 'Bodu.Text.Yaml' 'test' 'YamlTestCorpus')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# ----------------------------------------------------------------------------------------------------
# Download and extract the source archive.
# ----------------------------------------------------------------------------------------------------

$archiveUrl = "https://github.com/$Source/archive/refs/heads/$Ref.tar.gz"
$work = Join-Path ([System.IO.Path]::GetTempPath()) ("yaml-test-suite-" + [System.Guid]::NewGuid().ToString('N'))
$tarball = Join-Path $work 'corpus.tar.gz'
$extractDir = Join-Path $work 'extract'
New-Item -ItemType Directory -Path $extractDir -Force | Out-Null

Write-Host "Fetching $archiveUrl"
try {
    Invoke-WebRequest -Uri $archiveUrl -OutFile $tarball -UseBasicParsing
}
catch {
    # Fall back to the tag ref namespace when the branch ref is unavailable.
    $archiveUrl = "https://github.com/$Source/archive/refs/tags/$Ref.tar.gz"
    Write-Host "Branch ref not found; retrying tag $archiveUrl"
    Invoke-WebRequest -Uri $archiveUrl -OutFile $tarball -UseBasicParsing
}

Write-Host "Extracting archive"
tar -xzf $tarball -C $extractDir
if ($LASTEXITCODE -ne 0) {
    throw "tar failed to extract $tarball (exit $LASTEXITCODE)."
}

# The archive expands to a single top-level directory; treat it as the data root.
$dataRoot = (Get-ChildItem -LiteralPath $extractDir -Directory | Select-Object -First 1).FullName
if (-not $dataRoot) {
    throw "The archive did not contain a top-level directory."
}

# Resolve the commit SHA for provenance (best effort).
$commit = $Ref
try {
    $api = "https://api.github.com/repos/$Source/commits/$Ref"
    $headers = @{ 'User-Agent' = 'Bodu-Update-YamlTestCorpus'; 'Accept' = 'application/vnd.github+json' }
    $commit = (Invoke-RestMethod -Uri $api -Headers $headers).sha
}
catch {
    Write-Warning "Could not resolve commit SHA for '$Ref'; recording the ref name instead."
}

# ----------------------------------------------------------------------------------------------------
# Read the existing classification so human-reviewed categories survive a re-vendor.
# ----------------------------------------------------------------------------------------------------

$manifestPath = Join-Path $CorpusRoot 'classification.tsv'
$existingCategory = @{}
if (Test-Path -LiteralPath $manifestPath) {
    foreach ($line in Get-Content -LiteralPath $manifestPath) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $parts = $line -split "`t"
        if ($parts.Count -ge 3) { $existingCategory[$parts[0]] = $parts[2] }
    }
    Write-Host "Loaded $($existingCategory.Count) existing classification rows."
}

# ----------------------------------------------------------------------------------------------------
# Walk every upstream case (a directory containing in.yaml) and vendor its files.
# ----------------------------------------------------------------------------------------------------

$vendorFiles = @('in.yaml', 'in.json', 'error', 'test.event', 'out.yaml', 'emit.yaml')
$rows = [System.Collections.Generic.List[object]]::new()
$upstreamIds = [System.Collections.Generic.HashSet[string]]::new()
$added = [System.Collections.Generic.List[string]]::new()

foreach ($input in Get-ChildItem -LiteralPath $dataRoot -Recurse -File -Filter 'in.yaml') {
    $caseDir = $input.Directory.FullName
    $id = [System.IO.Path]::GetRelativePath($dataRoot, $caseDir).Replace('\', '/')
    [void]$upstreamIds.Add($id)

    $destDir = Join-Path $CorpusRoot ($id -replace '/', [System.IO.Path]::DirectorySeparatorChar)
    New-Item -ItemType Directory -Path $destDir -Force | Out-Null

    # The description file is '===' upstream and 'name' in the vendored tree.
    $nameSource = Join-Path $caseDir '==='
    if (Test-Path -LiteralPath $nameSource) {
        Copy-Item -LiteralPath $nameSource -Destination (Join-Path $destDir 'name') -Force
    }

    foreach ($file in $vendorFiles) {
        $src = Join-Path $caseDir $file
        if (Test-Path -LiteralPath $src) {
            Copy-Item -LiteralPath $src -Destination (Join-Path $destDir $file) -Force
        }
    }

    # Recompute the kind column from the vendored files.
    $kind =
        if (Test-Path -LiteralPath (Join-Path $caseDir 'error')) { 'invalid' }
        elseif (Test-Path -LiteralPath (Join-Path $caseDir 'in.json')) { 'valid' }
        else { 'valid-nojson' }

    $category = if ($existingCategory.ContainsKey($id)) { $existingCategory[$id] } else { 'Unclassified' }
    if (-not $existingCategory.ContainsKey($id)) { $added.Add($id) }

    $rows.Add([pscustomobject]@{ Id = $id; Kind = $kind; Category = $category })
}

# Remove vendored cases that no longer exist upstream.
$removed = [System.Collections.Generic.List[string]]::new()
foreach ($id in $existingCategory.Keys) {
    if (-not $upstreamIds.Contains($id)) {
        $removed.Add($id)
        $stale = Join-Path $CorpusRoot ($id -replace '/', [System.IO.Path]::DirectorySeparatorChar)
        if (Test-Path -LiteralPath $stale) { Remove-Item -LiteralPath $stale -Recurse -Force }
    }
}

# Vendor the upstream license.
$licenseSource = Join-Path $dataRoot 'LICENSE'
if (Test-Path -LiteralPath $licenseSource) {
    Copy-Item -LiteralPath $licenseSource -Destination (Join-Path $CorpusRoot 'LICENSE') -Force
}

# ----------------------------------------------------------------------------------------------------
# Write the classification manifest (sorted by id for a stable, reproducible diff).
# ----------------------------------------------------------------------------------------------------

$sorted = $rows | Sort-Object -Property Id -Culture 'ordinal'
$manifestLines = $sorted | ForEach-Object { "$($_.Id)`t$($_.Kind)`t$($_.Category)" }
Set-Content -LiteralPath $manifestPath -Value $manifestLines -NoNewline:$false -Encoding utf8

# ----------------------------------------------------------------------------------------------------
# Rewrite provenance.json with the recomputed counts.
# ----------------------------------------------------------------------------------------------------

$categoryCounts = [ordered]@{}
foreach ($row in $sorted) {
    if (-not $categoryCounts.Contains($row.Category)) { $categoryCounts[$row.Category] = 0 }
    $categoryCounts[$row.Category]++
}

$validCount = ($sorted | Where-Object { $_.Kind -ne 'invalid' }).Count
$invalidCount = ($sorted | Where-Object { $_.Kind -eq 'invalid' }).Count

$provenance = [ordered]@{
    source          = "https://github.com/$Source"
    branch          = $Ref
    commit          = $commit
    license         = 'MIT'
    caseCount       = $sorted.Count
    validCaseCount  = $validCount
    invalidCaseCount = $invalidCount
    classification  = $categoryCounts
}

$provenancePath = Join-Path $CorpusRoot 'provenance.json'
($provenance | ConvertTo-Json -Depth 5) | Set-Content -LiteralPath $provenancePath -Encoding utf8

# ----------------------------------------------------------------------------------------------------
# Report and clean up.
# ----------------------------------------------------------------------------------------------------

Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Vendored $($sorted.Count) cases from $Source@$Ref (commit $commit)."
Write-Host "  added:        $($added.Count)"
Write-Host "  removed:      $($removed.Count)"
$unclassified = ($sorted | Where-Object { $_.Category -eq 'Unclassified' }).Count
Write-Host "  unclassified: $unclassified"

if ($added.Count -gt 0) { Write-Host "  new ids: $([string]::Join(', ', $added))" }
if ($removed.Count -gt 0) { Write-Host "  removed ids: $([string]::Join(', ', $removed))" }

if ($unclassified -gt 0) {
    Write-Warning "There are $unclassified unclassified case(s); classify them before committing."
    exit 1
}

Write-Host "Corpus is fully classified."
