# Text.Yaml Samples

Console applications demonstrating the `Bodu.Text.Yaml` package. Each sample is a standalone
project; run one with:

```bash
dotnet run --project samples/Text.Yaml/<SampleName>
```

Every sample is offline and deterministic: the inputs are small committed `Data/*.yaml` files,
and every scenario prints the same output on every run. These are the YAML counterparts of the
`Text.Toml` and `Text.Bencode` samples — the same `System.Text.Json`-aligned stack.

## Sample → pattern → package matrix

| Sample | Demonstrates | Packages |
|---|---|---|
| `Bodu.Text.Yaml.Samples.YamlBasics` | The `YamlSerializer` POCO surface — file→POCO→YAML round trips over nested mappings and sequences, YAML's implicit scalar typing (string/int/float/bool/null) with `YamlNumberHandling`, naming policies layered under `[PropertyName]`/`[Ignore]`/`[Required]` with `WriteEnumsAsStrings`, and the wire knobs (`YamlSpecVersion` defaulting to v1.2, scalar style, duplicate-key and merge-key behaviours) | `Bodu.Text.Yaml` |
| `Bodu.Text.Yaml.Samples.YamlDocuments` | The layers beneath the serializer — the `Utf8YamlWriter`/`Utf8YamlReader` token surface, the mutable `YamlNode` DOM (edit through `YamlObject`/`YamlArray`/`YamlValue`, graft nodes, re-emit), the read-only `YamlDocument`/`YamlElement` DOM (typed getters, `EnumerateMapping`/`EnumerateSequence`, probing), and the stream/async serializer facade | `Bodu.Text.Yaml` |

Each sample project has its own README with the four-part per-scenario breakdown (Intent /
What it does / What to expect / APIs demonstrated).
