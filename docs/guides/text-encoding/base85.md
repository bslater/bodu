---
title: Using Base85 (Ascii85 and Z85)
---

# Using Base85 (Ascii85 and Z85)

`Base85` packs four input bytes into a 32-bit unsigned integer, then divides by 85 four times to emit five output
characters. The payload expansion is **25 %** — the smallest of any encoding the library ships, but at the cost
of a denser alphabet that includes ASCII punctuation.

```
input bytes  : 4D 61 6E 20                 (ASCII "Man ", 4 bytes)
              ┌── 32-bit unsigned big-endian = 0x4D616E20 ──┐
              │ 1298230816 / 85⁴ = 24 r 45,415,816          │
              │ 45,415,816 / 85³ = 73 r 584,691             │
              │ 584,691     / 85² = 80 r 6,691              │
              │ 6,691       / 85  = 78 r 61                 │
              │ 61                                          │
              └── digits 24, 73, 80, 78, 61                 ┘
encoded text : 9jqo^                       (Ascii85; alphabet '!' (33) + digit)
              k_kxi                         (Z85; different alphabet ordering)
```

## Quick reference

```csharp
using Bodu.Text.Encoding;

byte[] data = { 0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE, 0xBA, 0xBE };

// Adobe Ascii85 (default)
string a = Base85.Encode(data);
// 8 bytes → 10 characters

// ZeroMQ Z85
string z = Base85.Encode(data, Base85Variant.Z85);
// 8 bytes → 10 characters (different alphabet)

// Decode
byte[] back = Base85.Decode(a);
```

## Variant comparison

| Variant | Alphabet | `z` shortcut | Partial groups | Input alignment |
|---|---|---|---|---|
| `Ascii85` (Adobe Tech Note 5045) | `!` (33) through `u` (117) — 85 contiguous ASCII characters | Yes — `z` represents 4 zero bytes | **Yes** — 1, 2, or 3-byte tails permitted | Any length |
| `Z85` (RFC 32 — ZeroMQ) | `0-9 a-z A-Z .-:+=^!/*?&<>()[]{}@%$#` — shell-safe, no quote or backslash | No | **No** | **Multiple of 4 bytes** |

### When to pick each

| Variant | Reach for |
|---|---|
| `Ascii85` | PDF / PostScript embedded binary, Adobe Tech Note 5045-compatible streams, dense Base85 with shortcut |
| `Z85` | ZeroMQ wire keys, shell-pasted binary keys (alphabet avoids quote / backslash / semicolon) |

## The `z` shortcut (Ascii85 only)

Adobe Ascii85 reserves the character `z` (ASCII 122 — *outside* the `!`–`u` alphabet) as a shortcut for four
consecutive zero bytes. The encoder emits it automatically:

```csharp
byte[] zeros = new byte[8];
string encoded = Base85.Encode(zeros);   // "zz" — two shortcuts, not 10 chars

byte[] back = Base85.Decode("zz");        // 8 zero bytes
```

The shortcut is only valid at a **group boundary**. The decoder rejects `z` mid-group:

```csharp
Base85.Decode("9jz");                     // FormatException — z after partial group
```

Z85 has no shortcut — all-zero input emits five `0` characters per group.

## Partial groups (Ascii85 only)

Ascii85 allows trailing partial groups of 1, 2, or 3 bytes. The encoder pads the trailing bytes with zeros to fill
a 4-byte group, encodes the full group, then emits **(1 + remaining)** characters from the result:

| Trailing bytes | Encoded characters |
|---|---|
| 1 | 2 |
| 2 | 3 |
| 3 | 4 |
| 4 (full group) | 5 |

The decoder reverses this: a trailing partial group of 2, 3, or 4 characters is padded with the maximum digit
value (`u`) to fill 5 characters, decoded, then truncated to **(input - 1)** bytes.

