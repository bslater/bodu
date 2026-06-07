---
uid: Bodu.Security.Cryptography
---

![Bodu.Security.Cryptography](~/images/hero-crypto.svg)

## Purpose

**Bodu.Security.Cryptography** is a self-contained collection of managed block-cipher, cipher-mode, padding, AEAD, keyed-hash, cryptographic-hash, and Merkle-tree implementations that plug into the standard .NET cryptography contracts (<xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType> and <xref:System.Security.Cryptography.SymmetricAlgorithm?displayProperty=nameWithType>), plus Bodu's <xref:Bodu.Security.Cryptography.TweakableSymmetricAlgorithm> and <xref:Bodu.Security.Cryptography.IBlockCipher>.

Reach for this library when you need a tweakable block cipher that isn't in the BCL (Threefish, wide-block Serpent), a managed implementation of an AES-finalist cipher (Camellia, Twofish, Serpent, Blowfish, Skipjack), a software stream cipher (ChaCha20, XChaCha20, Salsa20, XSalsa20, Rabbit, HC-128), authenticated-encryption mode transforms for AES (GCM / CCM / OCB / EAX / SIV / GCM-SIV), the ASCON family (Hash256, HashA256, XOF128, CXOF128, AEAD128), a keyed hash for hash-table protection or message authentication (SipHash, Poly1305), a cryptographic digest with a specific design lineage (Tiger, CubeHash, Snefru, Whirlpool, BLAKE2/3, Skein, Shake), or a Merkle-tree hash for streaming integrity with per-chunk verifiability.

For non-cryptographic checksums and hash-table hashes (CRC, Fletcher, Adler, FNV, CityHash, MurmurHash3, Pearson, classic string hashes) see the companion <xref:Bodu.IO.Hashing> package, which is built on <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType>.

## Static documentation

- **[Bodu.Security.Cryptography introduction](~/docs/cryptography/index.md)** — namespaces, headline types, scenarios.
- **[Bodu.Security.Cryptography getting started](~/docs/cryptography/getting-started.md)** — install and minimal samples for ciphers, AEAD, keyed hashes, and digests.
- **[Bodu.Security.Cryptography guides](~/guides/cryptography/index.md)** — encryption basics, cipher modes, padding, composing primitives, stream ciphers, AEAD modes, keyed and cryptographic hashing, the ASCON family.
- **[Bodu.IO.Hashing introduction](~/docs/io-hashing/index.md)** — the sibling library, for non-cryptographic checksums and fingerprints (no adversary model).

## Key types

**Standard block ciphers** (`SymmetricAlgorithm` lifecycle)

- <xref:Bodu.Security.Cryptography.Skipjack> — 64-bit block, 80-bit key. **Legacy / interoperability use only.**
- <xref:Bodu.Security.Cryptography.Blowfish> — 64-bit block, 32–448-bit key. Well-studied legacy algorithm with an expensive key schedule.
- <xref:Bodu.Security.Cryptography.Camellia> — 128-bit block, 128 / 192 / 256-bit key (RFC 3713; ISO/IEC 18033-3).
- <xref:Bodu.Security.Cryptography.Twofish> — 128-bit block, 128 / 192 / 256-bit key (Schneier et al., AES finalist).
- <xref:Bodu.Security.Cryptography.Serpent128> — 128-bit block, 128 / 192 / 256-bit key (Anderson / Biham / Knudsen, AES finalist; highest margin).

**Tweakable block ciphers** (<xref:Bodu.Security.Cryptography.TweakableSymmetricAlgorithm> lifecycle, adds `Tweak` / `GenerateTweak()`)

- <xref:Bodu.Security.Cryptography.Threefish256>, <xref:Bodu.Security.Cryptography.Threefish512>, <xref:Bodu.Security.Cryptography.Threefish1024> — 256 / 512 / 1024-bit blocks and keys, all with a 128-bit tweak. Threefish-512 is the recommended general-purpose variant; Threefish-256 underpins Skein-256.
- <xref:Bodu.Security.Cryptography.Serpent256>, <xref:Bodu.Security.Cryptography.Serpent512>, <xref:Bodu.Security.Cryptography.Serpent1024> — wide-block tweakable Serpent constructions (non-standard).

