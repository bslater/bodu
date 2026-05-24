# Bodu.Security.Cryptography

Managed implementations of modern and legacy cryptographic primitives for .NET 8. The library provides block ciphers, AEAD modes, hash and MAC functions, padding schemes, and the supporting transform infrastructure to compose them. All algorithms are exposed through the standard `SymmetricAlgorithm` / `HashAlgorithm` / `KeyedHashAlgorithm` contracts so they slot into existing BCL pipelines (including `CryptoStream`).

## Not a FIPS-validated provider

This library is **not** a FIPS-validated cryptographic module and is not intended for environments that require FIPS 140-2 / 140-3 certification. For FIPS-validated AES, SHA-2, RNG, and similar primitives use the platform-provided `System.Security.Cryptography` types and the underlying OS provider. The algorithms here are managed implementations suitable for development, interop with non-FIPS systems, research, and use cases where the available BCL primitives do not cover what is needed (Blake2/Blake3, Ascon, Skein, Poly1305, SipHash, Threefish, Serpent, Camellia, Blowfish, Skipjack, OCB / EAX / SIV / GCM-SIV modes, etc.).

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

### Block cipher modes (unauthenticated)

| Mode | Notes |
|---|---|
| CBC | Standard CBC; pair with a MAC or use an AEAD for integrity |
| CFB | Self-synchronising; full-block segment size |
| CTR | Counter-mode keystream; same nonce-uniqueness rules as GCM apply |
| OFB | Synchronous stream from feedback register |
| CTS | Ciphertext stealing variant for non-aligned final blocks |
| ECB | Compatibility / primitive use only — leaks block-level patterns |

### Padding schemes

PKCS#7, ANSI X9.23, ISO 7816-4, ISO 10126, and `None`. All are exercised by `CryptoHelpersTests.PadBlock` / `.DepadBlock` with positive and negative cases.

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
| Tiger / Tiger2 | hash | Anderson & Biham | 128 / 160 / 192 | Legacy / compat | |
| FNV-1a | non-crypto hash | Fowler, Noll, Vo | 32 / 64 | Compatibility | Non-cryptographic |
| Adler-32 | checksum | RFC 1950 | 32 | Compatibility | Non-cryptographic |

CRC-3 through CRC-64 and Fletcher-16/32/64 live in the sibling `Bodu.IO.Hashing` package.

## Lifecycle and disposal guarantees

- **AEAD transforms** (`IAeadBlockCipherModeTransform`) are single-use per message. Each implementation tracks `_completed`, `_aadProcessed`, `_disposed`, rejects double-Encrypt/Decrypt with `InvalidOperationException`, rejects post-disposal access with `ObjectDisposedException`, compares tags with `CryptographicOperations.FixedTimeEquals`, and clears the plaintext destination on tag-verification failure.
- **`BlockCipherTransform`** owns and disposes the underlying `IBlockCipher`. Single-use; rejects `TransformBlock` after `TransformFinalBlock`. See the class XML remarks for the full ownership contract.
- **`Poly1305`** is a one-time MAC; the same instance throws `CryptographicException` on a second `ComputeHash` unless `Key` is explicitly reassigned. Disposing clears `_acc`, `_r`, `_s`, and `_key`.
- **`Blake3`** clears each `uint[]` on the chunk-CV stack on Dispose so per-subtree chaining values do not survive in heap memory.
- **GCM** rejects message lengths that would force its 32-bit `inc32` counter to wrap past `0xFFFFFFFF` while another block remains to be processed.

## Reusable infrastructure

| Helper | Purpose |
|---|---|
| `CryptoHelpers.Clear` / `ClearAndNullify` | Zero arrays, spans, or scalar fields in-place (delegates to `CryptographicOperations.ZeroMemory`) |
| `CryptoHelpers.ThrowIf*` | Argument-validation guards mirroring `Bodu.Core.ThrowHelper` conventions |
| `BlockCipherModeFactory` | Build a configured `BlockCipherTransform` from an `IBlockCipher`, `IBlockCipherModeTransform`, and `IPaddingStrategy` |
| `IAeadBlockCipherModeTransform` | Common surface for every AEAD mode (`ProcessAssociatedData` → `Encrypt`/`Decrypt`) |
| `IResumableHashAlgorithm` | Hash state snapshot/restore for resumable hashing |

## Testing

Tests live in `test/` and are organised as MSTest partial classes mirroring `src/`. Run tiers via the runsettings files at the solution root:

```bash
dotnet test Bodu.Security.Cryptography/test/Bodu.Security.Cryptography.Test.csproj --settings smoke.runsettings
dotnet test Bodu.Security.Cryptography/test/Bodu.Security.Cryptography.Test.csproj --settings bvt.runsettings
dotnet test Bodu.Security.Cryptography/test/Bodu.Security.Cryptography.Test.csproj --settings regression.runsettings
```

The shared `AeadBlockCipherModeTests<TTest, TTransform>` base contains the lifecycle / reuse / failed-tag-poisoning suite that every AEAD mode inherits. The shared `HashAlgorithmTests<TTest, TAlgorithm, TVariant>` base provides spec-driven KAT, boundary, and disposal coverage for every hash and MAC.
