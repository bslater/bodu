---
title: ASCON extendable output — AsconXof128 and AsconCxof128
---

# ASCON extendable output — AsconXof128 and AsconCxof128

An **extendable output function (XOF)** is a hash with no fixed output length. You absorb any
amount of input, then squeeze out as many bytes as you need — 32, 64, or a million. ASCON-XOF128
and ASCON-CXOF128 are the XOF members of the ASCON family, standardized in NIST SP 800-232.

**Bodu.Security.Cryptography** provides two concrete types, both inheriting from the abstract
`AsconXof<T>` base:

| Type | Algorithm name | Customization | Typical use |
|---|---|---|---|
| <xref:Bodu.Security.Cryptography.AsconXof128> | `ASCON-XOF128` | None | General-purpose variable-length output, key derivation, stream seed. |
| <xref:Bodu.Security.Cryptography.AsconCxof128> | `ASCON-CXOF128` | Optional customization string | Multiple independent output functions from the same primitive. |

## Fixed parameters

| Parameter | Value | Notes |
|---|---|---|
| State size | 320 bits (40 bytes) | Shared across the full ASCON family. |
| Rate (absorption and squeeze block) | 64 bits (8 bytes) | Eight bytes processed per permutation call. |
| Absorption rounds | 8 (Ascon-p8) | Applied after each absorbed block. |
| Transition permutation | 12 (Ascon-p12) | Always applied once when switching from absorb to squeeze. |
| Squeeze permutation | 8 (Ascon-p8) | Applied between successive squeeze blocks. |
| Output | Variable | Any positive multiple of 8 bits. |
| Security level | 128 bits | For outputs of at least 32 bytes. Shorter outputs cap collision resistance at half the output length, as with any XOF. |

## The absorb-then-squeeze lifecycle

Both types follow the same three-phase lifecycle:

1. **Absorb** — call `Absorb(ReadOnlySpan<byte>)` zero or more times to feed input data.
2. **Squeeze** — call `Squeeze(Span<byte>)` one or more times to read output bytes.
3. **Reset** — call `Initialize()` to discard all state and start a fresh message.

`Absorb` after `Squeeze` has started throws `InvalidOperationException`. Call `Initialize()` to
reset. For `AsconCxof128`, there is a fourth phase before absorption: an optional call to
`Customize`.

## Pattern 1 — producing output of a fixed length

The simplest path: absorb a message, squeeze an exact number of bytes.

```csharp
using System.Text;
using Bodu.Security.Cryptography;

byte[] message = Encoding.UTF8.GetBytes("the quick brown fox");

using var xof  = new AsconXof128();
xof.Absorb(message);
byte[] output  = xof.GetHash(64);    // squeeze exactly 64 bytes
```

`GetHash(int length)` is a convenience wrapper around `Squeeze` that allocates and returns a new
array. The instance transitions to the squeezing phase on the first call; subsequent calls to
`Squeeze` continue from where the last call left off.

## Pattern 2 — one-shot static method

For a single-pass operation that does not need a reusable instance, use the static `HashData`
method:

```csharp
using System.Text;
using Bodu.Security.Cryptography;

byte[] message = Encoding.UTF8.GetBytes("the quick brown fox");
byte[] output  = AsconXof128.HashData(message, outputLength: 32);
```

`HashData` constructs a temporary instance internally, absorbs the source, squeezes the
requested number of bytes, and returns them. It is equivalent to the three-step instance API in
a single call.

## Pattern 3 — deriving multiple keys from one input

Because squeezing is incremental, a single absorbed input can produce non-overlapping byte
streams for different purposes without rehashing.

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

// Derive per-session key material from a shared secret and a session ID.
byte[] sharedSecret = LoadSharedSecret();
byte[] sessionId    = GetSessionId();

using var xof = new AsconXof128();
xof.Absorb(sharedSecret);
xof.Absorb(sessionId);      // absorb can be called multiple times

byte[] encKey = new byte[32];   // 256-bit encryption key
byte[] macKey = new byte[32];   // 256-bit MAC key
byte[] iv     = new byte[16];   // 128-bit IV

xof.Squeeze(encKey);    // first 32 bytes of XOF output
xof.Squeeze(macKey);    // next 32 bytes — independent of encKey
xof.Squeeze(iv);        // next 16 bytes
```

The three slices are deterministic and non-overlapping for the same inputs. This is the XOF
alternative to HKDF-Expand: a single absorbed PRK produces an unlimited number of output bytes,
each region serving a separate purpose.

## Pattern 4 — incremental absorption

Call `Absorb` as many times as needed. The sponge buffers partial blocks internally, so calls do
not need to align to the 8-byte rate.

```csharp
using Bodu.Security.Cryptography;

