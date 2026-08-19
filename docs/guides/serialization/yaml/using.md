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
| integer family (`byte` … `long`, `nint`/`nuint`; `ulong` above `long.MaxValue` as a quoted string) | integer scalar |
| `double` / `float` | float scalar (`.nan` / `.inf` / `-.inf` for the special values) |
| `decimal` | quoted string of the exact text |
| `DateTime` / `DateTimeOffset` / `TimeSpan` | string scalar (ISO-8601 round-trip for the date kinds) |
| `bool` | Boolean scalar (`true` / `false`) |
| `null` / `Nullable<T>` when null | the null scalar |
| `enum` | string (member name), or integer when `WriteEnumsAsStrings = false` |
| arrays, lists, sets, collections | sequence |
| objects, dictionaries | mapping, in insertion order |
| `object` members | runtime type on write; a loosely-typed graph (`Dictionary<string, object?>` / `List<object?>` / scalars) on read |

Public fields join in when `IncludeFields` is set on the options. The full per-type catalog — including the radix forms (`0x`, `0o`) accepted on read and the quoting rules — is in the [built-in converter catalog](builtin-converters.md). A nested object becomes an indented block mapping and a collection of objects becomes a block sequence:

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
    PropertyNamingPolicy = NamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = IgnoreCondition.WhenWritingNull,
    WriteEnumsAsStrings = true,
    PropertyNameCaseInsensitive = true,
};

string yaml = YamlSerializer.Serialize(app, options);
```

Start from a scenario preset by constructing the options from <xref:Bodu.Text.Yaml.YamlSerializerDefaults> (for example `YamlSerializerDefaults.Web`, which selects camel-case naming and case-insensitive matching). Member shaping — naming policies, `[PropertyName]`, `[Ignore]`, the wider attribute family, and the options flags — is covered in [Mapping attributes](attributes.md).

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

`YamlValue.Create` has overloads for `string`, `long`, `double`, and `bool` — those four kinds are the entire scalar model, so build an `int` as `YamlValue.Create((long)42)` and a `float` as a `double`. `GetValue<T>()` reads a value back out, returning a stored value of the matching CLR type directly and otherwise coercing through `Convert.ChangeType` with `CultureInfo.InvariantCulture` (so `GetValue<string>()` always succeeds, and `GetValue<int>()` narrows a stored `long`); an impossible conversion raises `InvalidOperationException`. <xref:Bodu.Text.Yaml.Nodes.YamlValue.ValueKind> reports which of the four kinds a scalar holds.

The `YamlNode` tree is fully mutable and re-entrant: `AsObject()` / `AsArray()` / `AsValue()` cast a node to its concrete shape (throwing `InvalidOperationException` on a mismatch). <xref:Bodu.Text.Yaml.Nodes.YamlObject> preserves insertion order and exposes `Count`, `Keys`, `Add` (which throws on a duplicate key, unlike the adding `[string]` setter), `ContainsKey`, `Remove`, and `TryGetValue`; <xref:Bodu.Text.Yaml.Nodes.YamlArray> implements `IList<YamlNode?>` (`Add`, `Insert`, `RemoveAt`, `IndexOf`, the `[int]` indexer). A node may appear at most once in a tree. `WriteTo(ref Utf8YamlWriter)` drives the writer directly when you need control over indentation or the newline sequence; `ToYamlString()` is the convenience wrapper over it.

## Pattern 5 — Inspect a document with the read-only DOM

The read-only counterpart is a low-allocation view walked through `RootElement`. `YamlDocument` is `IDisposable`, so dispose a document you parse:

```csharp
using Bodu.Text.Yaml.Document;

