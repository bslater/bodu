---
title: Using stream ciphers
---

# Using stream ciphers

A **stream cipher** generates a key- and nonce-dependent keystream and XORs it with the plaintext. Unlike the block ciphers elsewhere in this library, there is no cipher block, no block mode, and no padding: any byte length is encrypted directly. All of Bodu's stream ciphers derive from <xref:Bodu.Security.Cryptography.SymmetricStreamAlgorithm> (itself a <xref:System.Security.Cryptography.SymmetricAlgorithm>), so they flow through `CreateEncryptor()` / `CreateDecryptor()`, a `CryptoStream`, and the `Encrypt` / `Decrypt` extension methods exactly like the block ciphers.

Because the keystream is XORed in, every stream cipher here is **self-inverse**: encryption and decryption are the same operation, and `CreateEncryptor()` and `CreateDecryptor()` are interchangeable. These ciphers derive from `SymmetricStreamAlgorithm`, not `SymmetricAlgorithm`, so the per-message nonce is supplied through the `Nonce` property (generated with `GenerateNonce()`) rather than a block-cipher `IV`.

> [!WARNING]
> These are **raw, confidentiality-only** ciphers — they provide no authentication. A given `(key, nonce)` pair must encrypt **at most one message**: reusing it XORs two keystreams together and reveals the XOR of the plaintexts. For most applications, prefer an **AEAD** construction (see the [AEAD modes guide](aead-modes.md) and [ASCON AEAD](ascon-aead.md)) so that tampering is detected; if you use a raw stream cipher, pair it with a MAC such as [Poly1305](poly1305.md) (encrypt-then-MAC).

## The family at a glance

| Cipher | Key | Nonce / IV | Counter | Lineage |
|---|---|---|---|---|
| <xref:Bodu.Security.Cryptography.ChaCha20> | 256 bits (32 B) | 96 bits (12 B) | 32-bit | Bernstein; RFC 8439 |
| <xref:Bodu.Security.Cryptography.XChaCha20> | 256 bits (32 B) | 192 bits (24 B) | 32-bit | Extended-nonce ChaCha20 (HChaCha20 subkey) |
| <xref:Bodu.Security.Cryptography.Salsa20> | 128 or 256 bits | 64 bits (8 B) | 64-bit | Bernstein; eSTREAM |
| <xref:Bodu.Security.Cryptography.XSalsa20> | 256 bits (32 B) | 192 bits (24 B) | 64-bit | Extended-nonce Salsa20 (HSalsa20 subkey); NaCl |
| <xref:Bodu.Security.Cryptography.Rabbit> | 128 bits (16 B) | 64 bits (8 B) | — (evolving state) | RFC 4503; eSTREAM |
| <xref:Bodu.Security.Cryptography.Hc128> | 128 bits (16 B) | 128 bits (16 B) | — (evolving state) | Wu; eSTREAM |

**Which one?** For new work prefer **ChaCha20** (the de-facto modern standard, RFC 8439) or, when nonces are chosen at random rather than from a counter, **XChaCha20** / **XSalsa20** — their 192-bit nonces are large enough to pick randomly without meaningful collision risk. Salsa20, Rabbit, and HC-128 are provided for interoperability and for completeness of the eSTREAM portfolio.

> [!NOTE]
> A 64-bit nonce (Salsa20, Rabbit) is **too short to choose randomly** without collision risk. Use a strict counter, or prefer an extended-nonce cipher.

## Nonce vs IV — and how to manage it

A stream cipher takes a `Nonce`, not a block-cipher `IV`. The two play the same role — both randomise the keystream so the same key encrypts different messages safely — but the requirements differ. A CBC IV must be *unpredictable*; a stream-cipher nonce only has to be *unique* under the key. The size dictates how you should generate it:

| Nonce width | Safe to pick at random? | Recommended source |
|---|---|---|
| 96-bit (ChaCha20) | Borderline — a counter is safer | A monotonic 96-bit counter, or a sequence number per session |
| 64-bit (Salsa20, Rabbit) | **No** — collisions at ~2³² messages | A strict counter; never `RandomNumberGenerator` |
| 192-bit (XChaCha20, XSalsa20) | **Yes** — collision risk negligible | `GenerateNonce()` / `RandomNumberGenerator` |

The birthday bound is why width matters: random 96-bit nonces reach a ~50 % collision probability near 2⁴⁸ messages, and 64-bit nonces near 2³². A single collision under a fixed key XORs two keystreams together and exposes the XOR of the two plaintexts — there is no recovery. When you cannot guarantee a counter never repeats (distributed senders, restarts that lose state), prefer an extended-nonce cipher and choose the nonce at random, or move to an AEAD that is misuse-resistant.

## Encrypt and decrypt — ChaCha20

```csharp
using System.Diagnostics;
using System.Linq;
using System.Text;
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] plaintext = Encoding.UTF8.GetBytes("message under ChaCha20");

byte[] key, nonce, ciphertext;
using (var alg = new ChaCha20())
{
    alg.GenerateKey();    // 32 bytes (256-bit)
    alg.GenerateNonce();     // 12 bytes (96-bit nonce) — unique per message

    key   = alg.Key;
    nonce = alg.Nonce;

    ciphertext = alg.Encrypt(plaintext);
}

byte[] recovered;
using (var alg = new ChaCha20 { Key = key, Nonce = nonce })
{
    recovered = alg.Decrypt(ciphertext);   // self-inverse — Encrypt would work too
}

Debug.Assert(plaintext.SequenceEqual(recovered));
```

## Streaming with `CryptoStream`

