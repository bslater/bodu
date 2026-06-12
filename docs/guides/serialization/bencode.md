---
title: Using Bencode
---

# Using Bencode

<xref:Bodu.Text.Bencode.BencodeSerializer> maps your types to and from [Bencode (BEP 3)](https://www.bittorrent.org/beps/bep_0003.html), the binary encoding used by BitTorrent. Behavior is configured through <xref:Bodu.Text.Bencode.BencodeSerializerOptions>; when you do not want a POCO, the same payloads are served by the mutable <xref:Bodu.Text.Bencode.Nodes.BencodeNode> DOM and the read-only <xref:Bodu.Text.Bencode.Document.BencodeDocument> DOM.

## Pattern 1 — Round-trip an object

```csharp
using Bodu.Text.Bencode;

byte[] payload = BencodeSerializer.Serialize(new FileEntry { Name = "ubuntu.iso", Length = 1024 });
FileEntry entry = BencodeSerializer.Deserialize<FileEntry>(payload);
```

`Serialize` also writes to an `IBufferWriter<byte>` or a `Stream` (with `SerializeAsync`); `Deserialize` reads a `ReadOnlySpan<byte>`, a `byte[]`, or a `Stream` (with `DeserializeAsync`). See [Pattern 8](#pattern-8--streams-and-async) for the stream surface.

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

## Pattern 3 — Worked example: torrent-style metadata

Bencode's natural habitat is torrent metadata: text fields, integers, a nested `info` dictionary, and the raw SHA-1 `pieces` blob side by side. `byte[]` members map straight to byte strings — no Base64 detour — and `[BencodePropertyName]` pins the lowercase, space-separated keys the format uses on the wire:

```csharp
using Bodu.Text.Bencode;
using Bodu.Text.Bencode.Serialization;

public sealed class Torrent
{
    [BencodePropertyName("announce")]
    public string? Announce { get; set; }

    [BencodePropertyName("info")]
    public TorrentInfo? Info { get; set; }
}

public sealed class TorrentInfo
{
    [BencodePropertyName("name")]
    public string? Name { get; set; }

    [BencodePropertyName("piece length")]
    public long PieceLength { get; set; }

    [BencodePropertyName("length")]
    public long Length { get; set; }

    [BencodePropertyName("pieces")]
    public byte[]? Pieces { get; set; }   // concatenated 20-byte SHA-1 hashes
}
```

Serializing produces canonical BEP 3 bytes — every key in ascending bytewise order, the `pieces` hashes carried verbatim:

```csharp
var torrent = new Torrent
{
    Announce = "http://tracker.example.com/announce",
    Info = new TorrentInfo
    {
        Name = "ubuntu.iso",
        PieceLength = 262_144,
        Length = 1_048_576,
        Pieces = pieceHashes,   // 20 bytes per piece
    },
};

byte[] payload = BencodeSerializer.Serialize(torrent);
// → d8:announce35:http://tracker.example.com/announce
//   4:infod6:lengthi1048576e4:name10:ubuntu.iso12:piece lengthi262144e6:pieces20:<raw bytes>e
//   e   (shown wrapped; the payload is a single run of bytes)
```

The same payload can be inspected without the model through the read-only DOM:

```csharp
using Bodu.Text.Bencode.Document;

using BencodeDocument doc = BencodeDocument.Parse(payload);
BencodeElement info = doc.RootElement.GetProperty("info");

string name   = info.GetProperty("name").GetString();        // → "ubuntu.iso"
long   length = info.GetProperty("piece length").GetInt64(); // → 262144
byte[] pieces = info.GetProperty("pieces").GetBytes();       // → the raw hash bytes
```

Because the writer is canonical and the reader accepts only canonical input, a successful round trip is byte-identical — which is exactly what an info-hash computed over the encoded `info` dictionary requires.

## Pattern 4 — Rely on canonical ordering

Member declaration order never leaks into the output. Declaring members out of order still emits sorted keys:

```csharp
public sealed class OutOfOrder
{
    public int Zeta  { get; set; } = 1;   // declared first
    public int Alpha { get; set; } = 2;   // declared second
}

byte[] bytes = BencodeSerializer.Serialize(new OutOfOrder());
// → d5:Alphai2e4:Zetai1ee   — "Alpha" precedes "Zeta" bytewise
```

This makes the encoded form deterministic for the same data, so it is safe to hash, sign, or compare payloads byte for byte. (`[BencodePropertyOrder]` affects only the order members are presented to the writer; the dictionary is re-sorted when it closes.)

## Pattern 5 — Rename members

```csharp
var options = new BencodeSerializerOptions
{
    PropertyNamingPolicy = BencodeNamingPolicy.SnakeCaseLower,
};
```

Naming policies cover `CamelCase`, `SnakeCaseLower` / `SnakeCaseUpper`, and `KebabCaseLower` / `KebabCaseUpper`. Pin a single member's name with `[BencodePropertyName("…")]`, which always wins over the policy.

Properties are mapped by default; public fields join in when `IncludeFields` is set on the options, or individually with `[BencodeInclude]` on the field. Fields follow the same naming-policy, ignore, required, and converter rules as properties. The full attribute family is catalogued in [Mapping attributes](attributes.md).

## Pattern 6 — Handle the kinds Bencode cannot represent

Bencode has exactly two scalar kinds — integers and byte strings — so several everyday .NET types have no native form. The library never invents a lossy representation implicitly; serializing such a member fails unless a [converter](converters.md) maps it to an integer or byte string:

| .NET kind | Native Bencode form | Typical bridge |
|---|---|---|
| `bool` | none | integer `i0e` / `i1e` |
| `double` / `float` / `decimal` | none | scaled integer, or invariant text as a byte string |
| `DateTime` / `DateTimeOffset` / `DateOnly` / `TimeOnly` | none | Unix seconds as an integer |
| `char` / `Guid` / `Uri` / `Version` / `TimeSpan` | none | invariant text as a byte string |

A `bool` bridged to an integer:

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

The same shape bridges a `DateTimeOffset` to Unix seconds:

```csharp
public sealed class UnixSecondsConverter : BencodeConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8BencodeReader reader, Type t, BencodeSerializerOptions o) =>
        DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64());

    public override void Write(Utf8BencodeWriter writer, DateTimeOffset value, BencodeSerializerOptions o) =>
        writer.WriteInteger(value.ToUnixTimeSeconds());
}

public sealed class Stamped
{
    [BencodeConverter(typeof(UnixSecondsConverter))]
    public DateTimeOffset CreatedAt { get; set; }
}

byte[] bytes = BencodeSerializer.Serialize(
    new Stamped { CreatedAt = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000) });
// → d9:CreatedAti1700000000ee
```

Register a converter on a member or type with `[BencodeConverter(typeof(…))]` as above, or for every occurrence via `options.Converters.Add(new UnixSecondsConverter())`. The full recipe, including converter factories for type families, is in [Writing converters](converters.md).

## Pattern 7 — Use a document model instead of a type

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

A deserialized `BencodeDocument` is caller-owned — dispose it (the `using` above) to return its pooled buffer. Elements obtained through the serializer's `object` mapping need no disposal.

## Pattern 8 — Streams and async

Both directions work over a `Stream`, synchronously and asynchronously, so a payload never has to materialize as a `byte[]` first:

```csharp
await using FileStream output = File.Create("ubuntu.torrent");
await BencodeSerializer.SerializeAsync(output, torrent, cancellationToken: ct);
```

```csharp
await using FileStream input = File.OpenRead("ubuntu.torrent");
Torrent torrent = await BencodeSerializer.DeserializeAsync<Torrent>(input, cancellationToken: ct);
```

The synchronous `Serialize(Stream, …)` / `Deserialize<T>(Stream, …)` overloads have the same shape without the token. Both async members accept an optional `BencodeSerializerOptions` before the `CancellationToken`.

## Pattern 9 — Process tokens by hand

For full control with no allocations, drive the <xref:Bodu.Text.Bencode.Reader.Utf8BencodeReader> / <xref:Bodu.Text.Bencode.Writer.Utf8BencodeWriter> ref-struct pair directly. The reader accepts only canonical BEP 3 (no leading or negative zeros, ascending unique dictionary keys, a single root with no trailing bytes). This is the same surface a [converter](converters.md) receives.

## Error handling

Two exception types separate "the bytes are not Bencode" from "the Bencode does not fit your type":

- <xref:Bodu.Text.Bencode.BencodeFormatException> — malformed input: truncated data, non-canonical integers, out-of-order or duplicate dictionary keys, trailing bytes. Carries the byte `Offset` where parsing failed.
- <xref:Bodu.Text.Bencode.BencodeSerializationException> — the document parsed, but a value cannot bind: a kind mismatch, a missing required member, or a value out of range for the target type.

```csharp
try
{
    TorrentInfo info = BencodeSerializer.Deserialize<TorrentInfo>(untrustedBytes);
}
catch (BencodeFormatException ex)
{
    // Not valid Bencode. ex.Offset is the failing byte position.
    log.Warn($"Malformed payload at offset {ex.Offset}: {ex.Message}");
}
catch (BencodeSerializationException ex)
{
    // Valid Bencode that does not match the model,
    // e.g. "Expected an integer but found 'ByteString'."
    log.Warn($"Payload does not bind: {ex.Message}");
}
```

`TryParse`-style members do not exist on the serializer; wrap `Deserialize` as above when reading untrusted input.

## See also

- [Using TOML](toml.md) — the twin library; every pattern above transfers with the `Toml` prefix.
- [Mapping attributes](attributes.md), [Writing converters](converters.md), [Serialization callbacks](callbacks.md), [Built-in converter catalog](builtin-converters.md) — the customization guides.
- [Bodu.Text.Bencode introduction](../../docs/serialization/bencode.md) — what is specific to the Bencode format, including the canonical-output guarantees.
- [Bodu serializers introduction](../../docs/serialization/index.md) and [core concepts](../../docs/serialization/concepts.md) — the family shape and vocabulary.
- [Text & Serialization guides](../topics/text-and-serialization.md) and the [topic overview](../../docs/topics/text-and-serialization.md).
- API reference — <xref:Bodu.Text.Bencode.BencodeSerializer>, <xref:Bodu.Text.Bencode.BencodeSerializerOptions>, <xref:Bodu.Text.Bencode.Nodes.BencodeNode>, <xref:Bodu.Text.Bencode.Document.BencodeDocument>.