using YamlDocument doc = YamlDocument.Parse(source);
YamlElement port = doc.RootElement.GetProperty("server").GetProperty("port");
// port.GetInt64() → 8080
```

Typed access goes through `GetString` / `GetInt64` / `GetDouble` / `GetBoolean`, each of which throws `InvalidOperationException` on a kind mismatch — gate them on `ValueKind` first. `GetProperty(name)` throws `KeyNotFoundException` when the key is absent; `TryGetProperty(name, out value)` is the non-throwing form. The `[int]` indexer and `GetSequenceLength` address a sequence, and `EnumerateMapping` / `EnumerateSequence` walk a container. `GetInt64` reads only an integer scalar, while `GetDouble` accepts an integer *or* float scalar (so a whole number bound to a `double` member needs no float marker). `ValueKind` reports the <xref:Bodu.Text.Yaml.YamlValueKind>; `ScalarStyle` records the original <xref:Bodu.Text.Yaml.YamlScalarStyle> of a parsed scalar, or `Any` for a non-scalar node.

> [!IMPORTANT]
> A <xref:Bodu.Text.Yaml.Document.YamlDocument> owns its parsed buffer and every <xref:Bodu.Text.Yaml.Document.YamlElement> is a struct view back onto it. Reading an element after the document is disposed raises `ObjectDisposedException`, so do not let an element (or a value pulled from one lazily) outlive the `using` block.

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

`Expand` (default) merges the referenced mapping in place, with **keys already present in the target taking precedence** over the merged ones — so `production`'s explicit `timeout: 60` overrides the merged `timeout: 30`. `Disabled` and `PreserveAsNormalKey` both retain `<<` as an ordinary mapping key without expanding it; the two are equivalent in the produced tree and differ only in expressed intent. Merge keys are gated on `SpecVersion = V1_1`: they are not part of the 1.2 core schema, so under the default `V1_2` a `<<` key is an ordinary key regardless of `MergeKeyBehavior`.

Anchors and aliases work under either spec version — they are structural, not implicit-typing, features. The reader composes the tree before any token surfaces, so an alias presents the same resolved value as its anchor; a cyclic anchor or an alias that names no anchor is rejected as a <xref:Bodu.Text.Yaml.YamlFormatException>. Duplicate mapping keys are governed separately by <xref:Bodu.Text.Yaml.YamlDuplicateKeyBehavior> (`Throw` by default — the only specification-conformant mode — or the lenient `UseFirst` / `UseLast`). The duplicate-key and merge-key policies apply equally to the serializer, both DOMs, and the raw reader, because all four parse through the same <xref:Bodu.Text.Yaml.Reader.YamlReaderOptions>.

## Pattern 9 — Drive the low-level reader and writer

Below the serializer and the DOMs sit the `ref struct` token machines. <xref:Bodu.Text.Yaml.Reader.Utf8YamlReader> walks a parsed document as a token stream, and <xref:Bodu.Text.Yaml.Writer.Utf8YamlWriter> emits one. Because YAML's anchors, aliases, merge keys, and indentation require a composed tree, the reader is **buffered**: its constructor copies the UTF-8 source and parses it fully, then `Read()` walks the in-memory store. This is unlike `Utf8JsonReader`'s single-pass scan, so the reader is not a streaming reader over a growing buffer — feed it the whole document.

```csharp
using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Reader;

