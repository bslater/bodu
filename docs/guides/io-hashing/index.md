---
title: Bodu.IO.Hashing guides
---

# Bodu.IO.Hashing guides

Recipe-style walk-throughs for **Bodu.IO.Hashing**, organized by namespace. Each guide on this page is a focused walk-through of one algorithm or family.

Part of the **[Hashing & Cryptography](../topics/hashing-and-cryptography.md)** topic.

If you have not yet installed the package or want the high-level shape of the library, start with the [Bodu.IO.Hashing introduction](../../docs/io-hashing/index.md) and the [getting-started page](../../docs/io-hashing/getting-started.md). The introduction's *Choosing a subfamily* section covers the structural differences between fingerprints, checksums, and check digits, and how they relate to the cryptographic families.

For the auto-generated API reference, see the [Bodu.IO.Hashing namespace page](xref:Bodu.IO.Hashing). For keyed or cryptographic hashes (SipHash, Poly1305, Tiger, CubeHash, Merkle trees), see the [Bodu.Security.Cryptography hashing guides](../cryptography/hashing.md).

## Namespace map

| Namespace | What lives here | Guides |
|---|---|---|
| `Bodu.IO.Hashing` | Fingerprints — `Fnv*`, `CityHash*`, `MurmurHash3_32` / `MurmurHash3_128`, `Pearson`, `Bernstein`, `BKDR`, `SDBM`, `JSHash`, `Elf64`, `ApHash`, `Pjw32`, `SuperFastHash`. Plus the streaming contracts `BlockNonCryptographicHashAlgorithm` and `IResumableHashAlgorithm`. | [FNV](fnv.md) · [CityHash](cityhash.md) · [MurmurHash3](murmurhash3.md) · [Pearson](pearson.md) · [Classic string hashes](string-hashes.md) |
| `Bodu.IO.Hashing.Checksums` | Polynomial-remainder and twin-accumulator checksums — `Crc` + `CrcStandard` + `CrcStandards`, `Fletcher16/32/64`, `Adler32` / `Adler32C` / `Adler64`, plus the `CrcLookupTableBuilder` / `CrcLookupTableCache` table machinery. | [CRC](crc.md) · [CRC catalogue](crc-catalogue.md) · [Fletcher](fletcher.md) · [Adler](adler.md) · [Streaming, async, and resumable hashing](streaming-and-async.md) |
| `Bodu.IO.Hashing.CheckDigits` | Every check-digit algorithm — decimal single-character (`Luhn`, `Damm`, `Verhoeff`, `Ean8`, `Ean13`, `Gtin14`, `UpcA`, `Isbn13`, `AbaRoutingNumber`), alphanumeric single-character (`Isin`, `Isbn10`, `Sedol`, `Cusip`, `Iso7064Mod11_2`, `Code39Mod43`, `Crockford32`), and multi-character (`Iban`, `Lei`, `Iso7064Mod97_10`). Plus the root `CheckValueAlgorithm` and its abstract bases `CheckDigitAlgorithm`, `AlphanumericCheckDigitAlgorithm`, `MultiCharCheckDigitAlgorithm`. | [Check digits overview](check-digits.md) · [Alphanumeric and encoded check digits](alphanumeric-check-digits.md) |
| `Bodu.IO.Hashing.Extensions` | One-shot, async, and verify helpers over `NonCryptographicHashAlgorithm`. | [Streaming, async, and resumable hashing](streaming-and-async.md) |

> **BCL note.** `XxHash32`, `XxHash64`, `XxHash3`, and `XxHash128` ship in `System.IO.Hashing` from .NET 6 onwards. Bodu does not duplicate them — use the BCL types directly when you want xxHash.

## Guides

### `Bodu.IO.Hashing` — Fingerprints

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="fnv.md">Using FNV</a></h3>
  <p>FNV-1 and FNV-1a at 32 and 64 bits — simple, fast, textbook fingerprint for in-memory hash tables.</p>
</div>

<div class="bodu-card">
  <h3><a href="cityhash.md">Using CityHash</a></h3>
  <p>32-, 64-, and 128-bit CityHash — Google's SIMD-friendly fingerprint for long inputs.</p>
</div>

<div class="bodu-card">
  <h3><a href="murmurhash3.md">Using MurmurHash3</a></h3>
  <p>Austin Appleby's MurmurHash3 — <code>MurmurHash3_32</code> and <code>MurmurHash3_128</code>; seeded with excellent avalanche.</p>
</div>

