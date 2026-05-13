---
uid: Bodu.IO.Hashing
---

![Bodu.IO.Hashing](~/images/hero-io.svg)

## Purpose

**Bodu.IO.Hashing** is a focused library of **non-cryptographic** hashes, checksums, and check digits built on the BCL <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType> contract. It covers three subfamilies: **fingerprints** (FNV, CityHash, MurmurHash3, Pearson, classic string hashes), **checksums** (the full CRC RevEng catalogue at widths 1–64 bits, Fletcher 16/32/64, Adler 32/32C/64, plus multi-character identifier checksums like IBAN and ISBN), and **check digits** (Luhn, Damm, Verhoeff, EAN/GTIN/UPC, ISIN, ABA routing).

Reach for this library when you need a fast, deterministic checksum for error detection, file integrity, framing, fingerprinting, or human-typed identifier validation — and when you want the result to drop straight into any API that accepts <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType>. If you need cryptographic integrity (against an active attacker, not just noise on the wire), see <xref:Bodu.Security.Cryptography> instead.

> **BCL note.** `XxHash32`, `XxHash64`, `XxHash3`, and `XxHash128` already ship in `System.IO.Hashing` from .NET 6 onwards — Bodu does not duplicate them. Use the BCL types directly when you want xxHash.

## Static documentation

- **[Bodu.IO.Hashing introduction](~/docs/io-hashing/index.md)** — namespaces, headline types, scenarios.
- **[Bodu.IO.Hashing getting started](~/docs/io-hashing/getting-started.md)** — install and minimal samples for each subfamily.
- **[Bodu.IO.Hashing guides](~/guides/io-hashing/index.md)** — per-algorithm walk-throughs.
- **[Algorithm families](~/docs/algorithm-families.md)** — fingerprint vs checksum vs check digit, plus the cryptographic families.

## Key types

**Fingerprints — `Bodu.IO.Hashing`**

- <xref:Bodu.IO.Hashing.Fnv1a32> / <xref:Bodu.IO.Hashing.Fnv1a64>, <xref:Bodu.IO.Hashing.Fnv132> / <xref:Bodu.IO.Hashing.Fnv164> — constant-memory streaming FNV; prefer `Fnv1a*` for better avalanche.
- <xref:Bodu.IO.Hashing.CityHash32> / <xref:Bodu.IO.Hashing.CityHash64> / <xref:Bodu.IO.Hashing.CityHash128> — SIMD-friendly hashes optimised for long inputs.
- <xref:Bodu.IO.Hashing.MurmurHash3_32> / <xref:Bodu.IO.Hashing.MurmurHash3_128> — seeded high-avalanche fingerprints.
- <xref:Bodu.IO.Hashing.Pearson> — table-driven hash with output widths from 8 to 2048 bits in 8-bit steps; five built-in permutation tables via <xref:Bodu.IO.Hashing.PearsonTableType>.
- <xref:Bodu.IO.Hashing.Bernstein>, <xref:Bodu.IO.Hashing.BKDR>, <xref:Bodu.IO.Hashing.SDBM>, <xref:Bodu.IO.Hashing.JSHash>, <xref:Bodu.IO.Hashing.Elf64>, <xref:Bodu.IO.Hashing.ApHash>, <xref:Bodu.IO.Hashing.Pjw32>, <xref:Bodu.IO.Hashing.SuperFastHash> — classic string hashes from compilers and early web tooling.
- <xref:Bodu.IO.Hashing.BlockNonCryptographicHashAlgorithm`1> — abstract CRTP base for buffered block-oriented algorithms.
- <xref:Bodu.IO.Hashing.IResumableHashAlgorithm> — optional contract that reverse-finalises a stored digest and continues appending; implemented by <xref:Bodu.IO.Hashing.Checksums.Crc>.

**Checksums — `Bodu.IO.Hashing.Checksums`**

- <xref:Bodu.IO.Hashing.Checksums.Crc> — the single CRC engine. Configured with a <xref:Bodu.IO.Hashing.Checksums.CrcStandard>, it handles widths from 1 to 64 bits, honours polynomial, initial value, input / output reflection, and final XOR, and ships with a shared lookup-table cache.
- <xref:Bodu.IO.Hashing.Checksums.CrcStandard> — an immutable parameter set: name, width, polynomial, initial value, reflect-in, reflect-out, XOR-out. Exposes common standards as named properties (`CRC32_ISOHDLC`, `CRC32_ISCSI`, `CRC16_MODBUS`, `CRC64_XZ`, …) and provides `FromName` / `TryFromName` over canonical names and published aliases.
- <xref:Bodu.IO.Hashing.Checksums.CrcStandards> — an enum covering every canonical CRC RevEng entry (113 standards as of the last catalogue fetch).
- <xref:Bodu.IO.Hashing.Checksums.CrcLookupTableCache> — thread-safe cache of 256-entry lookup tables, keyed by (width, polynomial, reflect-in), shared process-wide through <xref:Bodu.IO.Hashing.Checksums.Crc.GlobalCache>.
- <xref:Bodu.IO.Hashing.Checksums.CrcLookupTableBuilder> — builds a lookup table from parameters; used by the cache on first miss.
- <xref:Bodu.IO.Hashing.Checksums.Fletcher16> / <xref:Bodu.IO.Hashing.Checksums.Fletcher32> / <xref:Bodu.IO.Hashing.Checksums.Fletcher64> — twin-accumulator position-dependent checksums.
- <xref:Bodu.IO.Hashing.Checksums.Adler32> / <xref:Bodu.IO.Hashing.Checksums.Adler32C> / <xref:Bodu.IO.Hashing.Checksums.Adler64> — Adler-32 (canonical zlib), Adler-32C (SIMD), Adler-64.
- <xref:Bodu.IO.Hashing.Checksums.Iban>, <xref:Bodu.IO.Hashing.Checksums.Isbn10>, <xref:Bodu.IO.Hashing.Checksums.Isbn13>, <xref:Bodu.IO.Hashing.Checksums.Sedol>, <xref:Bodu.IO.Hashing.Checksums.Cusip>, <xref:Bodu.IO.Hashing.Checksums.Lei>, <xref:Bodu.IO.Hashing.Checksums.Iso7064Mod11_2>, <xref:Bodu.IO.Hashing.Checksums.Iso7064Mod97_10> — multi-character and alphanumeric identifier checksums.

**Check digits — `Bodu.IO.Hashing.CheckDigits`**

- <xref:Bodu.IO.Hashing.CheckDigits.Luhn>, <xref:Bodu.IO.Hashing.CheckDigits.Damm>, <xref:Bodu.IO.Hashing.CheckDigits.Verhoeff> — decimal-alphabet single-character check-digit algorithms with progressively wider error coverage.
- <xref:Bodu.IO.Hashing.CheckDigits.Ean8>, <xref:Bodu.IO.Hashing.CheckDigits.Ean13>, <xref:Bodu.IO.Hashing.CheckDigits.Gtin14>, <xref:Bodu.IO.Hashing.CheckDigits.UpcA> — retail / shipping barcodes.
- <xref:Bodu.IO.Hashing.CheckDigits.Isin>, <xref:Bodu.IO.Hashing.CheckDigits.AbaRoutingNumber> — securities and bank-routing identifiers.
- <xref:Bodu.IO.Hashing.CheckDigits.CheckDigitAlgorithm>, <xref:Bodu.IO.Hashing.CheckDigits.AlphanumericCheckDigitAlgorithm>, <xref:Bodu.IO.Hashing.CheckDigits.MultiCharCheckDigitAlgorithm> — abstract bases, extension points for custom schemes.

**Extensions — `Bodu.IO.Hashing.Extensions`**

- <xref:Bodu.IO.Hashing.Extensions.NonCryptographicHashAlgorithmExtensions> — `AppendData`, `AppendDataAsync`, `ComputeHash`, `ComputeHashAsync`, `VerifyHash`, `VerifyHashAsync`, `TryVerifyHash`, `TryVerifyHashAsync`.

## Example

```csharp
using System.Text;
using Bodu.IO.Hashing;
using Bodu.IO.Hashing.Checksums;

