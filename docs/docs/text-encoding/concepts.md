---
title: Bodu.Text.Encoding — Core concepts
---

# Bodu.Text.Encoding — Core concepts

This page is the vocabulary the rest of the documentation assumes. Read it once before the
[getting-started samples](getting-started.md) or the [guides](../../guides/text-encoding/index.md), and refer back
whenever a term feels imprecise.

For the high-level shape of the library and the type map, start with the [introduction](index.md).

## Encoding and variant

An **encoding** is the family-level concept: Base16, Base32, Base64, Base58, Base85. Each family has a radix (16,
32, 64, 58, 85) which determines how many bits each output symbol represents.

A **variant** is a specific choice within an encoding family — typically a different *alphabet* but sometimes
different padding, line-wrap, or shortcut rules:

| Family | Variants in this library |
|---|---|
| Base16 | lower-case (default), upper-case (via `BaseFormattingOptions.UpperCase`) |
| Base32 | `Standard` (RFC 4648 §6), `HexExtended` (RFC 4648 §7), `Crockford`, `ZBase32` |
| Base64 | `Standard` (RFC 4648 §4), `UrlSafe` (RFC 4648 §5), `Mime` (RFC 2045) |
| Base58 | `BitcoinFlickr` (default), `Ripple` |
| Base85 | `Ascii85` (Adobe Tech Note 5045), `Z85` (RFC 32 ZeroMQ) |

## Alphabet

The **alphabet** is the set of characters a variant uses to represent the radix digits. The position of each
character in the alphabet string is its numeric value.

```
Base16:                "0123456789abcdef"          (lower case, 16 chars)
Base32 Standard:       "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"   (RFC 4648 §6)
Base32 HexExtended:    "0123456789ABCDEFGHIJKLMNOPQRSTUV"   (RFC 4648 §7)
Base32 Crockford:      "0123456789ABCDEFGHJKMNPQRSTVWXYZ"   (no I L O U)
Base32 Z-Base-32:      "ybndrfg8ejkmcpqxot1uwisza345h769"
Base64 Standard:       "A..Za..z0..9+/"             (RFC 4648 §4)
Base64 URL-safe:       "A..Za..z0..9-_"             (RFC 4648 §5)
Base58 Bitcoin/Flickr: "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz"  (no 0 O I l)
Base85 Ascii85:        '!' (33) through 'u' (117)   (Adobe alphabet)
Base85 Z85:            "0..9a..zA..Z.-:+=^!/*?&<>()[]{}@%$#"  (shell-safe)
```

## Bit packing

For power-of-two radices (Base16, Base32, Base64), encoding is a **bit-stream operation**: input bytes are
concatenated into a bit stream, then chunked into N-bit groups (where N is `log2(radix)`). Each N-bit group becomes
one output symbol.

```
Base16 (4 bits / symbol):  AB → 10101011 → 1010 1011 → 'a' 'b'
Base32 (5 bits / symbol):  fooba → 01100110 01101111 01101111 01100010 01100001
                                  → 01100 11001 10111 10110 11000 10011 00001
                                  → 12 25 23 22 24 19 1
                                  → M  Z  X  W  Y  T  B
Base64 (6 bits / symbol):  foo   → 01100110 01101111 01101111
                                  → 011001 100110 111101 101111
                                  → 25 38 61 47
                                  → Z  m  9  v
```

Base58 is **not** a power of two: it uses big-integer divmod by 58. Base85 uses **4-byte blocks** packed into a
32-bit unsigned integer, then divided by 85 four times to emit five characters.

## Terminal quantum

The **terminal quantum** is the last group at the end of the input. When the input does not divide evenly into
the group size, the spec defines which character counts are legal and how padding aligns the output.

RFC 4648 quantum rules:

| Encoding | Group size | Valid terminal-quantum data-character counts |
|---|---|---|
| Base16 | 1 byte → 2 chars | Always even (length must be a multiple of 2) |
| Base32 (Standard / HexExtended) | 5 bytes → 8 chars | **{0, 2, 4, 5, 7, 8}** — counts 1, 3, 6 are invalid |
| Base64 (Standard / UrlSafe / Mime) | 3 bytes → 4 chars | **{0, 2, 3, 4}** — count 1 is invalid |

Crockford Base32 and z-base-32 do not impose terminal-quantum rules. Base58 has no quantum. Base85 partial-group
sizes are 1, 2, or 3 bytes for Ascii85; Z85 requires exact 4-byte alignment.

## Padding

**Padding** is the `=` character that Base32 and Base64 append after a partial terminal quantum so the output
length aligns to the group size. For a *3-byte* Base64 input, no padding is needed (3 → 4 chars exactly); for
*2-byte* input the output is 4 chars including one `=`; for *1-byte* input, two `=`.

| Variant | Padding by default |
|---|---|
| Base32 Standard / HexExtended | Yes (RFC 4648 mandates) |
| Base32 Crockford / ZBase32 | No (by spec convention) |
| Base64 Standard / Mime | Yes (RFC 4648 / 2045 mandate) |
| Base64 UrlSafe | No (JWT / OAuth convention; can be added with omission of `OmitPadding`) |
| Base58 | No padding character |
| Base85 | No padding character — alignment is via partial-group truncation (Ascii85) or fixed 4-byte alignment (Z85) |

