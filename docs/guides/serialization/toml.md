---
title: Using TOML
---

# Using TOML

<xref:Bodu.Text.Serialization.Toml.TomlSerializer> maps your types to and from [TOML](https://toml.io/). The document root must be a table, so the type you serialize at the root maps to an object.

## Pattern 1 — Round-trip a configuration type

```csharp
using Bodu.Text.Serialization.Toml;

string text = TomlSerializer.Serialize(config);
ServerConfig back = TomlSerializer.Deserialize<ServerConfig>(text);
```

`Serialize` also writes to a `TextWriter` or a UTF-8 `Stream` (with `SerializeAsync`); `Deserialize` reads a `string`, a `ReadOnlySpan<char>`, or a UTF-8 `Stream` (with `DeserializeAsync`).

## Pattern 2 — Know the type mapping

| .NET | TOML |
|---|---|
| `string` | string |
| integer types | integer |
| `double` / `float` | float (incl. `inf` / `nan`) |
| `bool` | boolean |
| `DateTimeOffset` | offset date-time |
| `DateTime` (`Unspecified`) | local date-time |
| `DateOnly` / `TimeOnly` | local date / local time |
| `enum` | string (member name) |
| `byte[]` | base64 string |
| arrays, lists | array |
| objects, string-keyed dictionaries | table |

TOML has no null: a null member is omitted by default, and a null array element is rejected. `decimal` is rejected unless a [converter](converters.md) maps it.

## Pattern 3 — Select the spec version

```csharp
var options = new TomlSerializerOptions { SpecVersion = TomlSpecVersion.V1_1 };
var doc = TomlSerializer.Deserialize<MyDoc>(text, options);
```

The default is strict **v1.0.0**. Opting in to **v1.1.0** additionally accepts the `\e` and `\xHH` escapes, time values without seconds, and multi-line and trailing-comma inline tables.

## Pattern 4 — Parse losslessly without binding

```csharp
using Bodu.Text.Serialization.Toml.Syntax;

TomlDocumentSyntax tree = TomlSyntaxTree.Parse(source);
Console.WriteLine(tree.ToFullString() == source); // True — the source is reproduced exactly
```

Malformed input raises <xref:Bodu.Text.Serialization.Toml.TomlFormatException> with the line, column, and offset; a value that cannot bind raises <xref:Bodu.Text.Serialization.FormatSerializationException>.