using var xof = new AsconXof128();

foreach (byte[] chunk in GetMessageChunks())
    xof.Absorb(chunk);

byte[] digest = xof.GetHash(32);
```

The result is identical to absorbing all chunks concatenated in a single call.

## Pattern 5 — incremental squeezing

Squeeze as many or as few bytes per call as you like. Each call continues from where the last
left off.

```csharp
using Bodu.Security.Cryptography;

using var xof = new AsconXof128();
xof.Absorb(seed);

byte[] block = new byte[8];
for (int i = 0; i < 1024; i++)
{
    xof.Squeeze(block);
    ProcessBlock(block, i);
}
```

This is useful for generating large amounts of deterministic pseudo-random material from a fixed
seed — effectively a stream cipher output.

## Pattern 6 — CXOF128 with a customization string

`AsconCxof128` extends the XOF lifecycle with an optional customization step before absorption.
Two instances with different customization strings produce completely independent outputs for the
same absorbed input.

```csharp
using System.Text;
using Bodu.Security.Cryptography;

byte[] message = Encoding.UTF8.GetBytes("the quick brown fox");

// Derive an encryption key — customization string identifies the context.
using var encXof = new AsconCxof128();
encXof.Customize(Encoding.UTF8.GetBytes("enc-key-v1"));
encXof.Absorb(message);
byte[] encKey = encXof.GetHash(32);

// Derive a MAC key — different customization string, independent output.
using var macXof = new AsconCxof128();
macXof.Customize(Encoding.UTF8.GetBytes("mac-key-v1"));
macXof.Absorb(message);
byte[] macKey = macXof.GetHash(32);

// encKey != macKey, even though message is identical.
```

Call `Customize` **before** any call to `Absorb`. Calling it afterwards, or calling it a second
time on the same instance, throws `InvalidOperationException`. Call `Initialize()` to reset and
allow a fresh `Customize`.

### Customization string semantics

The customization string is absorbed into the sponge using the standard Ascon padding rule, and
then a domain-separation constant (XOR of 1 into state word S₄) is injected before the message
absorption phase begins. This ensures:

- Two instances with different customization strings produce unrelated output.
- An instance with an empty customization string produces output that differs from
  `AsconXof128` for the same message (because the domain-separation constant is always applied).
- The customization string may be public — it is not a secret.

## Pattern 7 — empty customization string (not the same as no customization)

Calling `Customize(ReadOnlySpan<byte>.Empty)` still applies the ASCON-CXOF128 domain separation.
The output differs from `AsconXof128` even with no customization bytes:

```csharp
using Bodu.Security.Cryptography;

byte[] message = [0x01, 0x02, 0x03, 0x04];

using AsconCxof128 cxof = new AsconCxof128();
cxof.Customize(ReadOnlySpan<byte>.Empty);   // empty string — domain separation still applied
cxof.Absorb(message);
byte[] cxofOutput = cxof.GetHash(32);

byte[] xofOutput = AsconXof128.HashData(message, 32);

// cxofOutput != xofOutput
```

If `Customize` is not called at all, `AsconCxof128` operates without the customization domain
separation — identically to a plain `AsconXof128` for the same absorbed input. The
customization step is optional by design.

## Pattern 8 — reusing an instance

Call `Initialize()` to reset the sponge and reuse the instance for a new message:

```csharp
using Bodu.Security.Cryptography;

using var xof = new AsconXof128();

for (int i = 0; i < messages.Length; i++)
{
    xof.Initialize();
    xof.Absorb(messages[i]);
    digests[i] = xof.GetHash(32);
}
```

For `AsconCxof128`, `Initialize()` also clears the customization state, so `Customize` can be
called again on the reset instance.

## Algorithm selection

| If you need… | Reach for |
|---|---|
| Variable-length output from a single input | `AsconXof128` |
| Multiple independent output streams for different application domains | `AsconCxof128` with distinct customization strings |
| A fixed 256-bit digest | `AsconHash256` or `AsconHashA256` |
| Output derived from multiple pieces of context | Call `Absorb` multiple times on one `AsconXof128` instance |

## Where to go next

- [ASCON overview](ascon.md) — the full family and which algorithm to choose.
- [ASCON hashing](ascon-hashing.md) — fixed-length 256-bit digest patterns.
- [ASCON AEAD](ascon-aead.md) — authenticated encryption with `AsconAead128`.
- API reference: <xref:Bodu.Security.Cryptography.AsconXof128> · <xref:Bodu.Security.Cryptography.AsconCxof128>
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
