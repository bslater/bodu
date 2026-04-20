---
title: Padding
---

# Padding

Block ciphers encrypt fixed-size blocks. When a plaintext is not a whole number of blocks long, the last block has to be *padded* — and the receiver has to know how to *unpad* it. This page covers the padding schemes supported by the library and shows when each one is appropriate.

The five framework modes come from the standard <xref:System.Security.Cryptography.PaddingMode> enum. Set them via the algorithm's `Padding` property, or construct a strategy directly through <xref:Bodu.Security.Cryptography.PaddingFactory>. An extended <xref:Bodu.Security.Cryptography.BoduPaddingMode> enum mirrors those values and adds ISO/IEC 7816-4 bit padding for scenarios where the framework enum is not expressive enough.

## PKCS7 — the safe default

**Use for:** any block-oriented mode (CBC, ECB) where the plaintext is arbitrary binary or text.

PKCS7 appends *N* bytes, each with the value *N*, where *N* is chosen to bring the plaintext to the next block boundary. When the plaintext is already block-aligned, an entire extra block of *block_size* bytes is appended, each carrying the value *block_size*. That sounds wasteful but it's what makes unpadding unambiguous: the last byte always tells you how many pad bytes to strip.

```csharp
using var alg = new Threefish256
{
    BlockMode = CipherBlockMode.CBC,
    Padding   = PaddingMode.PKCS7,
};
alg.GenerateKey();
alg.GenerateIV();
alg.GenerateTweak();

byte[] plaintext  = new byte[100];      // not a multiple of 32
byte[] ciphertext = alg.Encrypt(plaintext);
byte[] recovered  = alg.Decrypt(ciphertext);

Debug.Assert(plaintext.SequenceEqual(recovered));
```

PKCS7 round-trips cleanly for any plaintext length, including zero.

**Note on padding oracles.** PKCS7 unpadding validates the trailing bytes and rejects a ciphertext whose last block is not well-formed. Historically this has been the source of "padding oracle" attacks against CBC when an attacker can distinguish a padding failure from a MAC failure via timing or error messages. The library validates in constant time inside `Pkcs7Padding.Unpad`, but you should still pair CBC-with-PKCS7 encryption with a MAC over the ciphertext (encrypt-then-MAC) when the ciphertext travels across a trust boundary.

## Zeros — for application-framed messages

**Use for:** plaintexts that already carry their own length (a length prefix, a terminator, or a framed binary record) and are produced with a deterministic shape.

Zero padding appends `0x00` bytes until the plaintext reaches a block boundary. It does nothing when the plaintext is already block-aligned. The problem with unpadding is that `0x00` is a perfectly valid plaintext byte — `Unpad` strips trailing zeros, and it cannot tell whether those zeros were padding or part of the real message.

```csharp
using var alg = new Threefish256
{
    BlockMode = CipherBlockMode.CBC,
    Padding   = PaddingMode.Zeros,
};
alg.GenerateKey();
alg.GenerateIV();
alg.GenerateTweak();

// Plaintext has a length prefix, so trailing zeros are unambiguous.
byte[] plaintext = BuildMessageWithLengthPrefix(body: new byte[97]);
byte[] ciphertext = alg.Encrypt(plaintext);
byte[] recovered  = alg.Decrypt(ciphertext);

// Recovered may end in extra 0x00 bytes; consult the length prefix to strip them.
```

Zero padding is appropriate when **the application layer already knows the real length**. If it doesn't, prefer PKCS7.

## ANSI X.923 — length byte, zero-filled interior

**Use for:** interoperability with legacy systems or specifications that mandate ANSI X.923.

ANSI X.923 appends `N - 1` bytes of value `0x00` followed by a trailing byte holding the padding length `N`. When the plaintext is already block-aligned, a full extra block of padding is appended so unpadding remains unambiguous. `Unpad` validates in constant time that all interior pad bytes are `0x00` and that the trailing length byte is in range.

```csharp
using var alg = new Threefish256
{
    BlockMode = CipherBlockMode.CBC,
    Padding   = PaddingMode.ANSIX923,
};
alg.GenerateKey();
alg.GenerateIV();
alg.GenerateTweak();

byte[] plaintext  = new byte[100];
byte[] ciphertext = alg.Encrypt(plaintext);
byte[] recovered  = alg.Decrypt(ciphertext);
```

The same padding-oracle caution as PKCS7 applies: pair with a MAC when the ciphertext crosses a trust boundary.

