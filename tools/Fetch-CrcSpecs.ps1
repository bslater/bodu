<#
.SYNOPSIS
    Fetches the CRC RevEng catalogue and rebuilds Bodu.IO.Hashing/src/crc-specs.json.

.DESCRIPTION
    Downloads the public CRC catalogue page (reveng.sourceforge.io/crc-catalogue/all.htm by
    default), parses every '<dfn>width=... name="..."</dfn>' parameter block, and writes the
    result as an ordered JSON array. Each entry's metadata (Class:, Alias:, Codeword:, Created:,
    Updated:) is read from the entry's text with its markup removed, so that a value wrapped in a
    link or a code span is not read as empty, and an Alias: line may list several comma-separated
    names. Hand-curated metadata is preserved from the existing JSON when the live page cannot
    supply it.

    Also writes a sidecar file `crc-specs.meta.json` containing the source URL and a UTC
    timestamp of this fetch, which downstream generators reuse when they emit attribution in
    the generated documentation page.

.PARAMETER Source
    URL of the RevEng all-entries catalogue page.

.PARAMETER OutputPath
    Path of the JSON file to write. Defaults to the checked-in crc-specs.json.

.PARAMETER MetaPath
    Path of the sidecar metadata file. Defaults to crc-specs.meta.json beside the JSON.

.EXAMPLE
    pwsh ./tools/Fetch-CrcSpecs.ps1

.NOTES
    Requires PowerShell 7+.
#>
#Requires -Version 7
[CmdletBinding()]
param(
    [string]$Source = 'https://reveng.sourceforge.io/crc-catalogue/all.htm',
    [string]$OutputPath = (Join-Path $PSScriptRoot '..' 'Bodu.IO.Hashing' 'src' 'crc-specs.json'),
    [string]$MetaPath   = (Join-Path $PSScriptRoot '..' 'Bodu.IO.Hashing' 'src' 'crc-specs.meta.json')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

Write-Host "Fetching $Source"
$response = Invoke-WebRequest -Uri $Source -UseBasicParsing
$html = $response.Content

# Preserve hand-curated metadata from any existing JSON.
$existing = @{}
if (Test-Path -LiteralPath $OutputPath) {
    try {
        $existingJson = Get-Content -LiteralPath $OutputPath -Raw | ConvertFrom-Json
        foreach ($entry in $existingJson) {
            $existing[$entry.name] = $entry
        }
        Write-Host "Loaded $($existing.Count) existing entries for metadata carry-over."
    }
    catch {
        Write-Warning "Could not parse existing JSON at $OutputPath; continuing without metadata carry-over."
    }
}

$paramRegex = [regex]@'
width=(?<size>\d+)\s+poly=(?<poly>0[xX][0-9a-fA-F]+)\s+init=(?<init>0[xX][0-9a-fA-F]+)\s+refin=(?<refin>true|false)\s+refout=(?<refout>true|false)\s+xorout=(?<xorout>0[xX][0-9a-fA-F]+)\s+check=(?<check>0[xX][0-9a-fA-F]+)\s+residue=(?<residue>0[xX][0-9a-fA-F]+)\s+name="(?<name>[^"]+)"
'@

$matches = $paramRegex.Matches($html)
Write-Host "Parsed $($matches.Count) CRC definitions."
if ($matches.Count -eq 0) {
    throw "No CRC entries matched. The RevEng page layout may have changed; inspect $Source and adjust the parser."
}

# Attempt to extract per-entry metadata (Class:, Created:, Updated:, Alias:, Codeword:) from the
# surrounding markup. Each CRC block is typically delimited by the next anchored <h3> element.
function Get-EntryBlock {
    param(
        [string]$Html,
        [int]$StartIndex,
        [int]$LimitIndex
    )
    # Find the end of this entry by looking for the next <h3 ... id="crc.cat..."> heading, and never
    # read past the next entry's parameter block, so one entry cannot collect the next one's labels.
    $endRegex = [regex]'<h3[^>]*id="crc\.cat[^"]*"'
    $nextHeading = $endRegex.Match($Html, $StartIndex + 1)
    $endIndex = if ($nextHeading.Success -and $nextHeading.Index -lt $LimitIndex) { $nextHeading.Index } else { $LimitIndex }
    return $Html.Substring($StartIndex, $endIndex - $StartIndex)
}

function ConvertTo-PlainText {
    param([string]$Html)
    # Block-level tags become line breaks so that each label keeps its own line, every other tag
    # is dropped so that a marked-up value reads as plain text, and character references are decoded.
    $text = $Html -replace '(?i)<\s*/?\s*(?:p|br|li|ul|ol|dl|dt|dd|div|tr|td|th|table|h[1-6])\b[^>]*>', "`n"
    $text = $text -replace '<[^>]*>', ''
    return [System.Net.WebUtility]::HtmlDecode($text)
}

