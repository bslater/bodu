---
title: Bodu.IO.Hashing — Getting started
---

# Bodu.IO.Hashing — Getting started

## Install

```bash
dotnet add package Bodu.IO.Hashing
```

Targets `net8.0`. Depends on `Bodu.Core` and the BCL `System.IO.Hashing` package.

## Minimal samples — one per subfamily

### Checksum — CRC-32

```csharp
using System.Text;
using Bodu.IO.Hashing.Checksums;

byte[] data = Encoding.UTF8.GetBytes("the quick brown fox");

using var crc = new Crc(CrcStandard.CRC32_ISOHDLC);
crc.Append(data);
string hex = Convert.ToHexString(crc.GetCurrentHash());
```

`CRC32_ISOHDLC` is the canonical zlib / PNG / Ethernet CRC-32. Swap it for `CRC32_ISCSI`, `CRC16_MODBUS`, `CRC64_XZ`, or any of the 113 entries in the [CRC catalogue](../../guides/io-hashing/crc-catalogue.md).

### Checksum — Fletcher-32

```csharp
using System.Text;
using Bodu.IO.Hashing.Checksums;

byte[] data = Encoding.UTF8.GetBytes("the quick brown fox");

using var fletcher = new Fletcher32();
fletcher.Append(data);
byte[] checksum = fletcher.GetCurrentHash();
```

Fletcher's twin-accumulator structure catches transpositions that a simple sum or XOR misses. Choose `Fletcher16` / `Fletcher32` / `Fletcher64` based on your output width.

### Fingerprint — FNV-1a 64

```csharp
using System.Text;
using Bodu.IO.Hashing;

byte[] data = Encoding.UTF8.GetBytes("the quick brown fox");

using var fnv = new Fnv1a64();
fnv.Append(data);
ulong key = BitConverter.ToUInt64(fnv.GetCurrentHash());
```

Constant-memory and streaming. For SIMD-friendly throughput on large buffers, swap in `CityHash64`. For seeded hashes used in databases or probabilistic data structures, use `MurmurHash3_128`. For xxHash specifically, prefer `System.IO.Hashing.XxHash64` from the BCL — Bodu does not duplicate it.

### Fingerprint — Pearson with custom output width

```csharp
using Bodu.IO.Hashing;

using var hash = new Pearson(outputWidthBits: 256);
hash.Append(data);
byte[] digest = hash.GetCurrentHash(); // 32 bytes
```

`Pearson` accepts any output width from 8 bits to 2048 bits in 8-bit steps.

### Check digit — Luhn (credit card)

```csharp
using Bodu.IO.Hashing.CheckDigits;

bool valid = Luhn.IsValid("4539 1488 0343 6467".Replace(" ", ""));
char digit  = Luhn.ComputeCheckDigit("453914880343646");
```

Other check-digit types follow the same `IsValid` / `ComputeCheckDigit` contract. For multi-character checksums, see <xref:Bodu.IO.Hashing.Checksums.Iban>, <xref:Bodu.IO.Hashing.Checksums.Isbn13>, <xref:Bodu.IO.Hashing.Checksums.Cusip>, and <xref:Bodu.IO.Hashing.Checksums.Lei>.

### Check digit — IBAN (multi-character)

```csharp
using Bodu.IO.Hashing.Checksums;

bool valid = Iban.IsValid("GB82WEST12345698765432");
```

### One-shot computation via the extension methods

```csharp
using Bodu.IO.Hashing;
using Bodu.IO.Hashing.Extensions;

using var hash = new Fnv1a64();
byte[] digest = hash.ComputeHash(data);   // Append + GetCurrentHash + Reset
bool   match  = hash.VerifyHash(data, expected);
```

`AppendDataAsync`, `ComputeHashAsync`, and `VerifyHashAsync` provide the streaming-friendly equivalents over a `Stream`.

## Where to go next

- **[Bodu.IO.Hashing introduction](index.md)** — namespaces, headline types, scenarios.
- **[Bodu.Security.Cryptography](../cryptography/index.md)** — the sibling library, for keyed and cryptographic hashes with a formal adversary model.
- **[Bodu.IO.Hashing guides](../../guides/io-hashing/index.md)** — per-algorithm walk-throughs.
- **[Bodu.IO.Hashing API reference](../../apidoc/Bodu.IO.Hashing.md)** — full type-by-type docs.
- **[CRC catalogue](../../guides/io-hashing/crc-catalogue.md)** — the full RevEng-catalogue table of named CRC standards.
