---
title: BLAKE2 and BLAKE3 hashing
---

# BLAKE2 and BLAKE3 hashing

The BLAKE family is the modern alternative to SHA-2 and SHA-3 for general-purpose cryptographic hashing. `Bodu.Security.Cryptography` ships all three variants:

- **BLAKE2b** — 64-bit-platform-optimised; 128–512-bit output; built-in MAC mode.
- **BLAKE2s** — 32-bit-platform-optimised; 128–256-bit output; built-in MAC mode.
- **BLAKE3** — high-throughput, tree-structured; 256-bit fixed output; parallelisable across cores on long inputs.

This guide covers all three on one page. For the broader "which hash do I pick?" framing, see [Hashing](hashing.md). For Merkle-tree hashing built on BLAKE3, see [Merkle trees](merkle-trees.md).

## At a glance

| Hash | Output | Key (MAC) | Block | Rounds | Strength |
|---|---|---|---|---|---|
| **BLAKE2b** | 128–512 bits (any multiple of 8 up to 512) | 0–64 bytes | 128 bytes | 12 | Faster than SHA-512 on 64-bit software |
| **BLAKE2s** | 128–256 bits | 0–32 bytes | 64 bytes | 10 | Lightweight, 32-bit platforms / embedded |
| **BLAKE3** | 256 bits (fixed; XOF can extend) | None (in this implementation) | 64-byte blocks, 1024-byte chunks | 7 (chunk), 7 (parent) | Tree-structured; parallel-friendly |

All three derive from RFC 7693 (BLAKE2) and the BLAKE3 spec respectively. None are known to be broken.

## When to pick which

