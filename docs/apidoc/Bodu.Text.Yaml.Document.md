---
uid: Bodu.Text.Yaml.Document
---

![Bodu.Text.Yaml.Document](~/images/hero-yaml.svg)

## Purpose

**Bodu.Text.Yaml.Document** is the read-only document object model of <xref:Bodu.Text.Yaml>, shaped after `System.Text.Json`'s `JsonDocument` / `JsonElement`. A parse produces a disposable <xref:Bodu.Text.Yaml.Document.YamlDocument> over a resolved node store; every <xref:Bodu.Text.Yaml.Document.YamlElement> is a lightweight `struct` cursor into it. It is also the home of multi-document stream parsing (`ParseAllDocuments`). Prefer it over the mutable <xref:Bodu.Text.Yaml.Nodes> tier for inspection; a member typed `YamlElement` binds through the <xref:Bodu.Text.Yaml.YamlSerializer>'s DOM bridge.

## Key types

- <xref:Bodu.Text.Yaml.Document.YamlDocument> — the disposable owner: `Parse` (from `string` or `ReadOnlySpan<byte>`), `ParseAllDocuments` for `---` / `...` delimited streams, and `RootElement`.
- <xref:Bodu.Text.Yaml.Document.YamlElement> — the value cursor: `ValueKind`, `ScalarStyle` (the original <xref:Bodu.Text.Yaml.YamlScalarStyle>), `GetString` / `GetInt64` / `GetDouble` / `GetBoolean`, `GetProperty` / `TryGetProperty`, an integer indexer, `GetSequenceLength`, `EnumerateSequence` / `EnumerateMapping`, and `WriteTo`.
- <xref:Bodu.Text.Yaml.Document.YamlElement.SequenceEnumerator> / <xref:Bodu.Text.Yaml.Document.YamlElement.MappingEnumerator> — the struct enumerators returned by `EnumerateSequence` / `EnumerateMapping`.
- <xref:Bodu.Text.Yaml.Document.YamlProperty> — a `Name` / `Value` pair yielded by `EnumerateMapping`.
- <xref:Bodu.Text.Yaml.Document.YamlDocumentOptions> — `SpecVersion`, `DuplicateKeyBehavior`, `MergeKeyBehavior`, and `MaxDepth`.

## Example

```csharp
using Bodu.Text.Yaml.Document;

using YamlDocument document = YamlDocument.Parse("host: localhost\nport: 8080\n");
YamlElement root = document.RootElement;

long port = root.GetProperty("port").GetInt64();
foreach (YamlProperty property in root.EnumerateMapping())
    Console.WriteLine($"{property.Name}: {property.Value.ValueKind}");
```

## Notes

- **Aliases are transparent.** An element reached through an alias resolves to its anchored target; the tree is guaranteed acyclic.
- **Multi-document streams.** The single-document `Parse` reads the first document; `ParseAllDocuments` returns every document, each independently disposable.
- **See also:** the [Bodu.Text.Yaml introduction](~/docs/serialization/yaml/index.md) and the [Using YAML](~/guides/serialization/yaml/using.md) guide (Pattern 5 — Inspect a document with the read-only DOM).
