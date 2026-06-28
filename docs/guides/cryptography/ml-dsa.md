---
title: ML-DSA post-quantum signatures
---

# ML-DSA post-quantum signatures

ML-DSA is the module-lattice digital signature algorithm standardized by NIST **FIPS 204** — the post-quantum companion to a classic signature scheme. Its security rests on the Module-LWE and SelfTargetMSIS problems, believed hard even for a large-scale quantum computer. This guide is for developers signing data whose signatures must remain trustworthy against future quantum attack.

**Bodu.Security.Cryptography** ships the three FIPS 204 parameter sets as sealed types over the shared <xref:Bodu.Security.Cryptography.MLDsa> base: <xref:Bodu.Security.Cryptography.MLDsa44>, <xref:Bodu.Security.Cryptography.MLDsa65>, and <xref:Bodu.Security.Cryptography.MLDsa87>. This is pure ML-DSA; the pre-hash variant HashML-DSA is out of scope.

For a compact classic signature scheme, see [Ed25519](signatures-ed25519.md); for the post-quantum key-agreement counterpart, see [ML-KEM](ml-kem.md).

## Parameter sets and sizes

| Type | NIST category | Public key | Private key | Signature |
|---|---|---|---|---|
| <xref:Bodu.Security.Cryptography.MLDsa44> | 2 | 1312 B | 2560 B | 2420 B |
| <xref:Bodu.Security.Cryptography.MLDsa65> | 3 | 1952 B | 4032 B | 3309 B |
| <xref:Bodu.Security.Cryptography.MLDsa87> | 5 | 2592 B | 4896 B | 4627 B |

The private seed ξ accepted by `ImportPrivateSeed` is always 32 bytes (`MLDsa.PrivateSeedSizeInBytes`); a signing context string is at most 255 bytes (`MLDsa.MaxContextSizeInBytes`). Each instance also reports its sizes at runtime through `PublicKeySizeInBytes`, `PrivateKeySizeInBytes`, and `SignatureSizeInBytes`.

**ML-DSA-65** is the most widely recommended general-purpose set. Pick ML-DSA-44 for the smallest signatures where category-2 security is acceptable, and ML-DSA-87 for the highest margin on the most sensitive material.

## Signing and verifying

Generate a key pair, sign with <xref:Bodu.Security.Cryptography.MLDsa.SignData(System.ReadOnlySpan{System.Byte})>, and verify with <xref:Bodu.Security.Cryptography.MLDsa.VerifyData(System.ReadOnlySpan{System.Byte},System.ReadOnlySpan{System.Byte})>.

```csharp
using System.Text;
using Bodu.Security.Cryptography;

byte[] message = Encoding.UTF8.GetBytes("release build 2.1.0");

using var signer = MLDsa65.Create();
signer.GenerateKey();

byte[] signature = signer.SignData(message);   // 3309 bytes

bool valid = signer.VerifyData(message, signature);   // true
```

`SignData` and `VerifyData` each have a context-string overload (next section) and a span overload — `SignData(data, context, destination)` writes the signature into a caller-supplied buffer of exactly `SignatureSizeInBytes`. `HasPrivateKey` / `HasPublicKey` report which halves an instance holds, exactly as for [Ed25519](signatures-ed25519.md).

## Key distribution

The signer keeps the private key (or the compact 32-byte seed) secret; the public key is distributed to every verifier. Importing the seed regenerates the full key pair; importing a public key onto an instance discards any private key it held, leaving a verify-only instance.

```csharp
using Bodu.Security.Cryptography;

using var signer = MLDsa65.Create();
signer.GenerateKey();

byte[] publicKey = signer.ExportPublicKey();    // 1952 bytes, distribute freely
byte[] privateKey = signer.ExportPrivateKey();  // private, store in a vault

// A verifier needs only the public key:
using var verifier = MLDsa65.Create();
verifier.ImportPublicKey(publicKey);
bool ok = verifier.VerifyData(message, signature);
```

`ImportPrivateKey` cross-checks the embedded public-key hash and throws <xref:System.ArgumentException> on a key whose secret vectors do not match.

## Context strings

ML-DSA signing accepts an optional **context** string of up to 255 bytes that domain-separates signatures across applications: a signature created with a context verifies **only** when the same context is supplied at verification. Use it to stop a signature minted for one purpose from being replayed in another.

```csharp
using System.Text;
using Bodu.Security.Cryptography;

byte[] message = Encoding.UTF8.GetBytes("release build 2.1.0");
byte[] context = Encoding.UTF8.GetBytes("myapp:firmware-v1");

using var signer = MLDsa65.Create();
signer.GenerateKey();

byte[] signature = signer.SignData(message, context);

// Verifying with the matching context succeeds; a different (or empty) context fails.
bool ok = signer.VerifyData(message, signature, context);          // true
bool wrong = signer.VerifyData(message, signature);               // false (empty context)
```

A context longer than 255 bytes throws <xref:System.ArgumentException> at both sign and verify time.

## Deterministic versus hedged signing

By default ML-DSA signing is **hedged**: each signature mixes 32 fresh random bytes into the nonce derivation. This is the FIPS 204 default and protects against fault-injection and randomness-disclosure attacks. The same message signed twice under the same key therefore yields **different** signatures — both valid.

Set <xref:Bodu.Security.Cryptography.MLDsa.DeterministicSigning> to `true` to substitute the all-zero string, making signatures reproducible for a fixed key, message, and context. Both variants are standard and verify identically.

```csharp
using var signer = MLDsa65.Create();
signer.GenerateKey();
signer.DeterministicSigning = true;   // reproducible output

byte[] a = signer.SignData(message);
byte[] b = signer.SignData(message);
// a and b are byte-for-byte identical.
```

Prefer the default hedged mode unless you have a specific reason to need reproducible signatures (for example, deterministic test vectors).

## Verification never throws on a bad signature

`VerifyData` returns `false` for every invalid input — a wrong-length, malformed, or non-canonical signature, a tampered message, or a mismatched context. It throws only when the instance holds no public key (<xref:System.Security.Cryptography.CryptographicException>), or when the context exceeds 255 bytes. Treat the boolean result as the sole signal and reject on `false` without branching on the reason.

## See also

- [Asymmetric algorithms overview](asymmetric-overview.md) — where ML-DSA sits in the family.
- [Signatures with Ed25519](signatures-ed25519.md) — the compact classic signature counterpart.
- [ML-KEM post-quantum key encapsulation](ml-kem.md) — the post-quantum key-agreement companion.
- <xref:Bodu.Security.Cryptography.MLDsa>, <xref:Bodu.Security.Cryptography.MLDsa44>, <xref:Bodu.Security.Cryptography.MLDsa65>, <xref:Bodu.Security.Cryptography.MLDsa87> — API reference.
