---
title: Using TOML
---

# Using TOML

<xref:Bodu.Text.Toml.TomlSerializer> maps your types to and from [TOML](https://toml.io/) (v1.0.0 / v1.1.0). Behavior is configured through <xref:Bodu.Text.Toml.TomlSerializerOptions>; when you do not want a POCO, the same documents are served by the mutable <xref:Bodu.Text.Toml.Nodes.TomlNode> DOM and the read-only <xref:Bodu.Text.Toml.Document.TomlDocument> DOM. The document root must map to a table, so the type you serialize at the root maps to an object — a top-level scalar or array throws.

## Pattern 1 — Round-trip a configuration type

```csharp
using Bodu.Text.Toml;

string text = TomlSerializer.Serialize(config);
ServerConfig back = TomlSerializer.Deserialize<ServerConfig>(text);
```

`Serialize` also writes to an `IBufferWriter<byte>` (UTF-8) or a `Stream` (with `SerializeAsync`); `Deserialize` reads a `string`, a `ReadOnlySpan<byte>` (UTF-8), or a `Stream` (with `DeserializeAsync`). Output is canonical TOML in document order, so `[PropertyOrder]` is honored. See [Pattern 8](#pattern-8--streams-and-async) for the stream surface.

## Pattern 2 — Know the type mapping

| .NET | TOML |
|---|---|
| `string` / `char` / `Guid` / `Uri` / `Version` | string |
| `TimeSpan` | string (invariant `"c"` format) |
| integer types (incl. `Int128` / `UInt128` within the i64 range) | integer |
| `double` / `float` / `Half` | float (incl. `inf` / `nan`) |
| `decimal` | float, or lossless string via `DecimalHandling` |
| `bool` | boolean |
| `DateTimeOffset` | offset date-time |
| `DateTime` (`Unspecified`) | local date-time |
| `DateOnly` / `TimeOnly` | local date / local time |
| `enum` | string (member name) |
| `byte[]` / `Memory<byte>` / `ReadOnlyMemory<byte>` | integer array, or Base64 string via `ByteArrayHandling` |
| arrays, lists, sets, queues, stacks, concurrent collections | array |
| objects, dictionaries | table |
| `object` members | runtime type on write, `TomlElement` on read |
| `TomlNode` / `TomlElement` / `TomlDocument` | the value's own kind |

TOML has no null: a null member is omitted by default. Dictionary keys may be strings, any integer type, an `enum`, a `Guid`, a `bool`, or a `char` — non-string keys are written as table keys in their invariant text (quoted when they fall outside the bare-key grammar) and parsed back on read, and a supported-key dictionary is valid at the document root. A `Stack<T>` round-trip reverses the stack: the writer emits pop order and the reader pushes in document order. The full per-type catalog, including each converter's read tolerances, is in the [built-in converter catalog](builtin-converters.md).

Choose the `byte[]` form with <xref:Bodu.Text.Toml.TomlByteArrayHandling> and the `decimal` form with <xref:Bodu.Text.Toml.TomlDecimalHandling> on the options:

<!-- compile -->
```csharp
var options = new TomlSerializerOptions
{
    ByteArrayHandling = TomlByteArrayHandling.Base64String,
    DecimalHandling = TomlDecimalHandling.String,   // lossless; default Float is native but binary64-bounded
};
```

## Pattern 3 — Worked example: nested tables and arrays of tables

A nested object becomes a `[table]`; a collection of objects becomes an `[[array of tables]]`. The full configuration shape round-trips through one model:

```csharp
using Bodu.Text.Toml;

public sealed class AppConfig
{
    public string? Title { get; set; }
    public ServerConfig? Server { get; set; }
    public List<EndpointConfig>? Endpoints { get; set; }
}

public sealed class ServerConfig
{
    public string? Host { get; set; }
    public int Port { get; set; }
}

public sealed class EndpointConfig
{
    public string? Path { get; set; }
    public bool AllowAnonymous { get; set; }
}

var config = new AppConfig
{
    Title = "demo",
    Server = new ServerConfig { Host = "localhost", Port = 8080 },
    Endpoints =
    [
        new EndpointConfig { Path = "/health", AllowAnonymous = true },
        new EndpointConfig { Path = "/admin", AllowAnonymous = false },
    ],
};

string text = TomlSerializer.Serialize(config);
```

The emitted document is the TOML a person would write — top-level keys first, then each table:

```toml
Title = "demo"

[Server]
Host = "localhost"
Port = 8080

[[Endpoints]]
Path = "/health"
AllowAnonymous = true

[[Endpoints]]
Path = "/admin"
AllowAnonymous = false
```

Deserializing the same text restores the full graph:

```csharp
AppConfig back = TomlSerializer.Deserialize<AppConfig>(text);
// back.Endpoints[1].Path → "/admin"
```

To emit lowercase keys (`title`, `[server]`, …) apply a naming policy ([Pattern 4](#pattern-4--rename-members)); to reorder the lines, use `[PropertyOrder]` ([Mapping attributes](attributes.md)).

## Pattern 4 — Rename members

```csharp
var options = new TomlSerializerOptions
{
    PropertyNamingPolicy = NamingPolicy.SnakeCaseLower,
};
```

Naming policies cover `CamelCase`, `SnakeCaseLower` / `SnakeCaseUpper`, and `KebabCaseLower` / `KebabCaseUpper`. Pin a single member's name with `[PropertyName("…")]`, which always wins over the policy. Start from a scenario preset by constructing the options from <xref:Bodu.Text.Toml.TomlSerializerDefaults> (for example `TomlSerializerDefaults.Web`, which also turns on case-insensitive matching).

On *read*, key matching is case-sensitive by default; set `PropertyNameCaseInsensitive = true` (or use the `Web` preset) to bind a key to a member regardless of case. The setting governs matching only — it does not change the name a member is *written* under.

Properties are mapped by default; public fields join in when `IncludeFields` is set on the options, or individually with `[Include]` on the field. Fields follow the same naming-policy, ordering, ignore, required, and converter rules as properties — including `[PropertyOrder]`, which reorders the emitted lines. The full attribute family is catalogued in [Mapping attributes](attributes.md).

## Pattern 5 — Select the spec version

```csharp
var options = new TomlSerializerOptions { SpecVersion = TomlSpecVersion.V1_1 };
var doc = TomlSerializer.Deserialize<MyDoc>(text, options);
```

The default is strict **v1.0.0**. Opting in to **v1.1.0** additionally accepts the `\e` and `\xHH` escapes, time values without seconds, and multi-line and trailing-comma inline tables. The writer always emits output valid under both versions.

## Pattern 6 — Edit a document with the mutable DOM

When you do not want a POCO, parse to <xref:Bodu.Text.Toml.Nodes.TomlNode> — index into the tree, mutate values, and write the document back:

```csharp
using Bodu.Text.Toml.Nodes;

TomlNode node = TomlNode.Parse(utf8Toml)!;   // the UTF-8 bytes of the document in Pattern 3
node["Server"]!["Port"] = 9090;

byte[] back = node.ToUtf8Bytes();
```

The re-emitted document keeps the same canonical layout with only the value changed:

```toml
[Server]
Host = "localhost"
Port = 9090
```

`Parse` takes UTF-8 bytes (`ReadOnlySpan<byte>`); for a `string` in hand, convert with `Encoding.UTF8.GetBytes(text)` first.

A <xref:Bodu.Text.Toml.Nodes.TomlNode> reads and writes through several conveniences: implicit conversions build a value node from a `string`, `long`, `int`, `double`, `bool`, or any of the four date-time types; explicit conversions (`(int)node`, `(string)node`, …) and the generic `node.GetValue<T>()` pull a scalar back out; and `AsObject()` / `AsArray()` / `AsValue()` narrow to the concrete node type. `TomlObject` is an ordered string-keyed map (`Add`, `Remove`, `TryGetValue`, `ContainsKey`) and `TomlArray` an ordered list (`Add`, `Insert`, `RemoveAt`, `IndexOf`); both preserve insertion order on write. `DeepClone()` copies a subtree and `TomlNode.DeepEquals(a, b)` compares two by structure.

```csharp
using Bodu.Text.Toml.Nodes;

var server = new TomlObject
{
    ["host"] = "localhost",   // implicit string → TomlValue
    ["port"] = 8080,          // implicit long → TomlValue
};
var root = new TomlObject { ["server"] = server };

int port = root["server"]!["port"]!.GetValue<int>();   // 8080
byte[] bytes = root.ToUtf8Bytes();
```

## Pattern 7 — Inspect a document with the read-only DOM

The read-only counterpart is a low-allocation view walked through `RootElement`:

```csharp
using Bodu.Text.Toml.Document;

using TomlDocument doc = TomlDocument.Parse(utf8Toml);
TomlElement port = doc.RootElement.GetProperty("Server").GetProperty("Port");
// port.GetInt64() → 8080
```

`TomlDocument.Parse` accepts a `string` as well as UTF-8 bytes. A document you parse (or deserialize as a member) is caller-owned — dispose it (the `using` above) when finished. Typed access goes through `GetString` / `GetInt64` / `GetDouble` / `GetBoolean` / `GetDateTimeOffset` / `GetDateTime` / `GetDateOnly` / `GetTimeOnly` on <xref:Bodu.Text.Toml.Document.TomlElement>, each of which throws if the element's `ValueKind` does not match. Walk structure with `GetProperty` / `TryGetProperty`, the integer indexer and `GetArrayLength` for arrays, and the allocation-light `EnumerateObject()` / `EnumerateArray()` enumerators:

```csharp
using Bodu.Text.Toml.Document;

using TomlDocument doc = TomlDocument.Parse(utf8Toml);

foreach (TomlProperty property in doc.RootElement.EnumerateObject())
{
    Console.WriteLine($"{property.Name} → {property.Value.ValueKind}");
}
```

Branch on <xref:Bodu.Text.Toml.TomlValueKind> (`String`, `Integer`, `Float`, `Boolean`, the four date-time kinds, `Array`, `Table`) before calling a typed getter when the shape is not known ahead of time.

## Pattern 8 — Streams and async

Both directions work over a `Stream`, synchronously on read and asynchronously in both directions, so a configuration file never has to materialize as a `string` first:

```csharp
await using FileStream output = File.Create("app.toml");
await TomlSerializer.SerializeAsync(output, config, cancellationToken: ct);
```

```csharp
await using FileStream input = File.OpenRead("app.toml");
AppConfig config = await TomlSerializer.DeserializeAsync<AppConfig>(input, cancellationToken: ct);
```

The synchronous `Deserialize<T>(Stream, …)` overload has the same shape without the token. Both async members accept an optional `TomlSerializerOptions` before the `CancellationToken`. Stream content is UTF-8.

## Pattern 9 — Process tokens by hand

For full control with no allocations, drive the reader/writer ref-struct machines directly. There are two readers, and which one you reach for depends on whether you care about the document's *surface syntax* or only its *logical shape*.

The **source-order** <xref:Bodu.Text.Toml.Reader.Utf8TomlReader> lexes the document as written — headers, dotted-key segments, inline tables, and comments all surface as their own tokens. Each `Read()` advances one token; the typed getters decode the current value, and `LineNumber` / `ColumnNumber` / `BytesConsumed` track position as byte-true offsets:

```csharp
using Bodu.Text.Toml;
using Bodu.Text.Toml.Reader;

var reader = new Utf8TomlReader("port = 8080 # listen\n"u8);

while (reader.Read())
{
    switch (reader.TokenType)
    {
        case TomlTokenType.Key:     Console.Write($"{reader.GetString()} = "); break;
        case TomlTokenType.Integer: Console.WriteLine(reader.GetInt64()); break;
        case TomlTokenType.Comment: Console.WriteLine($"// {reader.GetComment()}"); break;
    }
}
```

The **normalized** <xref:Bodu.Text.Toml.Reader.TomlDocumentReader> — the cursor a [converter](converters.md) receives — collapses every way of spelling a table onto a uniform `StartTable` / `PropertyName` / value / `EndTable` stream, so one read loop handles inline and header-defined tables alike. `Skip()` steps over a whole value, including nested tables and arrays:

```csharp
using Bodu.Text.Toml;
using Bodu.Text.Toml.Reader;

var reader = new TomlDocumentReader("""
    [server]
    host = "localhost"
    port = 8080
    """u8);

while (reader.Read())
{
    if (reader.TokenType == TomlTokenType.PropertyName && reader.GetString() == "port")
    {
        reader.Read();                       // advance onto the value
        Console.WriteLine(reader.GetInt64()); // 8080
    }
}
```

To emit tokens, drive the <xref:Bodu.Text.Toml.Writer.Utf8TomlWriter> over an `IBufferWriter<byte>` or a `Stream`. Write a structural skeleton with `WriteStartTable` / `WritePropertyName` / a typed `Write*` per value, and `Flush` (or `Dispose`) when streaming:

```csharp
using Bodu.Text.Toml.Writer;

var buffer = new ArrayBufferWriter<byte>();
var writer = new Utf8TomlWriter(buffer);

writer.WriteStartTable("server");
writer.WriteString("host", "localhost");
writer.WriteInteger("port", 8080);
writer.WriteEndTable();
// buffer.WrittenSpan now holds the UTF-8 bytes of:
//   [server]
//   host = "localhost"
//   port = 8080
```

> [!TIP]
> The `Utf8TomlReader` also parses **incrementally**: construct it with `isFinalBlock: false` and a <xref:Bodu.Text.Toml.Reader.TomlReaderState>, and when `Read()` returns `false` mid-token it rewinds wholly so you can resume over the next block carrying `CurrentState`. Use it to tokenize a document that arrives in chunks without buffering the whole thing first.

## Error handling

Two exception types separate "the text is not TOML" from "the TOML does not fit your type":

- <xref:Bodu.Text.Toml.TomlFormatException> — malformed input. Because TOML files are edited by hand, the exception carries the position: `LineNumber`, `ColumnNumber`, and byte `Offset`.
- <xref:Bodu.Text.Toml.TomlSerializationException> — the document parsed, but a value cannot bind: a kind mismatch, a missing required member, or a value the format cannot represent on write. It exposes the same `LineNumber` / `ColumnNumber` / `Offset` position where known, plus a `Path` naming the member that failed.

```csharp
try
{
    AppConfig config = TomlSerializer.Deserialize<AppConfig>(text);
}
catch (TomlFormatException ex)
{
    // Not valid TOML, e.g. an unterminated string:
    // "The string is not terminated." at line 1, column 14.
    log.Warn($"Parse error at {ex.LineNumber}:{ex.ColumnNumber}: {ex.Message}");
}
catch (TomlSerializationException ex)
{
    // Valid TOML that does not match the model,
    // e.g. "Expected a string but found 'Integer'."
    log.Warn($"Document does not bind: {ex.Message}");
}
```

`TryParse`-style members do not exist on the serializer; wrap `Deserialize` as above when reading untrusted input.

## See also

- [Mapping attributes](attributes.md), [Writing converters](converters.md), [Serialization callbacks](callbacks.md), [Built-in converter catalog](builtin-converters.md) — the customization guides.
- [Bodu.Text.Toml introduction](../../../docs/serialization/toml/index.md) — what is specific to the TOML format, including the value model and spec versions.
- [Bodu serializers introduction](../../../docs/serialization/index.md) and [core concepts](../../../docs/serialization/toml/concepts.md) — the family shape and the TOML vocabulary.
- [Text & Serialization guides](../../topics/text-and-serialization.md) and the [topic overview](../../../docs/topics/text-and-serialization.md).
- API reference — <xref:Bodu.Text.Toml.TomlSerializer>, <xref:Bodu.Text.Toml.TomlSerializerOptions>, <xref:Bodu.Text.Toml.Nodes.TomlNode>, <xref:Bodu.Text.Toml.Document.TomlDocument>.
