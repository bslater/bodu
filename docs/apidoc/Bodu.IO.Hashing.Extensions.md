---
uid: Bodu.IO.Hashing.Extensions
---

![Bodu.IO.Hashing](~/images/hero-io.svg)

## Purpose

**Bodu.IO.Hashing.Extensions** holds the extension methods for the BCL `NonCryptographicHashAlgorithm` base type that the Bodu hash algorithms derive from. (CRC lookup-table construction lives in <xref:Bodu.IO.Hashing.Checksums> — see <xref:Bodu.IO.Hashing.Checksums.CrcLookupTableBuilder>.)

## Key types

- <xref:Bodu.IO.Hashing.Extensions.NonCryptographicHashAlgorithmExtensions> — convenience methods on `System.IO.Hashing.NonCryptographicHashAlgorithm` — `ComputeHash`, `AppendData`, and `VerifyHash` / `TryVerifyHash` (with stream-based async overloads) that fit between the BCL surface and the Bodu hash algorithms.

## Example

```csharp
using Bodu.IO.Hashing;
using Bodu.IO.Hashing.Checksums;
using Bodu.IO.Hashing.Extensions;

var crc = new Crc(CrcStandard.CRC32_ISOHDLC);

// ComputeHash extension: one-shot digest over a span, no manual Append / reset.
byte[] digest = crc.ComputeHash("123456789"u8);
```

## Notes

- **One-shot vs. streaming.** `ComputeHash` resets the instance, appends, and snapshots in one call; use the base `Append` / `GetCurrentHash` pair directly when you need to hash in chunks.
- **See also:** the [CRC guide](~/guides/io-hashing/crc.md), the [Bodu.IO.Hashing landing page](xref:Bodu.IO.Hashing).
