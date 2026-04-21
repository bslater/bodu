# tools/

Maintenance scripts for the CRC subsystem. All scripts require **PowerShell 7+** and are
intended to be run from the repository root.

## Scripts

| Script | Purpose |
| --- | --- |
| `Fetch-CrcSpecs.ps1` | Downloads the CRC RevEng catalogue from `https://reveng.sourceforge.io/crc-catalogue/all.htm` and rebuilds `Bodu.Security.Cryptography/src/crc-specs.json`. Also writes `crc-specs.meta.json` with the source URL and fetch timestamp. Class, Created, Updated, Alias, and Codeword metadata is parsed from the page where possible, and otherwise carried over from the existing JSON so hand-curated fields survive a refresh. |
| `Generate-CrcCatalog.ps1` | Regenerates **two** C# files from the JSON: `CrcStandards.cs` (public `enum CrcStandards` with one entry per canonical standard) and `CrcStandard.Catalog.cs` (the packed `CatalogEntry[]` data table plus `Get(CrcStandards)`, `FromName(string)`, `TryFromName`, and lazy `All`). Entries are materialised on first access and memoised. |
| `Generate-CrcCatalogTests.ps1` | Regenerates `Bodu.Security.Cryptography/test/Security.Cryptography/CrcTests.Catalog.cs`, a data-driven `[DataTestMethod]` that asserts every catalogue entry's CRC of ASCII `"123456789"` matches the RevEng-published `check` value. Test data rows carry the `CrcStandards` enum value; the test materialises via `CrcStandard.Get(standardId)`. |
| `Generate-CrcDocs.ps1` | Regenerates `docs/guides/cryptography/crc-catalogue.md`, the public-facing attribution and catalogue page. |

## Standard workflow

```pwsh
# 1. Refresh the JSON from upstream
pwsh ./tools/Fetch-CrcSpecs.ps1

# 2. Rebuild the generated C# files and documentation
pwsh ./tools/Generate-CrcCatalog.ps1         # CrcStandards.cs + CrcStandard.Catalog.cs
pwsh ./tools/Generate-CrcCatalogTests.ps1    # CrcTests.Catalog.cs
pwsh ./tools/Generate-CrcDocs.ps1            # docs/guides/cryptography/crc-catalogue.md

# 3. Build and run tests
dotnet build Bodu.sln
dotnet test Bodu.Security.Cryptography/test/Bodu.Security.Cryptography.Test.csproj --settings test.runsettings
```

## Filtering rule

The generators share a single filter:

- **`-MaxSize <bits>`** — upper bound on CRC width. Entries whose `size` exceeds this value are skipped entirely: no enum member, no catalogue row, no name-lookup entry, no test row. The generated documentation page lists them in a separate "not supported" section for transparency. Defaults to `64` (the widest width representable as a `ulong`, matching `CrcStandard.MaxSize`).

Currently only `CRC-82/DARC` is excluded by the default limit.

## Common standards

A short list of commonly-used CRCs is exposed as **hand-maintained** `public static CrcStandard` properties on `CrcStandard` (in `CrcStandard.cs`). They delegate to `Get(CrcStandards.X)` so they share the lazy cache with the rest of the catalogue — they're purely a source-level convenience and add no storage cost beyond the shared cache slot.

Currently exposed:

- `CrcStandard.CRC8_SMBUS` (`CRC-8`)
- `CrcStandard.CRC8_MAXIMDOW` (`DOW-CRC`)
- `CrcStandard.CRC16_ARC` (`CRC-16`)
- `CrcStandard.CRC16_IBM3740` (`CRC-16/CCITT-FALSE`)
- `CrcStandard.CRC16_KERMIT`
- `CrcStandard.CRC16_MODBUS`
- `CrcStandard.CRC16_XMODEM`
- `CrcStandard.CRC32_ISOHDLC` — the default used by `new Crc()`
- `CrcStandard.CRC32_ISCSI` (`CRC-32C` / Castagnoli)
- `CrcStandard.CRC32_BZIP2`
- `CrcStandard.CRC64_ECMA182`
- `CrcStandard.CRC64_XZ`

Keep the list in `CrcStandard.cs` and the matching `$Common` set in `tools/Generate-CrcDocs.ps1` in sync.

Anything outside this list is still fully accessible through `CrcStandard.Get(CrcStandards.X)` or `CrcStandard.FromName("CRC-X/Y")`.
