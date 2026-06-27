---
title: Bodu.Text.Encoding — Getting started
---

# Bodu.Text.Encoding — Getting started

Unfamiliar with terms like *alphabet*, *terminal quantum*, *padding*, or *variant*? Read [Core concepts](concepts.md)
first.

## Install

```bash
dotnet add package Bodu.Text.Encoding
```

Targets `net8.0`. The package has a single dependency on `Bodu.Core` for shared throw-helpers; no external NuGet
references.

## Minimal samples

### Encode a hash digest as hex

```csharp
using Bodu.Text.Encoding;

byte[] digest = SHA256.HashData("hello"u8.ToArray());

string lower = Base16.Encode(digest);                                   // canonical Bodu form: lower case, no decoration
string upper = Base16.Encode(digest, BaseFormattingOptions.UpperCase);  // RFC 4648 §8 canonical case
string dump  = Base16.Encode(digest, BaseFormattingOptions.UpperCase
                                   | BaseFormattingOptions.InsertSpacing
                                   | BaseFormattingOptions.IncludePrefix);
// dump → "0x2C F2 4D BA 5F B0 A3 0E 26 E8 3B 2A C5 B9 E2 9E 1B 16 1E 5C 1F A7 42 5E 73 04 33 62 93 8B 98 24"
```

### Decode a JWT token segment

```csharp
using Bodu.Text.Encoding;

// JWT segments are URL-safe Base64 without padding.
string headerSegment = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9";

byte[] headerBytes = Base64.Decode(
    headerSegment,
    Base64Variant.UrlSafe,
    BaseFormatStyles.AllowMissingPadding);

string json = System.Text.Encoding.UTF8.GetString(headerBytes);
// json → "{\"alg\":\"HS256\",\"typ\":\"JWT\"}"
```

### Format a TOTP secret for a user

```csharp
using Bodu.Text.Encoding;

byte[] secret = RandomNumberGenerator.GetBytes(20); // 160-bit shared secret

// Google Authenticator-friendly Base32, no padding, no whitespace.
string display = Base32.Encode(
    secret,
    Base32Variant.Standard,
    BaseFormattingOptions.OmitPadding);
```

### Read a Bitcoin address

```csharp
using Bodu.Text.Encoding;

byte[] payload = Base58.Decode("1NS17iag9jJgTHD1VXjvLCEnZuQ3rJDE9L");
// payload[0] == 0x00 → mainnet P2PKH version byte
// payload[1..21]    → 20-byte HASH160 of the public key
// payload[21..25]   → 4-byte double-SHA256 checksum (Base58Check)
```

### Decode without throwing

```csharp
using Bodu.Text.Encoding;

byte[] buffer = new byte[256];
if (Base16.TryDecode(userInput.AsSpan(), buffer, out int bytesWritten))
{
    var actual = buffer.AsSpan(0, bytesWritten);
    // success
}
else
{
    // malformed input — show an error
}
```

### Stream-decode hex from a network buffer

```csharp
using Bodu.Text.Encoding;
using System.Buffers;

while (TryReadChunk(out ReadOnlySpan<byte> utf8Hex, out bool isFinal))
{
    OperationStatus status = Base16.DecodeFromUtf8(
        utf8Hex,
        outputBuffer,
        out int consumed,
        out int written,
        BaseFormatStyles.None,
        isFinalBlock: isFinal);

    switch (status)
    {
        case OperationStatus.Done:           // chunk fully decoded
        case OperationStatus.NeedMoreData:   // pull more bytes and continue
            break;
        case OperationStatus.DestinationTooSmall:
            // grow output buffer
            break;
        case OperationStatus.InvalidData:
            throw new FormatException("Bad hex");
    }
}
```

### Pick an encoding from configuration at runtime

```csharp
using Bodu.Text.Encoding;

string encodingName = configuration["TokenEncoding"]; // e.g. "base64-urlsafe" or "base32-crockford"
IBinaryEncoding encoding = BinaryEncodings.Get(encodingName);

string token   = encoding.Encode(rawBytes);
byte[] decoded = encoding.Decode(token);
```

### Fluent extension methods

```csharp
using Bodu.Text.Encoding;

byte[] data = { 0xDE, 0xAD, 0xBE, 0xEF };

string hex      = data.ToBase16String();
string base32   = data.ToBase32String();
string base64   = data.ToBase64String();
string base58   = data.ToBase58String();
string ascii85  = data.ToBase85String();

byte[] backFromHex = hex.FromBase16String();
```

