# Bodu.Text.Yaml

> **API stability - Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

A YAML library for .NET 8 and .NET 10. It maps plain CLR objects to and from YAML through a configurable converter model, over a token reader and writer, with both a mutable and a read-only document object model. The public surface matches the sibling `Bodu.Text.Toml` and `Bodu.Text.Bencode` libraries, so the patterns transfer directly between them.

## Installation

```shell
dotnet add package Bodu.Text.Yaml
```

Targets `net8.0` and `net10.0`.

## Conformance profile

Bodu.Text.Yaml implements the **Bodu YAML Core Tree Profile**: a YAML 1.2 core-schema, JSON-compatible tree model for configuration data. It is a predictable configuration- and document-mapping library, not a full YAML 1.2 representation-graph processor. The profile is enforced - inputs that fall outside it are rejected with `YamlFormatException` rather than silently degraded - and the enforcement is gated by the vendored `yaml-test-suite` conformance corpus (see [Conformance corpus](#conformance-corpus)).

**Supported**

- Block and flow sequences and mappings.
- Plain, single-quoted, double-quoted, literal (`|`), and folded (`>`) scalars, with chomping and indentation indicators.
- Comments, the `---` / `...` document markers, and multi-document streams, in which any document may begin with a byte-order mark.
- Implicit typing under the YAML 1.2 core schema by default (only `true`/`false` are booleans - no "Norway problem"), with opt-in YAML 1.1 typing via `SpecVersion` (`yes`/`no`, `on`/`off`, `y`/`n`, and the YAML 1.1 integer forms). Base-60 ("sexagesimal") numbers such as `1:30` are not resolved under either version and stay strings. A float beyond the binary64 range, such as `1e309`, reads as an infinity, as IEEE 754 conversion gives, under either version. A document's `%YAML` directive is honored for scalar resolution, overriding the configured `SpecVersion` for that document.
- Anchors and aliases, subject to **acyclic** tree resolution.
- The core tags (`!!str`, `!!null`, `!!bool`, `!!int`, `!!float`), written as shorthands or verbatim (`!<tag:yaml.org,2002:str>`) with any `%`-escaped characters decoded, and `%TAG` handle expansion. A tagged scalar is resolved from its text as written, so `!!str null` is the string `null` and `!!float .inf` is an infinity, and the non-specific tag `!` makes a scalar a string. An explicit core tag whose content is invalid for the tag (for example `!!int abc`) is rejected, not silently degraded.
- The YAML 1.1 merge key (`<<`) as a compatibility feature, expanded by default under either spec version; `YamlMergeKeyBehavior`, on the reader, document, and serializer options, can disable it so that `<<` is an ordinary key.

**Rejected** (each throws `YamlFormatException`)

- **Complex (non-scalar) and alias mapping keys** - keys must resolve to scalar strings. A sequence, a mapping, or an alias (even an alias of a scalar) used as a key is rejected, not coerced.
- **Duplicate mapping keys** - by default (configurable via `YamlDuplicateKeyBehavior`). A scalar key is named, and compared with the other keys, by its text as written in every key form (block, flow, explicit, and flow-pair keys), so `0x10` and `16`, or `y` and `yes` under YAML 1.1, are distinct keys.
- **Implicit keys longer than 1024 characters**, the specification's limit, counted with the key's node properties, its quotes, and the spaces before its `:`. A longer key must be an explicit key (`? key`), which is how `Utf8YamlWriter` writes one.
- **Invalid merge keys**, while merge keys are expanded: a merge value that is not a mapping, an alias of one, or a sequence of them, and a second merge key in one mapping, whatever the duplicate-key policy (several mappings merge through one `<<` whose value is a sequence). Only the plain, untagged `<<` is a merge key; a quoted or tagged `"<<"` is an ordinary key.
- **Duplicate / overriding anchors and cyclic aliases.** A repeated anchor name is rejected: anchor override is a YAML representation-graph feature outside this tree profile.
- **Tabs used as indentation**, including a tab between a `-` entry indicator or an explicit `:` and the compact sequence or mapping that follows it on the line, a tab in place of the spaces that indent a plain scalar's continuation line, and a tab anywhere in the leading whitespace of a line that begins a node, which counts as indentation even after the indentation spaces or before a top-level flow collection (tabs remain legal as separation whitespace, after a node property or a continuation line's indentation included).
- **Invalid UTF-8, unpaired surrogates, invalid Unicode escapes, and non-printable control characters.** A surrogate escape is valid only as a high surrogate followed directly by a low one, the escaped pair JSON writes for a character outside the Basic Multilingual Plane, which decodes to that one character; a lone or reversed surrogate escape is rejected.
- **Malformed or unsupported directives** and **unknown non-core tags**.

This profile aligns with the Bodu TOML / Bencode and `System.Text.Json` architectural family. Broader YAML graph support (complex keys, anchor identity, a streaming event reader, richer tag resolution) is a deliberate future extension, not a silent partial behavior of this release.

### Compliance matrix

| Feature | Profile support | Behavior |
|---|---|---|
| Block / flow collections | Supported | Parsed to mappings and sequences. |
| Scalar styles (plain, quoted, literal, folded) | Supported | Decoded with chomping and indentation indicators. |
| Core-schema typing (`null`/`bool`/`int`/`float`/`str`) | Supported | YAML 1.2 core by default; YAML 1.1 via `SpecVersion` or a `%YAML 1.1` directive. |
| Anchors / aliases | Supported (acyclic) | Resolved into tree nodes; cycles and anchor override rejected. |
| Merge key `<<` | Supported | Expanded by default under either spec version; `YamlMergeKeyBehavior` can disable it. |
| Core tags + `%TAG` | Supported | Expanded and validated; invalid tagged content rejected. |
| Multi-document streams | Supported | `YamlDocument.ParseAllDocuments`. |
| Complex (non-scalar) and alias keys | Rejected | `YamlFormatException`. |
| Duplicate mapping keys | Rejected by default | Configurable via `YamlDuplicateKeyBehavior`. |
| Anchor override, cyclic graphs | Rejected | Outside the tree profile. |
| Unknown / non-core tags | Rejected | Outside the core schema. |
| Tabs in indentation, invalid UTF-8, control chars | Rejected | Source validation. |

### Conformance corpus

The `yaml/yaml-test-suite` corpus is linked into the repository as the **`yaml-test-suite` git submodule** (under `Bodu.Text.Yaml/test/`, pinned to a released `data-YYYY-MM-DD` commit). Initialize it before running the Regression tier:

```shell
git submodule update --init Bodu.Text.Yaml/test/yaml-test-suite
```

The Regression test tier reads each upstream case through `YamlTestCorpusReader`, which walks the submodule directory tree and classifies each case in code into a `YamlTestVector` KAT - `SupportedPass`, `SupportedParseOnly`, `SupportedFail`, or `UnsupportedFeatureRejected`. Classification is derived from the case's own files (an `error` file marks an expected failure; an `in.json` marks a supported-valid case); the valid upstream cases the profile deliberately rejects or parses without value comparison are held by identifier in `YamlTestCorpusReader`'s two profile sets. A governance suite asserts every case resolves to exactly one category (**zero known gaps**), the by-name profile identifiers all resolve to real cases, the case and category counts match the values pinned in `YamlTestCorpusReader`, supported-pass vectors match their JSON expectation, round-trip through the writer, and produce a reader token stream whose structural shape matches the vector's `test.event` file (over the alias-free, single-document subset), and every profile-unsupported vector is rejected for a specific, recognized reason. To move to a newer suite release, check out the new tag inside the submodule, commit the updated pointer, update the pinned counts, and classify any added cases.

> **Reader note.** `Utf8YamlReader` exposes a forward-only token surface like `System.Text.Json.Utf8JsonReader`, but it is **buffered**: the constructor parses the whole document into an in-memory node store and `Read()` walks it. It is the analogue of the TOML library's `TomlDocumentReader` cursor, not the streaming `Utf8TomlReader` scanner - YAML's indentation context, back-referencing aliases, and merge keys cannot be resolved in a single forward pass.

## API shape

| Type(s) | Namespace | Role |
|---|---|---|
| `YamlSerializer` / `YamlSerializerOptions` / `YamlSerializerDefaults` | `Bodu.Text.Yaml` | Static serializer entry point, its configuration, and scenario presets (for example `Web`). |
| `YamlTokenType`, `YamlValueKind`, `YamlSpecVersion` | `Bodu.Text.Yaml` | Token/value classification and the spec-version selector (naming policies are the shared `Bodu.Text.Serialization.NamingPolicy`). |
| `YamlFormatException` / `YamlSerializationException` | `Bodu.Text.Yaml` | Failures split by cause: malformed input vs. values that cannot be mapped. |
| `Utf8YamlReader` (+ `YamlReaderOptions`) | `Bodu.Text.Yaml.Reader` | Buffered forward-only `ref struct` token reader. |
| `Utf8YamlWriter` (+ `YamlWriterOptions`) | `Bodu.Text.Yaml.Writer` | Forward-only `ref struct` token writer. |
| `YamlDocument` / `YamlElement` / `YamlProperty` | `Bodu.Text.Yaml.Document` | Read-only, low-allocation document object model. |
| `YamlNode` / `YamlObject` / `YamlArray` / `YamlValue` | `Bodu.Text.Yaml.Nodes` | Mutable document object model: parse, edit, write back. |
| `YamlConverter<T>` / `YamlConverterFactory`, `YamlStringEnumConverter` (+ generic) / `YamlNumberEnumConverter<TEnum>` | `Bodu.Text.Yaml.Serialization` | Custom converters, converter factories, and the public enum converters; the per-member attribute family is the shared `Bodu.Text.Serialization` set (`[PropertyName]` / `[Ignore]` / `[Converter]` / …). |

```csharp
using Bodu.Text.Yaml;

string yaml = YamlSerializer.Serialize(new ServerConfig { Host = "localhost", Port = 8080 });
ServerConfig config = YamlSerializer.Deserialize<ServerConfig>(yaml)!;

// Document object models
using Bodu.Text.Yaml.Nodes;
YamlNode node = YamlNode.Parse(yamlText)!;
node["server"]!["port"] = YamlValue.Create(9090);
string back = node.ToYamlString();
```

- The full serializer feature surface is present: converters and factories, the shared attribute family (`PropertyName`/`Ignore`/`Converter`/`PropertyOrder`/`Constructor`/`Required`/`Include`/`ExtensionData`/`NamingPolicy`/`UnmappedMemberHandling`/`ObjectCreationHandling`/`StringEnumMemberName`), the serialization callbacks, naming policies and `YamlSerializerDefaults.Web`, the string/number enum converters, and the DOM bridges (`Deserialize<YamlNode>` / `<YamlElement>` / `<YamlDocument>`).
- Failures surface through `YamlFormatException` (malformed input, with line/column/offset) and `YamlSerializationException` (binding failures, with the member path).

## Testing

Tests live in `test/` as MSTest classes mirroring `src/`. The Regression tier runs the vendored `yaml/yaml-test-suite` conformance corpus, classified in code by `YamlTestCorpusReader`. Run tiers via the runsettings files at the solution root:

```bash
dotnet test Bodu.Text.Yaml/test/Bodu.Text.Yaml.Test.csproj --settings bvt.runsettings
dotnet test Bodu.Text.Yaml/test/Bodu.Text.Yaml.Test.csproj --settings regression.runsettings
```

## License

MIT. © Bodu Pty. Ltd.
