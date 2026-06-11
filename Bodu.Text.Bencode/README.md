# Bodu.Text.Bencode

A Bencode (BEP 3) serializer for .NET 8, shaped after `System.Text.Json`. It maps plain CLR objects to and from Bencode through a configurable converter model, over a low-level, forward-only token reader and writer.

## Installation

```shell
dotnet add package Bodu.Text.Bencode
```

Targets `net8.0`.

## API shape

The public surface mirrors `System.Text.Json`, so the patterns are familiar:

| System.Text.Json | Bodu.Text.Bencode | Namespace |
|---|---|---|
| `JsonSerializer` | `BencodeSerializer` | `Bodu.Text.Bencode` |
| `JsonSerializerOptions` | `BencodeSerializerOptions` | `Bodu.Text.Bencode` |
| `Utf8JsonReader` / `Utf8JsonWriter` | `Utf8BencodeReader` / `Utf8BencodeWriter` | `Bodu.Text.Bencode` |
| `JsonTokenType` | `BencodeTokenType` | `Bodu.Text.Bencode` |
| `JsonNamingPolicy` | `BencodeNamingPolicy` | `Bodu.Text.Bencode` |
| `JsonConverter<T>` / `JsonConverterFactory` | `BencodeConverter<T>` / `BencodeConverterFactory` | `Bodu.Text.Bencode.Serialization` |
| `[JsonPropertyName]` / `[JsonIgnore]` | `[BencodePropertyName]` / `[BencodeIgnore]` | `Bodu.Text.Bencode.Serialization` |

```csharp
using Bodu.Text.Bencode;

byte[] payload = BencodeSerializer.Serialize(new TorrentInfo { Name = "ubuntu.iso", Length = 1024 });
TorrentInfo info = BencodeSerializer.Deserialize<TorrentInfo>(payload);
```

- `Serialize` / `Deserialize` over `byte[]`, `ReadOnlySpan<byte>`, `IBufferWriter<byte>`, and `Stream`, with async stream variants.
- Output is always canonical Bencode: dictionary entries are emitted in ascending bytewise key order.
- Strings and `byte[]` map to byte strings, the integer family to `i…e`, and enums to member-name byte strings. Types with no canonical Bencode form (Booleans, floating-point, date-times) require a registered `BencodeConverter<T>`; a `null` member is omitted on write.
- `Utf8BencodeReader` and `Utf8BencodeWriter` expose the low-level token surface directly for callers that do not want POCO mapping. The reader accepts only canonical BEP 3 (no leading or negative zeros, ascending unique dictionary keys, a single root with no trailing bytes).
- Failures surface through `BencodeFormatException` (malformed bytes) and `BencodeSerializationException` (binding failures).

## Testing

Tests live in `test/` as MSTest classes mirroring `src/`. Run tiers via the runsettings files at the solution root:

```bash
dotnet test Bodu.Text.Bencode/test/Bodu.Text.Bencode.Test.csproj --settings bvt.runsettings
dotnet test Bodu.Text.Bencode/test/Bodu.Text.Bencode.Test.csproj --settings regression.runsettings
```

## License

MIT. © Bodu Pty. Ltd.
