---
uid: Bodu.Text.Bencode
---

![Bodu.Text.Bencode](~/images/hero-bencode.svg)

## Purpose

**Bodu.Text.Bencode** is a self-contained [Bencode (BEP 3)](https://www.bittorrent.org/beps/bep_0003.html) serializer for .NET 8. It maps plain CLR objects to and from Bencode — the BitTorrent metadata format — through a configurable converter model, over a low-level, forward-only token reader and writer, with both a mutable and a read-only document object model.

The public surface layers four tiers: a static <xref:Bodu.Text.Bencode.BencodeSerializer> for object mapping, the <xref:Bodu.Text.Bencode.Reader.Utf8BencodeReader> / <xref:Bodu.Text.Bencode.Writer.Utf8BencodeWriter> `ref struct` pair for forward-only token processing, a mutable <xref:Bodu.Text.Bencode.Nodes.BencodeNode> DOM, and a read-only <xref:Bodu.Text.Bencode.Document.BencodeDocument> DOM. The twin library <xref:Bodu.Text.Toml> applies the identical shape to TOML.

Output is always canonical Bencode: dictionary entries are emitted in ascending bytewise key order. For binary-to-text encodings (Base16 / Base32 / Base64 / Base58 / Base85) that operate on flat byte sequences without a structural grammar, see the companion <xref:Bodu.Text.Encoding> package.

## Static documentation

- **[Bodu serializers introduction](~/docs/serialization/index.md)** — the three libraries, the shared tiers, and how to choose a format.
- **[Core concepts](~/docs/serialization/bencode/concepts.md)** — the serializer, the converter model, the two DOMs, and the reader/writer seam.
- **[Getting started](~/docs/serialization/bencode/getting-started.md)** — install and the first round trip.
- **[Using Bencode](~/guides/serialization/bencode/using.md)** — type mapping, canonical ordering, the DOMs, and unsupported kinds.
- **[Writing converters](~/guides/serialization/bencode/converters.md)** — custom shapes with `BencodeConverter<T>`.

## Key types

**Serializer (`Bodu.Text.Bencode`)**

- <xref:Bodu.Text.Bencode.BencodeSerializer> — static façade. `Serialize` to `byte[]` / `IBufferWriter<byte>` / `Stream` and `Deserialize<T>` from `ReadOnlySpan<byte>` / `byte[]` / `Stream`, sync and async.
- <xref:Bodu.Text.Bencode.BencodeSerializerOptions> — converters, naming policy, ignore conditions, depth, and `IncludeFields`; cached and frozen on first use.
- <xref:Bodu.Text.Bencode.BencodeSerializerDefaults> — the `General` / `Web` preset selector.
- <xref:Bodu.Text.Bencode.BencodeNamingPolicy> — camel, snake, and kebab casing policies.
- <xref:Bodu.Text.Bencode.BencodeTokenType>, <xref:Bodu.Text.Bencode.BencodeValueKind> — the token and value-kind enumerations.
- <xref:Bodu.Text.Bencode.BencodeFormatException> — malformed bytes. <xref:Bodu.Text.Bencode.BencodeSerializationException> — binding failures.

**Low-level reader / writer**

- <xref:Bodu.Text.Bencode.Reader.Utf8BencodeReader> (+ <xref:Bodu.Text.Bencode.Reader.BencodeReaderOptions>) — forward-only, allocation-free token reader; accepts only canonical BEP 3.
- <xref:Bodu.Text.Bencode.Writer.Utf8BencodeWriter> (+ <xref:Bodu.Text.Bencode.Writer.BencodeWriterOptions>) — forward-only token writer; emits canonical Bencode.

**Document object models**

- <xref:Bodu.Text.Bencode.Nodes.BencodeNode> / <xref:Bodu.Text.Bencode.Nodes.BencodeObject> / <xref:Bodu.Text.Bencode.Nodes.BencodeArray> / <xref:Bodu.Text.Bencode.Nodes.BencodeValue> — the mutable, editable DOM (parsing tuned by <xref:Bodu.Text.Bencode.Nodes.BencodeNodeOptions>).
- <xref:Bodu.Text.Bencode.Document.BencodeDocument> / <xref:Bodu.Text.Bencode.Document.BencodeElement> / <xref:Bodu.Text.Bencode.Document.BencodeProperty> — the read-only, low-allocation DOM.

**Converters and attributes (`Bodu.Text.Bencode.Serialization`)**

- <xref:Bodu.Text.Bencode.Serialization.BencodeConverter`1> / <xref:Bodu.Text.Bencode.Serialization.BencodeConverterFactory> — base types for custom per-type converters and converter families.
- <xref:Bodu.Text.Bencode.Serialization.BencodePropertyNameAttribute>, <xref:Bodu.Text.Bencode.Serialization.BencodeIgnoreAttribute>, <xref:Bodu.Text.Bencode.Serialization.BencodeConverterAttribute>, and the rest of the attribute family (`PropertyOrder`, `Constructor`, `Required`, `Include`, `ExtensionData`, `NamingPolicy`, `UnmappedMemberHandling`, `ObjectCreationHandling`, `StringEnumMemberName`).
- <xref:Bodu.Text.Bencode.Serialization.IBencodeOnSerializing>, <xref:Bodu.Text.Bencode.Serialization.IBencodeOnSerialized>, <xref:Bodu.Text.Bencode.Serialization.IBencodeOnDeserializing>, <xref:Bodu.Text.Bencode.Serialization.IBencodeOnDeserialized> — the serialization callbacks.
- <xref:Bodu.Text.Bencode.Serialization.BencodeStringEnumConverter>, <xref:Bodu.Text.Bencode.Serialization.BencodeNumberEnumConverter`1> — the built-in enum converters.

## Example

```csharp
using Bodu.Text.Bencode;

public sealed class TorrentInfo
{
    public string Name { get; set; } = "";
    public long Length { get; set; }
}

byte[] payload = BencodeSerializer.Serialize(new TorrentInfo { Name = "ubuntu.iso", Length = 1024 });
// d6:Lengthi1024e4:Name10:ubuntu.isoe   (dictionary keys in canonical order)

TorrentInfo info = BencodeSerializer.Deserialize<TorrentInfo>(payload);

// Edit a document without a model:
using Bodu.Text.Bencode.Nodes;
BencodeNode node = BencodeNode.Parse(payload)!;
byte[] back = node.ToByteArray();
```

## Notes

- **Full serializer surface.** The converter, attribute, callback, naming-policy, and enum-converter surfaces are all present.
- **Self-contained.** The library has no shared engine dependency — everything the serializer needs lives in this assembly. Its twin, <xref:Bodu.Text.Toml>, mirrors it type for type for TOML; <xref:Bodu.Text.Yaml> shares the architecture with a YAML-tuned surface.
- **Canonical output.** Bencode has exactly one canonical encoding for any value: integers use the shortest decimal representation with no padding and no `+` sign; dictionary keys are sorted by raw byte order; no whitespace is permitted. The serializer always produces canonical output, and the reader rejects every non-canonical input.
- **Value mapping.** Strings, `byte[]`, and memory-of-byte map to byte strings, the integer family — spanning the full `long.MinValue` through `ulong.MaxValue` range, with the 128-bit types confined to the 64-bit surfaces — to `i…e`, and enums to member-name byte strings. Collections (arrays, lists, sets, queues, stacks, and the concurrent collections) map to lists, with a `Stack<T>` round-trip reversing the stack (the writer emits pop order). Dictionaries map to canonical dictionaries; keys may be strings, integers, enums, `Guid`, `bool`, or `char`, stringified on the wire. An `object`-typed member writes its runtime type and reads back as a <xref:Bodu.Text.Bencode.Document.BencodeElement>; the read-only DOM types participate directly. Types with no canonical Bencode form — Booleans, floating-point, and date-times — require a registered <xref:Bodu.Text.Bencode.Serialization.BencodeConverter`1>; a `null` member is omitted on write. Public fields participate via `IncludeFields` or `[BencodeInclude]`. The full per-type list is in the [built-in converter catalog](~/guides/serialization/bencode/builtin-converters.md).
- **Errors.** Malformed bytes surface through <xref:Bodu.Text.Bencode.BencodeFormatException>; binding failures through <xref:Bodu.Text.Bencode.BencodeSerializationException>.
- **See also:** the [introduction](~/docs/serialization/index.md), [core concepts](~/docs/serialization/bencode/concepts.md), and [getting-started](~/docs/serialization/bencode/getting-started.md); the [Using Bencode](~/guides/serialization/bencode/using.md) and [writing converters](~/guides/serialization/bencode/converters.md) guides; and the sibling [TOML](xref:Bodu.Text.Toml) and [YAML](xref:Bodu.Text.Yaml) libraries.
