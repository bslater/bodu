---
title: Using Serpent
---

# Using Serpent

Serpent is the AES finalist by Anderson, Biham, and Knudsen: a 32-round substitution–permutation network over a 128-bit block with 128-, 192-, or 256-bit keys. `Bodu.Security.Cryptography` ships it in two very different forms. <xref:Bodu.Security.Cryptography.Serpent128> is the standard cipher, verified against the NESSIE submission vectors. <xref:Bodu.Security.Cryptography.Serpent256>, <xref:Bodu.Security.Cryptography.Serpent512>, and <xref:Bodu.Security.Cryptography.Serpent1024> are **Bodu-defined, non-standard, tweakable wide-block constructions** that reuse Serpent's S-boxes over a wider state — they interoperate with nothing but themselves.

> [!NOTE]
> Like the rest of the library, these implementations are not independently audited and offer best-effort, not guaranteed, side-channel resistance. The source states that control flow is constant-time but each 4-bit S-box substitution reads a 16-byte table at a data-dependent index, so the implementation is **not** hardened against cache-timing attacks.

## The family at a glance

| Type | Role | Block | Key | Tweak | Rounds | Standard |
|---|---|---|---|---|---|---|
| <xref:Bodu.Security.Cryptography.Serpent128> | `SymmetricAlgorithm` wrapper (an <xref:Bodu.Security.Cryptography.ExtendedSymmetricAlgorithm>) | 128 bits | 128 (default) / 192 / 256 bits | — | 32 | Serpent (NESSIE vectors) |
| <xref:Bodu.Security.Cryptography.Serpent128Cipher> | raw <xref:Bodu.Security.Cryptography.IBlockCipher> engine | 128 bits | 16 / 24 / 32 bytes | — | 32 | Serpent |
| <xref:Bodu.Security.Cryptography.Serpent256> / <xref:Bodu.Security.Cryptography.Serpent256Cipher> | tweakable wrapper (<xref:Bodu.Security.Cryptography.Serpent>) / engine | 256 bits | 256 bits | 128 bits | 48 | **Bodu-only** |
| <xref:Bodu.Security.Cryptography.Serpent512> / <xref:Bodu.Security.Cryptography.Serpent512Cipher> | tweakable wrapper / engine | 512 bits | 512 bits | 128 bits | 64 | **Bodu-only** |
| <xref:Bodu.Security.Cryptography.Serpent1024> / <xref:Bodu.Security.Cryptography.Serpent1024Cipher> | tweakable wrapper / engine | 1024 bits | 1024 bits | 128 bits | 80 | **Bodu-only** |

The wide variants take a key exactly as long as their block and a fixed 16-byte tweak; the engines expose `KeySize` constants (256 / 512 / 1024) and the wrappers report `LegalTweakSizes` of exactly 128 bits. <xref:Bodu.Security.Cryptography.SerpentBlockCipherBase> is the shared abstract base (S-boxes, linear transform, disposal) under both engine families, and <xref:Bodu.Security.Cryptography.SerpentBlockCipher> is the abstract wide-block engine that `Serpent256Cipher` and siblings specialize.

## Pattern 1 — a known-answer check on the raw engine

<xref:Bodu.Security.Cryptography.Serpent128Cipher> is the primitive: one block in, one block out. The Serpent NESSIE set 1 vector 0 (all-zero key and plaintext) is the quickest way to confirm you have standard Serpent:

```csharp
using Bodu.Security.Cryptography;

using IBlockCipher cipher = new Serpent128Cipher(new byte[16]);      // 16-, 24-, or 32-byte key
byte[] block = new byte[16];
byte[] ciphertext = new byte[16];
cipher.Encrypt(block, ciphertext);
// 3620B17AE6A993D09618B8768266BAE9

byte[] recovered = new byte[16];
cipher.Decrypt(ciphertext, recovered);                                // back to the zero block

using IBlockCipher keyed = new Serpent128Cipher(Convert.FromHexString("000102030405060708090A0B0C0D0E0F"));
keyed.Encrypt(Convert.FromHexString("33B3DC87EDDD9B0F6A1F407D14919365"), ciphertext);
// 00112233445566778899AABBCCDDEEFF  (NESSIE set 1 vector 9)
```

An engine validates lengths strictly: a key that is not 16, 24, or 32 bytes throws `ArgumentException` (`key`), and `Encrypt` / `Decrypt` throw `ArgumentException` unless both spans are exactly 16 bytes.

## Pattern 2 — Serpent-128 through the wrapper

`Serpent128` behaves like every other <xref:Bodu.Security.Cryptography.ExtendedSymmetricAlgorithm>: set `Key`, `IV`, `BlockMode`, and `Padding`, then use `CreateEncryptor` / `CryptoStream` or the one-shot extensions. `Serpent128.Create()` is the factory-style equivalent of `new Serpent128()`. The default key size is 128 bits; set `KeySize` **before** assigning or generating a longer key.

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] plaintext = "Serpent-128 through the SymmetricAlgorithm wrapper"u8.ToArray();

using var alg = Serpent128.Create();
alg.KeySize = 256;                                                   // 128 (default), 192, or 256
alg.Key = Convert.FromHexString("000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f");
alg.IV  = Convert.FromHexString("101112131415161718191a1b1c1d1e1f");
alg.BlockMode = CipherModeKind.CBC;
alg.Padding = PaddingMode.PKCS7;