Control padding at encode time with `BaseFormattingOptions.OmitPadding`, and accept unpadded input at decode time
with `BaseFormatStyles.AllowMissingPadding`.

## Shortcut

A **shortcut** is a single character that stands in for a full all-zero group. Only Adobe Ascii85 defines one:

| Variant | Shortcut | Meaning |
|---|---|---|
| Base85 Ascii85 | `z` | Four zero bytes (`0x00 0x00 0x00 0x00`) |

Z85 has no shortcut — even all-zero input emits five `0` characters.

## Decoration

A **decoration** is an optional, non-data character or marker added at encode time for human readability or
container framing, and tolerated at decode time when explicitly allowed:

| Decoration | Encode flag | Decode flag | Applies to |
|---|---|---|---|
| `0x` prefix | `BaseFormattingOptions.IncludePrefix` | `BaseFormatStyles.AllowPrefix` | Base16 |
| Byte spacing (e.g. `DE AD BE EF`) | `BaseFormattingOptions.InsertSpacing` | (combine with `IgnoreWhitespace`) | Base16 |
| Line breaks every N chars (64 for Base16, 76 for Base64 Mime) | `BaseFormattingOptions.InsertLineBreaks` | (combine with `IgnoreWhitespace`) | Base16, Base32, Base64 |
| Case folding (lower / upper) | `BaseFormattingOptions.UpperCase` | implicit (decoders are case-insensitive) | Base16, Base32 |

## Encoder vs. decoder strictness

Encoders are **deterministic** — given a byte sequence and an options set they always produce the same canonical
output. Decoders are typically **strict** by default — only the canonical alphabet, padding, and quantum length
are accepted — and **lenient** when `BaseFormatStyles` flags are set:

```
strict mode (BaseFormatStyles.None)
  • only alphabet characters
  • exact padding alignment for padded variants
  • valid terminal-quantum length

lenient mode (one or more BaseFormatStyles flags)
  AllowPrefix         → tolerate "0x" / "0X" prefix at the start
  IgnoreWhitespace    → strip ASCII space, tab, CR, LF anywhere
  AllowMissingPadding → accept inputs without the trailing "=" characters
```

The `Decode` overloads throw `FormatException` on validation failure. The `TryDecode` overloads return
`false` instead. The `IsValid` predicate returns `true` only for inputs that the strict decoder would accept
(when no leniency flags are passed).

## UTF-8 path

Every encoding family exposes a UTF-8 byte path alongside the character path:

```
encode:  EncodeToUtf8(ReadOnlySpan<byte>) → byte[]
         TryEncodeToUtf8(ReadOnlySpan<byte>, Span<byte>, out int, …)

decode:  FromBase{N}String(ReadOnlySpan<byte> utf8Source, Span<byte> dst, out int consumed, out int written)
                                                                      → OperationStatus
         DecodeFromUtf8(ReadOnlySpan<byte>, Span<byte>, out int, out int, …, isFinalBlock)
                                                                      → OperationStatus
```

Because every encoding alphabet is ASCII, the UTF-8 byte form is bit-identical to the character form. This path is
the natural choice when bytes come from a network / file pipeline and you want to avoid allocating a `string` or
`char[]` between stages.

## OperationStatus and streaming

The `OperationStatus` return convention (from `System.Buffers`) is the same one `System.Buffers.Text.Base64` uses:

| Value | Meaning |
|---|---|
| `Done` | Input fully consumed and decoded |
| `DestinationTooSmall` | Destination span cannot hold the output |
| `InvalidData` | Input contains a character outside the variant alphabet or violates structural rules |
| `NeedMoreData` | (Streaming only — `isFinalBlock: false`) The input ends mid-quantum and may resolve with more bytes |

Use `isFinalBlock: true` when the input you are passing is the entire data (default). Use `isFinalBlock: false` to
stream data through chunk by chunk — the decoder will report `NeedMoreData` instead of `InvalidData` for inputs
that could complete with another chunk.

> Base58 and Base85 are not streamable: each requires the entire input to be available before decode (Base58 because
> of big-integer arithmetic, Base85 because of fixed-size block packing). The `isFinalBlock` parameter on those
> variants is accepted for API consistency but has no behavioural effect.

## IBinaryEncoding interface

The static classes are the primary entry point. For code that must select an encoding at **runtime** — a
configuration-driven serializer, a plugin pipeline, a generic utility — use the <xref:Bodu.Text.Encoding.IBinaryEncoding>
interface and one of the pre-configured instances in <xref:Bodu.Text.Encoding.BinaryEncodings>:

```csharp
IBinaryEncoding encoding = BinaryEncodings.Get("base64-urlsafe");
string token = encoding.Encode(secretKey);
```

The interface deliberately hides variant-specific options (line breaks, padding control, MIME wrapping). Code that
needs those should keep using the static classes directly.

## Where to go next

- **[Getting started](getting-started.md)** — install + minimal sample per encoding type.
- **[Bodu.Text.Encoding guides](../../guides/text-encoding/index.md)** — using each encoding, choosing variants, streaming, the `IBinaryEncoding` interface.
- **[Introduction](index.md)** — type map, scenarios, where each encoding fits.
