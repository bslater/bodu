---
title: Guides
---

# Guides

Recipe-style walk-throughs and conceptual introductions for every library in the Bodu suite. Each library's guide section is organized by **namespace**, with one walk-through per headline type.

If you are new to Bodu, start with the [introduction](../docs/introduction.md) for the project overview, the [getting-started page](../docs/getting-started.md) for install commands, or the [algorithm families](../docs/algorithm-families.md) page if you need to choose between hashing or cryptography types that sound similar.

## Bodu.Core

General-purpose building blocks: bounded collections, eviction-aware caches, day-of-week patterns, date and numeric extensions.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="core/index.md">Overview</a></h3>
  <p>Namespace map (<code>Bodu.Collections.Generic</code>, <code>Bodu</code>, <code>Bodu.Extensions</code>) — key types and which guide covers each.</p>
</div>

<div class="bodu-card">
  <h3><a href="core/circular-buffer.md">Circular buffer</a></h3>
  <p>Fixed-capacity FIFO ring buffer — single-threaded and thread-safe variants, overwrite mode, peek / dequeue / try-enqueue patterns.</p>
</div>

<div class="bodu-card">
  <h3><a href="core/deque.md">Deque</a></h3>
  <p>Double-ended queue with O(1) add and remove at both ends; growable or fixed-capacity.</p>
</div>

<div class="bodu-card">
  <h3><a href="core/evicting-dictionary.md">Evicting dictionary</a></h3>
  <p>Capacity-bounded key-value store with FIFO, LRU, LFU, MRU, Random, and Second-Chance eviction policies.</p>
</div>

<div class="bodu-card">
  <h3><a href="core/week-pattern.md">WeekPattern</a></h3>
  <p>Immutable bitmask value type for day-of-week sets — composition, parsing, bitwise operators.</p>
</div>

</div>

[Bodu.Collections.Generic API reference](../apidoc/Bodu.Collections.Generic.md)

---

## Bodu.IO.Hashing

Non-cryptographic hashing — fingerprints, checksums, and check digits — built on the BCL <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType> contract. Nothing here is safe against an adversary; everything is fast and portable.

<div class="bodu-cards">

<div class="bodu-card">
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
  <p>32- and 128-bit MurmurHash3 — seeded, excellent avalanche.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/pearson.md">Using Pearson</a></h3>
  <p>Table-driven hash with output widths from 8 to 2048 bits.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/string-hashes.md">Classic string hashes</a></h3>
  <p>Bernstein, BKDR, SDBM, JSHash, Elf64, ApHash, PJW, SuperFastHash.</p>
</div>

</div>

> For xxHash specifically, use `System.IO.Hashing.XxHash32` / `XxHash64` / `XxHash3` / `XxHash128` from the BCL — Bodu does not duplicate them.

### `Bodu.IO.Hashing.Checksums` — Checksums

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="io-hashing/crc.md">Using CRC</a></h3>
  <p>One engine, 113 named standards (CRC-1 through CRC-64), custom parameter sets, shared lookup-table cache.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/crc-catalogue.md">CRC catalogue</a></h3>
  <p>Reference table of every named CRC standard — name, width, polynomial, init, reflect, XOR-out.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/fletcher.md">Using Fletcher</a></h3>
  <p>Twin-accumulator checksums in 16-, 32-, and 64-bit widths.</p>
</div>

<div class="bodu-card">
  <h3><a href="io-hashing/adler.md">Using Adler</a></h3>
  <p>Adler-32 (zlib), Adler-32C (SIMD), Adler-64.</p>
</div>

</div>

### `Bodu.IO.Hashing.CheckDigits`

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="io-hashing/check-digits.md">Check digits overview</a></h3>
  <p>Luhn, Damm, Verhoeff, EAN, GTIN, ISIN, IBAN, ISBN, SEDOL, CUSIP, ABA routing, LEI — single- and multi-character validators for human-typed identifiers.</p>
</div>

</div>

[Bodu.IO.Hashing API reference](../apidoc/Bodu.IO.Hashing.md)

---

## Bodu.Security.Cryptography

