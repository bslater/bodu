---
title: ASCON authenticated encryption — AsconAead128
---

# ASCON authenticated encryption — AsconAead128

**Authenticated encryption with associated data (AEAD)** combines confidentiality with
integrity: the recipient can verify that neither the ciphertext nor the associated metadata has
been altered since the sender encrypted it.

<xref:Bodu.Security.Cryptography.AsconAead128> implements `ASCON-AEAD128` as standardized in
NIST SP 800-232. It uses a 128-bit key, a 128-bit nonce, and appends a 128-bit authentication
tag to every encrypted message.

| Constant | Value | Meaning |
|---|---|---|
| `AsconAead128.KeyBytes` | 16 | Key length in bytes (128 bits). |
| `AsconAead128.NonceBytes` | 16 | Nonce length in bytes (128 bits). Must be unique per message. |
| `AsconAead128.TagBytes` | 16 | Authentication tag length in bytes (128 bits). |

## How it works

ASCON-AEAD128 is a sponge-based AEAD built on the same 320-bit state and Ascon-p permutation
used by the hash and XOF variants. Every encryption proceeds through four fixed phases:

1. **Initialization** — the state is loaded as `[IV ‖ K₀ ‖ K₁ ‖ N₀ ‖ N₁]` and Ascon-p12 is
   applied, followed by XORing the key into state words S₃ and S₄.
2. **Associated-data absorption** — each 16-byte block of AAD is absorbed with Ascon-p8 between
   blocks. A domain-separation constant (XOR of 1 into S₄) is always applied after the AD phase,
   even when AD is empty.
3. **Encryption / decryption** — plaintext or ciphertext is processed in 16-byte blocks with
   Ascon-p8 between blocks. The final partial block is Ascon-padded and absorbed without a
   trailing permutation.
4. **Finalization** — the key is injected into the state, Ascon-p12 is applied, and the
   128-bit tag is extracted by XORing the key into state words S₃ and S₄.

## The single-use instance contract

Every `AsconAead128` instance is **single-use** — one key/nonce pair, one message. The mandatory
call sequence is:

1. `new AsconAead128(key, nonce)` — construct.
2. `ProcessAssociatedData(aad)` — always call this, even with an empty span.
3. `Encrypt(plaintext, output)` **or** `Decrypt(ciphertextWithTag, output)` — one call only.
4. `Dispose()` — let the `using` statement handle this.

Skipping `ProcessAssociatedData` throws `InvalidOperationException`. Calling it a second time on
the same instance also throws. Construct a new instance for every message.

## Pattern 1 — encrypt with no associated data

```csharp
using System.Security.Cryptography;
using System.Text;
using Bodu.Security.Cryptography;

byte[] key       = RandomNumberGenerator.GetBytes(AsconAead128.KeyBytes);
byte[] nonce     = RandomNumberGenerator.GetBytes(AsconAead128.NonceBytes);
byte[] plaintext = Encoding.UTF8.GetBytes("secret payload");

// Output buffer: ciphertext is the same length as plaintext, tag follows immediately.
byte[] cipherWithTag = new byte[plaintext.Length + AsconAead128.TagBytes];

using (var enc = new AsconAead128(key, nonce))
{
    enc.ProcessAssociatedData(ReadOnlySpan<byte>.Empty);
    enc.Encrypt(plaintext, cipherWithTag);
}
```

`Encrypt` returns the total number of bytes written (`plaintext.Length + TagBytes`).

## Pattern 2 — decrypt and verify

`Decrypt` throws <xref:System.Security.Cryptography.CryptographicException> if the authentication
tag does not match. The output buffer is zeroed before the exception is thrown, so no
unauthenticated bytes can escape the method.

