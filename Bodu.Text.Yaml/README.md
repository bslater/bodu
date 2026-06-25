# Bodu.Text.Yaml

A YAML (1.1 / 1.2) library for .NET 8. It maps plain CLR objects to and from YAML through a configurable converter model, over a low-level forward-only token reader and writer, with both a mutable and a read-only document object model. It defaults to the YAML 1.2 core schema — so unquoted `no` / `yes` stay strings, not Booleans — and opts in to 1.1 implicit typing on demand.

## Installation

```shell
dotnet add package Bodu.Text.Yaml
```

Targets `net8.0`.

## API shape

The public surface follows the sibling `Bodu.Text.Toml` and `Bodu.Text.Bencode` libraries, so the patterns transfer directly. The types are organised into folders/namespaces by surface:

| Type(s) | Namespace | Role |
|---|---|---|
| `YamlSerializer` / `YamlSerializerOptions` | `Bodu.Text.Yaml` | Static serializer entry point and its configuration (naming policy, fields, null handling, enum form, case-insensitivity, spec version, converters). |
| `YamlNamingPolicy`, `YamlTokenType`, `YamlValueKind`, `YamlSpecVersion`, `YamlScalarStyle`, `YamlBlockChomping` | `Bodu.Text.Yaml` | Property naming policies, token and value-kind classification, the 1.1/1.2 selector, and scalar-presentation enums. |
| `YamlFormatException` / `YamlSerializationException` | `Bodu.Text.Yaml` | Failures split by cause: malformed input (with line/column/offset) vs. values that cannot be mapped. |
| `Utf8YamlReader` (+ `YamlReaderOptions`) | `Bodu.Text.Yaml.Reader` | Forward-only `ref struct` token reader over a buffered node store. |
| `Utf8YamlWriter` (+ `YamlWriterOptions`) | `Bodu.Text.Yaml.Writer` | Forward-only `ref struct` block-style token writer. |
| `YamlDocument` / `YamlElement` / `YamlProperty` | `Bodu.Text.Yaml.Document` | Read-only, low-allocation document object model; also `ParseAllDocuments` for multi-document streams. |
| `YamlNode` / `YamlObject` / `YamlArray` / `YamlValue` | `Bodu.Text.Yaml.Nodes` | Mutable document object model: parse, edit, write back. |
| `YamlConverter<T>`, `[YamlPropertyName]` / `[YamlIgnore]` | `Bodu.Text.Yaml.Serialization` | Custom converters and per-member attributes. |

```csharp
using Bodu.Text.Yaml;

string yaml = YamlSerializer.Serialize(new ServerConfig { Host = "localhost", Port = 8080 });
ServerConfig config = YamlSerializer.Deserialize<ServerConfig>(yaml);

// Document object models
using Bodu.Text.Yaml.Nodes;
YamlNode node = YamlNode.Parse(yaml)!;
node["server"]!["port"] = YamlValue.Create(9090L);
string back = node.ToYamlString();
```

## Scope

The reader and both document models implement the full YAML grammar — anchors and aliases, merge keys (`<<`), tags, block and flow styles, block scalars with chomping and indentation indicators, and multi-document streams. The serializer maps the common CLR surface (POCOs, the built-in scalar types, enums, `Nullable<T>`, collections, and string-keyed dictionaries) through a reflection-based engine with `YamlConverter<T>` as the extension point. Serialization callbacks, the wider attribute family, and a source-generated AOT path are not yet provided.
