---
title: Authenticated stream ciphers
---

# Authenticated stream ciphers

A raw stream cipher gives confidentiality only. `Bodu.Security.Cryptography` pairs the extended-nonce stream ciphers with Poly1305 in three ready-made AEAD constructions, all built on the abstract <xref:Bodu.Security.Cryptography.Poly1305AeadTransform> and all implementing <xref:Bodu.Security.Cryptography.IStreamAeadTransform>. They share one shape: a 256-bit (32-byte) key, a 192-bit (24-byte) nonce that is large enough to draw at random, a 128-bit (16-byte) tag, and the wire format `ciphertext ‖ tag`.

> [!NOTE]
> Like the rest of the library, these constructions are not independently audited and offer best-effort, not guaranteed, side-channel resistance.

## The three constructions

| Construction | Type | Keystream | Framing | Associated data | Interoperates with |
|---|---|---|---|---|---|
| XChaCha20-Poly1305 | <xref:Bodu.Security.Cryptography.XChaCha20Poly1305> | XChaCha20 (HChaCha20 subkey + ChaCha20) | RFC 8439 AEAD | Yes | `draft-irtf-cfrg-xchacha`; libsodium `crypto_aead_xchacha20poly1305_ietf` |
| XSalsa20-Poly1305 (secretbox) | <xref:Bodu.Security.Cryptography.XSalsa20Poly1305> | XSalsa20 (HSalsa20 subkey + Salsa20) | NaCl secretbox — tag over the ciphertext only | **No** — non-empty AAD throws `ArgumentException` | NaCl / libsodium `crypto_secretbox` (same bytes, different order — see Pattern 3) |
| XSalsa20-Poly1305-AEAD | <xref:Bodu.Security.Cryptography.XSalsa20Poly1305Aead> | XSalsa20 | RFC 8439 AEAD | Yes | **Nothing** — a Bodu-defined hybrid; use only when both peers are Bodu |

In every case the counter-0 keystream block supplies the one-time Poly1305 key and the message is encrypted from counter 1 (XChaCha20, XSalsa20-AEAD) or from byte 32 of the keystream (secretbox). The RFC 8439 framing authenticates `AAD ‖ pad16(AAD) ‖ ciphertext ‖ pad16(ciphertext) ‖ le64(|AAD|) ‖ le64(|ciphertext|)`.

**Which one?** `XChaCha20Poly1305` is the interoperable, random-nonce AEAD — the gap the BCL's 96-bit-nonce `ChaCha20Poly1305` leaves ([BCL interop](bcl-interop.md#pattern-6--xchacha20poly1305-is-not-chacha20poly1305)). `XSalsa20Poly1305` exists to talk to NaCl secretbox. `XSalsa20Poly1305Aead` exists for symmetry; prefer the other two unless a protocol names it.

## Pattern 1 — XChaCha20-Poly1305 with associated data

This is the `draft-irtf-cfrg-xchacha` appendix A.3.1 vector; the tag reproduces the draft's `c0875924c1c7987947deafd8780acf49`.

```csharp
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] key   = Convert.FromHexString("808182838485868788898a8b8c8d8e8f909192939495969798999a9b9c9d9e9f");
byte[] nonce = Convert.FromHexString("404142434445464748494a4b4c4d4e4f5051525354555657");
byte[] aad   = Convert.FromHexString("50515253c0c1c2c3c4c5c6c7");
byte[] plaintext = "Ladies and Gentlemen of the class of '99: If I could offer you only one tip for the future, sunscreen would be it."u8.ToArray();

using var enc = new XChaCha20Poly1305(key, nonce);
byte[] sealed_ = enc.Encrypt(plaintext, associatedData: aad);        // ciphertext ‖ tag: BD6D179D…  ‖ C0875924C1C7987947DEAFD8780ACF49

using var dec = new XChaCha20Poly1305(key, nonce);
byte[] recovered = dec.Decrypt(sealed_, associatedData: aad);        // throws CryptographicException on any tamper
```

`Encrypt` / `Decrypt` come from <xref:Bodu.Security.Cryptography.Extensions.AeadTransformExtensions>, which size and allocate the array; `TagSize` is 128 bits, and `XChaCha20Poly1305.KeySize` / `NonceSize` are 256 / 192.

