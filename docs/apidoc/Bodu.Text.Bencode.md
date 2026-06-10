---
uid: Bodu.Text.Bencode
---

![Bodu.Text.Bencode](~/images/hero-bencode.svg)

## Purpose

**Bodu.Text.Bencode** is a self-contained [Bencode (BEP 3)](https://www.bittorrent.org/beps/bep_0003.html) serializer for .NET 8, shaped after [`System.Text.Json`](https://learn.microsoft.com/dotnet/api/system.text.json). It maps plain CLR objects to and from Bencode — the BitTorrent metadata format — through a configurable converter model, over a low-level, forward-only token reader and writer, with both a mutable and a read-only document object model.

The public surface mirrors `System.Text.Json` member for member, so the patterns are familiar: a static <xref:Bodu.Text.Bencode.BencodeSerializer> (the `JsonSerializer` analogue), the <xref:Bodu.Text.Bencode.Reader.Utf8BencodeReader> / <xref:Bodu.Text.Bencode.Writer.Utf8BencodeWriter> `ref struct` pair (the `Utf8JsonReader` / `Utf8JsonWriter` analogue), a mutable <xref:Bodu.Text.Bencode.Nodes.BencodeNode> DOM (the `JsonNode` analogue), and a read-only <xref:Bodu.Text.Bencode.Document.BencodeDocument> DOM (the `JsonDocument` analogue). The twin library <xref:Bodu.Text.Toml> applies the identical shape to TOML.

Output is always canonical Bencode: dictionary entries are emitted in ascending bytewise key order. For binary-to-text encodings (Base16 / Base32 / Base64 / Base58 / Base85) that operate on flat byte sequences without a structural grammar, see the companion <xref:Bodu.Text.Encoding> package.

## Static documentation

- **[Bodu serializers introduction](~/docs/serialization/index.md)** — the two libraries, the three tiers, scenarios.
- **[Core concepts](~/docs/serialization/concepts.md)** — the serializer, the converter model, the two DOMs, and the reader/writer seam.
- **[Getting started](~/docs/serialization/getting-started.md)** — install and the first round trip.
- **[Using Bencode](~/guides/serialization/bencode.md)** — type mapping, canonical ordering, the DOMs, and unsupported kinds.
- **[Writing converters](~/guides/serialization/converters.md)** — custom shapes with `BencodeConverter<T>`.

## Key types

**Serializer (`Bodu.Text.Bencode`)**

- <xref:Bodu.Text.Bencode.BencodeSerializer> — static façade. `Serialize` to `byte[]` / `IBufferWriter<byte>` / `Stream` and `Deserialize<T>` from `ReadOnlySpan<byte>` / `byte[]` / `Stream`, sync and async.
- <xref:Bodu.Text.Bencode.BencodeSerializerOptions> — converters, naming policy, ignore conditions, depth; cached and frozen on first use. The `JsonSerializerOptions` analogue.
- <xref:Bodu.Text.Bencode.BencodeSerializerDefaults> — the `General` / `Web` preset selector.
- <xref:Bodu.Text.Bencode.BencodeNamingPolicy> — camel, snake, and kebab casing policies.
- <xref:Bodu.Text.Bencode.BencodeTokenType>, <xref:Bodu.Text.Bencode.BencodeValueKind> — the token and value-kind enumerations.
- <xref:Bodu.Text.Bencode.BencodeFormatException> — malformed bytes. <xref:Bodu.Text.Bencode.BencodeSerializationException> — binding failures.

**Low-level reader / writer**

- <xref:Bodu.Text.Bencode.Reader.Utf8BencodeReader> (+ <xref:Bodu.Text.Bencode.Reader.BencodeReaderOptions>) — forward-only, allocation-free token reader; accepts only canonical BEP 3.
- <xref:Bodu.Text.Bencode.Writer.Utf8BencodeWriter> (+ <xref:Bodu.Text.Bencode.Writer.BencodeWriterOptions>) — forward-only token writer; emits canonical Bencode.

**Document object models**

- <xref:Bodu.Text.Bencode.Nodes.BencodeNode> / <xref:Bodu.Text.Bencode.Nodes.BencodeObject> / <xref:Bodu.Text.Bencode.Nodes.BencodeArray> / <xref:Bodu.Text.Bencode.Nodes.BencodeValue> — the mutable, editable DOM. `JsonNode` analogue.
- <xref:Bodu.Text.Bencode.Document.BencodeDocument> / <xref:Bodu.Text.Bencode.Document.BencodeElement> / <xref:Bodu.Text.Bencode.Document.BencodeProperty> — the read-only, low-allocation DOM. `JsonDocument` analogue.

**Converters and attributes (`Bodu.Text.Bencode.Serialization`)**

- <xref:Bodu.Text.Bencode.Serialization.BencodeConverter`1> / <xref:Bodu.Text.Bencode.Serialization.BencodeConverterFactory> — the `JsonConverter<T>` / `JsonConverterFactory` analogues.
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

- **`System.Text.Json` alignment.** Every concept maps onto a BCL JSON counterpart with the `Json` prefix swapped for `Bencode`. The converter, attribute, callback, naming-policy, and enum-converter surfaces are all present.
- **Self-contained.** The library has no shared engine dependency — everything the serializer needs lives in this assembly. Its twin, <xref:Bodu.Text.Toml>, mirrors it type for type for TOML.
- **Canonical output.** Bencode has exactly one canonical encoding for any value: integers use the shortest decimal representation with no padding and no `+` sign; dictionary keys are sorted by raw byte order; no whitespace is permitted. The serializer always produces canonical output, and the reader rejects every non-canonical input.
- **Value mapping.** Strings and `byte[]` map to byte strings, the integer family to `i…e`, and enums to member-name byte strings. Types with no canonical Bencode form — Booleans, floating-point, and date-times — require a registered <xref:Bodu.Text.Bencode.Serialization.BencodeConverter`1>; a `null` member is omitted on write.
- **Errors.** Malformed bytes surface through <xref:Bodu.Text.Bencode.BencodeFormatException>; binding failures through <xref:Bodu.Text.Bencode.BencodeSerializationException>.
- **See also:** the [introduction](~/docs/serialization/index.md), [core concepts](~/docs/serialization/concepts.md), and [getting-started](~/docs/serialization/getting-started.md); the [Using Bencode](~/guides/serialization/bencode.md) and [writing converters](~/guides/serialization/converters.md) guides; and the twin [TOML](xref:Bodu.Text.Toml) library.
