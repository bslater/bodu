---
title: Bodu serializers — Core concepts
---

# Core concepts

**Bodu.Text.Bencode** and **Bodu.Text.Toml** share the same vocabulary and member-for-member shape — only the `Bencode` / `Toml` prefix changes between them. This page uses the neutral `…` placeholder to describe a concept once for both libraries.

Part of the **[Text & Serialization](../topics/text-and-serialization.md)** topic.

## The serializer

The static `…Serializer` (<xref:Bodu.Text.Bencode.BencodeSerializer>, <xref:Bodu.Text.Toml.TomlSerializer>) is the high-level entry point. `Serialize<T>` writes an object graph to the format; `Deserialize<T>` binds the format back to a type. Each has overloads over the format's natural surfaces:

- **Bencode** — `Serialize` to `byte[]` / `IBufferWriter<byte>` / `Stream`; `Deserialize<T>` from `ReadOnlySpan<byte>` / `byte[]` / `Stream`. Async stream variants are provided.
- **TOML** — `Serialize` to `string` / `IBufferWriter<byte>` (UTF-8), or to a `Stream` via `SerializeAsync`; `Deserialize<T>` from `string` / `ReadOnlySpan<byte>` / `Stream` (with `DeserializeAsync`).

## Options

`…SerializerOptions` (<xref:Bodu.Text.Bencode.BencodeSerializerOptions>, <xref:Bodu.Text.Toml.TomlSerializerOptions>) configures the serializer. It holds the converter list (`Converters`), the property naming policy (`PropertyNamingPolicy`), the `DefaultIgnoreCondition`, the unmapped-member policy, and the maximum depth (`MaxDepth`, default 64). Construct one from a `…SerializerDefaults` value (for example the `Web` preset) to start from a scenario's conventions.

An options instance becomes read-only the first time it is used — or eagerly via `MakeReadOnly()` — and then caches its resolved converters and type metadata. Configure one options object and reuse it across many operations.

## Converters and resolution

A `…Converter<T>` (<xref:Bodu.Text.Bencode.Serialization.BencodeConverter`1>, <xref:Bodu.Text.Toml.Serialization.TomlConverter`1>) converts one type, reading through the `Utf8…Reader` and writing through the `Utf8…Writer`. A `…ConverterFactory` produces converters for a family of types (every `Nullable<T>`, every enum, every collection) — the same pattern the built-in converters use.

For a given type the serializer resolves a converter by checking, in order:

1. a member-level converter attribute (`[…Converter(typeof(…))]`);
2. a type-level converter attribute;
3. the first matching converter in `options.Converters`;
4. the built-in converters.

The first match wins, and the result is cached on the options.

## Attributes, callbacks, and naming policies

Both libraries ship the full serialization surface in the `Bodu.Text.<Format>.Serialization` namespace:

- **Attributes** — `[…PropertyName]`, `[…Ignore]`, `[…Converter]`, `[…PropertyOrder]`, `[…Constructor]`, `[…Required]`, `[…Include]`, `[…ExtensionData]`, `[…NamingPolicy]`, `[…UnmappedMemberHandling]`, `[…ObjectCreationHandling]`, `[…StringEnumMemberName]`.
- **Callbacks** — the `I…OnSerializing` / `I…OnSerialized` / `I…OnDeserializing` / `I…OnDeserialized` interfaces, run at the matching point in the pipeline.
- **Naming policies** — `…NamingPolicy.CamelCase`, `.SnakeCaseLower` / `.SnakeCaseUpper`, `.KebabCaseLower` / `.KebabCaseUpper`, plus the `…SerializerDefaults.Web` preset.
- **Enum converters** — a string-enum converter (member names) and a number-enum converter.

## The document object models

When you do not want a model, each library offers two DOMs:

- **Mutable** — `…Node` / `…Object` / `…Array` / `…Value` (<xref:Bodu.Text.Bencode.Nodes.BencodeNode>, <xref:Bodu.Text.Toml.Nodes.TomlNode>). `Parse` a document, index into it, mutate values, and write it back (`ToUtf8Bytes()` for Bencode / TOML).
- **Read-only** — `…Document` / `…Element` / `…Property` (<xref:Bodu.Text.Bencode.Document.BencodeDocument>, <xref:Bodu.Text.Toml.Document.TomlDocument>). A low-allocation view over a parsed buffer, walked through `RootElement`.

## The low-level reader and writer

`Utf8…Reader` and `Utf8…Writer` (<xref:Bodu.Text.Bencode.Reader.Utf8BencodeReader>, <xref:Bodu.Text.Bencode.Writer.Utf8BencodeWriter>, <xref:Bodu.Text.Toml.Reader.Utf8TomlReader>, <xref:Bodu.Text.Toml.Writer.Utf8TomlWriter>) are forward-only, allocation-free `ref struct` token machines. The reader exposes the current token through a `…TokenType`; the serializer and every converter are built on this pair. Reach for it directly when you want to process tokens without binding to a model.

## Value mapping

Each format maps the BCL types it can represent natively and rejects the rest unless a converter handles them.

- **Bencode** — `string`, `byte[]`, and memory-of-byte → byte string, the integer family (through `ulong.MaxValue`, including the 128-bit types within the 64-bit surfaces) → `i…e`, enums → member-name byte strings; collections (including queues, stacks, and the concurrent collections) → lists; objects and dictionaries → dictionaries with keys in canonical ascending bytewise order. Dictionary keys may be strings, integers, enums, `Guid`, `bool`, or `char`, stringified on the wire. Booleans, floating-point, and date-times have **no** Bencode form and require a registered converter. An `object`-typed member writes its runtime type and reads back as a `BencodeElement`. A `null` member is omitted on write; public fields participate via `IncludeFields` or `[BencodeInclude]`.
- **TOML** — `string` / `char` / `Guid` / `Uri` / `Version` → string, `TimeSpan` → the invariant `"c"`-format string, the integer family (including the 128-bit types within the i64 range) → integer, `double` / `float` / `Half` → float, `decimal` → float or a lossless string per `DecimalHandling`, `bool` → boolean, and `DateTimeOffset` / `DateTime` / `DateOnly` / `TimeOnly` → the four RFC 3339 date-time forms; `byte[]` and memory-of-byte → an integer array (or a Base64 string via `ByteArrayHandling`); enums → member-name strings; collections (including queues, stacks, and the concurrent collections) → arrays; dictionaries → tables in insertion order, with string, integer, enum, `Guid`, `bool`, or `char` keys. An `object`-typed member writes its runtime type and reads back as a `TomlElement`. The document root must be a table, so a top-level scalar or array throws; public fields participate via `IncludeFields` or `[TomlInclude]`.

## Errors

A malformed document raises a format-specific parse exception (<xref:Bodu.Text.Bencode.BencodeFormatException>, <xref:Bodu.Text.Toml.TomlFormatException> — the latter carries line, column, and offset). A document that parses but cannot bind to your type — a type mismatch, a missing required member, a value the format cannot represent — raises a format-specific serialization exception (<xref:Bodu.Text.Bencode.BencodeSerializationException>, <xref:Bodu.Text.Toml.TomlSerializationException>).
