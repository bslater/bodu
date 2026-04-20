# tools/

Maintenance scripts for the CRC subsystem. All scripts require **PowerShell 7+** and are
intended to be run from the repository root.

## Scripts

| Script | Purpose |
| --- | --- |
| `Fetch-CrcSpecs.ps1` | Downloads the CRC RevEng catalogue from `https://reveng.sourceforge.io/crc-catalogue/all.htm` and rebuilds `Bodu.Security.Cryptography/src/crc-specs.json`. Informational fields (`class`, `created`, `updated`, `aliases`, `codewords`) that are not present in the RevEng parameter line are carried over from the existing JSON, so hand-curated metadata survives a refresh. |
| `Generate-CrcCatalog.ps1` | Regenerates `Bodu.Security.Cryptography/src/Security.Cryptography/CrcStandard.Catalog.cs` from `crc-specs.json`. Emits one `public static readonly CrcStandard` field per entry, plus the `All`, `FromName`, and `TryFromName` helpers. Standards listed in `-Exclude` are omitted from the emitted declarations but still referenced by `All`. |
| `Generate-CrcCatalogTests.ps1` | Regenerates `Bodu.Security.Cryptography/test/Security.Cryptography/CrcTests.Catalog.cs`, a data-driven `[DataTestMethod]` that asserts every catalogue entry's CRC of ASCII `"123456789"` matches the RevEng-published `check` value. |

## Standard workflow

```pwsh
# 1. Refresh the JSON from upstream
pwsh ./tools/Fetch-CrcSpecs.ps1

# 2. Rebuild the generated C# files
pwsh ./tools/Generate-CrcCatalog.ps1
pwsh ./tools/Generate-CrcCatalogTests.ps1

# 3. Build and run tests
dotnet build Bodu.sln
dotnet test Bodu.Security.Cryptography/test/Bodu.Security.Cryptography.Test.csproj --settings test.runsettings
```

## Core vs. catalogue standards

`CRC-32/ISO-HDLC` is excluded from the generated files by default. It is:

- Declared directly in `Bodu.Security.Cryptography/src/Security.Cryptography/CrcStandard.cs` as `CrcStandard.CRC32_ISOHDLC`.
- The default standard used by `new Crc()` (see `Crc.cs`).
- Exercised by the hand-written test vectors in `CrcTests.ComputeHash.cs` and friends, which cover more than just the single `check` input.

This keeps the generators purely concerned with the "long tail" of 112 alternate standards and
avoids redundant test coverage for the one we treat as first-class.

To override the exclusion list (for example, to take over another standard as first-class core
code), pass `-Exclude` to both generator scripts:

```pwsh
pwsh ./tools/Generate-CrcCatalog.ps1 -Exclude 'CRC-32/ISO-HDLC','CRC-32/ISCSI'
pwsh ./tools/Generate-CrcCatalogTests.ps1 -Exclude 'CRC-32/ISO-HDLC','CRC-32/ISCSI'
```

Any excluded name must still exist as a `public static readonly CrcStandard` with the generator's
naming convention (`CRC-32/ISCSI` → `CRC32_ISCSI`), either in `CrcStandard.cs` or another
`CrcStandard.*.cs` partial, so that `All` can reference it.
