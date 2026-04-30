---
title: Algorithm families
---

# Algorithm families

*Start here if you are new to Bodu, or if you need to choose between types that sound similar.*

The Bodu libraries provide six distinct algorithm families across two namespaces. This page maps those families, explains what distinguishes each from the others, and directs you to the guides that go deeper.

## The families at a glance

![Algorithm taxonomy — family hierarchy across both libraries](../images/diagrams/algorithm-taxonomy.svg)

---

## Non-cryptographic families — `Bodu.IO.Hashing`

These algorithms carry **no adversary model**. They are fast, portable error-detection and distribution tools, not security primitives. An attacker who can modify the input can always forge the result.

### Fingerprints — non-cryptographic hashes

**Namespace:** `Bodu.IO.Hashing`  
**Base class:** `System.IO.Hashing.NonCryptographicHashAlgorithm`

Fingerprint algorithms map an arbitrary byte sequence to a fixed-size integer value in a way that distributes evenly across the output range. The term *hash value* or *fingerprint* is used for the result. The mathematical structure is chosen for **distribution quality and speed**, not for error-detection coverage.

**Designed for:** hash-table bucketing, in-memory cache keys, fast deduplication, distributed routing, content-addressable lookups within a trust boundary.  
**Not designed for:** error-pattern detection (a bit flip in a specific position may not change the output), authentication, or any adversary-facing use.

| Type | Output | Notes |
|---|---|---|
| `Fnv1a32` / `Fnv1a64` | 32 / 64 bits | Simple, constant-memory, portable. Preferred over FNV-1 for better avalanche. |
| `Fnv132` / `Fnv164` | 32 / 64 bits | Original FNV-1 — use only for legacy interoperability. |
| `CityHash32` / `CityHash64` / `CityHash128` | 32 / 64 / 128 bits | SIMD-friendly; fastest option for large inputs, at the cost of in-memory buffering. |
| `MurmurHash3_32` / `MurmurHash3_x64_128` | 32 / 128 bits | Seeded; excellent avalanche and collision resistance for a non-cryptographic hash. |
| `XxHash32` / `XxHash64` | 32 / 64 bits | High-throughput; seeded; non-streaming (buffers input). |
| `Pearson` | 8–2048 bits | Table-driven; configurable output width in 8-bit steps; five built-in permutation tables. |
| `Bernstein` | 32 bits | Classic djb2; configurable add-vs-XOR variant and initial value. |
| `BKDR` / `SDBM` / `JSHash` / `Elf64` / `ApHash` / `Pjw32` | 32–64 bits | Classic string hashes from compilers, databases, and early web tooling. |

→ Guides: [Using FNV](io-hashing/fnv.md) · [Using CityHash](io-hashing/cityhash.md) · [Using MurmurHash3](io-hashing/murmurhash3.md) · [Using XxHash](io-hashing/xxhash.md) · [Using Pearson](io-hashing/pearson.md) · [Classic string hashes](io-hashing/string-hashes.md)

---

### Checksums

**Namespace:** `Bodu.IO.Hashing.Checksums`  
**Base class:** `System.IO.Hashing.NonCryptographicHashAlgorithm`

Checksum algorithms produce a short tag specifically designed to detect common **error patterns** in transmitted or stored data — single-bit flips, burst errors, adjacent transpositions. Their mathematical structure (polynomial remainder, twin-accumulator) is chosen for error-detection coverage over specific error models, not for distribution quality.

**Designed for:** protocol integrity (Ethernet, USB, CAN bus, Modbus, PKZIP, zlib, PNG), storage integrity (NVMe, Btrfs), transmission error detection in embedded systems.  
**Not designed for:** hash tables (poor distribution), security, or authentication.

| Type | Output | Notes |
|---|---|---|
| `Crc` + `CrcStandard` | 1–64 bits | 113 named standards from the RevEng catalogue; configurable polynomial, init, reflect, and XOR-out. |
| `Fletcher16` / `Fletcher32` / `Fletcher64` | 16 / 32 / 64 bits | Twin-accumulator design; catches transpositions that a simple sum or XOR misses. |
| `Adler32` / `Adler32C` / `Adler64` | 32 / 32 / 64 bits | Prime-modulus twin accumulator; Adler-32 is the canonical zlib checksum. Adler-32C uses a power-of-two modulus for SIMD throughput. |

