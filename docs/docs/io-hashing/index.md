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
| <xref:Bodu.IO.Hashing.Fnv1a32> / <xref:Bodu.IO.Hashing.Fnv1a64> | 32 / 64 bits | Constant-memory, streaming. Preferred over FNV-1 for better avalanche. |
| <xref:Bodu.IO.Hashing.Fnv132> / <xref:Bodu.IO.Hashing.Fnv164> | 32 / 64 bits | Original FNV-1; legacy interoperability only. |
| <xref:Bodu.IO.Hashing.CityHash32> / <xref:Bodu.IO.Hashing.CityHash64> / <xref:Bodu.IO.Hashing.CityHash128> | 32 / 64 / 128 bits | SIMD-friendly; fastest on long inputs (buffers internally). |
| <xref:Bodu.IO.Hashing.MurmurHash3_32> / <xref:Bodu.IO.Hashing.MurmurHash3_128> | 32 / 128 bits | Seeded; excellent avalanche; widely used in databases. |
| <xref:Bodu.IO.Hashing.Pearson> | 8–2048 bits | Table-driven; configurable output width in 8-bit steps. |
| <xref:Bodu.IO.Hashing.Bernstein> | 32 bits | Classic djb2; configurable add-vs-XOR variant. |
| <xref:Bodu.IO.Hashing.BKDR> / <xref:Bodu.IO.Hashing.SDBM> / <xref:Bodu.IO.Hashing.JSHash> / <xref:Bodu.IO.Hashing.Elf64> / <xref:Bodu.IO.Hashing.ApHash> / <xref:Bodu.IO.Hashing.Pjw32> | 32–64 bits | Classic string hashes from compilers and early web servers. |
| <xref:Bodu.IO.Hashing.SuperFastHash> | 32 bits | Paul Hsieh's hash; designed for short keys. |
| <xref:Bodu.IO.Hashing.BlockNonCryptographicHashAlgorithm`1> | — | Abstract base for buffered block-oriented algorithms; CRTP-style extension point. |
| <xref:Bodu.IO.Hashing.IResumableHashAlgorithm> | — | Optional contract: reverse-finalise a stored digest, append more bytes, finalise again. Implemented by `Crc`. |

> **BCL note.** `XxHash32` / `XxHash64` / `XxHash3` / `XxHash128` from `System.IO.Hashing` already cover the xxHash family — Bodu does not duplicate them. Use the BCL types directly when you want xxHash.

### `Bodu.IO.Hashing.Checksums` — Checksums

Error-detection algorithms with characterised guarantees over specific error patterns. Also hosts the multi-character / alphanumeric check-digit algorithms for codes like IBAN, ISBN, and CUSIP.

| Type | Output | Subfamily |
|---|---|---|
| <xref:Bodu.IO.Hashing.Checksums.Crc> + <xref:Bodu.IO.Hashing.Checksums.CrcStandard> + <xref:Bodu.IO.Hashing.Checksums.CrcStandards> | 1–64 bits | Polynomial-remainder; 113 named standards from the RevEng catalogue, plus custom parameter sets. |
| <xref:Bodu.IO.Hashing.Checksums.CrcLookupTableBuilder> / <xref:Bodu.IO.Hashing.Checksums.CrcLookupTableCache> | — | Shared lookup-table cache so identical CRC parameter sets share a table. |
| <xref:Bodu.IO.Hashing.Checksums.Fletcher16> / <xref:Bodu.IO.Hashing.Checksums.Fletcher32> / <xref:Bodu.IO.Hashing.Checksums.Fletcher64> | 16 / 32 / 64 bits | Twin-accumulator; catches transpositions a simple sum or XOR misses. |
| <xref:Bodu.IO.Hashing.Checksums.Adler32> / <xref:Bodu.IO.Hashing.Checksums.Adler32C> / <xref:Bodu.IO.Hashing.Checksums.Adler64> | 32 / 32 / 64 bits | Prime / power-of-two modulus twin accumulator; Adler-32 is the canonical zlib checksum. |
| <xref:Bodu.IO.Hashing.Checksums.Iban>, <xref:Bodu.IO.Hashing.Checksums.Isbn10>, <xref:Bodu.IO.Hashing.Checksums.Isbn13>, <xref:Bodu.IO.Hashing.Checksums.Sedol>, <xref:Bodu.IO.Hashing.Checksums.Cusip>, <xref:Bodu.IO.Hashing.Checksums.Lei> | Multi-char | Alphanumeric / multi-character identifier checksums. |
| <xref:Bodu.IO.Hashing.Checksums.Iso7064Mod11_2>, <xref:Bodu.IO.Hashing.Checksums.Iso7064Mod97_10> | 1–2 chars | Generic ISO 7064 checksum building blocks for custom alphanumeric schemes. |
| <xref:Bodu.IO.Hashing.Checksums.CheckDigitInputAlphabet> / <xref:Bodu.IO.Hashing.Checksums.CheckDigitOutputAlphabet> | — | Character-set enums consumed by the alphanumeric check-digit algorithms. |

### `Bodu.IO.Hashing.CheckDigits` — Check digits

Single-character check-digit algorithms over decimal alphabets, for human-typed identifiers like credit card numbers and barcodes.

| Type | Used by |
|---|---|
| <xref:Bodu.IO.Hashing.CheckDigits.Luhn> | Credit cards (Visa, Mastercard, Amex), IMEI, SIN |
| <xref:Bodu.IO.Hashing.CheckDigits.Damm> | General purpose; detects all single-digit and adjacent-transposition errors |
| <xref:Bodu.IO.Hashing.CheckDigits.Verhoeff> | German ID, medical device codes; widest decimal-alphabet error coverage |
| <xref:Bodu.IO.Hashing.CheckDigits.Ean8> / <xref:Bodu.IO.Hashing.CheckDigits.Ean13> | Retail barcodes |
| <xref:Bodu.IO.Hashing.CheckDigits.Gtin14> | Shipping cartons |
| <xref:Bodu.IO.Hashing.CheckDigits.UpcA> | US/Canada retail barcodes |
| <xref:Bodu.IO.Hashing.CheckDigits.Isin> | International securities identifiers (ISO 6166) |
| <xref:Bodu.IO.Hashing.CheckDigits.AbaRoutingNumber> | US bank routing numbers |
| <xref:Bodu.IO.Hashing.CheckDigits.CheckDigitAlgorithm>, <xref:Bodu.IO.Hashing.CheckDigits.AlphanumericCheckDigitAlgorithm>, <xref:Bodu.IO.Hashing.CheckDigits.MultiCharCheckDigitAlgorithm> | Abstract base classes — extension points for custom schemes. |

### `Bodu.IO.Hashing.Extensions`

Extension methods over `NonCryptographicHashAlgorithm` for ergonomic one-shot computation, async streaming, and verification.

| Type | Provides |
|---|---|
| <xref:Bodu.IO.Hashing.Extensions.NonCryptographicHashAlgorithmExtensions> | `AppendData`, `AppendDataAsync`, `ComputeHash`, `ComputeHashAsync`, `VerifyHash`, `VerifyHashAsync`, `TryVerifyHash`, `TryVerifyHashAsync`. |

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
- **For keyed and cryptographic hashes** (SipHash, Poly1305, Tiger, ASCON, Merkle trees), see [Bodu.Security.Cryptography](../cryptography/index.md).
