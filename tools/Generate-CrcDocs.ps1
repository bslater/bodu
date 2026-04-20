<#
.SYNOPSIS
    Generates the CRC catalogue documentation page at docs/guides/cryptography/crc-catalogue.md.

.DESCRIPTION
    Renders a Markdown reference page that lists every CRC standard in crc-specs.json, grouped by
    support status (supported canonicals, their aliases, and any skipped-by-width entries). The
    page credits the CRC RevEng project, records the last export timestamp from
    crc-specs.meta.json, and stamps its own regeneration time.

.PARAMETER SpecsPath
    Path to the crc-specs.json input file.

.PARAMETER MetaPath
    Path to the sidecar crc-specs.meta.json file written by Fetch-CrcSpecs.ps1.

.PARAMETER OutputPath
    Path of the markdown file to write.

.PARAMETER Exclude
    Canonical CRC names whose C# field declarations live outside the generated catalogue.

.PARAMETER MaxSize
    Maximum CRC width (in bits). Entries above this are marked as skipped in the documentation.

.EXAMPLE
    pwsh ./tools/Generate-CrcDocs.ps1

.NOTES
    Requires PowerShell 7+.
#>
#Requires -Version 7
[CmdletBinding()]
param(
    [string]$SpecsPath = (Join-Path $PSScriptRoot '..' 'Bodu.Security.Cryptography' 'src' 'crc-specs.json'),
    [string]$MetaPath  = (Join-Path $PSScriptRoot '..' 'Bodu.Security.Cryptography' 'src' 'crc-specs.meta.json'),
    [string]$OutputPath = (Join-Path $PSScriptRoot '..' 'docs' 'guides' 'cryptography' 'crc-catalogue.md'),
    [string[]]$Exclude = @('CRC-32/ISO-HDLC'),
    [int]$MaxSize = 64
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function ConvertTo-ConstantName {
    param([string]$Name)
    $s = $Name -replace '/', '_'
    $s = $s -replace '[^A-Za-z0-9_]', ''
    return $s.ToUpperInvariant()
}

function Format-HexDoc {
    param([string]$Value)
    if (-not $Value) { return '' }
    $hex = ($Value -replace '^0[xX]', '').ToUpperInvariant()
    return "0x$hex"
}

function Format-AnchorSlug {
    param([string]$Name)
    return 'crc.cat.' + ($Name -replace '/', '-').ToLowerInvariant()
}

$specs = Get-Content -LiteralPath $SpecsPath -Raw | ConvertFrom-Json
$excludeSet = [System.Collections.Generic.HashSet[string]]::new([string[]]$Exclude, [System.StringComparer]::Ordinal)

$source = 'https://reveng.sourceforge.io/crc-catalogue/all.htm'
$fetchedUtc = ''
$entryCount = $specs.Count
if (Test-Path -LiteralPath $MetaPath) {
    try {
        $meta = Get-Content -LiteralPath $MetaPath -Raw | ConvertFrom-Json
        if ($meta.PSObject.Properties['source'])     { $source     = [string]$meta.source }
        if ($meta.PSObject.Properties['fetchedUtc']) { $fetchedUtc = [string]$meta.fetchedUtc }
        if ($meta.PSObject.Properties['entryCount']) { $entryCount = [int]$meta.entryCount }
    }
    catch {
        Write-Warning "Could not read metadata at $MetaPath; using defaults."
    }
}
$regeneratedUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')

$lines = [System.Collections.Generic.List[string]]::new()
[void]$lines.Add('---')
[void]$lines.Add('title: CRC catalogue')
[void]$lines.Add('---')
[void]$lines.Add('')
[void]$lines.Add('# CRC catalogue')
[void]$lines.Add('')
[void]$lines.Add('The <xref:Bodu.Security.Cryptography.CrcStandard> type exposes a broad catalogue of named CRC parameter')
[void]$lines.Add('sets that can be passed to <xref:Bodu.Security.Cryptography.Crc> for CRC computation. The catalogue is')
[void]$lines.Add('mechanically derived from the **CRC RevEng** project.')
[void]$lines.Add('')
[void]$lines.Add('## Attribution')
[void]$lines.Add('')
[void]$lines.Add("The CRC parameter sets in this catalogue are sourced from **Greg Cook's CRC RevEng Catalogue of parametrised CRC algorithms** at [$source]($source).")
[void]$lines.Add('')
[void]$lines.Add('The catalogue is distributed as part of the CRC RevEng project at <https://reveng.sourceforge.io/>. Please')
[void]$lines.Add('consult the upstream page for the authoritative parameter definitions, alias history, and licence terms')
[void]$lines.Add('that apply to the underlying data.')
[void]$lines.Add('')
if ($fetchedUtc) {
    [void]$lines.Add("- **Catalogue last fetched (UTC):** $fetchedUtc")
}
[void]$lines.Add("- **This page regenerated (UTC):** $regeneratedUtc")
[void]$lines.Add("- **Entries in source:** $entryCount")
[void]$lines.Add('')
[void]$lines.Add('## Support policy')
[void]$lines.Add('')
[void]$lines.Add("`CrcStandard` represents all scalar parameters as <xref:System.UInt64>, so the library can materialise any CRC of width 1–64 bits. Entries whose width exceeds 64 bits are listed below for completeness but are **not** emitted as catalogue constants and cannot be constructed through `CrcStandard`.")
[void]$lines.Add('')
[void]$lines.Add('Aliases share a single catalogue instance with their canonical standard. `CrcStandard.FromName` resolves both canonical and alias names, so `FromName("CRC-32")` and `FromName("CRC-32/ISO-HDLC")` return the same instance.')
[void]$lines.Add('')

# Supported standards table
$supported = @($specs | Where-Object { $_.size -le $MaxSize })
$skipped   = @($specs | Where-Object { $_.size -gt $MaxSize })

[void]$lines.Add('## Supported standards')
[void]$lines.Add('')
[void]$lines.Add('| Name | Width | Class | Constant | Aliases | RevEng |')
[void]$lines.Add('|---|---:|---|---|---|---|')
foreach ($spec in $supported) {
    $c = ConvertTo-ConstantName $spec.name
    $cls = if ($spec.PSObject.Properties['class']) { [string]$spec.class } else { '' }
    $aliases = if ($spec.PSObject.Properties['aliases'] -and $spec.aliases) { @($spec.aliases) } else { @() }
    $aliasCell = if ($aliases.Count -eq 0) {
        '—'
    }
    else {
        ($aliases | ForEach-Object { '`' + $_ + '`' }) -join ', '
    }
    $anchor = Format-AnchorSlug $spec.name
    $refLink = "[spec]($source#$anchor)"
    $nameCell = if ($excludeSet.Contains($spec.name)) { "**$($spec.name)** (core)" } else { $spec.name }
    [void]$lines.Add("| $nameCell | $($spec.size) | $cls | ``CrcStandard.$c`` | $aliasCell | $refLink |")
}
[void]$lines.Add('')

if ($skipped.Count -gt 0) {
    [void]$lines.Add('## Not supported (width exceeds 64 bits)')
    [void]$lines.Add('')
    [void]$lines.Add('The following standards are listed in the source catalogue but are **not** exposed by `CrcStandard` because their width exceeds the 64-bit scalar representation used by this library.')
    [void]$lines.Add('')
    [void]$lines.Add('| Name | Width | Class | RevEng |')
    [void]$lines.Add('|---|---:|---|---|')
    foreach ($spec in $skipped) {
        $cls = if ($spec.PSObject.Properties['class']) { [string]$spec.class } else { '' }
        $anchor = Format-AnchorSlug $spec.name
        $refLink = "[spec]($source#$anchor)"
        [void]$lines.Add("| $($spec.name) | $($spec.size) | $cls | $refLink |")
    }
    [void]$lines.Add('')
}

[void]$lines.Add('## Regeneration')
[void]$lines.Add('')
[void]$lines.Add('This page is produced by `tools/Generate-CrcDocs.ps1`. To refresh the data from upstream:')
[void]$lines.Add('')
[void]$lines.Add('```pwsh')
[void]$lines.Add('pwsh ./tools/Fetch-CrcSpecs.ps1')
[void]$lines.Add('pwsh ./tools/Generate-CrcCatalog.ps1')
[void]$lines.Add('pwsh ./tools/Generate-CrcCatalogTests.ps1')
[void]$lines.Add('pwsh ./tools/Generate-CrcDocs.ps1')
[void]$lines.Add('```')
[void]$lines.Add('')

Set-Content -LiteralPath $OutputPath -Value ($lines -join "`n") -Encoding utf8
Write-Host "Wrote $OutputPath"
Write-Host "  Supported listed: $($supported.Count)"
Write-Host "  Oversize listed:  $($skipped.Count)"
