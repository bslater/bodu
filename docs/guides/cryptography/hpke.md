---
title: Hybrid public key encryption with HPKE
---

# Hybrid public key encryption with HPKE

<xref:Bodu.Security.Cryptography.Hpke> implements Hybrid Public Key Encryption as standardized in RFC 9180 — the modern, vetted answer to "encrypt this payload so only the holder of a given public key can read it." It replaces ad-hoc ECIES-style constructions and is the encryption layer beneath TLS Encrypted Client Hello, Message Layer Security (MLS), and Oblivious HTTP.

HPKE composes three primitives you have already met in this library: a **KEM** establishes a fresh shared secret against the recipient's public key, a **KDF** ([HKDF](hkdf.md)) derives an AEAD key and nonce from it, and an **AEAD** encrypts the payload. You never manage those steps by hand — you pick a suite and call `Seal` / `Open`.

> [!NOTE]
> This implementation supports the `DHKEM(X25519, HKDF-SHA256)` KEM (it builds on <xref:Bodu.Security.Cryptography.X25519>). Like the rest of the library it offers best-effort side-channel resistance and has not been independently audited.

## Choosing a suite

A <xref:Bodu.Security.Cryptography.HpkeSuite> fixes the KEM, the KDF, and the AEAD. Three combinations are pre-configured as static properties; construct any other directly.

| Suite | AEAD | Key | Use |
|---|---|---|---|
| <xref:Bodu.Security.Cryptography.HpkeSuite.X25519_HkdfSha256_Aes128Gcm> | AES-128-GCM | 16 B | Hardware-accelerated default on x86/ARM. |
| <xref:Bodu.Security.Cryptography.HpkeSuite.X25519_HkdfSha256_Aes256Gcm> | AES-256-GCM | 32 B | 256-bit key margin. |
| <xref:Bodu.Security.Cryptography.HpkeSuite.X25519_HkdfSha256_ChaCha20Poly1305> | ChaCha20Poly1305 | 32 B | Fast in software / constant-time without AES-NI. |

The KEM always uses `DHKEM(X25519, HKDF-SHA256)` — 32-byte public keys and a 32-byte encapsulated key. The suite's *KDF* is independent of the KEM's internal HKDF and selects the key schedule's hash: `HpkeKdf.HkdfSha256` (the default in all three pre-configured suites), `HkdfSha384`, or `HkdfSha512`. Constructing a suite with any other KEM, KDF, or AEAD value throws <xref:System.ArgumentOutOfRangeException>.

A suite is immutable and exposes the lengths RFC 9180 derives from its choices — `AeadKeySizeInBytes` (`Nk`), `AeadNonceSizeInBytes` (`Nn`), `AeadTagSizeInBytes` (`Nt`), and `EncapsulationSizeInBytes` (`Nenc`) — so you can size buffers without hard-coding constants. An export-only suite (`HpkeAead.ExportOnly`) reports zero for the AEAD lengths, derives secrets through `Export`, and refuses to seal or open.

## The single-shot façade

For one message, <xref:Bodu.Security.Cryptography.Hpke> does the whole exchange in one call. The sender produces an **encapsulated key** (`enc`) alongside the ciphertext; the recipient needs both, plus the same `info`.

```csharp
using Bodu.Security.Cryptography;

using var recipient = X25519.Create();
recipient.GenerateKey();
byte[] recipientPublicKey = recipient.ExportPublicKey();   // published, 32 bytes

HpkeSuite suite = HpkeSuite.X25519_HkdfSha256_Aes128Gcm;
byte[] info = "myapp v1"u8.ToArray();   // binds the exchange to a context
byte[] aad  = "headers"u8.ToArray();    // authenticated, not encrypted

// Sender — needs only the recipient's public key.
var (enc, ciphertext) = Hpke.Seal(suite, recipientPublicKey, info, aad, "secret message"u8);

// Recipient — needs its private key, the encapsulated key, and the same info/aad.
byte[] plaintext = Hpke.Open(suite, recipient, enc, info, aad, ciphertext);
```

`info` and `aad` must match on both sides or `Open` throws <xref:System.Security.Cryptography.CryptographicException>. The recipient private key is supplied as an <xref:Bodu.Security.Cryptography.X25519> instance so you control its lifetime; the recipient public key and `enc` travel as raw 32-byte spans.

## Sealing many messages — sender and receiver sessions

