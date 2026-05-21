---
title: Using Threefish-512
---

# Using Threefish-512

<xref:Bodu.Security.Cryptography.Threefish512> is the 512-bit variant of the Threefish tweakable block cipher family. It operates on **512-bit (64-byte) blocks** with a **512-bit (64-byte) key** and a **128-bit (16-byte) tweak**. Of the three Threefish variants, this is the one used as the core of the standard Skein hash function.

![Threefish round function — MIX, word permutation, and subkey injection](../../images/diagrams/threefish-round.svg)

## Fixed sizes at a glance

| Parameter | Size | Notes |
|---|---|---|
| Block size | 512 bits (64 bytes) | Ciphertext is a multiple of 64 bytes (except in stream modes). |
| Key size | 512 bits (64 bytes) | Fixed. |
| Tweak size | 128 bits (16 bytes) | Fixed across the Threefish family. |
| IV size | 512 bits (64 bytes) | Always matches the block size. |

## Encrypt and decrypt — CBC + PKCS7

```csharp
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] plaintext = Encoding.UTF8.GetBytes("a longer record payload for the 64-byte block cipher");

byte[] key, iv, tweak, ciphertext;
using (var alg = new Threefish512 { BlockMode = CipherModeKind.CBC, Padding = PaddingMode.PKCS7 })
{
    alg.GenerateKey();
    alg.GenerateIV();
    alg.GenerateTweak();

    key   = alg.Key;     // 64 bytes
    iv    = alg.IV;      // 64 bytes
    tweak = alg.Tweak;   // 16 bytes

    ciphertext = alg.Encrypt(plaintext);
}

byte[] recovered;
using (var alg = new Threefish512 { BlockMode = CipherModeKind.CBC, Padding = PaddingMode.PKCS7,
                                     Key = key, IV = iv, Tweak = tweak })
{
    recovered = alg.Decrypt(ciphertext);
}

Debug.Assert(plaintext.SequenceEqual(recovered));
```

## Encrypt and decrypt — CTR (stream mode, parallelisable)

```csharp
using var alg = new Threefish512
{
    BlockMode = CipherModeKind.CTR,
    Padding   = PaddingMode.None,
};
alg.GenerateKey();
alg.GenerateIV();
alg.GenerateTweak();

byte[] ciphertext = alg.Encrypt(plaintext);
byte[] recovered  = alg.Decrypt(ciphertext);

Debug.Assert(plaintext.SequenceEqual(recovered));
```

With a 512-bit block the counter space is astronomical; wrap-around is a theoretical concern only. CTR's usual rule still stands: never reuse an `(IV, Key)` pair across messages.

## Loading and reusing a known key

If you've stored a key in a secrets manager, load it directly rather than calling `GenerateKey()`:

```csharp
byte[] key = LoadKeyFromVault(keyId: "threefish-512/main");
Debug.Assert(key.Length == 64);

using var alg = new Threefish512 { Key = key, BlockMode = CipherModeKind.CTR, Padding = PaddingMode.None };
alg.GenerateIV();           // fresh per message
alg.GenerateTweak();        // or set a per-record tweak

byte[] ciphertext = alg.Encrypt(plaintext);
```

## Per-record tweak

Threefish's tweak is particularly useful when encrypting many small records under the same key. Using the record's stable ID as a tweak gives each record an independent encryption stream without key rotation:

```csharp
byte[] RecordTweak(long recordId)
{
    byte[] tweak = new byte[16];
    BinaryPrimitives.WriteInt64LittleEndian(tweak.AsSpan(0, 8), recordId);
    return tweak;
}

byte[] EncryptRecord(byte[] key, byte[] iv, long recordId, byte[] plaintext)
{
    using var alg = new Threefish512
    {
        Key       = key,
        IV        = iv,
        Tweak     = RecordTweak(recordId),
        BlockMode = CipherModeKind.CBC,
        Padding   = PaddingMode.PKCS7,
    };
    return alg.Encrypt(plaintext);
}
```

## Where to go next

- [Encryption basics](encryption-basics.md) — the Key/IV/Tweak lifecycle.
- [Cipher block modes](cipher-modes.md) — CFB / OFB / ECB also work with `Threefish512`.
- [Padding](padding.md) — which padding scheme pairs with which mode.
- Other variants: [Threefish-256](threefish-256.md), [Threefish-1024](threefish-1024.md).
