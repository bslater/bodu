---
title: Using Base32
---

# Using Base32

`Base32` packs five bits per output character (radix 32), so the payload expansion is **60 %** — between Base16's
100 % and Base64's 33 %. The library ships four variants: the two RFC 4648 alphabets plus two human-oriented
alternatives.

```
input bytes  : 66 6F 6F 62 61 72             (ASCII "foobar")
              :                              ┌── 6 bytes = 48 bits ──┐
              :                              │ 5 chars × 5 = 25 bits │ + 3 bits leftover
              :                              └──   ─ 5-byte group ─  ┘
encoded text : MZXW6YTBOI======              (Standard, 16 chars including padding)
              CPNMUOJ1E8======               (HexExtended)
              CPNMUOJ1E8                     (Crockford — no padding by default)
              csrtbarvni                     (Z-Base-32 — lowercase alphabet, no padding)
```

## Quick reference

```csharp
using Bodu.Text.Encoding;

byte[] data = System.Text.Encoding.ASCII.GetBytes("foobar");

// Standard RFC 4648 §6 (default variant)
string s = Base32.Encode(data);
// "MZXW6YTBOI======"

// Base32hex (RFC 4648 §7)
string h = Base32.Encode(data, Base32Variant.HexExtended);
// "CPNMUOJ1E8======"

// Crockford — no padding by default
string c = Base32.Encode(data, Base32Variant.Crockford);
// "CSQPYRK1E8"

// z-base-32 — lowercase human-oriented
string z = Base32.Encode(data, Base32Variant.ZBase32);
// (lowercase z-base-32 form)
```

## Variant comparison

| Variant | Alphabet | Padding default | Visually ambiguous chars excluded | Decode aliases |
|---|---|---|---|---|
| `Standard` (RFC 4648 §6) | `A-Z 2-7` | Yes (`=`) | — | Case-insensitive |
| `HexExtended` (RFC 4648 §7) | `0-9 A-V` | Yes (`=`) | — | Case-insensitive |
| `Crockford` | `0-9 A-Z` minus `I L O U` | No | Yes — `I` `L` `O` `U` | `I`/`L` → `1`, `O` → `0`, case-insensitive |
| `ZBase32` | Permuted lowercase | No | Indirectly | Case-insensitive |

### When to pick each

| Variant | Reach for |
|---|---|
| Standard | TOTP / HOTP shared secrets (Google Authenticator), DNSSEC NSEC3, RFC-spec compliance, MIME content-transfer |
| HexExtended | DNSSEC labels, anywhere that retains case-insensitive lexicographic order with the underlying bytes |
| Crockford | Human-spoken short IDs (transaction codes, voucher codes); tolerates 0/O and 1/I/L confusion |
| ZBase32 | Lowercase-only contexts where the human-friendly ordering of frequent letters helps recognition |

## Crockford specifics

Crockford Base32 is designed for human transcription. The encoder always emits the canonical alphabet
`0-9 A-Z` (skipping `I`, `L`, `O`, `U`), but the decoder is more permissive:

| Input character | Decoded as |
|---|---|
| `I`, `i`, `L`, `l` | `1` |
| `O`, `o` | `0` |
| Any other ASCII letter | Case-insensitive match against the alphabet |
| `U`, `u` | **Rejected** (explicitly excluded per spec) |

```csharp
// "D1G2" and "DIG2" / "DLG2" / "dlg2" all decode to the same bytes.
byte[] canonical = Base32.Decode("D1G2", Base32Variant.Crockford);
byte[] aliased   = Base32.Decode("DLG2", Base32Variant.Crockford);
Debug.Assert(canonical.SequenceEqual(aliased));
```

Crockford and z-base-32 also do **not** enforce the RFC 4648 §6 terminal-quantum rule, so terminal lengths like
1, 3, or 6 data characters are tolerated by those variants while rejected by Standard and HexExtended.

## Padding and missing-padding tolerance

Standard and HexExtended require canonical padding by default. Pass `BaseFormatStyles.AllowMissingPadding` on
decode and `BaseFormattingOptions.OmitPadding` on encode to opt out:

```csharp
string  withoutPad = Base32.Encode(secret, Base32Variant.Standard, BaseFormattingOptions.OmitPadding);
byte[]  back       = Base32.Decode(withoutPad, Base32Variant.Standard, BaseFormatStyles.AllowMissingPadding);
```

## Lenient parsing

| Flag | Effect |
|---|---|
| `BaseFormatStyles.IgnoreWhitespace` | Strip ASCII space, tab, CR, LF (handy for line-wrapped output) |
| `BaseFormatStyles.AllowMissingPadding` | Accept inputs that omit the trailing `=` characters |
| `BaseFormatStyles.AllowPrefix` | No-op for Base32 (no standard prefix) |
| `BaseFormatStyles.RequireCanonicalEncoding` | **Tightens** the decoder — rejects a terminal symbol with non-zero unused bits |