<div class="bodu-card">
  <h3><a href="pearson.md">Using Pearson</a></h3>
  <p>Pearson's table-driven hash with output widths from 8 to 2048 bits.</p>
</div>

<div class="bodu-card">
  <h3><a href="string-hashes.md">Classic string hashes</a></h3>
  <p>Bernstein (djb2), BKDR, SDBM, JSHash, Elf64, ApHash, PJW, SuperFastHash.</p>
</div>

</div>

### `Bodu.IO.Hashing.Checksums`

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="crc.md">Using CRC</a></h3>
  <p>The <code>Crc</code> engine and <code>CrcStandard</code>; <code>CrcStandards</code> enum; named lookups; custom parameter sets; lookup-table caches; resumable hashing.</p>
</div>

<div class="bodu-card">
  <h3><a href="crc-catalogue.md">CRC catalogue</a></h3>
  <p>The full table of named CRC standards from the RevEng catalogue — name, width, class, enum value, and aliases.</p>
</div>

<div class="bodu-card">
  <h3><a href="fletcher.md">Using Fletcher</a></h3>
  <p>Twin-accumulator checksums in 16, 32, and 64 bits — catches transpositions a simple sum or XOR misses.</p>
</div>

<div class="bodu-card">
  <h3><a href="adler.md">Using Adler</a></h3>
  <p>Adler-32 (zlib), Adler-32C (SIMD), Adler-64.</p>
</div>

<div class="bodu-card">
  <h3><a href="streaming-and-async.md">Streaming, async, and resumable hashing</a></h3>
  <p><code>HashingStream</code>, the <code>ComputeHashAsync</code> / <code>VerifyHashAsync</code> / <code>AppendDataAsync</code> extensions, <code>IResumableHashAlgorithm</code> across CRC, FNV, Fletcher, and Adler, and the digest byte-order table.</p>
</div>

</div>

### `Bodu.IO.Hashing.CheckDigits`

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="check-digits.md">Check digits overview</a></h3>
  <p>Luhn, Damm, Verhoeff, EAN, GTIN, UPC, ISIN, ABA routing — single-character validators. Plus IBAN, ISBN, SEDOL, CUSIP, LEI — all in the one <code>Bodu.IO.Hashing.CheckDigits</code> namespace.</p>
</div>

<div class="bodu-card">
  <h3><a href="alphanumeric-check-digits.md">Alphanumeric and encoded check digits</a></h3>
  <p><code>Code39Mod43</code>, <code>Crockford32</code>, <code>Gumm</code>, ISO 7064 MOD 11-2 / MOD 97-10, SEDOL, CUSIP, ISIN, LEI, IBAN under the four-level <code>CheckValueAlgorithm</code> hierarchy; the input / output alphabet enums; authoring a scheme; a measured error-class table.</p>
</div>

</div>

## Common lifecycle

Everything in this package derives from <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType>, so the lifecycle is identical regardless of which algorithm you pick:

```csharp
using Bodu.IO.Hashing;
using Bodu.IO.Hashing.Checksums;

var hash = new Crc();      // or Fletcher32, Adler32, Fnv1a64, CityHash64, …

hash.Append(chunk1);
hash.Append(chunk2);
byte[] partial = hash.GetCurrentHash();   // snapshot, non-destructive
hash.Append(chunk3);
byte[] full    = hash.GetCurrentHash();

hash.Reset();                              // back to the initial state
```

`Crc`, the FNV family (`Fnv132` / `Fnv164` / `Fnv1a32` / `Fnv1a64`), `Fletcher16` / `Fletcher32` / `Fletcher64`, and `Adler32` / `Adler32C` / `Adler64` implement `IResumableHashAlgorithm` — see [Streaming, async, and resumable hashing](streaming-and-async.md) for the pattern across all four families, and `HashingStream` for checksumming as a side effect of stream I/O.

## Where to go next

- [Runnable samples](../../samples/io-hashing.md) — offline sample projects under `samples/IO.Hashing/`: the CRC catalogue, checksum families, streaming/resumable digests, check digits, and a custom scheme with contract tests.
- [Bodu.IO.Hashing introduction](../../docs/io-hashing/index.md) — namespaces, headline types, scenarios.
- [Bodu.IO.Hashing getting started](../../docs/io-hashing/getting-started.md) — install and minimal samples.
- [Bodu.Security.Cryptography hashing guide](../cryptography/hashing.md) — keyed and cryptographic hashes.
- [Bodu.IO.Hashing API reference](xref:Bodu.IO.Hashing) — namespace overview with key types.
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
