---
title: SHAKE extendable output
---

# SHAKE extendable output

`Shake` is the NIST FIPS 202 eXtendable Output Function (XOF) family built on the Keccak-f[1600] permutation — the same permutation that underpins SHA-3. The headline difference from SHA-3 is that the output length is **independent of the security level** and is configurable per instance: SHAKE128 admits any positive number of output bits at a 128-bit security target; SHAKE256 admits the same at a 256-bit security target.

XOFs are useful whenever you need more output than a fixed-spec hash supplies — KMAC inputs, post-quantum signature schemes (e.g. Dilithium, Falcon), hash-based DRBGs, fast key derivation, deterministic randomness from a seed.

For the broader "which hash do I pick?" framing, see [Hashing](hashing.md).

## At a glance

| Property | Value |
|---|---|
| Underlying permutation | Keccak-f[1600] (1600-bit state) |
| Variants | SHAKE128 (rate 168 bytes, capacity 32 bytes) — SHAKE256 (rate 136 bytes, capacity 64 bytes) |
| Security level | 128 bits (SHAKE128) / 256 bits (SHAKE256) |
| Output | Configurable; any positive multiple of 8 bits |
| Domain-separation byte | `0x1F` (distinguishes from raw Keccak and SHA-3) |

## Construction

```csharp
using Bodu.Security.Cryptography;

// Default — SHAKE128 with 256-bit output.
using var shake = new Shake();
byte[] output = shake.ComputeHash(seed);   // 32 bytes

// Explicit — SHAKE256 with 1024-bit output.
using var shake256 = new Shake(outputBits: 1024, securityLevel: 256);
byte[] longOutput = shake256.ComputeHash(seed);   // 128 bytes
```

`outputBits` must be positive and divisible by 8 — the API does not support sub-byte output sizes. `securityLevel` is either `128` (SHAKE128) or `256` (SHAKE256); any other value is rejected at construction.

`AlgorithmName` reports `"SHAKE128"` or `"SHAKE256"` to match the chosen security level. `HashSize` returns the output size in bits.

`HashSize` is mutable before hashing starts; once `TransformBlock` / `ComputeHash` has been called, attempting to change it throws `CryptographicUnexpectedOperationException`.

## Streaming

```csharp
using var shake = new Shake(outputBits: 512, securityLevel: 256);

shake.TransformBlock(buffer1, 0, n1, null, 0);
shake.TransformBlock(buffer2, 0, n2, null, 0);
shake.TransformFinalBlock(buffer3, 0, n3);

byte[] output = shake.Hash!;   // 64 bytes
```

`CanReuseTransform` and `CanTransformMultipleBlocks` are both `true`.

## SHAKE128 vs. SHAKE256

The two variants differ in rate / capacity split and therefore in security level:

| Variant | Rate | Capacity | Security |
|---|---|---|---|
| SHAKE128 | 168 bytes / call | 32 bytes | 128-bit |
| SHAKE256 | 136 bytes / call | 64 bytes | 256-bit |

- **SHAKE128** absorbs and squeezes 168 bytes per Keccak call. Faster, lower security target. Adequate for any non-cryptographic use of the output, and for cryptographic uses where a 128-bit security target is sufficient (KMAC128, AES-128-equivalent contexts).
- **SHAKE256** absorbs and squeezes 136 bytes per Keccak call. Slower, higher security target. The right choice when the output drives a 256-bit-security primitive (KMAC256, AES-256-equivalent contexts, post-quantum schemes that require it).

## Worked example — deterministic randomness from a seed

```csharp
using Bodu.Security.Cryptography;

byte[] seed = RandomNumberGenerator.GetBytes(32);

// Squeeze 4 KiB of deterministic randomness from a 32-byte seed.
using var shake = new Shake(outputBits: 4 * 1024 * 8, securityLevel: 256);
byte[] randomness = shake.ComputeHash(seed);
```

This is the canonical XOF idiom: a high-entropy seed in, a deterministic but arbitrarily long sequence out, security target controlled by the variant.

## Worked example — KMAC input

```csharp
using Bodu.Security.Cryptography;

// Compose a SHAKE128 absorption with a domain string (poor-man's KMAC).
using var shake = new Shake(outputBits: 256, securityLevel: 128);

byte[] domain = "MyApp/auth/v1"u8.ToArray();
shake.TransformBlock(domain, 0, domain.Length, null, 0);
shake.TransformBlock(payload, 0, payload.Length, null, 0);
shake.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

byte[] mac = shake.Hash!;
```

This is *not* a real KMAC — KMAC has specific encoding rules for the domain, key, and customisation strings (FIPS SP 800-185). It illustrates the pattern. For interop with KMAC consumers, reach for a dedicated KMAC implementation.

## Security caveats

- **Domain separation matters.** SHAKE128 and SHAKE256 use domain-separation byte `0x1F`, which is different from raw Keccak (no separator) and from SHA-3 (`0x06`). Do not mix SHAKE output with raw-Keccak or SHA-3 output expecting interoperability — the absorbed messages differ even if the input bytes are identical.
- **Output truncation is safe.** Unlike Merkle–Damgård hashes, truncating a SHAKE output to a smaller size does not weaken the algorithm beyond the security target of the chosen variant.
- **Length extension is impossible.** SHAKE is a sponge construction; the capacity portion of the state is not exposed in the output, so length-extension does not apply.
- **Non-determinism caveat.** SHAKE output is deterministic given the input. If the input includes any non-deterministic component (system time, process ID, …), the output will also be non-deterministic — which is rarely what you want from a "deterministic randomness from a seed" pattern. Pin the input carefully.

## When *not* to use SHAKE

- **You need a fixed-spec hash.** Reach for SHA-2 / SHA-3 (BCL) or [BLAKE2 / 3](blake.md) for fixed-size output.
- **You need a MAC.** Reach for KMAC, BLAKE2b-MAC, or HMAC-SHA-256. SHAKE absorbed with a domain string is *almost* a MAC but lacks the formal KMAC construction.
- **You need a KDF.** Reach for the BCL `HKDF` class. HKDF is specifically designed for key derivation; SHAKE can be used as a KDF but HKDF's framing gives you the standard test vectors and the documented security argument.
- **You need a non-cryptographic fingerprint.** Reach for [`Bodu.IO.Hashing`](xref:Bodu.IO.Hashing).

## See also

- [Hashing overview](hashing.md) — the framework's overall hash story.
- [BLAKE](blake.md), [Tiger](tiger.md), [Skein](skein.md), [Whirlpool](whirlpool.md) — other cryptographic digests in the package.
- [Composing primitives](composing-primitives.md) — encrypt-then-MAC, key derivation patterns.
- [`Bodu.Security.Cryptography.Shake` API reference](xref:Bodu.Security.Cryptography.Shake)
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
