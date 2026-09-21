---
title: Security guarantees and limitations
---

# Security guarantees and limitations

This page states, in one place, what `Bodu.Security.Cryptography` does and does not promise: its audit status, which primitives assert constant-time behaviour and which explicitly disclaim it, what gets zeroed and when, how the SIMD fast paths affect determinism, the exception contract, the single-use rules, and the thread-safety model. Every claim below is taken from the source's own documentation or observed by running the library; nothing here is inferred.

## Audit status

**The library is not independently audited.** The source repeats the same sentence on the KDFs, the asymmetric algorithms, the OTP generators, and HPKE: the implementation "offers best-effort side-channel resistance and has not been independently audited". It is developed against published test vectors (NIST, RFC, NESSIE, Wycheproof and ACVP files in the test project) and a contract-test suite, which establishes *correctness* against the specifications, not resistance to an adversary with physical or timing access. Treat the library as you would any unaudited implementation: prefer the BCL where it implements the same algorithm ([Choosing a primitive](choosing-a-primitive.md)), and do not deploy the Bodu-only constructions (`XSalsa20Poly1305Aead`, `Serpent256/512/1024`) where an external review is expected.

## Constant-time claims by primitive

The source distinguishes three postures. Only the first is a positive claim.

| Posture | Primitives | What the source says |
|---|---|---|
| **Constant-time with respect to secrets** | `X25519` / Curve25519 field arithmetic, `Ed25519` (signing, precomputed-table selection, ladder), `MLKem` (decapsulation compare and implicit-rejection key selection), GHASH / POLYVAL (`GcmModeTransform`, `GcmSivModeTransform` — both the PCLMULQDQ and scalar paths), every AEAD tag comparison, `Hotp` / `Totp` code comparison, the value types' equality, `Pkcs7Padding` / `Ansix923Padding` / `Iso7816_4Padding` unpad validation, the ARX designs (BLAKE2, BLAKE3, Threefish) by construction | "runs in constant time with respect to the private key"; "the tag is compared in constant time before any plaintext byte is written"; "no secret-dependent branches or table lookups". |
| **Constant-time control flow, data-dependent table reads — explicitly *not* hardened** | `Blowfish`, `Skipjack`, `Camellia`, `Twofish`, `Serpent128`, `Serpent256/512/1024`, `Tiger`, `Whirlpool`, `Snefru` | "control flow is constant-time, but the S-box lookup tables are read at data-dependent indices … **not** hardened against timing or cache-based side-channel attacks." Serpent's per-substitution table is a single 16-byte line, which the source says *limits but does not formally eliminate* the exposure. |
| **Deliberately not constant-time** | `Iso10126Padding` unpad (random-filled interior; validation is "deliberately not masked"), `MLDsa` rejection sampling (public values only; "the values involved carry no secrets") | Documented as safe because no secret drives the timing. |
| **Best-effort, unqualified** | `Hkdf`, `Argon2*`, `Scrypt`, `Hpke`, `Hotp`, `Totp`, `MLDsa` | "best-effort, not guaranteed, side-channel resistance". |

`Argon2i` and `Argon2id` use data-independent memory addressing for their first pass; `Argon2d` does not, by design. `SipHash`, `Poly1305`, `Skein`, `CubeHash`, `Shake`, the Ascon family, and the stream ciphers make no explicit statement either way in their documentation; their designs are ARX or permutation-based without secret-indexed tables, but the library does not assert a guarantee for them.

`AesBlockCipher` delegates each block to the BCL `Aes`, so its timing posture is the platform's (hardware AES-NI where available).

## Zeroization on dispose

What is cleared, per the source and observed behaviour:

| Type | On `Dispose` |
|---|---|
| Block-cipher wrappers (`Blowfish`, `Camellia`, `Twofish`, `Serpent128`, `Skipjack`, `Threefish*`, `Serpent256/512/1024`) | `Key` and `IV` backing arrays are zeroed; reading `Key` afterwards throws `ObjectDisposedException`. A copy you took earlier via `alg.Key` is *yours* and is not cleared. |
| `BlockCipherTransform` | clears any held-back decryption block, disposes the mode transform (which zeroes its chaining vector / counter / tweak / feedback register) **and the engine**. |
| `SymmetricStreamAlgorithm` and the Poly1305 AEADs | retained key and nonce zeroed. |
| Hashes (`BufferedBlockHashAlgorithm` family) | residual block and `HashValue` cleared; keyed hashes (`KeyedBlockHashAlgorithm`, `KeyedDeferredFinalBlockHashAlgorithm`) also clear the key. Reading members afterwards throws `ObjectDisposedException`. |
| Asymmetric (`RawKeyAsymmetricAlgorithm`: `X25519`, `Ed25519`, `MLKem*`, `MLDsa*`) | private key material zeroed exactly once; replacing a key (`GenerateKey`, `Import*`) zeroes the previous private key first. |
| `SecretBytes` | pinned buffer zeroed and the instance latched; `Clear()` zeroes without disposing. |
| Pooled buffers (`AppendData`, `TransformAsync`, `HashAlgorithmHelper`, `ParallelMerkleTreeHash`) | returned to `ArrayPool<byte>.Shared` with `clearArray: true` on every exit path. |
| `Nonce`, `Salt`, `HashValue`, `AuthenticationTag`, `SignatureValue` | nothing — they are public values by design. |

