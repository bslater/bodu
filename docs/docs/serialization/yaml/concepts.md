---
title: Bodu.Text.Yaml — Core concepts
---

# Core concepts

This page describes the moving parts of **Bodu.Text.Yaml** — the serializer, the converter model, the two DOMs, and the reader/writer seam. The library keeps the [Bodu serializer family](../index.md) architecture but tunes its serializer surface to YAML; where a feature the [TOML](../toml/index.md) and [Bencode](../bencode/index.md) siblings carry has no equivalent here, that is called out below.

Part of the **[Text & Serialization](../../topics/text-and-serialization.md)** topic.

## The serializer

The static <xref:Bodu.Text.Yaml.YamlSerializer> is the high-level entry point. `Serialize` writes an object graph to YAML; `Deserialize<T>` binds YAML back to a type. Its surface is deliberately narrow — string text and UTF-8 bytes, with **no `Stream` overloads and no async API**:

- `Serialize<T>(T value, YamlSerializerOptions? options = null)` → `string`, and `Serialize(object? value, Type inputType, …)` → `string` for a runtime-typed value.
- `Deserialize<T>(string yaml, …)` → `T?` and `Deserialize<T>(ReadOnlySpan<byte> utf8Yaml, …)` → `T?`, plus `Deserialize(ReadOnlySpan<byte> utf8Yaml, Type returnType, …)` → `object?`.

`Deserialize<T>` returns a **nullable** `T?` — a top-level null scalar binds to `null`. Use the null-forgiving `!` where you know the document is non-null.

```csharp
using Bodu.Text.Yaml;

string yaml = YamlSerializer.Serialize(new ServerConfig { Host = "localhost", Port = 8080 });
ServerConfig config = YamlSerializer.Deserialize<ServerConfig>(yaml)!;
```

## Options

<xref:Bodu.Text.Yaml.YamlSerializerOptions> configures the serializer. The real properties are:

| Property | Effect |
|---|---|
| `PropertyNamingPolicy` | The <xref:Bodu.Text.Yaml.YamlNamingPolicy> applied to member names (`null` keeps the declared name). |
| `IncludeFields` | When `true`, public fields participate alongside properties. |
| `IgnoreNullValues` | When `true`, members whose value is `null` are omitted on write. |
| `WriteEnumsAsStrings` | When `true` (the default), enums write as member-name strings; when `false`, as integers. |
| `PropertyNameCaseInsensitive` | When `true`, mapping keys match members case-insensitively on read. |
| `SpecVersion` | <xref:Bodu.Text.Yaml.YamlSpecVersion> — `V1_2` (default) or `V1_1`. |
| `NumberHandling` | <xref:Bodu.Text.Yaml.YamlNumberHandling> — `Strict` (default) or `AllowFloatToInteger`. |
| `DuplicateKeyBehavior` | <xref:Bodu.Text.Yaml.YamlDuplicateKeyBehavior> — `Throw` (default), `UseFirst`, or `UseLast`. |
| `MergeKeyBehavior` | <xref:Bodu.Text.Yaml.YamlMergeKeyBehavior> — `Expand` (default), `Disabled`, or `PreserveAsNormalKey`. |
| `UnmappedMemberHandling` | <xref:Bodu.Text.Yaml.YamlUnmappedMemberHandling> — `Skip` (default) or `Disallow`. |
| `MaxDepth` | Maximum nesting depth; default 64. |
| `Converters` | The ordered list of custom <xref:Bodu.Text.Yaml.Serialization.YamlConverter> instances. |

An options instance becomes read-only the first time it is used — or eagerly via `MakeReadOnly()` (`IsReadOnly` reports the state) — and then caches its resolved converters and type metadata. **Configure one options object and reuse it** across many operations; constructing fresh options per call discards the caches.

## Converters and resolution

![YAML node model](../../../images/diagrams/yaml-node-model.svg)

