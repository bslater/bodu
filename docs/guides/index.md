---
title: Guides
---

# Guides

Recipe-style walk-throughs and conceptual introductions for every library in the Bodu suite. Each library's guide section is organised by **namespace**, with one walk-through per headline type.

If you are new to Bodu, start with the [Introduction section](../docs/introduction.md) for the project overview, the [Getting started page](../docs/getting-started.md) for install commands, or the [Algorithm families](../docs/algorithm-families.md) page if you need to choose between hashing or cryptography types that sound similar.

## Bodu.Core

General-purpose building blocks: bounded collections, eviction-aware caches, day-of-week patterns, date and numeric extensions.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="core/">Overview</a></h3>
  <p>Namespace map (<code>Bodu.Collections.Generic</code>, <code>Bodu</code>) — key types and which guide covers each.</p>
</div>

<div class="bodu-card">
  <h3><a href="core/circular-buffer.html">Circular buffer</a></h3>
  <p>Fixed-capacity FIFO ring buffer — single-threaded and thread-safe variants, overwrite mode, peek/dequeue/try-enqueue patterns.</p>
</div>

<div class="bodu-card">
  <h3><a href="core/deque.html">Deque</a></h3>
  <p>Double-ended queue with O(1) add and remove at both ends; growable or fixed-capacity.</p>
</div>

<div class="bodu-card">
  <h3><a href="core/evicting-dictionary.html">Evicting dictionary</a></h3>
  <p>Capacity-bounded key-value store with FIFO, LRU, and LFU eviction.</p>
</div>

<div class="bodu-card">
  <h3><a href="core/week-pattern.html">WeekPattern</a></h3>
  <p>Immutable bitmask value type for day-of-week sets — composition, parsing, bitwise operators.</p>
</div>

</div>

---

## Bodu.IO.Hashing

Non-cryptographic hashing — fingerprints, checksums, and check digits — built on the BCL <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType> contract. Nothing here is safe against an adversary; everything is fast and portable.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="io-hashing/index.md">Overview</a></h3>
  <h3><a href="io-hashing/">Overview</a></h3>
  <p>Namespace map (<code>Bodu.IO.Hashing</code>, <code>.Checksums</code>, <code>.CheckDigits</code>) — key types and which guide covers each.</p>
</div>

</div>

### `Bodu.IO.Hashing` — Fingerprints

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="io-hashing/fnv.html">Using FNV</a></h3>
  <p>FNV-1 and FNV-1a at 32- and 64-bit widths — the textbook constant-memory fingerprint.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/cityhash.html">Using CityHash</a></h3>
  <p>32-, 64-, and 128-bit Google CityHash — SIMD-friendly fingerprint for long inputs.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/murmurhash3.html">Using MurmurHash3</a></h3>
  <p>32- and x64-128-bit MurmurHash3 — seeded, excellent avalanche.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/xxhash.html">Using XxHash</a></h3>
  <p>XxHash32 and XxHash64 — high-throughput seeded fingerprints.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/pearson.html">Using Pearson</a></h3>
  <p>Table-driven hash with output widths from 8 to 2048 bits.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/string-hashes.html">Classic string hashes</a></h3>
  <p>Bernstein, BKDR, SDBM, JSHash, Elf64, ApHash, PJW.</p>
</div>

</div>

### `Bodu.IO.Hashing.Checksums` — Checksums

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="io-hashing/crc.html">Using CRC</a></h3>
  <p>One engine, 113 named standards (CRC-8 through CRC-64), custom parameter sets, shared lookup-table cache.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/crc-catalogue.html">CRC catalogue</a></h3>
  <p>Reference table of every named CRC standard — name, width, polynomial, init, reflect, XOR-out.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/fletcher.html">Using Fletcher</a></h3>
  <p>Twin-accumulator checksums in 16-, 32-, and 64-bit widths.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/adler.html">Using Adler</a></h3>
  <p>Adler-32 (zlib), Adler-32C (SIMD), Adler-64.</p>
</div>

</div>

### `Bodu.IO.Hashing.CheckDigits`

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="io-hashing/check-digits.html">Check digits overview</a></h3>
  <p>Luhn, Damm, Verhoeff, EAN, GTIN, ISIN, IBAN, ISBN, SEDOL, CUSIP, ABA routing, LEI — single-character validators for human-typed identifiers.</p>
</div>

</div>

---

## Bodu.Security.Cryptography

Cryptographic primitives with a formal adversary model — block ciphers, AEAD constructions, keyed and unkeyed hashes — derived from the standard BCL base classes (`SymmetricAlgorithm`, `HashAlgorithm`).

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="cryptography/">Overview</a></h3>
  <p>Namespace map and selection table for cipher, hash, and AEAD families.</p>
</div>

</div>

### Foundations

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="cryptography/encryption-basics.html">Encryption basics</a></h3>
  <p>Key, IV, Tweak, BlockMode, Padding — the mental model every cipher in the library follows.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/cipher-modes.html">Cipher block modes</a></h3>
  <p>ECB, CBC, CFB, OFB, CTR — one worked round-trip per mode.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/padding.html">Padding</a></h3>
  <p>PKCS7, Zeros, None — how each one pads and when it round-trips cleanly.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/composing-primitives.html">Composing primitives</a></h3>
  <p><code>IBlockCipher</code> + <code>BlockCipherModeFactory</code> + <code>PaddingFactory</code> vs the <code>SymmetricAlgorithm</code> wrappers.</p>
