---
title: Modes, transforms, and factories
---

# Modes, transforms, and factories

This is the reference for how a block cipher becomes an encryptor in `Bodu.Security.Cryptography`: which <xref:Bodu.Security.Cryptography.CipherModeKind> values the `SymmetricAlgorithm` wrappers accept, which modes exist only as direct transforms, where padding sizes are expressed in bits, and how the pieces — <xref:Bodu.Security.Cryptography.IBlockCipher>, <xref:Bodu.Security.Cryptography.BlockCipherModeFactory>, <xref:Bodu.Security.Cryptography.PaddingFactory>, <xref:Bodu.Security.Cryptography.BlockCipherTransform> — fit together. Every row below was checked against `BlockCipherModeFactory` and the transform classes, and every sample compiles and runs.

> [!NOTE]
> `Bodu.Security.Cryptography` is not independently audited and offers best-effort, not guaranteed, side-channel resistance. For authenticated encryption start from the [AEAD modes](aead-modes.md); the classic modes on this page give confidentiality only.

## The `CipherModeKind` support matrix

`CipherModeKind` has ten members. Five of them are what <xref:Bodu.Security.Cryptography.BlockCipherModeFactory.Create(Bodu.Security.Cryptography.CipherModeKind,Bodu.Security.Cryptography.IBlockCipher,System.Byte[])> can build — and therefore what the wrappers' `BlockMode` property can drive. The other five are declared in the enum but the factory throws `NotSupportedException` for them; they are reachable only by constructing the transform yourself.

| `CipherModeKind` | Value | Through `BlockMode` / `BlockCipherModeFactory` | Direct transform | IV | Input alignment (raw transform) | Padding through the wrapper |
|---|---|---|---|---|---|---|
| `ECB` | 2 | Yes → <xref:Bodu.Security.Cryptography.EcbModeTransform> | `new EcbModeTransform(cipher)` | none (`null` accepted) | whole blocks | yes (default PKCS7) |
| `CBC` | 1 | Yes → <xref:Bodu.Security.Cryptography.CbcModeTransform> | `new CbcModeTransform(cipher, iv)` | = block size | whole blocks | yes |
| `CFB` | 4 | Yes → <xref:Bodu.Security.Cryptography.CfbModeTransform> | `new CfbModeTransform(cipher, iv)` | = block size | whole blocks | yes — `PaddingMode.None` still requires aligned input |
| `OFB` | 3 | Yes → <xref:Bodu.Security.Cryptography.OfbModeTransform> | `new OfbModeTransform(cipher, iv)` | = block size | whole blocks | same as CFB |
| `CTR` | 1024 | Yes → <xref:Bodu.Security.Cryptography.CtrModeTransform> | `new CtrModeTransform(cipher, iv)` | = block size (initial counter) | **partial final block accepted** by the raw transform | `PaddingMode.None` still requires aligned input through the wrapper |
| `CTS` | 5 | **No** — `NotSupportedException` | <xref:Bodu.Security.Cryptography.CtsModeTransform> `(cipher, iv)` | = block size | ≥ one block, any length; output length = input length | n/a (never pad CTS) |
| `XTS` | 2048 | **No** — `NotSupportedException` | <xref:Bodu.Security.Cryptography.XtsModeTransform> `(dataCipher, tweakCipher, sector)` | 16-byte sector number | whole 16-byte blocks only (no ciphertext stealing); 128-bit block ciphers only | n/a |
| `OCB` | 4096 | **No** — `NotSupportedException` | <xref:Bodu.Security.Cryptography.OcbModeTransform> (AEAD) | 16-byte IV, first 12 used | any | n/a |
| `EAX` | 8192 | **No** — `NotSupportedException` | <xref:Bodu.Security.Cryptography.EaxModeTransform> (AEAD) | 16-byte nonce | any | n/a |
| `SIV` | 16384 | **No** — `NotSupportedException` | <xref:Bodu.Security.Cryptography.SivModeTransform> (AEAD) | ignored | any | n/a |
| *(no member)* | — | — | <xref:Bodu.Security.Cryptography.GcmModeTransform>, <xref:Bodu.Security.Cryptography.GcmSivModeTransform>, <xref:Bodu.Security.Cryptography.CcmModeTransform> (AEAD) | 12-byte nonce | any | n/a |

Two consequences worth stating plainly:

