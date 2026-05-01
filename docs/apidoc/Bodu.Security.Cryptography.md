---
uid: Bodu.Security.Cryptography
---

![Bodu.Security.Cryptography](~/images/hero-crypto.svg)

## Purpose

**Bodu.Security.Cryptography** is a self-contained collection of managed block-cipher, cipher-mode, keyed-hash, and cryptographic-hash implementations that plug into the standard .NET cryptography contracts (<xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType> and <xref:System.Security.Cryptography.SymmetricAlgorithm?displayProperty=nameWithType>).

Reach for this library when you need a tweakable block cipher that isn't in the BCL (Threefish), authenticated-encryption mode transforms for AES (GCM / CCM / OCB / EAX / SIV / GCM-SIV), a collision-resistant keyed hash for hash-table protection (SipHash), a one-time authenticator (Poly1305), a 64-bit-optimised cryptographic digest (Tiger), a Merkle-tree hash for streaming integrity with per-chunk verifiability, or a reference Skipjack implementation for research.

For non-cryptographic checksums and hash-table hashes (CRC, Fletcher, Adler, FNV, CityHash, Pearson, Bernstein, BKDR, SDBM, JSHash, Elf64, ApHash, Pjw32) see the companion <xref:Bodu.IO.Hashing> package, which is built on <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType>.

## Key types

**Block ciphers**

- <xref:Bodu.Security.Cryptography.Threefish256> — the tweakable 256-bit block / 256-bit key variant of the Threefish cipher family.
- <xref:Bodu.Security.Cryptography.Threefish512> — 512-bit blocks and keys; the cipher underlying the Skein hash function.
- <xref:Bodu.Security.Cryptography.Threefish1024> — 1024-bit blocks and keys for scenarios requiring larger block sizes.
- <xref:Bodu.Security.Cryptography.Blowfish> — 64-bit block / 32–448-bit key cipher. Well-studied legacy algorithm; prefer Threefish or the BCL's AES for new designs.
- <xref:Bodu.Security.Cryptography.Skipjack> — NSA-designed 64-bit block, 80-bit key cipher. **Historical / research only.**

**Cipher modes and authenticated encryption**

- <xref:Bodu.Security.Cryptography.CipherBlockMode> — the library's extended block-mode enum (ECB / CBC / CFB / OFB / CTR).
- <xref:Bodu.Security.Cryptography.BlockCipherModeFactory>, <xref:Bodu.Security.Cryptography.PaddingFactory> — compose any <xref:Bodu.Security.Cryptography.IBlockCipher> with a mode and a padding strategy.
- <xref:Bodu.Security.Cryptography.AesBlockCipher> — an <xref:Bodu.Security.Cryptography.IBlockCipher> adapter over the BCL's AES, used to drive the AEAD transforms.
- <xref:Bodu.Security.Cryptography.GcmModeTransform>, <xref:Bodu.Security.Cryptography.CcmModeTransform>, <xref:Bodu.Security.Cryptography.OcbModeTransform>, <xref:Bodu.Security.Cryptography.EaxModeTransform>, <xref:Bodu.Security.Cryptography.SivModeTransform>, <xref:Bodu.Security.Cryptography.GcmSivModeTransform> — AEAD mode transforms for AES.

**Keyed and cryptographic hashes**

- <xref:Bodu.Security.Cryptography.SipHash64> — keyed 64-bit hash, collision-resistant for hash-table protection; configurable compression and finalisation rounds.
- <xref:Bodu.Security.Cryptography.SipHash128> — 128-bit SipHash variant for longer-output keyed hashing.
- <xref:Bodu.Security.Cryptography.Poly1305> — one-time authenticator; pairs with a stream cipher for a MAC-with-AEAD construction.
- <xref:Bodu.Security.Cryptography.Tiger> — cryptographic hash by Anderson and Biham optimised for 64-bit platforms; supports 128 / 160 / 192-bit output with Tiger or Tiger2 padding.
- <xref:Bodu.Security.Cryptography.Snefru128>, <xref:Bodu.Security.Cryptography.Snefru256> — a cryptographic hash by Ralph Merkle (broken; included for research).
- <xref:Bodu.Security.Cryptography.CubeHash> — Bernstein's SHA-3 competition candidate.
- <xref:Bodu.Security.Cryptography.MerkleTreeHash>, <xref:Bodu.Security.Cryptography.ParallelMerkleTreeHash> — Merkle-tree root hashing with partial verification.

**Helpers**

- <xref:Bodu.Security.Cryptography.CryptoHelpers> — secure zeroisation, padding / de-padding, cryptographically secure random generation, bit reflection, and argument validation helpers intended for internal and consumer use.

## Example

```csharp
using System.Security.Cryptography;
using System.Text;
using Bodu.Security.Cryptography;

// Keyed hash for a hash-table protected against collision attacks.
byte[] data = Encoding.UTF8.GetBytes("the quick brown fox");
byte[] key  = RandomNumberGenerator.GetBytes(16);

using var sip = new SipHash64 { Key = key };
ulong slot = BitConverter.ToUInt64(sip.ComputeHash(data));
```

## Notes

- **Security caveats.**
  - <xref:Bodu.Security.Cryptography.Skipjack> is provided for historical and research purposes. It has an 80-bit key and a 64-bit block; **do not use it for new systems**.
  - <xref:Bodu.Security.Cryptography.Blowfish> is well-studied but dated; prefer Threefish or the BCL's AES for new designs — its 64-bit block limits the safe encryption volume per key.
  - <xref:Bodu.Security.Cryptography.SipHash64> is keyed and collision-resistant but short-output; use it for hash-table protection and message authentication over small inputs, not as a drop-in for a MAC like HMAC-SHA256.
  - <xref:Bodu.Security.Cryptography.Tiger> is a classic cryptographic hash. Prefer BCL-provided SHA-2 / SHA-3 for new designs; use Tiger for interoperability with existing Tiger-based systems.
  - For error-detection and hash-table distribution (CRC, Fletcher, Adler, FNV, CityHash, Pearson, and the classic short hashes) use the non-cryptographic types in <xref:Bodu.IO.Hashing>.
- **Thread safety.** Instances of the cipher and hash types follow the standard .NET convention: **not thread-safe** during a single `TransformBlock` / `ComputeHash` / encryption session. Create one instance per logical operation, or synchronise externally.
- **Allocation discipline.** Hot-path types allocate their working buffers in the constructor and reuse them; `CryptoHelpers.ClearIfNotNull` (and equivalents) zero secret material at disposal time.
- **Determinism and portability.** All algorithms produce identical byte-for-byte output across platforms and architectures for the same input and configuration.
- **See also:** <xref:Bodu.IO.Hashing> for CRC, Fletcher, and other non-cryptographic hashes on the <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType> contract.