```csharp
Base85.Encode(new byte[] { 0x00 });          // "!!"  (1 byte → 2 chars)
Base85.Encode(new byte[] { 0x00, 0x00 });    // "!!!" (2 bytes → 3 chars)
Base85.Encode(new byte[] { 0x00, 0x00, 0x00 }); // "!!!!" (3 bytes → 4 chars)
```

A single trailing character (or six characters — full group plus one) is rejected:

```csharp
Base85.Decode("9");                          // FormatException — single trailing char invalid
Base85.Decode("uuuuuu");                     // FormatException — full group + 1 invalid
```

## Z85 alignment requirement

Z85 enforces input alignment per RFC 32: encoder input length **must** be a multiple of four bytes, decoder input
length must be a multiple of five characters. Non-aligned input throws `ArgumentException` on encode and
`FormatException` on decode.

```csharp
Base85.Encode(new byte[5], Base85Variant.Z85);  // ArgumentException — 5 is not a multiple of 4
Base85.Decode("HelloW", Base85Variant.Z85);     // FormatException — 6 is not a multiple of 5
```

## Z85 shell-safe alphabet

Z85's defining feature is its alphabet choice — it avoids characters that shells, JSON, or quoted strings would
need to escape:

```
0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ.-:+=^!/*?&<>()[]{}@%$#
```

Notice the absence of `"`, `'`, `\`, `;`, `|`, `` ` `` — exactly the characters that would require escaping in
shell commands or in JSON / XML string literals. This makes Z85 a popular choice for binary keys that need to be
pasted into shell sessions or embedded directly in configuration files.

## Lenient parsing

| Flag | Effect |
|---|---|
| `BaseFormatStyles.IgnoreWhitespace` | Strip ASCII space / tab / CR / LF anywhere |
| `BaseFormatStyles.AllowPrefix` | No-op for Base85 |
| `BaseFormatStyles.AllowMissingPadding` | No-op for Base85 (no padding character) |

## Span path

```csharp
int maxChars = Base85.GetMaxEncodedLength(data.Length);
char[] buffer = new char[maxChars];

int written = Base85.Encode(data, buffer);
ReadOnlySpan<char> result = buffer.AsSpan(0, written);
```

Like Base58, Base85 is not streamable: the `EncodeToUtf8` / `DecodeFromUtf8` overloads exist for API consistency
with the other encodings but treat the input as a single block.

## Validation and sizing

```csharp
Base85.IsValid("9jqo^");                              // true (Ascii85)
Base85.IsValid("9jqov");                              // false ('v' is ASCII 118, above 'u')
Base85.IsValid("z");                                  // true (z shortcut)
Base85.IsBase85Digit('!');                            // true
Base85.IsBase85Digit('z');                            // false (z is the shortcut, not a digit)

Base85.GetEncodedLength(4, Base85Variant.Ascii85);     // 5 (no shortcut path)
Base85.GetEncodedLength(4, Base85Variant.Z85);         // 5
Base85.GetMaxDecodedLength(5);                         // 4
```

## Common patterns

### Embedded binary in a config file

```csharp
byte[] secret = Convert.FromBase64String(env);
string z85Key = Base85.Encode(secret, Base85Variant.Z85);
// z85Key can be pasted into JSON, YAML, or a shell variable without escaping
```

### PostScript / PDF Ascii85 stream

```csharp
string Stream(ReadOnlySpan<byte> binary) => "<~" + Base85.Encode(binary) + "~>";
// PostScript convention: <~…~> delimits an Ascii85 region
```

(The library does not add or strip the `<~`/`~>` delimiters automatically — they are a PostScript convention, not
part of the encoding itself.)

## Where to go next

- **[Base58 guide](base58.md)** — when the use case is blockchain or human-typed identifiers.
- **[Base64 guide](base64.md)** — when familiarity beats density.
- **[`IBinaryEncoding` interface](binary-encodings-interface.md)** — runtime-selected encoding choice.
