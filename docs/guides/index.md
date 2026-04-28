---
title: Guides
---

# Guides

Recipe-style walk-throughs and conceptual introductions for every library in the Bodu suite. Pick a library or start with the cross-library overview if you are not yet sure which primitive fits your problem.

## Start here

**[Algorithm families](algorithm-families.md)** — understand the six algorithm families (fingerprints, checksums, check digits, cryptographic hashes, keyed hashes/MACs, and symmetric ciphers) and how they relate across the two libraries. Start here if you are new to Bodu or need to choose between types that sound similar.

---

## Bodu.IO.Hashing

Non-cryptographic checksums, fingerprints, and identity-validation algorithms built on the BCL <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType> contract. Nothing in this package is safe against an adversary — it is optimised for error detection, distribution, and speed.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="io-hashing/">Overview</a></h3>
  <p>Algorithm-selection table, the common <code>Append / GetCurrentHash / Reset</code> lifecycle, and cross-references to the Security.Cryptography package for when you need more.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/crc.html">CRC</a></h3>
  <p>One engine, 113 named standards (CRC-8 through CRC-64), custom parameter sets, shared lookup-table cache, and resumable hashing.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/fletcher.html">Fletcher</a></h3>
  <p>Twin-accumulator checksums in 16-, 32-, and 64-bit widths. Catches transpositions that simple additive sums miss.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/adler.html">Adler</a></h3>
  <p>Prime-modulus twin accumulator. Adler-32 is the zlib checksum; Adler-32C adds SIMD-friendly throughput; Adler-64 extends the output width.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/fnv.html">FNV</a></h3>
  <p>FNV-1 and FNV-1a at 32- and 64-bit widths. Simple, constant-memory, portable fingerprint for in-memory hash tables.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/cityhash.html">CityHash</a></h3>
  <p>Google's SIMD-friendly fingerprint — 32-, 64-, and 128-bit outputs. Fastest option for large buffers at the cost of in-memory buffering.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/murmurhash3.html">MurmurHash3</a></h3>
  <p>Austin Appleby's MurmurHash3 — 32-bit and x64-128-bit variants. Seeded, excellent avalanche and distribution.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/xxhash.html">XxHash</a></h3>
  <p>Yann Collet's xxHash family — XxHash32, XxHash64. High-throughput fingerprints with optional seed.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/pearson.html">Pearson</a></h3>
  <p>Table-driven hash with output widths from 8 bits to 2048 bits and five built-in permutation tables.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/string-hashes.html">Classic string hashes</a></h3>
  <p>Bernstein (djb2), BKDR, SDBM, JSHash, Elf64, ApHash, Pjw32 — one-liner hash functions from compilers, textbooks, and early web servers.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/check-digits.html">Check digits</a></h3>
  <p>Luhn, Damm, Verhoeff, EAN, GTIN, ISIN, IBAN, ISBN, SEDOL, CUSIP, ABA routing, LEI — single-character validators for human-readable identifiers.</p>
</div>

</div>

---

## Bodu.Security.Cryptography

Cryptographic primitives — block ciphers, authenticated encryption, keyed hashes, and cryptographic digests — all carrying an adversary model. Types derive from the standard BCL base classes (`SymmetricAlgorithm`, `HashAlgorithm`) and integrate with the existing .NET cryptography infrastructure.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="cryptography/">Overview</a></h3>
  <p>Library overview, cipher and hash selection tables, AEAD families, and cross-references to Bodu.IO.Hashing for non-cryptographic alternatives.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/encryption-basics.html">Encryption basics</a></h3>
  <p>Key, IV, Tweak, BlockMode, Padding — the mental model every cipher in the library follows, plus lazy key generation, extension methods, and disposal.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/cipher-modes.html">Cipher block modes</a></h3>
  <p>ECB, CBC, CFB, OFB, CTR — one worked round-trip per mode with notes on when each is appropriate.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/aead-modes.html">AEAD modes</a></h3>
  <p>GCM, CCM, OCB3, SIV, GCM-SIV — authenticated encryption with associated data using AES.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/ascon.html">ASCON family</a></h3>
  <p>All five NIST SP 800-232 types — Hash256, HashA256, XOF128, CXOF128, and AEAD128 — with selection guidance.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/hashing.html">Hashing</a></h3>
  <p>Cross-cutting overview of keyed hashes (SipHash, Poly1305), cryptographic digests (Tiger, CubeHash, Snefru), and Merkle trees.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/threefish-256.html">Threefish</a></h3>
  <p>Tweakable block ciphers in 256-, 512-, and 1024-bit variants. Core of the Skein family; per-record domain separation via the 128-bit tweak.</p>
</div>

</div>
