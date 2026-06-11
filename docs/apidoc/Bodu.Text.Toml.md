---
uid: Bodu.Text.Toml
---

![Bodu.Text.Toml](~/images/hero-toml.svg)

## Purpose

**Bodu.Text.Toml** is a self-contained [TOML](https://toml.io/) (v1.0.0 / v1.1.0) library for .NET 8. It maps plain CLR objects to and from TOML through a configurable converter model, over a low-level forward-only token reader and writer, with both a mutable and a read-only document object model.

The public surface layers four tiers: a static <xref:Bodu.Text.Toml.TomlSerializer> for object mapping, the <xref:Bodu.Text.Toml.Reader.Utf8TomlReader> / <xref:Bodu.Text.Toml.Writer.Utf8TomlWriter> `ref struct` pair for forward-only token processing, a mutable <xref:Bodu.Text.Toml.Nodes.TomlNode> DOM, and a read-only <xref:Bodu.Text.Toml.Document.TomlDocument> DOM. The twin library <xref:Bodu.Text.Bencode> applies the identical shape to Bencode.

The types are organised into folders/namespaces by surface (`Reader`, `Writer`, `Document`, `Nodes`, `Serialization`). The reader enforces strict **TOML v1.0.0** by default and opts in to **TOML v1.1.0** grammar additions through the <xref:Bodu.Text.Toml.TomlSpecVersion> selector on the options. For EditorConfig-style INI configuration, see <xref:Bodu.Text.Ini>; for binary-to-text encodings (Base16 / Base32 / Base64 / Base58 / Base85), see the companion <xref:Bodu.Text.Encoding> package.

## Static documentation

- **[Bodu serializers introduction](~/docs/serialization/index.md)** — the two libraries, the three tiers, scenarios.
- **[Core concepts](~/docs/serialization/concepts.md)** — the serializer, the converter model, the two DOMs, and the reader/writer seam.
- **[Getting started](~/docs/serialization/getting-started.md)** — install and the first round trip.
- **[Using TOML](~/guides/serialization/toml.md)** — type mapping, spec-version selection, the DOMs, and streams.
- **[Writing converters](~/guides/serialization/converters.md)** — custom shapes with `TomlConverter<T>`.

## Key types

**Serializer (`Bodu.Text.Toml`)**

- <xref:Bodu.Text.Toml.TomlSerializer> — static façade. `Serialize` to `string` / `IBufferWriter<byte>` / `Stream` and `Deserialize<T>` from `string` / `ReadOnlySpan<byte>` / `Stream`, sync and async.
- <xref:Bodu.Text.Toml.TomlSerializerOptions> — converters, naming policy, ignore conditions, depth, `IncludeFields`, `SpecVersion`, and `ByteArrayHandling`; cached and frozen on first use.
- <xref:Bodu.Text.Toml.TomlSerializerDefaults> — the `General` / `Web` preset selector.
- <xref:Bodu.Text.Toml.TomlNamingPolicy> — camel, snake, and kebab casing policies.
- <xref:Bodu.Text.Toml.TomlTokenType>, <xref:Bodu.Text.Toml.TomlValueKind> — the token and value-kind enumerations.
- <xref:Bodu.Text.Toml.TomlSpecVersion> — the spec selector: `V1_0` (default) or `V1_1`. <xref:Bodu.Text.Toml.TomlByteArrayHandling> — integer-array or Base64-string `byte[]` mapping.
- <xref:Bodu.Text.Toml.TomlFormatException> — malformed input (with line / column / offset). <xref:Bodu.Text.Toml.TomlSerializationException> — binding failures.

**Low-level reader / writer**

- <xref:Bodu.Text.Toml.Reader.Utf8TomlReader> (+ <xref:Bodu.Text.Toml.Reader.TomlReaderOptions>) — forward-only, allocation-free token reader.
- <xref:Bodu.Text.Toml.Writer.Utf8TomlWriter> (+ <xref:Bodu.Text.Toml.Writer.TomlWriterOptions>) — forward-only token writer; emits canonical, block-style TOML.

**Document object models**

- <xref:Bodu.Text.Toml.Nodes.TomlNode> / <xref:Bodu.Text.Toml.Nodes.TomlObject> / <xref:Bodu.Text.Toml.Nodes.TomlArray> / <xref:Bodu.Text.Toml.Nodes.TomlValue> — the mutable, editable DOM.
- <xref:Bodu.Text.Toml.Document.TomlDocument> / <xref:Bodu.Text.Toml.Document.TomlElement> / <xref:Bodu.Text.Toml.Document.TomlProperty> — the read-only, low-allocation DOM.

**Converters and attributes (`Bodu.Text.Toml.Serialization`)**

- <xref:Bodu.Text.Toml.Serialization.TomlConverter`1> / <xref:Bodu.Text.Toml.Serialization.TomlConverterFactory> — base types for custom per-type converters and converter families.
- <xref:Bodu.Text.Toml.Serialization.TomlPropertyNameAttribute>, <xref:Bodu.Text.Toml.Serialization.TomlIgnoreAttribute>, <xref:Bodu.Text.Toml.Serialization.TomlConverterAttribute>, and the rest of the attribute family (`PropertyOrder`, `Constructor`, `Required`, `Include`, `ExtensionData`, `NamingPolicy`, `UnmappedMemberHandling`, `ObjectCreationHandling`, `StringEnumMemberName`).
- <xref:Bodu.Text.Toml.Serialization.ITomlOnSerializing>, <xref:Bodu.Text.Toml.Serialization.ITomlOnSerialized>, <xref:Bodu.Text.Toml.Serialization.ITomlOnDeserializing>, <xref:Bodu.Text.Toml.Serialization.ITomlOnDeserialized> — the serialization callbacks.
- <xref:Bodu.Text.Toml.Serialization.TomlStringEnumConverter>, <xref:Bodu.Text.Toml.Serialization.TomlNumberEnumConverter`1> — the built-in enum converters.

## Example

```csharp
using Bodu.Text.Toml;

public sealed class ServerConfig
{
    public string Host { get; set; } = "";
    public int Port { get; set; }
}

string toml = TomlSerializer.Serialize(new ServerConfig { Host = "localhost", Port = 8080 });
ServerConfig config = TomlSerializer.Deserialize<ServerConfig>(toml);

// Edit a document without a model:
using Bodu.Text.Toml.Nodes;
TomlNode node = TomlNode.Parse(utf8Toml)!;
node["server"]!["port"] = 9090;
byte[] back = node.ToUtf8Bytes();
```

## Notes

- **Full serializer surface.** The converter, attribute, callback, naming-policy (and `TomlSerializerDefaults.Web`), and enum-converter surfaces are all present.
- **Self-contained.** The library has no shared engine dependency — everything the serializer needs lives in this assembly. Its twin, <xref:Bodu.Text.Bencode>, mirrors it type for type for Bencode.
- **Value mapping.** `string` / `char` / `Guid` / `Uri` → string, the integer family → integer, `double` / `float` → float, `bool` → boolean, and `DateTimeOffset` / `DateTime` / `DateOnly` / `TimeOnly` → the four RFC 3339 date-time forms; `byte[]` → an integer array (or a Base64 string via <xref:Bodu.Text.Toml.TomlByteArrayHandling>); enums → member-name strings. Collections (arrays, lists, sets, queues, stacks, and the concurrent collections) map to arrays, with a `Stack<T>` round-trip reversing the stack (the writer emits pop order). Dictionaries map to tables in insertion order; keys may be strings, integers, enums, `Guid`, `bool`, or `char`, written as bare or quoted table keys. `decimal` and `TimeSpan` have no native TOML form and require a registered <xref:Bodu.Text.Toml.Serialization.TomlConverter`1>. Public fields participate via `IncludeFields` or `[TomlInclude]`.
- **Table root.** A TOML document root must map to a table, so a top-level scalar or array throws. Output is canonical TOML in document order, so `[TomlPropertyOrder]` is honored.
- **Spec-version selection.** Parsing defaults to strict v1.0.0; setting `SpecVersion` to `V1_1` accepts the v1.1.0 additions (`\e` and `\xHH` escapes, seconds-less times, multi-line / trailing-comma inline tables). The writer always emits output valid under both versions.
- **Errors.** Malformed input surfaces through <xref:Bodu.Text.Toml.TomlFormatException> (with line, column, and offset); binding failures through <xref:Bodu.Text.Toml.TomlSerializationException>.
- **See also:** the [introduction](~/docs/serialization/index.md), [core concepts](~/docs/serialization/concepts.md), and [getting-started](~/docs/serialization/getting-started.md); the [Using TOML](~/guides/serialization/toml.md) and [writing converters](~/guides/serialization/converters.md) guides; and the twin [Bencode](xref:Bodu.Text.Bencode) library.
