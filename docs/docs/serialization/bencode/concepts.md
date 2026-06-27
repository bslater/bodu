---
title: Bodu.Text.Bencode — Core concepts
---

# Core concepts

This page describes the vocabulary and shape of **Bodu.Text.Bencode**. Its sibling serializers — [Bodu.Text.Toml](../toml/index.md) and [Bodu.Text.Yaml](../yaml/index.md) — share the same architecture, so what you learn here transfers with the prefix changed; see the [family introduction](../index.md) for the cross-library view.

Part of the **[Text & Serialization](../../topics/text-and-serialization.md)** topic.

## The serializer

The static <xref:Bodu.Text.Bencode.BencodeSerializer> is the high-level entry point. `Serialize<T>` writes an object graph to Bencode; `Deserialize<T>` binds Bencode back to a type. Each has overloads over the format's natural surfaces: `Serialize` to `byte[]` / `IBufferWriter<byte>` / `Stream`, and `Deserialize<T>` from `ReadOnlySpan<byte>` / `byte[]` / `Stream`. Async stream variants are provided.

## Options

<xref:Bodu.Text.Bencode.BencodeSerializerOptions> configures the serializer. It holds the converter list (`Converters`), the property naming policy (`PropertyNamingPolicy`), the `DefaultIgnoreCondition`, the unmapped-member policy, and the maximum depth (`MaxDepth`, default 64). Construct one from a <xref:Bodu.Text.Bencode.BencodeSerializerDefaults> value (for example the `Web` preset) to start from a scenario's conventions.

An options instance becomes read-only the first time it is used — or eagerly via `MakeReadOnly()` — and then caches its resolved converters and type metadata. Configure one options object and reuse it across many operations.

## Converters and resolution

A <xref:Bodu.Text.Bencode.Serialization.BencodeConverter`1> converts one type, reading through the <xref:Bodu.Text.Bencode.Reader.Utf8BencodeReader> and writing through the <xref:Bodu.Text.Bencode.Writer.Utf8BencodeWriter>. A <xref:Bodu.Text.Bencode.Serialization.BencodeConverterFactory> produces converters for a family of types (every `Nullable<T>`, every enum, every collection) — the same pattern the built-in converters use.

For a given type the serializer resolves a converter by checking, in order:

1. a member-level converter attribute (`[BencodeConverter(typeof(…))]`);
2. a type-level converter attribute;
3. the first matching converter in `options.Converters`;
4. the built-in converters.

The first match wins, and the result is cached on the options.

## Attributes, callbacks, and naming policies

The full serialization surface lives in the `Bodu.Text.Bencode.Serialization` namespace:

- **Attributes** — `[BencodePropertyName]`, `[BencodeIgnore]`, `[BencodeConverter]`, `[BencodePropertyOrder]`, `[BencodeConstructor]`, `[BencodeRequired]`, `[BencodeInclude]`, `[BencodeExtensionData]`, `[BencodeNamingPolicy]`, `[BencodeUnmappedMemberHandling]`, `[BencodeObjectCreationHandling]`, `[BencodeStringEnumMemberName]`.
- **Callbacks** — the <xref:Bodu.Text.Bencode.Serialization.IBencodeOnSerializing> / <xref:Bodu.Text.Bencode.Serialization.IBencodeOnSerialized> / <xref:Bodu.Text.Bencode.Serialization.IBencodeOnDeserializing> / <xref:Bodu.Text.Bencode.Serialization.IBencodeOnDeserialized> interfaces, run at the matching point in the pipeline.
- **Naming policies** — <xref:Bodu.Text.Bencode.BencodeNamingPolicy>`.CamelCase`, `.SnakeCaseLower` / `.SnakeCaseUpper`, `.KebabCaseLower` / `.KebabCaseUpper`, plus the `BencodeSerializerDefaults.Web` preset.
- **Enum converters** — a string-enum converter (member names) and a number-enum converter.

## The document object models

When you do not want a model, the library offers two DOMs:

- **Mutable** — <xref:Bodu.Text.Bencode.Nodes.BencodeNode> / `BencodeObject` / `BencodeArray` / `BencodeValue`. `Parse` a document, index into it, mutate values, and write it back (`ToByteArray()`).
- **Read-only** — <xref:Bodu.Text.Bencode.Document.BencodeDocument> / `BencodeElement` / `BencodeProperty`. A low-allocation view over a parsed buffer, walked through `RootElement`.

## The low-level reader and writer

<xref:Bodu.Text.Bencode.Reader.Utf8BencodeReader> and <xref:Bodu.Text.Bencode.Writer.Utf8BencodeWriter> are forward-only, allocation-free `ref struct` token machines. The reader exposes the current token through a <xref:Bodu.Text.Bencode.BencodeTokenType>; the serializer and every converter are built on this pair. Reach for it directly when you want to process tokens without binding to a model.

## Value mapping

Bencode maps the BCL types it can represent natively and rejects the rest unless a converter handles them.

`string`, `byte[]`, and memory-of-byte map to byte strings; the integer family (through `ulong.MaxValue`, including the 128-bit types within the 64-bit surfaces) maps to `i…e`; enums map to member-name byte strings; collections (including queues, stacks, and the concurrent collections) map to lists; objects and dictionaries map to dictionaries with keys in canonical ascending bytewise order. Dictionary keys may be strings, integers, enums, `Guid`, `bool`, or `char`, stringified on the wire. Booleans, floating-point, and date-times have **no** Bencode form and require a registered converter. An `object`-typed member writes its runtime type and reads back as a `BencodeElement`. A `null` member is omitted on write; public fields participate via `IncludeFields` or `[BencodeInclude]`.

## Errors

A malformed document raises <xref:Bodu.Text.Bencode.BencodeFormatException> (carrying the byte `Offset` where parsing failed). A document that parses but cannot bind to your type — a type mismatch, a missing required member, a value the format cannot represent — raises <xref:Bodu.Text.Bencode.BencodeSerializationException>.

## Where to go next

- **[Bodu.Text.Bencode introduction](index.md)** — what is specific to the format: byte strings, canonical output, the kinds it cannot represent.
- **[Getting started](getting-started.md)** — install and the first round trip.
- **[Using Bencode](../../../guides/serialization/bencode/using.md)** — the worked walk-through.
- **[Bodu serializers introduction](../index.md)** — the shared family shape.
- **[Text & Serialization topic overview](../../topics/text-and-serialization.md)** — where the serializers sit among the codecs and document formats.
