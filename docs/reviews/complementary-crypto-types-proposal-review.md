# Complementary Cryptography and Hashing Types — Proposal Review

**Date:** 2026-06-13
**Scope:** `Bodu.Security.Cryptography`, `Bodu.IO.Hashing`, `Bodu.Text.Encoding`, and the shared test infrastructure (`Bodu.Test`, per-project `test/Infrastructure` folders).
**Subject:** An external design report recommending ~40 complementary "semantic value types" and supporting abstractions (hash/MAC/signature value types, key-material holders, nonce/salt/tag wrappers, encoding helpers, PEM support, streaming hashing, crypto policy, envelope formats, and test-vector infrastructure) for the Bodu cryptography and hashing libraries.

## 1. Method

Each proposed type was assessed against three questions:

1. **Does an equivalent already exist** in the solution or in the .NET BCL? Bodu prefers standard types over parallel abstractions.
2. **Does a consumer exist today** (or in active development) that would use the type? Types without consumers accrete as dead API surface.
3. **Is the type high-value** — something teams repeatedly hand-roll because the platform does not supply it — rather than niche or speculative?

The review was grounded in a full survey of the production and test surfaces of `Bodu.Security.Cryptography` (191 source files), `Bodu.IO.Hashing`, `Bodu.Text.Encoding`, and the KAT/assertion infrastructure.

## 2. Verdicts

### 2.1 Rejected — duplicative of existing Bodu types

| Proposed type | Existing equivalent | Notes |
|---|---|---|
| `Hex` | `Bodu.Text.Encoding.Base16` | Complete span/UTF-8 hex codec: strict decode by default, `TryDecode`/`IsValid`, case selection, prefix/spacing/line-break formatting, lenient styles (`BaseFormatStyles.AllowPrefix`, `IgnoreWhitespace`). Inside `Bodu.Security.Cryptography` (which does not reference `Bodu.Text.Encoding`), `System.Convert.To/FromHexString` covers the need. |
| `Base64Url` | `Bodu.Text.Encoding.Base64Url` | Already JOSE-compatible: unpadded output by default, accepts padded and unpadded input (`BaseFormatStyles.AllowMissingPadding`). |
| `KnownAnswerVector` / `KnownAnswerVectorSet` | Strongly typed KAT records | The proposal's `IReadOnlyDictionary<string, byte[]>` model is *weaker* than the established per-domain records: `AeadKnownAnswerVector`, `StreamAeadKnownAnswerVector` (with `AeadKatSourceKind` provenance and output-layout metadata), `KdfKnownAnswerVector`, `BlockCipherKnownAnswer`, `HashAlgorithmKnownAnswer`, `KeyedHashAlgorithmKnownAnswer`, plus the generic `Bodu.Test.Kat` primitives. Adopting a stringly-typed bag would regress type safety and IDE discoverability. |
| `VectorSourceKind` | `AeadKatSourceKind` | Already exists for the family that needs it; generalise only when a second family requires provenance metadata. |
| `IncrementalHashBuilder` | `System.IO.Hashing.NonCryptographicHashAlgorithm` (`Append`/`GetCurrentHash`/`Reset`) and `HashAlgorithmExtensions.AppendData`/`VerifyHash` | Both incremental models already exist and are idiomatic; an extra abstraction layer adds indirection without capability. |

### 2.2 Rejected — covered by standard BCL types

| Proposed type | BCL equivalent | Notes |
|---|---|---|
| `PemBlock` / `PemDocument` | `System.Security.Cryptography.PemEncoding` / `PemFields` | The BCL parses and writes PEM (including multi-block scans via repeated `TryFind`). Per project direction, Bodu builds on standard types rather than wrapping them. Revisit only if the asymmetric work surfaces a concrete ergonomic gap. |
| Fixed-time comparison helper | `CryptographicOperations.FixedTimeEquals` | Already used throughout the library; the adopted value types expose it as instance methods rather than re-implementing it. |
| Secret zeroing helper | `CryptographicOperations.ZeroMemory` (via `CryptographyHelper.Clear`) | Already established. |

### 2.3 Deferred — sequenced behind the in-development asymmetric work

Asymmetric algorithms are in active development for `Bodu.Security.Cryptography`. The following types are *sequenced*, not rejected: each needs the asymmetric key types (or an envelope/manifest feature) to define its shape, and designing them first would lock in guesses.