→ Guides: [Using CRC](io-hashing/crc.md) · [CRC catalogue](io-hashing/crc-catalogue.md) · [Using Fletcher](io-hashing/fletcher.md) · [Using Adler](io-hashing/adler.md)

> **Checksum vs fingerprint.** Choose a checksum when the error model matters — "I need to catch any single-bit flip in a 4 KiB packet." Use a fingerprint when you need fast, even distribution across a hash table. The best fingerprints are not the best checksums, and vice versa. CRC and Fletcher distribute poorly as hash functions; FNV and CityHash have weaker burst-error guarantees than CRC.

---

### Check digits

**Namespace:** `Bodu.IO.Hashing.CheckDigits` · `Bodu.IO.Hashing.Checksums`  
**Base class:** `CheckDigitAlgorithm` / `AlphanumericCheckDigitAlgorithm`

Check-digit algorithms operate on **character sequences** (not binary byte buffers) and append a **single character** to a human-readable identifier. That trailing character lets any future reader confirm that the identifier was not mis-typed or mis-transcribed. The target error patterns are those a human introduces when copying a number by hand: single-digit substitution, adjacent transpositions, and twin errors.

**Designed for:** validating credit card numbers, bank account numbers, barcodes, international securities codes, ISBNs, and similar human-facing identifiers.  
**Not designed for:** binary data, long payloads, or any security application. These are never cryptographic.

Types in `Bodu.IO.Hashing.CheckDigits`:

| Type | Algorithm | Used by |
|---|---|---|
| `Luhn` | Mod 10 | Credit cards (Visa, Mastercard, Amex), IMEI numbers, SIN |
| `Damm` | Quasigroup (Damm) | General use; detects **all** single-digit errors and adjacent transpositions |
| `Verhoeff` | Dihedral group D₅ | German ID, medical device codes; detects all single and twin errors |
| `Ean8` / `Ean13` | GS1 weighted mod 10 | Retail barcodes |
| `Gtin14` | GS1 weighted mod 10 | Shipping cartons |
| `UpcA` | GS1 weighted mod 10 | US/Canada retail barcodes |
| `Isin` | Luhn over alphanumeric | International securities identifiers (ISO 6166) |
| `AbaRoutingNumber` | Weighted mod 10 | US bank routing numbers |

Types in `Bodu.IO.Hashing.Checksums` (alphanumeric or multi-character output):

| Type | Algorithm | Used by |
|---|---|---|
| `Iban` | ISO 7064 Mod 97–10 | International bank account numbers (ISO 13616) |
| `Isbn10` / `Isbn13` | Weighted mod 11 / GS1 mod 10 | Book identifiers |
| `Sedol` | Weighted mod 10 | London Stock Exchange securities |
| `Cusip` | Weighted mod 10 | North American securities (ANSI X9.6) |
| `Lei` | ISO 17442 Mod 97–10 | Legal Entity Identifiers |
| `WeightedMod10` | Configurable weighted mod 10 | General-purpose base for custom schemes |

→ Guide: [Check digits](io-hashing/check-digits.md)

> **Check digit vs checksum.** A check digit operates on a *printed identifier* — a short, human-readable string. A checksum operates on a *binary payload* that only software sees. A check digit is always a single character (or two, for IBAN) appended to an identifier of known short length. A checksum is typically 2–8 bytes appended to a data frame of arbitrary length.

---

## Cryptographic families — `Bodu.Security.Cryptography`

These algorithms are designed with a formal **adversary model**: it must be computationally infeasible for an attacker — even one who knows the algorithm, can observe many inputs and outputs, and can choose inputs adaptively — to forge, invert, or find collisions.

![Structural input/output comparison across all six families](../images/diagrams/algorithm-io-model.svg)

---

### Cryptographic hash

**Namespace:** `Bodu.Security.Cryptography`  
**Base class:** `System.Security.Cryptography.HashAlgorithm`

One-way functions that compress an arbitrary-length input to a fixed-size digest. The security properties distinguish them from fingerprints:

