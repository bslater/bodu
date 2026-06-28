---
title: Key agreement with X25519
---

# Key agreement with X25519

<xref:Bodu.Security.Cryptography.X25519> is the elliptic-curve Diffie-Hellman (ECDH) key-agreement function of RFC 7748, performing scalar multiplication on the Montgomery form of Curve25519. Two parties each generate a key pair, exchange public keys, and independently arrive at the **same** 32-byte shared secret without ever transmitting it. This guide is for developers establishing a shared secret to bootstrap a symmetric session.

X25519 is **key agreement only** — it produces no ciphertext and no signature. To sign messages, see [Ed25519](signatures-ed25519.md); for a quantum-resistant alternative to this exchange, see [ML-KEM](ml-kem.md).

## Fixed sizes at a glance

| Parameter | Size | Constant |
|---|---|---|
| Private key | 32 bytes | `X25519.KeySizeInBytes` |
| Public key | 32 bytes | `X25519.KeySizeInBytes` |
| Shared secret | 32 bytes | `X25519.SharedSecretSizeInBytes` |
| Security level | 128 bits | — |

Both key halves are 32 bytes; the public key is the little-endian u-coordinate of the scalar multiple of the base point.

## The two-party exchange

Each party calls <xref:Bodu.Security.Cryptography.X25519.GenerateKey>, sends its public key over the (untrusted) wire, and calls <xref:Bodu.Security.Cryptography.X25519.DeriveSharedSecret(System.ReadOnlySpan{System.Byte})> with the *peer's* public key. The two derivations produce identical bytes. A span overload, `DeriveSharedSecret(peerPublicKey, destination)`, writes the 32-byte secret into a caller-supplied buffer without allocating — and zeroes that buffer if the peer point is rejected (below).

```csharp
using Bodu.Security.Cryptography;

using var alice = X25519.Create();
using var bob = X25519.Create();
alice.GenerateKey();
bob.GenerateKey();

// Each side exports its public key and sends it to the other.
byte[] alicePublic = alice.ExportPublicKey();   // 32 bytes
byte[] bobPublic = bob.ExportPublicKey();       // 32 bytes

// Each side derives the secret from the peer's public key.
byte[] aliceShared = alice.DeriveSharedSecret(bobPublic);
byte[] bobShared = bob.DeriveSharedSecret(alicePublic);

// aliceShared and bobShared are identical (32 bytes each).
```

## Importing a peer's public key

In practice you receive the peer's public key as raw bytes. There is no need to construct a peer `X25519` instance: `DeriveSharedSecret` accepts the 32-byte public key directly.

```csharp
using Bodu.Security.Cryptography;

byte[] peerPublic = ReceivePeerPublicKey();   // 32 bytes off the wire

using var local = X25519.Create();
local.GenerateKey();

byte[] shared = local.DeriveSharedSecret(peerPublic);
```

If you do hold the peer key as a separate instance — for example to keep it pinned — import it with <xref:Bodu.Security.Cryptography.X25519.ImportPublicKey(System.ReadOnlySpan{System.Byte})>. Importing a public key onto an instance discards any private key it held, leaving a public-only instance (`HasPublicKey` true, `HasPrivateKey` false) that can export but cannot derive — calling `DeriveSharedSecret` on it throws <xref:System.Security.Cryptography.CryptographicException>.

```csharp
using var peer = X25519.Create();
peer.ImportPublicKey(peerPublic);
byte[] pinned = peer.ExportPublicKey();   // round-trips byte-for-byte
```

To persist your own key pair across runs, export the private key and re-import it later:

```csharp
byte[] storedPrivate = local.ExportPrivateKey();   // keep secret

using var restored = X25519.Create();
restored.ImportPrivateKey(storedPrivate);          // public key is re-derived
```

## Derive the secret, then run a KDF — do not use it directly

The shared secret is a **raw curve point coordinate**, not uniform key material. Never use it directly as an AES or ChaCha20 key. Pass it through a key derivation function (KDF) — such as [HKDF](hkdf.md), or <xref:Bodu.Security.Cryptography.Blake2b> in keyed mode, or a memory-hard KDF like [Argon2](argon2.md) — to produce a uniformly random, context-bound symmetric key.

A salt and an application-specific `info` / context string bind the derived key to its purpose and prevent the same secret from yielding the same key in two unrelated contexts.

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

using var alice = X25519.Create();
using var bob = X25519.Create();
alice.GenerateKey();
bob.GenerateKey();

byte[] shared = alice.DeriveSharedSecret(bob.ExportPublicKey());

// Stretch the raw secret into a 32-byte session key with HKDF-SHA256.
byte[] sessionKey = Hkdf.DeriveKey(
    HashAlgorithmName.SHA256,
    inputKeyingMaterial: shared,
    outputLength: 32,
    salt: default,
    info: "myapp v1 session key"u8);

// 'sessionKey' is now safe to use with a symmetric AEAD (e.g. ChaCha20-Poly1305).
CryptographicOperations.ZeroMemory(shared);   // wipe the raw secret once stretched
```

## Low-order point rejection

A small set of low-order peer public keys force the shared secret to an all-zero value that an observer can predict without knowing the private key. `DeriveSharedSecret` applies the RFC 7748 §6.1 strict check and throws <xref:System.Security.Cryptography.CryptographicException> rather than returning attacker-predictable key material. You do not need to add your own check; just be prepared for the exception on hostile input.

## What X25519 is not

- **Not a signature scheme.** It proves nothing about *who* you agreed a key with. Authenticate the exchange (e.g. sign the public keys with [Ed25519](signatures-ed25519.md)) to prevent a man-in-the-middle.
- **Not quantum-resistant.** A future quantum computer breaks the discrete-log problem X25519 rests on. For long-lived secrets, combine it with [ML-KEM](ml-kem.md#hybrid-with-x25519).
- **Not a symmetric key as-is.** The raw secret must go through a KDF before use, as shown above.

## See also

- [Asymmetric algorithms overview](asymmetric-overview.md) — where X25519 sits in the family.
- [Signatures with Ed25519](signatures-ed25519.md) — authenticate the exchange.
- [ML-KEM post-quantum key encapsulation](ml-kem.md) — the post-quantum replacement and the hybrid pattern.
- [Using HKDF](hkdf.md) — the extract-and-expand KDF that turns the raw secret into usable key material.
- <xref:Bodu.Security.Cryptography.X25519>, <xref:Bodu.Security.Cryptography.Hkdf> — API reference.
