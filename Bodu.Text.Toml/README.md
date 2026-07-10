# Bodu.Text.Toml

> **API stability — Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

A TOML (v1.0.0 / v1.1.0) library for .NET 8. It maps plain CLR objects to and from TOML through a configurable converter model, over a low-level forward-only token reader and writer, with both a mutable and a read-only document object model.

## Installation

```shell
dotnet add package Bodu.Text.Toml
```

Targets `net8.0`.

## API shape

The public surface matches the sibling `Bodu.Text.Bencode` library, so the patterns transfer directly between the two. The types are organised into folders/namespaces by surface:

| Type(s) | Namespace | Role |
|---|---|---|
| `TomlSerializer` / `TomlSerializerOptions` / `TomlSerializerDefaults` | `Bodu.Text.Toml` | Static serializer entry point, its configuration, and scenario presets (for example `Web`). |
| `TomlNamingPolicy`, `TomlTokenType`, `TomlValueKind` | `Bodu.Text.Toml` | Property naming policies, token classification, and value-kind classification. |
| `TomlFormatException` / `TomlSerializationException` | `Bodu.Text.Toml` | Failures split by cause: malformed input vs. values that cannot be mapped. |
| `Utf8TomlReader` (+ `TomlReaderOptions`) | `Bodu.Text.Toml.Reader` | Forward-only, allocation-free `ref struct` token reader. |
| `Utf8TomlWriter` (+ `TomlWriterOptions`) | `Bodu.Text.Toml.Writer` | Forward-only `ref struct` token writer. |
| `TomlDocument` / `TomlElement` / `TomlProperty` | `Bodu.Text.Toml.Document` | Read-only, low-allocation document object model. |
| `TomlNode` / `TomlObject` / `TomlArray` / `TomlValue` | `Bodu.Text.Toml.Nodes` | Mutable document object model: parse, edit, write back. |
| `TomlConverter<T>` / `TomlConverterFactory`, `[TomlPropertyName]` / `[TomlIgnore]` / … | `Bodu.Text.Toml.Serialization` | Custom converters and the per-member attribute family. |

```csharp
using Bodu.Text.Toml;

string toml = TomlSerializer.Serialize(new ServerConfig { Host = "localhost", Port = 8080 });
ServerConfig config = TomlSerializer.Deserialize<ServerConfig>(toml);

// Document object models
using Bodu.Text.Toml.Nodes;
TomlNode node = TomlNode.Parse(utf8Toml)!;
node["server"]!["port"] = 9090;
byte[] back = node.ToUtf8Bytes();
```

- **Value mapping:** `string`/`char`/`Guid`/`Uri` → string, the integer family → integer, `double`/`float` → float, `bool` → boolean, and `DateTimeOffset`/`DateTime`/`DateOnly`/`TimeOnly` → the four RFC 3339 date-time forms; `byte[]` → an integer array (or a Base64 string via `TomlSerializerOptions.ByteArrayHandling`); enums → member-name strings. `decimal` and `TimeSpan` have no native TOML form and require a registered `TomlConverter<T>`.
- A TOML document root must map to a **table**, so a top-level scalar or array throws. Output is normalized TOML in document order — deterministic for a given input, with member order preserved (so `[TomlPropertyOrder]` is honored). It is not a canonical form: two equal documents built in different member orders serialize differently, so the output is unsuitable for hashing or signing.
- The full serializer feature surface is present: converters and factories, the attribute family (`TomlPropertyName`/`Ignore`/`Converter`/`PropertyOrder`/`Constructor`/`Required`/`Include`/`ExtensionData`/`NamingPolicy`/`UnmappedMemberHandling`/`ObjectCreationHandling`/`StringEnumMemberName`), the `ITomlOn*` serialization callbacks, naming policies and `TomlSerializerDefaults.Web`, and the string/number enum converters.
- Because the reader/writer/document types live in sub-namespaces, code that uses them alongside the serializer imports `using Bodu.Text.Toml.Reader;` / `.Writer;` / `.Document;` / `.Nodes;` as needed.
- Failures surface through `TomlFormatException` (malformed input, with line/column/offset) and `TomlSerializationException` (binding failures).

## Runnable samples

The repository ships offline, `dotnet run`-able sample projects for this package — the
`TomlSerializer` POCO surface (temporal kinds, naming policies and attributes, the wire
knobs) and the layers beneath it (both DOMs, the token reader/writer, streaming reads) —
under [`samples/Text.Toml/`](https://github.com/bslater/bodu/tree/master/samples/Text.Toml).

## Testing

Tests live in `test/` as MSTest classes mirroring `src/`. Run tiers via the runsettings files at the solution root:

```bash
dotnet test Bodu.Text.Toml/test/Bodu.Text.Toml.Test.csproj --settings bvt.runsettings
dotnet test Bodu.Text.Toml/test/Bodu.Text.Toml.Test.csproj --settings regression.runsettings
```

## License

MIT. © Bodu Pty. Ltd.
