---
uid: Bodu.IO.Hashing.Extensions
---

![Bodu.IO.Hashing](~/images/hero-io.svg)

## Purpose

**Bodu.IO.Hashing.Extensions** holds the shared extension surfaces for the hashing types — CRC lookup-table construction and helpers for the BCL `NonCryptographicHashAlgorithm` base type that the Bodu hash algorithms derive from.

## Key types

- <xref:Bodu.IO.Hashing.Checksums.CrcLookupTableBuilder> — builds the precomputed lookup table for a given CRC standard. The same table is cached process-wide by <xref:Bodu.IO.Hashing.Checksums.CrcLookupTableCache>; reach for the builder directly only when you need to compute a table for a non-standard CRC variant.
- <xref:Bodu.IO.Hashing.Extensions.NonCryptographicHashAlgorithmExtensions> — convenience methods on `System.IO.Hashing.NonCryptographicHashAlgorithm` — `ComputeHash`, `AppendData`, and `VerifyHash` / `TryVerifyHash` (with stream-based async overloads) that fit between the BCL surface and the Bodu hash algorithms.

## Example

```csharp
using Bodu.IO.Hashing;
using Bodu.IO.Hashing.Extensions;

var crc = new Crc(CrcStandard.CRC32_ISOHDLC);

// ComputeHash extension: one-shot digest over a span, no manual Append / reset.
byte[] digest = crc.ComputeHash("123456789"u8);
```

## Notes

- **Cache, don't rebuild.** Lookup-table construction is non-trivial; prefer <xref:Bodu.IO.Hashing.Checksums.CrcLookupTableCache> for standard CRC variants. The builder is exposed for non-standard cases (custom polynomial, custom refIn / refOut configuration).
- **See also:** the [CRC guide](~/guides/io-hashing/crc.md), the [Bodu.IO.Hashing landing page](xref:Bodu.IO.Hashing).
