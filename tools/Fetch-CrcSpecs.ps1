<#
.SYNOPSIS
    Fetches the CRC RevEng catalogue and rebuilds Bodu.Security.Cryptography/src/crc-specs.json.

.DESCRIPTION
    Downloads the public CRC catalogue page (reveng.sourceforge.io/crc-catalogue/all.htm by default),
    parses every '<dfn>width=... name="..."</dfn>' parameter block, and writes the result as an
    ordered JSON array. Informational fields that do not appear in the parameter line (class,
    created, updated, aliases, codewords) are preserved from the existing JSON when present, so
    hand-curated metadata is not silently lost by a refresh.

.PARAMETER Source
    URL of the RevEng all-entries catalogue page.

.PARAMETER OutputPath
    Path of the JSON file to write. Defaults to the checked-in crc-specs.json.

.EXAMPLE
    pwsh ./tools/Fetch-CrcSpecs.ps1

.NOTES
    Requires PowerShell 7+. Run from the repository root or from the tools directory.
#>
#Requires -Version 7
[CmdletBinding()]
param(
    [string]$Source = 'https://reveng.sourceforge.io/crc-catalogue/all.htm',
    [string]$OutputPath = (Join-Path $PSScriptRoot '..' 'Bodu.Security.Cryptography' 'src' 'crc-specs.json')
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

# Match the RevEng single-line parameter form:
#     width=3  poly=0x3  init=0x0  refin=true  refout=true  xorout=0x7  check=0x4  residue=0x2  name="CRC-3/GSM"
$paramRegex = [regex]@'
width=(?<size>\d+)\s+poly=(?<poly>0[xX][0-9a-fA-F]+)\s+init=(?<init>0[xX][0-9a-fA-F]+)\s+refin=(?<refin>true|false)\s+refout=(?<refout>true|false)\s+xorout=(?<xorout>0[xX][0-9a-fA-F]+)\s+check=(?<check>0[xX][0-9a-fA-F]+)\s+residue=(?<residue>0[xX][0-9a-fA-F]+)\s+name="(?<name>[^"]+)"
'@

$matches = $paramRegex.Matches($html)
Write-Host "Parsed $($matches.Count) CRC definitions."
if ($matches.Count -eq 0) {
    throw "No CRC entries matched. The RevEng page layout may have changed; inspect $Source and adjust the parser."
}

function Format-Hex {
    param([string]$Value)
    # Normalise '0xabc' -> '0XABC' to match the existing file style.
    return ('0X' + ($Value -replace '^0[xX]', '')).ToUpperInvariant()
}

$entries = foreach ($m in $matches) {
    $name = $m.Groups['name'].Value
    $prior = if ($existing.ContainsKey($name)) { $existing[$name] } else { $null }

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
        class        = if ($prior -and $prior.PSObject.Properties['class']) { [string]$prior.class } else { '' }
        created      = if ($prior -and $prior.PSObject.Properties['created']) { [string]$prior.created } else { '' }
        updated      = if ($prior -and $prior.PSObject.Properties['updated']) { [string]$prior.updated } else { '' }
        aliases      = if ($prior -and $prior.PSObject.Properties['aliases'] -and $prior.aliases) { @($prior.aliases) } else { @() }
        codewords    = if ($prior -and $prior.PSObject.Properties['codewords'] -and $prior.codewords) { @($prior.codewords) } else { @() }
    }
}

$json = $entries | ConvertTo-Json -Depth 5
# ConvertTo-Json on PS7 writes CRLF-free single-space-indented output; that is fine.
Set-Content -LiteralPath $OutputPath -Value $json -Encoding utf8
Write-Host "Wrote $OutputPath ($($entries.Count) entries)."
