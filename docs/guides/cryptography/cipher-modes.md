---
title: Cipher block modes
---

# Cipher block modes

This page walks through each of the five classic block cipher modes exposed via <xref:Bodu.Security.Cryptography.CipherModeKind>, shows a complete encrypt-and-decrypt round-trip for each, and calls out the IV rules and security trade-offs.

For the data-flow visualisation, see the panels on the <xref:Bodu.Security.Cryptography.CipherModeKind> API page. Each panel in that diagram corresponds to one section below.

## CBC — the default

**Use for:** general-purpose encryption of a whole message where you care about confidentiality only, and the IV is generated fresh per message.

**IV:** same length as the block, **unpredictable** (not just unique).

**Padding:** required for non-block-aligned plaintext. PKCS7 is the safe default.

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] plaintext = System.Text.Encoding.UTF8.GetBytes("confidential payload");

using var alg = new Threefish256
{
    BlockMode = CipherModeKind.CBC,
    Padding   = PaddingMode.PKCS7,
};
alg.GenerateKey();
alg.GenerateIV();
alg.GenerateTweak();

byte[] ciphertext = alg.Encrypt(plaintext);
byte[] recovered  = alg.Decrypt(ciphertext);
```

The IV must travel with the ciphertext so the receiver can decrypt. Don't put it in a predictable field (a counter, a timestamp) — that breaks CBC's security argument. `GenerateIV()` pulls from the platform CSPRNG, which is fine.

## CTR — parallel, seekable, stream-shaped

**Use for:** random-access scenarios (disk-like), high-throughput pipelines where you want to parallelise encryption per block, or when the plaintext length isn't known up front and you don't want padding.

**IV (counter):** same length as the block. **Must be unique per message under a given key.** Reuse is catastrophic.

**Padding:** not required — `PaddingMode.None` is valid, because CTR turns the block cipher into a stream cipher. The ciphertext is the same length as the plaintext.

```csharp
using var alg = new Threefish256
{
    BlockMode = CipherModeKind.CTR,
    Padding   = PaddingMode.None,   // CTR is a stream cipher
};
alg.GenerateKey();
alg.GenerateIV();                    // used as the initial counter block
alg.GenerateTweak();

byte[] ciphertext = alg.Encrypt(plaintext);
byte[] recovered  = alg.Decrypt(ciphertext);
```

**Design note.** The counter wraps after 2^(block_size) encryptions. `CtrModeTransform` detects this and throws <xref:System.Security.Cryptography.CryptographicException> rather than silently reusing keystream. In practice the block sizes on this library (64 to 1024 bits) are more than enough for any real workload, but this guard is your safety net.

## CFB — self-synchronising stream cipher

**Use for:** byte-streamed encryption where a small transmission error should self-heal within a block or two.

**IV:** same length as the block, unique per message. Unpredictability helps but is not strictly required.

**Padding:** not required if the plaintext is block-aligned, but PKCS7 works.

```csharp
using var alg = new Threefish256
{
    BlockMode = CipherModeKind.CFB,
    Padding   = PaddingMode.None,
};
alg.GenerateKey();
alg.GenerateIV();
alg.GenerateTweak();

byte[] ciphertext = alg.Encrypt(plaintext);
byte[] recovered  = alg.Decrypt(ciphertext);
```

Both directions use the cipher's *encryption* primitive — the decryptor never calls `Decrypt` on the underlying block cipher. That's a property of all CFB/OFB/CTR modes and matters if you've only implemented the encrypt path on a custom engine.

## OFB — synchronous stream cipher

**Use for:** stream encryption where bit-level error propagation must not happen (a single flipped bit in the ciphertext produces a single flipped bit in the plaintext — no cascade).

**IV:** same length as the block, **unique per message**. IV reuse under the same key is catastrophic because the keystream becomes identical.

**Padding:** not required.

```csharp
using var alg = new Threefish256
{
    BlockMode = CipherModeKind.OFB,
    Padding   = PaddingMode.None,
};
alg.GenerateKey();
alg.GenerateIV();
alg.GenerateTweak();

byte[] ciphertext = alg.Encrypt(plaintext);
byte[] recovered  = alg.Decrypt(ciphertext);
```

OFB is rarely the best choice today — CTR gives you the same stream-cipher behaviour *plus* random access. Reach for OFB only when you have a specific interoperability requirement.

## ECB — almost never

**Use for:** a primitive inside a higher-level construction (a custom AEAD, a MAC), and nothing else.

**IV:** none.

**Padding:** required for non-block-aligned plaintext; PKCS7.

```csharp
using var alg = new Threefish256
{
    BlockMode = CipherModeKind.ECB,
    Padding   = PaddingMode.PKCS7,
};
alg.GenerateKey();
alg.GenerateTweak();
// No IV is needed for ECB.

byte[] ciphertext = alg.Encrypt(plaintext);
byte[] recovered  = alg.Decrypt(ciphertext);
```

ECB encrypts every block independently. That means identical plaintext blocks encrypt to identical ciphertext blocks — an attacker can see the structure of your message without breaking the cipher. The classic *Tux the penguin* demonstration shows why this matters. Do not use ECB for real messages.

## Choosing a mode — quick guide

| If you need… | Use | Why |
|---|---|---|
| A safe default for a single message with a random IV | **CBC** | Confidentiality with simple assumptions. |
| Seekable, parallelisable, stream-shaped encryption | **CTR** | Keystream depends only on counter; blocks are independent. |
| Byte-level streaming with self-healing on errors | **CFB** | Error propagates for one block then resynchronises. |
| Bit-exact error isolation on an unreliable channel | **OFB** | Keystream is plaintext-independent. |
| A cipher primitive for something you're building | **ECB** | The lowest level; you must add your own chaining. |

## Where to go next

- [Padding](padding.md) — which padding scheme to pair with which mode.
- [Encryption basics](encryption-basics.md) — Key/IV/Tweak lifecycle, disposal, and common pitfalls.
- Per-algorithm: [Threefish-256](threefish-256.md) · [Skipjack](skipjack.md) · [Blowfish](blowfish.md).
