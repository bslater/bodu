---
title: Runnable samples
---

# Runnable samples

The repository ships runnable, self-contained sample projects for `Bodu.Text.Toml` under
[`samples/Text.Toml/`](https://github.com/bslater/bodu/tree/master/samples/Text.Toml). Both
samples are **offline and deterministic** — they run against small committed `Data/*.toml`
files — and are members of `bodu.slnx`, built and executed by CI, so the code they show cannot
drift from the current API. Each sample's README documents every scenario individually: its
intent, what the code does, the output to expect, and the APIs demonstrated.

Run either sample from the repository root:

```bash
dotnet run --project samples/Text.Toml/<SampleName>
```

## The samples

### Bodu.Text.Toml.Samples.TomlBasics

The front door: the <xref:Bodu.Text.Toml.TomlSerializer> POCO surface, shaped after
`System.Text.Json`. A committed config file (nested table, array of tables) deserializes into a
typed graph and round-trips; TOML's four native date-time kinds arrive as `DateOnly`,
`TimeOnly`, and `DateTimeOffset` — no invented midnights; wire names layer a naming policy
(<xref:Bodu.Text.Serialization.NamingPolicy>) under the attribute family (`[PropertyName]`,
`[Ignore]`, `[Required]`) with <xref:Bodu.Text.Toml.Serialization.TomlStringEnumConverter>
re-casing enums; and the two wire knobs — <xref:Bodu.Text.Toml.TomlSpecVersion> gating the TOML
v1.1.0 grammar on parse and <xref:Bodu.Text.Toml.TomlByteArrayHandling> choosing the `byte[]`
shape — each shown with both settings. *Package: `Bodu.Text.Toml`.*

### Bodu.Text.Toml.Samples.TomlDocuments

The layers beneath the serializer, mirroring the `System.Text.Json` stack: the mutable
<xref:Bodu.Text.Toml.Nodes.TomlNode> DOM (parse, edit through indexers, graft new tables,
re-emit), the read-only <xref:Bodu.Text.Toml.Document.TomlDocument> DOM (one parse, cheap
<xref:Bodu.Text.Toml.Document.TomlElement> cursors, typed getters, `EnumerateObject`,
`TryGetProperty` probing), the allocation-free <xref:Bodu.Text.Toml.Writer.Utf8TomlWriter> /
<xref:Bodu.Text.Toml.Reader.Utf8TomlReader> token surface, and streaming reads across buffer
boundaries with a resumable <xref:Bodu.Text.Toml.Reader.TomlReaderState> — the
`Utf8JsonReader` pattern, demonstrated by splitting the document mid-token and matching the
one-shot token count. *Package: `Bodu.Text.Toml`.*

## Guarded documentation

The TOML guides under [`docs/guides/serialization/toml/`](../guides/serialization/toml/index.md)
carry compile-guarded snippets: examples marked with a `<!-- compile -->` sentinel are compiled
against the current public API by `DocumentationSnippetCompileTests` in the library's test
project (Regression tier), so shown code cannot silently rot.

## Related

- [TOML guides](../guides/serialization/toml/index.md) — the full serializer, DOM, and
  reader/writer documentation.
- [Bencode samples](bencode.md) — the same System.Text.Json-aligned stack for BEP 3 Bencode.
