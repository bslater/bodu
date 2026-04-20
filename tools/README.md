# tools/

Maintenance scripts for the CRC subsystem. All scripts require **PowerShell 7+** and are
intended to be run from the repository root.

## Scripts

| Script | Purpose |
| --- | --- |
| `Fetch-CrcSpecs.ps1` | Downloads the CRC RevEng catalogue from `https://reveng.sourceforge.io/crc-catalogue/all.htm` and rebuilds `Bodu.Security.Cryptography/src/crc-specs.json`. Also writes `crc-specs.meta.json` with the source URL and fetch timestamp. Class, Created, Updated, Alias, and Codeword metadata is parsed from the page where possible, and otherwise carried over from the existing JSON so hand-curated fields survive a refresh. |
| `Generate-CrcCatalog.ps1` | Regenerates `Bodu.Security.Cryptography/src/Security.Cryptography/CrcStandard.Catalog.cs` from the JSON. Emits one `public static readonly CrcStandard` field per canonical entry **plus** one per alias (as a reference to the canonical), with full XML doc blocks including spec definition, Created/Updated dates, a link to the RevEng source anchor, and `<seealso>` cross-references between aliases. |
| `Generate-CrcCatalogTests.ps1` | Regenerates `Bodu.Security.Cryptography/test/Security.Cryptography/CrcTests.Catalog.cs`, a data-driven `[DataTestMethod]` that asserts every catalogue entry's CRC of ASCII `"123456789"` matches the RevEng-published `check` value. |
| `Generate-CrcDocs.ps1` | Regenerates `docs/guides/cryptography/crc-catalogue.md`, the public-facing attribution and support-matrix page. Reads `crc-specs.meta.json` for the last-fetched timestamp and stamps the page with the current regeneration time. |

## Standard workflow

```pwsh
# 1. Refresh the JSON from upstream
pwsh ./tools/Fetch-CrcSpecs.ps1

# 2. Rebuild the generated C# files and documentation
pwsh ./tools/Generate-CrcCatalog.ps1
pwsh ./tools/Generate-CrcCatalogTests.ps1
pwsh ./tools/Generate-CrcDocs.ps1

# 3. Build and run tests
dotnet build Bodu.sln
dotnet test Bodu.Security.Cryptography/test/Bodu.Security.Cryptography.Test.csproj --settings test.runsettings
```

## Filtering rules

All three generators share two filters:

- **`-Exclude <names>`** — canonical CRC names whose field declarations are owned elsewhere (typically by hand in `CrcStandard.cs`). They are not re-emitted as declarations in the catalogue, but their aliases are still emitted (pointing at the externally declared canonical), and the canonical is still referenced from `All` and the name lookup. Defaults to `CRC-32/ISO-HDLC`.
- **`-MaxSize <bits>`** — upper bound on CRC width. Entries whose `size` exceeds this value are skipped entirely: no field, no aliases, no `All` entry, no lookup entry, no test row. The generated documentation page lists them in a separate "not supported" section for transparency. Defaults to `64` (the widest width representable as a `ulong`, matching `CrcStandard.MaxSize`).

Currently the only oversize entry is `CRC-82/DARC`.

## Core vs. catalogue standards

`CRC-32/ISO-HDLC` is excluded from the generated C# files by default. It is:

- Declared directly in `Bodu.Security.Cryptography/src/Security.Cryptography/CrcStandard.cs` as `CrcStandard.CRC32_ISOHDLC`.
- The default standard used by `new Crc()` (see `Crc.cs`).
- Exercised by the hand-written test vectors in `CrcTests.ComputeHash.cs` and friends, which cover more than just the single `check` input.

Its **aliases** (for example `CRC-32`, `CRC-32/ADCCP`, `PKZIP`) are still emitted by the generator as `public static readonly CrcStandard` fields that reference the core declaration, so `CrcStandard.CRC32`, `CrcStandard.PKZIP`, etc. resolve correctly.

To reshape the split — for example to promote another standard to first-class core code — pass `-Exclude` to both generator scripts and hand-maintain the declaration in `CrcStandard.cs`:

```pwsh
pwsh ./tools/Generate-CrcCatalog.ps1      -Exclude 'CRC-32/ISO-HDLC','CRC-32/ISCSI'
pwsh ./tools/Generate-CrcCatalogTests.ps1 -Exclude 'CRC-32/ISO-HDLC','CRC-32/ISCSI'
pwsh ./tools/Generate-CrcDocs.ps1         -Exclude 'CRC-32/ISO-HDLC','CRC-32/ISCSI'
```

Any excluded canonical must still exist as a `public static readonly CrcStandard` with the generator's naming convention (`CRC-32/ISCSI` → `CRC32_ISCSI`) in `CrcStandard.cs` or another `CrcStandard.*.cs` partial, so that `All` and the generated aliases can reference it.
