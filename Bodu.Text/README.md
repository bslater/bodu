# Bodu.Text

Text-encoding utilities for .NET 8: byte-order-mark encoding detection and a broad span- and UTF-8-friendly extension surface over `System.Text.Encoding`, `ReadOnlySpan<char>`, `ReadOnlySpan<byte>`, and `string`. The helpers fill the gaps the BCL leaves around preamble handling, allocation-free transcoding, exact-length `GetBytes`/`GetChars`, and encoding classification.

## Installation

```shell
dotnet add package Bodu.Text
```

Targets `net8.0`. All types live in the `Bodu.Text` namespace.

## Encoding detection

`EncodingDetection.TryDetectByPreamble(ReadOnlySpan<byte>, out Encoding?)` identifies UTF-8, UTF-16 (LE/BE), and UTF-32 (LE/BE) from a leading byte-order mark.

## Extension surface

`EncodingExtensions` (on `Encoding` and spans) and `StringEncodingExtensions` (on `string`) group their helpers by concern:

| Concern | Representative members |
|---|---|
| UTF-8 fast paths | `ToUtf8Bytes`, `GetUtf8ByteCount`, `EncodeUtf8To`, `TryEncodeUtf8To`, `FromUtf8`, `DecodeUtf8To` |
| Preamble handling | `HasPreamble`, `GetPreambleLength`, `TryWritePreamble`, `StripPreamble`, `GetBytesWithPreamble`, `GetStringSkippingPreamble` |
| Transcoding | `Transcode`, `TranscodeTo`, `TryTranscodeTo` |
| Exact / try conversions | `GetBytesExactly`, `GetCharsExactly`, `TryGetBytes`, `TryGetChars` |
| Classification | `IsUtf8`, `IsUtf16LittleEndian`, `IsUtf32BigEndian`, `IsAnyUtf`, `IsAscii`, `GetDisplayName` |
| Fallback control | `WithExceptionFallbacks`, `WithReplacementFallbacks`, `UsesExceptionFallbacks` |
| Buffer writers | `WriteBytes`, `WriteChars`, `WritePreamble`, `WriteBytesWithPreamble` (on `IBufferWriter<>`) |

```csharp
using Bodu.Text;

if (EncodingDetection.TryDetectByPreamble(bytes, out var encoding))
{
    string text = encoding!.GetStringSkippingPreamble(bytes);
}

byte[] utf8 = "café".AsSpan().ToUtf8Bytes();
byte[] latin1 = utf8.Transcode(Encoding.UTF8, Encoding.Latin1);
```

> Base-N binary encodings (Base16/32/58/64/85, …) live in the sibling `Bodu.Text.Encoding` package; document formats (CSV, INI, .env, Bencode, TOML) live in `Bodu.Text.Formats`.

## Testing

Tests live in `test/` as MSTest partial classes mirroring `src/`. Run tiers via the runsettings files at the solution root:

```bash
dotnet test Bodu.Text/test/Bodu.Text.Test.csproj --settings bvt.runsettings
dotnet test Bodu.Text/test/Bodu.Text.Test.csproj --settings regression.runsettings
```

## License

MIT. © Bodu Pty. Ltd.
