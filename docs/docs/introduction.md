# Introduction

**Bodu** is a solution that ships four independent .NET NuGet packages, each focused on a narrow, well-defined problem domain. The packages share nothing at runtime — each is self-contained — but share a single set of source and documentation conventions, a single analyzer and test configuration, and a single quality bar.

## The libraries

| Package | Purpose | Target framework |
|---|---|---|
| **Bodu.Core** | Fixed-capacity collections (circular buffer, evicting dictionary), buffer conversion helpers, array and text extensions, argument-validation helpers. | `net8.0` |
| **Bodu.IO.Hashing** | Non-cryptographic checksums on `System.IO.Hashing.NonCryptographicHashAlgorithm` — the full CRC RevEng catalogue (widths 1–64 bits) and the Fletcher 16 / 32 / 64 family, with shared lookup-table caching and resumable hashing. | `net8.0` |
| **Bodu.Security.Cryptography** | Managed block ciphers (Threefish, Serpent, Camellia, Twofish, Blowfish, Skipjack), an AES adapter paired with AEAD mode transforms, keyed and cryptographic hashes (SipHash, Tiger, ASCON), Merkle-tree hashing, and the classic non-cryptographic hash families (Adler, FNV, CityHash). | `net8.0` |
| **Bodu.Globalization.Calendar** | Notable-date resolution and dynamic calendar calculators (Easter, Lunar New Year), driven from a pluggable XML rule source with an observance-adjustment pipeline. | `net8.0` |

Each package is versioned and released independently. Take the one you need and ignore the others — there are no cross-package runtime dependencies. `Bodu.IO.Hashing` and `Bodu.Security.Cryptography` both depend on `Bodu.Core` for shared argument-validation helpers.

## Design principles

- **Small by intent.** Each library solves one coherent problem. If something fits better elsewhere in the .NET ecosystem, we don't duplicate it.
- **Nullable reference types** are enabled solution-wide. Public APIs make their null-intent explicit.
- **Analyzer-clean.** The solution runs StyleCop.Analyzers, Roslynator, the .NET analyzers, AsyncFixer, and Microsoft.VisualStudio.Threading.Analyzers at build time. Doc-comment warnings (including `CS1591`) are treated as errors.
- **Deterministic builds** produce reproducible package outputs.
- **Documentation-first.** Every public type and member carries XML documentation in British English, and that documentation is the source of truth for this site. The API reference you see here is generated directly from the source.
- **MIT licensed**, no external runtime dependencies beyond the BCL.

## Cryptographic vs non-cryptographic hashing

This solution deliberately splits hashing across two packages:

- **`Bodu.IO.Hashing`** — non-cryptographic checksums (CRC, Fletcher) on the BCL's <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType> contract. Use for error detection, file integrity against noise, framing, fingerprinting, and hash-table distribution.
- **`Bodu.Security.Cryptography`** — keyed and cryptographic hashes (SipHash, Tiger, Merkle tree) on <xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType>, plus the classic non-cryptographic families (Adler, FNV, CityHash) that were written before the split. Use for authentication, signatures, content addressing, and collision-resistant keying.

If you don't know which to reach for, start with the [introduction to hashing and checksums](../guides/io-hashing/) and follow the decision table there.

## Testing and conventions

The solution uses **MSTest** with a partial-class test layout that mirrors the source layout one-to-one. Test methods follow the naming convention `<MethodOrProperty>_When<Condition>[_For<TypedCondition>]_Should<ExpectedResult>` and carry an XML `<summary>` that starts with "Verifies that …". This makes test intent readable directly in the test explorer.

## Where to go next

- **[Getting started](getting-started.md)** — prerequisites, install commands, and a one-minute sample from each library.
- **[API reference](../api/)** — the full auto-generated type-by-type documentation.
- Library overviews: [Bodu.Core](../apidoc/Bodu.Collections.Generic.md) · [Bodu.IO.Hashing](../apidoc/Bodu.IO.Hashing.md) · [Bodu.Security.Cryptography](../apidoc/Bodu.Security.Cryptography.md) · [Bodu.Globalization.Calendar](../apidoc/Bodu.Globalization.Calendar.md).
