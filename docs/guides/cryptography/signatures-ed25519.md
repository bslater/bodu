---
title: Signatures with Ed25519
---

# Signatures with Ed25519

<xref:Bodu.Security.Cryptography.Ed25519> is the EdDSA signature scheme over edwards25519 defined in RFC 8032 (the "pure" variant). It produces **deterministic** 64-byte signatures from a 32-byte private seed at a 128-bit security level: the same message signed twice under the same key yields the identical signature, and no per-signature nonce is consumed — which removes the catastrophic nonce-reuse failure mode of ECDSA-style schemes. This guide is for developers signing messages and verifying them with a distributed public key.

Ed25519 signs and verifies; it agrees no keys. For establishing a shared secret, see [X25519](key-agreement-x25519.md). For a quantum-resistant signature scheme, see [ML-DSA](ml-dsa.md).

## Fixed sizes at a glance

| Parameter | Size | Constant |
|---|---|---|
| Private key (seed) | 32 bytes | `Ed25519.PrivateKeySizeInBytes` |
| Public key | 32 bytes | `Ed25519.PublicKeySizeInBytes` |
| Signature (R ‖ S) | 64 bytes | `Ed25519.SignatureSizeInBytes` |
| Hash | SHA-512 | — |
| Security level | 128 bits | — |

## Signing and verifying

Generate a key pair, sign with <xref:Bodu.Security.Cryptography.Ed25519.SignData(System.ReadOnlySpan{System.Byte})>, and verify with <xref:Bodu.Security.Cryptography.Ed25519.VerifyData(System.ReadOnlySpan{System.Byte},System.ReadOnlySpan{System.Byte})>.

```csharp
using System.Text;
using Bodu.Security.Cryptography;

byte[] message = Encoding.UTF8.GetBytes("transfer 100 to account 42");

using var signer = Ed25519.Create();
signer.GenerateKey();

byte[] signature = signer.SignData(message);   // 64 bytes

bool valid = signer.VerifyData(message, signature);   // true
```

The same instance can both sign and verify while it holds the private key, but the typical deployment splits the two roles across machines.

`SignData` and `VerifyData` each have a span overload — `SignData(data, destination)` writes the 64-byte signature into a caller-supplied buffer with no allocation — for hot paths or pooled buffers. Query `HasPrivateKey` / `HasPublicKey` to see which halves an instance currently holds: a signer holds both, a verifier holds only the public half.

## Key distribution

The signer keeps the private seed secret; the public key is distributed to every party that needs to verify. The public key carries no secret and can be embedded in config, served over the network, or pinned in source.

```csharp
using var signer = Ed25519.Create();
signer.GenerateKey();

byte[] privateSeed = signer.ExportPrivateKey();   // 32 bytes — keep secret, store in a vault
byte[] publicKey = signer.ExportPublicKey();      // 32 bytes — distribute freely
```

To restore a signer from stored material, import the seed; the public key is re-derived automatically:

```csharp
using var restored = Ed25519.Create();
restored.ImportPrivateKey(privateSeed);   // public key re-derived from the seed
```

## Verifying with only the public key

A verifier never needs the private seed. Import the public key with <xref:Bodu.Security.Cryptography.Ed25519.ImportPublicKey(System.ReadOnlySpan{System.Byte})> and verify. Importing a public key onto an instance discards any private key it held, leaving a verify-only instance.

```csharp
using System.Text;
using Bodu.Security.Cryptography;

byte[] message = Encoding.UTF8.GetBytes("transfer 100 to account 42");
byte[] signature = ReceiveSignature();   // 64 bytes, off the wire
byte[] publicKey = LoadPinnedPublicKey();

using var verifier = Ed25519.Create();
verifier.ImportPublicKey(publicKey);

if (!verifier.VerifyData(message, signature))
    throw new InvalidOperationException("Signature verification failed.");
```

`ImportPublicKey` validates the encoding by fully decompressing the point — a non-canonical or off-curve 32-byte value throws <xref:System.ArgumentException> at import time, so a malformed key never reaches verification.

## Verification never throws on a bad signature

`VerifyData` returns `false` for **every** invalid input — a wrong-length signature, a tampered message, a non-canonical R, or an S component at or above the group order (the RFC 8032 malleability check). It throws only when the instance holds no public key (<xref:System.Security.Cryptography.CryptographicException>). Treat the boolean result as the sole signal:

```csharp
bool ok = verifier.VerifyData(message, signature);
if (!ok)
{
    // Reject — do not branch on why; any failure is a failure.
}
```

Because all inputs to verification are public, verification time may vary with the inputs; that is acceptable and expected.

## Recording the signature's wire format

An Ed25519 signature is the raw 64-byte `R ‖ S` string with no framing — `SignatureFormat.Raw`. That is unambiguous on its own, but when a signature travels through a layer that also handles ECDSA (which circulates as both ASN.1 DER and fixed-width P1363), wrap it in a <xref:Bodu.Security.Cryptography.SignatureValue> so the consumer reads the encoding instead of inferring it:

```csharp
SignatureValue value = SignatureValue.FromBytes(signature, SignatureFormat.Raw);

// 'value.FixedTimeEquals(other)' compares the bytes in constant time;
// 'value.Format' tells a polymorphic verifier this is a raw EdDSA signature, not DER or P1363.
```

## Determinism

Ed25519 signatures are deterministic by design: identical key and message always produce identical output. This is a feature — there is no signing randomness to mismanage — but it means you cannot distinguish two signings of the same message. If you need each signing to differ, include a unique element (timestamp, counter, nonce) in the signed message itself.

## What Ed25519 is not

- **Not key agreement.** It establishes no shared secret; use [X25519](key-agreement-x25519.md) for that.
- **Not quantum-resistant.** A future quantum computer breaks it. For long-lived signatures, use [ML-DSA](ml-dsa.md).
- **Not the pre-hash or context variants.** Only pure Ed25519 is implemented; Ed25519ph and Ed25519ctx are out of scope. If you need a domain-separation context on signatures, [ML-DSA](ml-dsa.md#context-strings) supports one.

## See also

- [Asymmetric algorithms overview](asymmetric-overview.md) — where Ed25519 sits in the family.
- [Key agreement with X25519](key-agreement-x25519.md) — the companion curve algorithm for shared secrets.
- [ML-DSA post-quantum signatures](ml-dsa.md) — the quantum-resistant signature scheme.
- <xref:Bodu.Security.Cryptography.Ed25519>, <xref:Bodu.Security.Cryptography.SignatureValue> — API reference.