A <xref:Bodu.Text.Yaml.Serialization.YamlConverter`1> converts one type. It reads from a <xref:Bodu.Text.Yaml.Document.YamlElement> (the already-parsed value) and writes through the <xref:Bodu.Text.Yaml.Writer.Utf8YamlWriter>:

```csharp
public abstract T? Read(YamlElement element, YamlSerializerOptions options);
public abstract void Write(ref Utf8YamlWriter writer, T value, YamlSerializerOptions options);
```

For a given type the serializer resolves a converter by scanning `options.Converters` for the first whose `CanConvert` returns `true`, falling back to the built-in handling when none matches. On the write path it looks up the value's **runtime** type first and then the declared type, so a converter registered for a concrete type still fires for a value held in an `object` or interface member. `CanConvert` on a <xref:Bodu.Text.Yaml.Serialization.YamlConverter`1> defaults to an **exact** type match (`typeof(T) == typeToConvert`), so a converter does not apply to subclasses unless you override `CanConvert`.

**There is no member-level or type-level converter attribute, and no converter factory** — those exist on the TOML and Bencode siblings but not here. The non-generic <xref:Bodu.Text.Yaml.Serialization.YamlConverter> base exists only so the collection can hold mixed converters; you always derive from the generic `YamlConverter<T>`. Register every converter on `options.Converters` before first use — the collection is guarded and throws once the options freeze. To shape members without a custom converter, use the naming policy, the `[YamlPropertyName]` / `[YamlIgnore]` attributes, and the options flags.

## The document object models

When you do not want a model, two DOMs serve the same documents:

- **Mutable** — <xref:Bodu.Text.Yaml.Nodes.YamlNode> / <xref:Bodu.Text.Yaml.Nodes.YamlObject> / <xref:Bodu.Text.Yaml.Nodes.YamlArray> / <xref:Bodu.Text.Yaml.Nodes.YamlValue>. `YamlNode.Parse(string)` builds the tree (returning `null` for a null/empty document); index into it with `[int]` / `[string]`, cast with `AsObject` / `AsArray` / `AsValue`, build scalars with `YamlValue.Create(…)` (overloads for `string` / `long` / `double` / `bool` — the four scalar kinds), read them back with `GetValue<T>()` (a direct return for the stored type, else a `Convert.ChangeType` coercion), and write it back with `ToYamlString()` (or `WriteTo(ref Utf8YamlWriter)`). `YamlObject` preserves insertion order; a node may appear at most once in a tree.
- **Read-only** — <xref:Bodu.Text.Yaml.Document.YamlDocument> / <xref:Bodu.Text.Yaml.Document.YamlElement> / <xref:Bodu.Text.Yaml.Document.YamlProperty>. A low-allocation view over a parsed buffer, walked through `RootElement`. `YamlDocument` is `IDisposable` — a document you parse is caller-owned, so dispose it when finished; an element read after disposal throws `ObjectDisposedException`. `ParseAllDocuments` returns every document in a multi-document stream, each independently caller-owned. Parsing options come from <xref:Bodu.Text.Yaml.Document.YamlDocumentOptions> (`SpecVersion`, `DuplicateKeyBehavior`, `MergeKeyBehavior`, `MaxDepth`).

Typed access on a `YamlElement` goes through `GetString` / `GetInt64` / `GetDouble` / `GetBoolean` (each throwing `InvalidOperationException` on a kind mismatch), with `GetProperty` (throwing `KeyNotFoundException`) / `TryGetProperty`, `EnumerateMapping`, `GetSequenceLength` / `EnumerateSequence`, the `[int]` indexer, `ValueKind` / `ScalarStyle`, and a `ToString()` that renders the scalar text or the container kind name.

## The buffered reader and the writer

<xref:Bodu.Text.Yaml.Reader.Utf8YamlReader> and <xref:Bodu.Text.Yaml.Writer.Utf8YamlWriter> are forward-only `ref struct` token machines, and the surface every converter receives. Two YAML-specific points:

