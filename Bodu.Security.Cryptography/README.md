# Bodu.Security.Cryptography

> **API stability — Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

Managed implementations of modern and legacy cryptographic primitives for .NET 8 and later. The library provides block ciphers, AEAD modes, hash and MAC functions, padding schemes, asymmetric key agreement and signatures (including the FIPS 203/204 post-quantum algorithms), and the supporting transform infrastructure to compose them. All algorithms are exposed through the standard `SymmetricAlgorithm` / `HashAlgorithm` / `KeyedHashAlgorithm` / `AsymmetricAlgorithm` contracts so they slot into existing BCL pipelines (including `CryptoStream`).

It also provides an RFC 6962 [Merkle tree](#merkle-trees) with inclusion and consistency proofs, built over any `HashAlgorithm` and able to hash a large input's leaves on every core.

## Security posture and limitations

Read this before using the library for anything that matters.

- **Not independently audited.** These are managed, from-scratch implementations. They are pinned to published known-answer vectors for *functional* correctness, but they have **not** undergone an independent security audit. Treat them as suitable for development, interop with non-FIPS systems, research, and education — not as a drop-in for a hardened production provider.
- **Not FIPS-validated.** This is not a FIPS 140-2 / 140-3 cryptographic module. For FIPS-validated AES, SHA-2, RNG, and similar primitives use the platform-provided `System.Security.Cryptography` types and the underlying OS provider.
- **Side-channel resistance is best-effort, not guaranteed.** Constant-time behaviour is implemented where practical: tag and hash comparisons use `CryptographicOperations.FixedTimeEquals` and padding removal is branchless. GHASH and POLYVAL (behind GCM, GMAC and GCM-SIV) neither branch on nor index memory by the key or the data on any path, the portable fallback included. The software table-based algorithms — Blowfish, Twofish, Camellia, Tiger, Whirlpool and Snefru — still index tables by secret-dependent values. Managed code runs on a JIT and GC the library does not control, so timing/cache invariance cannot be guaranteed end-to-end regardless. For workloads with a real side-channel adversary, prefer the hardware-backed BCL primitives.
- **AES delegates to the BCL.** `AesBlockCipher` wraps the platform `System.Security.Cryptography.Aes` (hardware-accelerated, constant-time, FIPS-validated). Everything else in the package is a bespoke managed implementation.
- **This is a toolbox, not a safe-by-default API.** ECB, `NoPadding`, raw CBC, and unauthenticated stream ciphers are all first-class. Prefer an AEAD mode (GCM, EAX, OCB, or a nonce-misuse-resistant SIV / GCM-SIV) unless you have a specific reason not to, and read the per-type remarks for the failure modes.

The primitives provided here exist mainly to cover what the BCL does not ship (Blake2/Blake3, Ascon, Skein, Poly1305, SipHash, Threefish, Serpent, Camellia, Blowfish, Skipjack, OCB / EAX / SIV / GCM-SIV modes, X25519 / Ed25519, the post-quantum ML-KEM / ML-DSA on .NET 8, RFC 6962 Merkle trees, etc.).

## Algorithm support matrix

### Block ciphers

| Algorithm | Standard | Key sizes (bits) | Block size (bits) | Status | KAT source |
|---|---|---:|---:|---|---|
| AES (wrapper) | NIST FIPS 197 | 128, 192, 256 | 128 | Recommended | NIST FIPS 197 |
| Camellia | RFC 3713 | 128, 192, 256 | 128 | Recommended | RFC 3713 |
| Serpent | AES candidate (Anderson, Biham, Knudsen) | 128, 192, 256 | 128 | Recommended | AES-candidate official vectors |
| Twofish | AES finalist (Schneier et al., 1998) | 128, 192, 256 | 128 | Recommended | Twofish reference vectors (`ecb_tbl`) |
| Threefish 256 / 512 / 1024 | Skein reference (NIST SHA-3 entry) | 256 / 512 / 1024 | 256 / 512 / 1024 | Recommended for keyed-tweak use | Skein 1.3 reference |
| Blowfish | Schneier (1993) | 32–448 | 64 | Legacy only — SWEET32 above ~32 GiB | Schneier reference vectors |
| Skipjack | NIST FIPS PUB 185 (1994) | 80 | 64 | Legacy / educational only | FIPS PUB 185 |

### AEAD modes

| Mode | Standard | Tag (bits) | Nonce semantics | Notes |
|---|---|---:|---|---|
| GCM | NIST SP 800-38D | 128 | 96-bit, must be unique per `(key, nonce)` | Fast, parallelisable; nonce reuse leaks GHASH subkey |
| CCM | NIST SP 800-38C | 128 | 96-bit, must be unique | Two-pass; common in constrained-environment standards |
| EAX | Bellare, Rogaway, Wagner (FSE 2004) | 128 | OMAC-derived; nonce must still be unique | Two-pass; flexible nonce length |
| OCB | RFC 7253 | 128 | Must be unique; graceful failure on reuse | Single-pass; previously patent-encumbered |
| SIV | RFC 5297 | 128 | Deterministic — supplied IV ignored | Nonce-misuse resistant (leaks only equality) |
| GCM-SIV | RFC 8452 | 128 | 96-bit; per-message key derivation | Nonce-misuse resistant; faster than SIV |
| Ascon-AEAD128 | NIST SP 800-232 | 128 | 128-bit, must be unique | Lightweight; intended for constrained devices |

### Stream ciphers

| Algorithm | Standard | Key sizes (bits) | Nonce (bits) | Status | Notes |
|---|---|---:|---:|---|---|
| ChaCha20 | RFC 8439 | 256 | 96 | Recommended | 32-bit block counter; pair with Poly1305 for integrity |
| XChaCha20 | draft-irtf-cfrg-xchacha | 256 | 192 | Recommended | Extended nonce; safe for random nonces |
| Salsa20 | Bernstein (eSTREAM) | 128 / 256 | 64 | Recommended | Predecessor to ChaCha |
| XSalsa20 | Bernstein | 256 | 192 | Recommended | Extended-nonce Salsa20 |
| Rabbit | RFC 4503 (eSTREAM) | 128 | 64 | Legacy / compat | |
| HC-128 | eSTREAM portfolio | 128 | 128 | Legacy / compat | |

The AEAD stream constructions pair a stream cipher with Poly1305: `ChaCha20-Poly1305` (RFC 8439), `XChaCha20-Poly1305`, and `XSalsa20-Poly1305` (NaCl `secretbox`). Each is single-use per message and verifies the tag with `CryptographicOperations.FixedTimeEquals`.

### Asymmetric algorithms

All asymmetric types derive from `System.Security.Cryptography.AsymmetricAlgorithm` and support only the raw byte
encodings of their defining specification (PKCS#8 / SubjectPublicKeyInfo DER import/export is not implemented).
Private key material is zeroed on dispose and exports return defensive copies.

| Algorithm | Kind | Standard | Key / output sizes (bytes) | Notes | KAT source |
|---|---|---|---|---|---|
| X25519 | key agreement | RFC 7748 | keys 32, shared secret 32 | Strict §6.1 all-zero rejection of low-order peer points; constant-time ladder | RFC 7748 + Wycheproof |
| Ed25519 | signature | RFC 8032 (pure) | keys 32, signature 64 | Deterministic; rejects S ≥ L and non-canonical points; Ed25519ph/ctx not implemented | RFC 8032 + Wycheproof |
| ML-KEM 512 / 768 / 1024 | post-quantum KEM | NIST FIPS 203 | ek 800/1184/1568, dk 1632/2400/3168, ct 768/1088/1568, secret 32 | Implicit rejection (tampered ciphertexts never throw); §7.2/§7.3 import checks | NIST ACVP |
| ML-DSA 44 / 65 / 87 | post-quantum signature | NIST FIPS 204 | pk 1312/1952/2592, sk 2560/4032/4896, sig 2420/3309/4627 | Hedged by default with `DeterministicSigning` opt-in; context strings up to 255 bytes; HashML-DSA not implemented | NIST ACVP |

### Hybrid public-key encryption (HPKE)

`Hpke` implements RFC 9180 in all four modes — Base, PSK, Auth and AuthPSK — with the `DHKEM(X25519, HKDF-SHA256)` KEM, HKDF-SHA256, HKDF-SHA384 or HKDF-SHA512, and AES-128-GCM, AES-256-GCM, ChaCha20-Poly1305 or export-only as the AEAD. `Hpke.Seal` and `Hpke.Open` handle a single message; `HpkeSender` and `HpkeReceiver` hold a context for a sequence of messages and derive exporter secrets with `Export`. A context checks its message counter before doing any cryptographic work, so it refuses the message that would reuse a nonce rather than sealing it. The suite is pinned to the RFC 9180 test vectors; see the [HPKE guide](https://bslater.github.io/bodu/guides/cryptography/hpke.html).

### Block cipher modes (unauthenticated)

| Mode | Notes |
|---|---|
| CBC | Standard CBC; pair with a MAC or use an AEAD for integrity |
| CFB | Self-synchronising; full-block segment size |
| CTR | Counter-mode keystream; same nonce-uniqueness rules as GCM apply |
| OFB | Synchronous stream from feedback register |
| CTS | Ciphertext stealing variant for non-aligned final blocks |
| XTS | Tweakable mode for length-preserving sector/storage encryption |
| ECB | Compatibility / primitive use only — leaks block-level patterns |

### Padding schemes

PKCS#7, ANSI X9.23, ISO 7816-4, ISO 10126, zero-padding, and `None`. All are exercised by the block-cipher transform tests with positive and negative cases.

### Hash and MAC

| Algorithm | Type | Standard | Output (bits) | Status | Notes |
|---|---|---|---:|---|---|
| BLAKE2b / BLAKE2s | hash + optional MAC | RFC 7693 | 8–512 / 8–256 | Recommended | Keyed mode is a one-step HMAC alternative |
| BLAKE3 | hash + XOF | BLAKE3 reference | 256 (extendable) | Recommended | Tree hashing; optionally [multithreaded](#multithreading) for large inputs |
| Skein 256 / 512 / 1024 | hash with UBI tweak | Skein 1.3 | up to state size | Recommended for tweakable use | SHA-3 candidate |
| Ascon-Hash256 / Ascon-HashA256 | hash | NIST SP 800-232 | 256 | Recommended for lightweight | Conservative (Ascon-p12) vs. fast (Ascon-p8) variants |
| Ascon-XOF128 / Ascon-CXOF128 | XOF / customisable XOF | NIST SP 800-232 | extendable | Recommended for lightweight | Sponge-mode streaming output |
| SHAKE128 / SHAKE256 | XOF | NIST FIPS 202 | extendable | Recommended | |
| Poly1305 | one-time MAC | RFC 8439 | 128 | Recommended (one-time key only) | Reuse with same key is rejected at runtime |
| SipHash-64 / SipHash-128 | keyed hash | Aumasson & Bernstein | 64 / 128 | Recommended for hash-table integrity | Multi-message key reuse permitted |
| CubeHash | hash | Bernstein (SHA-3 candidate) | configurable | Legacy / educational | Tunable rounds / block / output |
| Tiger / Tiger2 | hash | Anderson & Biham | 128 / 160 / 192 | Legacy / compat | |
| Whirlpool | hash | ISO/IEC 10118-3 | 512 | Legacy / compat | Software table-based |
| Snefru | hash | Merkle (1990) | 128 / 256 | Legacy / educational | Software table-based |

Non-cryptographic hashes and checksums — FNV-1a, Adler-32, CRC-3 through CRC-64, and Fletcher-16/32/64 — live in the sibling `Bodu.IO.Hashing` package.

### Key derivation and password hashing

| Algorithm | Type | Standard | Status | Notes |
|---|---|---|---|---|
| Argon2id / Argon2i / Argon2d | password hash + KDF | RFC 9106 | Recommended (Argon2id) | Memory-hard; versions 0x13 and 0x10; optional secret and associated data; `Hash` / `Verify` over PHC strings |
| scrypt | password hash + KDF | RFC 7914 | Recommended | Memory-hard, `128 · N · r` bytes per unit; `Hash` / `Verify` over PHC strings |
| HKDF | extract-and-expand KDF | RFC 5869 | Recommended | SHA-1 and SHA-2; mirrors the BCL's `HKDF`, and the platform type is preferable where it covers your need |

Argon2 and scrypt can divide a derivation across threads (see [Multithreading](#multithreading)), and both keep their working memory in pooled native memory: set the `Bodu.Security.Cryptography.Argon2.DisableMatrixReuse` switch to release it after every call instead. See the [Argon2](https://bslater.github.io/bodu/guides/cryptography/argon2.html), [scrypt](https://bslater.github.io/bodu/guides/cryptography/scrypt.html) and [HKDF](https://bslater.github.io/bodu/guides/cryptography/hkdf.html) guides.

### Merkle trees

`MerkleTree` computes the RFC 6962 (Certificate Transparency) Merkle Tree Hash over an ordered list of entries, or over the fixed-size blocks of a stream or buffer, and produces and verifies the proofs that go with it. It works over any `HashAlgorithm` — `new MerkleTree(SHA256.Create)` — and assumes no particular digest length.

| Capability | Members | Notes |
|---|---|---|
| Root over entries | `ComputeRoot`, `ComputeRootOfLeafHashes`, `HashLeaf`, `HashNode` | Leaves and nodes are domain-separated (`0x00` / `0x01` prefixes); an empty tree's root is `H()` |
| Root over blocks | `ComputeRootOfBlocks`, `ComputeRootOfBlocksAsync` | Folds as it reads, holding O(log n) hashes; takes a `Stream`, `ReadOnlyMemory<byte>`, `ReadOnlySpan<byte>` or `byte[]` |
| Root and leaf hashes | `ComputeBlocked`, `ComputeBlockedAsync` → `MerkleBlockComputation` | Keeps every leaf hash, for proofs later |
| Root while writing | `CreateBlockAccumulator` → `MerkleBlockAccumulator` | Builds the root from `Append` calls as the bytes are written; the same root as `ComputeRootOfBlocks` |
| Inclusion proofs | `AuthenticationPath`, `VerifyInclusion`, `VerifyInclusionOfLeafHash` | RFC 6962 §2.1.1 audit paths |
| Consistency proofs | `ConsistencyProof`, `ConsistencyProofOfLeafHashes`, `VerifyConsistency` | RFC 6962 §2.1.2: a later tree extends an earlier one |
| Length-bound roots | `BindRoot`, `VerifyInclusionBound`, `VerifyBlockInclusion` | An addition to RFC 6962 that binds the tree size into the root, so a prover cannot understate it |
| Tracing | `MerkleTreeDiagnostics` | Optional recorder of every node a computation produces |

- **Parallel leaf hashing.** Hashing the leaves is nearly all of a block computation's work. `new MerkleTree(SHA256.Create, maxDegreeOfParallelism: -1)` hashes them in batches on every core, with one `HashAlgorithm` per worker, and folds them in order on the calling thread. A parallel instance produces the same roots and proofs as a sequential one; the default, `1`, keeps everything on the calling thread.
- **Fan-out.** The default `fanOut` of 2 is RFC 6962's binary tree. A wider fan-out builds a shallower k-ary tree as an explicit non-RFC mode: its roots work, and the proof members throw `NotSupportedException`.
- **Sharing and verification.** An instance is immutable and safe to share across threads, provided the factory returns a new `HashAlgorithm` on each call, as `SHA256.Create` does. Verification returns `false` for a malformed proof rather than throwing. `VerifyInclusion` trusts the tree size it is given; when that size comes from the party being checked, publish a length-bound root and verify with `VerifyInclusionBound` or `VerifyBlockInclusion`, which fail closed.

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

var tree = new MerkleTree(SHA256.Create, maxDegreeOfParallelism: -1);

// The root and every leaf hash of a file in 1 MiB blocks, the leaves hashed on every core.
using var file = File.OpenRead("archive.bin");
MerkleBlockComputation blocks = tree.ComputeBlocked(file, blockSize: 1 << 20);

// Prove that block 3 is in the file, then check the proof.
byte[][] path = tree.AuthenticationPath(blocks.LeafHashes, leafIndex: 3);
bool included = tree.VerifyInclusionOfLeafHash(
    blocks.Root, treeSize: blocks.LeafHashes.Count, leafIndex: 3,
    leafHash: blocks.LeafHashes[3],
    path: path.Select(step => (ReadOnlyMemory<byte>)step).ToArray());
```

See the [Merkle tree guide](https://bslater.github.io/bodu/guides/cryptography/merkle-trees.html); the [Merkle tree sample](https://github.com/bslater/bodu/tree/master/samples/Security.Cryptography/Bodu.Security.Cryptography.Samples.MerkleTrees) runs each of these as a scenario.

### One-time passwords

| Algorithm | Type | Standard | Notes |
|---|---|---|---|
| `Hotp` | counter-based OTP | RFC 4226 | `GenerateCode` / `VerifyCode`; look-ahead resynchronization |
| `Totp` | time-based OTP | RFC 6238 | Time-derived counter over `Hotp`; clock-drift verification window |

Static, span-based, and built on the BCL one-shot HMAC (`OtpHashAlgorithm` selects SHA-1/256/512). Verification is constant-time. Secrets are raw bytes — decode a Base32 `otpauth://` secret with `Bodu.Text.Encoding.Base32` first. See the [HOTP/TOTP guide](https://bslater.github.io/bodu/guides/cryptography/one-time-passwords.html).

## Lifecycle and disposal guarantees

- **AEAD transforms** (`IAeadBlockCipherModeTransform`) are single-use per message. Each implementation tracks `_completed`, `_aadProcessed`, `_disposed`, rejects double-Encrypt/Decrypt with `InvalidOperationException`, rejects post-disposal access with `ObjectDisposedException`, compares tags with `CryptographicOperations.FixedTimeEquals`, and clears the plaintext destination on tag-verification failure.
- **`BlockCipherTransform`** owns and disposes the underlying `IBlockCipher`. Single-use; rejects `TransformBlock` after `TransformFinalBlock`. See the class XML remarks for the full ownership contract.
- **`Poly1305`** is a one-time MAC; the same instance throws `CryptographicException` on a second `ComputeHash` unless `Key` is explicitly reassigned. Disposing clears `_acc`, `_r`, `_s`, and `_key`.
- **`Blake3`** clears each `uint[]` on the chunk-CV stack on Dispose so per-subtree chaining values do not survive in heap memory.
- **GCM** rejects message lengths that would force its 32-bit `inc32` counter to wrap past `0xFFFFFFFF` while another block remains to be processed.

## Hardware acceleration

These primitives dispatch to vector, carry-less-multiply or wide-multiply instructions where the processor has them, and to a portable implementation where it does not. Every path produces bit-identical output: the test suite holds each kernel to its portable counterpart and to the published vectors, and CI runs it on both x64 and ARM64.

| Primitive | x64 | ARM64 |
|---|---|---|
| AES, in every mode | the platform `Aes` (AES-NI where the OS provider uses it) | the platform `Aes` (the AES instructions where the OS provider uses them) |
| GHASH and POLYVAL (GCM, GMAC, GCM-SIV) | PCLMULQDQ | PMULL |
| ChaCha20, XChaCha20, Salsa20, XSalsa20 | AVX-512 (16 blocks at a time), AVX2 (8) or SSSE3 (4) | AdvSimd (4) |
| Poly1305 (and the Poly1305 AEADs) | AVX-512 (8 blocks at a time) or AVX2 (4) from 512 bytes; BMI2 `mulx` for 64-bit products below that | `umulh` for 64-bit products |
| X25519, Ed25519 | BMI2 `mulx` for 64-bit products | `umulh` for 64-bit products |
| BLAKE2b | AVX-512 or AVX2, else SSSE3 | AdvSimd |
| BLAKE2s | AVX-512 or SSSE3 | AdvSimd |
| BLAKE3 | AVX-512 (16 chunks at a time), AVX2 (8) or SSSE3 (4) | AdvSimd (4) |
| CubeHash | AVX-512, AVX2 or SSSE3 | AdvSimd |
| Serpent-128, over several blocks, and the counter blocks of CTR, EAX and SIV | AVX-512 or AVX2 (8 blocks at a time), else SSSE3 (4) | AdvSimd (4) |
| Threefish-256 / 512 / 1024 | AVX-512 | — |
| Argon2 | AVX2, else SSSE3 | AdvSimd |
| scrypt | SSE2 | AdvSimd |

The 16-wide ChaCha20, Salsa20 and BLAKE3 kernels, and the 8-wide Poly1305 kernel, run where .NET accelerates 512-bit vectors (`Vector512.IsHardwareAccelerated`); other AVX-512 processors run the next narrower kernels.

Set the process-wide feature switch **`Bodu.Security.Cryptography.DisableSimd`** to `true` to force the portable path in place of every vector and carry-less-multiply kernel above (AES and the 64-bit multiplies are unaffected) — useful for reproducibility, differential testing, or audit. It is read once, before first use of any accelerated primitive, so set it via `runtimeconfig.json` / a `<RuntimeHostConfigurationOption>` item or an early `AppContext.SetSwitch(...)`. The paths are equivalent (the ARX designs such as BLAKE2/3, ChaCha20 and Threefish are constant-time in both forms); the switch is not a security control. See the [hardware-acceleration guide](https://bslater.github.io/bodu/guides/cryptography/hardware-acceleration.html) for details.

## Multithreading

Four types can spread a single operation across threads. Each takes a `maxDegreeOfParallelism` bound when constructed and exposes it as `MaxDegreeOfParallelism`: `1` keeps the work on the calling thread, `-1` allows up to one thread per processor, and a larger value caps the threads. The output never depends on the bound.

| Type | Default | What runs on threads |
|---|---|---|
| `MerkleTree` | `1` | Leaf hashing, in batches with one `HashAlgorithm` per worker; the tree is folded on the calling thread |
| `Blake3` | `1` | The whole chunks of a write of 256 KiB or more, as independent 64 KiB subtrees joined on the calling thread |
| `Argon2d` / `Argon2i` / `Argon2id` | `-1` | A derivation's lanes, once they reach about 1 MiB each |
| `Scrypt` | `1` | A derivation's `p` units, each with its own `V`; small units stay on the calling thread, and large ones use fewer threads so their `V`s stay within 2 GiB |

`Argon2.Verify` and `Scrypt.Verify` take the same bound. The defaults are `1` where a busy service would only pay for the hand-offs (or, for scrypt, the extra memory); raise the bound to process one large input faster. Argon2 defaults to `-1` because its lanes share one memory matrix, so threads do not multiply its memory the way scrypt's units do.

## Reusable infrastructure

| Helper | Purpose |
|---|---|
| `BlockCipherModeFactory` | Build a configured `BlockCipherTransform` from an `IBlockCipher`, `IBlockCipherModeTransform`, and `IPaddingStrategy` |
| `BlockCipherTransform` | `ICryptoTransform` that drives a cipher through a mode + padding; owns and disposes the underlying `IBlockCipher` |
| `IAeadBlockCipherModeTransform` | Common surface for every AEAD mode (`ProcessAssociatedData` → `Encrypt`/`Decrypt`) |
| `IBlockCipher` / `IPaddingStrategy` | Extension points for supplying a custom primitive or padding scheme |
| `IStreamCipher` / `IStreamAeadTransform` | Common surfaces for the stream-cipher and stream-AEAD families |

## Testing

Tests live in `test/` and are organised as MSTest partial classes mirroring `src/`. Run tiers via the runsettings files at the solution root:

```bash
dotnet test Bodu.Security.Cryptography/test/Bodu.Security.Cryptography.Test.csproj --settings smoke.runsettings
dotnet test Bodu.Security.Cryptography/test/Bodu.Security.Cryptography.Test.csproj --settings bvt.runsettings
dotnet test Bodu.Security.Cryptography/test/Bodu.Security.Cryptography.Test.csproj --settings regression.runsettings
```

The shared `AeadBlockCipherModeTests<TTest, TTransform>` base contains the lifecycle / reuse / failed-tag-poisoning suite that every AEAD mode inherits. The `HashAlgorithmTests<TTest, TAlgorithm, TVariant>` base (and its `BlockHashAlgorithmTests` / `KeyedBlockHashAlgorithmTests` specialisations) provides spec-driven KAT, boundary, and disposal coverage for every hash and MAC. Block ciphers extend `BlockCipherTests<TTest, TCipher, TVariant>`, and every stream cipher inherits `SymmetricStreamAlgorithmTests<TTest, TAlgorithm>` for key/nonce sizing, lifecycle, transform-reuse, overlap, and disposal coverage. The asymmetric families inherit a parallel contract hierarchy: `AsymmetricAlgorithmTests<TTest, TAlgorithm>` carries the shared construction, key-size, raw import/export, and disposal contract, and its operation bases `KeyAgreementAlgorithmTests` (X25519), `SignatureAlgorithmTests` (Ed25519 and, via `MLDsaContractTests`, ML-DSA), and `KemAlgorithmTests` (via `MLKemContractTests`, ML-KEM) add the derive/sign/encapsulate contracts. The asymmetric known-answer corpora (RFC 7748/8032 vectors as in-memory KAT rows, plus curated Wycheproof x25519/ed25519 subsets and NIST ACVP ML-KEM / ML-DSA vectors embedded with provenance headers) run in the BVT and Regression tiers.
