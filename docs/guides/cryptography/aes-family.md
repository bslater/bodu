---
title: AES-family block ciphers
---

# AES-family block ciphers

`Bodu.Security.Cryptography` ships four managed block ciphers in the "AES family" — block size 128 bits, key sizes 128 / 192 / 256 bits, and broadly interchangeable design constraints. They cover the gap between what the BCL ships (AES only) and the design diversity needed to pick a cipher based on threat model, performance, or interoperability requirements.

This guide compares the four side by side. For block-cipher modes (ECB, CBC, CTR, CFB, OFB, CTS, XTS) and how they wrap any of these ciphers, see [Cipher block modes](cipher-modes.md). For padding schemes, see [Padding](padding.md). For AEAD modes that compose a cipher with authentication, see [AEAD modes](aead-modes.md).

For the historical and legacy ciphers — Blowfish, Skipjack — see their own guides ([Blowfish](blowfish.md), [Skipjack](skipjack.md)). They are kept for compatibility with deployed systems, not for new designs.

## At a glance

| Cipher | Block | Key sizes | Rounds | Notes |
|---|---|---|---|---|
| **AES** (BCL adapter) | 128 bits | 16 / 24 / 32 bytes | 10 / 12 / 14 | Hardware-accelerated via the BCL on AES-NI capable CPUs. The default modern choice. |
| **Twofish** | 128 bits | 16 / 24 / 32 bytes | 16 | AES finalist (Schneier et al., 1998). Key-dependent S-boxes, strong software performance. |
| **Camellia** | 128 bits | 16 / 24 / 32 bytes | 18 / 24 | ISO/IEC 18033-3, RFC 3713, CRYPTREC, NESSIE. Wide approval; popular outside the US. |
| **Serpent-128** | 128 bits | 16 / 24 / 32 bytes | 32 | AES finalist (Anderson / Biham / Knudsen). The most conservative design — twice the rounds of AES; slower but with the largest security margin. |

All four implement the BCL `SymmetricAlgorithm` or `IBlockCipher` contract, so they slot into the standard cipher-mode + padding pipeline without bespoke wrappers.

> [!NOTE]
> **Two surface shapes in this family.** <xref:Bodu.Security.Cryptography.Twofish> and <xref:Bodu.Security.Cryptography.Serpent128> are full `SymmetricAlgorithm` wrappers — set `BlockMode` / `BlockPadding`, call `CreateEncryptor()`, or use the `Encrypt` / `Decrypt` extensions. <xref:Bodu.Security.Cryptography.AesBlockCipher> and `CamelliaBlockCipher` are raw <xref:Bodu.Security.Cryptography.IBlockCipher> primitives — one block in, one block out — so you compose them with a mode transform and padding strategy yourself (or via <xref:Bodu.Security.Cryptography.BlockCipherModeFactory>), as shown in [Composing primitives](composing-primitives.md). All four share the 128-bit block, so all four can drive the AEAD transforms.

The shared 128-bit block is also why this family does **not** suffer the SWEET32 birthday-bound exposure that limits the 64-bit-block legacy ciphers ([Blowfish](blowfish.md), [Skipjack](skipjack.md)): a 128-bit block pushes the birthday bound to ~2⁶⁴ blocks, far beyond any practical workload under a single key.

## When to pick which

- **Pick AES** unless you have a specific reason to deviate. It is the standard, has hardware acceleration on commodity CPUs, and is the cipher every interoperator already speaks.
- **Pick Twofish** when you want an AES-finalist alternative that performs well in pure software (no AES-NI), or when the design diversity matters to your threat model.
- **Pick Camellia** for interoperability with systems standardised on RFC 3713 / ISO 18033-3 — common in Japanese and European deployments.
- **Pick Serpent** when the security margin matters more than throughput. Serpent has the largest round count of any AES finalist; it is also the slowest by a meaningful margin. Reach for it when the data at risk justifies the trade.

## AES — `AesBlockCipher`

`AesBlockCipher` is a thin adapter over the BCL `Aes` algorithm that exposes the single-block ECB-mode encrypt / decrypt surface as an `IBlockCipher`. The wrapped BCL implementation is hardware-accelerated on AES-NI-capable CPUs.

```csharp
using Bodu.Security.Cryptography;

byte[] key = RandomNumberGenerator.GetBytes(32);   // AES-256

using var aes = new AesBlockCipher(key);

Span<byte> plaintext  = stackalloc byte[16];
Span<byte> ciphertext = stackalloc byte[16];
aes.Encrypt(plaintext, ciphertext);
aes.Decrypt(ciphertext, plaintext);
```

Use `AesBlockCipher` as the primitive that AEAD modes (GCM, GCM-SIV, CCM, OCB, EAX, SIV) compose with — see [AEAD modes](aead-modes.md). For a full ECB / CBC / CTR / CFB cipher stream, reach for the BCL `Aes` class directly: `Bodu.Security.Cryptography` does not duplicate the BCL surface.

