---
uid: Bodu.Text.Yaml
---

![Bodu.Text.Yaml](~/images/hero-yaml.svg)

## Purpose

**Bodu.Text.Yaml** is a self-contained [YAML](https://yaml.org/) (1.1 / 1.2) library for .NET 8. It maps plain CLR objects to and from YAML through a configurable converter model, over a low-level forward-only token reader and writer, with both a mutable and a read-only document object model.

The public surface layers four tiers: a static <xref:Bodu.Text.Yaml.YamlSerializer> for object mapping, the <xref:Bodu.Text.Yaml.Reader.Utf8YamlReader> / <xref:Bodu.Text.Yaml.Writer.Utf8YamlWriter> `ref struct` pair for forward-only token processing, a mutable <xref:Bodu.Text.Yaml.Nodes.YamlNode> DOM, and a read-only <xref:Bodu.Text.Yaml.Document.YamlDocument> DOM. It shares this `System.Text.Json`-aligned shape with the sibling <xref:Bodu.Text.Toml> and <xref:Bodu.Text.Bencode> libraries.

The reader defaults to the **YAML 1.2 core schema** — so unquoted `no` / `yes` resolve to strings rather than Booleans (avoiding the "Norway problem") — and opts in to **1.1** implicit typing through the <xref:Bodu.Text.Yaml.YamlSpecVersion> selector on the options. For TOML configuration see <xref:Bodu.Text.Toml>; for binary-to-text encodings see the companion <xref:Bodu.Text.Encoding> package.

## Static documentation

- **[Bodu serializers introduction](~/docs/serialization/index.md)** — the family, the three tiers, scenarios.
- **[Bodu.Text.Yaml introduction](~/docs/serialization/yaml.md)** — implicit typing, native features, positioned diagnostics.
- **[Getting started](~/docs/serialization/getting-started.md)** — install and the first round trip.
- **[Using YAML](~/guides/serialization/yaml.md)** — type mapping, spec-version selection, anchors and merge keys, the DOMs, and raw tokens.
- **[Writing converters](~/guides/serialization/converters.md)** — custom shapes with `YamlConverter<T>`.

## Key types

**Serializer (`Bodu.Text.Yaml`)**

- <xref:Bodu.Text.Yaml.YamlSerializer> — static façade. `Serialize` to a `string`; `Deserialize<T>` from a `string` or `ReadOnlySpan<byte>` (UTF-8).
- <xref:Bodu.Text.Yaml.YamlSerializerOptions> — converters, naming policy, `IncludeFields`, `IgnoreNullValues`, `WriteEnumsAsStrings`, `PropertyNameCaseInsensitive`, and `SpecVersion`.
- <xref:Bodu.Text.Yaml.YamlNamingPolicy> — camel, snake, and kebab casing policies.
- <xref:Bodu.Text.Yaml.YamlTokenType>, <xref:Bodu.Text.Yaml.YamlValueKind>, <xref:Bodu.Text.Yaml.YamlScalarStyle>, <xref:Bodu.Text.Yaml.YamlBlockChomping> — the token, value-kind, and scalar-presentation enumerations.
- <xref:Bodu.Text.Yaml.YamlSpecVersion> — the schema selector: `V1_2` (default) or `V1_1`.
- <xref:Bodu.Text.Yaml.YamlFormatException> — malformed input (with line / column / offset). <xref:Bodu.Text.Yaml.YamlSerializationException> — binding failures.

**Low-level reader / writer**

- <xref:Bodu.Text.Yaml.Reader.Utf8YamlReader> (+ <xref:Bodu.Text.Yaml.Reader.YamlReaderOptions>) — forward-only token reader over a buffered node store.
- <xref:Bodu.Text.Yaml.Writer.Utf8YamlWriter> (+ <xref:Bodu.Text.Yaml.Writer.YamlWriterOptions>) — forward-only token writer; emits block-style YAML.

**Document object models**

- <xref:Bodu.Text.Yaml.Nodes.YamlNode> / <xref:Bodu.Text.Yaml.Nodes.YamlObject> / <xref:Bodu.Text.Yaml.Nodes.YamlArray> / <xref:Bodu.Text.Yaml.Nodes.YamlValue> — the mutable, editable DOM.
- <xref:Bodu.Text.Yaml.Document.YamlDocument> / <xref:Bodu.Text.Yaml.Document.YamlElement> / <xref:Bodu.Text.Yaml.Document.YamlProperty> — the read-only, low-allocation DOM, with `ParseAllDocuments` for multi-document streams.

**Converters and attributes (`Bodu.Text.Yaml.Serialization`)**

- <xref:Bodu.Text.Yaml.Serialization.YamlConverter`1> — base type for a custom per-type converter.
- <xref:Bodu.Text.Yaml.Serialization.YamlPropertyNameAttribute>, <xref:Bodu.Text.Yaml.Serialization.YamlIgnoreAttribute> — per-member key override and exclusion.

## Example

```csharp
using Bodu.Text.Yaml;

public sealed class ServerConfig
{
    public string Host { get; set; } = "";
    public int Port { get; set; }
}

string yaml = YamlSerializer.Serialize(new ServerConfig { Host = "localhost", Port = 8080 });
ServerConfig config = YamlSerializer.Deserialize<ServerConfig>(yaml);

// Edit a document without a model:
using Bodu.Text.Yaml.Nodes;
YamlNode node = YamlNode.Parse(yaml)!;
node["Port"] = YamlValue.Create(9090L);
string back = node.ToYamlString();
```

## Notes

- **Core serializer surface.** The serializer maps POCOs, the built-in scalar types, enums, `Nullable<T>`, collections, and string-keyed dictionaries through a reflection-based engine, with <xref:Bodu.Text.Yaml.Serialization.YamlConverter`1> as the extension point and the `[YamlPropertyName]` / `[YamlIgnore]` attributes plus naming policies for shaping. Serialization callbacks, the wider attribute family, `Stream`/async overloads, and a source-generated AOT path are not yet provided.
- **Full reader grammar.** The reader and both document models implement anchors and aliases, merge keys (`<<`), core tags, block and flow styles, block scalars with chomping and indentation indicators, and multi-document streams.
- **Implicit typing.** Resolution defaults to the YAML 1.2 core schema; setting `SpecVersion` to `V1_1` opts in to the 1.1 rules (the `yes`/`no`/`on`/`off` Booleans and the additional number forms). Quoting always forces a string.
- **Value mapping.** `string` / `char` / `Guid` → string; the integer family → integer; `double` / `float` / `decimal` → float (incl. `.inf` / `.nan`); `bool` → boolean; `DateTime` / `DateTimeOffset` / `TimeSpan` → string; enums → member-name strings (or numbers via `WriteEnumsAsStrings`); collections → sequences; objects and string-keyed dictionaries → mappings. A string that resolves to another type is quoted so it round-trips. `null` maps to the null scalar, or is omitted when `IgnoreNullValues` is set.
- **Errors.** Malformed input surfaces through <xref:Bodu.Text.Yaml.YamlFormatException> (with line, column, and offset); binding failures through <xref:Bodu.Text.Yaml.YamlSerializationException>.
- **See also:** the [introduction](~/docs/serialization/index.md) and [getting-started](~/docs/serialization/getting-started.md); the [Using YAML](~/guides/serialization/yaml.md) guide; and the sibling [TOML](xref:Bodu.Text.Toml) and [Bencode](xref:Bodu.Text.Bencode) libraries.