- **Pre-image resistance** — given a digest, finding *any* input that produces it is computationally infeasible.
- **Second pre-image resistance** — given an input, finding a *different* input with the same digest is computationally infeasible.
- **Collision resistance** — finding *any* two distinct inputs with the same digest is computationally infeasible.

**Designed for:** content addressing, file integrity verification, digital signature inputs, commitment schemes.  
**Not designed for:** authentication without a separate signature or MAC — a hash alone does not prove who produced it.

| Type | Output | Notes |
|---|---|---|
| `Tiger` | 128 / 160 / 192 bits | Optimised for 64-bit platforms (1995); two padding variants (Tiger / Tiger2). |
| `CubeHash` | Configurable | SHA-3 finalist; tunable rounds and block size trade security margin for throughput. |
| `Snefru128` / `Snefru256` | 128 / 256 bits | **Cryptanalytically broken.** Use for interoperability only; never for new work. |
| `AsconHash256` | 256 bits | NIST SP 800-232; 12-round sponge permutation; maximum security margin. |
| `AsconHashA256` | 256 bits | NIST SP 800-232; 8-round sponge permutation; higher throughput, reduced margin. |
| `AsconXof128` | Variable | NIST SP 800-232; 8-round sponge; squeeze any number of bytes. |
| `AsconCxof128` | Variable | NIST SP 800-232 customisable XOF; accepts a domain customisation string. |

→ Guides: [Using Tiger](cryptography/tiger.md) · [Using CubeHash](cryptography/cubehash.md) · [Using Snefru](cryptography/snefru.md) · [ASCON hashing](cryptography/ascon-hashing.md) · [ASCON XOF](cryptography/ascon-xof.md)

---

### Keyed hash / MAC

**Namespace:** `Bodu.Security.Cryptography`  
**Base class:** `System.Security.Cryptography.HashAlgorithm`

Keyed hash algorithms require a **secret key** and produce an authentication tag. Without the key, an adversary cannot compute a valid tag for any message. This is the fundamental distinction from an ordinary (unkeyed) hash.

**Designed for:** message authentication (proving a message was produced by a key-holder), hash-flooding–resistant hash tables (SipHash), AEAD building blocks (Poly1305 + ChaCha20).  
**Not designed for:** general-purpose hashing (key management overhead), encryption (tags are not confidential).

| Type | Output | Notes |
|---|---|---|
| `SipHash64` | 64 bits | Keyed PRF designed for hash-flooding–resistant hash tables. Configurable compression and finalisation rounds (default: SipHash-2-4). |
| `SipHash128` | 128 bits | Same as SipHash64 with wider output; lower collision probability for routing / sharding use cases. |
| `Poly1305` | 128 bits | One-time authenticator (RFC 8439); the key **must not** be reused across messages. Intended as the MAC component of a ChaCha20-Poly1305 AEAD construction. |

→ Guides: [Using SipHash](cryptography/siphash.md) · [Using Poly1305](cryptography/poly1305.md)

> **Keyed hash vs cipher.** A MAC and a cipher both require a key, but they serve opposite purposes. A cipher transforms plaintext to ciphertext and back — it does not produce a summary. A MAC summarises a message into a fixed-size tag — it does not encrypt. Use both together (encrypt-then-MAC, or an AEAD mode) when you need both confidentiality and integrity.

---

### Symmetric block cipher — standard

**Namespace:** `Bodu.Security.Cryptography`  
**Base class:** `System.Security.Cryptography.SymmetricAlgorithm`

Block ciphers encrypt a fixed-size block of data under a secret key, producing a ciphertext block of the same size. A *cipher mode* (CBC, CTR, OFB, …) chains block operations to handle messages of arbitrary length. The operation is **reversible**: given the same key and IV, decryption recovers the original plaintext exactly.

**Designed for:** confidentiality — keeping data secret from anyone who does not hold the key.  
**Not designed for:** integrity or authentication alone — a cipher does not prove a ciphertext was not tampered with. Pair with a MAC or use an AEAD mode for that.

