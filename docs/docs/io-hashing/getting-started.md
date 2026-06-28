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

`CRC32_ISOHDLC` is the canonical zlib / PNG / Ethernet CRC-32. Swap it for `CRC32_ISCSI`, `CRC16_MODBUS`, `CRC64_XZ`, or any of the 112 entries in the [CRC catalogue](../../guides/io-hashing/crc-catalogue.md).

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

bool valid = Luhn.IsValid("4539148803436467");   // payload includes the check digit
char digit = Luhn.Compute("453914880343646");    // payload excludes it — returns the digit to append
```

Every single-character scheme exposes the same pair: `Compute(ReadOnlySpan<char>)` returns the `char` to append to a payload that does *not* yet carry the check; `IsValid(ReadOnlySpan<char>)` validates a payload that *does*. Substitute `Damm`, `Verhoeff`, `Ean13`, `Gtin14`, `UpcA`, `Isin`, or `AbaRoutingNumber` — the contract is identical. The multi-character schemes (<xref:Bodu.IO.Hashing.CheckDigits.Iban>, <xref:Bodu.IO.Hashing.CheckDigits.Lei>) return a `string` from `Compute` instead.

### Check digit — IBAN (multi-character)

```csharp
using Bodu.IO.Hashing.CheckDigits;

bool valid = Iban.IsValid("GB82WEST12345698765432");
```

### One-shot computation via the extension methods

```csharp
using Bodu.IO.Hashing;
using Bodu.IO.Hashing.Extensions;

using var hash = new Fnv1a64();
byte[] digest   = hash.ComputeHash(data);            // Append + GetCurrentHash + Reset, in one call
bool   match    = hash.VerifyHash(data, digest);     // recompute and compare against the byte[]
bool   matchHex = hash.VerifyHash(data, Convert.ToHexString(digest)); // or compare against a stored hex string
```

`ComputeHash` resets the instance and returns the one-shot digest, so the same instance is immediately reusable. `VerifyHash` has overloads taking either a `byte[]` digest or a hex `string`, and `Stream`-based forms that hash the stream first. `TryVerifyHash` returns `false` instead of throwing when the candidate is malformed (wrong length, non-hex characters) — the safer choice over user-supplied input.

> [!IMPORTANT]
> `VerifyHash` compares with `SequenceEqual` and short-circuits on the first mismatching byte — it is **not** constant-time and must not be used to check an authenticator supplied by an untrusted caller. These are error-detection comparisons. For constant-time verification of a keyed digest, use the `Bodu.Security.Cryptography` `VerifyHash` overloads, which call `CryptographicOperations.FixedTimeEquals`.

### Async streaming over a `Stream`

```csharp
using Bodu.IO.Hashing;
using Bodu.IO.Hashing.Extensions;

await using FileStream fs = File.OpenRead("archive.bin");

using var hash = new Crc(CrcStandard.CRC32_ISOHDLC);
byte[] digest = await hash.ComputeHashAsync(fs);     // rents a pooled buffer, appends in chunks
```

`AppendDataAsync`, `ComputeHashAsync`, `VerifyHashAsync`, and `TryVerifyHashAsync` are the streaming-friendly equivalents; each accepts an optional `bufferSize` and a `CancellationToken`. They rent the working buffer from `ArrayPool<byte>.Shared`, so hashing a large file allocates only the final digest.

## Where to go next

- **[Bodu.IO.Hashing introduction](index.md)** — namespaces, headline types, scenarios.
- **[Bodu.Security.Cryptography](../cryptography/index.md)** — the sibling library, for keyed and cryptographic hashes with a formal adversary model.
- **[Bodu.IO.Hashing guides](../../guides/io-hashing/index.md)** — per-algorithm walk-throughs.
- **[Bodu.IO.Hashing API reference](xref:Bodu.IO.Hashing)** — full type-by-type docs.
- **[CRC catalogue](../../guides/io-hashing/crc-catalogue.md)** — the full RevEng-catalogue table of named CRC standards.
