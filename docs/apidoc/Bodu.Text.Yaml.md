---
uid: Bodu.Text.Yaml
---

![Bodu.Text.Yaml](~/images/hero-yaml.svg)

## Purpose

**Bodu.Text.Yaml** is a self-contained [YAML](https://yaml.org/) library for .NET 8. It maps plain CLR objects to and from YAML through a configurable converter model, over a buffered token reader and a forward-only writer, with both a mutable and a read-only document object model.

The library is the third member of the [Bodu serializer family](~/docs/serialization/index.md), alongside <xref:Bodu.Text.Toml> and <xref:Bodu.Text.Bencode>. It shares the family's architecture — a static serializer façade, a low-level reader/writer pair, a mutable DOM, and a read-only DOM — but tunes the serializer surface to YAML and exposes YAML's richer presentation model. The types are organised into folders/namespaces by surface (`Reader`, `Writer`, `Document`, `Nodes`, `Serialization`).

Bodu.Text.Yaml implements the **Bodu YAML Core Tree Profile**: a YAML 1.2 core-schema, JSON-compatible tree model. Mapping keys resolve to unique scalar strings, anchors are unique and acyclic, and tabs are rejected as indentation. It supports block and flow collections, quoted and block scalars, comments, anchors and aliases, opt-in YAML 1.1 merge keys, core tags, and multi-document streams. The reader is **buffered** — it parses into an in-memory node store rather than scanning in a single pass. For EditorConfig-style INI configuration, see <xref:Bodu.Text.Ini>; for binary-to-text encodings (Base16 / Base32 / Base64 / Base58 / Base85), see the companion <xref:Bodu.Text.Encoding> package.

## Static documentation

- **[Bodu serializers introduction](~/docs/serialization/index.md)** — the three libraries, the shared tiers, and how to choose a format.
- **[Bodu.Text.Yaml introduction](~/docs/serialization/yaml/index.md)** — what is specific to YAML: the value model, presentation, multi-document streams, and diagnostics.
- **[Core concepts](~/docs/serialization/yaml/concepts.md)** — the serializer, the converter model, the two DOMs, and the reader/writer seam.
- **[Getting started](~/docs/serialization/yaml/getting-started.md)** — install and the first round trip.
- **[Using YAML](~/guides/serialization/yaml/using.md)** — type mapping, spec-version selection, the DOMs, and multi-document streams.
- **[Writing converters](~/guides/serialization/yaml/converters.md)** — custom shapes with `YamlConverter<T>`.

## Key types

**Serializer (`Bodu.Text.Yaml`)**

- <xref:Bodu.Text.Yaml.YamlSerializer> — static façade. `Serialize` to a `string` (from a typed value or an `object` + `Type`) and `Deserialize<T>` from a `string` or `ReadOnlySpan<byte>` (UTF-8).
- <xref:Bodu.Text.Yaml.YamlSerializerOptions> — naming policy, converters, `IncludeFields`, `IgnoreNullValues`, `WriteEnumsAsStrings`, `PropertyNameCaseInsensitive`, `SpecVersion`, `NumberHandling`, `DuplicateKeyBehavior`, `MergeKeyBehavior`, `UnmappedMemberHandling`, and `MaxDepth`; cached and frozen on first use.
- <xref:Bodu.Text.Serialization.NamingPolicy> — camel, lower-snake, and lower-kebab casing policies.
- <xref:Bodu.Text.Yaml.YamlTokenType>, <xref:Bodu.Text.Yaml.YamlValueKind> — the token and value-kind enumerations.
- <xref:Bodu.Text.Yaml.YamlSpecVersion> — the spec selector: `V1_2` (default core schema) or `V1_1` (adds `yes`/`no`/`on`/`off` booleans and sexagesimal numbers). <xref:Bodu.Text.Yaml.YamlNumberHandling> — float-to-integer coercion. <xref:Bodu.Text.Yaml.YamlScalarStyle> — the plain / quoted / literal / folded scalar styles. <xref:Bodu.Text.Yaml.YamlBlockChomping> — block-scalar trailing-newline handling. <xref:Bodu.Text.Yaml.YamlDuplicateKeyBehavior>, <xref:Bodu.Text.Yaml.YamlMergeKeyBehavior>, <xref:Bodu.Text.Serialization.UnmappedMemberHandling> — mapping-key policies.
- <xref:Bodu.Text.Yaml.YamlFormatException> — malformed input (with line / column / offset). <xref:Bodu.Text.Yaml.YamlSerializationException> — binding failures (with offset and member path).

**Low-level reader / writer**

- <xref:Bodu.Text.Yaml.Reader.Utf8YamlReader> (+ <xref:Bodu.Text.Yaml.Reader.YamlReaderOptions>) — forward-only, buffered token reader over parsed YAML.
- <xref:Bodu.Text.Yaml.Writer.Utf8YamlWriter> (+ <xref:Bodu.Text.Yaml.Writer.YamlWriterOptions>) — forward-only token writer; emits block-style YAML with configurable indentation.

**Document object models**

- <xref:Bodu.Text.Yaml.Nodes.YamlNode> / <xref:Bodu.Text.Yaml.Nodes.YamlObject> / <xref:Bodu.Text.Yaml.Nodes.YamlArray> / <xref:Bodu.Text.Yaml.Nodes.YamlValue> — the mutable, editable DOM.
- <xref:Bodu.Text.Yaml.Document.YamlDocument> / <xref:Bodu.Text.Yaml.Document.YamlElement> / <xref:Bodu.Text.Yaml.Document.YamlProperty> — the read-only, low-allocation DOM; `ParseAllDocuments` returns every document in a multi-document stream (parsing tuned by <xref:Bodu.Text.Yaml.Document.YamlDocumentOptions>).

**Converters and attributes (`Bodu.Text.Yaml.Serialization`)**

- <xref:Bodu.Text.Yaml.Serialization.YamlConverter`1> / <xref:Bodu.Text.Yaml.Serialization.YamlConverter> — base types for custom per-type converters; a converter reads a <xref:Bodu.Text.Yaml.Document.YamlElement> and writes through the <xref:Bodu.Text.Yaml.Writer.Utf8YamlWriter>.
- <xref:Bodu.Text.Serialization.PropertyNameAttribute>, <xref:Bodu.Text.Serialization.IgnoreAttribute> — the declarative member-shaping attributes.

## Example

```csharp
using Bodu.Text.Yaml;

public sealed class ServerConfig
{
    public string Host { get; set; } = "";
    public int Port { get; set; }
}

string yaml = YamlSerializer.Serialize(new ServerConfig { Host = "localhost", Port = 8080 });
ServerConfig config = YamlSerializer.Deserialize<ServerConfig>(yaml)!;

// Edit a document without a model:
using Bodu.Text.Yaml.Nodes;
YamlNode node = YamlNode.Parse(yamlText)!;
node["server"]!["port"] = YamlValue.Create(9090);
string back = node.ToYamlString();
```

## Notes

- **Self-contained.** The library has no shared engine dependency — everything the serializer needs lives in this assembly, mirroring its siblings <xref:Bodu.Text.Toml> and <xref:Bodu.Text.Bencode>.
- **Tuned serializer surface.** Member shaping is covered by the naming policies, the `[YamlPropertyName]` / `[YamlIgnore]` attributes, the options flags, and custom `YamlConverter<T>` converters. There is no converter-attribute, callback-interface, or converter-factory surface — those features have no equivalent here.
- **Value mapping.** `string` / `char` / `Guid` / `Uri` and the integer family → string or integer scalars, `double` / `float` → float scalars, `bool` → boolean, `null` → the null scalar; enums → member-name strings (or integers when `WriteEnumsAsStrings` is `false`). Collections map to sequences; dictionaries and objects map to mappings in insertion order. An `object`-typed member reads back as a <xref:Bodu.Text.Yaml.Document.YamlElement>. Public fields participate via `IncludeFields`.
- **Presentation is resolved, not stored.** Scalar style (plain / quoted / literal / folded), block vs. flow layout, and anchors and aliases are handled by the reader and chosen by the writer rather than surfaced as distinct value kinds; <xref:Bodu.Text.Yaml.Document.YamlElement.ScalarStyle> records the original scalar style.
- **Spec-version selection.** Parsing defaults to the strict **1.2 core schema** (only `true`/`false` are booleans); setting `SpecVersion` to `V1_1` additionally accepts `yes`/`no`/`on`/`off` booleans and sexagesimal numbers, and the `%YAML` directive overrides typing per document.
- **Multi-document streams.** `YamlDocument.ParseAllDocuments` returns every document delimited by `---` / `...`; the single-document methods read the first.
- **Errors.** Malformed input surfaces through <xref:Bodu.Text.Yaml.YamlFormatException> (with line, column, and offset); binding failures through <xref:Bodu.Text.Yaml.YamlSerializationException> (with offset and a dotted member path).
- **See also:** the [introduction](~/docs/serialization/index.md), [core concepts](~/docs/serialization/yaml/concepts.md), and [getting-started](~/docs/serialization/yaml/getting-started.md); the [Using YAML](~/guides/serialization/yaml/using.md) and [writing converters](~/guides/serialization/yaml/converters.md) guides; and the sibling [TOML](xref:Bodu.Text.Toml) and [Bencode](xref:Bodu.Text.Bencode) libraries.
