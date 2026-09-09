---
uid: Bodu.Text.Bencode.Document
---

![Bodu.Text.Bencode.Document](~/images/hero-bencode.svg)

## Purpose

**Bodu.Text.Bencode.Document** is the read-only document object model of <xref:Bodu.Text.Bencode>, shaped after `System.Text.Json`'s `JsonDocument` / `JsonElement`. A parse produces a single disposable <xref:Bodu.Text.Bencode.Document.BencodeDocument> that indexes the input bytes once; every <xref:Bodu.Text.Bencode.Document.BencodeElement> is a lightweight `struct` cursor into that index. Prefer it over the mutable <xref:Bodu.Text.Bencode.Nodes> tier when you only need to inspect a payload, and note that an `object`-typed member deserialized by the <xref:Bodu.Text.Bencode.BencodeSerializer> reads back as a `BencodeElement`.

## Key types

- <xref:Bodu.Text.Bencode.Document.BencodeDocument> — the disposable owner: `Parse` (from `byte[]` or `ReadOnlySpan<byte>`), `RootElement`, `WriteTo`.
- <xref:Bodu.Text.Bencode.Document.BencodeElement> — the value cursor: `ValueKind`, `GetString` / `GetBytes` / `GetRawBytes` / `GetInt64` / `GetUInt64` (+ `TryGet*`), `GetProperty` / `TryGetProperty`, an integer indexer, `GetArrayLength`, `EnumerateArray` / `EnumerateObject`, `Clone`, and `WriteTo`.
- <xref:Bodu.Text.Bencode.Document.BencodeElement.ArrayEnumerator> / <xref:Bodu.Text.Bencode.Document.BencodeElement.ObjectEnumerator> — the struct enumerators returned by `EnumerateArray` / `EnumerateObject`.
- <xref:Bodu.Text.Bencode.Document.BencodeProperty> — a `Name` / `Value` pair yielded by `EnumerateObject`.
- <xref:Bodu.Text.Bencode.Document.BencodeDocumentOptions> — `MaxDepth`, `AllowUnsortedKeys`, `AllowDuplicateKeys`.

## Example

```csharp
using Bodu.Text.Bencode.Document;

using BencodeDocument document = BencodeDocument.Parse("d6:lengthi1024e4:name10:ubuntu.isoe"u8);
BencodeElement root = document.RootElement;

long length = root.GetProperty("length").GetInt64();
foreach (BencodeProperty property in root.EnumerateObject())
    Console.WriteLine($"{property.Name}: {property.Value.ValueKind}");
```

## Notes

- **Lifetime.** Elements are only valid while their document is undisposed; call `Clone` to detach one.
- **Integers beyond `long`.** A value in the `ulong` range reads through `GetUInt64` / `TryGetUInt64`.
- **See also:** the [Bodu.Text.Bencode introduction](~/docs/serialization/bencode/index.md) and the [Using Bencode](~/guides/serialization/bencode/using.md) guide (Pattern 7 — Use a document model instead of a type).