Cryptographic primitives with a formal adversary model — block ciphers, AEAD constructions, keyed and unkeyed hashes — derived from the standard BCL base classes (`SymmetricAlgorithm`, `HashAlgorithm`).

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="cryptography/index.md">Overview</a></h3>
  <p>Namespace map and selection table for cipher, hash, and AEAD families.</p>
</div>

</div>

### Foundations

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="cryptography/encryption-basics.md">Encryption basics</a></h3>
  <p>Key, IV, Tweak, BlockMode, Padding — the mental model every cipher in the library follows.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/cipher-modes.md">Cipher block modes</a></h3>
  <p>ECB, CBC, CFB, OFB, CTR — one worked round-trip per mode.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/padding.md">Padding</a></h3>
  <p>PKCS7, Zeros, None, ISO 10126, ISO 7816-4, ANSI X9.23 — how each one pads and when it round-trips cleanly.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/composing-primitives.md">Composing primitives</a></h3>
  <p><code>IBlockCipher</code> + <code>BlockCipherModeFactory</code> + <code>PaddingFactory</code> vs the <code>SymmetricAlgorithm</code> wrappers.</p>
</div>

</div>

### Symmetric ciphers — Standard

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="cryptography/skipjack.md">Using Skipjack</a></h3>
  <p>NSA design (declassified 1998); legacy interoperability only.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/blowfish.md">Using Blowfish</a></h3>
  <p>Schneier 1993; 64-bit block; expensive key schedule.</p>
</div>

</div>

`Camellia`, `Twofish`, and `Serpent128` follow the same `SymmetricAlgorithm` lifecycle — see the [Bodu.Security.Cryptography API reference](../apidoc/Bodu.Security.Cryptography.md) for their parameters.

### Symmetric ciphers — Tweakable

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="cryptography/threefish-256.md">Using Threefish-256</a></h3>
  <p>Smallest Threefish variant; 256-bit block, 256-bit key, 128-bit tweak.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/threefish-512.md">Using Threefish-512</a></h3>
  <p>Recommended general-purpose Threefish variant; 512-bit block, 512-bit key, 128-bit tweak.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/threefish-1024.md">Using Threefish-1024</a></h3>
  <p>Highest Threefish security margin; 1024-bit block, 1024-bit key, 128-bit tweak.</p>
</div>

</div>

`Serpent256` / `Serpent512` / `Serpent1024` are wide-block tweakable Serpent constructions — non-standard, see the [API reference](../apidoc/Bodu.Security.Cryptography.md) for their parameters.

### Symmetric ciphers — AEAD

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="cryptography/aead-modes.md">AEAD modes</a></h3>
  <p>GCM, CCM, OCB, EAX, SIV, GCM-SIV — authenticated encryption using <code>AesBlockCipher</code> + a mode transform.</p>
</div>

</div>

### Cryptographic hashes

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="cryptography/hashing.md">Hashing overview</a></h3>
  <p>Cross-cutting overview of keyed hashes, cryptographic digests, and Merkle trees.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/tiger.md">Using Tiger</a></h3>
  <p>128 / 160 / 192-bit cryptographic digest optimized for 64-bit platforms.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/cubehash.md">Using CubeHash</a></h3>
  <p>SHA-3 finalist with tunable rounds and block size.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/snefru.md">Using Snefru</a></h3>
  <p>Legacy cryptographic digest; interop only (cryptanalytically broken).</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/merkle-trees.md">Using Merkle trees</a></h3>
  <p>Tree-structured streaming integrity over any inner <code>HashAlgorithm</code>.</p>
</div>

</div>

`Whirlpool`, `Blake2b`, `Blake2s`, `Blake3`, `Skein256` / `Skein512` / `Skein1024`, and `Shake` ship without dedicated walk-throughs — consult the [API reference](../apidoc/Bodu.Security.Cryptography.md) directly.

### Keyed hashes (MAC)

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="cryptography/siphash.md">Using SipHash</a></h3>
  <p>SipHash-64 / SipHash-128 — keyed PRF for hash-flooding-resistant tables.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/poly1305.md">Using Poly1305</a></h3>
  <p>One-time authenticator (RFC 8439); pair with ChaCha20 or AES-CTR.</p>
