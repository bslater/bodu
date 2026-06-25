---
title: Asymmetric algorithms overview
---

# Asymmetric algorithms overview

This guide is the map of the **asymmetric** primitives in **Bodu.Security.Cryptography** — the public-key family, where each party holds a key pair and the public half can be shared freely. It is aimed at developers choosing between elliptic-curve and post-quantum building blocks, and explains how all four types share the same shape so the detail pages read consistently.

The library ships four asymmetric types, split across two generations and two roles:

| Generation | Key agreement | Signatures |
|---|---|---|
| Classic ECC | <xref:Bodu.Security.Cryptography.X25519> (RFC 7748) | <xref:Bodu.Security.Cryptography.Ed25519> (RFC 8032) |
| Post-quantum | ML-KEM — <xref:Bodu.Security.Cryptography.MLKem512> / <xref:Bodu.Security.Cryptography.MLKem768> / <xref:Bodu.Security.Cryptography.MLKem1024> (FIPS 203) | ML-DSA — <xref:Bodu.Security.Cryptography.MLDsa44> / <xref:Bodu.Security.Cryptography.MLDsa65> / <xref:Bodu.Security.Cryptography.MLDsa87> (FIPS 204) |

The classic curve algorithms are battle-tested, compact, and fast; they are *not* believed secure against a future large-scale quantum computer. The post-quantum (PQC) algorithms are the NIST-standardized lattice schemes designed to survive that threat, at the cost of much larger keys and ciphertexts. For data that must stay confidential for years — the "harvest now, decrypt later" risk — pair the two (see the [hybrid note in the ML-KEM guide](ml-kem.md#hybrid-with-x25519)).

## The shared `AsymmetricAlgorithm` base

Every type derives from <xref:System.Security.Cryptography.AsymmetricAlgorithm?displayProperty=nameWithType> and follows the same conventions, so once you know one, you know the surface of all four:

- A static `Create()` factory returns a fresh instance with **no** key material.
- `GenerateKey()` draws a new key pair from a cryptographically secure source.
- `Import*` / `Export*` members move **raw** key bytes in and out (see below).
- `Has*` properties report which key halves are currently present.
- The type is `IDisposable` — private key material is zeroed on dispose, so always wrap instances in `using`.

```csharp
using Bodu.Security.Cryptography;

using var alg = X25519.Create();   // no keys yet
alg.GenerateKey();                 // now holds a private + public key
// ... use alg ...
// dispose (via 'using') zeroes the private key
```

For ML-KEM and ML-DSA the reported `KeySize` is **not** a bit length — it is the FIPS parameter-set designator (512 / 768 / 1024 for ML-KEM; 44 / 65 / 87 for ML-DSA), because module-lattice keys have no single meaningful bit-length.

## Raw key encodings only

These types expose **only** the raw byte encodings defined by their specifications — the fixed-width RFC 7748 / RFC 8032 keys for the curve algorithms, and the FIPS 203 / FIPS 204 byte strings (and seeds) for the lattice algorithms. The PKCS#8 / SubjectPublicKeyInfo (DER / PEM) members inherited from `AsymmetricAlgorithm` are **not** implemented and retain their base throwing behavior. If you need to persist or interchange a key, store the raw bytes from the `Export*` method directly.

## Choosing a type

| You need… | Reach for | Detail page |
|---|---|---|
| A shared secret between two parties (ECDH) | <xref:Bodu.Security.Cryptography.X25519> | [Key agreement with X25519](key-agreement-x25519.md) |
| To sign and verify messages (classic) | <xref:Bodu.Security.Cryptography.Ed25519> | [Signatures with Ed25519](signatures-ed25519.md) |
| A quantum-resistant key encapsulation (KEM) | ML-KEM (`MLKem512/768/1024`) | [ML-KEM post-quantum key encapsulation](ml-kem.md) |
| Quantum-resistant signatures | ML-DSA (`MLDsa44/65/87`) | [ML-DSA post-quantum signatures](ml-dsa.md) |
| Long-lived confidentiality against future quantum attack | X25519 **and** ML-KEM, combined | [hybrid note](ml-kem.md#hybrid-with-x25519) |

A KEM (ML-KEM) is the post-quantum stand-in for Diffie-Hellman key agreement: instead of both parties contributing a public key to derive a shared secret, one party encapsulates a fresh secret *to* the other's public key. Use it where you would otherwise have used X25519 to bootstrap a symmetric session.

## Disposal and lifecycle

All four types are disposable and hold sensitive key material. The rules are uniform:

- Always construct with `using` (or call `Dispose()` explicitly) so the private key is zeroed.
- A fresh instance holds no keys — call `GenerateKey()` or an `Import*` method before using it. Operating without the required key half throws <xref:System.Security.Cryptography.CryptographicException>.
- Importing a *public* key onto an instance discards any private key it held, leaving a verify-only / encapsulate-only instance.
- After disposal every member throws <xref:System.ObjectDisposedException>.

> [!NOTE]
> Like the rest of the library, these implementations offer best-effort side-channel resistance and have **not** been independently audited. Prefer a platform-provided implementation where one exists and your threat model demands certified code.

## See also

- [Bodu.Security.Cryptography guides](index.md) — the full guide index for the library.
- [Encryption basics](encryption-basics.md) — key material, randomness, and disposal for the symmetric side.
- [Key agreement with X25519](key-agreement-x25519.md), [Signatures with Ed25519](signatures-ed25519.md), [ML-KEM](ml-kem.md), [ML-DSA](ml-dsa.md) — the four detail pages.
- <xref:Bodu.Security.Cryptography.X25519>, <xref:Bodu.Security.Cryptography.Ed25519>, <xref:Bodu.Security.Cryptography.MLKem>, <xref:Bodu.Security.Cryptography.MLDsa> — API reference.