var reader = new Utf8YamlReader(Encoding.UTF8.GetBytes("host: localhost\nport: 8080\n"));
while (reader.Read())
{
    switch (reader.TokenType)
    {
        case YamlTokenType.PropertyName:
            string key = reader.GetString();          // "host", then "port"
            break;
        case YamlTokenType.String:
            string s = reader.GetString();            // "localhost"
            break;
        case YamlTokenType.Integer:
            long n = reader.GetInt64();               // 8080
            break;
    }
}
```

`TokenType` reports the current <xref:Bodu.Text.Yaml.YamlTokenType> (the `StartMapping` / `EndMapping` / `StartSequence` / `EndSequence` brackets, `PropertyName`, and the `Null` / `String` / `Integer` / `Float` / `Boolean` scalar tokens), `CurrentDepth` the nesting level, and `ValueTextEquals(ReadOnlySpan<byte>)` compares the current key against UTF-8 text without allocating. The scalar getters throw `InvalidOperationException` if the current token is the wrong kind. <xref:Bodu.Text.Yaml.Reader.YamlReaderOptions> carries the same `SpecVersion`, `DuplicateKeyBehavior`, `MergeKeyBehavior`, and `MaxDepth` the document layer uses.

The writer's surface is `WriteStartMapping` / `WriteEndMapping`, `WriteStartSequence` / `WriteEndSequence`, `WritePropertyName`, and the scalar writers `WriteString` / `WriteInt64` / `WriteDouble` / `WriteBoolean` / `WriteNull`. It always emits **block** collections, quoting a string scalar only when a plain rendering would be ambiguous (it would resolve as a non-string, begin with an indicator character, contain `": "`, and so on) and falling back to a double-quoted, escaped form otherwise. Empty mappings and sequences are written as flow `{}` / `[]` so they round-trip as empty rather than null. <xref:Bodu.Text.Yaml.Writer.YamlWriterOptions> sets `IndentSize` (default 2, capped at 16), `NewLine` (only `"\n"` or `"\r\n"`), and `MaxDepth`:

```csharp
using Bodu.Text.Yaml.Writer;
using System.Buffers;

var buffer = new ArrayBufferWriter<byte>();
var writer = new Utf8YamlWriter(buffer, new YamlWriterOptions { IndentSize = 4 });
writer.WriteStartMapping();
writer.WritePropertyName("host");
writer.WriteString("localhost");
writer.WriteEndMapping();
string yaml = Encoding.UTF8.GetString(buffer.WrittenSpan);
```

Both types are `ref struct`s — they cannot be boxed, stored on the heap, or captured by a lambda, and the writer enforces a well-formed call sequence (a value without a pending key, a mismatched end, or a second root all throw `InvalidOperationException`).

## Error handling

Two exception types separate "the text is not YAML" from "the YAML does not fit your type":

- <xref:Bodu.Text.Yaml.YamlFormatException> — malformed input. A subtype of `FormatException`. Because YAML is edited by hand, the exception carries `LineNumber`, `ColumnNumber`, and byte `Offset` (each nullable when no position applies; the offset and column count UTF-8 bytes). It is raised for inconsistent indentation, a tab used as indentation, an unterminated quoted scalar, an invalid escape, a cyclic or dangling alias, a duplicate mapping key under `Throw`, and nesting beyond `MaxDepth`.
- <xref:Bodu.Text.Yaml.YamlSerializationException> — the document parsed, but a value cannot bind: a kind mismatch, an out-of-range or non-integral number under `Strict`, a multi-character `char`, a duplicate dictionary key on write, or an unmapped member when `UnmappedMemberHandling` is `Disallow`. It carries `Offset`, `LineNumber`, `ColumnNumber`, and a dotted member `Path` such as `server.endpoints[0].timeout` that pinpoints the offending member, index, or key.

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

- [Mapping attributes](attributes.md) — declarative shaping with `[PropertyName]`, `[Ignore]`, the wider attribute family, and the naming policies.
- [Writing converters](converters.md) and the [built-in converter catalog](builtin-converters.md) — custom and provisioned type handling.
- [Bodu.Text.Yaml introduction](../../../docs/serialization/yaml/index.md) and [core concepts](../../../docs/serialization/yaml/concepts.md) — the format specifics and family vocabulary.
- [Bodu serializer guides](../index.md) and the [Text & Serialization guides](../../topics/text-and-serialization.md).
- API reference — <xref:Bodu.Text.Yaml.YamlSerializer>, <xref:Bodu.Text.Yaml.YamlSerializerOptions>, <xref:Bodu.Text.Yaml.Nodes.YamlNode>, <xref:Bodu.Text.Yaml.Document.YamlDocument>.
