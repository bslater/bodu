# Bodu.Text.Serialization.Bencode

A `System.Text.Json`-style POCO serializer for Bencode (BitTorrent [BEP 3](https://www.bittorrent.org/beps/bep_0003.html)) on .NET 8, built on `Bodu.Text.Serialization`. Map your types to and from torrent-style payloads with converters, attributes, and naming policies.

## Installation

```shell
dotnet add package Bodu.Text.Serialization.Bencode
```

Targets `net8.0`.

## API shape

```csharp
using Bodu.Text.Serialization.Bencode;

byte[] payload = BencodeSerializer.Serialize(new FileEntry { Name = "ubuntu.iso", Length = 1024 });
FileEntry entry = BencodeSerializer.Deserialize<FileEntry>(payload);
```

- `Serialize<T>` returns `byte[]`, writes to an `IBufferWriter<byte>`, or writes to a `Stream` (async).
- `Deserialize<T>` reads a `ReadOnlySpan<byte>`, a `byte[]`, or a `Stream` (async).
- The low-level `BencodeSyntaxTree.Parse` returns a lossless concrete syntax tree whose `ToByteArray()` reproduces the source bytes exactly.

## Type mapping

| .NET | Bencode |
|---|---|
| `string` | byte string (UTF-8) |
| `byte[]` | byte string |
| integer types | integer (`i…e`) |
| `enum` | byte string (member name) |
| arrays, lists | list (`l…e`) |
| objects, string-keyed dictionaries | dictionary (`d…e`), keys in canonical order |

Bencode has no Boolean, floating-point, or date-time kind: those types are rejected unless a converter (`FormatConverter<T>`) maps them to an integer or byte string.

## Testing

```bash
dotnet test Bodu.Text.Serialization.Bencode/test/Bodu.Text.Serialization.Bencode.Test.csproj --settings bvt.runsettings
```

## License

MIT. © Bodu Pty. Ltd.