- Setting `BlockMode = CipherModeKind.CTS` (or `XTS`, `OCB`, `EAX`, `SIV`) on a wrapper compiles and does not fail at assignment; the `NotSupportedException` surfaces when `CreateEncryptor` / `CreateDecryptor` runs the factory. The [cipher block modes](cipher-modes.md) page's CTS and XTS sections describe the direct transforms.
- The wrapper never produces an unaligned stream-mode ciphertext. Its <xref:Bodu.Security.Cryptography.BlockCipherTransform> pads on `TransformFinalBlock`, and <xref:Bodu.Security.Cryptography.NoPadding> throws `ArgumentException` for input that is not a block multiple. For CTR over an arbitrary length, use the raw `CtrModeTransform` (Pattern 3) or accept PKCS7 padding.

### `BlockMode` and the inherited `Mode`

<xref:Bodu.Security.Cryptography.ExtendedSymmetricAlgorithm> keeps `BlockMode` and the BCL `Mode` in sync where a value exists in both enums (`CBC`, `ECB`, `OFB`, `CFB`, `CTS`). Values with no `CipherMode` equivalent leave `Mode` untouched, so after `BlockMode = CipherModeKind.CTR` the inherited property still reports the previous value. Read `BlockMode`, not `Mode`, to know what a Bodu wrapper will do. The tweakable wrappers (<xref:Bodu.Security.Cryptography.Threefish256> and siblings, <xref:Bodu.Security.Cryptography.Serpent256> and siblings) declare their own `BlockMode` on <xref:Bodu.Security.Cryptography.TweakableSymmetricAlgorithm>-derived bases and do not mirror it into `Mode` at all.

## Pattern 1 — the five wrapper modes

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] key = Convert.FromHexString("000102030405060708090a0b0c0d0e0f");
byte[] iv  = Convert.FromHexString("101112131415161718191a1b1c1d1e1f");
byte[] unaligned = "composition reference: 37 bytes long!!"u8.ToArray();          // 38 bytes
byte[] aligned   = "composition reference: exactly forty-eight bytes"u8.ToArray(); // 48 bytes = 3 blocks

foreach (CipherModeKind mode in new[] { CipherModeKind.ECB, CipherModeKind.CBC, CipherModeKind.CFB, CipherModeKind.OFB, CipherModeKind.CTR })
{
    using var alg = new Camellia { BlockMode = mode, Key = key, IV = iv };
    alg.Padding = mode is CipherModeKind.ECB or CipherModeKind.CBC ? PaddingMode.PKCS7 : PaddingMode.None;

    byte[] input = alg.Padding == PaddingMode.None ? aligned : unaligned;   // None demands block-aligned input
    byte[] ciphertext = alg.Encrypt(input);
    byte[] recovered  = alg.Decrypt(ciphertext);
    // ECB/CBC: 38 -> 48 bytes; CFB/OFB/CTR: 48 -> 48 bytes; all round-trip
}
```

`ECB` is the one mode that accepts a `null` IV; the other four validate `iv.Length == BlockSize / 8` and throw `CryptographicException` otherwise.

## Pattern 2 — composing by hand with the factories

The wrappers are a convenience over three parts you can assemble yourself: an <xref:Bodu.Security.Cryptography.IBlockCipher> engine, an <xref:Bodu.Security.Cryptography.IBlockCipherModeTransform> from `BlockCipherModeFactory`, and an <xref:Bodu.Security.Cryptography.IPaddingStrategy> from `PaddingFactory`. Both factories and the strategies express **block size in bits** — pass `cipher.BlockSize` (128), never the byte count.

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

byte[] key = Convert.FromHexString("000102030405060708090a0b0c0d0e0f");
byte[] iv  = Convert.FromHexString("101112131415161718191a1b1c1d1e1f");
byte[] plaintext = "composition reference: 37 bytes long!!"u8.ToArray();

using IBlockCipher cipher = new CamelliaBlockCipher(key);
using IBlockCipherModeTransform cbc = BlockCipherModeFactory.Create(CipherModeKind.CBC, cipher, iv);
IPaddingStrategy padding = PaddingFactory.Create(PaddingMode.PKCS7);

byte[] padded = padding.Pad(plaintext, cipher.BlockSize);     // blockSize is in BITS: 128, not 16 → 48 bytes
byte[] ciphertext = new byte[padded.Length];
int written = cbc.Transform(padded, ciphertext, encrypt: true);  // 48

// Decrypt with a fresh mode transform (the chaining state is per instance).
using IBlockCipher cipher2 = new CamelliaBlockCipher(key);
using IBlockCipherModeTransform cbc2 = BlockCipherModeFactory.Create(CipherModeKind.CBC, cipher2, iv);
byte[] decrypted = new byte[ciphertext.Length];
cbc2.Transform(ciphertext, decrypted, encrypt: false);
byte[] recovered = padding.Unpad(decrypted, cipher2.BlockSize);
```