| Type | Block | Key | Notes |
|---|---|---|---|
| `Skipjack` | 64 bits (8 B) | 80 bits (10 B) | NSA design, declassified 1998. The 80-bit key is below modern security margins. **Legacy and interoperability use only.** |
| `Blowfish` | 64 bits (8 B) | 32–448 bits (variable) | Bruce Schneier, 1993. Expensive key schedule; 64-bit block is a practical limitation for large data volumes (birthday bound ≈ 4 GB). |
| `Camellia` | 128 bits (16 B) | 128 / 192 / 256 bits | NTT and Mitsubishi (RFC 3713). ISO/IEC 18033-3 standard; comparable security margin to AES at matching key size. |
| `Twofish` | 128 bits (16 B) | 128 / 192 / 256 bits | Schneier et al., AES finalist (1998). Conservative design with extensive published cryptanalysis; widely used outside the AES standard itself. |
| `Serpent128` | 128 bits (16 B) | 128 / 192 / 256 bits | Anderson, Biham, and Knudsen, AES finalist (1998). Highest security margin among the AES finalists; slower than Rijndael / Twofish in software. |

All five expose the standard `SymmetricAlgorithm` lifecycle: set `BlockMode`, `Padding`, `Key`, and `IV`; call `CreateEncryptor()` / `CreateDecryptor()`. AES is exposed slightly differently — through the lower-level `AesBlockCipher` adapter over the BCL `Aes` engine — so it pairs naturally with the AEAD mode transforms; see the [AEAD modes guide](cryptography/aead-modes.md).

→ Guides: [Using Skipjack](cryptography/skipjack.md) · [Using Blowfish](cryptography/blowfish.md) · [Cipher block modes](cryptography/cipher-modes.md) · [Padding](cryptography/padding.md)

---

### Symmetric block cipher — tweakable

**Namespace:** `Bodu.Security.Cryptography`  
**Base class:** `TweakableSymmetricAlgorithm` (extends `SymmetricAlgorithm`)

A *tweakable block cipher* accepts a third public input — the **tweak** — in addition to the key and the plaintext. Encrypting the same plaintext under the same key with a **different tweak** yields an entirely independent ciphertext. The tweak is analogous to an IV but carries additional domain-separation semantics: it lets a single key serve many independent "cipher instances" without needing a new key for each.

Typical uses: disk encryption (block/sector number as tweak), per-record encryption (record ID as tweak), protocol domain separation.

**Designed for:** all the same use cases as a standard cipher, plus fine-grained domain separation without re-keying.  
**Not designed for:** replacing IVs — the tweak is public domain separation, not a randomisation source. A fresh, unique IV is still required per message.

| Type | Block | Key | Tweak | Notes |
|---|---|---|---|---|
| `Threefish256` | 256 bits (32 B) | 256 bits (32 B) | 128 bits (16 B) | Core of Skein-256. Smallest Threefish variant. |
| `Threefish512` | 512 bits (64 B) | 512 bits (64 B) | 128 bits (16 B) | Core of Skein-512. Recommended general-purpose variant. |
| `Threefish1024` | 1024 bits (128 B) | 1024 bits (128 B) | 128 bits (16 B) | Highest security margin; most padding waste for short messages. |
| `Serpent256` | 256 bits (32 B) | 256 bits (32 B) | 128 bits (16 B) | Wide-block tweakable Serpent construction. **Non-standard** — no published reference vectors; built on the round function of `Serpent128`. |
| `Serpent512` | 512 bits (64 B) | 512 bits (64 B) | 128 bits (16 B) | Wide-block tweakable Serpent construction. **Non-standard** — see `Serpent256`. |
| `Serpent1024` | 1024 bits (128 B) | 1024 bits (128 B) | 128 bits (16 B) | Wide-block tweakable Serpent construction. **Non-standard** — see `Serpent256`. |

All six expose `TweakableSymmetricAlgorithm`, which adds a `Tweak` property and `GenerateTweak()` to the standard `SymmetricAlgorithm` surface. Configure `BlockMode`, `Padding`, `Key`, `IV`, and `Tweak`; then call `CreateEncryptor()` / `CreateDecryptor()` or the `Encrypt` / `Decrypt` extension methods.

→ Guides: [Using Threefish-256](cryptography/threefish-256.md) · [Using Threefish-512](cryptography/threefish-512.md) · [Using Threefish-1024](cryptography/threefish-1024.md) · [Encryption basics](cryptography/encryption-basics.md)