**Stream ciphers** (<xref:Bodu.Security.Cryptography.SymmetricStreamAlgorithm> lifecycle — a `SymmetricAlgorithm` with no block mode or padding; the nonce is the `IV`. Self-inverse and **confidentiality-only — no authentication**)

- <xref:Bodu.Security.Cryptography.ChaCha20> — 256-bit key, 96-bit nonce, 32-bit counter (Bernstein; RFC 8439). The modern default.
- <xref:Bodu.Security.Cryptography.XChaCha20> — 256-bit key, 192-bit nonce; extended-nonce ChaCha20 via an HChaCha20 subkey, so the nonce can be chosen at random.
- <xref:Bodu.Security.Cryptography.Salsa20> — 128- or 256-bit key, 64-bit nonce, 64-bit counter (Bernstein; eSTREAM).
- <xref:Bodu.Security.Cryptography.XSalsa20> — 256-bit key, 192-bit nonce; extended-nonce Salsa20 (NaCl / libsodium).
- <xref:Bodu.Security.Cryptography.Rabbit> — 128-bit key, 64-bit IV (RFC 4503; eSTREAM). Evolving internal state, no seekable counter.
- <xref:Bodu.Security.Cryptography.Hc128> — 128-bit key, 128-bit IV (Wu; eSTREAM). Table-based with an expensive setup.
- <xref:Bodu.Security.Cryptography.IStreamCipher>, <xref:Bodu.Security.Cryptography.TransformMode> — the keystream-cipher contract and the encrypt/decrypt direction selector shared by the stream ciphers above.

**Authenticated stream ciphers** (Poly1305 AEAD over the extended-nonce stream ciphers)

- <xref:Bodu.Security.Cryptography.Poly1305AeadTransform> — abstract base for the stream-cipher AEAD constructions; provides span and `byte[]` `Encrypt` / `Decrypt` with associated data and in-place support.
- <xref:Bodu.Security.Cryptography.XChaCha20Poly1305> — XChaCha20-Poly1305 AEAD with associated data (wire `ciphertext ‖ tag`).
- <xref:Bodu.Security.Cryptography.XSalsa20Poly1305Aead> — XSalsa20-Poly1305 AEAD (RFC 8439 framing) with associated data.
- <xref:Bodu.Security.Cryptography.XSalsa20Poly1305> — the NaCl / libsodium `secretbox` construction (no associated data), with `ToLibsodiumCombined` / `FromLibsodiumCombined` layout converters.
- <xref:Bodu.Security.Cryptography.IAeadTransform>, <xref:Bodu.Security.Cryptography.IStreamAeadTransform> — the AEAD and stream-AEAD transform contracts these constructions implement.

**Cipher composition** — block-cipher contracts, mode transforms, padding strategies

- <xref:Bodu.Security.Cryptography.IBlockCipher> — block-cipher contract; implemented by every cipher and by `AesBlockCipher`.
- <xref:Bodu.Security.Cryptography.AesBlockCipher> — an `IBlockCipher` adapter over the BCL `Aes` engine — the bridge between AES and the AEAD mode transforms.
- <xref:Bodu.Security.Cryptography.SerpentBlockCipher>, <xref:Bodu.Security.Cryptography.ThreefishBlockCipher> — the raw `IBlockCipher` engines (and the <xref:Bodu.Security.Cryptography.SerpentBlockCipherBase> base) that back the Serpent and Threefish `SymmetricAlgorithm` wrappers; use them directly to drive a mode transform without the full algorithm lifecycle.
- <xref:Bodu.Security.Cryptography.BlockCipherTransform>, <xref:Bodu.Security.Cryptography.BlockCipherModeFactory> — compose any `IBlockCipher` with a mode (<xref:Bodu.Security.Cryptography.CipherModeKind> selects from `ECB`, `CBC`, `CFB`, `OFB`, `CTR`, `CTS`, `XTS`) and a padding strategy.
- <xref:Bodu.Security.Cryptography.CipherModeKind>, <xref:Bodu.Security.Cryptography.PaddingModeKind> — the library's extended block-mode and padding enums (the latter mirrors `System.Security.Cryptography.PaddingMode` and adds `ISO7816_4`).
- <xref:Bodu.Security.Cryptography.IBlockCipherModeTransform>, <xref:Bodu.Security.Cryptography.IAeadBlockCipherModeTransform> — per-block / per-stripe transform contracts; the latter adds AEAD nonce / tag / associated-data semantics.
- Classic mode transforms: <xref:Bodu.Security.Cryptography.EcbModeTransform>, <xref:Bodu.Security.Cryptography.CbcModeTransform>, <xref:Bodu.Security.Cryptography.CfbModeTransform>, <xref:Bodu.Security.Cryptography.OfbModeTransform>, <xref:Bodu.Security.Cryptography.CtrModeTransform>, <xref:Bodu.Security.Cryptography.CtsModeTransform>, <xref:Bodu.Security.Cryptography.XtsModeTransform>.
- AEAD mode transforms: <xref:Bodu.Security.Cryptography.GcmModeTransform>, <xref:Bodu.Security.Cryptography.CcmModeTransform>, <xref:Bodu.Security.Cryptography.OcbModeTransform>, <xref:Bodu.Security.Cryptography.EaxModeTransform>, <xref:Bodu.Security.Cryptography.SivModeTransform>, <xref:Bodu.Security.Cryptography.GcmSivModeTransform>.
- Padding: <xref:Bodu.Security.Cryptography.IPaddingStrategy> with built-in strategies <xref:Bodu.Security.Cryptography.Pkcs7Padding>, <xref:Bodu.Security.Cryptography.NoPadding>, <xref:Bodu.Security.Cryptography.Iso10126Padding>, <xref:Bodu.Security.Cryptography.Iso7816_4Padding>, <xref:Bodu.Security.Cryptography.Ansix923Padding>, selected via <xref:Bodu.Security.Cryptography.PaddingFactory>.

