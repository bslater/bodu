---
title: Bodu.Security.Cryptography — Introduction
---

# Bodu.Security.Cryptography

![Bodu.Security.Cryptography](../../images/hero-crypto.svg)

**Bodu.Security.Cryptography** is the cryptographic primitives package of the Bodu suite, and one half of the **[Hashing & Cryptography](../topics/hashing-and-cryptography.md)** topic — managed block ciphers, authenticated encryption, keyed hashes, cryptographic digests, elliptic-curve and post-quantum public-key primitives, and password-hashing and key-derivation functions, all with a formal adversary model. Everything plugs into the standard BCL contracts (<xref:System.Security.Cryptography.SymmetricAlgorithm?displayProperty=nameWithType>, <xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType>, <xref:System.Security.Cryptography.AsymmetricAlgorithm?displayProperty=nameWithType>, and Bodu's own `IBlockCipher` / `TweakableSymmetricAlgorithm`), so any code that already speaks .NET cryptography can adopt these types without changes.

The library lives in two namespaces: `Bodu.Security.Cryptography` for primitives, and `Bodu.Security.Cryptography.Extensions` for ergonomic helpers.

> [!IMPORTANT]
> **Cryptographic primitives are easy to misuse.** Even algorithm-correct implementations leak security when used incorrectly. Before adopting this library in production, internalise these rules:
>
> - **Never reuse a nonce or IV under the same key.** Stream ciphers and most AEAD modes lose all confidentiality on nonce reuse. Use a counter or `RandomNumberGenerator.GetBytes` for unpredictability where required.
> - **Always verify the AEAD authentication tag** before trusting decrypted plaintext. The library's AEAD transforms reject mismatched tags with `CryptographicException` — do not catch and ignore.
> - **Compare tags and digests in constant time.** Use `CryptographicOperations.FixedTimeEquals` or the BCL constant-time helpers when checking MAC equality.
> - **Prefer AEAD over encrypt-then-MAC-by-hand.** Authenticated modes (GCM, OCB, EAX, SIV) bundle confidentiality and authenticity in a single primitive with fewer pitfalls.
> - **Prefer the BCL where it covers your case.** `System.Security.Cryptography` ships hardware-accelerated AES, AES-GCM, and SHA-2/3 implementations. Reach for the Bodu primitives when you need an algorithm the BCL does not ship (Threefish, Camellia, Ascon, BLAKE2/3, Skein, …).
> - **Hash passwords with a memory-hard KDF**, never a bare digest. Use `Argon2id` (or `Scrypt`) with a per-password salt for stored passwords; reserve `Hkdf` for stretching *high-entropy* inputs such as a Diffie-Hellman shared secret or a KEM output.
> - **Treat every verification failure as fatal.** A failed signature check, KEM decapsulation, or HPKE open must abort the operation — do not fall back to the unverified data. Import only public keys you obtained over a trusted channel.
>
> See the [Core concepts](concepts.md) page for the full safety vocabulary and the [cipher-modes](../../guides/cryptography/cipher-modes.md) and [AEAD-modes](../../guides/cryptography/aead-modes.md) guides for worked-example walkthroughs.

## The shape of the library

![Algorithm taxonomy across both libraries](../../images/diagrams/algorithm-taxonomy.svg)

Every algorithm here is designed against a formal **adversary model**: it must be computationally infeasible for an attacker — even one who knows the algorithm, observes many inputs and outputs, and chooses inputs adaptively — to forge, invert, or find collisions. That is the line between this package and [Bodu.IO.Hashing](../io-hashing/index.md), whose fingerprints and checksums carry *no* adversary model and must never be used where an attacker can choose the input.

The package spans five families. They share BCL base classes but differ structurally in what they consume and produce:

![Structural input and output comparison across the cryptographic families](../../images/diagrams/algorithm-io-model.svg)

- **Cryptographic hash** — a one-way function compressing arbitrary input to a fixed digest, with pre-image, second-pre-image, and collision resistance. Three structural shapes: *plain digest* (fixed output), *extendable output* (XOF — squeeze any number of bytes), and *tree* (parallel leaves combined into a verifiable root). Use for content addressing, integrity verification, and signature inputs — not for authentication on its own.
- **Keyed hash / MAC** — a secret key plus a message yields an authentication tag that no one can forge without the key. Two subtypes: a reusable *PRF* (SipHash — one key authenticates many messages) and a *one-time authenticator* (Poly1305 — the key must never be reused).
- **Symmetric cipher** — reversible encryption under a key, in four subtypes: a *standard block cipher*; a *tweakable block cipher*, where a public **tweak** gives per-record or per-sector domain separation without re-keying; a *stream cipher*, which XORs a key/nonce-derived keystream over data of any length (raw confidentiality, no authentication); and *AEAD*, which encrypts and authenticates in a single pass.
- **Asymmetric (public-key)** — a key *pair*, where one half is published and the other kept secret, in four roles: a *signature* scheme (sign with the private key, verify with the public key — Ed25519 and the post-quantum ML-DSA); *key agreement* (two public keys derive a shared secret — X25519); a *KEM* (encapsulate a fresh secret to a public key — the post-quantum ML-KEM); and *HPKE*, which seals a message to a recipient's public key by combining a KEM, a KDF, and an AEAD.
- **Key derivation & password hashing** — turns one secret into key material. A *memory-hard password hash* (Argon2id, scrypt) stretches a low-entropy password so offline guessing is expensive; an *extract-and-expand KDF* (HKDF) derives one or more context-bound keys from a high-entropy input.

> **Keyed hash vs cipher.** Both take a key, but they serve opposite purposes. A cipher transforms plaintext to ciphertext and back without summarizing; a MAC summarizes a message into a fixed-size tag without encrypting. Use both together — encrypt-then-MAC, or an AEAD mode — when you need confidentiality *and* integrity.

> **ASCON is multi-role.** The ASCON family (NIST SP 800-232) spans the cryptographic-hash, XOF, and AEAD roles under a single sponge permutation, which makes it a compact one-primitive choice for constrained environments. It appears in both the hash and AEAD tables below.

## Choosing a primitive

A compact decision table for the most common requirements. The "BCL alternative" column flags the case where `System.Security.Cryptography` already ships a hardware-accelerated implementation — start there unless the algorithm column is the specific reason you reached for Bodu.

| If you need… | Reach for | Output | Standards | BCL alternative |
|---|---|---|---|---|
| **Confidentiality only**, block cipher | `Camellia`, `Twofish`, `Serpent128/256/512/1024`, `Threefish256/512/1024`, `Blowfish`, `Skipjack` | Block-aligned ciphertext + IV | RFC 3713 / FIPS-181 / NIST CFL / Threefish whitepaper | `Aes` (BCL — preferred for 128-bit-block AES) |
| **Confidentiality only**, stream cipher | `ChaCha20`, `XChaCha20`, `Salsa20`, `XSalsa20`, `Rabbit`, `Hc128` | Keystream-XOR ciphertext | RFC 7539 / XSalsa20 paper / eSTREAM | None (for these specific algorithms) |
| **Confidentiality + integrity + authenticity** (AEAD) | `Aes` + `GcmModeTransform` / `CcmModeTransform` / `OcbModeTransform` / `EaxModeTransform` / `SivModeTransform` / `GcmSivModeTransform`; or `AsconAead128` | Ciphertext + auth tag | NIST SP 800-38D / RFC 5116 / RFC 7253 / NIST SP 800-232 | `AesGcm`, `AesCcm` (BCL — preferred for those modes) |
| **Per-record / per-sector encryption** with public domain separation | `Threefish256/512/1024` with `Tweak`; `XtsModeTransform` | Tweakable ciphertext | IEEE P1619 (XTS) / Threefish whitepaper | None |
| **Cryptographic digest** for content addressing | `Tiger`, `CubeHash`, `Whirlpool`, `Snefru`, `Blake2b`, `Blake3`, `AsconHash256`, `Skein256/512/1024` | 128 – 1024 bits | NESSIE / SHA-3 / RFC 7693 / NIST SP 800-232 | `SHA256`, `SHA384`, `SHA512`, `SHA3_256` (BCL — preferred where available) |
| **Variable-length / extendable output** (XOF) | `AsconXof128`, `AsconCxof128`, `Shake`, `Blake3` | Configurable | NIST SP 800-185 / FIPS 202 / NIST SP 800-232 | `Shake128`, `Shake256` (BCL — preferred where available) |
| **Keyed hash / MAC** (reusable PRF) | `SipHash64`, `SipHash128` | 64 / 128 bits | Aumasson & Bernstein SipHash paper | `HMACSHA256` (BCL) |
| **One-time message authenticator** (key + message — never reuse key) | `Poly1305` | 128 bits | RFC 8439 | None — paired with `ChaCha20` in BCL `ChaCha20Poly1305` |
| **Verifiable tree hashing** (Merkle root + inclusion proofs) | `MerkleTreeHash`, `ParallelMerkleTreeHash` | Configurable leaf hash | Merkle 1979 / Certificate Transparency | None |
| **Digital signature** (classical, sign / verify) | `Ed25519` | 64-byte deterministic signature | RFC 8032 | None on `net8.0` |
| **Digital signature** (post-quantum) | `MLDsa44`, `MLDsa65`, `MLDsa87` | 2420 – 4627-byte signature | FIPS 204 | None on `net8.0` |
| **Key agreement** (derive a shared secret from two public keys) | `X25519` | 32-byte shared secret | RFC 7748 | None on `net8.0` |
| **Key encapsulation** (post-quantum, seal a fresh secret to a public key) | `MLKem512`, `MLKem768`, `MLKem1024` | 32-byte secret + ciphertext | FIPS 203 | None on `net8.0` |
| **Seal a message to a recipient's public key** (hybrid PKE) | `Hpke` | Encapsulated key + ciphertext + tag | RFC 9180 | None on `net8.0` |
| **Password hashing / storage** (memory-hard) | `Argon2id`, `Argon2i`, `Argon2d`, `Scrypt` | Salted derived tag | RFC 9106 / RFC 7914 | None on `net8.0` |
| **Derive keys from a high-entropy secret** (extract-and-expand) | `Hkdf` | Configurable | RFC 5869 | `HKDF` (BCL — preferred where it covers your hash) |

Cryptographic digests in this table provide **integrity only when the digest itself is transmitted via an authenticated channel**. For integrity + authenticity in a single primitive, pick a MAC or an AEAD mode. See the [Core concepts](concepts.md) page for the full safety vocabulary.

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
*<xref:Bodu.Security.Cryptography.SymmetricStreamAlgorithm> lifecycle (a `SymmetricAlgorithm` with no block mode or padding): configure `Key` and `IV` (the nonce), then `CreateEncryptor()` / `Encrypt`. Self-inverse and raw — **confidentiality only, no authentication**. Never reuse a `(key, nonce)` pair; pair with a MAC or prefer AEAD.*

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
| <xref:Bodu.Security.Cryptography.BlockHashAlgorithm>, <xref:Bodu.Security.Cryptography.BufferedBlockHashAlgorithm>, <xref:Bodu.Security.Cryptography.DeferredFinalBlockHashAlgorithm>, <xref:Bodu.Security.Cryptography.KeyedBlockHashAlgorithm> | — | Abstract bases for block-oriented digests (extension points). |
| <xref:Bodu.Security.Cryptography.HashAlgorithmFactory>, <xref:Bodu.Security.Cryptography.IHashAlgorithmFactory`1>, <xref:Bodu.Security.Cryptography.DelegateHashAlgorithmFactory`1> | — | Factory abstraction over `HashAlgorithm` for keyed / Merkle constructions. |

### Keyed hashes / MACs
*`HashAlgorithm` with a required `Key` property.*

| Type | Output | Subtype |
|---|---|---|
| <xref:Bodu.Security.Cryptography.SipHash64> | 64 bits | PRF; default rounds SipHash-2-4. |
| <xref:Bodu.Security.Cryptography.SipHash128> | 128 bits | PRF; wider output for routing / sharding. |
| <xref:Bodu.Security.Cryptography.Poly1305> | 128 bits | One-time authenticator (RFC 8439). |

### Asymmetric primitives — signatures, key agreement, KEM, HPKE
*Public-key schemes over <xref:System.Security.Cryptography.AsymmetricAlgorithm?displayProperty=nameWithType>. Raw key encodings only — PKCS#8 / SPKI (DER / PEM) are deliberately out of scope. Lifecycle: `Create()`, `GenerateKey()`, export the public half, then sign / verify, agree, or encapsulate.*

| Type | Role | Standard | Notes |
|---|---|---|---|
| <xref:Bodu.Security.Cryptography.Ed25519> | Signature | RFC 8032 | Deterministic EdDSA over edwards25519; 32-byte keys, 64-byte signature, 128-bit security. `SignData` / `VerifyData`. |
| <xref:Bodu.Security.Cryptography.MLDsa44> / <xref:Bodu.Security.Cryptography.MLDsa65> / <xref:Bodu.Security.Cryptography.MLDsa87> | Signature (post-quantum) | FIPS 204 | Module-lattice ML-DSA; `MLDsa65` the default. Signatures 2420 – 4627 bytes; API mirrors `Ed25519`. |
| <xref:Bodu.Security.Cryptography.X25519> | Key agreement | RFC 7748 | ECDH over Curve25519; 32-byte keys and 32-byte shared secret. `DeriveSharedSecret(peerPublicKey)`. |
| <xref:Bodu.Security.Cryptography.MLKem512> / <xref:Bodu.Security.Cryptography.MLKem768> / <xref:Bodu.Security.Cryptography.MLKem1024> | KEM (post-quantum) | FIPS 203 | Module-lattice ML-KEM; `MLKem768` the default. `Encapsulate()` / `Decapsulate(ciphertext)` / `ExportEncapsulationKey()`. |
| <xref:Bodu.Security.Cryptography.Hpke>, <xref:Bodu.Security.Cryptography.HpkeSender>, <xref:Bodu.Security.Cryptography.HpkeReceiver>, <xref:Bodu.Security.Cryptography.HpkeSuite> | Hybrid PKE | RFC 9180 | `DHKEM(X25519, HKDF-SHA256)` + HKDF + AEAD. `Hpke.Seal` / `Hpke.Open`; suites `X25519_HkdfSha256_Aes128Gcm` / `_Aes256Gcm` / `_ChaCha20Poly1305`. |
| <xref:Bodu.Security.Cryptography.SignatureFormat>, <xref:Bodu.Security.Cryptography.SignatureValue> | — | — | Signature-encoding selector and value type shared by the signature schemes. |

See the [asymmetric overview](../../guides/cryptography/asymmetric-overview.md) and [HPKE](../../guides/cryptography/hpke.md) guides for worked walk-throughs.

### Key derivation & password hashing
*Turn one secret into key material. Memory-hard password hashes stretch low-entropy passwords; HKDF expands a high-entropy secret into context-bound keys.*

| Type | Standard | Notes |
|---|---|---|
| <xref:Bodu.Security.Cryptography.Argon2id> / <xref:Bodu.Security.Cryptography.Argon2i> / <xref:Bodu.Security.Cryptography.Argon2d> | RFC 9106 | Memory-hard password hash; `Argon2id` the recommended default. <xref:Bodu.Security.Cryptography.Argon2Parameters> carries `MemoryKiB`, `Iterations`, `Parallelism`, `TagLength`. One-shot `Argon2id.DeriveKey(password, salt, parameters)`. |
| <xref:Bodu.Security.Cryptography.Scrypt> | RFC 7914 | Memory-hard password hash; <xref:Bodu.Security.Cryptography.ScryptParameters> carries `CostN`, `BlockSizeR`, `Parallelization`. Peak memory ≈ `128 · N · r` bytes. |
| <xref:Bodu.Security.Cryptography.Hkdf> | RFC 5869 | HMAC extract-and-expand over SHA-1/256/384/512; `Extract` / `Expand` / `DeriveKey`. Backs the HPKE labeled KDF. **Not** a password hash — feed it high-entropy input only. |

See the [Argon2](../../guides/cryptography/argon2.md), [scrypt](../../guides/cryptography/scrypt.md), and [HKDF](../../guides/cryptography/hkdf.md) guides.

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
| <xref:Bodu.Security.Cryptography.HashAlgorithmHelper> | Helper utilities for `HashAlgorithm` consumers. |

Random key/IV/tweak generation, padding helpers, and secure-clear helpers ship as internal infrastructure; consumers reach them indirectly through the extension surfaces above.

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
| Sign a message and verify it with a distributed public key | `Ed25519` (classical), `MLDsa65` (post-quantum) |
| Establish a shared secret between two parties | `X25519` (classical), `MLKem768` (post-quantum) |
| Encrypt a payload so only a given public key can read it | `Hpke` |
| Store a password so offline guessing is expensive | `Argon2id`, `Scrypt` |
| Derive session / traffic keys from a shared secret | `Hkdf` |

## Where to go next

- **[Core concepts](concepts.md)** — glossary the rest of the documentation assumes.
- **[Getting started](getting-started.md)** — install + minimal sample for a cipher, an AEAD round-trip, a keyed hash, and a digest.
- **[Bodu.Security.Cryptography guides](../../guides/cryptography/index.md)** — recipe-style walk-throughs.
- **[Asymmetric cryptography overview](../../guides/cryptography/asymmetric-overview.md)** — signatures, key agreement, KEM, and the [HPKE](../../guides/cryptography/hpke.md) seal-to-public-key recipe.
- **[Bodu.Security.Cryptography API reference](xref:Bodu.Security.Cryptography)** — full type-by-type docs.
- **For non-cryptographic checksums and fingerprints** (CRC, Fletcher, Adler, FNV, CityHash, MurmurHash3), see [Bodu.IO.Hashing](../io-hashing/index.md).
- **[Hashing & Cryptography topic](../topics/hashing-and-cryptography.md)** — this package and its sibling Bodu.IO.Hashing side by side.
