# tools/

Maintenance scripts for the library. CRC scripts are PowerShell; cipher-vector
scripts are Python. All scripts are intended to be run from the repository root.

## CRC subsystem (PowerShell 7+)

### Scripts

| Script | Purpose |
| --- | --- |
| `Fetch-CrcSpecs.ps1` | Downloads the CRC RevEng catalogue from `https://reveng.sourceforge.io/crc-catalogue/all.htm` and rebuilds `Bodu.Security.Cryptography/src/crc-specs.json`. Also writes `crc-specs.meta.json` with the source URL and fetch timestamp. Class, Created, Updated, Alias, and Codeword metadata is parsed from the page where possible, and otherwise carried over from the existing JSON so hand-curated fields survive a refresh. |
| `Generate-CrcCatalog.ps1` | Regenerates **two** C# files from the JSON: `CrcStandards.cs` (public `enum CrcStandards` with one entry per canonical standard) and `CrcStandard.Catalog.cs` (the packed `CatalogEntry[]` data table plus `Get(CrcStandards)`, `FromName(string)`, `TryFromName`, and lazy `All`). Entries are materialised on first access and memoized. |
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

## Cipher vector cross-validation (Python 3.8+)

| Path | Purpose |
| --- | --- |
| `cipher-vectors/wide_serpent.py` | Independent Python port of the wide-block tweakable Serpent variants (Serpent-256, Serpent-512, Serpent-1024). The wide-block constructions are non-standard and have no published reference vectors, so the C# KAT files (`Serpent256KnownAnswers.cs`, `Serpent512KnownAnswers.cs`, `Serpent1024KnownAnswers.cs`) pin ciphertexts that have been confirmed by this second implementation. Running the script prints the same vector rows the KAT files reference. |

```bash
python3 tools/cipher-vectors/wide_serpent.py
```

If the script's output ever diverges from the KAT rows in the test project, either the C# implementation has regressed or this port has, and the discrepancy must be investigated before changing either side.

## CodeStyle analyzer (PowerShell 7+)

| Script | Purpose |
| --- | --- |
| `Update-CodeStyleAnalyzer.ps1` | One-command refresh of the in-repo `Bodu.CodeStyle.XmlDocumentation` analyzer: packs it into `local-packages/` (delegating to `bld/pack-codestyle-analyzer.ps1`, which also evicts NuGet's global cache), force-restores a consumer, then builds it so Roslyn loads the new payload. Run it after changing anything under `Bodu.CodeStyle/`. |

```pwsh
# Repack the analyzer, then force-restore + build the solution against it (benchmarks excluded)
pwsh ./tools/Update-CodeStyleAnalyzer.ps1

# Narrow the restore/build target for faster iteration
pwsh ./tools/Update-CodeStyleAnalyzer.ps1 -Target Bodu.Core/src/Bodu.Core.csproj

# Pack + force-restore only (skip the consumer build)
pwsh ./tools/Update-CodeStyleAnalyzer.ps1 -SkipBuild

# Include benchmark projects too (no exclusions)
pwsh ./tools/Update-CodeStyleAnalyzer.ps1 -ExcludeProjectPattern @()
```

When the target is a solution, the script drops projects that don't exercise the XML-documentation analyzer — by default any `/bench/` project — by generating a temporary solution filter (`.slnf`) listing only the kept projects and restoring/building that. `-ExcludeProjectPattern` takes one or more regexes tested against each project path; pass `@()` to build the whole solution unfiltered, or add patterns to skip more. The parameter is ignored when `-Target` is a single project.

The analyzer is always packed in **Release** (the configuration committed to `local-packages/` and used by CI); `-Configuration` governs only the consumer restore/build. Packing runs under the SDK 8 pin in `Bodu.CodeStyle/global.json` while the consumer build runs under the repo-root SDK 10 pin — each `dotnet` invocation resolves its own SDK from its working directory. After a successful run, commit the regenerated `local-packages/Bodu.CodeStyle.XmlDocumentation.1.0.0.nupkg` alongside your source changes. See `Bodu.CodeStyle/README.md` for the full analyzer-authoring workflow.