Because a stream cipher imposes no block alignment, it composes naturally with `CryptoStream` for arbitrary-length, chunked data:

```csharp
using System.IO;
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

using var alg = new XChaCha20();
alg.GenerateKey();
alg.GenerateNonce();                       // 24-byte nonce — safe to choose at random

using var output = new MemoryStream();
using (var crypto = new CryptoStream(output, alg.CreateEncryptor(alg.Key, alg.Nonce), CryptoStreamMode.Write))
{
    crypto.Write(firstChunk);
    crypto.Write(secondChunk);          // any lengths; the keystream carries across calls
}

byte[] ciphertext = output.ToArray();
```

## Salsa20 with a 128-bit key

Salsa20 is the only cipher in this family that accepts more than one key size. Set `KeySize` to `128` *before* generating or assigning the key:

```csharp
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

using var alg = new Salsa20 { KeySize = 128 };   // default is 256
alg.GenerateKey();                               // 16 bytes
alg.GenerateNonce();                                // 8-byte nonce

byte[] ciphertext = alg.Encrypt(plaintext);
```

## Choosing the keystream start — `InitialCounter`

ChaCha20, XChaCha20, Salsa20, and XSalsa20 expose an `InitialCounter` that sets the block counter for the first keystream block. The default is `0`; set it to match an external convention (for example, some protocols reserve block 0 for a one-time MAC key and start the message at block 1):

```csharp
using var alg = new ChaCha20 { InitialCounter = 1 };
alg.GenerateKey();
alg.GenerateNonce();

byte[] ciphertext = alg.Encrypt(plaintext);
```

Rabbit and HC-128 have no seekable counter — their keystream comes from an evolving internal state, so a message is always encrypted as one forward sequence.

## A note on initialization cost

HC-128 has a comparatively expensive setup: it warms up two 512-word tables before releasing any keystream. Rabbit's key/IV setup is lighter but still non-trivial. As with Blowfish's key schedule, **do not** build a fresh instance per message under the same key — cache the instance and call `CreateEncryptor()` / `CreateDecryptor()` per message instead.

## Authenticated stream ciphers — Poly1305 AEAD

A raw stream cipher gives you confidentiality but **not** integrity. For authenticated encryption, the library pairs the extended-nonce stream ciphers with Poly1305 in three ready-made constructions. All three derive from the abstract <xref:Bodu.Security.Cryptography.Poly1305AeadTransform> base, take a 256-bit (32-byte) key and a 192-bit (24-byte) extended nonce, produce a 128-bit (16-byte) tag, and emit the wire format `ciphertext ‖ tag`.

| Construction | Stream cipher | Associated data? | Wire / framing |
|---|---|---|---|
| <xref:Bodu.Security.Cryptography.XChaCha20Poly1305> | XChaCha20 | Yes | `ciphertext ‖ tag` (RFC 8439-style framing) |
| <xref:Bodu.Security.Cryptography.XSalsa20Poly1305Aead> | XSalsa20 | Yes | `ciphertext ‖ tag` (RFC 8439-style framing) |
| <xref:Bodu.Security.Cryptography.XSalsa20Poly1305> | XSalsa20 | **No** (NaCl `secretbox`) | `ciphertext ‖ tag`; libsodium layout via converters |

Each instance is **single-use**: create a fresh instance for every message, because reusing a nonce under the same key destroys both confidentiality and authenticity.

### XChaCha20-Poly1305 with associated data

The span-based `Encrypt` / `Decrypt` on the base return the number of bytes written; the convenience `byte[]` overloads (from `Bodu.Security.Cryptography.Extensions`) allocate the result for you:

```csharp
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

using var enc = new XChaCha20Poly1305(key, nonce);
byte[] sealedMsg = enc.Encrypt(plaintext, associatedData: header);   // ciphertext || tag

using var dec = new XChaCha20Poly1305(key, nonce);
byte[] plain = dec.Decrypt(sealedMsg, associatedData: header);       // throws on tamper
```

Decryption verifies the tag (and the associated data) before returning; a modified ciphertext, tag, or header fails authentication and throws rather than returning altered plaintext.

### NaCl `secretbox` — XSalsa20-Poly1305

<xref:Bodu.Security.Cryptography.XSalsa20Poly1305> is the classic NaCl/libsodium `secretbox` construction. It does **not** accept associated data — passing any throws <xref:System.ArgumentException>. When you need associated data with XSalsa20, use `XSalsa20Poly1305Aead` instead:

```csharp
using var box = new XSalsa20Poly1305(key, nonce);
byte[] sealedMsg = box.Encrypt(plaintext);   // ciphertext || tag, no associated data
```

This library emits `ciphertext ‖ tag`, whereas libsodium's combined `secretbox` places the tag first (`tag ‖ ciphertext`). Convert between the two layouts with the static helpers when interoperating:

```csharp
XSalsa20Poly1305.ToLibsodiumCombined(ciphertextThenTag, tagThenCiphertext);
XSalsa20Poly1305.FromLibsodiumCombined(tagThenCiphertext, ciphertextThenTag);
```

### In-place operation

The Poly1305 AEAD transforms support exact in-place use — the output span may begin at the same location as the input. Any other partial overlap is rejected with <xref:System.ArgumentException>.

## Where to go next

- [Encryption basics](encryption-basics.md) — the Key/IV lifecycle shared with the block ciphers.
- [Using Poly1305](poly1305.md) — pair a raw stream cipher with a one-time authenticator (encrypt-then-MAC).
- [AEAD modes](aead-modes.md) and [ASCON AEAD](ascon-aead.md) — authenticated encryption, the recommended default when you need integrity as well as confidentiality.
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