- **The reader is buffered**, not a single-pass streaming scanner. Because YAML's anchors, aliases, merge keys, and indentation context require a resolved tree, the constructor copies the source and parses it fully into an in-memory node store; `Read()` then walks that store in document order. It exposes the current token through `TokenType` (a <xref:Bodu.Text.Yaml.YamlTokenType>), the typed getters `GetString()` / `GetInt64()` / `GetDouble()` / `GetBoolean()` (each throwing on a kind mismatch, with `GetDouble()` accepting an integer token too), `CurrentDepth`, and `ValueTextEquals(ReadOnlySpan<byte>)` for an allocation-free key comparison. <xref:Bodu.Text.Yaml.Reader.YamlReaderOptions> mirrors the relevant options (`SpecVersion`, `DuplicateKeyBehavior`, `MergeKeyBehavior`, `MaxDepth`). In this respect the reader is the analogue of the buffered Toml document cursor rather than the streaming `Utf8TomlReader`.
- **The writer emits block-style YAML.** Collections are always written in block form for readability; only an empty container falls back to flow `[]` / `{}` so it round-trips as empty rather than null. A string is emitted plain when that is unambiguous and double-quoted (with escapes) otherwise — there is no public scalar-style control on the write path. The writer enforces a well-formed call sequence: a value without a pending key, a mismatched `WriteEnd…`, or a second document root each throw `InvalidOperationException`. <xref:Bodu.Text.Yaml.Writer.YamlWriterOptions> sets `IndentSize` (default 2, capped at 16), `MaxDepth`, and `NewLine` (only `"\n"` or `"\r\n"`).

## Value mapping

The serializer maps the BCL types it can represent natively:

- `string` / `char` / `Guid` / `Uri` and the integer family → string or integer scalars; `double` / `float` → float scalars (with `.nan` / `.inf` / `-.inf`); `bool` → a Boolean scalar; `null` and a null `Nullable<T>` → the null scalar.
- `decimal` → a quoted exact-text string; `DateTime` / `DateTimeOffset` → a round-trip ISO-8601 string; `TimeSpan` → its invariant string. None of these is a native YAML kind, so each travels as a string and parses back with `CultureInfo.InvariantCulture`.
- Enums → member-name strings (or integers when `WriteEnumsAsStrings` is `false`); read back by name (case-insensitively) or by integer.
- Collections → sequences; dictionaries and plain objects → mappings. A dictionary keeps insertion order; an object writes its properties first (reflection order) then public fields when `IncludeFields` is set.
- An `object`-typed member writes its runtime type's form. On **read** an `object` target binds to a loosely-typed graph — `Dictionary<string, object?>` for a mapping, `List<object?>` for a sequence, and `bool` / `long` / `double` / `string` / `null` for scalars — not a <xref:Bodu.Text.Yaml.Document.YamlElement>. (A custom converter's `Read`, by contrast, *does* receive a `YamlElement`.)

The full per-type catalog — including the radix forms accepted on read and the quoting rules — is in the [built-in converter catalog](../../../guides/serialization/yaml/builtin-converters.md).

## Multi-document streams

A YAML stream may carry several documents separated by `---` and optionally terminated by `...`. <xref:Bodu.Text.Yaml.Document.YamlDocument.ParseAllDocuments*> returns every document; the single-document `Parse` and the serializer's `Deserialize<T>` read the first document only.

## Errors

A malformed document raises <xref:Bodu.Text.Yaml.YamlFormatException>, carrying `LineNumber`, `ColumnNumber`, and `Offset`. A document that parses but cannot bind — a kind mismatch, a value the format cannot represent, or an unmapped member under `Disallow` — raises <xref:Bodu.Text.Yaml.YamlSerializationException>, carrying `Offset`, `LineNumber`, `ColumnNumber`, and a dotted member `Path`.

## Where to go next

- **[Bodu.Text.Yaml introduction](index.md)** — the format specifics: presentation, spec versions, and multi-document streams.
- **[Getting started](getting-started.md)** — install and the first round trip.
- **[Using YAML](../../../guides/serialization/yaml/using.md)** — worked patterns across the serializer and both DOMs.
- **[Writing converters](../../../guides/serialization/yaml/converters.md)** and the **[built-in converter catalog](../../../guides/serialization/yaml/builtin-converters.md)**.
- **[Bodu serializers introduction](../index.md)** and the **[guides hub](../../../guides/serialization/index.md)**.
- **API reference** — <xref:Bodu.Text.Yaml>.
