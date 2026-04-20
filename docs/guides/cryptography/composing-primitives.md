---
title: Composing primitives — direct use vs. SymmetricAlgorithm
---

# Composing primitives — direct use vs. `SymmetricAlgorithm`

`Bodu.Security.Cryptography` exposes its block ciphers at **two levels**:

1. The **raw block primitive** — `SkipjackBlockCipher`, `BlowfishBlockCipher`, `Threefish256Cipher`, `Threefish512Cipher`, `Threefish1024Cipher`, `AesBlockCipher`. Each implements <xref:Bodu.Security.Cryptography.IBlockCipher>: encrypt and decrypt one fixed-size block. Compose with <xref:Bodu.Security.Cryptography.BlockCipherModeFactory> and <xref:Bodu.Security.Cryptography.PaddingFactory> when you need the full mode + padding pipeline visible at the call site.
2. The **`SymmetricAlgorithm`** wrappers — <xref:Bodu.Security.Cryptography.Skipjack>, <xref:Bodu.Security.Cryptography.Blowfish>, <xref:Bodu.Security.Cryptography.Threefish256>, <xref:Bodu.Security.Cryptography.Threefish512>, <xref:Bodu.Security.Cryptography.Threefish1024>. These derive from <xref:System.Security.Cryptography.SymmetricAlgorithm?displayProperty=nameWithType> and integrate with `CryptoStream`, `ICryptoTransform`, and the `Encrypt` / `Decrypt` extension methods.

Both levels produce **byte-for-byte identical ciphertext** for the same Key, IV, (Tweak), Mode, and Padding. Pick the level that matches your use case:

| Use the primitive when… | Use the SymmetricAlgorithm when… |
|---|---|
| You want each step (encrypt block, accumulate MAC, manage IV, pad) explicit at the call site. | You want a one-liner for `byte[] cipher = alg.Encrypt(plaintext)`. |
| You're composing custom AEAD, custom padding, or a streaming protocol. | You're integrating with `CryptoStream`, `RSACryptoServiceProvider`-style ceremony, or .NET ecosystem code. |
| You need to share an `IBlockCipher` between an AEAD transform and a classic mode transform. | You're following the common .NET pattern most readers already know. |

## A note on AEAD modes

The five AEAD mode transforms (`GcmModeTransform`, `CcmModeTransform`, `OcbModeTransform`, `SivModeTransform`, `GcmSivModeTransform`) are designed for **128-bit-block ciphers** — their counter formats, GHASH/POLYVAL field, and offset schedules all assume 16-byte blocks. Skipjack and Blowfish have 8-byte blocks; the Threefish family starts at 32 bytes. Only <xref:Bodu.Security.Cryptography.AesBlockCipher> fits, which is why the [AEAD modes guide](aead-modes.md) is exclusively AES-based. The classic modes shown here (CBC / CTR / CFB / OFB / ECB) work with any block size and so cover the rest of the cipher family.

## Pattern 1 — direct primitive composition

Encrypt a message under Skipjack with CBC + PKCS7, manually composing the cipher, mode transform, and padding strategy:

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

byte[] key = new byte[10];   RandomNumberGenerator.Fill(key);
byte[] iv  = new byte[8];    RandomNumberGenerator.Fill(iv);

byte[] plaintext = System.Text.Encoding.UTF8.GetBytes("legacy payload");

// 1. Build the primitive.
using IBlockCipher cipher = new SkipjackBlockCipher(key);

// 2. Wrap with a mode transform.
IBlockCipherModeTransform mode = BlockCipherModeFactory.Create(CipherBlockMode.CBC, cipher, iv);

// 3. Choose a padding strategy.
IPaddingStrategy padding = PaddingFactory.Create(PaddingMode.PKCS7);

// 4. Pad, transform, and emit.
byte[] padded = padding.Pad(plaintext, cipher.BlockSize);
byte[] ciphertext = new byte[padded.Length];
mode.Transform(padded, ciphertext, encrypt: true);
```

Decrypt is the same flow reversed:

```csharp
using IBlockCipher cipher = new SkipjackBlockCipher(key);
IBlockCipherModeTransform mode = BlockCipherModeFactory.Create(CipherBlockMode.CBC, cipher, iv);
IPaddingStrategy padding = PaddingFactory.Create(PaddingMode.PKCS7);

byte[] decrypted = new byte[ciphertext.Length];
mode.Transform(ciphertext, decrypted, encrypt: false);
byte[] recovered = padding.Unpad(decrypted, cipher.BlockSize);
```

The same shape works for **every** primitive — swap `SkipjackBlockCipher` for `BlowfishBlockCipher`, or for one of the Threefish variants (which take a key *and* a 16-byte tweak):

```csharp
byte[] key   = new byte[32];  RandomNumberGenerator.Fill(key);
byte[] iv    = new byte[32];  RandomNumberGenerator.Fill(iv);
byte[] tweak = new byte[16];  RandomNumberGenerator.Fill(tweak);