`IPaddingStrategy.StripsPaddingOnUnpad` tells a decryptor whether `Unpad` can shorten the output: `true` for PKCS7, ANSI X.923, ISO 10126, and ISO/IEC 7816-4; `false` for `Zeros` and `None`, whose `Unpad` returns the input unchanged. `BlockCipherTransform` uses that flag to hold back the final block during decryption so it can strip padding safely.

## Pattern 3 — CTR with a partial final block

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] key = Convert.FromHexString("000102030405060708090a0b0c0d0e0f");
byte[] iv  = Convert.FromHexString("101112131415161718191a1b1c1d1e1f");
byte[] plaintext = "composition reference: 37 bytes long!!"u8.ToArray();

// Through the wrapper, PaddingMode.None insists on block-aligned input even for CTR.
using var alg = new Camellia { BlockMode = CipherModeKind.CTR, Padding = PaddingMode.None, Key = key, IV = iv };
try { alg.Encrypt(plaintext); }
catch (ArgumentException) { /* "Input must be a multiple of block size when using no padding." */ }

// The raw CtrModeTransform handles the trailing partial block itself.
using IBlockCipher cipher = new CamelliaBlockCipher(key);
using IBlockCipherModeTransform ctr = BlockCipherModeFactory.Create(CipherModeKind.CTR, cipher, iv);
byte[] ciphertext = new byte[plaintext.Length];
int written = ctr.Transform(plaintext, ciphertext, encrypt: true);      // 38 — same length as the input
```

`CfbModeTransform` and `OfbModeTransform` do not: their `Transform` throws `CryptographicException` for input that is not a block multiple.

## Pattern 4 — CTS and XTS as direct transforms

```csharp
using Bodu.Security.Cryptography;

byte[] key = Convert.FromHexString("000102030405060708090a0b0c0d0e0f");
byte[] iv  = Convert.FromHexString("101112131415161718191a1b1c1d1e1f");
byte[] plaintext = "composition reference: 37 bytes long!!"u8.ToArray();

// CTS (CS3 / IEEE 1619 order): no padding, ciphertext length == plaintext length, input >= one block.
using IBlockCipher cipher = new AesBlockCipher(key);
using var cts = new CtsModeTransform(cipher, iv);
byte[] ciphertext = new byte[plaintext.Length];
cts.Transform(plaintext, ciphertext, encrypt: true);

using IBlockCipher cipher2 = new AesBlockCipher(key);
using var cts2 = new CtsModeTransform(cipher2, iv);
byte[] recovered = new byte[ciphertext.Length];
cts2.Transform(ciphertext, recovered, encrypt: false);
```

```csharp
using Bodu.Security.Cryptography;

// XTS: two independently keyed 128-bit-block ciphers and a 16-byte little-endian sector number.
byte[] key1 = Convert.FromHexString("000102030405060708090a0b0c0d0e0f");
byte[] key2 = Convert.FromHexString("f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff");
byte[] sector = new byte[16];
BitConverter.TryWriteBytes(sector, 42L);

byte[] sectorData = new byte[64];                 // whole 16-byte blocks only — no ciphertext stealing
Array.Fill(sectorData, (byte)0xA5);

using IBlockCipher data  = new AesBlockCipher(key1);
using IBlockCipher tweak = new AesBlockCipher(key2);
using var xts = new XtsModeTransform(data, tweak, sector);
byte[] ciphertext = new byte[sectorData.Length];
xts.Transform(sectorData, ciphertext, encrypt: true);   // first block EB3C020DDDDBCF88932217B467047B56
```

An `XtsModeTransform` given input that is not a multiple of 16 bytes throws `CryptographicException`; both ciphers must report a 128-bit block or the constructor throws `ArgumentException`.

## Pattern 5 — the AEAD transforms

`OCB`, `EAX`, and `SIV` appear in `CipherModeKind` but only as names; the factory rejects them. All six block-cipher AEADs implement <xref:Bodu.Security.Cryptography.IAeadBlockCipherModeTransform> (and through it <xref:Bodu.Security.Cryptography.IAeadTransform>), take an `AesBlockCipher`, and are single-use per message:

```csharp
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] key   = Convert.FromHexString("000102030405060708090a0b0c0d0e0f");
byte[] key2  = Convert.FromHexString("f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff");
byte[] nonce = Convert.FromHexString("cafebabefacedbaddecaf888");     // 12 bytes
byte[] iv16  = new byte[16]; nonce.CopyTo(iv16, 0);                    // 16-byte IV whose first 12 bytes are the nonce
byte[] iv    = Convert.FromHexString("101112131415161718191a1b1c1d1e1f");
byte[] plaintext = "composition reference: 37 bytes long!!"u8.ToArray();
byte[] aad = "hdr"u8.ToArray();