**Cryptographic hashes** (`HashAlgorithm` lifecycle)

- <xref:Bodu.Security.Cryptography.Tiger> — 128 / 160 / 192-bit cryptographic digest optimized for 64-bit platforms; two padding variants (Tiger / Tiger2).
- <xref:Bodu.Security.Cryptography.CubeHash> — Bernstein's SHA-3 competition candidate.
- <xref:Bodu.Security.Cryptography.Snefru128>, <xref:Bodu.Security.Cryptography.Snefru256> — Ralph Merkle's hash (**cryptanalytically broken**; included for research and interoperability).
- <xref:Bodu.Security.Cryptography.Whirlpool> — 512-bit digest (ISO/IEC 10118-3) with an AES-derived round function.
- <xref:Bodu.Security.Cryptography.Blake2b>, <xref:Bodu.Security.Cryptography.Blake2s>, <xref:Bodu.Security.Cryptography.Blake3> — modern high-throughput digests; BLAKE3 is parallel and tree-structured.
- <xref:Bodu.Security.Cryptography.Skein256>, <xref:Bodu.Security.Cryptography.Skein512>, <xref:Bodu.Security.Cryptography.Skein1024> — Skein UBI-mode digests built on Threefish.
- <xref:Bodu.Security.Cryptography.Shake> — Keccak XOF (FIPS 202).
- <xref:Bodu.Security.Cryptography.MerkleTreeHash>, <xref:Bodu.Security.Cryptography.ParallelMerkleTreeHash> — Merkle-tree root hashing with partial verification.

**Keyed hashes / MACs**

- <xref:Bodu.Security.Cryptography.SipHash64> — 64-bit PRF; collision-resistant for hash-table protection.
- <xref:Bodu.Security.Cryptography.SipHash128> — 128-bit SipHash variant for longer-output keyed hashing.
- <xref:Bodu.Security.Cryptography.Poly1305> — one-time authenticator (RFC 8439); pairs with a stream cipher for an AEAD construction.

**ASCON family — NIST SP 800-232**

