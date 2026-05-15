---
title: The IBinaryEncoding interface
---

# The `IBinaryEncoding` interface

The per-encoding static classes (`Base16`, `Base32`, `Base64`, `Base58`, `Base85`) are the recommended entry point
when the encoding is known at compile time. They are fast, support variant-specific options (line breaks, padding
control, MIME wrapping), and produce the cleanest call sites.

For code that must select the encoding **at runtime** — configuration-driven serializers, plugin pipelines,
generic utilities — the library exposes a unified contract:

```csharp
public interface IBinaryEncoding
{
    string Name { get; }
    string Description { get; }
    int GetMaxEncodedLength(int byteCount);
    int GetMaxDecodedLength(int charCount);
    string Encode(ReadOnlySpan<byte> bytes);
    byte[] Decode(ReadOnlySpan<char> chars);
    bool TryEncode(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten);
    bool TryDecode(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten);
    bool IsValid(ReadOnlySpan<char> source);
}
```

The interface deliberately hides variant-specific encoding options. Code that needs `OmitPadding`,
`InsertLineBreaks`, MIME wrapping, or other per-variant flags should keep using the static classes.

## Ready-made instances

The `BinaryEncodings` static class exposes thread-safe singleton instances for every variant the library ships:

| Property | Underlying static call |
|---|---|
| `BinaryEncodings.Base16Lower` | `Base16.Encode(bytes)` (default lower case) |
| `BinaryEncodings.Base16Upper` | `Base16.Encode(bytes, BaseFormattingOptions.UpperCase)` |
| `BinaryEncodings.Base32` | `Base32.Encode(bytes, Base32Variant.Standard)` |
| `BinaryEncodings.Base32Hex` | `Base32.Encode(bytes, Base32Variant.HexExtended)` |
| `BinaryEncodings.Base32Crockford` | `Base32.Encode(bytes, Base32Variant.Crockford)` |
| `BinaryEncodings.Base32ZBase32` | `Base32.Encode(bytes, Base32Variant.ZBase32)` |
| `BinaryEncodings.Base64` | `Base64.Encode(bytes, Base64Variant.Standard)` |
| `BinaryEncodings.Base64UrlSafe` | `Base64.Encode(bytes, Base64Variant.UrlSafe)` |
| `BinaryEncodings.Base64Mime` | `Base64.Encode(bytes, Base64Variant.Mime)` |
| `BinaryEncodings.Base58` | `Base58.Encode(bytes, Base58Variant.BitcoinFlickr)` |
| `BinaryEncodings.Base58Ripple` | `Base58.Encode(bytes, Base58Variant.Ripple)` |
| `BinaryEncodings.Ascii85` | `Base85.Encode(bytes, Base85Variant.Ascii85)` |
| `BinaryEncodings.Z85` | `Base85.Encode(bytes, Base85Variant.Z85)` |

```csharp
using Bodu.Text.Encoding;

IBinaryEncoding encoding = BinaryEncodings.Base64UrlSafe;

string token = encoding.Encode(secretKey);
byte[] back  = encoding.Decode(token);
```

## Runtime lookup by name

`BinaryEncodings.Get(name)` resolves a case-insensitive string identifier to the matching instance. The recognised
names include canonical forms and common aliases:

| Canonical | Aliases |
|---|---|
| `base16-lower` | `base16`, `hex` |
| `base16-upper` | `hex-upper` |
| `base32` | — |
| `base32hex` | — |
| `base32-crockford` | — |
| `z-base-32` | `zbase32` |
| `base64` | — |
| `base64-urlsafe` | `base64url` |
| `base64-mime` | — |
| `base58` | `base58-bitcoin`, `base58-flickr` |
| `base58-ripple` | — |
| `ascii85` | `base85` |
| `z85` | — |

```csharp
string encodingName = configuration["TokenEncoding"];   // e.g. "base64-urlsafe"
IBinaryEncoding encoding = BinaryEncodings.Get(encodingName);

string encoded = encoding.Encode(rawBytes);
byte[] decoded = encoding.Decode(encoded);
```

Unknown names throw `ArgumentException`.

## Extension methods

The `BinaryEncodingExtensions` class includes two generic extensions that route through `IBinaryEncoding`:

```csharp
using Bodu.Text.Encoding;

byte[] data = { 0xDE, 0xAD, 0xBE, 0xEF };
IBinaryEncoding encoding = BinaryEncodings.Get(name);

string s    = data.Encode(encoding);   // dispatches through the interface
byte[] back = s.Decode(encoding);
```

It also exposes per-encoding fluent shortcuts that dispatch through the static classes (no virtual call):

```csharp
data.ToBase16String();     // "deadbeef"
data.ToBase32String();     // "32W6P3XO"
data.ToBase64String();     // "3q2+7w=="
data.ToBase58String();     // "6h8cQN"
data.ToBase85String();     // ".(0%g"

"deadbeef".FromBase16String();
"3q2+7w==".FromBase64String();
"6h8cQN".FromBase58String();
```

## Common patterns

### Configuration-driven serialization

```csharp
public sealed class TokenEncoder
{
    private readonly IBinaryEncoding _encoding;

    public TokenEncoder(string encodingName)
    {
        _encoding = BinaryEncodings.Get(encodingName);
    }

    public string Encode(ReadOnlySpan<byte> payload) => _encoding.Encode(payload);
    public byte[] Decode(string token) => _encoding.Decode(token);
}
```

### Plugin pipeline

```csharp
public interface ITokenStage
{
    IBinaryEncoding InputEncoding { get; }
    IBinaryEncoding OutputEncoding { get; }
    byte[] Process(byte[] input);
}

public static byte[] Run(ITokenStage stage, string input)
{
    byte[] bytes = stage.InputEncoding.Decode(input);
    byte[] output = stage.Process(bytes);
    return output; // caller wraps with stage.OutputEncoding.Encode
}
```

### Generic round-trip helper

```csharp
public static bool TryRoundTrip(IBinaryEncoding encoding, ReadOnlySpan<byte> data)
{
    string encoded = encoding.Encode(data);
    byte[] back = encoding.Decode(encoded);
    return data.SequenceEqual(back);
}
```

## When to *not* use the interface

The interface is a convenience for the runtime-selection use case. Skip it when:

- The encoding is known at compile time — use the static class directly for the cleanest call site.
- You need variant-specific options like `BaseFormattingOptions.InsertLineBreaks`,
  `BaseFormattingOptions.OmitPadding`, or `BaseFormatStyles.AllowPrefix` — those are not exposed via the interface
  by design (each encoding has different option semantics).
- You need the `UTF-8` byte path or the `OperationStatus` streaming decode — those are only available on the
  static classes.

## Where to go next

- **[Base16 guide](base16.md)**, **[Base32 guide](base32.md)**, **[Base64 guide](base64.md)**,
  **[Base58 guide](base58.md)**, **[Base85 guide](base85.md)** — the static-class entry points with the full
  option set.
- **[Core concepts](../../docs/text-encoding/concepts.md)** — vocabulary refresher.
