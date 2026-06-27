---
title: Using YAML
---

# Using YAML

<xref:Bodu.Text.Yaml.YamlSerializer> maps your types to and from [YAML](https://yaml.org/). Behavior is configured through <xref:Bodu.Text.Yaml.YamlSerializerOptions>; when you do not want a POCO, the same documents are served by the mutable <xref:Bodu.Text.Yaml.Nodes.YamlNode> DOM and the read-only <xref:Bodu.Text.Yaml.Document.YamlDocument> DOM. The surface is string text and UTF-8 bytes — there are no `Stream` overloads and no async API.

## Pattern 1 — Round-trip an object

```csharp
using Bodu.Text.Yaml;

public sealed class ServerConfig
{
    public string Host { get; set; } = "";
    public int Port { get; set; }
}

var config = new ServerConfig { Host = "localhost", Port = 8080 };

string yaml = YamlSerializer.Serialize(config);
ServerConfig back = YamlSerializer.Deserialize<ServerConfig>(yaml)!;
```

`Serialize` returns a `string`; an `object` + `Type` overload covers runtime-typed values. `Deserialize<T>` reads a `string` or a `ReadOnlySpan<byte>` (UTF-8) and returns `T?`, so use `!` when the document is known to be non-null:

```csharp
ReadOnlySpan<byte> utf8 = Encoding.UTF8.GetBytes(yaml);
ServerConfig fromBytes = YamlSerializer.Deserialize<ServerConfig>(utf8)!;
```

## Pattern 2 — Know the type mapping

| .NET | YAML |
|---|---|
| `string` / `char` / `Guid` / `Uri` | string scalar |
| integer family | integer scalar |
| `double` / `float` | float scalar |
| `bool` | Boolean scalar (`true` / `false`) |
| `null` | the null scalar |
| `enum` | string (member name), or integer when `WriteEnumsAsStrings = false` |
| arrays, lists, sets, collections | sequence |
| objects, dictionaries | mapping, in insertion order |
| `object` members | runtime type on write, <xref:Bodu.Text.Yaml.Document.YamlElement> on read |

Public fields join in when `IncludeFields` is set on the options. The full per-type catalog is in the [built-in converter catalog](builtin-converters.md). A nested object becomes an indented block mapping and a collection of objects becomes a block sequence:

```csharp
public sealed class AppConfig
{
    public string? Title { get; set; }
    public ServerConfig? Server { get; set; }
    public List<string>? Tags { get; set; }
}

var app = new AppConfig
{
    Title = "demo",
    Server = new ServerConfig { Host = "localhost", Port = 8080 },
    Tags = ["web", "api"],
};

string yaml = YamlSerializer.Serialize(app);
```

```yaml
Title: demo
Server:
  Host: localhost
  Port: 8080
Tags:
  - web
  - api
```

## Pattern 3 — Configure via YamlSerializerOptions

One options object holds every setting. Configure it once and reuse it — it freezes on first use and then caches its resolved converters and type metadata:

```csharp
var options = new YamlSerializerOptions
{
    PropertyNamingPolicy = YamlNamingPolicy.SnakeCaseLower,
    IgnoreNullValues = true,
    WriteEnumsAsStrings = true,
    PropertyNameCaseInsensitive = true,
};

string yaml = YamlSerializer.Serialize(app, options);
```

Member shaping — naming policies, `[YamlPropertyName]`, `[YamlIgnore]`, and the options flags — is covered in [Mapping attributes](attributes.md).

## Pattern 4 — Edit a document with the mutable DOM

Parse to <xref:Bodu.Text.Yaml.Nodes.YamlNode>, index into the tree, build scalars with `YamlValue.Create(…)`, and write it back with `ToYamlString()`:

```csharp
using Bodu.Text.Yaml.Nodes;

string source = """
    server:
      host: localhost
      port: 8080
    """;

YamlNode root = YamlNode.Parse(source)!;
root["server"]!["port"] = YamlValue.Create(9090);

string updated = root.ToYamlString();
```

`YamlValue.Create` has overloads for `string`, `long`, `double`, and `bool`; read a value back with `GetValue<T>()`. `YamlObject` exposes `Count`, `Keys`, `Add`, `ContainsKey`, `Remove`, and `TryGetValue`; `YamlArray` implements `IList<YamlNode?>`. `WriteTo(ref Utf8YamlWriter)` drives the writer directly when you need control over indentation.

## Pattern 5 — Inspect a document with the read-only DOM

The read-only counterpart is a low-allocation view walked through `RootElement`. `YamlDocument` is `IDisposable`, so dispose a document you parse:

```csharp
using Bodu.Text.Yaml.Document;

using YamlDocument doc = YamlDocument.Parse(source);
YamlElement port = doc.RootElement.GetProperty("server").GetProperty("port");
// port.GetInt64() → 8080
```

Typed access goes through `GetString` / `GetInt64` / `GetDouble` / `GetBoolean`, with `TryGetProperty`, the `[int]` indexer, `GetSequenceLength`, and the `EnumerateMapping` / `EnumerateSequence` enumerators. `ValueKind` reports the <xref:Bodu.Text.Yaml.YamlValueKind>; `ScalarStyle` records the original <xref:Bodu.Text.Yaml.YamlScalarStyle> of a parsed scalar.

```csharp
foreach (YamlProperty property in doc.RootElement.GetProperty("server").EnumerateMapping())
{
    // property.Name → "host", "port"; property.Value is a YamlElement
}
```

## Pattern 6 — Multi-document streams

A YAML stream can hold several documents separated by `---` (and optionally terminated by `...`). <xref:Bodu.Text.Yaml.Document.YamlDocument.ParseAllDocuments*> returns every document; the single-document `Parse` and `Deserialize<T>` read the first only:

```csharp
using Bodu.Text.Yaml.Document;

string stream = """
    name: first
    ---
    name: second
    """;

IReadOnlyList<YamlDocument> documents = YamlDocument.ParseAllDocuments(stream);
foreach (YamlDocument document in documents)
{
    using (document)
    {
        string name = document.RootElement.GetProperty("name").GetString();
        // "first", then "second"
    }
}
```

Each returned document is caller-owned — dispose each when finished.

## Pattern 7 — Select the spec version

```csharp
var options = new YamlSerializerOptions { SpecVersion = YamlSpecVersion.V1_1 };
var config = YamlSerializer.Deserialize<ServerConfig>(text, options);
```

The default is the strict **1.2 core schema**, where only `true` / `false` are Booleans. Opting in to **1.1** additionally accepts the `yes` / `no` / `on` / `off` Boolean spellings and sexagesimal numbers, and enables merge keys (Pattern 8). A `%YAML` directive in the document overrides the typing per document.

## Pattern 8 — Anchors, aliases, and merge keys on the read path

Anchors (`&a`) and aliases (`*a`) are resolved transparently by the reader — an alias produces the same value as the anchor it names (anchors must be unique and acyclic). Merge keys (`<<`) are a YAML 1.1 feature governed by <xref:Bodu.Text.Yaml.YamlMergeKeyBehavior>:

```csharp
string text = """
    defaults: &defaults
      retries: 3
      timeout: 30
    production:
      <<: *defaults
      timeout: 60
    """;

var options = new YamlSerializerOptions
{
    SpecVersion = YamlSpecVersion.V1_1,
    MergeKeyBehavior = YamlMergeKeyBehavior.Expand,   // the default
};

using YamlDocument doc = YamlDocument.Parse(Encoding.UTF8.GetBytes(text));
// With Expand, production resolves to { retries: 3, timeout: 60 }.
```

`Expand` (default) merges the referenced mapping in place; `Disabled` treats `<<` as an ordinary key; `PreserveAsNormalKey` keeps `<<` literally without merging. Duplicate mapping keys are governed separately by <xref:Bodu.Text.Yaml.YamlDuplicateKeyBehavior> (`Throw` by default, or `UseFirst` / `UseLast`).

## Error handling

Two exception types separate "the text is not YAML" from "the YAML does not fit your type":

- <xref:Bodu.Text.Yaml.YamlFormatException> — malformed input. Because YAML is edited by hand, the exception carries `LineNumber`, `ColumnNumber`, and byte `Offset`. Tabs used as indentation are rejected here.
- <xref:Bodu.Text.Yaml.YamlSerializationException> — the document parsed, but a value cannot bind: a kind mismatch, a value the format cannot represent on write, or an unmapped member when `UnmappedMemberHandling` is `Disallow`. It carries `Offset`, `LineNumber`, `ColumnNumber`, and a dotted member `Path`.

```csharp
try
{
    AppConfig config = YamlSerializer.Deserialize<AppConfig>(text)!;
}
catch (YamlFormatException ex)
{
    // Not valid YAML, e.g. a tab used as indentation.
    log.Warn($"Parse error at {ex.LineNumber}:{ex.ColumnNumber}: {ex.Message}");
}
catch (YamlSerializationException ex)
{
    // Valid YAML that does not match the model.
    log.Warn($"Document does not bind at '{ex.Path}': {ex.Message}");
}
```

`TryParse`-style members do not exist on the serializer; wrap `Deserialize` as above when reading untrusted input.

## Where to go next

- [Mapping attributes](attributes.md) — declarative shaping with `[YamlPropertyName]`, `[YamlIgnore]`, and the naming policies.
- [Writing converters](converters.md) and the [built-in converter catalog](builtin-converters.md) — custom and provisioned type handling.
- [Bodu.Text.Yaml introduction](../../../docs/serialization/yaml/index.md) and [core concepts](../../../docs/serialization/yaml/concepts.md) — the format specifics and family vocabulary.
- [Bodu serializer guides](../index.md) and the [Text & Serialization guides](../../topics/text-and-serialization.md).
- API reference — <xref:Bodu.Text.Yaml.YamlSerializer>, <xref:Bodu.Text.Yaml.YamlSerializerOptions>, <xref:Bodu.Text.Yaml.Nodes.YamlNode>, <xref:Bodu.Text.Yaml.Document.YamlDocument>.