byte[] data = Encoding.UTF8.GetBytes("the quick brown fox");

// CRC-32/ISO-HDLC — the canonical zlib / PNG / Ethernet CRC.
using var crc = new Crc(CrcStandard.CRC32_ISOHDLC);
crc.Append(data);
string crc32 = Convert.ToHexString(crc.GetCurrentHash());

// Fletcher-32 — position-dependent, drops into anything that takes a NonCryptographicHashAlgorithm.
using var fletcher = new Fletcher32();
fletcher.Append(data);
string fl32 = Convert.ToHexString(fletcher.GetCurrentHash());

// Resume a CRC from a previously stored digest and keep hashing.
byte[] previous = crc.GetCurrentHash();
byte[] combined = crc.ComputeHashFrom(previous, Encoding.UTF8.GetBytes(" jumps over"));
```

## Notes

- **Not cryptographically secure.** Every algorithm here is designed for error detection and hash-table distribution, not authentication. An attacker who can choose the input can trivially forge the output. Pair with a MAC or signature if integrity against an adversary matters — see <xref:Bodu.Security.Cryptography.SipHash64> for a keyed short-input hash, or `System.Security.Cryptography.SHA256` for a full cryptographic digest.
- **Shared lookup tables.** <xref:Bodu.IO.Hashing.Checksums.Crc> instances with identical (width, polynomial, reflect-in) triples share a single 256-entry lookup table through <xref:Bodu.IO.Hashing.Checksums.Crc.GlobalCache>. Constructing a hundred `Crc(CrcStandard.CRC32_ISOHDLC)` instances allocates one table, not a hundred.
- **Non-destructive `GetCurrentHash`.** Calling `NonCryptographicHashAlgorithm.GetCurrentHash` snapshots the accumulator and applies the final reflect / XOR / width-mask on the copy, so in-progress hashing is not disturbed. Call it as many times as you like.
- **Resumable.** <xref:Bodu.IO.Hashing.Checksums.Crc> implements <xref:Bodu.IO.Hashing.IResumableHashAlgorithm> — reverse-finalise a stored digest, append further data, re-finalise. Handy for chunked streams where re-reading earlier bytes is expensive.
- **Determinism and portability.** All algorithms produce identical byte-for-byte output across platforms and architectures for the same input and configuration.
- **See also:** the [Using CRC](~/guides/io-hashing/crc.md) and [Using Fletcher](~/guides/io-hashing/fletcher.md) guides, the [full CRC catalogue](~/guides/io-hashing/crc-catalogue.md), the [Bodu.IO.Hashing introduction](~/docs/io-hashing/index.md), and the [Algorithm families](~/docs/algorithm-families.md) overview.
