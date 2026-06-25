---
title: Using HKDF
---

# Using HKDF

<xref:Bodu.Security.Cryptography.Hkdf> is the HMAC-based Extract-and-Expand Key Derivation Function of RFC 5869. It turns input keying material that is merely *high-entropy* — a Diffie-Hellman shared secret, a KEM output, a master key — into one or more cryptographically strong, fixed-length keys, each bound to an application-specific context. Reach for it whenever you have a strong but non-uniform secret and need usable symmetric key material; for *low*-entropy inputs such as passwords, use a memory-hard KDF instead ([Argon2](argon2.md) or [scrypt](scrypt.md)).

> [!NOTE]
> The platform already ships <xref:System.Security.Cryptography.HKDF?displayProperty=nameWithType>, and its surface is interchangeable with this type — prefer the BCL implementation where it covers your need. `Bodu`'s `Hkdf` exists to give the rest of the library a self-contained HKDF (it backs the HPKE labeled KDF). It is not independently audited and offers best-effort, not guaranteed, side-channel resistance.

## The two stages

HKDF is two steps that you can call separately or together:

| Stage | Method | Purpose |
|---|---|---|
| Extract | <xref:Bodu.Security.Cryptography.Hkdf.Extract(System.Security.Cryptography.HashAlgorithmName,System.ReadOnlySpan{System.Byte},System.ReadOnlySpan{System.Byte})> | Condense the input keying material (with an optional salt) into a pseudorandom key (PRK) of one hash length. |
| Expand | <xref:Bodu.Security.Cryptography.Hkdf.Expand(System.Security.Cryptography.HashAlgorithmName,System.ReadOnlySpan{System.Byte},System.Int32,System.ReadOnlySpan{System.Byte})> | Stretch the PRK into output keying material of any length, bound to an optional `info` context. |
| Both | <xref:Bodu.Security.Cryptography.Hkdf.DeriveKey(System.Security.Cryptography.HashAlgorithmName,System.ReadOnlySpan{System.Byte},System.Int32,System.ReadOnlySpan{System.Byte},System.ReadOnlySpan{System.Byte})> | Extract then Expand in one call — the common case. |

The supported hash algorithms are SHA-1, SHA-256, SHA-384, and SHA-512. The maximum output of a single Expand is `255 × HashLen` bytes.

## Pattern 1 — derive a key in one call

`DeriveKey` is the method you want most of the time:

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

byte[] sharedSecret = GetSharedSecret();   // high-entropy, but not uniform

byte[] sessionKey = Hkdf.DeriveKey(
    HashAlgorithmName.SHA256,
    inputKeyingMaterial: sharedSecret,
    outputLength: 32,
    salt: salt,                            // optional, non-secret; binds the derivation
    info: "myapp v1 traffic key"u8);       // optional context / label

CryptographicOperations.ZeroMemory(sharedSecret);   // wipe the raw secret once stretched
```

The `salt` and `info` matter: a salt strengthens extraction when the input is structured, and a distinct `info` ensures the same secret yields *different* keys for different purposes (a key, a nonce, a MAC key) without re-running the agreement.

## Pattern 2 — extract once, expand many

When you derive several independent keys from one secret, extract a single PRK and expand it repeatedly with different `info` labels:

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

byte[] prk = Hkdf.Extract(HashAlgorithmName.SHA256, ikm: sharedSecret, salt: salt);

byte[] encryptionKey = Hkdf.Expand(HashAlgorithmName.SHA256, prk, 32, info: "encrypt"u8);
byte[] macKey        = Hkdf.Expand(HashAlgorithmName.SHA256, prk, 32, info: "mac"u8);

CryptographicOperations.ZeroMemory(prk);
```

## Pattern 3 — span destinations (allocation-free)

Every operation has an overload that writes into a caller-supplied span, so a key can be derived into a pooled or stack buffer without an intermediate array:

```csharp
Span<byte> key = stackalloc byte[32];
Hkdf.DeriveKey(HashAlgorithmName.SHA256, sharedSecret, key, salt, info: "traffic"u8);
// ... use key ...
key.Clear();
```

## What HKDF is not

- **Not a password hash.** HKDF is fast by design. Never feed it a password or PIN — use [Argon2id](argon2.md) or [scrypt](scrypt.md), which are deliberately slow and memory-hard.
- **Not an authenticator.** HKDF derives keys; it does not protect integrity. Authenticate with an AEAD or a MAC.
- **Not a source of entropy.** It cannot manufacture randomness — the security of its output is bounded by the entropy of the input keying material.

## Where to go next

- [Key agreement with X25519](key-agreement-x25519.md) — the canonical source of a shared secret to feed into HKDF.
- [Hybrid public key encryption with HPKE](hpke.md) — builds the labeled HKDF of RFC 9180 on top of this primitive.
- [Using Argon2](argon2.md) / [Using scrypt](scrypt.md) — KDFs for low-entropy (password) inputs.
- <xref:Bodu.Security.Cryptography.Hkdf> — API reference.
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic.
