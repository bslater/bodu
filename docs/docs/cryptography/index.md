---
title: Bodu.Security.Cryptography — Introduction
---

# Bodu.Security.Cryptography

**Bodu.Security.Cryptography** is the cryptographic primitives package of the Bodu suite — managed block ciphers, authenticated encryption, keyed hashes, and cryptographic digests with a formal adversary model. Everything plugs into the standard BCL contracts (<xref:System.Security.Cryptography.SymmetricAlgorithm?displayProperty=nameWithType>, <xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType>, and Bodu's own `IBlockCipher` / `TweakableSymmetricAlgorithm`), so any code that already speaks .NET cryptography can adopt these types without changes.

The library lives in two namespaces: `Bodu.Security.Cryptography` for primitives, and `Bodu.Security.Cryptography.Extensions` for ergonomic helpers.

## The shape of the library

![Algorithm taxonomy across both libraries](../../images/diagrams/algorithm-taxonomy.svg)

Every algorithm here is designed against a formal **adversary model**: it must be computationally infeasible for an attacker — even one who knows the algorithm, observes many inputs and outputs, and chooses inputs adaptively — to forge, invert, or find collisions. That is the line between this package and [Bodu.IO.Hashing](../io-hashing/index.md), whose fingerprints and checksums carry *no* adversary model and must never be used where an attacker can choose the input.

The package contains six subfamilies. They share BCL base classes but differ structurally in what they consume and produce:

![Structural input and output comparison across the cryptographic families](../../images/diagrams/algorithm-io-model.svg)

- **Cryptographic hash** — a one-way function compressing arbitrary input to a fixed digest, with pre-image, second-pre-image, and collision resistance. Three structural shapes: *plain digest* (fixed output), *extendable output* (XOF — squeeze any number of bytes), and *tree* (parallel leaves combined into a verifiable root). Use for content addressing, integrity verification, and signature inputs — not for authentication on its own.
- **Keyed hash / MAC** — a secret key plus a message yields an authentication tag that no one can forge without the key. Two subtypes: a reusable *PRF* (SipHash — one key authenticates many messages) and a *one-time authenticator* (Poly1305 — the key must never be reused).
- **Symmetric cipher** — reversible encryption under a key, in four subtypes: a *standard block cipher*; a *tweakable block cipher*, where a public **tweak** gives per-record or per-sector domain separation without re-keying; a *stream cipher*, which XORs a key/nonce-derived keystream over data of any length (raw confidentiality, no authentication); and *AEAD*, which encrypts and authenticates in a single pass.

> **Keyed hash vs cipher.** Both take a key, but they serve opposite purposes. A cipher transforms plaintext to ciphertext and back without summarizing; a MAC summarizes a message into a fixed-size tag without encrypting. Use both together — encrypt-then-MAC, or an AEAD mode — when you need confidentiality *and* integrity.

> **ASCON is multi-role.** The ASCON family (NIST SP 800-232) spans the cryptographic-hash, XOF, and AEAD roles under a single sponge permutation, which makes it a compact one-primitive choice for constrained environments. It appears in both the hash and AEAD tables below.

## Subfamilies and headline types

### Standard symmetric block ciphers
*`SymmetricAlgorithm` lifecycle: configure `Key`, `IV`, the Bodu-specific `BlockMode` (and the inherited `Padding`), then call `CreateEncryptor()` / `CreateDecryptor()` or the `Encrypt` / `Decrypt` extension methods.*

| Type | Block | Key | Notes |
|---|---|---|---|
| <xref:Bodu.Security.Cryptography.Skipjack> | 64 bits | 80 bits | NSA design (declassified 1998); legacy interoperability only. |
| <xref:Bodu.Security.Cryptography.Blowfish> | 64 bits | 32–448 bits | Schneier 1993; expensive key schedule. |
| <xref:Bodu.Security.Cryptography.Camellia> | 128 bits | 128 / 192 / 256 bits | NTT/Mitsubishi (RFC 3713); ISO/IEC 18033-3. |
| <xref:Bodu.Security.Cryptography.Twofish> | 128 bits | 128 / 192 / 256 bits | Schneier et al., AES finalist (1998). |
| <xref:Bodu.Security.Cryptography.Serpent128> | 128 bits | 128 / 192 / 256 bits | Anderson/Biham/Knudsen, AES finalist; highest margin. |

### Tweakable symmetric block ciphers
*<xref:Bodu.Security.Cryptography.TweakableSymmetricAlgorithm> lifecycle adds `Tweak` and `GenerateTweak()` to the standard surface — domain separation without re-keying.*

| Type | Block | Key | Tweak | Notes |
|---|---|---|---|---|
| <xref:Bodu.Security.Cryptography.Threefish256> | 256 bits | 256 bits | 128 bits | Core of Skein-256. |
| <xref:Bodu.Security.Cryptography.Threefish512> | 512 bits | 512 bits | 128 bits | Core of Skein-512; recommended general-purpose variant. |
| <xref:Bodu.Security.Cryptography.Threefish1024> | 1024 bits | 1024 bits | 128 bits | Highest margin; most padding waste for short messages. |
| <xref:Bodu.Security.Cryptography.Serpent256> | 256 bits | 256 bits | 128 bits | Wide-block tweakable Serpent — non-standard construction. |
| <xref:Bodu.Security.Cryptography.Serpent512> | 512 bits | 512 bits | 128 bits | Wide-block tweakable Serpent — non-standard construction. |
| <xref:Bodu.Security.Cryptography.Serpent1024> | 1024 bits | 1024 bits | 128 bits | Wide-block tweakable Serpent — non-standard construction. |

### Stream ciphers
*<xref:Bodu.Security.Cryptography.StreamCipherAlgorithm> lifecycle (a `SymmetricAlgorithm` with no block mode or padding): configure `Key` and `IV` (the nonce), then `CreateEncryptor()` / `Encrypt`. Self-inverse and raw — **confidentiality only, no authentication**. Never reuse a `(key, nonce)` pair; pair with a MAC or prefer AEAD.*

| Type | Key | Nonce / IV | Notes |
|---|---|---|---|
| <xref:Bodu.Security.Cryptography.ChaCha20> | 256 bits | 96 bits | Bernstein (RFC 8439); the modern default. |
| <xref:Bodu.Security.Cryptography.XChaCha20> | 256 bits | 192 bits | Extended-nonce ChaCha20 — nonce safe to choose at random. |
| <xref:Bodu.Security.Cryptography.Salsa20> | 128 / 256 bits | 64 bits | Bernstein (eSTREAM); 64-bit nonce requires a counter. |
| <xref:Bodu.Security.Cryptography.XSalsa20> | 256 bits | 192 bits | Extended-nonce Salsa20 (NaCl / libsodium). |
| <xref:Bodu.Security.Cryptography.Rabbit> | 128 bits | 64 bits | RFC 4503; evolving internal state (no seekable counter). |
| <xref:Bodu.Security.Cryptography.Hc128> | 128 bits | 128 bits | Wu (eSTREAM); table-based, expensive setup. |

### Cipher composition (modes, padding, AEAD transforms)
*Lower-level building blocks that the `SymmetricAlgorithm` wrappers compose internally — also usable directly via <xref:Bodu.Security.Cryptography.IBlockCipher> for pairing AES with the AEAD mode transforms.*

| Type | Provides |
|---|---|
| <xref:Bodu.Security.Cryptography.IBlockCipher> | Block-cipher contract; implemented by every cipher and by `AesBlockCipher`. |
| <xref:Bodu.Security.Cryptography.AesBlockCipher> | `IBlockCipher` over the BCL `Aes` engine — the bridge between AES and the AEAD mode transforms. |
| <xref:Bodu.Security.Cryptography.BlockCipherTransform> | `ICryptoTransform` adapter over an `IBlockCipher` and a mode. |
| <xref:Bodu.Security.Cryptography.BlockCipherModeFactory> | Builds a mode transform (`CbcModeTransform`, `CtrModeTransform`, …) from a <xref:Bodu.Security.Cryptography.CipherModeKind> value. |
| <xref:Bodu.Security.Cryptography.CipherModeKind> | Enum: `ECB`, `CBC`, `CFB`, `OFB`, `CTR`, `CTS`, `XTS`. |
| <xref:Bodu.Security.Cryptography.IBlockCipherModeTransform> | Mode-transform contract (per-block / per-stripe). |
| <xref:Bodu.Security.Cryptography.IAeadBlockCipherModeTransform> | AEAD-specific extension of the above; includes nonce / tag / associated-data semantics. |
| <xref:Bodu.Security.Cryptography.EcbModeTransform>, <xref:Bodu.Security.Cryptography.CbcModeTransform>, <xref:Bodu.Security.Cryptography.CfbModeTransform>, <xref:Bodu.Security.Cryptography.OfbModeTransform>, <xref:Bodu.Security.Cryptography.CtrModeTransform>, <xref:Bodu.Security.Cryptography.CtsModeTransform>, <xref:Bodu.Security.Cryptography.XtsModeTransform> | Standard cipher modes. |
| <xref:Bodu.Security.Cryptography.GcmModeTransform>, <xref:Bodu.Security.Cryptography.CcmModeTransform>, <xref:Bodu.Security.Cryptography.OcbModeTransform>, <xref:Bodu.Security.Cryptography.EaxModeTransform>, <xref:Bodu.Security.Cryptography.SivModeTransform>, <xref:Bodu.Security.Cryptography.GcmSivModeTransform> | AEAD mode transforms. |
| <xref:Bodu.Security.Cryptography.IPaddingStrategy> | Padding contract. |
| <xref:Bodu.Security.Cryptography.Pkcs7Padding>, <xref:Bodu.Security.Cryptography.NoPadding>, <xref:Bodu.Security.Cryptography.Iso10126Padding>, <xref:Bodu.Security.Cryptography.Iso7816_4Padding>, <xref:Bodu.Security.Cryptography.Ansix923Padding> | Built-in padding strategies. |
| <xref:Bodu.Security.Cryptography.PaddingFactory> + <xref:Bodu.Security.Cryptography.PaddingModeKind> | Selects a padding strategy from an enum that mirrors `System.Security.Cryptography.PaddingMode` and adds `ISO7816_4`. |

### Cryptographic hashes
*`HashAlgorithm` lifecycle: `Append` then `GetHashAndReset()` (or BCL `ComputeHash`).*

| Type | Output | Shape |
|---|---|---|
| <xref:Bodu.Security.Cryptography.Tiger> | 128 / 160 / 192 bits | Plain digest (1995); two padding variants via `Tiger.HashingVariant`. |
| <xref:Bodu.Security.Cryptography.CubeHash> | Configurable | Plain digest; tunable rounds / block size. |
| <xref:Bodu.Security.Cryptography.Snefru128> / <xref:Bodu.Security.Cryptography.Snefru256> | 128 / 256 bits | Plain digest — **cryptanalytically broken**, interop only. |
| <xref:Bodu.Security.Cryptography.Whirlpool> | 512 bits | Plain digest (ISO/IEC 10118-3); `WhirlpoolVersion` selects the variant. |
| <xref:Bodu.Security.Cryptography.Blake2b> / <xref:Bodu.Security.Cryptography.Blake2s> | Configurable | Modern high-throughput plain digest. |
| <xref:Bodu.Security.Cryptography.Blake3> | Configurable | Parallel, tree-structured digest. |
| <xref:Bodu.Security.Cryptography.Skein256> / <xref:Bodu.Security.Cryptography.Skein512> / <xref:Bodu.Security.Cryptography.Skein1024> | Configurable | Plain digest built on Threefish in UBI mode. |
| <xref:Bodu.Security.Cryptography.Shake> | Variable | Keccak XOF (FIPS 202). |
| <xref:Bodu.Security.Cryptography.AsconHash256> / <xref:Bodu.Security.Cryptography.AsconHashA256> | 256 bits | NIST SP 800-232 sponge digest; 12 / 8 round variants. |
| <xref:Bodu.Security.Cryptography.AsconXof128> / <xref:Bodu.Security.Cryptography.AsconCxof128> | Variable | NIST SP 800-232 XOF / customizable XOF. |
| <xref:Bodu.Security.Cryptography.MerkleTreeHash> / <xref:Bodu.Security.Cryptography.ParallelMerkleTreeHash> | Configurable | Tree hashing over any inner `HashAlgorithm`. |
| <xref:Bodu.Security.Cryptography.BlockHashAlgorithm`1>, <xref:Bodu.Security.Cryptography.BufferedBlockHashAlgorithm`1>, <xref:Bodu.Security.Cryptography.DeferredFinalBlockHashAlgorithm`1>, <xref:Bodu.Security.Cryptography.KeyedBlockHashAlgorithm`1> | — | Abstract bases for block-oriented digests (extension points). |
| <xref:Bodu.Security.Cryptography.HashAlgorithmFactory>, <xref:Bodu.Security.Cryptography.IHashAlgorithmFactory`1>, <xref:Bodu.Security.Cryptography.DelegateHashAlgorithmFactory`1> | — | Factory abstraction over `HashAlgorithm` for keyed / Merkle constructions. |

### Keyed hashes / MACs
*`HashAlgorithm` with a required `Key` property.*

| Type | Output | Subtype |
|---|---|---|
| <xref:Bodu.Security.Cryptography.SipHash64> | 64 bits | PRF; default rounds SipHash-2-4. |
| <xref:Bodu.Security.Cryptography.SipHash128> | 128 bits | PRF; wider output for routing / sharding. |
| <xref:Bodu.Security.Cryptography.Poly1305> | 128 bits | One-time authenticator (RFC 8439). |

### ASCON family — multi-role
*Spans hash, XOF, and AEAD under a single sponge permutation. NIST SP 800-232.*

| Type | Role |
|---|---|
| <xref:Bodu.Security.Cryptography.AsconHash256> / <xref:Bodu.Security.Cryptography.AsconHashA256> | 256-bit cryptographic digest |
| <xref:Bodu.Security.Cryptography.AsconXof128> / <xref:Bodu.Security.Cryptography.AsconCxof128> | Variable-length / customizable XOF |
| <xref:Bodu.Security.Cryptography.AsconAead128> | 128-bit-key authenticated encryption |

### Extensions

| Type | Provides |
|---|---|
| <xref:Bodu.Security.Cryptography.Extensions.SymmetricAlgorithmExtensions> | `Encrypt`, `Decrypt`, `EncryptAsync`, `DecryptAsync`, `TryCreateEncryptor`, `TryCreateDecryptor`. |
| <xref:Bodu.Security.Cryptography.Extensions.TweakableSymmetricAlgorithmExtensions> | `TryCreateEncryptor` / `TryCreateDecryptor` overloads that accept a tweak. |
| <xref:Bodu.Security.Cryptography.Extensions.AeadBlockCipherModeTransformExtensions> | One-shot AEAD encrypt / decrypt over `IBlockCipher` + `IAeadBlockCipherModeTransform`. |
| <xref:Bodu.Security.Cryptography.Extensions.HashAlgorithmExtensions> | `AppendData`, `AppendDataAsync`, `VerifyHash`, `VerifyHashAsync`, `TryVerifyHash`, `TryVerifyHashAsync`. |
| <xref:Bodu.Security.Cryptography.Extensions.ICryptoTransformExtensions> | `Transform`, `TransformAsync`, `TransformBlock`, `TransformFinalBlock` over a stream. |

### Helpers

| Type | Purpose |
|---|---|
| <xref:Bodu.Security.Cryptography.CryptoHelpers> | Random key/IV/tweak generation, padding helpers, secure-clear helpers. |
| <xref:Bodu.Security.Cryptography.HashAlgorithmHelper> | Helper utilities for `HashAlgorithm` consumers. |

## Scenarios this library covers

| Scenario | Reach for |
|---|---|
| Encrypt a message under a key | `Threefish512`, `Camellia`, `Twofish`, `Serpent128`, `Blowfish`, `Skipjack` |
| Per-record / per-sector encryption without re-keying | `Threefish256` / `Threefish512` / `Threefish1024` with `Tweak` |
| Stream encryption of arbitrary-length data (no padding) | `ChaCha20`, `XChaCha20`, `Salsa20`, `XSalsa20`, `Rabbit`, `Hc128` |
| Authenticated encryption (encrypt + integrity in one) | `AesBlockCipher` + `GcmModeTransform`, `AsconAead128` |
| Hash-table flooding defense | `SipHash64` / `SipHash128` |
| One-time authenticator (e.g. paired with `ChaCha20`) | `Poly1305` |
| Cryptographic digest for content addressing | `Tiger`, `CubeHash`, `AsconHash256`, `Blake2b`, `Whirlpool`, `Skein512` |
| Variable-length output | `AsconXof128`, `AsconCxof128`, `Shake`, `Blake3` |
| Tree / Merkle hashing for verifiable inclusion proofs | `MerkleTreeHash`, `ParallelMerkleTreeHash` |

## Where to go next

- **[Core concepts](concepts.md)** — glossary the rest of the documentation assumes.
- **[Getting started](getting-started.md)** — install + minimal sample for a cipher, an AEAD round-trip, a keyed hash, and a digest.
- **[Bodu.Security.Cryptography guides](../../guides/cryptography/index.md)** — recipe-style walk-throughs.
- **[Bodu.Security.Cryptography API reference](xref:Bodu.Security.Cryptography)** — full type-by-type docs.
- **For non-cryptographic checksums and fingerprints** (CRC, Fletcher, Adler, FNV, CityHash, MurmurHash3), see [Bodu.IO.Hashing](../io-hashing/index.md).
