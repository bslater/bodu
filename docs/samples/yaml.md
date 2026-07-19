---
title: Runnable samples
---

# Runnable samples

The repository ships runnable, self-contained sample projects for `Bodu.Text.Yaml` under
[`samples/Text.Yaml/`](https://github.com/bslater/bodu/tree/master/samples/Text.Yaml). Both
samples are **offline and deterministic** — they run against small committed `Data/*.yaml`
files — and are members of `bodu.slnx`, built and executed by CI, so the code they show cannot
drift from the current API. Each sample's README documents every scenario individually: its
intent, what the code does, the output to expect, and the APIs demonstrated. These are the YAML
counterparts of the [Toml](toml.md) and [Bencode](bencode.md) samples — the same
`System.Text.Json`-aligned stack.

Run either sample from the repository root:

```bash
dotnet run --project samples/Text.Yaml/<SampleName>
```

## The samples

### Bodu.Text.Yaml.Samples.YamlBasics

The `YamlSerializer` POCO surface: a committed config file (nested mapping, sequence) deserializes
into a typed graph and round-trips; YAML's implicit scalar typing coerces string/int/float/bool/
null with the `YamlNumberHandling` knob; sequences and mappings bind to `List<T>`/arrays and
`Dictionary<,>`; wire names layer a naming policy under the shared attribute family
(`[PropertyName]`, `[Ignore]`, `[Required]`) with `WriteEnumsAsStrings` controlling enum output;
and the wire knobs — `YamlSpecVersion` (defaulting to the Norway-problem-safe v1.2), scalar-style
selection, and the duplicate-key and merge-key behaviours — are each shown. *Package:
`Bodu.Text.Yaml`.*

### Bodu.Text.Yaml.Samples.YamlDocuments

The layers beneath the serializer, mirroring the `System.Text.Json` stack: the
`Utf8YamlWriter` / `Utf8YamlReader` token surface, the mutable `YamlNode` DOM (parse, edit
through `YamlObject`/`YamlArray`/`YamlValue`, graft nodes, re-emit), the read-only `YamlDocument`
DOM (one parse, cheap `YamlElement` cursors, `EnumerateMapping`/`EnumerateSequence`,
`TryGetProperty`, typed getters), and the stream/async serializer facade (`Deserialize(Stream)`,
`DeserializeAsync`, `Serialize(IBufferWriter<byte>)`). *Package: `Bodu.Text.Yaml`.*

## Related

- [Toml samples](toml.md) and [Bencode samples](bencode.md) — the sibling
  `System.Text.Json`-aligned serializers.
