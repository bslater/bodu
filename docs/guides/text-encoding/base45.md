---
title: Using Base45
---

# Using Base45

`Base45` implements the encoding defined by [RFC 9285](https://www.rfc-editor.org/rfc/rfc9285) — the compact
alphanumeric format designed to carry binary data inside a QR code's **Alphanumeric mode**. Its best-known deployment
is the EU Digital COVID Certificate (the HCERT payload), but it suits any scenario where bytes must travel through a
QR code's restricted 45-character symbol set with the smallest possible footprint.

Unlike the bit-stream encodings, Base45 packs **two input bytes into three output characters** (a trailing single
byte becomes two characters). A 16-bit group `n = a·256 + b` is written as three base-45 digits, least-significant
first: `c = n % 45`, `d = (n / 45) % 45`, `e = n / 2025`.

```
input bytes        : 41 42          ("AB")
                     └── 0x4142 = 16706 ──┐
                       16706 = 11 + 11·45 + 8·2025
encoded text       : B  B  8        (c=11 → 'B', d=11 → 'B', e=8 → '8'; least-significant first)
```

The alphabet is 45 characters: the digits `0-9`, the upper-case letters `A-Z`, and the nine symbols
`space $ % * + - . / :`.

## Quick reference

```csharp
using Bodu.Text.Encoding;

// RFC 9285 worked examples
Base45.Encode("AB"u8.ToArray());        // "BB8"
Base45.Encode("Hello!!"u8.ToArray());   // "%69 VD92EX0"
Base45.Encode("base-45"u8.ToArray());   // "UJCLQE7W581"

byte[] back = Base45.Decode("%69 VD92EX0");   // "Hello!!"
```

## Why Base45 for QR codes

A QR code in Alphanumeric mode stores 11 bits per two characters (5.5 bits/char) drawn from a fixed 45-symbol set.
Encoding binary as Base64 forces the QR code into the far less dense Byte mode; Base45 keeps it in Alphanumeric mode,
producing a meaningfully smaller symbol. The 50 % size overhead of Base45 itself is more than repaid by the denser QR
encoding it unlocks.

## Space is a data character

The space character is part of the Base45 alphabet, so it carries data — it is **not** ignorable whitespace. The
`IgnoreWhitespace` style strips only tab, carriage return, and line feed; it never strips spaces:

```csharp
Base45.Decode("%69 VD92EX0");                               // valid — the space is data
Base45.Decode("%69 VD9\n2EX0", BaseFormatStyles.IgnoreWhitespace);  // newline stripped, then decoded
```

## Strictness

Base45 is **strict** per RFC 9285. The decoder rejects:

- characters outside the 45-symbol alphabet;
- a final group of a single character (1, 4, 7, … characters — only 2-character and 3-character terminal groups are legal);
- a three-character group whose value exceeds `0xFFFF` (would not fit in two bytes);
- a two-character group whose value exceeds `0xFF`.

```csharp
Base45.Decode("GGW");   // FormatException — 0x10000, out of 16-bit range
Base45.Decode("0");     // FormatException — illegal single-character terminal group
```

`Decode` throws `FormatException` on any of these; `TryDecode` returns `false` instead.

## Span path and sizing

Because Base45 packs in fixed groups, the encoded length is **exact**, not an upper bound:

```csharp
int chars = Base45.GetEncodedLength(payload.Length);     // exact output length
Span<char> destination = stackalloc char[chars];
int written = Base45.Encode(payload, destination);

int maxBytes = Base45.GetMaxDecodedLength(text.Length);  // decode upper bound
```

Base45 is **not streamable** — it has no `OperationStatus` path. Each call needs the entire input: pass the whole
payload (encode) or the whole string (decode) as a single span.

## Validation

```csharp
Base45.IsValid("BB8");        // true
Base45.IsValid("bb8");        // false — lower case is not in the alphabet
Base45.IsBase45Digit(' ');    // true  — space is a data symbol
Base45.IsBase45Digit('a');    // false
```

`IsValid` returns `true` only for input the strict decoder would accept (subject to any `BaseFormatStyles` you pass).

## Where to go next

- **[Base62 guide](base62.md)** — compact identifiers without QR-specific constraints.
- **[Base64 guide](base64.md)** — when density matters less than ubiquity.
- **[`IBinaryEncoding` interface](binary-encodings-interface.md)** — Base45 is registered as `BinaryEncodings.Base45` for runtime selection.
