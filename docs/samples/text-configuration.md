---
title: Runnable samples
---

# Runnable samples

The repository ships runnable, self-contained sample projects for `Bodu.Text.Configuration`
and its `Microsoft.Extensions.Configuration` bridge under
[`samples/Text.Configuration/`](https://github.com/bslater/bodu/tree/master/samples/Text.Configuration).
Both samples are **offline and deterministic** — they run against committed `Data/` files —
and are members of `bodu.slnx`, built and executed by CI. Each README documents every
scenario individually: its intent, what the code does, the output to expect, and the APIs
demonstrated.

Run either sample from the repository root:

```bash
dotnet run --project samples/Text.Configuration/<SampleName>
```

## The samples

### Bodu.Text.Configuration.Samples.ConfigCascade

The document/resolver pipeline end to end: `Parse` vs
<xref:Bodu.Text.Configuration.ConfigurationDocument.ParseWithDiagnostics*> under the Relaxed
profile (diagnostics collected, document still usable); the heart of the library — path-targeted
resolution, where every section whose glob matches the target contributes keys and later
sections win, shown by resolving one file for three different targets and reading typed values
(`GetInt32`, `GetEnum<DayOfWeek>`, fallbacks) from the
<xref:Bodu.Text.Configuration.ConfigurationView>; the `unset` dialect decision —
<xref:Bodu.Text.Configuration.ConfigurationUnsetValueMode> `TreatAsLiteral` vs
`RemoveEffectiveValue`, and the canonical profile option sets
(`ConfigurationResolveOptions.EditorConfigCompatible`) that switch the whole pipeline
coherently; and the write phase — in-place section edits, composing an appended section, and
`ConfigurationDocument.Save` with every comment preserved. *Package:
`Bodu.Text.Configuration`.*

### Bodu.Extensions.Configuration.Text.Samples.BridgeHosting

The same file formats flowing into the standard `Microsoft.Extensions.Configuration`
pipeline: `AddTextConfigurationFile` resolving the `.boduconfig` cascade for a supplied
`targetPath` at load time (the same file yields `logging:level = information` for a dev
target and `warning` for a production target); `AddTomlFile` flattening TOML tables onto
colon-separated configuration keys (`[server.limits]` → `server:limits:*`) with
`optional: true` skipping missing files; and the final hop —
`AddConfigurationOptions<TOptions>` binding a section to a POCO registered with dependency
injection, consumed as `IOptions<ServerOptions>` by code that never learns the values came
from TOML. The README also documents the path-handling difference between the two sources
(file-provider-relative vs direct). *Packages: `Bodu.Extensions.Configuration.Text`,
`Bodu.Text.Configuration`, `Bodu.Text.Toml`.*

## Guarded documentation

The guides under [`docs/guides/text-configuration/`](../guides/text-configuration/index.md)
and
[`docs/guides/extensions-configuration-text/`](../guides/extensions-configuration-text/index.md)
carry compile-guarded snippets: examples marked with a `<!-- compile -->` sentinel are
compiled against the current public API by `DocumentationSnippetCompileTests` in each
library's test project (Regression tier).

## Related

- [Text.Configuration guides](../guides/text-configuration/index.md) — parsing, profiles,
  views and resolution, diagnostics.
- [Configuration-bridge guides](../guides/extensions-configuration-text/index.md) — the
  `IConfigurationBuilder` sources and options binding.
- [Formats samples](formats.md) — plain INI and DotEnv, for when you don't need cascades.
