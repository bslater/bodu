---
title: Whirlpool hashing
---

# Whirlpool hashing

`Whirlpool` is the ISO/IEC 10118-3 standardised hash designed by Vincent Rijmen and Paulo Barreto. It is a Merkle–Damgård construction wrapping an internal 512-bit block cipher (W) with Rijndael-inspired wide-trail design — large S-boxes (8 × 8 over GF(2⁸)), an MDS-based diffusion layer, 10 rounds.

The hash has had three formal revisions, all of which are supported by `Bodu.Security.Cryptography` through the `WhirlpoolVersion` enum.

For the broader "which hash do I pick?" framing, see [Hashing](hashing.md).

## At a glance

| Property | Value |
|---|---|
| Output size | 512 bits (64 bytes), fixed |
| Block size | 64 bytes (512 bits) |
| Message-length trailer | 256 bits, big-endian, appended after `0x80` padding |
| Standardisation | ISO/IEC 10118-3 |
| Rounds | 10 |

## Construction

```csharp
using Bodu.Security.Cryptography;

// Default — ISO/IEC 10118-3 (2003) Whirlpool.
using var hasher = new Whirlpool();
byte[] hash = hasher.ComputeHash(payload);   // 64 bytes
```

The output is always 512 bits — the algorithm does not support truncation or an alternative output size.

## Whirlpool revisions

`WhirlpoolVersion` selects the algorithm revision. The three revisions differ subtly in the S-box and diffusion matrix; the wire-format is identical, but a given message produces a different digest under each revision:

| Version | Year | Symbolic name | Notes |
|---|---|---|---|
| `WhirlpoolInfo1` | 2000 | Whirlpool-0 | Original publication. Superseded; do not use for new designs. |
| `WhirlpoolInfo2` | 2001 | Whirlpool-T | First revision (transition). Used by some early adopters. |
| `WhirlpoolInfo3` | 2003 | Whirlpool *(default)* | The current ISO/IEC 10118-3 algorithm. **Default and the right choice for new designs.** |

```csharp
using Bodu.Security.Cryptography;

using var legacy = new Whirlpool { Version = WhirlpoolVersion.WhirlpoolInfo1 };
byte[] hash = legacy.ComputeHash(payload);

Console.WriteLine(legacy.AlgorithmName);   // "Whirlpool-0"
```

`Version` is mutable before hashing starts; once `TransformBlock` / `ComputeHash` has been called, attempting to change it throws `CryptographicUnexpectedOperationException`.

The default `Whirlpool()` constructor selects `WhirlpoolInfo3`. Use the older variants only for compatibility with deployed systems that committed to a specific revision.

## Streaming

```csharp
using var hasher = new Whirlpool();

hasher.TransformBlock(buffer1, 0, n1, null, 0);
hasher.TransformBlock(buffer2, 0, n2, null, 0);
hasher.TransformFinalBlock(buffer3, 0, n3);

byte[] hash = hasher.Hash!;
```

`CanReuseTransform` and `CanTransformMultipleBlocks` are both `true`. The standard `HashAlgorithm` streaming pattern works without modification.

## Security caveats

- **Status.** Whirlpool is unbroken — no practical pre-image, second-pre-image, or collision attack on the standardised 2003 revision is known. Theoretical attacks reduce the security margin on reduced-round variants but do not threaten the full algorithm.
- **Use the 2003 revision.** Whirlpool-0 (2000) was withdrawn because of a flaw in the diffusion matrix; Whirlpool-T (2001) was the corrected interim version. Only `WhirlpoolInfo3` (2003) is the current ISO/IEC standard. The earlier versions are exposed for compatibility with archived data, not for new designs.
- **Merkle–Damgård length extension.** Whirlpool inherits the length-extension property of Merkle–Damgård hashes — if you use it for a MAC, use HMAC-Whirlpool, not "key ‖ message". For native MAC modes, prefer Skein or BLAKE2 / 3.

## When *not* to use Whirlpool

- **You need a faster modern hash.** SHA-512 (BCL), BLAKE2b, BLAKE3, and Skein-512 are all faster than Whirlpool in software.
- **You need a built-in MAC mode.** Whirlpool does not have a native MAC mode — reach for [BLAKE2b](blake.md) (BLAKE2b-MAC) or [Skein-512](skein.md) (Skein-MAC) instead. Use HMAC-Whirlpool only if interoperability requires it.
- **You need an XOF or variable-length output.** Reach for [SHAKE](shake.md).
- **You need a non-cryptographic fingerprint.** Reach for [`Bodu.IO.Hashing`](~/apidoc/Bodu.IO.Hashing.md).

## See also

- [Hashing overview](hashing.md) — the framework's overall hash story.
- [BLAKE](blake.md), [Tiger](tiger.md), [Skein](skein.md), [SHAKE](shake.md) — other cryptographic digests in the package.
- [Composing primitives](composing-primitives.md) — encrypt-then-MAC, HMAC, KDFs.
- [`Bodu.Security.Cryptography.Whirlpool` API reference](xref:Bodu.Security.Cryptography.Whirlpool)
- [`Bodu.Security.Cryptography.WhirlpoolVersion` API reference](xref:Bodu.Security.Cryptography.WhirlpoolVersion)
