# Bodu.Text.Serialization.Toml

A `System.Text.Json`-style POCO serializer for TOML (v1.0.0 / v1.1.0) on .NET 8, built on `Bodu.Text.Serialization`. Map your configuration types to and from TOML with converters, attributes, and naming policies.

## Installation

```shell
dotnet add package Bodu.Text.Serialization.Toml
```

Targets `net8.0`.

## API shape

```csharp
using Bodu.Text.Serialization.Toml;

string text = TomlSerializer.Serialize(new ServerConfig { Host = "localhost", Port = 8080 });
ServerConfig config = TomlSerializer.Deserialize<ServerConfig>(text);
```

- `Serialize<T>` returns a `string`, or writes to a `TextWriter` or `Stream` (UTF-8, async).
- `Deserialize<T>` reads a `string`, a `ReadOnlySpan<char>`, or a `Stream` (UTF-8, async).
- `TomlSerializerOptions.SpecVersion` opts in to the TOML v1.1.0 grammar.
- The low-level `TomlSyntaxTree.Parse` returns a concrete syntax tree whose `ToFullString()` reproduces the source exactly.

## Type mapping

| .NET | TOML |
|---|---|
| `string` | string |
| integer types | integer |
| `double` / `float` | float (incl. `inf` / `nan`) |
| `bool` | boolean |
| `DateTimeOffset` | offset date-time |
| `DateTime` (Unspecified) | local date-time |
| `DateOnly` / `TimeOnly` | local date / local time |
| `enum` | string (member name) |
| `byte[]` | base64 string |
| arrays, lists | array |
| objects, string-keyed dictionaries | table |

The document root must map to a table. TOML has no null, so a null member is omitted by default and a null array element is rejected. `decimal` is rejected unless a converter (`FormatConverter<decimal>`) maps it.

## Testing

```bash
dotnet test Bodu.Text.Serialization.Toml/test/Bodu.Text.Serialization.Toml.Test.csproj --settings regression.runsettings
```

## License

MIT. © Bodu Pty. Ltd.
