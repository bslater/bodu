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

> [!NOTE]
> **Harvest now, decrypt later.** An adversary cannot break X25519 or ML-KEM today, but it can *record* an exchange today and decrypt it the moment a cryptographically relevant quantum computer exists. A signature only needs to be quantum-resistant when it is *verified* in that future; a *confidentiality* exchange must be quantum-resistant the moment it is *captured*. That asymmetry is why long-lived secrets motivate ML-KEM (or an X25519 + ML-KEM hybrid) now, while signatures can migrate to ML-DSA more gradually.

## Three roles, two generations

The four types fill three distinct roles. Knowing which role you need is the first cut; the generation (classic vs post-quantum) is the second.

| Role | What it gives you | Classic | Post-quantum |
|---|---|---|---|
| **Signature** | Integrity and authenticity — the private-key holder signs, anyone with the public key verifies. No shared secret. | <xref:Bodu.Security.Cryptography.Ed25519> | <xref:Bodu.Security.Cryptography.MLDsa> |
| **Key agreement** | A shared secret both parties derive, each contributing a public key (ECDH). | <xref:Bodu.Security.Cryptography.X25519> | — (use a KEM instead) |
| **Key encapsulation (KEM)** | A shared secret one party encapsulates *to* the other's public key, transmitting a ciphertext. | — | <xref:Bodu.Security.Cryptography.MLKem> |

