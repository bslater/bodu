# Bodu.Text.Configuration

An EditorConfig-inspired, INI-backed configuration document model with parse, resolve, and write phases
that consumers can compose independently.

The library separates three concerns:

```
source text ──► ConfigurationDocument.Parse  ──► IniDocument
IniDocument ──► .Resolve(targetPath)         ──► ConfigurationView
ConfigurationView ──► .GetXxx(key)           ──► typed value
```

Profiles select a coherent set of parse, resolve, and write defaults: `Bodu` (permissive default),
`EditorConfigCompatible` (strict EditorConfig 0.17.2 alignment), `Strict` (deterministic parsing for
generated files), and `Relaxed` (collect-diagnostics for user-authored files).

## Where to start

- [Documentation index](../docs/docs/text-configuration/index.md) — high-level pipeline overview.
- [Concepts](../docs/docs/text-configuration/concepts.md) — vocabulary used throughout the API: documents,
  views, profiles, target paths, preamble, unset values, diagnostics.
- [Getting started](../docs/docs/text-configuration/getting-started.md) — worked samples for parsing,
  resolving, typed value access, and round-tripping.

## When to reach for the bridge package

If you want a `.boduconfig` file to flow into `Microsoft.Extensions.Configuration` (the standard
`IConfiguration` / `IOptions<T>` pipeline) rather than calling the document/resolver model directly, use
[`Bodu.Extensions.Configuration.Text`](../Bodu.Extensions.Configuration.Text). It bridges the same
document model into the `IConfigurationBuilder` API surface.