using IBlockCipher cipher = new Threefish256Cipher(key, tweak);
IBlockCipherModeTransform mode = BlockCipherModeFactory.Create(CipherBlockMode.CTR, cipher, iv);

// CTR is a stream mode — no padding.
byte[] ciphertext = new byte[plaintext.Length];
mode.Transform(plaintext, ciphertext, encrypt: true);
```

## Pattern 2 — `SymmetricAlgorithm` wrapper

The same Skipjack-CBC-PKCS7 encryption written through the high-level wrapper:

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

using var alg = new Skipjack
{
    BlockMode = CipherBlockMode.CBC,
    Padding   = PaddingMode.PKCS7,
    Key       = key,    // same 10 bytes
    IV        = iv,     // same 8 bytes
};

byte[] ciphertext = alg.Encrypt(plaintext);
byte[] recovered  = alg.Decrypt(ciphertext);
```

Internally, `Skipjack.CreateEncryptor` builds the same `SkipjackBlockCipher` + `CbcModeTransform` + `Pkcs7Padding` you composed by hand in Pattern 1. **The ciphertext is byte-for-byte identical.** That's the contract worth remembering: the two patterns are equivalent — the wrapper exists for convenience, not for behaviour.

The Threefish wrappers carry the same equivalence, with `Tweak` flowing into the constructor of `Threefish*Cipher`:

```csharp
using var alg = new Threefish256
{
    BlockMode = CipherBlockMode.CTR,
    Padding   = PaddingMode.None,
    Key       = key,    // 32 bytes
    IV        = iv,     // 32 bytes
    Tweak     = tweak,  // 16 bytes
};

byte[] ciphertext = alg.Encrypt(plaintext);
```

## Verifying the equivalence

Inside a single test you can prove the two patterns produce the same ciphertext byte-for-byte:

```csharp
byte[] key       = RandomNumberGenerator.GetBytes(10);
byte[] iv        = RandomNumberGenerator.GetBytes(8);
byte[] plaintext = RandomNumberGenerator.GetBytes(64);

// Direct path.
byte[] direct;
using (IBlockCipher cipher = new SkipjackBlockCipher(key))
{
    var mode = BlockCipherModeFactory.Create(CipherBlockMode.CBC, cipher, iv);
    var pad  = PaddingFactory.Create(PaddingMode.PKCS7);

    byte[] padded = pad.Pad(plaintext, cipher.BlockSize);
    direct = new byte[padded.Length];
    mode.Transform(padded, direct, encrypt: true);
}

// SymmetricAlgorithm path.
byte[] viaAlg;
using (var alg = new Skipjack
{
    BlockMode = CipherBlockMode.CBC,
    Padding   = PaddingMode.PKCS7,
    Key       = key,
    IV        = iv,
})
{
    viaAlg = alg.Encrypt(plaintext);
}

Debug.Assert(direct.SequenceEqual(viaAlg));
```

## When to favour each pattern

**Reach for the direct primitive when:**

- You're building a *custom* construction — your own AEAD, a MAC over framing fields, an authenticated channel — and you need explicit access to the block primitive at every step.
- You're composing across modes — for example, encrypting a header with CBC and a body with CTR under the same key — and want a single `IBlockCipher` instance shared by both transforms.
- You need to pass a Bodu cipher into a third-party API that expects an `IBlockCipher`.

**Reach for the `SymmetricAlgorithm` wrapper when:**

- You're emitting a single message and want the one-liner.
- You're plugging into `System.Security.Cryptography.CryptoStream`, ASP.NET DataProtection, or any other API that accepts a `SymmetricAlgorithm`.
- You're following the convention readers already know — `Encrypt` / `Decrypt` extension methods, `CreateEncryptor` / `CreateDecryptor` for stream-style use.

## Where to go next

- [Encryption basics](encryption-basics.md) — the Key / IV / Tweak / Padding lifecycle for the `SymmetricAlgorithm` wrappers.
- [Cipher block modes](cipher-modes.md) — what each of the five classic modes does, and worked examples through the wrapper API.
- [AEAD modes](aead-modes.md) — for authenticated encryption (AES-only, via `AesBlockCipher`).
- [Padding](padding.md) — PKCS7 / Zeros / None and when each is safe.
- API reference: [<xref:Bodu.Security.Cryptography.IBlockCipher>] · [<xref:Bodu.Security.Cryptography.BlockCipherModeFactory>] · [<xref:Bodu.Security.Cryptography.PaddingFactory>] · [<xref:Bodu.Security.Cryptography.IBlockCipherModeTransform>].