</div>

</div>

### ASCON family

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="cryptography/ascon.md">ASCON overview</a></h3>
  <p>All five NIST SP 800-232 types with selection guidance.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/ascon-hashing.md">ASCON hashing</a></h3>
  <p><code>AsconHash256</code> and <code>AsconHashA256</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/ascon-xof.md">ASCON XOF</a></h3>
  <p><code>AsconXof128</code> and <code>AsconCxof128</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="cryptography/ascon-aead.md">ASCON AEAD</a></h3>
  <p><code>AsconAead128</code> — sponge-based authenticated encryption.</p>
</div>

</div>

[Bodu.Security.Cryptography API reference](../apidoc/Bodu.Security.Cryptography.md)

---

## Bodu.Globalization.Calendar

Rule-driven notable-date (public holiday, observance, festival) resolution for any year, territory, or calendar system.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="calendar/index.md">Overview</a></h3>
  <p>Resolution pipeline, namespace map, and key-type table.</p>
</div>

<div class="bodu-card">
  <h3><a href="calendar/notable-dates.md">Using NotableDateService</a></h3>
  <p>Resolving for a year, filtering by territory and category, layering overrides.</p>
</div>

<div class="bodu-card">
  <h3><a href="calendar/rule-authoring.md">Authoring notable-date rules</a></h3>
  <p>In-code, embedded XML / JSON, companion assemblies, runtime overrides.</p>
</div>

<div class="bodu-card">
  <h3><a href="calendar/algorithms.md">Date calculation algorithms</a></h3>
  <p>Built-in algorithms (Easter, Hindu Lunar, Losar, Vesak, Asalha Puja, Qingming) and custom-algorithm walk-through.</p>
</div>

<div class="bodu-card">
  <h3><a href="calendar/data-packs.md">Calendar data packs</a></h3>
  <p>Official Americas / Europe / Asia-Pacific companion assemblies.</p>
</div>

</div>

[Bodu.Globalization.Calendar API reference](../apidoc/Bodu.Globalization.Calendar.md)

---

## Bodu.Text.Encoding

Binary-to-text encoders for **Base16**, **Base32**, **Base64**, **Base58**, and **Base85** with every common
variant — span- and UTF-8-friendly, `OperationStatus`-aware, with a unified `IBinaryEncoding` interface for
runtime-pluggable encoding choice.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="text-encoding/index.md">Overview</a></h3>
  <p>The encoding family, payload-expansion comparison, and the choose-an-encoding decision table.</p>
</div>

<div class="bodu-card">
  <h3><a href="text-encoding/base16.md">Using Base16 (hexadecimal)</a></h3>
  <p>Formatting decorations (case / prefix / spacing / line breaks), lenient parsing, hex dumps, BCL aliases.</p>
</div>

<div class="bodu-card">
  <h3><a href="text-encoding/base32.md">Using Base32</a></h3>
  <p>Standard / HexExtended / Crockford / Z-Base-32 variants, TOTP secrets, padding control.</p>
</div>

<div class="bodu-card">
  <h3><a href="text-encoding/base64.md">Using Base64</a></h3>
  <p>Standard / URL-safe / MIME variants, JWT decoding, 76-character line wrapping.</p>
</div>

<div class="bodu-card">
  <h3><a href="text-encoding/base58.md">Using Base58</a></h3>
  <p>Bitcoin / Flickr / Ripple alphabets, leading-zero preservation, address decoding.</p>
</div>

<div class="bodu-card">
  <h3><a href="text-encoding/base85.md">Using Base85 (Ascii85 and Z85)</a></h3>
  <p>Adobe Ascii85 with the <code>z</code> shortcut and partial-group rules; ZeroMQ Z85 with shell-safe alphabet.</p>
</div>

<div class="bodu-card">
  <h3><a href="text-encoding/binary-encodings-interface.md">The IBinaryEncoding interface</a></h3>
  <p>Runtime-selected encoding choice via <code>BinaryEncodings.Get(name)</code> and the <code>IBinaryEncoding</code> contract.</p>
</div>

</div>
