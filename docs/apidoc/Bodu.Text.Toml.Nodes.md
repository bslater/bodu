---
uid: Bodu.Text.Toml.Nodes
---

![Bodu.Text.Toml.Nodes](~/images/hero-toml.svg)

## Purpose

**Bodu.Text.Toml.Nodes** is the mutable document object model of <xref:Bodu.Text.Toml>, shaped after `System.Text.Json.Nodes`. Parse UTF-8 TOML into an editable tree, build one from scratch, change it, and write it back through the <xref:Bodu.Text.Toml.Writer.Utf8TomlWriter>. For read-only inspection with fewer allocations, use the sibling <xref:Bodu.Text.Toml.Document> tier; for typed binding, use the <xref:Bodu.Text.Toml.TomlSerializer>.

## Key types

- <xref:Bodu.Text.Toml.Nodes.TomlNode> — abstract base: `Parse`, `AsObject` / `AsArray` / `AsValue`, `GetValue<T>`, `GetValueKind`, `WriteTo`, `ToUtf8Bytes`, `DeepEquals`, string and integer indexers, and implicit conversions from every TOML scalar CLR type.
- <xref:Bodu.Text.Toml.Nodes.TomlObject> — a table node keyed by string, with `Add` / `Remove` / `TryGetValue` and an indexer.
- <xref:Bodu.Text.Toml.Nodes.TomlArray> — an array node implementing `IList<TomlNode?>`.
- <xref:Bodu.Text.Toml.Nodes.TomlValue> — a scalar leaf (string, integer, float, boolean, or one of the four date-time kinds), created via `Create` and read via `GetValue<T>` / `TryGetValue<T>`.
- <xref:Bodu.Text.Toml.Nodes.TomlNodeOptions> — parsing options for `Parse`.

## Example

```csharp
using Bodu.Text.Toml.Nodes;

TomlNode root = TomlNode.Parse("[server]\nhost = \"localhost\"\nport = 8080\n"u8)!;
root["server"]!["port"] = 9090;                 // implicit conversion from int
root["server"]!.AsObject().Add("tls", true);    // implicit conversion from bool

byte[] back = root.ToUtf8Bytes();
```

## Notes

- **Table root.** The root of a parsed or written tree is always a <xref:Bodu.Text.Toml.Nodes.TomlObject>; writing a scalar or array as the root throws.
- **Layout is normalized.** Comments and the original header/inline layout are not preserved; the writer re-emits the tree in canonical block form.
- **See also:** the [Bodu.Text.Toml introduction](~/docs/serialization/toml/index.md) and the [Using TOML](~/guides/serialization/toml/using.md) guide (Pattern 6 — Edit a document with the mutable DOM).
