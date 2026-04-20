---
uid: Bodu.Security.Cryptography
---

![Bodu.Security.Cryptography](~/images/hero-crypto.svg)

## Purpose

**Bodu.Security.Cryptography** is a small, self-contained collection of managed block-cipher and hash / checksum implementations that plug into the standard .NET cryptography contracts (<xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType> and <xref:System.Security.Cryptography.SymmetricAlgorithm?displayProperty=nameWithType>).

Reach for this library when you need a tweakable block cipher that isn't in the BCL (Threefish), a fast non-cryptographic checksum for framing or fingerprinting (Fletcher, Adler, FNV-1a), a collision-resistant keyed hash for hash-table protection (SipHash), a 64-bit-optimised cryptographic hash (Tiger), or a reference Skipjack implementation for research.

## Key types

**Block ciphers**

- <xref:Bodu.Security.Cryptography.Threefish256> — the tweakable 256-bit block / 256-bit key variant of the Threefish cipher family.
- <xref:Bodu.Security.Cryptography.Threefish512> — 512-bit blocks and keys; the cipher underlying the Skein hash function.
- <xref:Bodu.Security.Cryptography.Threefish1024> — 1024-bit blocks and keys for scenarios requiring larger block sizes.
- <xref:Bodu.Security.Cryptography.Skipjack> — NSA-designed 64-bit block, 80-bit key cipher. **Historical / research only.**

**Hashes and checksums**

- <xref:Bodu.Security.Cryptography.Fletcher32> — position-dependent 32-bit checksum (part of the <xref:Bodu.Security.Cryptography.Fletcher> family).
- <xref:Bodu.Security.Cryptography.Adler32> — fast 32-bit checksum in the Adler family; SIMD- and scalar-path implementations.
- <xref:Bodu.Security.Cryptography.SipHash64> — keyed 64-bit hash, collision-resistant for hash-table protection; configurable compression and finalisation rounds.
- <xref:Bodu.Security.Cryptography.Fnv1a32> — fast, non-cryptographic FNV-1a 32-bit hash for checksums, fingerprints, and hash tables.
- <xref:Bodu.Security.Cryptography.Tiger> — cryptographic hash by Anderson and Biham optimised for 64-bit platforms; supports 128 / 160 / 192-bit output with Tiger or Tiger2 padding.

**Helpers**

- <xref:Bodu.Security.Cryptography.CryptoHelpers> — secure-zeroisation, padding / de-padding, cryptographically secure random generation, bit reflection, and argument validation helpers intended for internal and consumer use.

## Example

```csharp
using System.Text;
using Bodu.Security.Cryptography;

// Non-cryptographic checksum: standard HashAlgorithm contract.
byte[] data = Encoding.UTF8.GetBytes("the quick brown fox");

using var fletcher = new Fletcher32();
string hex = Convert.ToHexString(fletcher.ComputeHash(data));

// Keyed hash for a hash-table against collision attacks.
byte[] key = RandomNumberGenerator.GetBytes(16);
using var sip = new SipHash64 { Key = key };
ulong digest = BitConverter.ToUInt64(sip.ComputeHash(data), 0);
```

## Notes

- **Security caveats.**
  - <xref:Bodu.Security.Cryptography.Skipjack> is provided for historical and research purposes. It has an 80-bit key and a 64-bit block; **do not use it for new systems**.
  - <xref:Bodu.Security.Cryptography.Fletcher32>, <xref:Bodu.Security.Cryptography.Adler32>, and <xref:Bodu.Security.Cryptography.Fnv1a32> are **non-cryptographic**. They are appropriate for error detection and hash-table distribution, not for authentication or integrity against an active attacker.
  - <xref:Bodu.Security.Cryptography.SipHash64> is keyed and collision-resistant but short-output; use it for hash-table protection and message authentication over small inputs, not as a drop-in for a MAC like HMAC-SHA256.
  - <xref:Bodu.Security.Cryptography.Tiger> is a classic cryptographic hash. Prefer BCL-provided SHA-2 / SHA-3 for new designs; use Tiger for interoperability with existing Tiger-based systems.
- **Thread safety.** Instances of the cipher and hash types follow the standard .NET convention: **not thread-safe** during a single `TransformBlock` / `ComputeHash` / encryption session. Create one instance per logical operation, or synchronise externally.
- **Allocation discipline.** Hot-path types allocate their working buffers in the constructor and reuse them; `CryptoHelpers.ClearIfNotNull` (and equivalents) zero secret material at disposal time.
- **Determinism and portability.** All algorithms produce identical byte-for-byte output across platforms and architectures for the same input and configuration.
