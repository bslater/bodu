---
title: Bodu.Text.Bencode — Core concepts
---

# Core concepts

This page describes the vocabulary and shape of **Bodu.Text.Bencode**. Its sibling serializers — [Bodu.Text.Toml](../toml/index.md) and [Bodu.Text.Yaml](../yaml/index.md) — share the same architecture, so what you learn here transfers with the prefix changed; see the [family introduction](../index.md) for the cross-library view.

Part of the **[Text & Serialization](../../topics/text-and-serialization.md)** topic.

## The serializer

The static <xref:Bodu.Text.Bencode.BencodeSerializer> is the high-level entry point. `Serialize<T>` writes an object graph to Bencode; `Deserialize<T>` binds Bencode back to a type. Each has overloads over the format's natural surfaces:

| Direction | Overloads |
|---|---|
| `Serialize<T>` | to `byte[]` (the return value), `IBufferWriter<byte>`, and `Stream`, plus `SerializeAsync<T>(Stream, …)`. |
| `Deserialize<T>` | from `ReadOnlySpan<byte>`, `byte[]`, and `Stream`, plus `DeserializeAsync<T>(Stream, …)`. |
| DOM bridges | `SerializeToNode<T>` (to a mutable <xref:Bodu.Text.Bencode.Nodes.BencodeNode>), `SerializeToDocument<T>` (to a read-only <xref:Bodu.Text.Bencode.Document.BencodeDocument>), and `Deserialize<T>(BencodeNode, …)` (bind straight from a node tree). |

Every overload accepts an optional <xref:Bodu.Text.Bencode.BencodeSerializerOptions>; the async pair takes it before the `CancellationToken`. There are no `TryDeserialize`-style members — wrap a call in a `try`/`catch` over the two exception types when reading untrusted input.

## Options

<xref:Bodu.Text.Bencode.BencodeSerializerOptions> configures the serializer:

| Member | Default | Governs |
|---|---|---|
| `Converters` | empty | The user converter list, searched ahead of the built-ins. |
| `PropertyNamingPolicy` | `null` | The <xref:Bodu.Text.Bencode.NamingPolicy> applied to member names with no explicit `[PropertyName]`. |
| `PropertyNameCaseInsensitive` | `false` | Whether a document key matches a member name ignoring case on read. |
| `IncludeFields` | `false` | Whether public fields join properties as serializable members. |
| `DefaultIgnoreCondition` | `Never` | The fallback <xref:Bodu.Text.Bencode.Serialization.IgnoreCondition> for members with no explicit `[Ignore]`. |
| `UnmappedMemberHandling` | `Skip` | Whether an unmapped key is skipped or rejected on read (<xref:Bodu.Text.Bencode.Serialization.UnmappedMemberHandling>). |
| `PreferredObjectCreationHandling` | `Replace` | Whether a member is replaced or populated on read (<xref:Bodu.Text.Bencode.Serialization.ObjectCreationHandling>). |
| `AllowUnsortedKeys` | `false` | Read-only leniency: accept dictionaries whose keys are not in ascending bytewise order. |
| `AllowDuplicateKeys` | `false` | Read-only leniency: accept repeated keys (last occurrence wins). |
| `MaxDepth` | `64` (`DefaultMaxDepth`) | The maximum nesting depth before a depth guard trips. |

Construct one from a <xref:Bodu.Text.Bencode.BencodeSerializerDefaults> value to start from a scenario's conventions: `General` leaves names unchanged with default-case matching; `Web` applies camel-case naming and case-insensitive matching.

`AllowUnsortedKeys` and `AllowDuplicateKeys` relax only the *read* path — the writer is unconditionally canonical, so anything written is byte-for-byte BEP 3 regardless of how lenient the read was.

An options instance becomes read-only the first time it is used — or eagerly via `MakeReadOnly()` (`IsReadOnly` reports the state) — and then caches its resolved converters and type metadata. Mutating a frozen instance throws `InvalidOperationException`. Configure one options object and reuse it across many operations.

