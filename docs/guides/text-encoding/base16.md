---
title: Using Base16 (hexadecimal)
---

# Using Base16 (hexadecimal)

`Base16` is the simplest of the encoding family — every input byte maps to exactly two hex characters, so 100 %
payload expansion. It is the canonical form for hash digests, low-level binary inspection, and any time the data
needs to be human-readable at the byte boundary.

```
input bytes  : DE AD BE EF                 (4 bytes)
encoded text : deadbeef                    (8 chars, lower case default)
              "DEADBEEF"                    (UpperCase)
              "0xDEADBEEF"                  (UpperCase | IncludePrefix)
              "DE AD BE EF"                 (UpperCase | InsertSpacing)
              "0xDE AD BE EF"               (UpperCase | IncludePrefix | InsertSpacing)
```

## Quick reference

```csharp
using Bodu.Text.Encoding;

byte[] data = { 0xDE, 0xAD, 0xBE, 0xEF };

// Encode
string lower = Base16.Encode(data);                                  // "deadbeef"
string upper = Base16.Encode(data, BaseFormattingOptions.UpperCase); // "DEADBEEF"

// Or select the case with the variant overload, matching the uniform
// (bytes, variant, options) shape used by Base32/58/64/85:
string up2  = Base16.Encode(data, Base16Variant.Upper);              // "DEADBEEF"

// Decode (strict — no decorations, even length, alphabet-only)
byte[] back  = Base16.Decode("deadbeef");
byte[] mixed = Base16.Decode("DeAdBeEf");                            // case-insensitive

// Decode (lenient — 0x prefix, whitespace)
byte[] dump = Base16.Decode(
    "0xDE AD BE EF",
    BaseFormatStyles.AllowPrefix | BaseFormatStyles.IgnoreWhitespace);
```

## Formatting decorations

`BaseFormattingOptions` is a flag enum — combine flags with `|`:

| Flag | Effect |
|---|---|
| `None` | Default: lower case, continuous, no decoration |
| `UpperCase` | Output upper-case digits |
| `IncludePrefix` | Prepend `0x` |
| `InsertSpacing` | Single space between byte pairs (no trailing space) |
| `InsertLineBreaks` | `\r\n` every 64 encoded characters |

### Decoration examples

```csharp
byte[] hash = SHA256.HashData("hello"u8);

Base16.Encode(hash);
// "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824"

Base16.Encode(hash, BaseFormattingOptions.UpperCase | BaseFormattingOptions.InsertSpacing);
// "2C F2 4D BA 5F B0 A3 0E 26 E8 3B 2A C5 B9 E2 9E 1B 16 1E 5C 1F A7 42 5E 73 04 33 62 93 8B 98 24"

Base16.Encode(hash, BaseFormattingOptions.UpperCase | BaseFormattingOptions.IncludePrefix | BaseFormattingOptions.InsertLineBreaks);
// "0x2CF24DBA5FB0A30E26E83B2AC5B9E29E1B161E5C1FA7425E73043362938B9824"
```

`InsertLineBreaks` breaks at 64 chars to match the PEM / hex-dump convention; `InsertSpacing` adds one space
between every encoded byte. The two are independent.

## Lenient parsing

The decoder is **strict** by default — alphabet only, even length, no decorations. The `BaseFormatStyles` flags
relax specific rules:

| Flag | Effect |
|---|---|
| `None` | Strict mode — exact even-length hex digits, nothing else |
| `AllowPrefix` | Tolerate a leading `0x` / `0X` |
| `IgnoreWhitespace` | Strip ASCII space, tab, CR, LF anywhere in the input |

```csharp
// All three of these recover the same bytes when the matching flags are set:
Base16.Decode("0xDEADBEEF",      BaseFormatStyles.AllowPrefix);
Base16.Decode("DE AD BE EF",     BaseFormatStyles.IgnoreWhitespace);
Base16.Decode("0xDE AD BE EF",   BaseFormatStyles.AllowPrefix | BaseFormatStyles.IgnoreWhitespace);
```

Non-ASCII whitespace (no-break space, em space) is *not* stripped — `IgnoreWhitespace` is deliberately strict
about which characters count as whitespace.

## BCL-style aliases

If you prefer the `System.Convert` naming convention:

```csharp
Base16.ToHexString(data);        // "DEADBEEF" (upper case, matches Convert.ToHexString)
Base16.ToHexStringLower(data);   // "deadbeef" (matches Convert.ToHexStringLower from .NET 9+)
Base16.FromHexString("DEADBEEF");// byte[] { 0xDE, 0xAD, 0xBE, 0xEF }
```

The fluent extension methods are also available:

```csharp
data.ToBase16String();           // lower case (default Bodu form)
"DEADBEEF".FromBase16String();   // byte[] { 0xDE, 0xAD, 0xBE, 0xEF }
```

## Span and UTF-8 paths

When you have a destination buffer and want to avoid the `string` allocation:

```csharp
Span<char> buffer = stackalloc char[Base16.GetEncodedLength(data.Length)];
int written = Base16.Encode(data, buffer);
// or non-throwing:
if (Base16.TryEncode(data, buffer, out int charsWritten)) { … }
```

For UTF-8 byte buffers (network / file pipelines):

```csharp
byte[] utf8 = Base16.EncodeToUtf8(data);                 // ASCII bytes — bit-identical to chars
Base16.TryEncodeToUtf8(data, utf8Buffer, out int len);

// Streaming decode using OperationStatus
var status = Base16.DecodeFromUtf8(
    utf8Source,
    byteDestination,
    out int bytesConsumed,
    out int bytesWritten,
    BaseFormatStyles.None,
    isFinalBlock: false);
```

`Base16.DecodeFromUtf8` with `isFinalBlock: false` returns `OperationStatus.NeedMoreData` when the chunk ends on
an odd nibble — the partial pair is left unconsumed for the next call.

## Validation and sizing helpers

```csharp
Base16.IsValid("deadbeef");        // true
Base16.IsValid("dead beef");       // false (whitespace under strict mode)
Base16.IsValid("0xdead", BaseFormatStyles.AllowPrefix); // false — odd digit count after prefix
Base16.IsHexDigit('A');            // true
Base16.IsHexDigit('g');            // false

Base16.GetEncodedLength(32);                                    // 64 (bytes * 2)
Base16.GetEncodedLength(32, BaseFormattingOptions.InsertSpacing); // 95 (32*3-1)
Base16.GetMaxDecodedLength(64);                                 // 32 (chars / 2)
Base16.GetDecodedLength("deadbeef");                            // 4 (exact)
```

## Common patterns

### Diagnostic hex dump

```csharp
string Dump(ReadOnlySpan<byte> bytes) =>
    Base16.Encode(bytes,
        BaseFormattingOptions.UpperCase
            | BaseFormattingOptions.InsertSpacing
            | BaseFormattingOptions.InsertLineBreaks);
```

### Hash-digest formatter

```csharp
string FormatDigest(byte[] hash) => Base16.ToHexStringLower(hash);
```

### CLI / env-var input parser

```csharp
byte[] ParseHexInput(string input) =>
    Base16.Decode(input,
        BaseFormatStyles.AllowPrefix | BaseFormatStyles.IgnoreWhitespace);
```

## Where to go next

- **[Base32 guide](base32.md)** — when 60 % expansion is enough and you need a smaller alphabet.
- **[Base64 guide](base64.md)** — when 33 % expansion matters more than human readability.
- **[`IBinaryEncoding` interface](binary-encodings-interface.md)** — runtime-selected encoding choice.