A signature establishes *who*; key agreement and a KEM establish a *secret*. Neither secret-establishing role authenticates the peer on its own — combine it with a signature, a pre-shared key, or an authenticated channel (HPKE's auth modes do exactly this).

## The shared `AsymmetricAlgorithm` base

Every type derives from <xref:System.Security.Cryptography.AsymmetricAlgorithm?displayProperty=nameWithType> and follows the same conventions, so once you know one, you know the surface of all four:

- A static `Create()` factory returns a fresh instance with **no** key material.
- `GenerateKey()` draws a new key pair from a cryptographically secure source.
- `Import*` / `Export*` members move **raw** key bytes in and out (see below).
- `Has*` properties report which key halves are currently present.
- The type is `IDisposable` — private key material is zeroed on dispose, so always wrap instances in `using`.

The lifecycle is uniform: **`Create()` → `GenerateKey()` (or `Import*`) → use → dispose.** "Use" is the one step that differs by role — sign/verify, agree, or encapsulate/decapsulate.

```csharp
using Bodu.Security.Cryptography;

using var alg = X25519.Create();   // no keys yet
alg.GenerateKey();                 // now holds a private + public key
// ... use alg ...
// dispose (via 'using') zeroes the private key
```

For ML-KEM and ML-DSA the reported `KeySize` is **not** a bit length — it is the FIPS parameter-set designator (512 / 768 / 1024 for ML-KEM; 44 / 65 / 87 for ML-DSA), because module-lattice keys have no single meaningful bit-length.

## Raw key encodings only

These types expose **only** the raw byte encodings defined by their specifications — the fixed-width RFC 7748 / RFC 8032 keys for the curve algorithms, and the FIPS 203 / FIPS 204 byte strings (and seeds) for the lattice algorithms. The PKCS#8 / SubjectPublicKeyInfo (DER / PEM) members inherited from `AsymmetricAlgorithm` are **not** implemented and retain their base throwing behaviour. If you need to persist or interchange a key, store the raw bytes from the `Export*` method directly.

The seed-bearing lattice types (`ImportPrivateSeed`) let you store the compact seed — 32 bytes for ML-DSA, 64 for ML-KEM — instead of the full multi-kilobyte private key, and regenerate the whole key pair on import. The curve algorithms re-derive the public key from the 32-byte private seed the same way.

## Which key half is present

Each type reports the halves it holds through its own `Has*` pair. A freshly created instance has neither; `GenerateKey()` sets both; importing only a public key sets the public half and discards the private. The naming follows each role's vocabulary:

| Type | Private half | Public half |
|---|---|---|
| <xref:Bodu.Security.Cryptography.X25519> / <xref:Bodu.Security.Cryptography.Ed25519> | `HasPrivateKey` | `HasPublicKey` |
| <xref:Bodu.Security.Cryptography.MLDsa> | `HasPrivateKey` | `HasPublicKey` |
| <xref:Bodu.Security.Cryptography.MLKem> | `HasDecapsulationKey` | `HasEncapsulationKey` |

## Verify-or-fail discipline

For the two signature schemes, `VerifyData` returns a `bool` and **never** throws on a bad signature — a wrong length, a tampered message, a non-canonical encoding, or a mismatched ML-DSA context all return `false`. Treat the boolean as the *only* signal and reject on `false` without inspecting why:

```csharp
if (!verifier.VerifyData(message, signature))
    throw new InvalidOperationException("Signature verification failed.");
```

`VerifyData` throws only on a *configuration* error — the instance holds no public key (<xref:System.Security.Cryptography.CryptographicException>), or an ML-DSA context exceeds 255 bytes (<xref:System.ArgumentException>). The secret-establishing roles fail differently and deliberately: X25519 throws on a low-order peer point, while ML-KEM's `Decapsulate` *succeeds* on a tampered ciphertext but yields an unrelated secret (implicit rejection) — so a successful decapsulation is never proof the ciphertext was genuine. Confirm the secret downstream through an AEAD or MAC. Each detail page covers its own failure contract.

## Recording a signature's wire format

A raw signature is just bytes; the same mathematical signature can circulate in incompatible encodings (the classic ECDSA DER-vs-P1363 split). When a signature crosses a boundary where the encoding is ambiguous, <xref:Bodu.Security.Cryptography.SignatureValue> pairs the bytes with a <xref:Bodu.Security.Cryptography.SignatureFormat> tag so a downstream verifier branches on the recorded format instead of guessing. Ed25519 and ML-DSA both emit `SignatureFormat.Raw`, so this matters only at interop seams with DER/P1363 ECDSA.

```csharp
SignatureValue value = SignatureValue.FromBytes(signature, SignatureFormat.Raw);
// value.FixedTimeEquals(other) compares bytes in fixed time; value.Format carries the encoding.
```

## Choosing a type

| You need… | Reach for | Detail page |
|---|---|---|
| A shared secret between two parties (ECDH) | <xref:Bodu.Security.Cryptography.X25519> | [Key agreement with X25519](key-agreement-x25519.md) |
| To sign and verify messages (classic) | <xref:Bodu.Security.Cryptography.Ed25519> | [Signatures with Ed25519](signatures-ed25519.md) |
| A quantum-resistant key encapsulation (KEM) | ML-KEM (`MLKem512/768/1024`) | [ML-KEM post-quantum key encapsulation](ml-kem.md) |
| Quantum-resistant signatures | ML-DSA (`MLDsa44/65/87`) | [ML-DSA post-quantum signatures](ml-dsa.md) |
| Long-lived confidentiality against future quantum attack | X25519 **and** ML-KEM, combined | [hybrid note](ml-kem.md#hybrid-with-x25519) |
| Encrypt a message *to* a public key (hybrid PKE) | <xref:Bodu.Security.Cryptography.Hpke> over X25519 | [Hybrid public key encryption with HPKE](hpke.md) |

A KEM (ML-KEM) is the post-quantum stand-in for Diffie-Hellman key agreement: instead of both parties contributing a public key to derive a shared secret, one party encapsulates a fresh secret *to* the other's public key. Use it where you would otherwise have used X25519 to bootstrap a symmetric session.

These types are the building blocks, not a complete encryption scheme. To encrypt a payload directly to a recipient's public key, <xref:Bodu.Security.Cryptography.Hpke> (RFC 9180) composes the X25519 KEM, [HKDF](hkdf.md), and an AEAD into the standardized Hybrid Public Key Encryption construction — prefer it over assembling key agreement, key derivation, and a cipher by hand. See [Hybrid public key encryption with HPKE](hpke.md).

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
- [Hybrid public key encryption with HPKE](hpke.md) — the standardized scheme that composes the X25519 KEM, [HKDF](hkdf.md), and an AEAD.
- <xref:Bodu.Security.Cryptography.X25519>, <xref:Bodu.Security.Cryptography.Ed25519>, <xref:Bodu.Security.Cryptography.MLKem>, <xref:Bodu.Security.Cryptography.MLDsa>, <xref:Bodu.Security.Cryptography.SignatureValue> — API reference.
