# Bodu.Text.Toml

A TOML (v1.0.0 / v1.1.0) serializer for .NET 8, shaped after `System.Text.Json`. It maps plain CLR objects to and from TOML through a configurable converter model, over a low-level, forward-only token reader and writer.

## Installation

```shell
dotnet add package Bodu.Text.Toml
```

Targets `net8.0`.

## API shape

The public surface mirrors `System.Text.Json`, so the patterns are familiar:

| System.Text.Json | Bodu.Text.Toml | Namespace |
|---|---|---|
| `JsonSerializer` | `TomlSerializer` | `Bodu.Text.Toml` |
| `JsonSerializerOptions` | `TomlSerializerOptions` | `Bodu.Text.Toml` |
| `Utf8JsonReader` / `Utf8JsonWriter` | `Utf8TomlReader` / `Utf8TomlWriter` | `Bodu.Text.Toml` |
| `JsonTokenType` | `TomlTokenType` | `Bodu.Text.Toml` |
| `JsonValueKind` | `TomlValueKind` | `Bodu.Text.Toml` |
| `JsonNamingPolicy` | `TomlNamingPolicy` | `Bodu.Text.Toml` |
| `JsonConverter<T>` / `JsonConverterFactory` | `TomlConverter<T>` / `TomlConverterFactory` | `Bodu.Text.Toml.Serialization` |
| `[JsonPropertyName]` / `[JsonIgnore]` | `[TomlPropertyName]` / `[TomlIgnore]` | `Bodu.Text.Toml.Serialization` |

```csharp
using Bodu.Text.Toml;

string toml = TomlSerializer.Serialize(new ServerConfig { Host = "localhost", Port = 8080 });
ServerConfig config = TomlSerializer.Deserialize<ServerConfig>(toml);
```

- Output is always canonical TOML.
- Failures surface through `TomlFormatException` (malformed input) and `TomlSerializationException` (binding failures).

## Testing

Tests live in `test/` as MSTest classes mirroring `src/`. Run tiers via the runsettings files at the solution root:

```bash
dotnet test Bodu.Text.Toml/test/Bodu.Text.Toml.Test.csproj --settings bvt.runsettings
dotnet test Bodu.Text.Toml/test/Bodu.Text.Toml.Test.csproj --settings regression.runsettings
```

## License

MIT. © Bodu Pty. Ltd.