What is **not** covered: arrays you allocate and hand to the library (`byte[] key = …`) are copied, not adopted; clear your own copies with `CryptographicOperations.ZeroMemory`. Managed memory can still be copied by the GC before `Dispose` runs; only `SecretBytes` pins.

## SIMD determinism and the `DisableSimd` switch

BLAKE2b, BLAKE2s, BLAKE3, Threefish-256/512/1024, and CubeHash carry AVX-512 kernels; GHASH in GCM / GCM-SIV has a PCLMULQDQ path. All of them produce **bit-identical output** to their scalar reference — the switch exists for reproducibility and audit, not safety. The feature switch `Bodu.Security.Cryptography.DisableSimd` forces the scalar path for the whole process:

```xml
<ItemGroup>
  <RuntimeHostConfigurationOption Include="Bodu.Security.Cryptography.DisableSimd" Value="true" Trim="false" />
</ItemGroup>
```

or, in a `runtimeconfig.template.json`, `{ "configProperties": { "Bodu.Security.Cryptography.DisableSimd": true } }`, or `AppContext.SetSwitch("Bodu.Security.Cryptography.DisableSimd", true)` before the first accelerated primitive runs. The switch is read **once** at type initialization and cannot be toggled afterwards. The repository proves the scalar paths independently: `Bodu.Security.Cryptography.Simd.Test` is a separate test assembly whose `runtimeconfig.template.json` sets the switch, so the accelerated primitives are exercised through their scalar code and must still reproduce the published digests. See [Hardware acceleration and the SIMD opt-out](hardware-acceleration.md).

```csharp
using Bodu.Security.Cryptography;

AppContext.TryGetSwitch("Bodu.Security.Cryptography.DisableSimd", out bool disabled);
using var blake = new Blake2b();
byte[] digest = blake.ComputeHash("abc"u8.ToArray());     // BA80A53F981C4D0D… whether or not the switch is set
```

## The exception contract

The library uses four exception families with consistent meaning. The table lists the boundary, the type you will see, and a representative trigger — each row was produced by running the library.

| Situation | Exception | Examples |
|---|---|---|
| Argument is structurally wrong for a **raw engine, transform, or value type** | `ArgumentException` (`ParamName` set) / `ArgumentOutOfRangeException` / `ArgumentNullException` | `new CamelliaBlockCipher(10-byte key)`, `new Serpent256Cipher(key, 8-byte tweak)`, `new GcmModeTransform(cipher, 16-byte nonce)`, `new XChaCha20Poly1305(key, 16-byte nonce)`, a ciphertext shorter than the tag, a detached tag of the wrong length, secretbox given AAD, partial buffer overlap, `BlockCipherModeFactory` without an IV, `Blake2b(100)`, `Nonce.Random(0)`, `Hkdf` with MD5, Argon2 memory below `8 × parallelism`, scrypt `N` not a power of two, HKDF output over `255 × HashLen`, OTP `digits` outside 6–8, `SignatureValue.FromBytes` with an undefined format, `Ed25519.ImportPublicKey(31 bytes)`, ML-KEM ciphertext of the wrong length |
| Key material or configuration is invalid on a **`SymmetricAlgorithm` / stream-algorithm wrapper**, or a cryptographic check fails | `CryptographicException` | `camellia.Key = 10 bytes`, `CreateEncryptor` with a mis-sized or missing IV, `chacha.Key = 8 bytes`, `Blake2b.Key` over 64 bytes, `AesBlockCipher(10-byte key)` (from the BCL `Aes`), an unknown `PaddingMode`, invalid PKCS7 padding on decrypt, an XTS input that is not whole blocks, CFB/OFB input not block-aligned, **any AEAD tag mismatch**, `Ed25519.SignData` without a private key, `X25519` given a low-order peer point, a second stream-cipher transform for the same nonce, a second Poly1305 message under one key |
| A `HashAlgorithm` is reconfigured after hashing has started | `CryptographicUnexpectedOperationException` | setting `Tiger.Variant` after `TransformBlock` |
| The object's **lifecycle** forbids the call | `InvalidOperationException` | a second `Encrypt` / `Decrypt` on any AEAD transform (including after a failed decrypt), `TransformFinalBlock` twice on a `BlockCipherTransform`, `ParallelMerkleTreeHash` over an empty stream |
| The feature is not implemented for this type | `NotSupportedException` | `BlockMode = CipherModeKind.CTS` / `XTS` / `OCB` / `EAX` / `SIV` at `CreateEncryptor`, the BCL one-shots (`EncryptCbc` …), `ToXmlString`, encrypted PKCS#8, PKCS#8 / SPKI on ML-KEM / ML-DSA, `Poly1305` with padding |
| Use after `Dispose` | `ObjectDisposedException` | any member of a disposed hash, cipher, transform, asymmetric algorithm, or `SecretBytes` |
| Text that is not hex | `FormatException` | `HashValue.ParseHex("zz")`; PHC-string parsing in `Argon2.Verify` / `Scrypt.Verify` |

