# Bodu.Text.Encoding

> **API stability — Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

Binary-to-text encodings for .NET 8 — Base16, Base32, Base45, Base58, Base62, Base64, Base85, and Bech32 — with a uniform static API, variant selection, encode-time formatting options, and decode-time leniency styles. Every encoding exposes span, string, UTF-8, and `IBufferWriter<>` surfaces with `Try*` and length-calculation overloads for allocation-free use.

## Installation

```shell
dotnet add package Bodu.Text.Encoding
```

Targets `net8.0`. All types live in the `Bodu.Text.Encoding` namespace.

## Encodings and variants

| Encoding | Variants (`*Variant` enum) | Notes |
|---|---|---|
| `Base16` | `Lower`, `Upper` | Hexadecimal |
| `Base32` | `Standard` (RFC 4648), `HexExtended` (RFC 4648 §7), `Crockford`, `ZBase32` | |
| `Base45` | — | RFC 9285 (QR payloads) |
| `Base58` | `BitcoinFlickr`, `Ripple` | `Base58Check` adds a checksum for address-style payloads |
| `Base62` | — | |
| `Base64` | `Standard` (RFC 4648), `UrlSafe` (§5), `Mime` (RFC 2045) | `Base64Url` wraps the BCL URL-safe helpers |
| `Base85` | `Ascii85` (Adobe/PDF), `Z85` (ZeroMQ) | |
| `Bech32` | `Bech32Encoding` checksum variants | |

## API shape

Each encoding is a static class with a consistent surface:

```csharp
using Bodu.Text.Encoding;

string hex = Base16.Encode(data, Base16Variant.Upper);
byte[] back = Base16.Decode(hex);

// Variant + formatting options
string b64 = Base64.Encode(data, Base64Variant.UrlSafe, BaseFormattingOptions.OmitPadding);

// Allocation-free
Span<char> dest = stackalloc char[Base32.GetEncodedLength(data.Length)];
Base32.TryEncode(data, dest, out int written, Base32Variant.Crockford);
```

- `Encode` / `Decode`, `TryEncode` / `TryDecode`, `GetEncodedLength` / `GetDecodedLength`.
- UTF-8 surfaces (`EncodeToUtf8`, `DecodeFromUtf8`) and `IBufferWriter<byte>` / `IBufferWriter<char>` overloads via `BinaryEncodingExtensions`.
- `BinaryEncodings` provides variant-aware `IBinaryEncoding` instances for polymorphic selection.

`BaseFormattingOptions` (encode) toggles casing, line breaks, prefixes, spacing, and padding omission; `BaseFormatStyles` (decode) controls leniency — prefix tolerance, whitespace skipping, missing padding, and canonical-form enforcement.

## Runnable samples

The repository ships offline, `dotnet run`-able sample projects for this package — the
catalogue and variant tour, the formatting/parse-style knobs, checksummed Base58Check and
Bech32 corruption detection, the `BinaryEncodings` registry, and a custom Base36 codec proven
by the shipped contract-test base — under
[`samples/Text.Encoding/`](https://github.com/bslater/bodu/tree/master/samples/Text.Encoding).

## Testing

Tests live in `test/` as MSTest partial classes mirroring `src/`. Run tiers via the runsettings files at the solution root:

```bash
dotnet test Bodu.Text.Encoding/test/Bodu.Text.Encoding.Test.csproj --settings bvt.runsettings
dotnet test Bodu.Text.Encoding/test/Bodu.Text.Encoding.Test.csproj --settings regression.runsettings
```

Every encoding is held to the shared `BinaryEncodingContractTests<TEncoding>` base and driven by published `BinaryEncodingKat` vectors, with invalid-input handling pinned by `InvalidEncodedTextKat` rows.

## License

MIT. © Bodu Pty. Ltd.