## Twofish

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

using var twofish = Twofish.Create();
twofish.KeySize    = 256;
twofish.Key        = RandomNumberGenerator.GetBytes(32);
twofish.BlockMode  = CipherModeKind.CBC;
twofish.BlockPadding = PaddingModeKind.PKCS7;
twofish.GenerateIV();

using ICryptoTransform encryptor = twofish.CreateEncryptor();
byte[] ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
```

`Twofish` extends `SymmetricAlgorithm`, so the standard `ICryptoTransform` pattern works. Defaults are CBC + PKCS7 with a 256-bit key.

## Camellia

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

byte[] key = RandomNumberGenerator.GetBytes(32);
using var camellia = new CamelliaBlockCipher(key);

Span<byte> plaintext  = stackalloc byte[16];
Span<byte> ciphertext = stackalloc byte[16];
camellia.Encrypt(plaintext, ciphertext);
camellia.Decrypt(ciphertext, plaintext);
```

`CamelliaBlockCipher` implements `IBlockCipher` directly (no `SymmetricAlgorithm` wrapper). Pair it with the cipher-mode transforms (`CbcModeTransform`, `CtrModeTransform`, …) for a full streaming cipher pipeline — see [Cipher block modes](cipher-modes.md).

## Serpent-128

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

using var serpent = Serpent128.Create();
serpent.KeySize     = 256;
serpent.Key         = RandomNumberGenerator.GetBytes(32);
serpent.BlockMode   = CipherModeKind.CBC;
serpent.BlockPadding = PaddingModeKind.PKCS7;
serpent.GenerateIV();

using ICryptoTransform encryptor = serpent.CreateEncryptor();
byte[] ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
```

`Serpent128` extends `SymmetricAlgorithm`. The wider-block Serpent variants (`Serpent256`, `Serpent512`, `Serpent1024`) are non-standard tweakable constructions with larger block sizes — they are *not* drop-in for AES.

## Composing with modes and padding

Every cipher above wraps with the standard `CipherModeKind` / `PaddingModeKind` pipeline:

```csharp
using var cipher = Twofish.Create();
cipher.Key          = key;
cipher.IV           = iv;
cipher.BlockMode    = CipherModeKind.CTR;
cipher.BlockPadding = PaddingModeKind.None;   // CTR doesn't need padding

using ICryptoTransform t = cipher.CreateEncryptor();
byte[] ciphertext = t.TransformFinalBlock(plaintext, 0, plaintext.Length);
```

For AEAD constructions — where authentication is part of the cipher rather than a separate step — see [AEAD modes](aead-modes.md). Any of AES / Twofish / Camellia / Serpent-128 can be plugged into the AEAD transforms (GCM, CCM, OCB, EAX, SIV, GCM-SIV).

## Security caveats

- **AES is the conservative default.** The four ciphers here are all considered safe at standard key sizes, but AES has had the most cryptanalysis attention. Twofish and Camellia are unbroken; Serpent has the largest theoretical margin.
- **Twofish key-dependent S-boxes** add a small per-key setup cost. For workloads that frequently re-key, AES is faster.
- **Camellia and Twofish have no hardware acceleration in commodity CPUs** — the BCL AES path is several × faster on hardware that supports AES-NI.
- **Serpent's 32-round design is intentional** — it is slower than AES by a meaningful margin (~3 × in software). Pick it when latency does not dominate.

## When *not* to use this family

- **You need a stream cipher.** Reach for [Stream ciphers](stream-ciphers.md) — ChaCha20 / Salsa20 / Rabbit / HC-128. For most modern workloads, ChaCha20-Poly1305 (an AEAD construction over ChaCha20) is the better choice than any 128-bit block cipher in CBC.
- **You need authenticated encryption.** Reach for [AEAD modes](aead-modes.md) — GCM, CCM, OCB, EAX, SIV, GCM-SIV. Unauthenticated CBC + HMAC is error-prone; AEAD removes the foot-guns.
- **You need a tweakable cipher.** Reach for [Threefish-256](threefish-256.md) / [Threefish-512](threefish-512.md) / [Threefish-1024](threefish-1024.md).
- **You need a legacy cipher for compatibility.** [Blowfish](blowfish.md) and [Skipjack](skipjack.md) have their own guides.

## See also

- [Encryption basics](encryption-basics.md) — modes, padding, IVs, key management.
- [Cipher block modes](cipher-modes.md) — ECB, CBC, CFB, OFB, CTR, CTS, XTS over any block cipher.
- [Padding](padding.md) — PKCS7, ANSI X.923, ISO 10126, ISO 7816-4, Zero padding.
- [AEAD modes](aead-modes.md) — GCM, CCM, OCB, EAX, SIV, GCM-SIV.
- [Composing primitives](composing-primitives.md) — encrypt-then-MAC, key derivation, nonce management.
- [Bodu.Security.Cryptography landing page](xref:Bodu.Security.Cryptography).
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
