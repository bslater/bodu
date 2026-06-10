---
title: Using TOML
---

# Using TOML

<xref:Bodu.Text.Toml.TomlSerializer> maps your types to and from [TOML](https://toml.io/) (v1.0.0 / v1.1.0), shaped after `System.Text.Json`. The document root must map to a table, so the type you serialize at the root maps to an object — a top-level scalar or array throws.

## Pattern 1 — Round-trip a configuration type

```csharp
using Bodu.Text.Toml;

string text = TomlSerializer.Serialize(config);
ServerConfig back = TomlSerializer.Deserialize<ServerConfig>(text);
```

`Serialize` also writes to an `IBufferWriter<byte>` (UTF-8) or a `Stream` (with `SerializeAsync`); `Deserialize` reads a `string`, a `ReadOnlySpan<byte>` (UTF-8), or a `Stream` (with `DeserializeAsync`). Output is canonical TOML in document order, so `[TomlPropertyOrder]` is honored.

## Pattern 2 — Know the type mapping

| .NET | TOML |
|---|---|
| `string` / `char` / `Guid` / `Uri` | string |
| integer types | integer |
| `double` / `float` | float (incl. `inf` / `nan`) |
| `bool` | boolean |
| `DateTimeOffset` | offset date-time |
| `DateTime` (`Unspecified`) | local date-time |
| `DateOnly` / `TimeOnly` | local date / local time |
| `enum` | string (member name) |
| `byte[]` | integer array, or Base64 string via `ByteArrayHandling` |
| arrays, lists | array |
| objects, string-keyed dictionaries | table |

TOML has no null: a null member is omitted by default. `decimal` and `TimeSpan` are rejected unless a [converter](converters.md) maps them.

Choose the `byte[]` form with <xref:Bodu.Text.Toml.TomlByteArrayHandling> on the options:

```csharp
var options = new TomlSerializerOptions { ByteArrayHandling = TomlByteArrayHandling.Base64String };
```

## Pattern 3 — Rename members

```csharp
var options = new TomlSerializerOptions
{
    PropertyNamingPolicy = TomlNamingPolicy.SnakeCaseLower,
};
```

Naming policies cover `CamelCase`, `SnakeCaseLower` / `SnakeCaseUpper`, and `KebabCaseLower` / `KebabCaseUpper`. Pin a single member's name with `[TomlPropertyName("…")]`, which always wins over the policy. Start from a scenario preset by constructing the options from <xref:Bodu.Text.Toml.TomlSerializerDefaults> (for example `TomlSerializerDefaults.Web`).

## Pattern 4 — Select the spec version

```csharp
var options = new TomlSerializerOptions { SpecVersion = TomlSpecVersion.V1_1 };
var doc = TomlSerializer.Deserialize<MyDoc>(text, options);
```

The default is strict **v1.0.0**. Opting in to **v1.1.0** additionally accepts the `\e` and `\xHH` escapes, time values without seconds, and multi-line and trailing-comma inline tables. The writer always emits output valid under both versions.

## Pattern 5 — Use a document model instead of a type

When you do not want a POCO, parse to one of the two DOMs.

Mutable (`JsonNode`-style) — parse, edit, and write back:

```csharp
using Bodu.Text.Toml.Nodes;

TomlNode node = TomlNode.Parse(utf8Toml)!;
node["server"]!["port"] = 9090;
byte[] back = node.ToUtf8Bytes();
```

Read-only (`JsonDocument`-style) — a low-allocation view walked through `RootElement`:

```csharp
using Bodu.Text.Toml.Document;

using TomlDocument doc = TomlDocument.Parse(utf8Toml);
TomlElement port = doc.RootElement.GetProperty("server").GetProperty("port");
```

## Pattern 6 — Process tokens by hand

For full control with no allocations, drive the <xref:Bodu.Text.Toml.Reader.Utf8TomlReader> / <xref:Bodu.Text.Toml.Writer.Utf8TomlWriter> ref-struct pair directly — the `Utf8JsonReader` / `Utf8JsonWriter` analogues. This is the same surface a [converter](converters.md) receives.

Malformed input raises <xref:Bodu.Text.Toml.TomlFormatException> with the line, column, and offset; a value that cannot bind raises <xref:Bodu.Text.Toml.TomlSerializationException>.
