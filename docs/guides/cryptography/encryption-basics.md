---
title: Encryption basics
---

# Encryption basics

This page introduces the mental model that every cipher in the library follows. If you know `System.Security.Cryptography.SymmetricAlgorithm` from the BCL, most of this will feel familiar — with three twists:

1. **`BlockMode`** replaces `Mode`. The inherited `Mode` property (type <xref:System.Security.Cryptography.CipherMode>) only knows about the modes the BCL defined. Bodu ciphers expose a new `BlockMode` property of type <xref:Bodu.Security.Cryptography.CipherModeKind>, which adds `CTR`, `OFB`, and friends. *Set `BlockMode`, not `Mode`.*
2. **Tweak** is a first-class input for Threefish. Threefish is a *tweakable* block cipher; each call is parameterized by a key, an IV, **and** a 128-bit tweak that acts as a domain-separation label.
3. **Key / IV / Tweak are lazily generated.** If you never set them, they are materialized on first read from a cryptographically secure RNG. Read the property, or call `GenerateKey()` / `GenerateIV()` / `GenerateTweak()` explicitly.

## Anatomy of an encryption

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

// 1. Choose and configure the algorithm.
using var alg = new Threefish256();
alg.BlockMode = CipherModeKind.CBC;   // how blocks chain
alg.Padding   = PaddingMode.PKCS7;     // how the last (partial) block is filled

// 2. Produce key material. This is cryptographically random.
alg.GenerateKey();                     // 32 bytes for Threefish-256
alg.GenerateIV();                      // 32 bytes, matches the block size
alg.GenerateTweak();                   // 16 bytes, Threefish-specific

// 3. Encrypt.
byte[] plaintext  = System.Text.Encoding.UTF8.GetBytes("hello, world");
byte[] ciphertext;
using (ICryptoTransform enc = alg.CreateEncryptor())
    ciphertext = enc.TransformFinalBlock(plaintext, 0, plaintext.Length);

// 4. Decrypt using the same Key, IV, and Tweak.
byte[] recovered;
using (ICryptoTransform dec = alg.CreateDecryptor())
    recovered = dec.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

Debug.Assert(plaintext.SequenceEqual(recovered));
```

The four numbered steps above are the shape of **every** encryption in the library.

## Key, IV, Tweak — what each is for

| Input | Role | Secret? | Reuse across messages? |
|---|---|---|---|
| **Key** | Selects which permutation family to use. | **Yes.** Never transmit or log in the clear. | Yes, within a rotation policy (e.g. rotate per month / per volume). |
| **IV** (initialization vector) | Randomizes the ciphertext so two messages with the same key and plaintext encrypt to different ciphertexts. | No — the IV travels with the ciphertext. | **No.** Must be unique per message under a given key. For `CBC` it must also be *unpredictable*. For `CTR` / `OFB` reuse is catastrophic. |
| **Tweak** (Threefish only) | Domain separator. Encrypting the same plaintext under the same key but a different tweak yields unrelated ciphertext. | No — treat like an IV. | Depends on use. For generic encryption, it behaves like an auxiliary IV; for disk encryption-style uses, it encodes the sector/record number. |

## Using the extension methods

The repetitive `CreateEncryptor()` / `TransformFinalBlock()` dance can be collapsed to one call with the `Encrypt` / `Decrypt` extension methods in `Bodu.Security.Cryptography.Extensions`:

```csharp
using Bodu.Security.Cryptography.Extensions;

using var alg = new Threefish256 { BlockMode = CipherModeKind.CBC, Padding = PaddingMode.PKCS7 };
alg.GenerateKey();
alg.GenerateIV();
alg.GenerateTweak();

byte[] ciphertext = alg.Encrypt(plaintext);
byte[] recovered  = alg.Decrypt(ciphertext);
```

Both extension methods also have overloads for `Stream` sources and destinations, so you can encrypt a file in one call:

```csharp
using var src = File.OpenRead("plaintext.bin");
using var dst = File.Create("cipher.bin");
int bytesRead = alg.Encrypt(src, dst, bufferSize: 4096);
```

## Lazy material generation

Reading the `Key`, `IV`, or `Tweak` property on an un-initialized algorithm **allocates random bytes**. That is usually what you want for a fresh encryption, but it means that:

- Reading `alg.Key` once and then encrypting is safe — the same bytes are cached in `KeyValue` and used on the next access.
- Reading `alg.Key` *before* decrypting a previously-encrypted message will silently generate a **different** key. Decryption will then fail with garbled output or a padding error. Always re-assign the exact bytes used at encryption time.

```csharp
// ✗ Wrong — this generates a NEW key for decryption.
byte[] cipher = alg.Encrypt(plaintext);
using var fresh = new Threefish256();
byte[] recovered = fresh.Decrypt(cipher);  // fails: wrong key

// ✓ Right — carry Key/IV/Tweak across the boundary.
byte[] key = alg.Key, iv = alg.IV, tweak = alg.Tweak;
byte[] cipher = alg.Encrypt(plaintext);
// …store (cipher, iv, tweak) next to the message; keep key in a vault.

using var fresh = new Threefish256 { Key = key, IV = iv, Tweak = tweak };
byte[] recovered = fresh.Decrypt(cipher);
```

## Storage layout

A common convention, used by many protocols, is to prepend the IV (and, for Threefish, the tweak) to the ciphertext:

```text
┌────────┬──────────┬──────────────────┐
│   IV   │  tweak   │    ciphertext    │
│ B bytes│ 16 bytes │        …         │
└────────┴──────────┴──────────────────┘
```

The receiver knows the cipher's block size, so it can slice the fixed-width prefix off and then pass the remaining ciphertext through the decryptor with the recovered IV and tweak. The key is **not** in this envelope — it lives separately in a secrets store.

## Disposal

Every `SymmetricAlgorithm` holds sensitive material (the expanded key schedule, the IV, intermediate buffers). Always wrap in `using`:

```csharp
using var alg = new Threefish256();
// …
// alg is disposed here; its internal state is zeroed.
```

`ICryptoTransform` instances returned from `CreateEncryptor()` / `CreateDecryptor()` are also `IDisposable` — wrap them too, unless you're using the extension methods which already do.

## Common pitfalls

- **Reusing a `(Key, IV)` pair** in CTR / OFB / CFB completely breaks confidentiality: the XOR of two ciphertexts recovers the XOR of the plaintexts.
- **Reusing a `Key` in ECB** makes identical plaintext blocks produce identical ciphertext blocks — structure leaks.
- **Using `PaddingMode.None` with an unpadded plaintext** will throw if the plaintext length is not a multiple of the block size. See [Padding](padding.md) for details.
- **Logging the key** (e.g. via `Convert.ToHexString(alg.Key)`) makes the key available to anyone with log access. Don't.
- **Holding the algorithm instance alive longer than needed** keeps the expanded key schedule in memory. Dispose as soon as encryption finishes.

## Where to go next

- [Cipher block modes](cipher-modes.md) — ECB, CBC, CFB, OFB, CTR side by side.
- [Padding](padding.md) — which scheme for which situation.
- Per-algorithm: [Threefish-256](threefish-256.md) · [Threefish-512](threefish-512.md) · [Threefish-1024](threefish-1024.md) · [Skipjack](skipjack.md) · [Blowfish](blowfish.md).
