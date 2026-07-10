# Bodu.Text.Configuration

> **API stability — Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

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

## Quick start

```csharp
using Bodu.Text.Configuration;

ConfigurationDocument document = ConfigurationDocument.Parse("""
    root = true

    [*]
    indent_size = 4
    start_day = Monday

    [src/**.cs]
    indent_size = 8
    """);

// Sections whose glob matches the target path apply, later sections winning.
ConfigurationView view = document.Resolve("src/App/Program.cs");

var indent = view.GetInt32("indent_size");                     // 8
var start = view.GetEnum<DayOfWeek>("start_day");              // Monday
var missing = view.TryGetValue("theme", out string? theme);    // false - no throw

// Presets switch the whole pipeline's dialect in one place:
ConfigurationView compat = document.Resolve(
    "src/App/Program.cs", ConfigurationResolveOptions.EditorConfigCompatible);
```

`ParseWithDiagnostics` returns the document plus collected `ConfigurationDiagnostic` rows instead
of throwing; `ConfigurationDocument.Save(document, path, writeOptions)` round-trips the document
(comments preserved by default).

## Where to start

- [Documentation index](../docs/docs/text-configuration/index.md) — high-level pipeline overview.
- [Concepts](../docs/docs/text-configuration/concepts.md) — vocabulary used throughout the API: documents,
  views, profiles, target paths, preamble, unset values, diagnostics.
- [Getting started](../docs/docs/text-configuration/getting-started.md) — worked samples for parsing,
  resolving, typed value access, and round-tripping.

## Runnable samples

The repository ships offline, `dotnet run`-able sample projects for this package and its
bridge — the resolve cascade, diagnostics, `unset` dialect handling, save round trips, and
the `Microsoft.Extensions.Configuration` integration — under
[`samples/Text.Configuration/`](https://github.com/bslater/bodu/tree/master/samples/Text.Configuration).

## When to reach for the bridge package

If you want a `.boduconfig` file to flow into `Microsoft.Extensions.Configuration` (the standard
`IConfiguration` / `IOptions<T>` pipeline) rather than calling the document/resolver model directly, use
[`Bodu.Extensions.Configuration.Text`](../Bodu.Extensions.Configuration.Text). It bridges the same
document model into the `IConfigurationBuilder` API surface.