To send a stream of messages under one encapsulation, set up a <xref:Bodu.Security.Cryptography.HpkeSender> once and call `Seal` repeatedly. Each call advances an internal sequence number that derives a fresh per-message nonce, so the matching <xref:Bodu.Security.Cryptography.HpkeReceiver> must `Open` the messages **in the same order**.

```csharp
using Bodu.Security.Cryptography;

using var recipient = X25519.Create();
recipient.GenerateKey();

// Sender side.
using HpkeSender sender = HpkeSender.SetupBase(suite, recipient.ExportPublicKey(), info, out byte[] enc);
byte[] c0 = sender.Seal(aad, "message zero"u8);
byte[] c1 = sender.Seal(aad, "message one"u8);

// Receiver side — opens in sequence.
using HpkeReceiver receiver = HpkeReceiver.SetupBase(suite, recipient, enc, info);
byte[] m0 = receiver.Open(aad, c0);
byte[] m1 = receiver.Open(aad, c1);
```

Both session types are `IDisposable` and zero their derived key material on dispose — always wrap them in `using`.

## The four modes

HPKE offers four establishment modes (RFC 9180 §5.1). Each has its own `Setup*` / single-shot pair, so the inputs a mode requires are always explicit.

| Mode | Adds | Sender setup | Receiver setup |
|---|---|---|---|
| **Base** | nothing | `SetupBase` | `SetupBase` |
| **PSK** | a pre-shared key both parties hold | `SetupPsk` | `SetupPsk` |
| **Auth** | sender authentication via the sender's static key | `SetupAuth` | `SetupAuth` |
| **AuthPSK** | both of the above | `SetupAuthPsk` | `SetupAuthPsk` |

In the **auth** modes the sender proves possession of a static private key; the receiver verifies it with the sender's public key:

```csharp
using var senderKey = X25519.Create();
senderKey.GenerateKey();

using HpkeSender sender = HpkeSender.SetupAuth(suite, recipient.ExportPublicKey(), info, senderKey, out byte[] enc);
byte[] ciphertext = sender.Seal(aad, "authenticated payload"u8);

using HpkeReceiver receiver = HpkeReceiver.SetupAuth(suite, recipient, enc, info, senderKey.ExportPublicKey());
byte[] plaintext = receiver.Open(aad, ciphertext);   // fails unless it was sealed by senderKey
```

The **PSK** modes mix in a symmetric secret shared out of band, identified by a `pskId`; the pre-shared key and its identifier must both be present (or both absent), and must match the mode, or setup throws <xref:System.Security.Cryptography.CryptographicException>.

## Exporting secrets

Beyond encryption, every context can derive independent secrets bound to a label, via `Export` — the basis of protocols like Oblivious HTTP. The sender and receiver derive identical bytes for the same context and length:

```csharp
byte[] senderSecret   = sender.Export("confirmation"u8, 32);
byte[] receiverSecret = receiver.Export("confirmation"u8, 32);
// senderSecret and receiverSecret are identical.
```

An **export-only** suite (`new HpkeSuite(HpkeKem.X25519HkdfSha256, HpkeKdf.HkdfSha256, HpkeAead.ExportOnly)`) supports `Export` but throws <xref:System.NotSupportedException> from `Seal` / `Open`.

## What HPKE is not

- **Not a transport / session protocol.** It encrypts discrete messages; it has no handshake, key rotation, or replay window of its own. Build those above it.
- **Not sender-authenticated by default.** Base and PSK modes do not prove *who* sealed a message — use an auth mode (or sign separately) when origin matters.
- **Not nonce-managed by you.** The per-message nonce is derived from the sequence counter; never reuse a context to "re-seal" the same sequence number, and keep sender and receiver in step.

## Where to go next

- [Key agreement with X25519](key-agreement-x25519.md) — the KEM HPKE builds on.
- [Using HKDF](hkdf.md) — the key-derivation stage inside HPKE's key schedule.
- [Asymmetric algorithms overview](asymmetric-overview.md) — where HPKE sits in the public-key family.
- <xref:Bodu.Security.Cryptography.Hpke>, <xref:Bodu.Security.Cryptography.HpkeSuite>, <xref:Bodu.Security.Cryptography.HpkeMode> (`Base` / `Psk` / `Auth` / `AuthPsk`), <xref:Bodu.Security.Cryptography.HpkeSender>, <xref:Bodu.Security.Cryptography.HpkeReceiver> — API reference.
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic.
