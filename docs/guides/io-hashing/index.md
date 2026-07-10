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
| `Bodu.IO.Hashing` | Fingerprints — `Fnv*`, `CityHash*`, `MurmurHash3_32` / `MurmurHash3_128`, `Pearson`, `Bernstein`, `BKDR`, `SDBM`, `JSHash`, `Elf64`, `ApHash`, `Pjw32`, `SuperFastHash`. Plus the streaming contracts `BlockNonCryptographicHashAlgorithm<T>` and `IResumableHashAlgorithm`. | [FNV](fnv.md) · [CityHash](cityhash.md) · [MurmurHash3](murmurhash3.md) · [Pearson](pearson.md) · [Classic string hashes](string-hashes.md) |
| `Bodu.IO.Hashing.Checksums` | Polynomial-remainder and twin-accumulator checksums — `Crc` + `CrcStandard` + `CrcStandards`, `Fletcher16/32/64`, `Adler32` / `Adler32C` / `Adler64`. Multi-character and alphanumeric identifier checksums — `Iban`, `Isbn10`, `Isbn13`, `Sedol`, `Cusip`, `Lei`, `Iso7064Mod11_2`, `Iso7064Mod97_10`. | [CRC](crc.md) · [CRC catalogue](crc-catalogue.md) · [Fletcher](fletcher.md) · [Adler](adler.md) |
| `Bodu.IO.Hashing.CheckDigits` | Single-character check-digit algorithms over decimal alphabets — `Luhn`, `Damm`, `Verhoeff`, `Ean8`, `Ean13`, `Gtin14`, `UpcA`, `Isin`, `AbaRoutingNumber`. Plus the abstract bases `CheckDigitAlgorithm`, `AlphanumericCheckDigitAlgorithm`, `MultiCharCheckDigitAlgorithm`. | [Check digits overview](check-digits.md) |
| `Bodu.IO.Hashing.Extensions` | One-shot, async, and verify helpers over `NonCryptographicHashAlgorithm`. | (covered in the per-algorithm guides) |

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

</div>

### `Bodu.IO.Hashing.CheckDigits`

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="check-digits.md">Check digits overview</a></h3>
  <p>Luhn, Damm, Verhoeff, EAN, GTIN, UPC, ISIN, ABA routing — single-character validators. Plus IBAN, ISBN, SEDOL, CUSIP, LEI from <code>Bodu.IO.Hashing.Checksums</code>.</p>
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

Only `Crc` currently implements `IResumableHashAlgorithm` — see the [CRC guide](crc.md#pattern-6--resume-from-a-stored-digest).

## Where to go next

- [Bodu.IO.Hashing introduction](../../docs/io-hashing/index.md) — namespaces, headline types, scenarios.
- [Bodu.IO.Hashing getting started](../../docs/io-hashing/getting-started.md) — install and minimal samples.
- [Bodu.Security.Cryptography hashing guide](../cryptography/hashing.md) — keyed and cryptographic hashes.
- [Bodu.IO.Hashing API reference](xref:Bodu.IO.Hashing) — namespace overview with key types.
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
