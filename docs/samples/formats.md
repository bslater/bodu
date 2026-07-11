---
title: Runnable samples
---

# Runnable samples

The repository ships runnable, self-contained sample projects for `Bodu.Text.Formats` under
[`samples/Text.Formats/`](https://github.com/bslater/bodu/tree/master/samples/Text.Formats).
Both samples are **offline and deterministic** — they run against small committed `Data/`
files plus inline snippets of deliberately dirty input — and are members of `bodu.slnx`,
built and executed by CI. Each README documents every scenario individually: its intent, what
the code does, the output to expect, and the APIs demonstrated.

Run either sample from the repository root:

```bash
dotnet run --project samples/Text.Formats/<SampleName>
```

> Both samples' root namespaces sit under `Bodu.Samples.*` rather than `Bodu.Text.*`: from
> inside a namespace under `Bodu.Text`, the simple names `Delimited`, `Ini`, and `DotEnv`
> resolve to their *namespaces* instead of the facade classes. Each README documents the
> pitfall for consumers whose own code lives under `Bodu.Text`.

## The samples

### Bodu.Text.Formats.Samples.DelimitedData

RFC 4180 CSV/TSV via `Bodu.Text.Delimited`: parse a committed trades file into a
<xref:Bodu.Text.Delimited.DelimitedDocument> and read fields by header name with the
culture-safe `ISpanParsable` getters (`GetValue<int>` / `<decimal>` / `<DateTimeOffset>`);
the policy knobs for dirty input — <xref:Bodu.Text.Delimited.DelimitedFieldCountBehavior>
(`Strict` throws, `Ragged` admits short/long rows) and
<xref:Bodu.Text.Delimited.DelimitedMalformedRecordBehavior> (`SkipRecord` truncates the
malformed record, which is why lenient ingestion pairs it with `Ragged`); `Delimited.Format`
round trips with selective quoting plus one-argument CSV→TSV dialect conversion; and a
constant-memory <xref:Bodu.Text.Delimited.DelimitedReader> →
<xref:Bodu.Text.Delimited.DelimitedWriter> filter pipeline that never materializes a
document. *Package: `Bodu.Text.Formats`.*

### Bodu.Text.Formats.Samples.ConfigFiles

The two config-file formats: INI (`Bodu.Text.Ini`) — the global section, named sections,
typed getters, comment lines preserved on the entries they precede, and the mutate + `Format`
edit loop where every original comment survives (with `IniEntry.InlineComment` as the
write-side inline-comment surface); and DotEnv (`Bodu.Text.DotEnv`) — `export` prefixes,
quoting, inline comments, empty-vs-absent values, the deliberate *no-interpolation* contract
(values are literal; `${VAR}` expansion is the consumer's explicit decision), and the
streaming <xref:Bodu.Text.DotEnv.DotEnvReader> with per-entry line numbers, composed into a
miniature secrets lint pass. *Package: `Bodu.Text.Formats`.*

## Guarded documentation

The formats guides under [`docs/guides/formats/`](../guides/formats/index.md) carry
compile-guarded snippets: examples marked with a `<!-- compile -->` sentinel — including the
INI and DotEnv quick starts and the delimited `Ragged` example — are compiled against the
current public API by `DocumentationSnippetCompileTests` in the library's test project
(Regression tier).

## Related

- [Formats guides](../guides/formats/index.md) — the full Delimited, INI, and DotEnv
  documentation.
- [Text.Configuration samples](text-configuration.md) — the richer `.boduconfig` cascade
  format, for when INI-style files need profiles and path-targeted resolution.