- <xref:Bodu.Security.Cryptography.AsconHash256>, <xref:Bodu.Security.Cryptography.AsconHashA256> — 256-bit sponge digests (12- and 8-round variants), over the shared <xref:Bodu.Security.Cryptography.AsconHash`1> base.
- <xref:Bodu.Security.Cryptography.AsconXof128>, <xref:Bodu.Security.Cryptography.AsconCxof128> — variable-length / customizable XOF.
- <xref:Bodu.Security.Cryptography.AsconAead128> — sponge-based authenticated encryption (no separate block cipher required).

**Extensions and helpers**

- <xref:Bodu.Security.Cryptography.Extensions.SymmetricAlgorithmExtensions>, <xref:Bodu.Security.Cryptography.Extensions.TweakableSymmetricAlgorithmExtensions>, <xref:Bodu.Security.Cryptography.Extensions.AeadBlockCipherModeTransformExtensions>, <xref:Bodu.Security.Cryptography.Extensions.HashAlgorithmExtensions>, <xref:Bodu.Security.Cryptography.Extensions.ICryptoTransformExtensions> — ergonomic one-shot, async, and verify helpers.
- Secure-zeroization, padding, and cryptographically secure random key/IV/tweak generation helpers ship as internal infrastructure; consumers reach them indirectly through the extension surfaces above (for example `SymmetricAlgorithmExtensions.GenerateNonce`, `HashAlgorithmExtensions.VerifyHash`).
- <xref:Bodu.Security.Cryptography.HashAlgorithmHelper>, <xref:Bodu.Security.Cryptography.HashAlgorithmFactory>, <xref:Bodu.Security.Cryptography.IHashAlgorithmFactory`1>, <xref:Bodu.Security.Cryptography.DelegateHashAlgorithmFactory`1> — helper utilities for `HashAlgorithm` consumers and factory abstractions used by the keyed / Merkle constructions.
- <xref:Bodu.Security.Cryptography.KeyedDeferredFinalBlockHashAlgorithm`1> — abstract base for keyed hashes that defer the final block (the extension point shared by the keyed-hash constructions).

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
  - <xref:Bodu.Security.Cryptography.Blowfish> is well-studied but dated; prefer Threefish, AES, or Camellia / Twofish / Serpent for new designs — its 64-bit block limits the safe encryption volume per key.
  - <xref:Bodu.Security.Cryptography.Snefru128> and <xref:Bodu.Security.Cryptography.Snefru256> are cryptanalytically broken — interop / research only.
  - <xref:Bodu.Security.Cryptography.SipHash64> is keyed and collision-resistant but short-output; use it for hash-table protection and message authentication over small inputs, not as a drop-in for a MAC like HMAC-SHA256.
  - <xref:Bodu.Security.Cryptography.Tiger> is a classic cryptographic hash. Prefer BCL-provided SHA-2 / SHA-3 for new designs; use Tiger for interoperability with existing Tiger-based systems.
  - The stream ciphers (<xref:Bodu.Security.Cryptography.ChaCha20>, <xref:Bodu.Security.Cryptography.XChaCha20>, <xref:Bodu.Security.Cryptography.Salsa20>, <xref:Bodu.Security.Cryptography.XSalsa20>, <xref:Bodu.Security.Cryptography.Rabbit>, <xref:Bodu.Security.Cryptography.Hc128>) are **raw and unauthenticated**. A `(key, nonce)` pair must encrypt at most one message — reuse reveals the XOR of the plaintexts — and ciphertext integrity is not protected. Pair them with a MAC (encrypt-then-MAC with <xref:Bodu.Security.Cryptography.Poly1305>) or prefer an AEAD construction. A 64-bit nonce (`Salsa20`, `Rabbit`) is too short to choose randomly; use a counter, or an extended-nonce variant (`XChaCha20` / `XSalsa20`).
  - For error-detection and hash-table distribution (CRC, Fletcher, Adler, FNV, CityHash, MurmurHash3, Pearson, and the classic short hashes) use the non-cryptographic types in <xref:Bodu.IO.Hashing>.
- **Thread safety.** Instances of the cipher and hash types follow the standard .NET convention: **not thread-safe** during a single `TransformBlock` / `ComputeHash` / encryption session. Create one instance per logical operation, or synchronize externally. AEAD mode transforms (`GcmModeTransform`, etc.) are **single-use per message** — construct a fresh transform on the encrypt side and another on the decrypt side.
- **Allocation discipline.** Hot-path types allocate their working buffers in the constructor and reuse them; `CryptoHelpers.ClearIfNotNull` (and equivalents) zero secret material at disposal time.
- **Determinism and portability.** All algorithms produce identical byte-for-byte output across platforms and architectures for the same input and configuration.
- **See also:** <xref:Bodu.IO.Hashing> for CRC, Fletcher, Adler, and other non-cryptographic hashes; the [Bodu.Security.Cryptography introduction](~/docs/cryptography/index.md), the [encryption basics guide](~/guides/cryptography/encryption-basics.md), the [AEAD modes guide](~/guides/cryptography/aead-modes.md), and the [hashing guide](~/guides/cryptography/hashing.md).