```csharp
byte[] recovered = new byte[cipherWithTag.Length - AsconAead128.TagBytes];

try
{
    using (var dec = new AsconAead128(key, nonce))
    {
        dec.ProcessAssociatedData(ReadOnlySpan<byte>.Empty);
        dec.Decrypt(cipherWithTag, recovered);
    }
    // recovered == original plaintext
}
catch (CryptographicException)
{
    // Tag did not verify — the ciphertext, tag, or AAD has been tampered with.
    // recovered is zeroed. Do not use it.
}
```

> [!WARNING]
> **Never act on output from `Decrypt` before it returns.** Only read `recovered` after
> `Decrypt` has returned without throwing. The method zeroes the output buffer before throwing
> on a tag mismatch — unauthenticated bytes never escape, but the buffer contents are undefined
> until the method returns normally.

## Pattern 3 — with associated data

Associated data is authenticated but **not** encrypted. Use it for any metadata that must travel
alongside the ciphertext in the clear but must not be tampered with — packet headers, record
types, user IDs, protocol framing.

```csharp
using System.Text;
using Bodu.Security.Cryptography;

byte[] key       = RandomNumberGenerator.GetBytes(AsconAead128.KeyBytes);
byte[] nonce     = RandomNumberGenerator.GetBytes(AsconAead128.NonceBytes);
byte[] aad       = Encoding.UTF8.GetBytes("user-id:42|record-type:invoice");
byte[] plaintext = GetInvoiceBytes();

byte[] cipherWithTag = new byte[plaintext.Length + AsconAead128.TagBytes];

using (var enc = new AsconAead128(key, nonce))
{
    enc.ProcessAssociatedData(aad);
    enc.Encrypt(plaintext, cipherWithTag);
}

// Decryption must supply the same AAD. Supplying different AAD causes
// Decrypt to throw CryptographicException — even if the ciphertext is intact.
byte[] recovered = new byte[plaintext.Length];
using (var dec = new AsconAead128(key, nonce))
{
    dec.ProcessAssociatedData(aad);
    dec.Decrypt(cipherWithTag, recovered);
}
```

The AAD is public — it does not need to be secret. Its integrity is protected by the tag.

## Pattern 4 — multi-block plaintext

The rate is 16 bytes (128 bits). Plaintexts longer than 16 bytes are automatically split into
full blocks with Ascon-p8 applied between them; the API is unchanged regardless of plaintext
length.

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

byte[] key   = RandomNumberGenerator.GetBytes(AsconAead128.KeyBytes);
byte[] nonce = RandomNumberGenerator.GetBytes(AsconAead128.NonceBytes);

// 537 bytes — 33 full 16-byte blocks plus a 9-byte final partial block.
byte[] plaintext = new byte[537];
RandomNumberGenerator.Fill(plaintext);

byte[] cipherWithTag = new byte[plaintext.Length + AsconAead128.TagBytes];
using (var enc = new AsconAead128(key, nonce))
{
    enc.ProcessAssociatedData(ReadOnlySpan<byte>.Empty);
    int written = enc.Encrypt(plaintext, cipherWithTag);
    // written == plaintext.Length + AsconAead128.TagBytes
}
```

## Pattern 5 — encrypting an empty plaintext

An empty plaintext produces only the 16-byte authentication tag:

```csharp
using Bodu.Security.Cryptography;

byte[] tag = new byte[AsconAead128.TagBytes];

using (var enc = new AsconAead128(key, nonce))
{
    enc.ProcessAssociatedData(aad);
    enc.Encrypt(ReadOnlySpan<byte>.Empty, tag);
}
```

This pattern is useful for authenticating metadata alone, with no payload.

## Pattern 6 — key and nonce generation

Keys must be generated from a cryptographically secure random source and kept secret:

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

// Generated once (per session, per record, or per application — depends on your key management).
byte[] key = RandomNumberGenerator.GetBytes(AsconAead128.KeyBytes);

// Generated fresh for every message. Never reuse a (key, nonce) pair.
byte[] nonce = RandomNumberGenerator.GetBytes(AsconAead128.NonceBytes);
```

A 128-bit random nonce has negligible collision probability for fewer than approximately 2⁶⁴
messages under the same key. For higher message volumes, use a counter instead:

```csharp
// 128-bit counter encoded into the nonce. Increment for every message; never wrap.
byte[] nonce = new byte[AsconAead128.NonceBytes];
BinaryPrimitives.WriteUInt64LittleEndian(nonce, messageCounter++);
```

Either approach is valid. The critical invariant is that `(key, nonce)` must never repeat across
two distinct messages under the same key.

## Pattern 7 — span-based constructors

The primary constructor accepts `ReadOnlySpan<byte>` for zero-allocation key/nonce loading.
The array-based overload (`byte[]`, `byte[]`) is provided for convenience when you already have
managed arrays:

```csharp
using Bodu.Security.Cryptography;

// Span constructor — no defensive copy of a managed array.
ReadOnlySpan<byte> keySpan   = stackalloc byte[AsconAead128.KeyBytes];
ReadOnlySpan<byte> nonceSpan = stackalloc byte[AsconAead128.NonceBytes];
using var enc = new AsconAead128(keySpan, nonceSpan);

// Array constructor — null-check included; a span is taken from the array internally.
byte[] keyArr   = new byte[AsconAead128.KeyBytes];
byte[] nonceArr = new byte[AsconAead128.NonceBytes];
using var enc2  = new AsconAead128(keyArr, nonceArr);
```

Both constructors reject keys or nonces that are not exactly 16 bytes, throwing
`ArgumentException`.

## Security requirements

| Requirement | Detail |
|---|---|
| **Key secrecy** | The key must never be disclosed. Exposure breaks both confidentiality and authenticity. |
| **Nonce uniqueness** | Never encrypt two distinct messages under the same `(key, nonce)` pair. Nonce reuse leaks the XOR of the two plaintexts and enables tag forgery. |
| **Constant-time tag check** | `Decrypt` uses `CryptographicOperations.FixedTimeEquals` internally. Do not extract and compare tags manually with `SequenceEqual`. |
| **Single-use instances** | Construct a new `AsconAead128` for each message. Calling `Encrypt` or `Decrypt` twice on the same instance is not supported — `ProcessAssociatedData` throws on a second call. |
| **Reject on tag mismatch** | When `Decrypt` throws `CryptographicException`, discard the entire message. Do not retry with a different key or partial output. |

## ASCON-AEAD128 vs AES-GCM

| Consideration | `AsconAead128` | AES-GCM (BCL `AesGcm`) |
|---|---|---|
| NIST standard | SP 800-232 (2025) | SP 800-38D |
| Key / nonce size | 128 bit / 128 bit | 128–256 bit / 96 bit |
| Tag size | 128 bits (fixed) | 96–128 bits (configurable) |
| Hardware acceleration | Software only on most platforms | AES-NI + PCLMULQDQ on x86-64; hardware on ARM |
| Nonce reuse consequence | Catastrophic (XOR leak + forgery) | Catastrophic (GHASH key recovered) |
| State size | 320 bits (40 bytes) | AES key schedule + GHASH state |
| Throughput (software) | Competitive on constrained hardware | Fast with hardware; slow in software |

Use `AsconAead128` when you need a NIST-approved AEAD that runs well in software on any
platform, or when targeting hardware without AES-GCM acceleration. Use the BCL's `AesGcm` or
the library's `GcmModeTransform` when throughput on AES-NI hardware is the priority.

## Where to go next

- [ASCON overview](ascon.md) — the full family and which algorithm to pick.
- [ASCON hashing](ascon-hashing.md) — fixed-length 256-bit digest patterns.
- [ASCON XOF](ascon-xof.md) — variable-length output with `AsconXof128` and `AsconCxof128`.
- [AEAD modes guide](aead-modes.md) — AES-based AEAD (GCM, CCM, OCB3, SIV, GCM-SIV).
- API reference: <xref:Bodu.Security.Cryptography.AsconAead128> · <xref:Bodu.Security.Cryptography.IAeadBlockCipherModeTransform>
