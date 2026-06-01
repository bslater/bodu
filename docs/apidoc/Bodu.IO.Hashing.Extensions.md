---
uid: Bodu.IO.Hashing.Extensions
---

![Bodu.IO.Hashing](~/images/hero-io.svg)

## Purpose

**Bodu.IO.Hashing.Extensions** holds the shared extension surfaces for the hashing types — CRC lookup-table construction and helpers for the BCL `NonCryptographicHashAlgorithm` base type that the Bodu hash algorithms derive from.

## Key types

- <xref:Bodu.IO.Hashing.Extensions.CrcLookupTableBuilder> — builds the precomputed lookup table for a given CRC standard. The same table is cached process-wide by <xref:Bodu.IO.Hashing.CrcLookupTableCache>; reach for the builder directly only when you need to compute a table for a non-standard CRC variant.
- <xref:Bodu.IO.Hashing.Extensions.NonCryptographicHashAlgorithmExtensions> — convenience methods on `System.IO.Hashing.NonCryptographicHashAlgorithm` — `GetCurrentHashAsUInt32`, `GetCurrentHashAsUInt64`, stream / span overloads, and helpers that fit between the BCL surface and the Bodu hash algorithms.

## Example

```csharp
using Bodu.IO.Hashing;
using Bodu.IO.Hashing.Extensions;

var crc = new Crc(CrcStandard.Crc32);
crc.Append("123456789"u8);

// Convenience accessor instead of materialising bytes.
uint value = crc.GetCurrentHashAsUInt32();
```

## Notes

- **Cache, don't rebuild.** Lookup-table construction is non-trivial; prefer <xref:Bodu.IO.Hashing.CrcLookupTableCache> for standard CRC variants. The builder is exposed for non-standard cases (custom polynomial, custom refIn / refOut configuration).
- **See also:** the [CRC guide](~/guides/io-hashing/crc.md), the [Bodu.IO.Hashing landing page](xref:Bodu.IO.Hashing).
