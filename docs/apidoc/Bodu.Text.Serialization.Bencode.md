---
uid: Bodu.Text.Serialization.Bencode
---

![Bodu.Text.Serialization.Bencode](~/images/hero-serialization.svg)

## Purpose

**Bodu.Text.Serialization.Bencode** is the Bencode object-mapper of the Bodu suite: a [`System.Text.Json`](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializer)-style serializer that binds your types to and from [Bencode (BEP 3)](https://www.bittorrent.org/beps/bep_0003.html), the binary encoding used by BitTorrent. It builds on the shared <xref:Bodu.Text.Serialization> engine — converters, options, naming policies, and attributes all apply.

Strings and byte arrays map to byte strings, integers to `i…e`, collections to lists, and objects and string-keyed dictionaries to dictionaries with keys in canonical bytewise order. Parsing produces a lossless concrete syntax tree (<xref:Bodu.Text.Serialization.Bencode.Syntax.BencodeSyntaxTree>) whose `ToByteArray()` reproduces the source bytes exactly. Booleans, floating-point numbers, and date-times have no Bencode representation and are rejected unless a converter maps them.

For the document-model Bencode codec (parse to a `BencodedValue` rather than your own types), see <xref:Bodu.Text.Bencode> in the **Bodu.Text.Formats** package.

## Static documentation

- **[Using Bencode](~/guides/serialization/bencode.md)** — type mapping, canonical ordering, and unsupported kinds.
- **[Bodu.Text.Serialization introduction](~/docs/serialization/index.md)** and **[core concepts](~/docs/serialization/concepts.md)**.

## Key types

- <xref:Bodu.Text.Serialization.Bencode.BencodeSerializer> — static façade. `Serialize` to `byte[]` / `IBufferWriter<byte>` / `Stream` and `Deserialize<T>` from `ReadOnlySpan<byte>` / `byte[]` / `Stream`, sync and async.
- <xref:Bodu.Text.Serialization.Bencode.BencodeSerializerOptions> — extends <xref:Bodu.Text.Serialization.FormatSerializerOptions>.
- <xref:Bodu.Text.Serialization.Bencode.Syntax.BencodeSyntaxTree> — the lossless parse entry point.
- <xref:Bodu.Text.Serialization.Bencode.Syntax.BencodeDocumentSyntax> — the document model.
- <xref:Bodu.Text.Serialization.Bencode.BencodeFormatException> — a parse failure with the byte offset.
