---
uid: Bodu.Text.Yaml.Nodes
---

![Bodu.Text.Yaml.Nodes](~/images/hero-yaml.svg)

## Purpose

**Bodu.Text.Yaml.Nodes** is the mutable document object model of <xref:Bodu.Text.Yaml>, shaped after `System.Text.Json.Nodes`. Parse YAML text into an editable tree, build one from scratch, change it, and write it back through the <xref:Bodu.Text.Yaml.Writer.Utf8YamlWriter>. For read-only inspection with fewer allocations, use the sibling <xref:Bodu.Text.Yaml.Document> tier; for typed binding, use the <xref:Bodu.Text.Yaml.YamlSerializer>. A member typed `YamlNode` binds through the serializer's DOM bridge.

## Key types

- <xref:Bodu.Text.Yaml.Nodes.YamlNode> — abstract base: `Parse`, `AsObject` / `AsArray` / `AsValue`, string and integer indexers, `WriteTo`, and `ToYamlString`.
- <xref:Bodu.Text.Yaml.Nodes.YamlObject> — a mapping node keyed by string, with `Add` / `Remove` / `TryGetValue` and an indexer.
- <xref:Bodu.Text.Yaml.Nodes.YamlArray> — a sequence node with `Add` / `Remove` / `RemoveAt` and an integer indexer.
- <xref:Bodu.Text.Yaml.Nodes.YamlValue> — a scalar leaf (string, integer, float, or boolean), created via `Create` and read via `GetValue<T>`.

## Example

```csharp
using Bodu.Text.Yaml.Nodes;

YamlObject root = YamlNode.Parse("host: localhost\nport: 8080\n")!.AsObject();
root["port"] = YamlValue.Create(9090);
root.Add("tls", YamlValue.Create(true));

string yaml = root.ToYamlString();
```

## Notes

- **Explicit scalar creation.** Unlike the TOML and Bencode node DOMs there are no implicit conversions; wrap scalars with `YamlValue.Create` so the value kind is unambiguous under YAML's implicit typing.
- **Presentation is normalized.** Comments, anchors, aliases, and the original scalar styles are not retained; `ToYamlString` re-emits the tree in block style.
- **See also:** the [Bodu.Text.Yaml introduction](~/docs/serialization/yaml/index.md) and the [Using YAML](~/guides/serialization/yaml/using.md) guide (Pattern 4 — Edit a document with the mutable DOM).
