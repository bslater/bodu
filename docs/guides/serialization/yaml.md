---
title: Using YAML
---

# Using YAML

<xref:Bodu.Text.Yaml.YamlSerializer> maps your types to and from [YAML](https://yaml.org/) (1.1 / 1.2). Behavior is configured through <xref:Bodu.Text.Yaml.YamlSerializerOptions>; when you do not want a POCO, the same documents are served by the mutable <xref:Bodu.Text.Yaml.Nodes.YamlNode> DOM and the read-only <xref:Bodu.Text.Yaml.Document.YamlDocument> DOM. Any node may sit at the document root.

## Pattern 1 — Round-trip a configuration type

```csharp
using Bodu.Text.Yaml;

string text = YamlSerializer.Serialize(config);
ServerConfig back = YamlSerializer.Deserialize<ServerConfig>(text);
```

`Serialize` returns a block-style YAML `string`; `Deserialize<T>` reads a `string` or a `ReadOnlySpan<byte>` (UTF-8). Output is emitted in member order.

## Pattern 2 — Know the type mapping

| .NET | YAML |
|---|---|
| `string` / `char` / `Guid` | string |
| `TimeSpan` | string |
| integer types | integer |
| `double` / `float` / `decimal` | float (incl. `.inf` / `.nan`) |
| `bool` | boolean (`true` / `false`) |
| `DateTime` / `DateTimeOffset` | string (round-trip `"o"` format) |
| `enum` | string member name, or its number when `WriteEnumsAsStrings` is `false` |
| `null` | the null scalar (omitted when `IgnoreNullValues` is set) |
| arrays, lists, collection interfaces | sequence |
| objects, string-keyed dictionaries | mapping |
| `object` members | runtime type on write, a dictionary / list / scalar graph on read |

A string that *looks* like another type — `"123"`, `"true"`, `"no"` — is emitted quoted so it round-trips as a string. Dictionary keys are written as mapping keys; on read, keys convert to the dictionary's key type (string, enum, or a convertible primitive).

## Pattern 3 — Worked example: nested mappings and sequences

A nested object becomes an indented mapping; a collection of objects becomes a block sequence of mappings:

```csharp
using Bodu.Text.Yaml;

public sealed class AppConfig
{
    public string? Title { get; set; }
    public ServerConfig? Server { get; set; }
    public List<string>? Tags { get; set; }
}

public sealed class ServerConfig
{
    public string? Host { get; set; }
    public int Port { get; set; }
}

var config = new AppConfig
{
    Title = "demo",
    Server = new ServerConfig { Host = "localhost", Port = 8080 },
    Tags = ["web", "internal"],
};

string text = YamlSerializer.Serialize(config);
```

The emitted document is the YAML a person would write:

```yaml
Title: demo
Server:
  Host: localhost
  Port: 8080
Tags:
  - web
  - internal
```

Deserializing the same text restores the full graph:

```csharp
AppConfig back = YamlSerializer.Deserialize<AppConfig>(text);
// back.Server.Port → 8080
```

To emit lowercase keys (`title`, `server`, …) apply a naming policy ([Pattern 4](#pattern-4--rename-members)).

## Pattern 4 — Rename members

```csharp
var options = new YamlSerializerOptions
{
    PropertyNamingPolicy = YamlNamingPolicy.SnakeCaseLower,
};
```

Naming policies cover `CamelCase`, `SnakeCaseLower`, and `KebabCaseLower`. Pin a single member's name with `[YamlPropertyName("…")]`, which always wins over the policy, and exclude a member with `[YamlIgnore]`. Public fields join the mapping when `IncludeFields` is set on the options. Set `PropertyNameCaseInsensitive` to bind keys regardless of case on the read path.

## Pattern 5 — Select the spec version (implicit typing)

```csharp
var options = new YamlSerializerOptions { SpecVersion = YamlSpecVersion.V1_1 };
var doc = YamlSerializer.Deserialize<MyDoc>(text, options);
```

The default is the **1.2 core schema**: only `true` / `false` are Booleans, so unquoted `no` / `yes` / `on` / `off` stay strings (no "Norway problem"). Opting in to **1.1** additionally reads those Boolean spellings and the binary / underscored / sexagesimal number forms. Quoting always forces a string under either version.

## Pattern 6 — Anchors, aliases, and merge keys

On the read path the document models resolve YAML's node-sharing features. An alias (`*name`) resolves to its anchor's (`&name`) value, and a merge key imports a mapping's entries:

```csharp
using Bodu.Text.Yaml.Document;

string yaml = """
defaults: &defaults
  retries: 3
  timeout: 30
service:
  <<: *defaults
  timeout: 60
""";

using YamlDocument doc = YamlDocument.Parse(yaml);
YamlElement service = doc.RootElement.GetProperty("service");
// service.GetProperty("retries").GetInt64() → 3   (merged from defaults)
// service.GetProperty("timeout").GetInt64() → 60  (explicit key wins)
```

A merge value may also be a sequence of mappings (`<<: [*a, *b]`), in which case earlier sources take precedence; an alias that references no anchor raises <xref:Bodu.Text.Yaml.YamlFormatException>.

## Pattern 7 — Read a multi-document stream

A YAML stream may hold several documents separated by `---`. Read them all with <xref:Bodu.Text.Yaml.Document.YamlDocument.ParseAllDocuments*>:

```csharp
using Bodu.Text.Yaml.Document;

IReadOnlyList<YamlDocument> docs = YamlDocument.ParseAllDocuments("""
---
name: first
---
name: second
...
""");
// docs.Count → 2 ; docs[1].RootElement.GetProperty("name").GetString() → "second"
```

## Pattern 8 — Edit a document with the mutable DOM

When you do not want a POCO, parse to <xref:Bodu.Text.Yaml.Nodes.YamlNode> — index into the tree, mutate values, and write the document back as text:

```csharp
using Bodu.Text.Yaml.Nodes;

YamlNode node = YamlNode.Parse(yaml)!;
node["Server"]!["Port"] = YamlValue.Create(9090L);

string back = node.ToYamlString();
```

`YamlObject` and `YamlArray` are fully mutable (indexer, `Add`, `Remove`); scalars are created with `YamlValue.Create(…)`.

## Pattern 9 — Inspect a document with the read-only DOM

The read-only counterpart is a low-allocation view walked through `RootElement`:

```csharp
using Bodu.Text.Yaml.Document;

using YamlDocument doc = YamlDocument.Parse(yaml);
YamlElement port = doc.RootElement.GetProperty("Server").GetProperty("Port");
// port.GetInt64() → 8080
```

`YamlDocument.Parse` accepts a `string` as well as UTF-8 bytes. A document you parse is caller-owned — dispose it (the `using` above) when finished. Typed access goes through `GetString` / `GetInt64` / `GetDouble` / `GetBoolean` on <xref:Bodu.Text.Yaml.Document.YamlElement>, with `EnumerateMapping` / `EnumerateSequence` for traversal.

## Pattern 10 — Process tokens by hand

For full control with no intermediate model, drive the <xref:Bodu.Text.Yaml.Reader.Utf8YamlReader> / <xref:Bodu.Text.Yaml.Writer.Utf8YamlWriter> ref-struct pair directly. The reader presents the document as a forward-only token stream (`StartMapping` / `PropertyName` / scalar / `EndMapping`, …); the writer emits block-style YAML from the matching calls. This is the same surface a [converter](converters.md) builds on.

## Pattern 11 — Customize a type with a converter

Register a <xref:Bodu.Text.Yaml.Serialization.YamlConverter`1> to control how one type is read and written:

```csharp
using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Serialization;

var options = new YamlSerializerOptions();
options.Converters.Add(new MyConverter());
```

A converter's `Read` receives a <xref:Bodu.Text.Yaml.Document.YamlElement> and its `Write` receives the `Utf8YamlWriter`.

## Error handling

Two exception types separate "the text is not YAML" from "the YAML does not fit your type":

- <xref:Bodu.Text.Yaml.YamlFormatException> — malformed input. Because YAML is indentation-sensitive and edited by hand, the exception carries the position: `LineNumber`, `ColumnNumber`, and byte `Offset`.
- <xref:Bodu.Text.Yaml.YamlSerializationException> — the document parsed, but a value cannot bind: a kind mismatch or a value the target type cannot represent.

```csharp
try
{
    AppConfig config = YamlSerializer.Deserialize<AppConfig>(text);
}
catch (YamlFormatException ex)
{
    log.Warn($"Parse error at {ex.LineNumber}:{ex.ColumnNumber}: {ex.Message}");
}
catch (YamlSerializationException ex)
{
    log.Warn($"Document does not bind: {ex.Message}");
}
```

## See also

- [Using TOML](toml.md) and [Using Bencode](bencode.md) — the sibling serializers; the round-trip, DOM, and converter patterns transfer with the prefix swap.
- [Bodu.Text.Yaml introduction](../../docs/serialization/yaml.md) — what is specific to the YAML format, including implicit typing and native features.
- [Bodu serializers introduction](../../docs/serialization/index.md) and [core concepts](../../docs/serialization/concepts.md) — the family shape and vocabulary.
- API reference — <xref:Bodu.Text.Yaml.YamlSerializer>, <xref:Bodu.Text.Yaml.YamlSerializerOptions>, <xref:Bodu.Text.Yaml.Nodes.YamlNode>, <xref:Bodu.Text.Yaml.Document.YamlDocument>.