byte[] ciphertext = alg.Encrypt(plaintext);                          // 64 bytes: 040E605C…848BCFAC
byte[] recovered  = alg.Decrypt(ciphertext);
```

`LegalKeySizes` reports 128–256 in 64-bit steps. Because it is a 128-bit-block cipher, `Serpent128Cipher` is *not* accepted by the AEAD transforms — those are written for <xref:Bodu.Security.Cryptography.AesBlockCipher> only — but every classic mode and padding scheme in [Modes, transforms, and factories](cipher-composition-reference.md) applies.

## Pattern 3 — the wide-block, tweakable variants

The three wide variants derive from <xref:Bodu.Security.Cryptography.Serpent>, itself a <xref:Bodu.Security.Cryptography.TweakableSymmetricAlgorithm>. They add `Tweak` / `GenerateTweak()` and the three-argument `CreateEncryptor(key, iv, tweak)`, and carry their own `BlockMode` property (the inherited `Mode` is not synchronized). Each variant has a static `Create()`.

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] plaintext = "wide-block, tweakable, Bodu-only"u8.ToArray();   // 32 bytes = exactly one block

using var alg = Serpent256.Create();
alg.Key   = Convert.FromHexString("000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f");   // 32 bytes = block size
alg.IV    = Convert.FromHexString("202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f");   // 32 bytes
alg.Tweak = Convert.FromHexString("404142434445464748494a4b4c4d4e4f");                                   // 16 bytes, always
alg.BlockMode = CipherModeKind.CTR;
alg.Padding = PaddingMode.None;

byte[] ciphertext = alg.Encrypt(plaintext);      // A63B7A5F4029BFA7E86CEF5834AB9097A83E57EF51C100D502945EC189B957C3
byte[] recovered  = alg.Decrypt(ciphertext);

alg.Tweak = Convert.FromHexString("404142434445464748494a4b4c4d4e50");   // one bit of tweak → unrelated ciphertext
byte[] other = alg.Encrypt(plaintext);           // B8B27397…351E678C
```

The tweak is injected into the state after every fourth round together with a round counter, so — as with Threefish — it separates *domains* under one key rather than replacing the IV. [Using Threefish-256](threefish-256.md#what-the-tweak-is--and-why-it-is-not-an-iv) explains the IV-versus-tweak distinction; it applies verbatim here.

The raw engines take the key and tweak together and drop into `BlockCipherModeFactory` like any other `IBlockCipher`:

```csharp
using Bodu.Security.Cryptography;

byte[] key = new byte[32], tweak = new byte[16], iv = new byte[32];
using IBlockCipher cipher = new Serpent256Cipher(key, tweak);        // Serpent512Cipher: 64-byte key; Serpent1024Cipher: 128-byte key
using IBlockCipherModeTransform ctr = BlockCipherModeFactory.Create(CipherModeKind.CTR, cipher, iv);

byte[] plaintext = "raw engine + CTR"u8.ToArray();
byte[] ciphertext = new byte[plaintext.Length];
ctr.Transform(plaintext, ciphertext, encrypt: true);                 // 0BC34B78EC9E17A0F388E6E7B845F6C8
```

A wrong key length throws `ArgumentException` (`key`); a tweak that is not 16 bytes throws `ArgumentException` (`tweak`). All-zero key, tweak, and plaintext encrypt under Serpent-256 to `79A23C5889F070C99DEDC6CC9806A29A98A3F2B854B61D719C1FA832ADF900D0` — a value you can pin in your own tests, but not one any other implementation will reproduce.

## When not to use the wide variants

> [!WARNING]
> `Serpent256`, `Serpent512`, and `Serpent1024` are experimental constructions defined by this library. There is no specification, no third-party implementation, no published cryptanalysis, and no test vector outside this repository. Do not use them where interoperability, external review, or a compliance regime matters. For a standard cipher use `Serpent128` or, preferably, AES; for a *reviewed* tweakable wide-block cipher use [Threefish](threefish-256.md), which comes from the Skein SHA-3 submission and carries published vectors.

They exist for experiments that want Serpent's S-box structure over a wider state — for example comparing wide-block behaviour against Threefish under identical modes. Treat any ciphertext they produce as tied to this library's version.

## API summary

| Member | Serpent-128 | Serpent-256 / 512 / 1024 |
|---|---|---|
| Wrapper type | `Serpent128 : ExtendedSymmetricAlgorithm` | `Serpent256` … `: Serpent : TweakableSymmetricAlgorithm` |
| Engine type | `Serpent128Cipher(ReadOnlySpan<byte> key)` | `Serpent256Cipher(key, tweak)` … |
| `Create()` | `Serpent128.Create()` | `Serpent256.Create()`, `Serpent512.Create()`, `Serpent1024.Create()` (none on the abstract `Serpent`) |
| Block / key sizes | 128 / 128–256 step 64 | block = key = 256 / 512 / 1024 |
| Tweak | — | `Tweak`, `TweakSize` (128), `LegalTweakSizes`, `GenerateTweak()` |
| Mode selection | `BlockMode` (synced with `Mode`) + `BlockPadding` / `Padding` | own `BlockMode`; BCL `Padding` only |
| Transforms | `CreateEncryptor(key, iv)` | `CreateEncryptor(key, iv, tweak)` and the parameterless overload |

## Where to go next

- [AES-family block ciphers](aes-family.md) — Serpent-128 beside AES, Twofish, and Camellia.
- [Using Threefish-256](threefish-256.md) — the reviewed tweakable alternative.
- [Modes, transforms, and factories](cipher-composition-reference.md) — what the wrappers and engines plug into.
- [Security guarantees and limitations](security-posture.md) — the table-lookup timing caveat in context.
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
