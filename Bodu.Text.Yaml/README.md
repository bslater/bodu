# Bodu.Text.Yaml

A YAML library for .NET 8. It maps plain CLR objects to and from YAML through a configurable converter model, over a token reader and writer, with both a mutable and a read-only document object model. The public surface matches the sibling `Bodu.Text.Toml` and `Bodu.Text.Bencode` libraries, so the patterns transfer directly between them.

## Installation

```shell
dotnet add package Bodu.Text.Yaml
```

Targets `net8.0`.

## Conformance profile

Bodu.Text.Yaml implements a **YAML 1.2 core, JSON-compatible tree profile**. It is a predictable configuration- and document-mapping library, not a full YAML 1.2 graph processor. The profile is enforced: inputs that fall outside it are rejected with `YamlFormatException` rather than silently degraded.

**Supported**

- Block and flow sequences and mappings.
- Plain, single-quoted, double-quoted, literal (`|`), and folded (`>`) scalars, with chomping and indentation indicators.
- Comments, the `---` / `...` document markers, and multi-document streams.
- Implicit typing under the YAML 1.2 core schema by default (only `true`/`false` are booleans — no "Norway problem"), with opt-in YAML 1.1 typing via `SpecVersion` (`yes`/`no`, `on`/`off`, `y`/`n`, sexagesimal numbers).
- Anchors and aliases, subject to **acyclic** tree resolution.
- The core tags (`!!str`, `!!null`, `!!bool`, `!!int`, `!!float`) and `%TAG` handle expansion.
- The YAML 1.1 merge key (`<<`) as an **opt-in** compatibility feature (`YamlMergeKeyBehavior`).

**Rejected** (each throws `YamlFormatException`)

- **Complex (non-scalar) mapping keys** — keys must resolve to scalar strings. A sequence or mapping used as a key is rejected, not coerced.
- **Duplicate mapping keys** — by default (configurable via `YamlDuplicateKeyBehavior`).
- **Duplicate or cyclic anchors / aliases.**
- **Tabs used as indentation** (tabs remain legal as separation whitespace).
- **Invalid UTF-8, unpaired surrogates, invalid Unicode escapes, and non-printable control characters.**
- **Malformed or unsupported directives** and unknown non-core tags.

This profile aligns with the Bodu TOML / Bencode and `System.Text.Json` architectural family. Broader YAML graph support (complex keys, anchor identity, a streaming event reader, richer tag resolution) is a deliberate future extension, not a silent partial behavior of this release.

> **Reader note.** `Utf8YamlReader` exposes a forward-only token surface like `System.Text.Json.Utf8JsonReader`, but it is **buffered**: the constructor parses the whole document into an in-memory node store and `Read()` walks it. It is the analogue of the TOML library's `TomlDocumentReader` cursor, not the streaming `Utf8TomlReader` scanner — YAML's indentation context, back-referencing aliases, and merge keys cannot be resolved in a single forward pass.

## API shape

| Type(s) | Namespace | Role |
|---|---|---|
| `YamlSerializer` / `YamlSerializerOptions` | `Bodu.Text.Yaml` | Static serializer entry point and its configuration. |
| `YamlNamingPolicy`, `YamlTokenType`, `YamlValueKind`, `YamlSpecVersion` | `Bodu.Text.Yaml` | Naming policies, token/value classification, and the spec-version selector. |
| `YamlFormatException` / `YamlSerializationException` | `Bodu.Text.Yaml` | Failures split by cause: malformed input vs. values that cannot be mapped. |
| `Utf8YamlReader` (+ `YamlReaderOptions`) | `Bodu.Text.Yaml.Reader` | Buffered forward-only `ref struct` token reader. |
| `Utf8YamlWriter` (+ `YamlWriterOptions`) | `Bodu.Text.Yaml.Writer` | Forward-only `ref struct` token writer. |
| `YamlDocument` / `YamlElement` / `YamlProperty` | `Bodu.Text.Yaml.Document` | Read-only, low-allocation document object model. |
| `YamlNode` / `YamlObject` / `YamlArray` / `YamlValue` | `Bodu.Text.Yaml.Nodes` | Mutable document object model: parse, edit, write back. |
| `YamlConverter<T>`, `[YamlPropertyName]` / `[YamlIgnore]` / … | `Bodu.Text.Yaml.Serialization` | Custom converters and the per-member attribute family. |

```csharp
using Bodu.Text.Yaml;

string yaml = YamlSerializer.Serialize(new ServerConfig { Host = "localhost", Port = 8080 });
ServerConfig config = YamlSerializer.Deserialize<ServerConfig>(yaml);

// Document object models
using Bodu.Text.Yaml.Nodes;
YamlNode node = YamlNode.Parse(utf8Yaml)!;
node["server"]!["port"] = 9090;
byte[] back = node.ToUtf8Bytes();
```

- Failures surface through `YamlFormatException` (malformed input, with line/column/offset) and `YamlSerializationException` (binding failures, with the member path).

## Testing

Tests live in `test/` as MSTest classes mirroring `src/`. The Regression tier runs the vendored `yaml/yaml-test-suite` conformance corpus under a classification manifest. Run tiers via the runsettings files at the solution root:

```bash
dotnet test Bodu.Text.Yaml/test/Bodu.Text.Yaml.Test.csproj --settings bvt.runsettings
dotnet test Bodu.Text.Yaml/test/Bodu.Text.Yaml.Test.csproj --settings regression.runsettings
```

## License

MIT. © Bodu Pty. Ltd.