</div>

</div>

### Symmetric ciphers — Standard

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="cryptography/skipjack.html">Using Skipjack</a></h3>
  <p>NSA design (declassified 1998); legacy interoperability only.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/blowfish.html">Using Blowfish</a></h3>
  <p>Schneier 1993; 64-bit block; expensive key schedule.</p>
</div>

</div>

### Symmetric ciphers — Tweakable

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="cryptography/threefish-256.html">Using Threefish-256</a></h3>
  <p>Smallest Threefish variant; 128-bit tweak.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/threefish-512.html">Using Threefish-512</a></h3>
  <p>Recommended general-purpose Threefish variant; 128-bit tweak.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/threefish-1024.html">Using Threefish-1024</a></h3>
  <p>Highest Threefish security margin; 128-bit tweak.</p>
</div>

</div>

### Symmetric ciphers — AEAD

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="cryptography/aead-modes.html">AEAD modes</a></h3>
  <p>GCM, CCM, OCB3, EAX, SIV, GCM-SIV — authenticated encryption using AES.</p>
</div>

</div>

### Cryptographic hashes

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="cryptography/hashing.html">Hashing overview</a></h3>
  <p>Cross-cutting overview of keyed hashes, cryptographic digests, and Merkle trees.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/tiger.html">Using Tiger</a></h3>
  <p>128 / 160 / 192-bit cryptographic digest optimised for 64-bit platforms.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/cubehash.html">Using CubeHash</a></h3>
  <p>SHA-3 finalist with tunable rounds and block size.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/snefru.html">Using Snefru</a></h3>
  <p>Legacy cryptographic digest; interop only (cryptanalytically broken).</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/merkle-trees.html">Using Merkle trees</a></h3>
  <p>Tree-structured streaming integrity over any inner <code>HashAlgorithm</code>.</p>
</div>

</div>

### Keyed hashes (MAC)

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="cryptography/siphash.html">Using SipHash</a></h3>
  <p>SipHash-64 / SipHash-128 — keyed PRF for hash-flooding-resistant tables.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/poly1305.html">Using Poly1305</a></h3>
  <p>One-time authenticator (RFC 8439); pair with ChaCha20 or AES-CTR.</p>
</div>

</div>

### ASCON family

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="cryptography/ascon.html">ASCON overview</a></h3>
  <p>All five NIST SP 800-232 types with selection guidance.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/ascon-hashing.html">ASCON hashing</a></h3>
  <p><code>AsconHash256</code> and <code>AsconHashA256</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/ascon-xof.html">ASCON XOF</a></h3>
  <p><code>AsconXof128</code> and <code>AsconCxof128</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/ascon-aead.html">ASCON AEAD</a></h3>
  <p><code>AsconAead128</code> — sponge-based authenticated encryption.</p>
</div>

</div>

---

## Bodu.Globalization.Calendar

Rule-driven notable date (public holiday, observance, festival) resolution for any year, territory, or calendar system.
  <h3><a href="io-hashing/index.md">Overview</a></h3>
  <p>Namespace map (<code>Bodu.IO.Hashing</code>, <code>.Checksums</code>, <code>.CheckDigits</code>) — key types and which guide covers each.</p>
</div>

</div>

### `Bodu.IO.Hashing` — Fingerprints

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="io-hashing/fnv.md">Using FNV</a></h3>
  <p>FNV-1 and FNV-1a at 32- and 64-bit widths — the textbook constant-memory fingerprint.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/cityhash.md">Using CityHash</a></h3>
  <p>32-, 64-, and 128-bit Google CityHash — SIMD-friendly fingerprint for long inputs.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/murmurhash3.md">Using MurmurHash3</a></h3>
  <p>32- and x64-128-bit MurmurHash3 — seeded, excellent avalanche.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/xxhash.md">Using XxHash</a></h3>
  <p>XxHash32 and XxHash64 — high-throughput seeded fingerprints.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/pearson.md">Using Pearson</a></h3>
  <p>Table-driven hash with output widths from 8 to 2048 bits.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/string-hashes.md">Classic string hashes</a></h3>
  <p>Bernstein, BKDR, SDBM, JSHash, Elf64, ApHash, PJW.</p>
</div>

</div>

### `Bodu.IO.Hashing.Checksums` — Checksums

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="calendar/">Overview</a></h3>
  <p>Resolution pipeline, namespace map, and key-type table.</p>
</div>

<div class="bodu-card">
  <h3><a href="calendar/notable-dates.html">Using NotableDateService</a></h3>
  <p>Resolving for a year, filtering by territory and category, layering overrides.</p>
</div>

<div class="bodu-card">
  <h3><a href="calendar/rule-authoring.html">Authoring notable date rules</a></h3>
  <p>In-code, embedded XML / JSON, companion assemblies, runtime overrides.</p>
</div>

<div class="bodu-card">
  <h3><a href="calendar/algorithms.html">Date calculation algorithms</a></h3>
  <p>Built-in algorithms (Easter, Hindu Lunar, Losar, Vesak, Asalha Puja, Qingming) and custom-algorithm walk-through.</p>
</div>

<div class="bodu-card">
  <h3><a href="calendar/data-packs.html">Calendar data packs</a></h3>
  <p>Official Americas / Europe / Asia-Pacific companion assemblies.</p>
</div>

</div>