> [!WARNING]
> Name the `associatedData:` argument. `Poly1305AeadTransform` also has a public `Encrypt(ReadOnlySpan<byte> plaintext, Span<byte> output, ReadOnlySpan<byte> associatedData = default)`; with two positional `byte[]` arguments that overload wins, your AAD array becomes the **output buffer**, and the call returns an `int`. `Encrypt(plaintext)` with a single argument is unambiguous.

## Pattern 2 — single use, and what happens on tamper

Every instance is stateful and single-use. A second `Encrypt` or `Decrypt` on the same instance throws `InvalidOperationException` — including after a failed tag check, so a "retry on the same transform" is impossible by construction. A modified ciphertext, tag, or AAD throws `CryptographicException` and writes no plaintext.

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] key = new byte[32], nonce = new byte[24];
byte[] plaintext = "single use"u8.ToArray();

using var enc = new XChaCha20Poly1305(key, nonce);
byte[] sealed_ = enc.Encrypt(plaintext);
try { enc.Encrypt(plaintext); } catch (InvalidOperationException) { /* one message per instance */ }

sealed_[0] ^= 0x01;
using var dec = new XChaCha20Poly1305(key, nonce);
try { dec.Decrypt(sealed_); } catch (CryptographicException) { /* tag did not verify */ }
try { dec.Decrypt(sealed_); } catch (InvalidOperationException) { /* the instance is burned */ }
```

> [!WARNING]
> A `(key, nonce)` pair must encrypt at most one message. The 24-byte nonce is large enough to draw from `RandomNumberGenerator` (or <xref:Bodu.Security.Cryptography.Nonce.Random(System.Int32)>) per message without meaningful collision risk; that is the whole point of the extended nonce. Do not reuse a key across these constructions and the raw `XChaCha20` / `XSalsa20` ciphers without an HKDF-style key separation.

## Pattern 3 — NaCl secretbox and the libsodium layout

<xref:Bodu.Security.Cryptography.XSalsa20Poly1305> reproduces the secretbox body byte for byte, but emits `ciphertext ‖ tag` like the rest of the library where libsodium's `crypto_secretbox_easy` emits `tag ‖ ciphertext`. Two static helpers swap the order at the boundary; both accept an aliased destination for in-place conversion.

```csharp
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] key   = Convert.FromHexString("808182838485868788898a8b8c8d8e8f909192939495969798999a9b9c9d9e9f");
byte[] nonce = Convert.FromHexString("404142434445464748494a4b4c4d4e4f5051525354555657");
byte[] plaintext = "Ladies and Gentlemen of the class of '99: If I could offer you only one tip for the future, sunscreen would be it."u8.ToArray();

using var box = new XSalsa20Poly1305(key, nonce);
byte[] sealed_ = box.Encrypt(plaintext);                     // ciphertext ‖ tag; tag = 838EBCFC76D4B1D823B89BB6E0FC0520

byte[] libsodium = new byte[sealed_.Length];
XSalsa20Poly1305.ToLibsodiumCombined(sealed_, libsodium);   // tag ‖ ciphertext — what crypto_secretbox_easy produces

byte[] back = new byte[libsodium.Length];
XSalsa20Poly1305.FromLibsodiumCombined(libsodium, back);    // ciphertext ‖ tag again

using var open = new XSalsa20Poly1305(key, nonce);
byte[] recovered = open.Decrypt(back);
```

Secretbox authenticates no associated data: `Encrypt(plaintext, associatedData: header)` with a non-empty header throws `ArgumentException` (`associatedData`). When you need AAD with an XSalsa20 keystream use `XSalsa20Poly1305Aead`, whose call shape is identical to Pattern 1 but whose output no other library will open.

## Pattern 4 — programming against the interface

All three types are <xref:Bodu.Security.Cryptography.IStreamAeadTransform>, and through it <xref:Bodu.Security.Cryptography.IAeadTransform> — the same interface the block-cipher AEADs and `AsconAead128` implement — so one code path can serve any of them. The span-based members return the number of bytes written; the output must be at least `plaintext.Length + TagSize / 8`.

```csharp
using Bodu.Security.Cryptography;

byte[] key = new byte[32], nonce = new byte[24], aad = "hdr"u8.ToArray();
byte[] plaintext = "polymorphic"u8.ToArray();

