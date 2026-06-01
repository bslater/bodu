---
uid: Bodu.IO.Hashing.Checksums
---

![Bodu.IO.Hashing](~/images/hero-io.svg)

## Purpose

**Bodu.IO.Hashing.Checksums** ships the cyclic redundancy check (CRC) catalogue from the [RevEng project](https://reveng.sourceforge.io/crc-catalogue/) — every variant from CRC-1 through CRC-64, addressable by name. The catalogue is auto-generated and partial; the consumer-facing surface is the static <xref:Bodu.IO.Hashing.Checksums.CrcStandard> class, used as a parameter to <xref:Bodu.IO.Hashing.Crc>.

## Key types

- <xref:Bodu.IO.Hashing.Checksums.CrcStandard> — static catalogue of CRC standards. Each standard is a static property returning a configuration record carrying the polynomial, initial value, refIn / refOut flags, XOR-out value, and check value.

## Example

```csharp
using Bodu.IO.Hashing;
using Bodu.IO.Hashing.Checksums;

// CRC-32 (the standard variant used by Ethernet, ZIP, PNG).
var crc32 = new Crc(CrcStandard.Crc32);
ReadOnlySpan<byte> data = "123456789"u8;
crc32.Append(data);
uint result = (uint)crc32.GetCurrentHashAsUInt64();
// 0xCBF43926 — the canonical CRC-32 check value
```

## Notes

- **Auto-generated catalogue.** The standards are produced from the RevEng database; consumers reference them by name (`CrcStandard.Crc32`, `CrcStandard.Crc16Modbus`, …) rather than constructing the configuration manually.
- **Resumable.** `Crc` implements <xref:Bodu.IO.Hashing.IResumableHashAlgorithm>, so a long input stream can be hashed across multiple `Append` calls.
- **See also:** the [CRC guide](~/guides/io-hashing/crc.md), the [CRC catalogue guide](~/guides/io-hashing/crc-catalogue.md), and the parent <xref:Bodu.IO.Hashing> landing page.