function Get-LabelledValue {
    param(
        [string]$Block,
        [string]$Label
    )
    # Matches forms like "Class: academic" or "Created: 30 March 2005" within the block,
    # with or without surrounding HTML tags.
    $rx = [regex]("(?is)\b" + [regex]::Escape($Label) + "\s*[:：]\s*(?<v>[^<\n\r;]*)")
    $m = $rx.Match($Block)
    if ($m.Success) { return $m.Groups['v'].Value.Trim() } else { return '' }
}

function Get-LabelledList {
    param(
        [string]$Block,
        [string]$Label
    )
    # Collects every "<Label>: <value>" line in the block (so multiple "Alias: X" / "Codeword: Y"
    # entries are all captured). Returns a [string[]] and uses the comma trick to prevent
    # PowerShell from unwrapping an empty array to $null on function return.
    $rx = [regex]("(?im)\b" + [regex]::Escape($Label) + "\s*[:：]\s*(?<v>[^<\n\r;]+)")
    $results = foreach ($m in $rx.Matches($Block)) {
        $m.Groups['v'].Value.Trim()
    }
    $filtered = @($results | Where-Object { $_ })
    return ,[string[]]$filtered
}

function Format-Hex {
    param([string]$Value)
    return ('0X' + ($Value -replace '^0[xX]', '')).ToUpperInvariant()
}

$entries = for ($i = 0; $i -lt $matches.Count; $i++) {
    $m = $matches[$i]
    $name = $m.Groups['name'].Value
    $prior = if ($existing.ContainsKey($name)) { $existing[$name] } else { $null }

    $limit = if ($i + 1 -lt $matches.Count) { $matches[$i + 1].Index } else { $html.Length }
    $block = ConvertTo-PlainText (Get-EntryBlock -Html $html -StartIndex $m.Index -LimitIndex $limit)
    $class = Get-LabelledValue -Block $block -Label 'Class'
    $created = Get-LabelledValue -Block $block -Label 'Created'
    $updated = Get-LabelledValue -Block $block -Label 'Updated'
    $aliases = [string[]]@(Get-LabelledList -Block $block -Label 'Alias' | ForEach-Object { $_ -split '\s*,\s*' } | Where-Object { $_ })
    $codewords = Get-LabelledList -Block $block -Label 'Codeword'

    if (-not $class   -and $prior -and $prior.PSObject.Properties['class'])   { $class   = [string]$prior.class }
    if (-not $created -and $prior -and $prior.PSObject.Properties['created']) { $created = [string]$prior.created }
    if (-not $updated -and $prior -and $prior.PSObject.Properties['updated']) { $updated = [string]$prior.updated }
    if ((($null -eq $aliases) -or ($aliases.Count -eq 0)) -and $prior -and $prior.PSObject.Properties['aliases'] -and $prior.aliases) {
        $aliases = [string[]]@($prior.aliases)
    }
    if ((($null -eq $codewords) -or ($codewords.Count -eq 0)) -and $prior -and $prior.PSObject.Properties['codewords'] -and $prior.codewords) {
        $codewords = [string[]]@($prior.codewords)
    }

    # Normalise to empty [string[]] so the emitted JSON contains `[]` (not `null` or `[null]`).
    if ($null -eq $aliases)   { $aliases   = [string[]]@() }
    if ($null -eq $codewords) { $codewords = [string[]]@() }

    [ordered]@{
        name         = $name
        size         = [int]$m.Groups['size'].Value
        polynomial   = Format-Hex $m.Groups['poly'].Value
        initialValue = Format-Hex $m.Groups['init'].Value
        reflectIn    = [bool]::Parse($m.Groups['refin'].Value)
        reflectOut   = [bool]::Parse($m.Groups['refout'].Value)
        xorOut       = Format-Hex $m.Groups['xorout'].Value
        check        = Format-Hex $m.Groups['check'].Value
        residue      = Format-Hex $m.Groups['residue'].Value
        class        = $class
        created      = $created
        updated      = $updated
        aliases      = @($aliases)
        codewords    = @($codewords)
    }
}

$json = $entries | ConvertTo-Json -Depth 5
Set-Content -LiteralPath $OutputPath -Value $json -Encoding utf8
Write-Host "Wrote $OutputPath ($($entries.Count) entries)."

# Write sidecar metadata file.
$meta = [ordered]@{
    source     = $Source
    fetchedUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    entryCount = $entries.Count
}
$metaJson = $meta | ConvertTo-Json -Depth 3
Set-Content -LiteralPath $MetaPath -Value $metaJson -Encoding utf8
Write-Host "Wrote $MetaPath"