foreach (Func<IStreamAeadTransform> make in new Func<IStreamAeadTransform>[]
{
    () => new XChaCha20Poly1305(key, nonce),
    () => new XSalsa20Poly1305Aead(key, nonce),
})
{
    using IStreamAeadTransform enc = make();
    byte[] output = new byte[plaintext.Length + enc.TagSize / 8];
    int written = enc.Encrypt(plaintext, output, aad);

    using IStreamAeadTransform dec = make();
    byte[] plain = new byte[written - enc.TagSize / 8];
    int plainLength = dec.Decrypt(output.AsSpan(0, written), plain, aad);
}
```

Exact in-place operation is supported — the output span may start at the same address as the input — but any other overlap throws `ArgumentException`:

```csharp
using Bodu.Security.Cryptography;

byte[] key = new byte[32], nonce = new byte[24];
byte[] plaintext = "in place"u8.ToArray();
byte[] buffer = new byte[plaintext.Length + 16];
plaintext.CopyTo(buffer, 0);

using var enc = new XChaCha20Poly1305(key, nonce);
int written = enc.Encrypt(buffer.AsSpan(0, plaintext.Length), buffer);          // same start: allowed

using var dec = new XChaCha20Poly1305(key, nonce);
int plainLength = dec.Decrypt(buffer.AsSpan(0, written), buffer);
```

## Pattern 5 — detached tags

`EncryptDetached` / `DecryptDetached` — the overloads that return an <xref:Bodu.Security.Cryptography.AuthenticationTag> separately from the ciphertext — are declared on <xref:Bodu.Security.Cryptography.Extensions.AeadBlockCipherModeTransformExtensions> for <xref:Bodu.Security.Cryptography.IAeadBlockCipherModeTransform> only. The stream AEADs do not have them; because their layout is always `ciphertext ‖ tag`, detaching is a slice:

```csharp
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] key = new byte[32], nonce = new byte[24], aad = "hdr"u8.ToArray();
byte[] plaintext = "detached"u8.ToArray();

using var enc = new XChaCha20Poly1305(key, nonce);
byte[] combined = enc.Encrypt(plaintext, associatedData: aad);

int tagBytes = enc.TagSize / 8;
byte[] ciphertext = combined[..^tagBytes];
AuthenticationTag tag = AuthenticationTag.FromBytes(combined.AsSpan(^tagBytes));   // travels out of band

using var dec = new XChaCha20Poly1305(key, nonce);
byte[] recovered = dec.Decrypt([.. ciphertext, .. tag.AsSpan()], associatedData: aad);
```

See [Nonces, salts, tags, and secrets](value-types.md) for `AuthenticationTag` and the block-cipher `EncryptDetached` shape.

## Building your own

<xref:Bodu.Security.Cryptography.Poly1305AeadTransform> is abstract and public. A subclass supplies `CreateEngine()` — an <xref:Bodu.Security.Cryptography.IStreamCipher> positioned at block counter 0 — and may override `SealCore` / `OpenCore` to substitute a framing (that is how the secretbox variant is built) or `SupportsAssociatedData` to reject AAD. The base handles length validation, the overlap rule, the single-use latch, and zeroizing the retained key and nonce on `Dispose`. The library's own keystream engines are internal, so a subclass brings its own `IStreamCipher`.

## API summary

| Member | Where | Notes |
|---|---|---|
| `Encrypt(plaintext, output, associatedData = default)` / `Decrypt(…)` → `int` | `Poly1305AeadTransform` (`IAeadTransform`) | span form; output ≥ input + 16 / input − 16 |
| `Encrypt(plaintext, associatedData = default)` / `Decrypt(…)` → `byte[]` | <xref:Bodu.Security.Cryptography.Extensions.AeadTransformExtensions> | name `associatedData:` |
| `TagSize` | `IAeadTransform` | 128 bits |
| `KeySize` / `NonceSize` constants | each concrete type | 256 / 192 bits |
| `ToLibsodiumCombined` / `FromLibsodiumCombined` | `XSalsa20Poly1305` (static) | swap `ciphertext ‖ tag` ↔ `tag ‖ ciphertext` |
| `CreateEngine`, `SealCore`, `OpenCore`, `SupportsAssociatedData` | `Poly1305AeadTransform` (protected) | extension points |
| `Dispose` | all | zeroes the retained key and nonce |

## Where to go next

- [Using stream ciphers](stream-ciphers.md) — the raw, unauthenticated keystream ciphers these are built on.
- [Using Poly1305](poly1305.md) — the one-time authenticator on its own.
- [AEAD modes](aead-modes.md) and [ASCON AEAD](ascon-aead.md) — the block-cipher and sponge alternatives.
- [Interoperating with System.Security.Cryptography](bcl-interop.md) — why this is not the BCL `ChaCha20Poly1305`.
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
