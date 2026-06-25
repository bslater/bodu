---
title: Bodu.Text.Yaml — Introduction
---

# Bodu.Text.Yaml

**Bodu.Text.Yaml** is a self-contained library for [YAML](https://yaml.org/) 1.1 and 1.2, the indentation-based, human-readable data format used across configuration, CI pipelines, and infrastructure tooling. It shares its `System.Text.Json`-aligned shape with the [Bodu serializer family](index.md) — the serializer / DOM / reader-writer tiers, naming policies, and the `YamlConverter<T>` extension point — so most of the [family introduction](index.md) applies here unchanged. This page covers what is *specific* to YAML.

## The format in one paragraph

YAML expresses the same node model as JSON — scalars, sequences, and mappings — but recovers structure from indentation rather than brackets, and adds presentation richness on top: block and flow styles, single-, double-, and block-quoted scalars, comments, anchors and aliases for sharing nodes, merge keys for combining mappings, tags for explicit typing, and multi-document streams. Any node may sit at the document root, so unlike TOML a top-level scalar or sequence is valid.

## Implicit typing and the Norway problem

YAML resolves the type of an *unquoted* scalar from its text, and the rules differ between spec versions. Bodu.Text.Yaml defaults to the **1.2 core schema**, which recognises only `true` / `false` as Booleans and `null` / `~` / the empty scalar as null. This avoids the well-known "Norway problem", where YAML 1.1 silently reads unquoted `no`, `yes`, `on`, and `off` as Booleans — so a country code `NO` stays the string `"NO"`. Setting <xref:Bodu.Text.Yaml.YamlSpecVersion> to `V1_1` on the options opts in to the broader 1.1 rules (the extra Boolean spellings, binary and underscored numbers, and sexagesimal forms where supported). Quoting always forces a string, so `id: "007"` is the string `007` under either version.

## Native YAML features

The reader and both document models implement the full grammar:

- **Anchors and aliases** — `&name` marks a node and `*name` references it; aliases resolve to the anchored node's value in the document model.
- **Merge keys** — `<<: *defaults` (or a sequence of mappings) imports keys into a mapping, with explicit keys and earlier sources taking precedence.
- **Tags** — the core `!!str` / `!!int` / `!!float` / `!!bool` / `!!null` tags coerce a scalar's resolved type.
- **Scalar styles** — plain, single-quoted, double-quoted (with the full backslash-escape set), and the literal (`|`) and folded (`>`) block styles with chomping (`-` / `+`) and explicit indentation indicators.
- **Multi-document streams** — documents separated by `---` (and optional `...` end markers) are read together with <xref:Bodu.Text.Yaml.Document.YamlDocument.ParseAllDocuments*>.

## Diagnostics with positions

YAML files are edited by hand and are sensitive to indentation, so parse failures must point at the offending line. A malformed document raises <xref:Bodu.Text.Yaml.YamlFormatException> carrying the **line, column, and byte offset**; a document that parses but cannot bind to your type raises <xref:Bodu.Text.Yaml.YamlSerializationException>.

## Headline types

| Type | Purpose |
|---|---|
| <xref:Bodu.Text.Yaml.YamlSerializer> | `Serialize` to a `string`; `Deserialize<T>` from a `string` or `ReadOnlySpan<byte>` (UTF-8). |
| <xref:Bodu.Text.Yaml.YamlSerializerOptions> | Converters, naming policy, `IncludeFields`, `IgnoreNullValues`, `WriteEnumsAsStrings`, `PropertyNameCaseInsensitive`, `SpecVersion`. |
| <xref:Bodu.Text.Yaml.Serialization.YamlConverter`1> | Base class for a custom converter over the reader/writer pair. |
| <xref:Bodu.Text.Yaml.Nodes.YamlNode> | Mutable DOM — `Parse`, index, mutate, `ToYamlString`. |
| <xref:Bodu.Text.Yaml.Document.YamlDocument> | Read-only, low-allocation DOM walked through `RootElement`; `ParseAllDocuments` for streams. |
| <xref:Bodu.Text.Yaml.Reader.Utf8YamlReader> / <xref:Bodu.Text.Yaml.Writer.Utf8YamlWriter> | Forward-only `ref struct` token machines. |
| <xref:Bodu.Text.Yaml.YamlFormatException> / <xref:Bodu.Text.Yaml.YamlSerializationException> | Malformed input (with line/column/offset) vs a value that cannot bind. |

## Common scenarios

| You want to… | Use |
|---|---|
| Load a `.yaml` configuration into a typed record | `YamlSerializer.Deserialize<T>` |
| Emit readable, block-style YAML | `YamlSerializer.Serialize` |
| Patch one value in an existing document without a model | <xref:Bodu.Text.Yaml.Nodes.YamlNode> — index, mutate, `ToYamlString` |
| Inspect a document with minimal allocation | <xref:Bodu.Text.Yaml.Document.YamlDocument> and `RootElement` |
| Read every document in a `---`-separated stream | <xref:Bodu.Text.Yaml.Document.YamlDocument.ParseAllDocuments*> |
| Accept legacy 1.1 typing (`yes`/`no` Booleans) | `SpecVersion = YamlSpecVersion.V1_1` on the options |

## What is not yet here

Compared with the fuller <xref:Bodu.Text.Toml> serializer, the YAML serializer ships a deliberate core subset: it has `YamlConverter<T>`, `[YamlPropertyName]`, `[YamlIgnore]`, and naming policies, but **not** serialization callbacks, the wider attribute family (property order, required, extension data, …), `Stream`/async overloads, or a source-generated AOT path. Mapping is reflection-based today.

## Where to go next

- **[Bodu serializers introduction](index.md)** — the shared shape: tiers, converters, naming policies.
- **[Getting started](getting-started.md)** — install and the first round trip.
- **[Using YAML](../../guides/serialization/yaml.md)** — worked patterns: type mapping, spec-version selection, anchors and merge keys, both DOMs, raw tokens.
- **API reference** — <xref:Bodu.Text.Yaml>.
