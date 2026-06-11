---
title: Using Bencode
---

# Using Bencode

<xref:Bodu.Text.Bencode.BencodeSerializer> maps your types to and from [Bencode (BEP 3)](https://www.bittorrent.org/beps/bep_0003.html), the binary encoding used by BitTorrent.

## Pattern 1 — Round-trip an object

```csharp
using Bodu.Text.Bencode;

byte[] payload = BencodeSerializer.Serialize(new FileEntry { Name = "ubuntu.iso", Length = 1024 });
FileEntry entry = BencodeSerializer.Deserialize<FileEntry>(payload);
```

`Serialize` also writes to an `IBufferWriter<byte>` or a `Stream` (with `SerializeAsync`); `Deserialize` reads a `ReadOnlySpan<byte>`, a `byte[]`, or a `Stream` (with `DeserializeAsync`).

## Pattern 2 — Know the type mapping

| .NET | Bencode |
|---|---|
| `string` | byte string (UTF-8) |
| `byte[]` / `Memory<byte>` / `ReadOnlyMemory<byte>` | byte string |
| integer types (incl. `ulong` and `UInt128` beyond `long.MaxValue`, up to `ulong.MaxValue`) | integer (`i…e`) |
| `enum` | byte string (member name) |
| arrays, lists, sets, queues, stacks, concurrent collections | list (`l…e`) |
| objects, dictionaries | dictionary (`d…e`) |
| `object` members | runtime type on write, `BencodeElement` on read |
| `BencodeNode` / `BencodeElement` / `BencodeDocument` | the value's own kind |

Output is always canonical: dictionary entries are emitted in ascending bytewise key order regardless of member declaration order, and a `null` member is omitted on write. Dictionary keys may be strings, any integer type, an `enum`, a `Guid`, a `bool`, or a `char` — non-string keys are written as their invariant text and parsed back on read. A `Stack<T>` round-trip reverses the stack: the writer emits pop order and the reader pushes in document order. The full per-type catalog, including each converter's read tolerances, is in the [built-in converter catalog](builtin-converters.md).

## Pattern 3 — Rename members

```csharp
var options = new BencodeSerializerOptions
{
    PropertyNamingPolicy = BencodeNamingPolicy.SnakeCaseLower,
};
```

Naming policies cover `CamelCase`, `SnakeCaseLower` / `SnakeCaseUpper`, and `KebabCaseLower` / `KebabCaseUpper`. Pin a single member's name with `[BencodePropertyName("…")]`, which always wins over the policy.

Properties are mapped by default; public fields join in when `IncludeFields` is set on the options, or individually with `[BencodeInclude]` on the field. Fields follow the same naming-policy, ignore, required, and converter rules as properties. The full attribute family is catalogued in [Mapping attributes](attributes.md).

## Pattern 4 — Handle the kinds Bencode cannot represent

Bencode has no Boolean, floating-point, or date-time kind. Serializing such a member fails unless a [converter](converters.md) maps it to an integer or byte string:

```csharp
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Serialization;
using Bodu.Text.Bencode.Writer;

public sealed class BoolAsIntConverter : BencodeConverter<bool>
{
    public override bool Read(ref Utf8BencodeReader reader, Type t, BencodeSerializerOptions o) =>
        reader.GetInt64() != 0;

    public override void Write(Utf8BencodeWriter writer, bool value, BencodeSerializerOptions o) =>
        writer.WriteInteger(value ? 1 : 0);
}
```

## Pattern 5 — Use a document model instead of a type

When you do not want a POCO, parse to one of the two DOMs.

Mutable DOM — parse, edit, and write back:

```csharp
using Bodu.Text.Bencode.Nodes;

BencodeNode node = BencodeNode.Parse(payload)!;
byte[] back = node.ToByteArray();
```

Read-only DOM — a low-allocation view walked through `RootElement`:

```csharp
using Bodu.Text.Bencode.Document;

using BencodeDocument doc = BencodeDocument.Parse(payload);
BencodeElement name = doc.RootElement.GetProperty("info").GetProperty("name");
```

## Pattern 6 — Process tokens by hand

For full control with no allocations, drive the <xref:Bodu.Text.Bencode.Reader.Utf8BencodeReader> / <xref:Bodu.Text.Bencode.Writer.Utf8BencodeWriter> ref-struct pair directly. The reader accepts only canonical BEP 3 (no leading or negative zeros, ascending unique dictionary keys, a single root with no trailing bytes).

Malformed input raises <xref:Bodu.Text.Bencode.BencodeFormatException>; a value that cannot bind raises <xref:Bodu.Text.Bencode.BencodeSerializationException>.
