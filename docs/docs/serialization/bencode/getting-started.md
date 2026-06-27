---
title: Bodu.Text.Bencode — Getting started
---

# Getting started

## Install

Add the package. It is self-contained — there is no shared engine package to add.

```shell
dotnet add package Bodu.Text.Bencode
```

It targets `net8.0`.

## A first Bencode round trip

```csharp
using Bodu.Text.Bencode;

public sealed class FileEntry
{
    public string Name { get; set; } = "";
    public long Length { get; set; }
}

byte[] payload = BencodeSerializer.Serialize(new FileEntry { Name = "ubuntu.iso", Length = 1024 });
// d6:Lengthi1024e4:Name10:ubuntu.isoe   (dictionary keys in canonical order)

FileEntry entry = BencodeSerializer.Deserialize<FileEntry>(payload);
```

## Rename members

```csharp
using Bodu.Text.Bencode;

var options = new BencodeSerializerOptions
{
    PropertyNamingPolicy = BencodeNamingPolicy.SnakeCaseLower,
};

// "Name" is written as "name", "Length" as "length".
byte[] payload = BencodeSerializer.Serialize(entry, options);
```

Or pin a single member's name with `[BencodePropertyName]`, which always wins over the policy.

## Edit a document without a model

When you need to change a value but do not want a POCO, parse to the **mutable DOM**:

```csharp
using Bodu.Text.Bencode.Nodes;

BencodeNode node = BencodeNode.Parse(payload)!;
node["Length"] = 2048;
byte[] back = node.ToByteArray();
```

## Read a document without a model

For inspection only, the **read-only DOM** is the lighter choice — a low-allocation view over the parsed buffer, walked through `RootElement`:

```csharp
using Bodu.Text.Bencode.Document;

using BencodeDocument doc = BencodeDocument.Parse(payload);

BencodeElement info = doc.RootElement.GetProperty("info");
string name   = info.GetProperty("name").GetString();        // "ubuntu.iso"
long   length = info.GetProperty("piece length").GetInt64(); // 262144
```

`BencodeDocument` is disposable — wrap it in `using` and copy out any values that must outlive it, since disposal returns its pooled buffer.

## Round-trip through a Stream

The serializer reads and writes `Stream` directly, with async variants, so a payload never has to materialize as a `byte[]` first:

```csharp
using Bodu.Text.Bencode;

await using (FileStream stream = File.Create("ubuntu.torrent"))
{
    await BencodeSerializer.SerializeAsync(stream, torrent);
}

await using (FileStream stream = File.OpenRead("ubuntu.torrent"))
{
    Torrent loaded = await BencodeSerializer.DeserializeAsync<Torrent>(stream);
}
```

The synchronous `Serialize(Stream, …)` / `Deserialize<T>(Stream, …)` overloads have the same shape without the token.

## When something goes wrong

Failures split into two exception types, so you can tell *bad input* apart from *wrong type*:

- A **malformed document** — bytes the grammar rejects — raises <xref:Bodu.Text.Bencode.BencodeFormatException>, which carries the byte `Offset` where parsing failed.
- A document that **parses but cannot bind** to your type — a type mismatch, a missing required member, a value the format cannot represent — raises <xref:Bodu.Text.Bencode.BencodeSerializationException>.

```csharp
try
{
    FileEntry loaded = BencodeSerializer.Deserialize<FileEntry>(payload);
}
catch (BencodeFormatException ex)
{
    Console.Error.WriteLine($"Malformed Bencode at byte {ex.Offset}: {ex.Message}");
}
catch (BencodeSerializationException ex)
{
    Console.Error.WriteLine($"Document does not match FileEntry: {ex.Message}");
}
```

## Where to go next

- **[Bodu.Text.Bencode introduction](index.md)** — what is specific to Bencode: byte strings, canonical output, the kinds it cannot represent.
- **[Core concepts](concepts.md)** — the serializer, converter model, both DOMs, and the reader/writer seam.
- **[Using Bencode](../../../guides/serialization/bencode/using.md)** — byte strings, canonical ordering, the DOMs, and unsupported kinds.
- **[Writing converters](../../../guides/serialization/bencode/converters.md)** — custom shapes with `BencodeConverter<T>`.
- **[Text & Serialization topic overview](../../topics/text-and-serialization.md)** — where the serializers sit among the codecs and document formats.
