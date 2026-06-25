---
title: ML-KEM post-quantum key encapsulation
---

# ML-KEM post-quantum key encapsulation

ML-KEM is the module-lattice key-encapsulation mechanism standardized by NIST **FIPS 203** — the post-quantum replacement for Diffie-Hellman-style key agreement. Its security rests on the Module-LWE problem, believed hard even for a large-scale quantum computer. This guide is for developers bootstrapping a symmetric session that must stay confidential against future quantum attack.

A KEM works differently from ECDH: instead of both parties contributing a public key, the receiver publishes an **encapsulation key**, and any sender calls <xref:Bodu.Security.Cryptography.MLKem.Encapsulate> to produce a **ciphertext** plus a fresh 32-byte shared secret. The receiver recovers the same secret from the ciphertext with <xref:Bodu.Security.Cryptography.MLKem.Decapsulate(System.ReadOnlySpan{System.Byte})>.

**Bodu.Security.Cryptography** ships the three FIPS 203 parameter sets as sealed types over the shared <xref:Bodu.Security.Cryptography.MLKem> base: <xref:Bodu.Security.Cryptography.MLKem512>, <xref:Bodu.Security.Cryptography.MLKem768>, and <xref:Bodu.Security.Cryptography.MLKem1024>.

## Parameter sets and sizes

| Type | NIST category | Comparable to | Encapsulation key | Decapsulation key | Ciphertext | Shared secret |
|---|---|---|---|---|---|---|
| <xref:Bodu.Security.Cryptography.MLKem512> | 1 | AES-128 | 800 B | 1632 B | 768 B | 32 B |
| <xref:Bodu.Security.Cryptography.MLKem768> | 3 | AES-192 | 1184 B | 2400 B | 1088 B | 32 B |
| <xref:Bodu.Security.Cryptography.MLKem1024> | 5 | AES-256 | 1568 B | 3168 B | 1568 B | 32 B |

The shared secret is always 32 bytes (`MLKem.SharedSecretSizeInBytes`); the private seed accepted by `ImportPrivateSeed` is always 64 bytes (`MLKem.PrivateSeedSizeInBytes`). Each instance also reports its sizes at runtime through `EncapsulationKeySizeInBytes`, `DecapsulationKeySizeInBytes`, and `CiphertextSizeInBytes`.

## When to pick which

- **ML-KEM-768** is the default. It is the parameter set most widely deployed for TLS hybrid key exchange and the right balance of margin and size for almost everyone.
- **ML-KEM-512** trades margin for the smallest keys and ciphertext; choose it only when bandwidth or storage is tight and category-1 security is acceptable.
- **ML-KEM-1024** is the conservative high-margin choice for the most sensitive, longest-lived secrets.

## The encapsulate / decapsulate flow

The receiver generates a key pair and publishes the encapsulation (public) key. The sender imports it, encapsulates, and transmits only the ciphertext. The receiver decapsulates to recover the matching secret.

```csharp
using Bodu.Security.Cryptography;

// Receiver: generate a key pair and publish the encapsulation key.
using var receiver = MLKem768.Create();
receiver.GenerateKey();
byte[] encapsulationKey = receiver.ExportEncapsulationKey();   // 1184 bytes, public

// Sender: import the receiver's public key and encapsulate a fresh secret.
using var sender = MLKem768.Create();
sender.ImportEncapsulationKey(encapsulationKey);
(byte[] ciphertext, byte[] senderSecret) = sender.Encapsulate();

// Sender transmits 'ciphertext' (1088 bytes); 'senderSecret' stays local.

// Receiver: recover the same secret from the ciphertext.
byte[] receiverSecret = receiver.Decapsulate(ciphertext);

// senderSecret and receiverSecret are identical (32 bytes each).
```

As with ECDH, run the 32-byte shared secret through a KDF (HKDF, or <xref:Bodu.Security.Cryptography.Blake2b> in keyed mode) before using it as a symmetric key, binding it to an application-specific context.

## Persisting and restoring keys

Export the decapsulation (private) key, or the compact 64-byte seed, to persist a receiver across runs. Importing the seed regenerates the full key pair; importing an encapsulation key onto an instance discards any decapsulation key it held, leaving an encapsulate-only instance.

```csharp
using Bodu.Security.Cryptography;

using var receiver = MLKem768.Create();
receiver.GenerateKey();

byte[] decapsulationKey = receiver.ExportDecapsulationKey();   // private, store in a vault

// Later, restore a receiver from the encoded decapsulation key:
using var restored = MLKem768.Create();
restored.ImportDecapsulationKey(decapsulationKey);
```

`ImportDecapsulationKey` and `ImportEncapsulationKey` apply the FIPS 203 §7.3 hash-consistency and §7.2 modulus checks respectively, throwing <xref:System.ArgumentException> on a malformed or wrong-length key.

## Implicit rejection — decapsulation does not throw on tampering

A tampered ciphertext of the **correct length** does not throw. Per FIPS 203, `Decapsulate` silently returns an unrelated key (the implicit-rejection value), so an attacker cannot use decapsulation failures as an oracle. Both parties simply end up with different secrets, and the mismatch surfaces later when the symmetric session fails to authenticate. Only a *wrong-length* ciphertext throws <xref:System.ArgumentException>.

This means you must **not** treat a successful `Decapsulate` as proof the ciphertext was genuine — confirm the secret matches by using it in an authenticated channel (an AEAD or a MAC).

## Hybrid with X25519

A KEM resists quantum attack but is newer and less battle-tested than the curve algorithms. The standard mitigation is a **hybrid**: run both [X25519](key-agreement-x25519.md) and ML-KEM, then derive the session key from **both** secrets concatenated. The result is secure as long as *either* primitive holds — quantum-safe from ML-KEM, classically conservative from X25519.

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

// Classic half: X25519 ECDH (each side already exchanged public keys).
using var x = X25519.Create();
x.GenerateKey();
byte[] classicSecret = x.DeriveSharedSecret(peerX25519PublicKey);

// Post-quantum half: ML-KEM encapsulation to the receiver's key.
using var kem = MLKem768.Create();
kem.ImportEncapsulationKey(receiverEncapsulationKey);
(byte[] kemCiphertext, byte[] pqSecret) = kem.Encapsulate();

// Combine both secrets, then derive the session key with a KDF.
byte[] combined = new byte[classicSecret.Length + pqSecret.Length];
classicSecret.CopyTo(combined, 0);
pqSecret.CopyTo(combined, classicSecret.Length);

byte[] sessionKey = HKDF.DeriveKey(
    HashAlgorithmName.SHA256,
    ikm: combined,
    outputLength: 32,
    salt: null,
    info: "myapp v1 hybrid session key"u8.ToArray());

CryptographicOperations.ZeroMemory(classicSecret);
CryptographicOperations.ZeroMemory(pqSecret);
CryptographicOperations.ZeroMemory(combined);
```

The sender transmits both the X25519 public key and the ML-KEM ciphertext; the receiver performs the matching X25519 derivation and ML-KEM decapsulation and runs the identical KDF.

## See also

- [Asymmetric algorithms overview](asymmetric-overview.md) — where ML-KEM sits in the family.
- [Key agreement with X25519](key-agreement-x25519.md) — the classic counterpart and the hybrid partner.
- [ML-DSA post-quantum signatures](ml-dsa.md) — the post-quantum signature companion.
- <xref:Bodu.Security.Cryptography.MLKem>, <xref:Bodu.Security.Cryptography.MLKem512>, <xref:Bodu.Security.Cryptography.MLKem768>, <xref:Bodu.Security.Cryptography.MLKem1024> — API reference.
