---
uid: Bodu.Text.Bencode.Nodes
---

![Bodu.Text.Bencode.Nodes](~/images/hero-bencode.svg)

## Purpose

**Bodu.Text.Bencode.Nodes** is the mutable document object model of <xref:Bodu.Text.Bencode>, shaped after `System.Text.Json.Nodes`. Parse bytes into an editable tree, build one from scratch, change it, and write it back through the <xref:Bodu.Text.Bencode.Writer.Utf8BencodeWriter>. For read-only inspection with fewer allocations, use the sibling <xref:Bodu.Text.Bencode.Document> tier; for typed binding, use the <xref:Bodu.Text.Bencode.BencodeSerializer>.

## Key types

- <xref:Bodu.Text.Bencode.Nodes.BencodeNode> — abstract base: `Parse`, `AsObject` / `AsArray` / `AsValue`, `GetValue<T>`, `GetValueKind`, `WriteTo`, `ToByteArray`, `DeepEquals`, and the implicit conversions from `string`, integers, and `byte[]`.
- <xref:Bodu.Text.Bencode.Nodes.BencodeObject> — a dictionary node keyed by string, with `Add` / `Remove` / `TryGetValue` and an indexer.
- <xref:Bodu.Text.Bencode.Nodes.BencodeArray> — a list node implementing `IList<BencodeNode?>`.
- <xref:Bodu.Text.Bencode.Nodes.BencodeValue> — an integer or byte-string leaf, created via `Create` and read via `GetValue<T>` / `TryGetValue<T>`.
- <xref:Bodu.Text.Bencode.Nodes.BencodeNodeOptions> — parsing options for `Parse`.

## Example

```csharp
using Bodu.Text.Bencode.Nodes;

BencodeObject root = BencodeNode.Parse("d6:lengthi1024e4:name10:ubuntu.isoe"u8)!.AsObject();
root["length"] = 2048;               // implicit conversion from long
root.Add("comment", "mirror copy");  // implicit conversion from string

byte[] back = root.ToByteArray();    // keys re-emitted in canonical order
```

## Notes

- **Canonical on write.** Insertion order is kept in memory, but `WriteTo` / `ToByteArray` always emit dictionary keys in bytewise order.
- **Parent tracking.** A node belongs to at most one parent; adding an attached node elsewhere throws <xref:System.InvalidOperationException>.
- **See also:** the [Bodu.Text.Bencode introduction](~/docs/serialization/bencode/index.md) and the [Using Bencode](~/guides/serialization/bencode/using.md) guide (Pattern 7 — Use a document model instead of a type).