## Converters and resolution

A <xref:Bodu.Text.Bencode.Serialization.BencodeConverter`1> converts one type, reading through the <xref:Bodu.Text.Bencode.Reader.Utf8BencodeReader> and writing through the <xref:Bodu.Text.Bencode.Writer.Utf8BencodeWriter>. A <xref:Bodu.Text.Bencode.Serialization.BencodeConverterFactory> produces converters for a family of types (every `Nullable<T>`, every enum, every collection) — the same pattern the built-in converters use.

For a given type the serializer resolves a converter by checking, in order:

1. a member-level converter attribute (`[Converter(typeof(…))]`);
2. a type-level converter attribute;
3. the first matching converter in `options.Converters`;
4. the built-in converters.

The first match wins, and the result is cached on the options.

## Attributes, callbacks, and naming policies

The full serialization surface lives in the `Bodu.Text.Bencode.Serialization` namespace:

- **Attributes** — `[PropertyName]`, `[Ignore]`, `[BencodeConverter]`, `[PropertyOrder]`, `[Constructor]`, `[Required]`, `[Include]`, `[ExtensionData]`, `[NamingPolicy]`, `[UnmappedMemberHandling]`, `[ObjectCreationHandling]`, `[StringEnumMemberName]`.
- **Callbacks** — the <xref:Bodu.Text.Bencode.Serialization.IOnSerializing> / <xref:Bodu.Text.Bencode.Serialization.IOnSerialized> / <xref:Bodu.Text.Bencode.Serialization.IOnDeserializing> / <xref:Bodu.Text.Bencode.Serialization.IOnDeserialized> interfaces, run at the matching point in the pipeline.
- **Naming policies** — <xref:Bodu.Text.Bencode.NamingPolicy>`.CamelCase`, `.SnakeCaseLower` / `.SnakeCaseUpper`, `.KebabCaseLower` / `.KebabCaseUpper`, plus the `BencodeSerializerDefaults.Web` preset.
- **Enum converters** — a string-enum converter (member names) and a number-enum converter.

## The document object models

When you do not want a model, the library offers two DOMs:

- **Mutable** — <xref:Bodu.Text.Bencode.Nodes.BencodeNode> with the concrete `BencodeObject` (a keyed dictionary node), `BencodeArray` (a list node), and `BencodeValue` (a scalar node). `Parse` a document into a tree, index into it with `node["key"]` / `node[index]`, mutate it, and write it back with `ToByteArray()`. Scalars convert with implicit operators (`string`, `long`, `int`, `ulong`, `byte[]` → `BencodeNode`) and explicit operators back the other way, and the tree supports `DeepClone()`, `DeepEquals(…)`, `ReplaceWith(…)`, and `GetPath()`. `Parse` returns `null` for an empty document.
- **Read-only** — <xref:Bodu.Text.Bencode.Document.BencodeDocument> with `BencodeElement` and `BencodeProperty`. A low-allocation view over a parsed buffer, walked through `RootElement`: `GetProperty` / `TryGetProperty`, the integer indexer for lists, `EnumerateObject()` / `EnumerateArray()`, and typed getters (`GetString`, `GetBytes`, `GetInt64`, `GetUInt64`, and the `TryGet…` pair). Each element's kind is a <xref:Bodu.Text.Bencode.BencodeValueKind> (`Object`, `Array`, `ByteString`, `Integer`). `BencodeDocument` is disposable — it owns a pooled buffer, so wrap it in `using` and `Clone()` out any element that must outlive it.

## The low-level reader and writer

<xref:Bodu.Text.Bencode.Reader.Utf8BencodeReader> and <xref:Bodu.Text.Bencode.Writer.Utf8BencodeWriter> are forward-only, allocation-free `ref struct` token machines over `ReadOnlySpan<byte>` and `IBufferWriter<byte>` respectively. The serializer and every converter are built on this pair; reach for it directly to process tokens without binding to a model.

The reader is positioned on a token by `Read()` (which returns `false` at the end), and the token is classified by `TokenType` — a <xref:Bodu.Text.Bencode.BencodeTokenType> with the values `None`, `StartList`, `EndList`, `StartDictionary`, `EndDictionary`, `PropertyName`, `Integer`, and `ByteString`. A Bencode dictionary surfaces as alternating `PropertyName` and value tokens between `StartDictionary` and `EndDictionary`. Once positioned, value getters read the current token without advancing:

| Reader member | Reads |
|---|---|
| `GetString()` / `GetBytes()` | The current byte-string or property-name token as UTF-8 text or raw bytes. |
| `GetInt32()` / `GetInt64()` / `GetUInt64()` | The current integer token, range-checked to the target width; the `TryGet…` overloads return `false` instead of throwing. |
| `ValueSpan` / `ValueTextEquals(…)` | The raw token bytes, or a zero-allocation comparison against UTF-8/`char`/`string` text. |
| `Skip()` / `TrySkip()` | Step over the current value in full, including a nested list or dictionary subtree. |
| `BytesConsumed` / `CurrentDepth` / `TokenStartIndex` | Diagnostic position state. |

The writer is the dual: structural pairs `WriteStartList()` / `WriteEndList()` and `WriteStartDictionary()` / `WriteEndDictionary()`, dictionary keys via `WritePropertyName(…)`, and scalars via `WriteInteger(long)` / `WriteInteger(ulong)`, `WriteByteString(ReadOnlySpan<byte>)`, and `WriteString(string)` (UTF-8). Convenience `name`-plus-value overloads (for example `WriteString(name, value)`) write a key and its value in one call, and `WriteRawValue(…)` splices a pre-encoded fragment. The writer re-sorts each dictionary's entries into ascending bytewise key order when the dictionary closes, so canonical output is automatic regardless of the order keys are presented.

## Value mapping

Bencode maps the BCL types it can represent natively and rejects the rest unless a converter handles them.

`string`, `byte[]`, and memory-of-byte map to byte strings; the integer family (through `ulong.MaxValue`, including the 128-bit types within the 64-bit surfaces) maps to `i…e`; enums map to member-name byte strings; collections (including queues, stacks, and the concurrent collections) map to lists; objects and dictionaries map to dictionaries with keys in canonical ascending bytewise order. Dictionary keys may be strings, integers, enums, `Guid`, `bool`, or `char`, stringified on the wire. Booleans, floating-point, and date-times have **no** Bencode form and require a registered converter. An `object`-typed member writes its runtime type and reads back as a `BencodeElement`. A `null` member is omitted on write; public fields participate via `IncludeFields` or `[Include]`.

## Errors

A malformed document raises <xref:Bodu.Text.Bencode.BencodeFormatException> (carrying the byte `Offset` where parsing failed) — truncated data, non-canonical integers, trailing bytes, and, unless the matching leniency option is set, out-of-order or duplicate dictionary keys. A document that parses but cannot bind to your type — a type mismatch, a missing required member, a value out of range for the target — raises <xref:Bodu.Text.Bencode.BencodeSerializationException> (carrying the byte `BytesOffset` of the failing value where one is known).

## Where to go next

- **[Bodu.Text.Bencode introduction](index.md)** — what is specific to the format: byte strings, canonical output, the kinds it cannot represent.
- **[Getting started](getting-started.md)** — install and the first round trip.
- **[Using Bencode](../../../guides/serialization/bencode/using.md)** — the worked walk-through.
- **[Bodu serializers introduction](../index.md)** — the shared family shape.
- **[Text & Serialization topic overview](../../topics/text-and-serialization.md)** — where the serializers sit among the codecs and document formats.