using (var c = new AesBlockCipher(key)) _ = new GcmModeTransform(c, nonce).Encrypt(plaintext, associatedData: aad);
using (var c = new AesBlockCipher(key)) _ = new CcmModeTransform(c, iv16).Encrypt(plaintext, associatedData: aad);
using (var c = new AesBlockCipher(key)) _ = new OcbModeTransform(c, iv16, tagSize: 96).Encrypt(plaintext, associatedData: aad);
using (var c = new AesBlockCipher(key)) _ = new EaxModeTransform(c, iv).Encrypt(plaintext, associatedData: aad);
using (var a = new AesBlockCipher(key)) using (var b = new AesBlockCipher(key2))
    _ = new SivModeTransform(a, b, new byte[16]).Encrypt(plaintext, associatedData: aad);
using (var c = new AesBlockCipher(key))
    _ = new GcmSivModeTransform(c, static k => new AesBlockCipher(k), iv16).Encrypt(plaintext, associatedData: aad);
```

> [!WARNING]
> Name the `associatedData:` argument. Each AEAD also has a public `Encrypt(ReadOnlySpan<byte> plaintext, Span<byte> output)` instance method; with two positional `byte[]` arguments that overload wins, writes the ciphertext into your AAD array, and returns an `int`.

## Padding — `PaddingMode`, `PaddingModeKind`, and the dual properties

`PaddingFactory` has two overloads. `Create(PaddingMode)` covers the BCL enum — `PKCS7`, `Zeros`, `None`, `ANSIX923`, `ISO10126` — and throws `CryptographicException` for anything else. `Create(PaddingModeKind)` adds `ISO7816_4` (value 1024), the one scheme the BCL enum cannot name.

<xref:Bodu.Security.Cryptography.ExtendedSymmetricAlgorithm> exposes both: `BlockPadding` (<xref:Bodu.Security.Cryptography.PaddingModeKind>) and the inherited `Padding` (`PaddingMode`). Setting either updates the other when the value exists in both enums; `ISO7816_4` updates only `BlockPadding`. `CreateEncryptor` reads `BlockPadding`, so ISO 7816-4 is honoured.

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

using var alg = new Camellia();
// defaults: BlockMode CBC / Mode CBC, BlockPadding PKCS7 / Padding PKCS7

alg.BlockPadding = PaddingModeKind.ISO7816_4;   // BlockPadding ISO7816_4, Padding still PKCS7 (no BCL value)
alg.Padding = PaddingMode.ANSIX923;             // both ANSIX923
alg.BlockMode = CipherModeKind.CTR;             // BlockMode CTR, Mode still CBC
alg.Mode = CipherMode.ECB;                      // both ECB
```

The tweakable wrappers (`Threefish*`, `Serpent256/512/1024`) derive from `TweakableSymmetricAlgorithm`, not `ExtendedSymmetricAlgorithm`: they have `BlockMode` but only the BCL `Padding` property, so ISO 7816-4 is not selectable on them. [Padding](padding.md) describes what each scheme emits; its `Pad` calls take the block size in bits.

## `BlockCipherTransform` — what the wrappers hand back

`CreateEncryptor()` on every block-cipher wrapper returns a <xref:Bodu.Security.Cryptography.BlockCipherTransform>: an `ICryptoTransform` that owns one engine, one mode transform, and one padding strategy. Its constructors are `protected internal`, so you obtain one from a wrapper or by subclassing (see [Extending the library](extending.md)). Contract, as observed:

| Member | Value |
|---|---|
| `InputBlockSize` / `OutputBlockSize` | the cipher block size in **bytes** (16 for Camellia) |
| `CanTransformMultipleBlocks` | `true` |
| `CanReuseTransform` | `false` — after `TransformFinalBlock`, any further call throws `InvalidOperationException` |
| `TransformBlock` | encrypt: input must be block-aligned; decrypt with a stripping padding: the last block is held back |
| `TransformFinalBlock` | encrypt: pads then encrypts (an empty input still emits a padding block for PKCS7-style schemes); decrypt: validates alignment, decrypts, unpads |
| `Dispose` | disposes the mode transform **and the engine** |

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] key = Convert.FromHexString("000102030405060708090a0b0c0d0e0f");
byte[] iv  = Convert.FromHexString("101112131415161718191a1b1c1d1e1f");

