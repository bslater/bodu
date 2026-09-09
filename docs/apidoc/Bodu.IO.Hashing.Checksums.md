---
uid: Bodu.IO.Hashing.Checksums
---

![Bodu.IO.Hashing](~/images/hero-io.svg)

## Purpose

**Bodu.IO.Hashing.Checksums** ships the cyclic redundancy check (CRC) catalogue from the [RevEng project](https://reveng.sourceforge.io/crc-catalogue/) — every variant from CRC-1 through CRC-64, addressable by name. The catalogue is auto-generated; the consumer-facing surface is the immutable <xref:Bodu.IO.Hashing.Checksums.CrcStandard> parameter set, used as a parameter to <xref:Bodu.IO.Hashing.Checksums.Crc>. The namespace also hosts the twin-accumulator checksums (Fletcher, Adler) and the CRC lookup-table machinery.

## Key types

- <xref:Bodu.IO.Hashing.Checksums.Crc> — the CRC engine, configured with a `CrcStandard`.
- <xref:Bodu.IO.Hashing.Checksums.CrcStandard> — a sealed, immutable CRC parameter set: `Name`, `Size`, `Polynomial`, `InitialValue`, `ReflectIn`, `ReflectOut`, `XOrOut`. Common standards are exposed as static properties (`CRC32_ISOHDLC`, `CRC16_MODBUS`, …); every catalogue entry is reachable through `Get(CrcStandards)` and `FromName` / `TryFromName`; custom variants come from the public constructor.
- <xref:Bodu.IO.Hashing.Checksums.CrcStandards> — the enum naming every canonical catalogue entry.
- <xref:Bodu.IO.Hashing.Checksums.CrcLookupTableBuilder> / <xref:Bodu.IO.Hashing.Checksums.CrcLookupTableCache> — lookup-table construction and the process-wide cache shared by every `Crc` with the same (width, polynomial, reflect-in) triple.
- <xref:Bodu.IO.Hashing.Checksums.Fletcher16> / <xref:Bodu.IO.Hashing.Checksums.Fletcher32> / <xref:Bodu.IO.Hashing.Checksums.Fletcher64> and <xref:Bodu.IO.Hashing.Checksums.Adler32> / <xref:Bodu.IO.Hashing.Checksums.Adler32C> / <xref:Bodu.IO.Hashing.Checksums.Adler64> — the twin-accumulator checksums.

## Example

```csharp
using Bodu.IO.Hashing;
using Bodu.IO.Hashing.Checksums;

// CRC-32 (the standard variant used by Ethernet, ZIP, PNG).
var crc32 = new Crc(CrcStandard.CRC32_ISOHDLC);
ReadOnlySpan<byte> data = "123456789"u8;
crc32.Append(data);
byte[] digest = crc32.GetCurrentHash();
// 0xCBF43926 — the canonical CRC-32 check value
```

## Notes

- **Auto-generated catalogue.** The standards are produced from the RevEng database; consumers reference them by name (`CrcStandard.CRC32_ISOHDLC`, `CrcStandard.CRC16_MODBUS`, …) rather than constructing the configuration manually.
- **Resumable.** `Crc`, the Fletcher family, and the Adler family implement <xref:Bodu.IO.Hashing.IResumableHashAlgorithm> — `ComputeHashFrom` reverse-finalizes a previously stored digest, appends more bytes, and finalizes again, without re-reading the original input. (Plain multi-`Append` streaming needs no special contract; every algorithm supports it.)
- **See also:** the [CRC guide](~/guides/io-hashing/crc.md), the [CRC catalogue guide](~/guides/io-hashing/crc-catalogue.md), and the parent <xref:Bodu.IO.Hashing> landing page.
