---
title: Using Bencode
---

# Using Bencode

<xref:Bodu.Text.Serialization.Bencode.BencodeSerializer> maps your types to and from [Bencode (BEP 3)](https://www.bittorrent.org/beps/bep_0003.html), the binary encoding used by BitTorrent.

## Pattern 1 — Round-trip an object

```csharp
using Bodu.Text.Serialization.Bencode;

byte[] payload = BencodeSerializer.Serialize(new FileEntry { Name = "ubuntu.iso", Length = 1024 });
FileEntry entry = BencodeSerializer.Deserialize<FileEntry>(payload);
```

`Serialize` also writes to an `IBufferWriter<byte>` or a `Stream` (with `SerializeAsync`); `Deserialize` reads a `ReadOnlySpan<byte>`, a `byte[]`, or a `Stream` (with `DeserializeAsync`).

## Pattern 2 — Know the type mapping

| .NET | Bencode |
|---|---|
| `string` | byte string (UTF-8) |
| `byte[]` | byte string |
| integer types | integer (`i…e`) |
| `enum` | byte string (member name) |
| arrays, lists | list (`l…e`) |
| objects, string-keyed dictionaries | dictionary (`d…e`) |

Dictionary keys are always written in ascending bytewise order, as the grammar requires — so the output is canonical regardless of member declaration order.

## Pattern 3 — Handle the kinds Bencode cannot represent

Bencode has no Boolean, floating-point, or date-time kind. Serializing such a member throws `NotSupportedException` unless a [converter](converters.md) maps it to an integer or byte string:

```csharp
public sealed class BoolAsIntConverter : FormatConverter<bool>
{
    public override bool Read(ISerializationReader reader, Type t, FormatSerializerOptions o) => reader.GetInt64() != 0;
    public override void Write(ISerializationWriter writer, bool value, FormatSerializerOptions o) => writer.WriteInt64(value ? 1 : 0);
}
```

## Pattern 4 — Parse losslessly without binding

```csharp
using Bodu.Text.Serialization.Bencode.Syntax;

BencodeDocumentSyntax tree = BencodeSyntaxTree.Parse(bytes);
bool exact = tree.ToByteArray().AsSpan().SequenceEqual(bytes); // True — canonical bytes reproduced
```

Malformed input raises <xref:Bodu.Text.Serialization.Bencode.BencodeFormatException> with the byte offset.
