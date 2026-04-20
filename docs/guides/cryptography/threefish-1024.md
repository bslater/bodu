---
title: Using Threefish-1024
---

# Using Threefish-1024

<xref:Bodu.Security.Cryptography.Threefish1024> is the largest variant in the Threefish family: **1024-bit (128-byte) blocks** with a **1024-bit (128-byte) key** and a **128-bit (16-byte) tweak**. Reach for Threefish-1024 when you want the widest plausible block size — for example in domains that demand very large key/block margins, or where you want to encrypt a chunk that's naturally 128 bytes wide without chaining overhead per inner field.

## Fixed sizes at a glance

| Parameter | Size | Notes |
|---|---|---|
| Block size | 1024 bits (128 bytes) | Ciphertext is a multiple of 128 bytes (except in stream modes). |
| Key size | 1024 bits (128 bytes) | Fixed. |
| Tweak size | 128 bits (16 bytes) | Fixed across the Threefish family. |
| IV size | 1024 bits (128 bytes) | Always matches the block size. |

## Encrypt and decrypt — CBC + PKCS7

```csharp
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] plaintext = Encoding.UTF8.GetBytes("a substantial payload that benefits from a 128-byte block");

byte[] key, iv, tweak, ciphertext;
using (var alg = new Threefish1024 { BlockMode = CipherBlockMode.CBC, Padding = PaddingMode.PKCS7 })
{
    alg.GenerateKey();
    alg.GenerateIV();
    alg.GenerateTweak();

    key   = alg.Key;     // 128 bytes
    iv    = alg.IV;      // 128 bytes
    tweak = alg.Tweak;   // 16 bytes

    ciphertext = alg.Encrypt(plaintext);
}

byte[] recovered;
using (var alg = new Threefish1024 { BlockMode = CipherBlockMode.CBC, Padding = PaddingMode.PKCS7,
                                      Key = key, IV = iv, Tweak = tweak })
{
    recovered = alg.Decrypt(ciphertext);
}

Debug.Assert(plaintext.SequenceEqual(recovered));
```

## Encrypt and decrypt — CTR (stream mode)

```csharp
using var alg = new Threefish1024
{
    BlockMode = CipherBlockMode.CTR,
    Padding   = PaddingMode.None,
};
alg.GenerateKey();
alg.GenerateIV();
alg.GenerateTweak();

byte[] ciphertext = alg.Encrypt(plaintext);
byte[] recovered  = alg.Decrypt(ciphertext);

Debug.Assert(plaintext.SequenceEqual(recovered));
```

## Trade-offs worth knowing

- **Memory and throughput.** Threefish-1024 has the largest key schedule of the family (17 subkeys of 128 bytes each) and therefore the largest per-instance memory footprint. Throughput is roughly half of Threefish-512 on the same CPU.
- **Padding waste.** A PKCS7 round-up to the next 128-byte boundary costs *more* than the 32-byte Threefish-256 round-up. If your plaintexts are short, Threefish-256 produces smaller ciphertexts.
- **When to prefer it.** When your natural record size is already ≥ 128 bytes, when you want a single-block encryption of a large structured field, or when you want the widest defensive margin against future block-size attacks.

## Where to go next

- [Encryption basics](encryption-basics.md) — the Key/IV/Tweak lifecycle.
- [Cipher block modes](cipher-modes.md) — CFB / OFB / ECB also work with `Threefish1024`.
- [Padding](padding.md) — which padding scheme pairs with which mode.
- Other variants: [Threefish-256](threefish-256.md), [Threefish-512](threefish-512.md).
