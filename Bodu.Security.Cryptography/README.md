# Bodu.Security.Cryptography

Managed implementations of modern and legacy cryptographic primitives for .NET 8. The library provides block ciphers, AEAD modes, hash and MAC functions, padding schemes, asymmetric key agreement and signatures (including the FIPS 203/204 post-quantum algorithms), and the supporting transform infrastructure to compose them. All algorithms are exposed through the standard `SymmetricAlgorithm` / `HashAlgorithm` / `KeyedHashAlgorithm` / `AsymmetricAlgorithm` contracts so they slot into existing BCL pipelines (including `CryptoStream`).

## Security posture and limitations

Read this before using the library for anything that matters.

- **Not independently audited.** These are managed, from-scratch implementations. They are pinned to published known-answer vectors for *functional* correctness, but they have **not** undergone an independent security audit. Treat them as suitable for development, interop with non-FIPS systems, research, and education — not as a drop-in for a hardened production provider.
- **Not FIPS-validated.** This is not a FIPS 140-2 / 140-3 cryptographic module. For FIPS-validated AES, SHA-2, RNG, and similar primitives use the platform-provided `System.Security.Cryptography` types and the underlying OS provider.
- **Side-channel resistance is best-effort, not guaranteed.** Constant-time behaviour is implemented where practical: tag and hash comparisons use `CryptographicOperations.FixedTimeEquals` and padding removal is branchless. Work is ongoing to remove secret-dependent branches and table lookups from GHASH and the software table-based ciphers (Blowfish, Twofish, Camellia, Tiger, Whirlpool, Snefru). Managed code runs on a JIT and GC the library does not control, so timing/cache invariance cannot be guaranteed end-to-end regardless. For workloads with a real side-channel adversary, prefer the hardware-backed BCL primitives.
- **AES delegates to the BCL.** `AesBlockCipher` wraps the platform `System.Security.Cryptography.Aes` (hardware-accelerated, constant-time, FIPS-validated). Everything else in the package is a bespoke managed implementation.
- **This is a toolbox, not a safe-by-default API.** ECB, `NoPadding`, raw CBC, and unauthenticated stream ciphers are all first-class. Prefer an AEAD mode (GCM, EAX, OCB, or a nonce-misuse-resistant SIV / GCM-SIV) unless you have a specific reason not to, and read the per-type remarks for the failure modes.

The primitives provided here exist mainly to cover what the BCL does not ship (Blake2/Blake3, Ascon, Skein, Poly1305, SipHash, Threefish, Serpent, Camellia, Blowfish, Skipjack, OCB / EAX / SIV / GCM-SIV modes, X25519 / Ed25519, the post-quantum ML-KEM / ML-DSA on .NET 8, etc.).

## Algorithm support matrix

### Block ciphers

| Algorithm | Standard | Key sizes (bits) | Block size (bits) | Status | KAT source |
|---|---|---:|---:|---|---|
| AES (wrapper) | NIST FIPS 197 | 128, 192, 256 | 128 | Recommended | NIST FIPS 197 |
| Camellia | RFC 3713 | 128, 192, 256 | 128 | Recommended | RFC 3713 |
| Serpent | AES candidate (Anderson, Biham, Knudsen) | 128, 192, 256 | 128 | Recommended | AES-candidate official vectors |
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
| BLAKE3 | hash + XOF | BLAKE3 reference | 256 (extendable) | Recommended | Tree hashing; chunk and merge stack |
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

## Lifecycle and disposal guarantees

- **AEAD transforms** (`IAeadBlockCipherModeTransform`) are single-use per message. Each implementation tracks `_completed`, `_aadProcessed`, `_disposed`, rejects double-Encrypt/Decrypt with `InvalidOperationException`, rejects post-disposal access with `ObjectDisposedException`, compares tags with `CryptographicOperations.FixedTimeEquals`, and clears the plaintext destination on tag-verification failure.
- **`BlockCipherTransform`** owns and disposes the underlying `IBlockCipher`. Single-use; rejects `TransformBlock` after `TransformFinalBlock`. See the class XML remarks for the full ownership contract.
- **`Poly1305`** is a one-time MAC; the same instance throws `CryptographicException` on a second `ComputeHash` unless `Key` is explicitly reassigned. Disposing clears `_acc`, `_r`, `_s`, and `_key`.
- **`Blake3`** clears each `uint[]` on the chunk-CV stack on Dispose so per-subtree chaining values do not survive in heap memory.
- **GCM** rejects message lengths that would force its 32-bit `inc32` counter to wrap past `0xFFFFFFFF` while another block remains to be processed.

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

The shared `AeadBlockCipherModeTests<TTest, TTransform>` base contains the lifecycle / reuse / failed-tag-poisoning suite that every AEAD mode inherits. The `HashAlgorithmTests<TTest, TAlgorithm, TVariant>` base (and its `BlockHashAlgorithmTests` / `KeyedBlockHashAlgorithmTests` specialisations) provides spec-driven KAT, boundary, and disposal coverage for every hash and MAC. Block ciphers extend `BlockCipherTests<TTest, TCipher, TVariant>`, and every stream cipher inherits `SymmetricStreamAlgorithmTests<TTest, TAlgorithm>` for key/nonce sizing, lifecycle, transform-reuse, overlap, and disposal coverage. The post-quantum families inherit `MLKemContractTests<TKem>` / `MLDsaContractTests<TDsa>`, and the asymmetric known-answer corpora (curated Wycheproof x25519/ed25519 subsets and NIST ACVP ML-KEM / ML-DSA vectors, each with a provenance header) are embedded in the test assembly and run in the Regression tier.
