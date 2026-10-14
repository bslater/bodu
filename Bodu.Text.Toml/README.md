# Bodu.Text.Toml

> **API stability - Stable.** The public API surface is committed: breaking changes are reserved for a major-version bump per [SemVer](https://semver.org), except a deliberate break that is recorded in the release notes and in the package's `CompatibilitySuppressions.xml`. See [versioning and support](https://bslater.github.io/bodu/docs/versioning-and-support.html).

A TOML (v1.0.0 / v1.1.0) library for .NET 8 and .NET 10. It maps plain CLR objects to and from TOML through a configurable converter model, over a low-level forward-only token reader and writer, with both a mutable and a read-only document object model.

## Installation

```shell
dotnet add package Bodu.Text.Toml
```

Targets `net8.0` and `net10.0`.

## API shape

The public surface follows the same `System.Text.Json`-shaped architecture as the sibling `Bodu.Text.Bencode` and `Bodu.Text.Yaml` libraries, so the patterns transfer directly between them; the options differ where the formats do (for example, `PropertyNameCaseInsensitive` defaults to `false` here and to `true` in Bencode). The types are organised into folders/namespaces by surface:

| Type(s) | Namespace | Role |
|---|---|---|
| `TomlSerializer` / `TomlSerializerOptions` / `TomlSerializerDefaults` | `Bodu.Text.Toml` | Static serializer entry point, its configuration, and scenario presets (for example `Web`). |
| `TomlTokenType`, `TomlValueKind` | `Bodu.Text.Toml` | Token classification and value-kind classification (naming policies are the shared `Bodu.Text.Serialization.NamingPolicy`). |
| `TomlFormatException` / `TomlSerializationException` | `Bodu.Text.Toml` | Failures split by cause: malformed input vs. values that cannot be mapped. |
| `Utf8TomlReader` (+ `TomlReaderOptions`) | `Bodu.Text.Toml.Reader` | Forward-only, allocation-free `ref struct` token reader. |
| `Utf8TomlWriter` (+ `TomlWriterOptions`) | `Bodu.Text.Toml.Writer` | Forward-only `ref struct` token writer. |
| `TomlDocument` / `TomlElement` / `TomlProperty` | `Bodu.Text.Toml.Document` | Read-only, low-allocation document object model. |
| `TomlNode` / `TomlObject` / `TomlArray` / `TomlValue` | `Bodu.Text.Toml.Nodes` | Mutable document object model: parse, edit, write back. |
| `TomlConverter<T>` / `TomlConverterFactory`, `TomlStringEnumConverter` (+ generic) / `TomlNumberEnumConverter<TEnum>` | `Bodu.Text.Toml.Serialization` | Custom converters, converter factories, and the public enum converters; the per-member attribute family is the shared `Bodu.Text.Serialization` set (`[PropertyName]` / `[Ignore]` / `[Converter]` / …). |

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

- **Value mapping:** `string`/`char`/`Guid`/`Uri` → string, the integer family → integer, `double`/`float` → float, `bool` → boolean, and `DateTimeOffset`/`DateTime`/`DateOnly`/`TimeOnly` → the four RFC 3339 date-time forms; `byte[]` → an integer array (or a Base64 string via `TomlSerializerOptions.ByteArrayHandling`); enums → member-name strings. `decimal` maps to a float or, for full precision, a string (`TomlSerializerOptions.DecimalHandling`), and `TimeSpan` to its invariant `"c"`-format string.
- A TOML document root must map to a **table**, so a top-level scalar or array throws. Output is normalized TOML in document order - deterministic for a given input, with member order preserved (so `[PropertyOrder]` is honored). It is not a canonical form: two equal documents built in different member orders serialize differently, so the output is unsuitable for hashing or signing.
- The full serializer feature surface is present: converters and factories, the shared attribute family (`PropertyName`/`Ignore`/`Converter`/`PropertyOrder`/`Constructor`/`Required`/`Include`/`ExtensionData`/`NamingPolicy`/`UnmappedMemberHandling`/`ObjectCreationHandling`/`StringEnumMemberName`), the serialization callbacks (`IOnSerializing` / `IOnSerialized` / `IOnDeserializing` / `IOnDeserialized`), naming policies and `TomlSerializerDefaults.Web`, and the string/number enum converters.
- Because the reader/writer/document types live in sub-namespaces, code that uses them alongside the serializer imports `using Bodu.Text.Toml.Reader;` / `.Writer;` / `.Document;` / `.Nodes;` as needed.
- Failures surface through `TomlFormatException` (malformed input, with line/column/offset) and `TomlSerializationException` (binding failures).

## Runnable samples

The repository ships offline, `dotnet run`-able sample projects for this package - the
`TomlSerializer` POCO surface (temporal kinds, naming policies and attributes, the wire
knobs) and the layers beneath it (both DOMs, the token reader/writer, streaming reads) -
under [`samples/Text.Toml/`](https://github.com/bslater/bodu/tree/master/samples/Text.Toml).

## Building and testing

To build this package from source and run its tests, see [CONTRIBUTING.md](https://github.com/bslater/bodu/blob/master/CONTRIBUTING.md).

## License

MIT. © Bodu Pty. Ltd.