`BaseFormattingOptions.InsertLineBreaks` is supported on encode and wraps at 64 characters — the same convention
as Base16.

### Canonical form and the 5-byte quantum

Base32 repeats a **5-byte → 8-character** quantum (40 bits = lcm of the 8-bit byte and the 5-bit symbol). A
partial tail does not fill every bit of its final symbol — a 1-byte tail leaves 2 unused bits, a 2-byte tail
leaves 4, and so on — and RFC 4648 §3.5 lets a decoder accept the result whatever those leftover bits are. Two
encoded strings that differ only in those unused trailing bits therefore decode to the *same* bytes. By default
the decoder tolerates the non-canonical form; `RequireCanonicalEncoding` rejects it, so every byte sequence has a
single accepted spelling:

```csharp
byte[] bytes = { 0xDE, 0xAD };

string canonical = Base32.Encode(bytes, Base32Variant.Standard);   // the one form the encoder ever emits
Base32.Decode(canonical, Base32Variant.Standard);                  // ok

// A hand-edited tail whose unused bits are non-zero still decodes to { 0xDE, 0xAD } by default …
Base32.Decode(canonical, Base32Variant.Standard,
    BaseFormatStyles.RequireCanonicalEncoding);                    // … but is rejected under canonical enforcement
```

Pass `RequireCanonicalEncoding` when each byte sequence must have exactly one accepted spelling — content-addressed
keys, deduplication, or a value that feeds a signature. The flag is a no-op for the leftover-bit-free families
(Base16) and the non-bit-stream families (Base58, Base85).

## Span and UTF-8 paths

```csharp
// Predict size and write into a destination span
char[] buffer = new char[Base32.GetEncodedLength(data.Length)];
int written = Base32.Encode(data, buffer);

// Non-throwing
bool ok = Base32.TryEncode(data, buffer, out int charsWritten);

// UTF-8 encode / decode
byte[] utf8 = Base32.EncodeToUtf8(data);
var status = Base32.DecodeFromUtf8(
    utf8Source, byteDestination,
    out int bytesConsumed, out int bytesWritten,
    Base32Variant.Standard, BaseFormatStyles.None, isFinalBlock: true);
```

## Validation and sizing

```csharp
Base32.IsValid("MZXW6YTBOI======");          // true (Standard, padded)
Base32.IsValid("MZXW6YTBOI");                // false (strict — missing padding)
Base32.IsValid("MZXW6YTBOI",
    Base32Variant.Standard,
    BaseFormatStyles.AllowMissingPadding);   // true

Base32.IsBase32Digit('A');                    // true (Standard alphabet)
Base32.IsBase32Digit('0', Base32Variant.Standard);    // false ('0' is excluded)
Base32.IsBase32Digit('0', Base32Variant.Crockford);   // true

Base32.GetEncodedLength(20);                  // 32 (5-byte groups → 8-char groups, padded)
Base32.GetEncodedLength(20, Base32Variant.Crockford); // 32 (still 32 chars without padding fits)
Base32.GetMaxDecodedLength(32);               // 20
```

## Encoding a GUID

`Base32` encodes a <xref:System.Guid> into a 26-character (padless) or 32-character (padded) token — denser than
hex and case-insensitive, so it survives shouting down a phone line in Crockford form:

```csharp
Guid id = Guid.NewGuid();

string token = Base32.Encode(id, Base32Variant.Crockford);    // 26 chars, no padding, no I/L/O/U ambiguity
Guid back    = Base32.DecodeGuid(token, Base32Variant.Crockford);
bool ok      = Base32.TryDecodeGuid(token, out Guid parsed, Base32Variant.Crockford);
```

The 16 GUID bytes are written in their native mixed-endian layout (matching `Guid.TryWriteBytes`), so the round
trip is exact for whichever variant you encode and decode with.

## Common patterns

### TOTP / HOTP shared secret

```csharp
// Secret is canonical Base32 Standard, but most consumer apps accept lower case and missing padding too.
byte[] secret = RandomNumberGenerator.GetBytes(20);
string display = Base32.Encode(
    secret,
    Base32Variant.Standard,
    BaseFormattingOptions.OmitPadding); // e.g. "JBSWY3DPEHPK3PXP"
```

### Crockford-friendly short ID

```csharp
byte[] randomBytes = RandomNumberGenerator.GetBytes(5);
string code = Base32.Encode(randomBytes, Base32Variant.Crockford);
// e.g. "F08NHM37" — 8 characters, no ambiguity with O/0/I/L
```

## Where to go next

- **[Base64 guide](base64.md)** — when you need denser packing.
- **[Base16 guide](base16.md)** — when you need explicit byte boundaries.
- **[`IBinaryEncoding` interface](binary-encodings-interface.md)** — runtime-selected encoding choice.
- **[Text & Serialization guides](../topics/text-and-serialization.md)** — every guide in this topic, across Bodu.Text.Encoding, Bodu.Text.Formats, and the Bencode / TOML serializers.
