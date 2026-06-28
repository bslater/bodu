---
title: Using Quoted-Printable (MIME bodies)
---

# Using Quoted-Printable (MIME bodies)

`QuotedPrintable` implements the MIME Quoted-Printable body content-transfer encoding of
**RFC 2045 §6.7** — the sibling of MIME Base64 for *mostly-readable*, 7-bit-safe text. Printable ASCII passes through
literally; every other octet becomes an `=HH` escape with uppercase hexadecimal digits. Lines are kept within a
configurable limit by inserting **soft line breaks** (a trailing `=` followed by the newline) that the decoder removes.

```
input bytes  : 63 61 66 C3 A9                 (UTF-8 "café", 5 bytes)
escaped text : caf=C3=A9                       (printable run + two =HH escapes)
```

> [!NOTE]
> `QuotedPrintable` encodes and decodes **only** the Quoted-Printable body transform. It does **not** implement the
> RFC 2047 encoded-word `Q` header encoding (different syntax, underscore-for-space), and it does not parse MIME
> messages, headers, multiparts, charsets, or content-transfer-encoding declarations.

## Quick reference

```csharp
using Bodu.Text.Encoding;

byte[] data = "café = møney"u8.ToArray();

// Binary mode (default) — arbitrary octets, 76-column soft wrapping, CRLF.
string encoded = QuotedPrintable.Encode(data);

// Round-trip.
byte[] back = QuotedPrintable.Decode(encoded);
```

## Binary vs text mode

`QuotedPrintable` does not fit the flat-byte `Base{N}` shape — its output length depends on the content — so it is a
**static type, not an [`IBinaryEncoding`](binary-encodings-interface.md)**. The only structural choice is how line
breaks in the *source* are treated:

| Mode | CR / LF in source | Use for |
|---|---|---|
| `Binary` (default) | Escaped as `=0D` / `=0A`; no byte sequence is a hard break | Arbitrary octet round-trips |
| `Text` | A canonical `CRLF` pair becomes a hard break (`options.NewLine`); a lone CR or LF is escaped | RFC 2045 canonical text bodies |

```csharp
byte[] crlf = "line1\r\nline2"u8.ToArray();

QuotedPrintable.Encode(crlf);                                              // "line1=0D=0Aline2"  (binary)
QuotedPrintable.Encode(crlf, new(QuotedPrintableEncodingMode.Text));       // "line1\r\nline2"     (text)
```

## Encoding rules

| Octet | Output |
|---|---|
| Printable ASCII `0x21`–`0x3C`, `0x3E`–`0x7E` (except `=`) | Literal |
| `=` (`0x3D`) | Always `=3D` |
| Space / tab in the middle of a line | Literal |
| Space / tab at the end of a line | `=20` / `=09` (a decoder may delete trailing whitespace) |
| Any other octet | `=HH` with **uppercase** hex |

The encoder never emits a literal space or tab as the last character on a line, so canonical output round-trips through
the strict decoder without loss.

## Line length and soft breaks

Encoded lines never exceed `MaxLineLength` characters (default **76**), and the trailing soft-break `=` is counted
within that limit. A `MaxLineLength` of `0` selects the RFC default; values below `4` are rejected.

```csharp
string wrapped = QuotedPrintable.Encode(new byte[200]); // long input → 76-column lines, each soft-wrapped with '='
```

## Decoding options

Decoding is **strict by default**. The relaxations are opt-in:

| Flag | Effect |
|---|---|
| *(none)* | Uppercase `=HH` only; bare LF rejected; trailing literal whitespace rejected |
| `AllowLowercaseHex` | Accept `=3d` as well as `=3D` |
| `AllowBareLineFeed` | Accept a bare `LF` as a hard break and `=\n` as a soft break |
| `IgnoreTrailingWhitespace` | Delete transport-inserted trailing space / tab instead of rejecting it |

```csharp
QuotedPrintable.Decode("=3d");                                             // FormatException (strict)
QuotedPrintable.Decode("=3d", QuotedPrintableDecodingOptions.AllowLowercaseHex); // { 0x3D }
```

The strict decoder rejects a bare `=` at end of input, `=` plus one character, `=` plus non-hex, a lone CR, non-ASCII
characters, and stray control characters.

## Validation and sizing

```csharp
QuotedPrintable.IsValid("abc=\r\ndef");                  // true (soft break)
QuotedPrintable.IsValid("=GG");                          // false
QuotedPrintable.IsValid(new string('A', 77));            // false — exceeds the 76-char line limit

QuotedPrintable.GetEncodedLength(data);                  // exact encoded length (scans the data)
QuotedPrintable.GetMaxEncodedLength(data.Length);        // worst-case upper bound
QuotedPrintable.TryGetDecodedLength(text, out int n);    // exact decoded length, false if malformed
```

`IsValid` checks **canonical** RFC 2045 conformance — including the 76-character encoded-line limit (the soft-break
`=` counted, the CRLF not). `Decode` is more lenient: it recovers overlong lines that `IsValid` rejects, so
`IsValid(x) == true` implies `Decode(x)` succeeds but not the reverse. `TryGetDecodedLength` mirrors `Decode` (it
ignores the line limit), so it can always size a decode buffer.

## Span path

```csharp
char[] buffer = new char[QuotedPrintable.GetMaxEncodedLength(data.Length)];
bool ok = QuotedPrintable.TryEncode(data, buffer, out int written);
```

`TryEncode` / `TryDecode` never throw for malformed input, invalid options, or an undersized destination — they return
`false` and write `0`.

## Where to go next

- **[Base64 guide](base64.md)** — the other MIME content-transfer encoding, including the 76-column MIME variant.
- **[Percent-encoding guide](percent-encoding.md)** — the URI / form escape encoding, also `=HH`-style but for URLs.
- **[Text & Serialization guides](../topics/text-and-serialization.md)** — every guide in this topic.
