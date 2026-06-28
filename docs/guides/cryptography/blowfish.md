---
title: Using Blowfish
---

# Using Blowfish

<xref:Bodu.Security.Cryptography.Blowfish> is Bruce Schneier's 1993 Feistel cipher. It operates on **64-bit (8-byte) blocks** with a **variable-length key from 32 to 448 bits (4 to 56 bytes)**, chosen in 8-bit increments.

> [!NOTE]
> Blowfish has a 64-bit block size, which makes it vulnerable to birthday-bound attacks (SWEET32) when more than a few gigabytes are encrypted under the same key. For new work, prefer a 128-bit-block cipher such as AES or Threefish-256.

## Sizes at a glance

| Parameter | Size | Notes |
|---|---|---|
| Block size | 64 bits (8 bytes) | Fixed. |
| Key size | 32 – 448 bits (4 – 56 bytes), 8-bit steps | Default is 128 bits. Set via `KeySize` before `GenerateKey()`. |
| IV size | 64 bits (8 bytes) | Matches the block size. |

## Encrypt and decrypt — CBC + PKCS7 (default 128-bit key)

```csharp
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] plaintext = Encoding.UTF8.GetBytes("message under Blowfish");

byte[] key, iv, ciphertext;
using (var alg = new Blowfish { BlockMode = CipherModeKind.CBC, Padding = PaddingMode.PKCS7 })
{
    alg.GenerateKey();    // 16 bytes at the default 128-bit key size
    alg.GenerateIV();     // 8 bytes

    key = alg.Key;
    iv  = alg.IV;

    ciphertext = alg.Encrypt(plaintext);
}

byte[] recovered;
using (var alg = new Blowfish { BlockMode = CipherModeKind.CBC, Padding = PaddingMode.PKCS7,
                                 Key = key, IV = iv })
{
    recovered = alg.Decrypt(ciphertext);
}

Debug.Assert(plaintext.SequenceEqual(recovered));
```

## Using a longer key

Blowfish accepts keys up to 448 bits. Set `KeySize` *before* generating the key, because `GenerateKey()` sizes its output from that property:

```csharp
using var alg = new Blowfish
{
    KeySize   = 256,              // 32-byte key
    BlockMode = CipherModeKind.CBC,
    Padding   = PaddingMode.PKCS7,
};
alg.GenerateKey();                // 32 bytes
alg.GenerateIV();

byte[] ciphertext = alg.Encrypt(plaintext);
```

Any byte length from 4 to 56 is valid; longer keys only strengthen the cipher up to the point where the key schedule is the attack vector rather than the round function.

## Encrypt and decrypt — CTR (stream mode)

```csharp
using var alg = new Blowfish
{
    BlockMode = CipherModeKind.CTR,
    Padding   = PaddingMode.None,
};
alg.GenerateKey();
alg.GenerateIV();                  // 8-byte counter block, unique per message

byte[] ciphertext = alg.Encrypt(plaintext);
byte[] recovered  = alg.Decrypt(ciphertext);
```

As with Skipjack, the 8-byte counter means you should not encrypt more than a few gigabytes under the same key in CTR mode.

## A note on the key schedule

Blowfish's key schedule is intentionally expensive: it derives the P-array and four S-boxes from the key by running the cipher thousands of times over itself. This makes brute-force searches meaningfully harder at the cost of a slow setup — roughly a few milliseconds for a one-off key.

The practical implication is that you should **not** build a new `Blowfish` instance per message if you're re-using the same key. Cache the instance and call `CreateEncryptor()` / `CreateDecryptor()` per message instead.

## Dropping to the raw primitive

`Blowfish` is the `SymmetricAlgorithm` wrapper; underneath it is `BlowfishBlockCipher`, a raw <xref:Bodu.Security.Cryptography.IBlockCipher>. Reach for the primitive when you want to share one keyed engine across several mode transforms, or to compose a custom pipeline — the wrapper and the primitive produce byte-for-byte identical ciphertext for the same key, IV, mode, and padding. See [Composing primitives](composing-primitives.md) for the full pattern.

```csharp
using IBlockCipher cipher = new BlowfishBlockCipher(key);
IBlockCipherModeTransform mode = BlockCipherModeFactory.Create(CipherModeKind.CBC, cipher, iv);
IPaddingStrategy padding = PaddingFactory.Create(PaddingMode.PKCS7);
```

Because the key schedule is the expensive part, constructing one `BlowfishBlockCipher` and reusing it across messages amortises that cost — exactly the same caching argument as for the wrapper.

> [!IMPORTANT]
> Blowfish is unbroken at the round-function level, but its **64-bit block** caps its safe data volume per key (SWEET32) and its design predates authenticated encryption. For new work, prefer a 128-bit-block cipher ([AES family](aes-family.md)) under an [AEAD mode](aead-modes.md), or [Threefish](threefish-256.md) when you need a tweak.

## Where to go next

- [Encryption basics](encryption-basics.md) — the Key/IV lifecycle.
- [Cipher block modes](cipher-modes.md) — CFB, OFB, ECB also work with Blowfish.
- [Padding](padding.md) — which padding scheme pairs with which mode.
- [Composing primitives](composing-primitives.md) — `BlowfishBlockCipher` + mode + padding by hand.
- [AES-family block ciphers](aes-family.md) — the 128-bit-block successors to prefer for new work.
- [Using Skipjack](skipjack.md) — the other 64-bit-block cipher in the library.
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