### Encode a QR-code payload with Base45

```csharp
using Bodu.Text.Encoding;

// RFC 9285 — the encoding the EU Digital COVID Certificate carries in a QR code.
byte[] payload = "Hello!!"u8.ToArray();
string base45 = Base45.Encode(payload);   // "%69 VD92EX0"

byte[] back = Base45.Decode(base45);
```

### Generate a compact identifier with Base62

```csharp
using Bodu.Text.Encoding;

// URL-safe, no special characters — ideal for short links and slugs.
byte[] random = RandomNumberGenerator.GetBytes(16);
string id = Base62.Encode(random);

byte[] bytes = Base62.Decode(id);
```

### Encode an address with Bech32 / Bech32m

```csharp
using Bodu.Text.Encoding;

// Bech32 carries a human-readable part (HRP), the data, and a checksum together.
// EncodeFromBytes / DecodeToBytes repack 8-bit bytes to and from the 5-bit data part.
byte[] program = Base16.Decode("751e76e8199196d454941c45d1b3a323f1433bd6");
string address = Bech32.EncodeFromBytes("bc", program, Bech32Encoding.Bech32m);

Bech32.DecodeToBytes(address, out string hrp, out byte[] data, out Bech32Encoding scheme);
// hrp == "bc"; data == program; scheme == Bech32Encoding.Bech32m

// Non-throwing form reports which scheme validated the checksum.
if (Bech32.TryDecodeToBytes(address, out string? hrp2, out byte[]? data2, out Bech32Encoding scheme2))
{
    // hrp2 / data2 are non-null; scheme2 tells Bech32 from Bech32m
}
```

### Protect a payload with a checksum (Base58Check)

```csharp
using Bodu.Text.Encoding;

string encoded = Base58Check.Encode(payload);   // payload + 4-byte double-SHA-256 checksum
byte[] decoded = Base58Check.Decode(encoded);   // verifies the checksum, then strips it
```

### Encode a Git binary-patch payload (Base85 Git)

```csharp
using Bodu.Text.Encoding;

// Compact, self-delimiting — round-trips without external metadata.
string compact = Base85.Encode("hello"u8.ToArray(), Base85Variant.Git);   // "Xk~0{Zv"
byte[] back    = Base85.Decode(compact, Base85Variant.Git);

// Exact Git line primitive — always five characters per group; caller tracks the length.
string padded  = Base85.EncodeGitPadded(new byte[] { 0x01 });             // "0RR91"
byte[] bytes   = Base85.DecodeGitPadded(padded, decodedLength: 1);
```

### Encode a MIME message body (Quoted-Printable)

```csharp
using Bodu.Text.Encoding;

byte[] body = "café = møney"u8.ToArray();

string encoded = QuotedPrintable.Encode(body);   // printable run + =HH escapes, 76-column wrapping
byte[] decoded = QuotedPrintable.Decode(encoded);
```

### Escape a value for a URL (percent-encoding)

```csharp
using Bodu.Text.Encoding;

// URI component (default).
string component = PercentEncoding.EncodeString("a/b?c=d");   // "a%2Fb%3Fc%3Dd"

// HTML form field — space becomes '+'.
string field = PercentEncoding.EncodeString("a b+c", mode: PercentEncodingMode.FormUrlEncoded); // "a+b%2Bc"

string value = PercentEncoding.DecodeString("a%2Fb");         // "a/b"
```

## Round-trip example with whitespace tolerance

```csharp
using Bodu.Text.Encoding;

byte[] original = { 0xDE, 0xAD, 0xBE, 0xEF };

string display = Base16.Encode(
    original,
    BaseFormattingOptions.UpperCase
        | BaseFormattingOptions.IncludePrefix
        | BaseFormattingOptions.InsertSpacing
        | BaseFormattingOptions.InsertLineBreaks);
// display →
//   "0xDE AD BE EF"

byte[] recovered = Base16.Decode(
    display,
    BaseFormatStyles.AllowPrefix | BaseFormatStyles.IgnoreWhitespace);

Debug.Assert(original.SequenceEqual(recovered));
```

## Where to go next

- **[Bodu.Text.Encoding guides](../../guides/text-encoding/index.md)** — per-encoding deep dives.
- **[Core concepts](concepts.md)** — vocabulary refresher.
- **[Introduction](index.md)** — type map and scenario index.
- **[Bodu.Text.Encoding API reference](xref:Bodu.Text.Encoding)** — full type-by-type docs.