Two rules follow. First, **a failed authentication is `CryptographicException`** everywhere — AEAD tags, Poly1305, signature verification does *not* throw but returns `false` (`Ed25519.VerifyData`, `MLDsa.VerifyData`, `Hotp.VerifyCode` return `false` for a wrong or wrongly sized input). Second, the *same* mistake surfaces differently by layer: a bad key length is `ArgumentException` on an engine and `CryptographicException` on its wrapper, because the wrapper follows the BCL `SymmetricAlgorithm` convention. Catch `CryptographicException` on the wrapper path and `ArgumentException` when you compose engines yourself.

## Single-use rules

| Object | Rule | Violation |
|---|---|---|
| Every AEAD transform (`GcmModeTransform`, `GcmSivModeTransform`, `CcmModeTransform`, `OcbModeTransform`, `EaxModeTransform`, `SivModeTransform`, `AsconAead128`, `XChaCha20Poly1305`, `XSalsa20Poly1305`, `XSalsa20Poly1305Aead`) | one `Encrypt` **or** one `Decrypt` per instance; a failed decrypt also consumes it | `InvalidOperationException` |
| `Poly1305` | one message per key; `Initialize()` does not reset the one-time latch | `CryptographicException` |
| `SymmetricStreamAlgorithm` (`ChaCha20` …) | one transform per nonce through the parameterless `CreateEncryptor()` / `CreateDecryptor()`; use `GenerateNonce()` or the `(key, nonce)` overloads for another | `CryptographicException` |
| `BlockCipherTransform` and the stream-cipher transforms | `CanReuseTransform == false`; nothing after `TransformFinalBlock` | `InvalidOperationException` |
| `HashAlgorithm` subclasses | reusable: `ComputeHash` resets; `Initialize()` resets a streaming computation | — |
| Nonces and IVs | never reuse a `(key, nonce)` pair for GCM, CTR, CCM, OCB, EAX, or any stream cipher; SIV and GCM-SIV degrade gracefully; XChaCha20 / XSalsa20 nonces are safe to draw at random | not detected — a protocol responsibility |

## Thread safety

No instance in the library is thread-safe. The source states it for the mode transforms, the AEAD transforms, `MerkleTreeHash`, `ParallelMerkleTreeHash` ("single-caller only"), and `SecretBytes`; the `HashAlgorithm` and `SymmetricAlgorithm` bases inherit the BCL's per-instance model. Static entry points (`Hkdf`, `Hotp`, `Totp`, `Hpke`, `Argon2id.DeriveKey`, `Scrypt.DeriveKey`, `HashAlgorithmHelper`, `BlockCipherModeFactory`, `PaddingFactory`) hold no shared mutable state and may be called concurrently. `ParallelMerkleTreeHash` parallelizes *inside* one call through its own workers; two callers must use two instances. Share a factory (`IHashAlgorithmFactory<T>`), not an algorithm.

## Trimming and AOT

The library's public API is reflection-free (`Enum.TryParse` is used only for mode/padding name mapping on the wrappers), and the feature switch is declared with `Trim="false"` in the recommended project snippet so it survives trimming. No member is annotated with `RequiresUnreferencedCode` or `RequiresDynamicCode`. This is an observation about the source, not a tested guarantee; the repository does not ship a trimmed or AOT-published test.

## Where to go next

- [Choosing a primitive](choosing-a-primitive.md) — when to prefer the BCL.
- [Hardware acceleration and the SIMD opt-out](hardware-acceleration.md) — the accelerated kernels and the switch in depth.
- [Nonces, salts, tags, and secrets](value-types.md) — the constant-time value types and `SecretBytes`.
- [Authenticated stream ciphers](stream-aead.md) and [AEAD modes](aead-modes.md) — the single-use contract in practice.
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
