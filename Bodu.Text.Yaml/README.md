# Bodu.Text.Yaml

> **API stability — Preview.** The public API surface is largely settled but is still being finalized ahead of the 1.0 release and may change; breaking changes can land in a minor version until then.

A YAML library for .NET 8. It maps plain CLR objects to and from YAML through a configurable converter model, over a token reader and writer, with both a mutable and a read-only document object model. The public surface matches the sibling `Bodu.Text.Toml` and `Bodu.Text.Bencode` libraries, so the patterns transfer directly between them.

## Installation

```shell
dotnet add package Bodu.Text.Yaml
```

Targets `net8.0`.

## Conformance profile

Bodu.Text.Yaml implements the **Bodu YAML Core Tree Profile**: a YAML 1.2 core-schema, JSON-compatible tree model for configuration data. It is a predictable configuration- and document-mapping library, not a full YAML 1.2 representation-graph processor. The profile is enforced — inputs that fall outside it are rejected with `YamlFormatException` rather than silently degraded — and the enforcement is gated by the vendored `yaml-test-suite` conformance corpus (see [Conformance corpus](#conformance-corpus)).

**Supported**

- Block and flow sequences and mappings.
- Plain, single-quoted, double-quoted, literal (`|`), and folded (`>`) scalars, with chomping and indentation indicators.
- Comments, the `---` / `...` document markers, and multi-document streams.
- Implicit typing under the YAML 1.2 core schema by default (only `true`/`false` are booleans — no "Norway problem"), with opt-in YAML 1.1 typing via `SpecVersion` (`yes`/`no`, `on`/`off`, `y`/`n`, sexagesimal numbers). A document's `%YAML` directive is honored for scalar resolution, overriding the configured `SpecVersion` for that document.
- Anchors and aliases, subject to **acyclic** tree resolution.
- The core tags (`!!str`, `!!null`, `!!bool`, `!!int`, `!!float`) and `%TAG` handle expansion. An explicit core tag whose content is invalid for the tag (for example `!!int abc`) is rejected, not silently degraded.
- The YAML 1.1 merge key (`<<`) as an **opt-in** compatibility feature (`YamlMergeKeyBehavior`, on the reader, document, and serializer options).

**Rejected** (each throws `YamlFormatException`)

- **Complex (non-scalar) mapping keys** — keys must resolve to scalar strings. A sequence or mapping used as a key is rejected, not coerced.
- **Duplicate mapping keys** — by default (configurable via `YamlDuplicateKeyBehavior`).
- **Duplicate / overriding anchors and cyclic aliases.** A repeated anchor name is rejected: anchor override is a YAML representation-graph feature outside this tree profile.
- **Tabs used as indentation** (tabs remain legal as separation whitespace).
- **Invalid UTF-8, unpaired surrogates, invalid Unicode escapes, and non-printable control characters.**
- **Malformed or unsupported directives** and **unknown non-core tags**.

This profile aligns with the Bodu TOML / Bencode and `System.Text.Json` architectural family. Broader YAML graph support (complex keys, anchor identity, a streaming event reader, richer tag resolution) is a deliberate future extension, not a silent partial behavior of this release.

### Compliance matrix

| Feature | Profile support | Behavior |
|---|---|---|
| Block / flow collections | Supported | Parsed to mappings and sequences. |
| Scalar styles (plain, quoted, literal, folded) | Supported | Decoded with chomping and indentation indicators. |
| Core-schema typing (`null`/`bool`/`int`/`float`/`str`) | Supported | YAML 1.2 core by default; YAML 1.1 via `SpecVersion` or a `%YAML 1.1` directive. |
| Anchors / aliases | Supported (acyclic) | Resolved into tree nodes; cycles and anchor override rejected. |
| Merge key `<<` | Opt-in | Controlled by `YamlMergeKeyBehavior` (expand by default). |
| Core tags + `%TAG` | Supported | Expanded and validated; invalid tagged content rejected. |
| Multi-document streams | Supported | `YamlDocument.ParseAllDocuments`. |
| Complex (non-scalar) keys | Rejected | `YamlFormatException`. |
| Duplicate mapping keys | Rejected by default | Configurable via `YamlDuplicateKeyBehavior`. |
| Anchor override, cyclic graphs | Rejected | Outside the tree profile. |
| Unknown / non-core tags | Rejected | Outside the core schema. |
| Tabs in indentation, invalid UTF-8, control chars | Rejected | Source validation. |

### Conformance corpus

The `yaml/yaml-test-suite` corpus is linked into the repository as the **`yaml-test-suite` git submodule** (under `Bodu.Text.Yaml/test/`, pinned to a released `data-YYYY-MM-DD` commit). Initialize it before running the Regression tier:

```shell
git submodule update --init Bodu.Text.Yaml/test/yaml-test-suite
```

The Regression test tier reads each upstream case through `YamlTestCorpusReader`, which walks the submodule directory tree and classifies each case in code into a `YamlTestVector` KAT — `SupportedPass`, `SupportedParseOnly`, `SupportedFail`, or `UnsupportedFeatureRejected`. Classification is derived from the case's own files (an `error` file marks an expected failure; an `in.json` marks a supported-valid case); the valid upstream cases the profile deliberately rejects or parses without value comparison are held by identifier in `YamlTestCorpusReader`'s two profile sets. A governance suite asserts every case resolves to exactly one category (**zero known gaps**), the by-name profile identifiers all resolve to real cases, the case and category counts match the values pinned in `YamlTestCorpusReader`, supported-pass vectors match their JSON expectation, round-trip through the writer, and produce a reader token stream whose structural shape matches the vector's `test.event` file (over the alias-free, single-document subset), and every profile-unsupported vector is rejected for a specific, recognized reason. To move to a newer suite release, check out the new tag inside the submodule, commit the updated pointer, update the pinned counts, and classify any added cases.

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

Tests live in `test/` as MSTest classes mirroring `src/`. The Regression tier runs the vendored `yaml/yaml-test-suite` conformance corpus, classified in code by `YamlTestCorpusReader`. Run tiers via the runsettings files at the solution root:

```bash
dotnet test Bodu.Text.Yaml/test/Bodu.Text.Yaml.Test.csproj --settings bvt.runsettings
dotnet test Bodu.Text.Yaml/test/Bodu.Text.Yaml.Test.csproj --settings regression.runsettings
```

## License

MIT. © Bodu Pty. Ltd.
