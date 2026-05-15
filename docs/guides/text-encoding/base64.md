---
title: Using Base64
---

# Using Base64

`Base64` packs six bits per output character (radix 64), giving the lowest payload expansion of the radix
encodings the library ships: **33 %**. It is the canonical encoding for MIME, JWT, certificates, and most general
"put binary in a string" scenarios.

The library defers the inner conversion to `Convert.TryToBase64Chars` / `Convert.TryFromBase64Chars` so it
inherits the BCL's SIMD-accelerated paths, then adds variant alphabet swapping, padding control, and lenient
parsing on top.

```
input bytes  : 66 6F 6F 62 61 72         (ASCII "foobar")
              :                          ┌── 6 bytes = 48 bits ───┐
              :                          │  8 chars × 6 = 48 bits │
              :                          └──── 3-byte groups ─────┘
encoded text : Zm9vYmFy                  (Standard / URL-safe — same here, no +/- in this example)
              Zm9vYmFy                    (URL-safe)
              Zm9vYmFy\r\n...             (MIME — 76-char line wrap)
```

## Quick reference

```csharp
using Bodu.Text.Encoding;

byte[] data = System.Text.Encoding.ASCII.GetBytes("foobar");

// Standard RFC 4648 §4
string s = Base64.Encode(data);                                // "Zm9vYmFy"

// URL-safe RFC 4648 §5 — '+' → '-', '/' → '_', no padding by default
string u = Base64.Encode(data, Base64Variant.UrlSafe);         // "Zm9vYmFy"

// MIME RFC 2045 — 76-char wrap with \r\n
byte[] big = new byte[300];
string m = Base64.Encode(big, Base64Variant.Mime);
// Contains "\r\n" every 76 chars

// Decode
byte[] back = Base64.Decode("Zm9vYmFy");
```

## Variant comparison

| Variant | Alphabet | Padding default | Line wrapping |
|---|---|---|---|
| `Standard` (RFC 4648 §4) | `A-Z a-z 0-9 + /` | Yes (`=`) | No |
| `UrlSafe` (RFC 4648 §5) | `A-Z a-z 0-9 - _` | **No** (JWT / OAuth convention) | No |
| `Mime` (RFC 2045) | Standard alphabet | Yes (`=`) | **Yes — 76 chars** |

### When to pick each

| Variant | Reach for |
|---|---|
| `Standard` | Generic binary-in-text, TLS certificates, classic SMTP / MIME content |
| `UrlSafe` | JWT, OAuth tokens, query parameters, filenames |
| `Mime` | RFC 2045 attachments, PEM-style files (which extend MIME), `Convert.ToBase64String(..., InsertLineBreaks)` compatibility |

## URL-safe specifics

URL-safe Base64 swaps two characters: `+` → `-`, `/` → `_`. The decoder is **strict** by default — `+` and `/`
are rejected in URL-safe mode, and `-` / `_` are rejected in Standard mode. To accept either alphabet, use the
`Get(name)` lookup with the appropriate variant or pre-normalise the input.

URL-safe also omits padding by default. To re-add padding pass `BaseFormattingOptions.None` and the encoder will
emit canonical `=` padding:

```csharp
Base64.Encode(data, Base64Variant.UrlSafe);                                  // no padding (default)
Base64.Encode(data, Base64Variant.UrlSafe, BaseFormattingOptions.None);     // no padding (same)
Base64.Encode(data, Base64Variant.UrlSafe, BaseFormattingOptions.OmitPadding); // explicit no-padding (same)
```

JWT tokens never use padding — decoders accept either form via `BaseFormatStyles.AllowMissingPadding`:

```csharp
byte[] headerBytes = Base64.Decode(
    "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9",
    Base64Variant.UrlSafe,
    BaseFormatStyles.AllowMissingPadding);
```

## MIME line wrapping

MIME mandates `\r\n` every 76 characters. The encoder honours this automatically when the variant is `Mime`:

```csharp
byte[] payload = new byte[300];
RandomNumberGenerator.Fill(payload);
string mime = Base64.Encode(payload, Base64Variant.Mime);
// "AAAA…\r\nBBBB…\r\n…" — wraps at column 76

byte[] back = Base64.Decode(mime, Base64Variant.Mime);
// Implicitly strips whitespace — no extra style flag needed
```

`Base64Variant.Mime` implicitly enables whitespace stripping on decode because MIME content always contains
line breaks. Standard / URL-safe require the explicit `BaseFormatStyles.IgnoreWhitespace` flag when input
contains whitespace.

## Lenient parsing

| Flag | Effect |
|---|---|
| `BaseFormatStyles.IgnoreWhitespace` | Strip ASCII space / tab / CR / LF anywhere |
| `BaseFormatStyles.AllowMissingPadding` | Accept inputs without trailing `=` |
| `BaseFormatStyles.AllowPrefix` | No-op for Base64 (no standard prefix) |

## Span and UTF-8 paths

```csharp
// Predict size and encode in place
Span<char> buffer = stackalloc char[Base64.GetEncodedLength(data.Length)];
int written = Base64.Encode(data, buffer);

// UTF-8 byte path for network pipelines
byte[] utf8 = Base64.EncodeToUtf8(data);
var status = Base64.DecodeFromUtf8(
    utf8Source, byteDestination,
    out int bytesConsumed, out int bytesWritten,
    Base64Variant.Standard, BaseFormatStyles.None, isFinalBlock: true);
```

## Validation and sizing

```csharp
Base64.IsValid("Zm9vYmFy");                              // true
Base64.IsValid("Zm-v");                                  // false (URL-safe char in Standard)
Base64.IsValid("Zm-v", Base64Variant.UrlSafe);          // true
Base64.IsBase64Digit('+');                               // true (Standard alphabet)
Base64.IsBase64Digit('+', Base64Variant.UrlSafe);       // false

Base64.GetEncodedLength(6, Base64Variant.Standard);     // 8 (padded)
Base64.GetEncodedLength(6, Base64Variant.UrlSafe);      // 8 (no padding needed for 6 bytes)
Base64.GetMaxDecodedLength(8);                           // 6
```

## Common patterns

### JWT segment decode

```csharp
static byte[] DecodeJwtSegment(string segment) =>
    Base64.Decode(segment, Base64Variant.UrlSafe, BaseFormatStyles.AllowMissingPadding);
```

### MIME attachment encode

```csharp
static string EncodeAttachment(ReadOnlySpan<byte> content) =>
    Base64.Encode(content, Base64Variant.Mime);
```

### Strict round-trip with span allocation

```csharp
static bool TryRoundTrip(ReadOnlySpan<byte> data)
{
    Span<char> charBuffer = stackalloc char[Base64.GetEncodedLength(data.Length)];
    Span<byte> byteBuffer = stackalloc byte[data.Length];

    return Base64.TryEncode(data, charBuffer, out int charsWritten)
        && Base64.TryDecode(charBuffer[..charsWritten], byteBuffer, out int bytesWritten)
        && data.SequenceEqual(byteBuffer[..bytesWritten]);
}
```

## Where to go next

- **[Base32 guide](base32.md)** — when human readability beats density.
- **[Base85 guide](base85.md)** — when 25 % expansion matters more than alphabet familiarity.
- **[`IBinaryEncoding` interface](binary-encodings-interface.md)** — runtime-selected encoding choice.
