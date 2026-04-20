---
title: Using Skipjack
---

# Using Skipjack

<xref:Bodu.Security.Cryptography.Skipjack> is an NSA-designed block cipher declassified in 1998. It is included for **legacy interoperability and research** — do not use it to protect sensitive data in new applications.

> [!IMPORTANT]
> Skipjack has an 80-bit key and a 64-bit block. The short key is well below modern security margins, and the 64-bit block is vulnerable to birthday-bound attacks (SWEET32) when more than a few gigabytes are encrypted under the same key. For new work, use AES (via the BCL's <xref:System.Security.Cryptography.Aes?displayProperty=nameWithType>).

## Fixed sizes at a glance

| Parameter | Size | Notes |
|---|---|---|
| Block size | 64 bits (8 bytes) | Fixed — cannot be configured. |
| Key size | 80 bits (10 bytes) | Fixed. |
| IV size | 64 bits (8 bytes) | Matches the block size. |
| Tweak | — | Skipjack is not tweakable. |

## Encrypt and decrypt — CBC + PKCS7

```csharp
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] plaintext = Encoding.UTF8.GetBytes("legacy payload");

byte[] key, iv, ciphertext;
using (var alg = new Skipjack { BlockMode = CipherBlockMode.CBC, Padding = PaddingMode.PKCS7 })
{
    alg.GenerateKey();           // 10 bytes
    alg.GenerateIV();            // 8 bytes

    key = alg.Key;
    iv  = alg.IV;

    ciphertext = alg.Encrypt(plaintext);
}

byte[] recovered;
using (var alg = new Skipjack { BlockMode = CipherBlockMode.CBC, Padding = PaddingMode.PKCS7,
                                 Key = key, IV = iv })
{
    recovered = alg.Decrypt(ciphertext);
}

Debug.Assert(plaintext.SequenceEqual(recovered));
```

## Encrypt and decrypt — CTR (stream mode)

```csharp
using var alg = new Skipjack
{
    BlockMode = CipherBlockMode.CTR,
    Padding   = PaddingMode.None,
};
alg.GenerateKey();
alg.GenerateIV();              // initial counter block — must be unique per message

byte[] ciphertext = alg.Encrypt(plaintext);
byte[] recovered  = alg.Decrypt(ciphertext);
```

Skipjack's 8-byte counter space is small. Encrypting more than ~2³² blocks (32 GB) under the same key in CTR mode is unsafe because the counter is close to wrapping. `CtrModeTransform` will throw if the counter actually reaches its initial value.

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
    BlockMode = CipherBlockMode.CBC,
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

## Where to go next

- [Encryption basics](encryption-basics.md) — the Key/IV lifecycle.
- [Cipher block modes](cipher-modes.md) — CFB, OFB, ECB also work with Skipjack.
- [Padding](padding.md) — which padding scheme pairs with which mode.
- [Using Blowfish](blowfish.md) — another 64-bit-block cipher with a variable key size.
