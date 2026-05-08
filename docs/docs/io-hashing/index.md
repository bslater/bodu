---
title: Bodu.IO.Hashing — Introduction
---

# Bodu.IO.Hashing

**Bodu.IO.Hashing** is the non-cryptographic hashing package of the Bodu suite. Everything in the package derives from <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType>, so the lifecycle (`Append` / `GetCurrentHash` / `Reset`) is identical regardless of which algorithm you pick.

> **Adversary model: none.** Nothing in this library is safe against an attacker who can choose inputs. Use it for error detection, distribution, fingerprinting, and identifier validation — for anything security-sensitive, see [Bodu.Security.Cryptography](../cryptography/index.md).

The library is organised around three subfamilies, each in its own namespace.

## Namespaces and headline types

### `Bodu.IO.Hashing` — Fingerprints

Fast, distribution-quality hash functions for hash-table keys, in-memory cache buckets, and content-addressable lookups.

| Type | Output | Notes |
|---|---|---|
| `Fnv1a32` / `Fnv1a64` | 32 / 64 bits | Constant-memory, streaming. Preferred over FNV-1 for better avalanche. |
| `Fnv132` / `Fnv164` | 32 / 64 bits | Original FNV-1; legacy interoperability only. |
| `CityHash32` / `CityHash64` / `CityHash128` | 32 / 64 / 128 bits | SIMD-friendly; fastest on long inputs (buffers internally). |
| `MurmurHash3_32` / `MurmurHash3_x64_128` | 32 / 128 bits | Seeded; excellent avalanche; widely used in databases. |
| `XxHash32` / `XxHash64` | 32 / 64 bits | High-throughput; seeded; non-streaming. |
| `Pearson` | 8–2048 bits | Table-driven; configurable output width in 8-bit steps. |
| `Bernstein` | 32 bits | Classic djb2; configurable add-vs-XOR variant. |
| `BKDR` / `SDBM` / `JSHash` / `Elf64` / `ApHash` / `Pjw32` | 32–64 bits | Classic string hashes from compilers and early web servers. |
| `SuperFastHash` | 32 bits | Paul Hsieh's hash; designed for short keys. |
| `BlockNonCryptographicHashAlgorithm<T>` | — | Internal base for buffered block-oriented algorithms. |
| `IResumableHashAlgorithm` | — | Optional contract: reverse-finalise a stored digest, append more bytes, finalise again. Implemented by `Crc`. |

### `Bodu.IO.Hashing.Checksums` — Checksums

Error-detection algorithms with characterised guarantees over specific error patterns. Also hosts the multi-character / alphanumeric check-digit algorithms for codes like IBAN, ISBN, and CUSIP.

| Type | Output | Subfamily |
|---|---|---|
| `Crc` + `CrcStandard` + `CrcStandards` | 1–64 bits | Polynomial-remainder; 113 named standards from the RevEng catalogue, plus custom parameter sets. |
| `CrcLookupTableBuilder` / `CrcLookupTableCache` | — | Shared lookup-table cache so identical CRC parameter sets share a table. |
| `Fletcher16` / `Fletcher32` / `Fletcher64` | 16 / 32 / 64 bits | Twin-accumulator; catches transpositions a simple sum or XOR misses. |
| `Adler32` / `Adler32C` / `Adler64` | 32 / 32 / 64 bits | Prime / power-of-two modulus twin accumulator; Adler-32 is the canonical zlib checksum. |
| `Iban`, `Isbn10`, `Isbn13`, `Sedol`, `Cusip`, `Lei` | Multi-char | Alphanumeric / multi-character identifier checksums. |
| `WeightedMod10` | 1 char | Configurable base for custom weighted-mod-10 schemes. |
| `Iso7064Mod11_2`, `Iso7064Mod97_10` | 1–2 chars | Generic ISO 7064 checksum building blocks. |
| `CheckDigitInputAlphabet` / `CheckDigitOutputAlphabet` / `Alphanumeric` | — | Character-set helpers used by alphanumeric algorithms. |

### `Bodu.IO.Hashing.CheckDigits` — Check digits

Single-character check-digit algorithms over decimal alphabets, for human-typed identifiers like credit card numbers and barcodes.

| Type | Used by |
|---|---|
| `Luhn` | Credit cards (Visa, Mastercard, Amex), IMEI, SIN |
| `Damm` | General purpose; detects all single-digit and adjacent-transposition errors |
| `Verhoeff` | German ID, medical device codes; widest decimal-alphabet error coverage |
| `Ean8` / `Ean13` | Retail barcodes |
| `Gtin14` | Shipping cartons |
| `UpcA` | US/Canada retail barcodes |
| `Isin` | International securities identifiers (ISO 6166) |
| `AbaRoutingNumber` | US bank routing numbers |
| `CheckDigitAlgorithm` / `AlphanumericCheckDigitAlgorithm` / `MultiCharCheckDigitAlgorithm` | Abstract base classes — extension points for custom schemes. |

### `Bodu.IO.Hashing.Extensions`

Extension methods over `NonCryptographicHashAlgorithm` for ergonomic one-shot computation, async streaming, and verification.

| Type | Provides |
|---|---|
| `NonCryptographicHashAlgorithmExtensions` | `AppendData`, `AppendDataAsync`, `ComputeHash`, `ComputeHashAsync`, `VerifyHash`, `VerifyHashAsync`, `TryVerifyHash`, `TryVerifyHashAsync`. |

## Subfamily comparison

For the structural differences between fingerprints, checksums, and check digits — and how to choose between types that look similar — see [Algorithm families](../algorithm-families.md). The short version:

| Subfamily | Optimised for | Operates on |
|---|---|---|
| Fingerprint | Distribution and speed | Binary buffer |
| Checksum | Error-pattern detection | Binary buffer |
| Check digit | Human transcription errors | Character sequence |

## Common lifecycle

```csharp
using Bodu.IO.Hashing;

using var hash = new Crc();      // or Fletcher32, Adler32, Fnv1a64, CityHash64, …

hash.Append(chunk1);
hash.Append(chunk2);
byte[] partial = hash.GetCurrentHash();   // snapshot, non-destructive
hash.Append(chunk3);
byte[] full    = hash.GetCurrentHash();

hash.Reset();                              // back to the initial state
```

Only `Crc` currently implements `IResumableHashAlgorithm` (reverse-finalise a stored digest, append more bytes, finalise again).

## Where to go next

- **[Getting started](getting-started.md)** — install + one minimal sample per subfamily.
- **[Algorithm families](../algorithm-families.md)** — fingerprints vs checksums vs check digits, plus the cryptographic families.
- **[Bodu.IO.Hashing guides](../../guides/io-hashing/index.md)** — recipe-style walk-throughs per algorithm.
- **[Bodu.IO.Hashing API reference](../../apidoc/Bodu.IO.Hashing.md)** — full type-by-type docs.