- **Pick BLAKE2b** for general-purpose hashing on 64-bit platforms when the output size needs to be flexible (e.g. you want 384-bit output without paying for SHA-384's slower compression) or when keyed-MAC mode is convenient.
- **Pick BLAKE2s** for the same reasons on 32-bit platforms or embedded devices where 64-bit arithmetic is expensive.
- **Pick BLAKE3** when input is long enough to amortise the tree-structure overhead (typically multiple kilobytes) and when parallel hashing across cores is desirable.
- **Pick SHA-2 or SHA-3** for interoperability with systems that already standardise on them. BLAKE2 / 3 are unbroken but less universally available.

## BLAKE2b

```csharp
using Bodu.Security.Cryptography;

// Plain hash with the default 512-bit output.
using var hasher = new Blake2b();
byte[] hash = hasher.ComputeHash(payload);   // 64 bytes

// Custom output size — 256 bits.
using var blake2b256 = new Blake2b(hashSize: 256);
byte[] hash256 = blake2b256.ComputeHash(payload);   // 32 bytes
```

Permitted output sizes: 128, 160, 192, 224, 256, 384, or 512 bits. `HashSize` is mutable before hashing starts; once `TransformBlock` / `ComputeHash` has been called, attempting to change it throws `CryptographicUnexpectedOperationException`.

### BLAKE2b-MAC mode

```csharp
using Bodu.Security.Cryptography;

byte[] macKey = RandomNumberGenerator.GetBytes(32);    // 0-64 bytes
using var mac = new Blake2b(hashSize: 256);
mac.Key = macKey;

byte[] tag = mac.ComputeHash(payload);
```

Set `Key` to a non-empty byte array (0 – 64 bytes) to switch to BLAKE2b-MAC mode (RFC 7693 §2.8). The key is zero-padded to 128 bytes and prepended as the first message block. Empty key = plain hash.

BLAKE2b-MAC is the right primitive when HMAC-SHA-512 would be overkill and you want a single algorithm that does plain hash and MAC.

### Streaming

```csharp
using var hasher = new Blake2b(hashSize: 512);

hasher.TransformBlock(buffer1, 0, n1, null, 0);
hasher.TransformBlock(buffer2, 0, n2, null, 0);
hasher.TransformFinalBlock(buffer3, 0, n3);

byte[] hash = hasher.Hash!;
```

`CanReuseTransform` and `CanTransformMultipleBlocks` are both `true`; the standard streaming `HashAlgorithm` pattern works without modification.

## BLAKE2s

```csharp
using Bodu.Security.Cryptography;

using var hasher = new Blake2s();                                  // default 256-bit
using var b160   = new Blake2s(hashSize: 160);                     // 160-bit
using var mac    = new Blake2s(hashSize: 256) { Key = macKey };    // 0-32 byte key
```

Permitted output sizes: 128, 160, 192, 224, or 256 bits. Key length: 0 – 32 bytes (BLAKE2s-MAC mode). The streaming pattern is identical to BLAKE2b.

The only meaningful difference from BLAKE2b is the platform-optimisation tier: BLAKE2s uses 32-bit arithmetic, so on 64-bit platforms BLAKE2b is faster per byte. On 32-bit platforms BLAKE2s is faster.

## BLAKE3

```csharp
using Bodu.Security.Cryptography;

using var hasher = new Blake3();
byte[] hash = hasher.ComputeHash(payload);   // always 32 bytes (256 bits)
```

BLAKE3's output is fixed at 256 bits in this implementation. The underlying spec supports XOF-style squeezing of arbitrary-length output by repeatedly invoking the root compression; that surface is not exposed by `Bodu.Security.Cryptography.Blake3` — reach for `Bodu.Security.Cryptography.Shake` if you need a true XOF.

BLAKE3's headline feature is the tree structure: input is split into 1024-byte chunks, each chunk is compressed block-by-block, and chunk chaining values are folded pairwise up a binary tree until a single root remains. The tree topology means hashing long inputs is naturally parallelisable across cores — though this implementation does the tree synchronously.

### Streaming

```csharp
using var hasher = new Blake3();

hasher.TransformBlock(buffer1, 0, n1, null, 0);
hasher.TransformBlock(buffer2, 0, n2, null, 0);
hasher.TransformFinalBlock(buffer3, 0, n3);

byte[] hash = hasher.Hash!;
```

`CanReuseTransform` and `CanTransformMultipleBlocks` are both `true`.

### Keyed mode and KDF mode

The BLAKE3 spec defines a keyed-hash mode and a key-derivation mode. Both require initialisation with a key, and the chunk-flags input is set differently. **This implementation does not expose either mode.** For keyed hashing reach for BLAKE2b-MAC or BLAKE2s-MAC; for KDF, reach for the BCL `HKDF` class.

## Security caveats

- **Output truncation.** All three algorithms are designed so that *truncating* a longer output to fewer bytes gives a hash with reduced security level. BLAKE2b's 128-bit output has only 64-bit collision resistance — adequate for non-cryptographic fingerprints but not for cryptographic commitment. Pick a size that matches your threat model.
- **Length extension.** Unlike Merkle–Damgård designs (MD5, SHA-1, SHA-256, SHA-512), all three BLAKE variants are immune to length-extension attacks. You can use them as a MAC by simply prepending the key (BLAKE2b-MAC / BLAKE2s-MAC); HMAC is unnecessary.
- **Output size mutation.** `HashSize` on BLAKE2b / BLAKE2s is mutable before the first `TransformBlock`. Once hashing begins, the algorithm parameters are fixed — changing them mid-stream throws.

## When *not* to use BLAKE

- **You need a fixed-spec hash for interoperability.** Reach for SHA-256 / SHA-512 (BCL) or SHA-3 (BCL on .NET 8+). BLAKE is unbroken but less universally available.
- **You need an XOF.** Reach for [SHAKE](shake.md) — BLAKE3 does support XOF in principle, but this implementation does not expose it.
- **You need authenticated encryption.** Reach for [AEAD modes](aead-modes.md) — combining a hash with a cipher correctly is the hard part; AEAD modes hide the foot-guns.
- **You need a non-cryptographic fingerprint.** Reach for [`Bodu.IO.Hashing`](xref:Bodu.IO.Hashing) (FNV, MurmurHash3, CityHash, CRC) — much faster, no cryptographic guarantees.

## See also

- [Hashing overview](hashing.md) — the framework's overall hash story.
- [Merkle trees](merkle-trees.md) — BLAKE3-style tree hashing for verifiable commitments.
- [Tiger](tiger.md), [Skein](skein.md), [Whirlpool](whirlpool.md) — other digests in the package.
- [SHAKE](shake.md) — extendable output function.
- [`Bodu.Security.Cryptography.Blake2b` API reference](xref:Bodu.Security.Cryptography.Blake2b)
- [`Bodu.Security.Cryptography.Blake2s` API reference](xref:Bodu.Security.Cryptography.Blake2s)
- [`Bodu.Security.Cryptography.Blake3` API reference](xref:Bodu.Security.Cryptography.Blake3)
