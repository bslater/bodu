---
title: Skein hashing
---

# Skein hashing

`Skein` is a SHA-3 finalist hash family (Schneier, Ferguson, Lucks, Whiting, Bellare, Kohno, Callas, and Walker — the 1.3 specification). It is built on the Threefish tweakable block cipher and uses the **UBI** (Unique Block Iteration) construction: each block feeds into Threefish under a tweak that identifies the block's role (configuration, key, message, output), its position, and first / final flags.

`Bodu.Security.Cryptography` ships three variants matching the three Threefish state sizes — Skein-256, Skein-512, and Skein-1024 — each with optional keyed-MAC mode and configurable output size.

This guide covers all three on one page. For the broader "which hash do I pick?" framing, see [Hashing](hashing.md). For the underlying Threefish cipher, see [Threefish-256](threefish-256.md) / [-512](threefish-512.md) / [-1024](threefish-1024.md).

## At a glance

| Hash | State / block | Threefish backend | Key (MAC) | Output |
|---|---|---|---|---|
| **Skein-256** | 256 bits / 32 bytes | Threefish-256 | 0–128 bytes | Configurable |
| **Skein-512** | 512 bits / 64 bytes | Threefish-512 | 0–128 bytes | Configurable |
| **Skein-1024** | 1024 bits / 128 bytes | Threefish-1024 | 0–128 bytes | Configurable |

All three derive from `KeyedBlockHashAlgorithm<T>` and support both plain hash and Skein-MAC mode (a preliminary KEY UBI phase). `MaxKeySize` is 8192 bits (128 bytes) across all variants.

## When to pick which

- **Pick Skein-512** as the default. It is the spec's "primary" variant — 512-bit state, 512-bit output by default, optimised for 64-bit platforms.
- **Pick Skein-256** when memory is tight or output is short — embedded devices, short message authentication.
- **Pick Skein-1024** when the output size needs to be larger than 512 bits, or when the larger state's security margin matters for very long inputs.

## Skein-512

```csharp
using Bodu.Security.Cryptography;

// Plain hash with the default 512-bit output.
using var hasher = new Skein512();
byte[] hash = hasher.ComputeHash(payload);   // 64 bytes

// Custom output size — 256 bits.
using var skein512_256 = new Skein512(hashSizeBits: 256);
byte[] hash256 = skein512_256.ComputeHash(payload);   // 32 bytes
```

`AlgorithmName` reports `"Skein-512-512"`, `"Skein-512-256"`, etc., reflecting both the state size and the chosen output size.

### Skein-512-MAC mode

```csharp
using Bodu.Security.Cryptography;

byte[] macKey = RandomNumberGenerator.GetBytes(64);   // 0-128 bytes
using var mac = new Skein512(hashSizeBits: 512);
mac.Key = macKey;

byte[] tag = mac.ComputeHash(payload);
```

Set `Key` to a non-empty byte array to switch to Skein-MAC mode. The KEY UBI phase runs over the key before the message blocks; the rest of the pipeline is identical to plain hash. Empty key = plain hash.

## Skein-256 and Skein-1024

The API shape is identical to Skein-512 — same constructors, same `Key` property, same UBI tweak-driven pipeline. The only difference is the backing Threefish state size:

```csharp
using Bodu.Security.Cryptography;

using var s256  = new Skein256();                       // default output
using var s256k = new Skein256(hashSizeBits: 224) { Key = macKey };

using var s1024 = new Skein1024();
using var s1024_768 = new Skein1024(hashSizeBits: 768); // wider output than 512 supports
```

`AlgorithmName` reports `"Skein-256-<n>"` and `"Skein-1024-<n>"` respectively.

## Streaming

```csharp
using var hasher = new Skein512(hashSizeBits: 512);

hasher.TransformBlock(buffer1, 0, n1, null, 0);
hasher.TransformBlock(buffer2, 0, n2, null, 0);
hasher.TransformFinalBlock(buffer3, 0, n3);

byte[] hash = hasher.Hash!;
```

`CanReuseTransform` and `CanTransformMultipleBlocks` are both `true`. The standard `HashAlgorithm` streaming pattern works without modification.

## Configuration parameters not exposed

The Skein specification defines optional configuration parameters — personalisation strings, key derivation identifiers, nonce input, and tree-hashing modes. **This implementation does not expose them.** For tree hashing, reach for [`MerkleTreeHash`](merkle-trees.md). For personalised hashing or HKDF-like key derivation, reach for the BCL `HKDF` class.

## Security caveats

- **Status.** Skein was a SHA-3 finalist; the SHA-3 selection chose Keccak (which became SHAKE / SHA-3 / KMAC). Skein is unbroken but is not a standardised algorithm — pick SHA-3 or BLAKE2 / 3 when interoperability matters more than the specific algorithm.
- **Output truncation.** Truncating a Skein output to a smaller size reduces the security level — 256-bit Skein-512 output has 128-bit collision resistance, adequate for most uses. Don't truncate below 128 bits for cryptographic purposes.
- **MAC mode safety.** Skein's UBI construction is immune to length-extension attacks, so Skein-MAC by prepending the key is safe — HMAC is unnecessary.
- **Key length.** Any length 0 – 128 bytes is accepted. The spec recommends matching the key length to the state size (32 bytes for Skein-256, 64 bytes for Skein-512, 128 bytes for Skein-1024) for the cleanest security argument.

## When *not* to use Skein

- **You need a standardised hash.** Reach for SHA-2 (BCL `SHA256` / `SHA512`) or SHA-3 (BCL `SHA3_256` / `SHA3_512` on .NET 8+). Skein is unbroken but lost the SHA-3 competition.
- **You need an XOF.** Reach for [SHAKE](shake.md). Skein supports XOF-style output extension in principle, but this implementation does not expose it.
- **You need a faster non-cryptographic hash.** Reach for [`Bodu.IO.Hashing`](~/apidoc/Bodu.IO.Hashing.md).
- **You need tweakable block-cipher behaviour rather than hashing.** Reach for [Threefish-256](threefish-256.md) / [-512](threefish-512.md) / [-1024](threefish-1024.md) directly.

## See also

- [Hashing overview](hashing.md) — the framework's overall hash story.
- [Threefish-256](threefish-256.md), [Threefish-512](threefish-512.md), [Threefish-1024](threefish-1024.md) — the underlying tweakable block cipher.
- [BLAKE](blake.md), [Tiger](tiger.md), [Whirlpool](whirlpool.md) — other cryptographic digests in the package.
- [`Bodu.Security.Cryptography.Skein256` API reference](xref:Bodu.Security.Cryptography.Skein256)
- [`Bodu.Security.Cryptography.Skein512` API reference](xref:Bodu.Security.Cryptography.Skein512)
- [`Bodu.Security.Cryptography.Skein1024` API reference](xref:Bodu.Security.Cryptography.Skein1024)