using var alg = new Twofish { Key = key, IV = iv, BlockMode = CipherModeKind.CBC };
using ICryptoTransform encryptor = alg.CreateEncryptor();               // a BlockCipherTransform
byte[] ciphertext = encryptor.Transform("composition reference: 37 bytes long!!"u8.ToArray());   // one-shot extension
// encryptor.InputBlockSize == 16; encryptor.CanReuseTransform == false; a second final block throws InvalidOperationException
```

## The engine contracts

| Type | Members | Notes |
|---|---|---|
| <xref:Bodu.Security.Cryptography.IBlockCipher> | `BlockSize` (bits), `Encrypt(input, output)`, `Decrypt(input, output)`, default `EncryptBlocks` / `DecryptBlocks`, `Dispose` | One block per call; the default block-loop members process whole blocks and ignore a trailing partial block. Engines: `AesBlockCipher(byte[])`, `CamelliaBlockCipher`, `TwofishBlockCipher`, `Serpent128Cipher`, `BlowfishBlockCipher`, `SkipjackBlockCipher` (span key), `Threefish256Cipher` / `Serpent256Cipher` … `(key, tweak)`. |
| <xref:Bodu.Security.Cryptography.IBlockCipherModeTransform> | `Transform(input, output, bool encrypt)`, `Dispose` | Stateful, not thread-safe; a new instance per message. `Dispose` clears chaining state but does **not** dispose the engine. |
| <xref:Bodu.Security.Cryptography.IStreamCipher> | `BlockSize`, `NextKeystreamBlock(Span<byte>)` | The keystream engine behind the stream ciphers. The shipped engines are internal; the interface is public so a <xref:Bodu.Security.Cryptography.Poly1305AeadTransform> subclass can supply its own through `CreateEngine()`. |
| <xref:Bodu.Security.Cryptography.IStreamAeadTransform> | (marker) `: IAeadTransform` | Implemented by `XChaCha20Poly1305`, `XSalsa20Poly1305`, `XSalsa20Poly1305Aead` — see [Authenticated stream ciphers](stream-aead.md). |
| <xref:Bodu.Security.Cryptography.SymmetricStreamAlgorithm> | `Key`, `Nonce`, `KeySize`, `NonceSize`, `GenerateKey`, `GenerateNonce`, `CreateEncryptor` / `CreateDecryptor` / `CreateTransform` | The stream-cipher wrapper base (not a `SymmetricAlgorithm`). The parameterless `Create*` overloads allow **one transform per nonce**; pass `(key, nonce)` explicitly to create a second (decrypting) transform. |
| <xref:Bodu.Security.Cryptography.TransformMode> | `Encrypt`, `Decrypt` | A public enum that no public member currently consumes; the transforms take a `bool encrypt` instead. |

## API summary

| Type | Role |
|---|---|
| <xref:Bodu.Security.Cryptography.CipherModeKind> | Ten mode names; five are factory-buildable. |
| <xref:Bodu.Security.Cryptography.BlockCipherModeFactory> | `Create(mode, cipher, iv)` → ECB / CBC / CFB / OFB / CTR; `NotSupportedException` otherwise; `ArgumentException` for a missing or mis-sized IV. |
| <xref:Bodu.Security.Cryptography.PaddingFactory> / <xref:Bodu.Security.Cryptography.PaddingModeKind> | `Create(PaddingMode)` (five schemes) and `Create(PaddingModeKind)` (adds ISO 7816-4); `CryptographicException` for unknown values. |
| <xref:Bodu.Security.Cryptography.IPaddingStrategy> | `Pad(input, blockSizeBits)`, `Unpad(input, blockSizeBits)`, `StripsPaddingOnUnpad`. |
| <xref:Bodu.Security.Cryptography.ExtendedSymmetricAlgorithm> | `BlockMode` ↔ `Mode`, `BlockPadding` ↔ `Padding` synchronization for the standard wrappers. |
| <xref:Bodu.Security.Cryptography.BlockCipherTransform> | The `ICryptoTransform` the wrappers return; `protected internal` constructors. |
| <xref:Bodu.Security.Cryptography.CtsModeTransform>, <xref:Bodu.Security.Cryptography.XtsModeTransform> | Direct-only modes. |

## Where to go next

- [Composing primitives](composing-primitives.md) — the manual-versus-wrapper walk-through this page tabulates.
- [Cipher block modes](cipher-modes.md) — one round trip per mode with the IV rules.
- [Padding](padding.md) — what each scheme emits and the padding-oracle caveat.
- [AEAD modes](aead-modes.md) — the six authenticated transforms in depth.
- [Extending the library](extending.md) — a custom `IBlockCipher` behind `BlockCipherTransform`.
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
