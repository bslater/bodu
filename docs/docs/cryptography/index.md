---
title: Bodu.Security.Cryptography — Introduction
---

# Bodu.Security.Cryptography

**Bodu.Security.Cryptography** is the cryptographic primitives package of the Bodu suite — managed block ciphers, authenticated encryption, keyed hashes, and cryptographic digests with a formal adversary model. Everything plugs into the standard BCL contracts (<xref:System.Security.Cryptography.SymmetricAlgorithm?displayProperty=nameWithType>, <xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType>, and Bodu's own `IBlockCipher` / `TweakableSymmetricAlgorithm`), so any code that already speaks .NET cryptography can adopt these types without changes.

The library lives in two namespaces: `Bodu.Security.Cryptography` for primitives, and `Bodu.Security.Cryptography.Extensions` for ergonomic helpers.

## The shape of the library

![Algorithm taxonomy across both libraries](../../images/diagrams/algorithm-taxonomy.svg)

The package contains five subfamilies. They share base classes but differ structurally — see [Algorithm families](../algorithm-families.md) for the cross-cutting taxonomy.

## Subfamilies and headline types

### Standard symmetric block ciphers
*`SymmetricAlgorithm` lifecycle: configure `Key`, `IV`, `BlockMode`, `Padding`; call `CreateEncryptor()` / `CreateDecryptor()` or the `Encrypt` / `Decrypt` extension methods.*

| Type | Block | Key | Notes |
|---|---|---|---|
| `Skipjack` | 64 bits | 80 bits | NSA design (declassified 1998); legacy interoperability only. |
| `Blowfish` | 64 bits | 32–448 bits | Schneier 1993; expensive key schedule. |
| `Camellia` | 128 bits | 128 / 192 / 256 bits | NTT/Mitsubishi (RFC 3713); ISO/IEC 18033-3. |
| `Twofish` | 128 bits | 128 / 192 / 256 bits | Schneier et al., AES finalist (1998). |
| `Serpent128` | 128 bits | 128 / 192 / 256 bits | Anderson/Biham/Knudsen, AES finalist; highest margin. |

### Tweakable symmetric block ciphers
*`TweakableSymmetricAlgorithm` lifecycle adds `Tweak` and `GenerateTweak()` to the standard surface — domain separation without re-keying.*

| Type | Block | Key | Tweak | Notes |
|---|---|---|---|---|
| `Threefish256` | 256 bits | 256 bits | 128 bits | Core of Skein-256. |
| `Threefish512` | 512 bits | 512 bits | 128 bits | Core of Skein-512; recommended general-purpose variant. |
| `Threefish1024` | 1024 bits | 1024 bits | 128 bits | Highest margin; most padding waste for short messages. |
| `Serpent256` | 256 bits | 256 bits | 128 bits | Wide-block tweakable Serpent — non-standard construction. |
| `Serpent512` | 512 bits | 512 bits | 128 bits | Wide-block tweakable Serpent — non-standard construction. |
| `Serpent1024` | 1024 bits | 1024 bits | 128 bits | Wide-block tweakable Serpent — non-standard construction. |

### Cipher composition (modes, padding, AEAD transforms)
*Lower-level building blocks that the `SymmetricAlgorithm` wrappers compose internally — also usable directly via `IBlockCipher` for pairing AES with the AEAD mode transforms.*

| Type | Provides |
|---|---|
| `IBlockCipher` | Block-cipher contract; implemented by every cipher and by `AesBlockCipher`. |
| `AesBlockCipher` | `IBlockCipher` over the BCL `Aes` engine — the bridge between AES and the AEAD mode transforms. |
| `BlockCipherTransform` | `ICryptoTransform` adapter over an `IBlockCipher` and a mode. |
| `BlockCipherModeFactory` | Builds a mode transform (`CbcModeTransform`, `CtrModeTransform`, …) from a `CipherBlockMode` enum. |
| `CipherBlockMode` | Enum: `Ecb`, `Cbc`, `Cfb`, `Ofb`, `Ctr`, `Cts`, `Xts`. |
| `IBlockCipherModeTransform` | Mode-transform contract (per-block / per-stripe). |
| `IAeadBlockCipherModeTransform` | AEAD-specific extension of the above; includes nonce / tag / associated-data semantics. |
| `EcbModeTransform`, `CbcModeTransform`, `CfbModeTransform`, `OfbModeTransform`, `CtrModeTransform`, `CtsModeTransform`, `XtsModeTransform` | Standard cipher modes. |
| `GcmModeTransform`, `CcmModeTransform`, `OcbModeTransform`, `EaxModeTransform`, `SivModeTransform`, `GcmSivModeTransform` | AEAD mode transforms. |
| `IPaddingStrategy` | Padding contract. |
| `Pkcs7Padding`, `ZeroPadding`, `NoPadding`, `Iso10126Padding`, `Iso7816_4Padding`, `Ansix923Padding` | Built-in padding strategies. |
| `PaddingFactory` + `BoduPaddingMode` | Selects a padding strategy from an enum. |

### Cryptographic hashes
*`HashAlgorithm` lifecycle: `Append` then `GetHashAndReset()` (or BCL `ComputeHash`).*

| Type | Output | Shape |
|---|---|---|
| `Tiger` | 128 / 160 / 192 bits | Plain digest (1995); two padding variants via `TigerHashingVariant`. |
| `CubeHash` | Configurable | Plain digest; tunable rounds / block size. |
| `Snefru128` / `Snefru256` | 128 / 256 bits | Plain digest — **cryptanalytically broken**, interop only. |
| `Whirlpool` | 512 bits | Plain digest (ISO/IEC 10118-3); `WhirlpoolVersion` selects the variant. |
| `Blake2b` / `Blake2s` | Configurable | Modern high-throughput plain digest. |
| `Blake3` | Configurable | Parallel, tree-structured digest. |
| `Skein256` / `Skein512` / `Skein1024` | Configurable | Plain digest built on Threefish in UBI mode (`SkeinTweakType`). |
| `Shake` | Variable | Keccak XOF (FIPS 202). |
| `AsconHash256` / `AsconHashA256` | 256 bits | NIST SP 800-232 sponge digest; 12 / 8 round variants. |
| `AsconXof128` / `AsconCxof128` | Variable | NIST SP 800-232 XOF / customisable XOF. |
| `MerkleTreeHash` / `ParallelMerkleTreeHash` | Configurable | Tree hashing over any inner `HashAlgorithm`. |
| `BlockHashAlgorithm` / `BufferedBlockHashAlgorithm` / `DeferredFinalBlockHashAlgorithm` | — | Internal base classes for block-oriented digests. |
| `HashAlgorithmFactory` / `IHashAlgorithmFactory` / `DelegateHashAlgorithmFactory` | — | Factory abstraction over `HashAlgorithm` for keyed / Merkle constructions. |

### Keyed hashes / MACs
*`HashAlgorithm` with a required `Key` property.*

| Type | Output | Subtype |
|---|---|---|
| `SipHash64` | 64 bits | PRF; default rounds SipHash-2-4. |
| `SipHash128` | 128 bits | PRF; wider output for routing / sharding. |
| `Poly1305` | 128 bits | One-time authenticator (RFC 8439). |

### ASCON family — multi-role
*Spans hash, XOF, and AEAD under a single sponge permutation. NIST SP 800-232.*

| Type | Role |
|---|---|
| `AsconHash256` / `AsconHashA256` | 256-bit cryptographic digest |
| `AsconXof128` / `AsconCxof128` | Variable-length / customisable XOF |
| `AsconAead128` | 128-bit-key authenticated encryption |
| `AsconHash` / `AsconXof` / `AsconState` | Internal building blocks. |

### Extensions

| Type | Provides |
|---|---|
| `SymmetricAlgorithmExtensions` | `Encrypt`, `Decrypt`, `EncryptAsync`, `DecryptAsync`, `TryCreateEncryptor`, `TryCreateDecryptor`. |
| `TweakableSymmetricAlgorithmExtensions` | `TryCreateEncryptor` / `TryCreateDecryptor` overloads that accept a tweak. |
| `AeadBlockCipherModeTransformExtensions` | One-shot AEAD encrypt / decrypt over `IBlockCipher` + `IAeadBlockCipherModeTransform`. |
| `HashAlgorithmExtensions` | `AppendData`, `AppendDataAsync`, `VerifyHash`, `VerifyHashAsync`, `TryVerifyHash`, `TryVerifyHashAsync`. |
| `ICryptoTransformExtensions` | `Transform`, `TransformAsync`, `TransformBlock`, `TransformFinalBlock` over a stream. |

### Helpers

| Type | Purpose |
|---|---|
| `CryptoHelpers` | Random key/IV/tweak generation, padding helpers, secure-clear helpers. |
| `HashAlgorithmHelper` | Helper utilities for `HashAlgorithm` consumers. |

## Scenarios this library covers

| Scenario | Reach for |
|---|---|
| Encrypt a message under a key | `Threefish512`, `Camellia`, `Twofish`, `Serpent128`, `Blowfish`, `Skipjack` |
| Per-record / per-sector encryption without re-keying | `Threefish256` / `Threefish512` / `Threefish1024` with `Tweak` |
| Authenticated encryption (encrypt + integrity in one) | `AesBlockCipher` + `GcmModeTransform`, `AsconAead128` |
| Hash-table flooding defence | `SipHash64` / `SipHash128` |
| One-time authenticator (e.g. paired with ChaCha20) | `Poly1305` |
| Cryptographic digest for content addressing | `Tiger`, `CubeHash`, `AsconHash256`, `Blake2b`, `Whirlpool`, `Skein512` |
| Variable-length output | `AsconXof128`, `AsconCxof128`, `Shake`, `Blake3` |
| Tree / Merkle hashing for verifiable inclusion proofs | `MerkleTreeHash`, `ParallelMerkleTreeHash` |

## Where to go next

- **[Getting started](getting-started.md)** — install + minimal sample for a cipher, an AEAD round-trip, a keyed hash, and a digest.
- **[Algorithm families](../algorithm-families.md)** — cipher subtypes, hash structural shapes, and the cross-library map.
- **[Bodu.Security.Cryptography guides](../../guides/cryptography/index.md)** — recipe-style walk-throughs.
- **[Bodu.Security.Cryptography API reference](../../apidoc/Bodu.Security.Cryptography.md)** — full type-by-type docs.
