# Bodu.Text.Toml

A TOML (v1.0.0 / v1.1.0) library for .NET 8, shaped after `System.Text.Json`. It maps plain CLR objects to and from TOML through a configurable converter model, over a low-level forward-only token reader and writer, with both a mutable and a read-only document object model.

## Installation

```shell
dotnet add package Bodu.Text.Toml
```

Targets `net8.0`.

## API shape

The public surface mirrors `System.Text.Json` (and the sibling `Bodu.Text.Bencode` library), so the patterns are familiar. The types are organised into folders/namespaces that follow the `System.Text.Json` source layout:

| System.Text.Json | Bodu.Text.Toml | Namespace |
|---|---|---|
| `JsonSerializer` / `JsonSerializerOptions` / `JsonSerializerDefaults` | `TomlSerializer` / `TomlSerializerOptions` / `TomlSerializerDefaults` | `Bodu.Text.Toml` |
| `JsonNamingPolicy`, `JsonTokenType`, `JsonValueKind` | `TomlNamingPolicy`, `TomlTokenType`, `TomlValueKind` | `Bodu.Text.Toml` |
| `JsonException` | `TomlFormatException` / `TomlSerializationException` | `Bodu.Text.Toml` |
| `Utf8JsonReader` | `Utf8TomlReader` (+ `TomlReaderOptions`) | `Bodu.Text.Toml.Reader` |
| `Utf8JsonWriter` | `Utf8TomlWriter` (+ `TomlWriterOptions`) | `Bodu.Text.Toml.Writer` |
| `JsonDocument` / `JsonElement` / `JsonProperty` | `TomlDocument` / `TomlElement` / `TomlProperty` | `Bodu.Text.Toml.Document` |
| `JsonNode` / `JsonObject` / `JsonArray` / `JsonValue` | `TomlNode` / `TomlObject` / `TomlArray` / `TomlValue` | `Bodu.Text.Toml.Nodes` |
| `JsonConverter<T>` / `JsonConverterFactory`, `[JsonPropertyName]` / `[JsonIgnore]` / … | `TomlConverter<T>` / `TomlConverterFactory`, `[TomlPropertyName]` / `[TomlIgnore]` / … | `Bodu.Text.Toml.Serialization` |

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
- A TOML document root must map to a **table**, so a top-level scalar or array throws. Output is canonical TOML in document order (member order is preserved, so `[TomlPropertyOrder]` is honored).
- The full System.Text.Json alignment surface is present: converters and factories, the attribute family (`TomlPropertyName`/`Ignore`/`Converter`/`PropertyOrder`/`Constructor`/`Required`/`Include`/`ExtensionData`/`NamingPolicy`/`UnmappedMemberHandling`/`ObjectCreationHandling`/`StringEnumMemberName`), the `ITomlOn*` serialization callbacks, naming policies and `TomlSerializerDefaults.Web`, and the string/number enum converters.
- Because the reader/writer/document types live in sub-namespaces, code that uses them alongside the serializer imports `using Bodu.Text.Toml.Reader;` / `.Writer;` / `.Document;` / `.Nodes;` as needed.
- Failures surface through `TomlFormatException` (malformed input, with line/column/offset) and `TomlSerializationException` (binding failures).

## Testing

Tests live in `test/` as MSTest classes mirroring `src/`. Run tiers via the runsettings files at the solution root:

```bash
dotnet test Bodu.Text.Toml/test/Bodu.Text.Toml.Test.csproj --settings bvt.runsettings
dotnet test Bodu.Text.Toml/test/Bodu.Text.Toml.Test.csproj --settings regression.runsettings
```

## License

MIT. © Bodu Pty. Ltd.