| Proposed type | Revisit trigger |
|---|---|
| `KeyFingerprint`, `KeyIdentifier` | When public-key types land and define canonical key encodings. |
| `KeyMaterial` / `KeyUsage` | When more than one algorithm family needs usage/policy metadata beyond `SecretBytes`. |
| `AeadEnvelope`, `EncryptedEnvelope`, `EnvelopeRecipient`, `AlgorithmSuite` | When an envelope-encryption feature is committed; depends on `KeyIdentifier` and algorithm identifiers. |
| `CryptoPolicy` / `CryptoPolicyResult` | When the library hosts algorithms a policy would gate (legacy hashes, RSA key sizes). |
| `HashAlgorithmId` / `HashAlgorithmInfo`, `MacAlgorithmId`, `SignatureAlgorithmId` | When manifests, envelopes, or the asymmetric naming scheme need a registry; `CrcStandard` is the in-repo precedent for catalogue design. |
| `VerificationResult` | When signature verification APIs exist; `VerifyHash`/`TryVerifyHash` extensions cover hashes today. |
| `HashValue<TAlgorithm>` | The proposal itself rates this P1; revisit alongside the algorithm-id work. |
| `HashWriter` (structured/length-prefixed hashing) | When a consumer needs canonical structured hashing (e.g. fingerprints over key metadata). |
| `DigestManifest` / `DigestManifestEntry` | When a file-manifest feature is committed; natural home is `Bodu.IO.Hashing`. |
| `MacValue` | Keyed-hash outputs flow through `KeyedHashAlgorithm` today; a separate MAC value type earns its place when MAC-specific APIs (detached MACs, algorithm ids) exist. |

### 2.4 Adopted — implemented in this change

High-value, commonly hand-rolled, immediately consumable:

| Type | Location | Rationale |
|---|---|---|
| `HashValue` | `Bodu.Security.Cryptography` | Immutable digest value with strict hex parsing, lowercase hex/Base64 formatting, and explicit fixed-time equality — the wrapper every project reinvents around `byte[]` digests. |
| `Nonce`, `Salt` | `Bodu.Security.Cryptography` | Semantic inputs for the AEAD modes (GCM, GCM-SIV, CCM, EAX, OCB, SIV, Ascon, XChaCha20-Poly1305) and KDFs (Argon2, Scrypt); `Random(length)` factories over the existing CSPRNG helpers. |
| `AuthenticationTag` | `Bodu.Security.Cryptography` | AEAD tag value with fixed-time comparison; clarifies APIs and test assertions. |
| `SignatureValue` + `SignatureFormat` | `Bodu.Security.Cryptography` | Foundation for the asymmetric work. Carries the wire format (`Der` vs `P1363` vs `Raw`) explicitly — the single most common interop failure for ECDSA-style signatures. |
| `SecretBytes` | `Bodu.Security.Cryptography` | Disposable, pinned, zero-on-dispose holder for secret material, built on `CryptographicOperations.ZeroMemory`. Documented honestly as hygiene, not an enclave. |
| `HashingStream` | `Bodu.IO.Hashing` | Pass-through `Stream` that feeds a `NonCryptographicHashAlgorithm` while bytes are read or written — hashing-while-copying without a second pass. |
| `CryptoAssert`, `Tamper` | crypto test `Infrastructure/` | Byte-equality assertion with hex diff output, and bit/byte-flip mutation helpers for AEAD tamper tests. Exception assertions remain inline at call sites per repository convention. |

## 3. Recorded design decisions

These bind the adopted types and should guide the deferred ones:

1. **Flat namespaces.** New types join `Bodu.Security.Cryptography` and `Bodu.IO.Hashing` directly, matching the projects' existing flat layout. The proposal's deep namespace tree (`…Cryptography.Hashing`, `…Cryptography.Keys`, `…Cryptography.Symmetric`) is rejected: it would strand the new types away from the 190+ existing types they complement.
2. **Equality policy.** Structs implement ordinary structural `IEquatable<T>` equality (`Equals`, `==`/`!=`, `GetHashCode` via `HashCode.AddBytes`). Fixed-time comparison is an explicit opt-in (`FixedTimeEquals`) on the security-relevant types (`HashValue`, `AuthenticationTag`, `SignatureValue`, `SecretBytes`), matching the BCL precedent where `CryptographicOperations.FixedTimeEquals` is always a deliberate call. A constant-time `Equals` would be false comfort while `GetHashCode` and dictionary behaviour still leak.
3. **Hex casing.** `ToHexString()` emits lowercase (the digest-tool convention); parsing accepts both cases. `ToString()` on non-secret values returns the hex form; `SecretBytes.ToString()` never reveals content.
4. **`default(T)` is the empty value.** All byte-wrapping structs normalise an unset backing array to empty — `Length == 0`, `IsEmpty`, empty span, `""` hex — with no throw-on-default trapdoors.
5. **Defensive copying.** Persistent values copy on the way in (`FromBytes`) and on the way out (`ToArray`); spans are exposed read-only.
6. **Honest secret handling.** `SecretBytes` pins its buffer and zeroes it on dispose, and its documentation states plainly what managed .NET cannot guarantee (runtime copies, diagnostics, swap).

## 4. Out-of-scope confirmation

This change is purely additive: no existing algorithm, transform, extension method, or test was modified. The only edits to existing files are new resource-string entries (`CryptoResourceStrings`, `HashingResourceStrings`) required by the no-hard-coded-messages rule.
