---
title: Using Skipjack
---

# Using Skipjack

<xref:Bodu.Security.Cryptography.Skipjack> is an NSA-designed block cipher declassified in 1998. It is included for **legacy interoperability and research** - do not use it to protect sensitive data in new applications.

> [!IMPORTANT]
> Skipjack has an 80-bit key and a 64-bit block. The short key is well below modern security margins, and the 64-bit block is vulnerable to birthday-bound attacks (SWEET32) when more than a few gigabytes are encrypted under the same key. For new work, use AES (via the BCL's <xref:System.Security.Cryptography.Aes?displayProperty=nameWithType>).

## Fixed sizes at a glance

| Parameter | Size | Notes |
|---|---|---|
| Block size | 64 bits (8 bytes) | Fixed - cannot be configured. |
| Key size | 80 bits (10 bytes) | Fixed. |
| IV size | 64 bits (8 bytes) | Matches the block size. |
| Tweak | - | Skipjack is not tweakable. |

## Encrypt and decrypt - CBC + PKCS7

<!-- compile -->
```csharp
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] plaintext = Encoding.UTF8.GetBytes("legacy payload");

byte[] key, iv, ciphertext;
using (var alg = new Skipjack { BlockMode = CipherModeKind.CBC, Padding = PaddingMode.PKCS7 })
{
    alg.GenerateKey();           // 10 bytes
    alg.GenerateIV();            // 8 bytes

    key = alg.Key;
    iv  = alg.IV;

    ciphertext = alg.Encrypt(plaintext);
}

byte[] recovered;
using (var alg = new Skipjack { BlockMode = CipherModeKind.CBC, Padding = PaddingMode.PKCS7,
                                 Key = key, IV = iv })
{
    recovered = alg.Decrypt(ciphertext);
}

Debug.Assert(plaintext.SequenceEqual(recovered));
```

## Encrypt and decrypt - CTR (stream mode)

CTR needs no padding expansion, but through the `Skipjack` wrapper `PaddingMode.None` still requires block-aligned input (a multiple of 8 bytes); for arbitrary lengths drive <xref:Bodu.Security.Cryptography.CtrModeTransform> directly, which accepts a trailing partial block (see [Modes, transforms, and factories](cipher-composition-reference.md)):

```csharp
using var alg = new Skipjack
{
    BlockMode = CipherModeKind.CTR,
    Padding   = PaddingMode.None,  // CTR doesn't pad - input must be block-aligned here
};
alg.GenerateKey();
alg.GenerateIV();              // initial counter block - must be unique per message

byte[] aligned = new byte[64]; // eight 8-byte blocks
RandomNumberGenerator.Fill(aligned);

byte[] ciphertext = alg.Encrypt(aligned);   // same length as the input
byte[] recovered  = alg.Decrypt(ciphertext);
```

Skipjack's 8-byte block is small. Encrypting more than ~2³² blocks (32 GB) under the same key in CTR mode is unsafe because of the 64-bit birthday bound (SWEET32), long before the 64-bit counter could wrap. `CtrModeTransform` throws only if the counter rolls over to zero.

## Loading a known key

Skipjack's key is small enough (10 bytes) that you might keep it embedded in a configuration file or a key-wrapping envelope. Either way, set it explicitly:

```csharp
byte[] key = new byte[]
{
    0x00, 0x99, 0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11,
};

using var alg = new Skipjack
{
    Key       = key,
    BlockMode = CipherModeKind.CBC,
    Padding   = PaddingMode.PKCS7,
};
alg.GenerateIV();
byte[] ciphertext = alg.Encrypt(plaintext);
```

## When to use Skipjack

- **Interoperability** with systems that historically used Skipjack (e.g. legacy US government protocols).
- **Educational exercises** and cipher-design studies.
- **Regression fixtures** where you need a deterministic pinpoint cipher whose test vectors are well-known.

For anything else, reach for AES or Threefish.

## Two security limits, not one

Skipjack fails the modern bar on **two independent axes**, and both are structural - no mode or padding choice fixes them:

- **80-bit key.** The key space is 2⁸⁰. That was defensible in 1998 but is now within reach of a well-resourced adversary; modern designs use 128-bit keys as the floor. There is no longer-key Skipjack variant.
- **64-bit block.** Like Blowfish, the 8-byte block triggers the SWEET32 birthday bound: after roughly 2³² blocks (~32 GB) under one key, collisions in CBC/CTR ciphertext begin to leak plaintext relationships. `CtrModeTransform` throws if the counter actually wraps, but that guard fires long after the statistical danger zone begins.

Treat Skipjack strictly as an interop and research cipher. For confidentiality use the BCL <xref:System.Security.Cryptography.Aes?displayProperty=nameWithType> or [Threefish](threefish-256.md); for confidentiality plus integrity use an [AEAD mode](aead-modes.md).

## Dropping to the raw primitive

`Skipjack` is the `SymmetricAlgorithm` wrapper; `SkipjackBlockCipher` is the underlying raw <xref:Bodu.Security.Cryptography.IBlockCipher>. Use it when composing a pipeline by hand - both paths yield identical ciphertext. See [Composing primitives](composing-primitives.md), which uses Skipjack as its worked example.

## Where to go next

- [Encryption basics](encryption-basics.md) - the Key/IV lifecycle.
- [Cipher block modes](cipher-modes.md) - CFB, OFB, ECB also work with Skipjack.
- [Padding](padding.md) - which padding scheme pairs with which mode.
- [Composing primitives](composing-primitives.md) - `SkipjackBlockCipher` + mode + padding by hand.
- [Using Blowfish](blowfish.md) - another 64-bit-block cipher with a variable key size.
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** - every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