---

## ASCON — a multi-role family

**ASCON** (NIST SP 800-232) is a compact, sponge-based family that spans three of the cryptographic families above under a single permutation primitive. It is particularly suited to constrained environments (embedded hardware, firmware, applications that want a small dependency footprint) and is the first lightweight cryptography standard published by NIST.

| Type | Family | Role |
|---|---|---|
| `AsconHash256` | Crypto Hash | 256-bit one-way digest; 12-round permutation; maximum margin. |
| `AsconHashA256` | Crypto Hash | 256-bit one-way digest; 8-round permutation; higher throughput. |
| `AsconXof128` | Crypto Hash (XOF) | Variable-length output; squeeze any number of bytes; 8-round permutation. |
| `AsconCxof128` | Crypto Hash (XOF) | Customisable XOF; accepts a domain customisation string to domain-separate instances. |
| `AsconAead128` | Cipher (AEAD) | 128-bit key, 128-bit nonce, 128-bit tag — authenticated encryption with associated data. |

Use ASCON when you need a standards-backed primitive with a compact software footprint, or when targeting hardware without AES-NI. See the [ASCON family guide](cryptography/ascon.md) for selection guidance across the five types.

---

## Master selection guide

| You need… | Reach for | Family |
|---|---|---|
| Fast hash-table key or in-memory bucket index | `Fnv1a64`, `CityHash64` | Fingerprint |
| Best throughput on large buffers | `CityHash64`, `XxHash64` | Fingerprint |
| A specific on-wire checksum (zlib, PNG, Ethernet, Modbus, iSCSI, NVMe) | `Crc` + `CrcStandard.*` | Checksum |
| A cheap checksum that also catches adjacent transpositions | `Fletcher32` or `Adler32` | Checksum |
| Validate a credit card or barcode a user typed | `Luhn`, `Ean13`, `Gtin14` | Check Digit |
| Validate an IBAN, ISIN, or ISBN | `Iban`, `Isin`, `Isbn13` | Check Digit |
| Cryptographic digest for content addressing or signature input | `AsconHash256`, `Tiger`, or BCL `SHA256` | Crypto Hash |
| Variable-length output (arbitrary number of bytes) | `AsconXof128` or `AsconCxof128` | Crypto Hash (XOF) |
| Hash-table safety against deliberate hash-flooding | `SipHash64` / `SipHash128` | Keyed Hash |
| Authenticate a message without encrypting it | `Poly1305` (with ChaCha20) | MAC |
| Encrypt data for a key-holder only | `Threefish512`, `Blowfish` | Cipher |
| Encrypt with per-record domain separation (no re-keying) | `Threefish256` / `Threefish512` / `Threefish1024` with `Tweak` | Tweakable Cipher |
| Encrypt **and** authenticate in one operation (AEAD) | `AsconAead128`, or `AesBlockCipher` + `GcmModeTransform` | AEAD |
| A single compact primitive for hash · XOF · AEAD | ASCON family | Multi-role |

---

## Where to go next

**Starting points by library**
- [Bodu.Core overview](core/) — bounded collections, evicting caches, WeekPattern, date extensions.
- [Bodu.IO.Hashing overview](io-hashing/) — algorithm-selection table and common lifecycle.
- [Bodu.Security.Cryptography overview](cryptography/) — encryption, hashing, and AEAD families.
- [Bodu.Globalization.Calendar overview](calendar/) — notable date resolution, territory filtering, and algorithms.

**Encryption**
- [Encryption basics](cryptography/encryption-basics.md) — Key, IV, Tweak, mode, and padding in one place.
- [Cipher block modes](cryptography/cipher-modes.md) — ECB, CBC, CFB, OFB, CTR with worked examples.
- [AEAD modes](cryptography/aead-modes.md) — GCM, CCM, OCB3, SIV, GCM-SIV with AES.
- [ASCON AEAD](cryptography/ascon-aead.md) — lightweight NIST-standardised authenticated encryption.

**Hashing**
- [Using hashes and checksums](cryptography/hashing.md) — keyed and cryptographic hash patterns.
- [Check digits](io-hashing/check-digits.md) — validating human-readable identifiers.