## ISO 10126 — length byte, random interior

**Use for:** interoperability with existing ISO 10126 ciphertexts. The scheme was withdrawn by ISO in 2007 and is not recommended for new designs — prefer PKCS7.

ISO 10126 is shaped like ANSI X.923 except the interior pad bytes are cryptographically random instead of `0x00`. Because the random bytes cannot be reconstructed during decryption, `Unpad` only validates the trailing length byte.

```csharp
using var alg = new Threefish256
{
    BlockMode = CipherBlockMode.CBC,
    Padding   = PaddingMode.ISO10126,
};
```

## ISO/IEC 7816-4 — bit padding (one-and-zeros)

**Use for:** smart-card protocols and crypto constructions that require the 7816-4 shape (for example CMAC, SHA-3/Keccak). Access this mode through the extended <xref:Bodu.Security.Cryptography.BoduPaddingMode> enum, which is not part of the framework `PaddingMode` surface.

ISO/IEC 7816-4 appends a single `0x80` byte followed by `0x00` bytes out to the next block boundary. When the plaintext is already block-aligned, a full extra block of padding is appended. `Unpad` scans the final block in constant time for the rightmost `0x80`: everything after it must be `0x00`, and the byte itself marks the end of the plaintext.

```csharp
IPaddingStrategy bitPadding = PaddingFactory.Create(BoduPaddingMode.ISO7816_4);

byte[] padded   = bitPadding.Pad(plaintext, blockSize: 32);
byte[] unpadded = bitPadding.Unpad(padded, blockSize: 32);
```

## None — only for block-aligned or stream modes

**Use for:** stream-shaped modes (CTR, CFB, OFB) that don't need padding, or when you have already padded the plaintext in your own code and want the cipher to leave it alone.

```csharp
// Stream-shaped mode: ciphertext length == plaintext length.
using var alg = new Threefish256
{
    BlockMode = CipherBlockMode.CTR,
    Padding   = PaddingMode.None,
};
alg.GenerateKey();
alg.GenerateIV();
alg.GenerateTweak();

byte[] plaintext  = new byte[100];     // any length is fine
byte[] ciphertext = alg.Encrypt(plaintext);    // 100 bytes out
byte[] recovered  = alg.Decrypt(ciphertext);
```

`PaddingMode.None` combined with a block-oriented mode (CBC, ECB) and a plaintext that isn't a multiple of the block size will throw <xref:System.Security.Cryptography.CryptographicException> during encryption. Don't use the two together unless you know your plaintext is block-aligned.

## Choosing — quick guide

| If your plaintext is… | and your mode is… | Use | Ciphertext length |
|---|---|---|---|
| Arbitrary bytes | CBC, ECB | **PKCS7** | Plaintext + 1–block bytes |
| Framed (length-prefixed / terminated) | CBC, ECB | **Zeros** or **PKCS7** | ≥ plaintext, to next block boundary |
| Any length | CTR, CFB, OFB | **None** | Exactly plaintext length |
| Already block-aligned | CBC, ECB | **None** (if sure) | Exactly plaintext length |
| Interop with ANSI X.923 spec | CBC, ECB | **ANSIX923** | Plaintext + 1–block bytes |
| Interop with legacy ISO 10126 ciphertext | CBC, ECB | **ISO10126** | Plaintext + 1–block bytes |
| Smart-card / CMAC / bit-oriented protocols | CBC, ECB | **ISO7816_4** (via `BoduPaddingMode`) | Plaintext + 1–block bytes |

## Using the strategy directly

For advanced scenarios you can obtain an <xref:Bodu.Security.Cryptography.IPaddingStrategy> directly and apply it to a byte span:

```csharp
using Bodu.Security.Cryptography;
using System.Security.Cryptography;

IPaddingStrategy pkcs7 = PaddingFactory.Create(PaddingMode.PKCS7);

byte[] padded   = pkcs7.Pad(plaintext, blockSize: 32);
byte[] unpadded = pkcs7.Unpad(padded, blockSize: 32);
```

This is primarily useful if you're composing a custom encryption pipeline against <xref:Bodu.Security.Cryptography.IBlockCipherModeTransform>.

## Where to go next

- [Cipher block modes](cipher-modes.md) — which padding pairs with which mode.
- [Encryption basics](encryption-basics.md) — the full Key/IV/Tweak/Padding lifecycle.
