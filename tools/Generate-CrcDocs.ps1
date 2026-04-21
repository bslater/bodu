<#
.SYNOPSIS
    Generates the CRC catalogue documentation page at docs/guides/cryptography/crc-catalogue.md.

.DESCRIPTION
    Renders a Markdown reference page that lists every CRC standard in crc-specs.json. The page
    credits the CRC RevEng project, records the last export timestamp from
    crc-specs.meta.json, stamps its own regeneration time, and describes how to access the
    catalogue via the common strongly-typed properties, the `CrcStandards` enum (via
    `CrcStandard.Get`), or by name (via `CrcStandard.FromName`).

.PARAMETER SpecsPath
    Path to the crc-specs.json input file.

.PARAMETER MetaPath
    Path to the sidecar crc-specs.meta.json file written by Fetch-CrcSpecs.ps1.

.PARAMETER OutputPath
    Path of the markdown file to write.

.PARAMETER MaxSize
    Maximum CRC width (in bits). Entries above this are listed as unsupported.

.EXAMPLE
    pwsh ./tools/Generate-CrcDocs.ps1

.NOTES
    Requires PowerShell 7+.
#>
#Requires -Version 7
[CmdletBinding()]
param(
    [string]$SpecsPath  = (Join-Path $PSScriptRoot '..' 'Bodu.IO' 'src' 'crc-specs.json'),
    [string]$MetaPath   = (Join-Path $PSScriptRoot '..' 'Bodu.IO' 'src' 'crc-specs.meta.json'),
    [string]$OutputPath = (Join-Path $PSScriptRoot '..' 'docs' 'guides' 'cryptography' 'crc-catalogue.md'),
    [int]$MaxSize = 64
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Canonical catalogue names for the 12 CRC standards exposed as strongly-typed properties on
# CrcStandard. Keep in sync with CrcStandard.cs.
$Common = [System.Collections.Generic.HashSet[string]]::new([string[]]@(
    'CRC-8/SMBUS', 'CRC-8/MAXIM-DOW',
    'CRC-16/ARC', 'CRC-16/IBM-3740', 'CRC-16/KERMIT', 'CRC-16/MODBUS', 'CRC-16/XMODEM',
    'CRC-32/ISO-HDLC', 'CRC-32/ISCSI', 'CRC-32/BZIP2',
    'CRC-64/ECMA-182', 'CRC-64/XZ'
), [System.StringComparer]::Ordinal)

function ConvertTo-ConstantName {
    param([string]$Name)
    $s = $Name -replace '/', '_'
    $s = $s -replace '[^A-Za-z0-9_]', ''
    return $s.ToUpperInvariant()
}

function Format-AnchorSlug {
    param([string]$Name)
    return 'crc.cat.' + ($Name -replace '/', '-').ToLowerInvariant()
}

$specs = Get-Content -LiteralPath $SpecsPath -Raw | ConvertFrom-Json

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

$supported = @($specs | Where-Object { $_.size -le $MaxSize })
$skipped   = @($specs | Where-Object { $_.size -gt $MaxSize })

$lines = [System.Collections.Generic.List[string]]::new()
[void]$lines.Add('---')
[void]$lines.Add('title: CRC catalogue')
[void]$lines.Add('---')
[void]$lines.Add('')
[void]$lines.Add('# CRC catalogue')
[void]$lines.Add('')
[void]$lines.Add('The <xref:Bodu.Security.Cryptography.CrcStandard> type exposes a broad catalogue of named CRC parameter sets that can be passed to <xref:Bodu.Security.Cryptography.Crc> for CRC computation. The catalogue is mechanically derived from the **CRC RevEng** project.')
[void]$lines.Add('')
[void]$lines.Add('## Attribution')
[void]$lines.Add('')
[void]$lines.Add("The CRC parameter sets in this catalogue are sourced from **Greg Cook's CRC RevEng Catalogue of parametrised CRC algorithms** at [$source]($source).")
[void]$lines.Add('')
[void]$lines.Add('The catalogue is distributed as part of the CRC RevEng project at <https://reveng.sourceforge.io/>. Please consult the upstream page for the authoritative parameter definitions, alias history, and licence terms that apply to the underlying data.')
[void]$lines.Add('')
if ($fetchedUtc) {
    [void]$lines.Add("- **Catalogue last fetched (UTC):** $fetchedUtc")
}
[void]$lines.Add("- **This page regenerated (UTC):** $regeneratedUtc")
[void]$lines.Add("- **Entries in source:** $entryCount")
[void]$lines.Add('')
[void]$lines.Add('## Accessing standards')
[void]$lines.Add('')
[void]$lines.Add('The catalogue is a **lazy-materialised data table**. Loading <xref:Bodu.Security.Cryptography.CrcStandard> allocates only the packed spec rows and the per-entry cache slots — individual <xref:Bodu.Security.Cryptography.CrcStandard> instances are constructed on first access and then memoised, so a process that uses only a handful of standards pays for only a handful of allocations.')
[void]$lines.Add('')
[void]$lines.Add('Three entry points:')
[void]$lines.Add('')
[void]$lines.Add('```csharp')
[void]$lines.Add('// 1. Strongly-typed common standards — most convenient for the usual suspects.')
[void]$lines.Add('using var crc = new Crc(CrcStandard.CRC32_ISOHDLC);')
[void]$lines.Add('using var crc = new Crc(CrcStandard.CRC32_ISCSI);          // iSCSI / Castagnoli')
[void]$lines.Add('using var crc = new Crc(CrcStandard.CRC16_MODBUS);')
[void]$lines.Add('')
[void]$lines.Add('// 2. By enum — covers every canonical catalogue entry (112 in total).')
[void]$lines.Add('using var crc = new Crc(CrcStandard.Get(CrcStandards.CRC8_SAEJ1850));')
[void]$lines.Add('')
[void]$lines.Add('// 3. By name — resolves canonical names AND published aliases.')
[void]$lines.Add('using var crc1 = new Crc(CrcStandard.FromName("CRC-32/ISO-HDLC"));')
[void]$lines.Add('using var crc2 = new Crc(CrcStandard.FromName("PKZIP"));   // same instance as crc1')
[void]$lines.Add('')
[void]$lines.Add('// Iterate every catalogue standard')
[void]$lines.Add('foreach (CrcStandard std in CrcStandard.All) { ... }')
[void]$lines.Add('```')
[void]$lines.Add('')
[void]$lines.Add('`FromName` is ordinal and case-sensitive. `TryFromName` returns `false` rather than throwing when a name is unknown.')
[void]$lines.Add('')
[void]$lines.Add('## Support policy')
[void]$lines.Add('')
[void]$lines.Add('`CrcStandard` represents all scalar parameters as <xref:System.UInt64>, so the library can materialise any CRC of width 1–64 bits. Entries whose width exceeds 64 bits are listed below for completeness but are **not** exposed by <xref:Bodu.Security.Cryptography.CrcStandards> and cannot be constructed through `CrcStandard`.')
[void]$lines.Add('')
[void]$lines.Add('Aliases share a single catalogue instance with their canonical standard. `CrcStandard.FromName` resolves both canonical and alias names, so `FromName("CRC-32")` and `FromName("CRC-32/ISO-HDLC")` return the same instance.')
[void]$lines.Add('')

[void]$lines.Add('## Common standards (strongly-typed)')
[void]$lines.Add('')
[void]$lines.Add('These are exposed as `public static CrcStandard` properties on <xref:Bodu.Security.Cryptography.CrcStandard> for convenience — the underlying cache is still shared with the enum-based lookup.')
[void]$lines.Add('')
[void]$lines.Add('| Name | Width | Property | Aliases |')
[void]$lines.Add('|---|---:|---|---|')
foreach ($spec in $supported) {
    if (-not $Common.Contains($spec.name)) { continue }
    $c = ConvertTo-ConstantName $spec.name
    $aliases = if ($spec.PSObject.Properties['aliases'] -and $spec.aliases) { @($spec.aliases) } else { @() }
    $aliasCell = if ($aliases.Count -eq 0) { '—' } else { ($aliases | ForEach-Object { '`' + $_ + '`' }) -join ', ' }
    [void]$lines.Add("| $($spec.name) | $($spec.size) | ``CrcStandard.$c`` | $aliasCell |")
}
[void]$lines.Add('')

[void]$lines.Add('## Full catalogue')
[void]$lines.Add('')
[void]$lines.Add('Access the following via `CrcStandard.Get(CrcStandards.X)` or `CrcStandard.FromName("name")`.')
[void]$lines.Add('')
[void]$lines.Add('| Name | Width | Class | Enum | Aliases | RevEng |')
[void]$lines.Add('|---|---:|---|---|---|---|')
foreach ($spec in $supported) {
    $c = ConvertTo-ConstantName $spec.name
    $cls = if ($spec.PSObject.Properties['class']) { [string]$spec.class } else { '' }
    $aliases = if ($spec.PSObject.Properties['aliases'] -and $spec.aliases) { @($spec.aliases) } else { @() }
    $aliasCell = if ($aliases.Count -eq 0) { '—' } else { ($aliases | ForEach-Object { '`' + $_ + '`' }) -join ', ' }
    $anchor = Format-AnchorSlug $spec.name
    $refLink = "[spec]($source#$anchor)"
    $nameCell = if ($Common.Contains($spec.name)) { "**$($spec.name)**" } else { $spec.name }
    [void]$lines.Add("| $nameCell | $($spec.size) | $cls | ``CrcStandards.$c`` | $aliasCell | $refLink |")
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
[void]$lines.Add('This page and the generated C# sources are produced by the scripts in `tools/`. To refresh the data from upstream:')
[void]$lines.Add('')
[void]$lines.Add('```pwsh')
[void]$lines.Add('pwsh ./tools/Fetch-CrcSpecs.ps1')
[void]$lines.Add('pwsh ./tools/Generate-CrcCatalog.ps1        # regenerates CrcStandards.cs and CrcStandard.Catalog.cs')
[void]$lines.Add('pwsh ./tools/Generate-CrcCatalogTests.ps1   # regenerates CrcTests.Catalog.cs')
[void]$lines.Add('pwsh ./tools/Generate-CrcDocs.ps1           # regenerates this page')
[void]$lines.Add('```')
[void]$lines.Add('')

Set-Content -LiteralPath $OutputPath -Value ($lines -join "`n") -Encoding utf8
Write-Host "Wrote $OutputPath"
Write-Host "  Supported listed: $($supported.Count)"
Write-Host "  Common:           $(($supported | Where-Object { $Common.Contains($_.name) }).Count)"
Write-Host "  Oversize listed:  $($skipped.Count)"
