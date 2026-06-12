---
title: Configuration — Concepts
---

# Configuration — Concepts

The shared vocabulary for the [Configuration topic](configuration.md) — the terms that cross the boundary between [`Bodu.Text.Configuration`](../text-configuration/index.md) and [`Bodu.Extensions.Configuration.Text`](../extensions-configuration-text/index.md). Each member library has its own deeper concepts page (linked in the closing table); this page covers only what you need to navigate both at once.

## Profile

A **profile** is a named, validated combination of parse, resolve, and write options. Four ship in the box, and each option type exposes them as static presets plus a `For(profile)` factory:

| Profile | Intent |
|---|---|
| `Bodu` *(default)* | Permissive Bodu defaults: dotted-to-colon key mapping, whitespace-introduced inline comments, last-wins duplicates, preamble participates in resolve. |
| `EditorConfigCompatible` | Strict alignment with EditorConfig 0.17.2: inline comments disabled, identity key mapping, only the `root` key honoured from the preamble. |
| `Strict` | Deterministic parsing for generated files: duplicate keys rejected, key-only properties not permitted. |
| `Relaxed` | Permissive parsing of user-authored files: diagnostics are collected rather than thrown, duplicates last-wins. |

Profiles are starting points, not contracts — every property on every options bag is mutable through `init` setters.

## Preamble vs glob-anchored section

The **preamble** is the file's global section: properties that appear before any `[...]` header. Under the default `Bodu` profile, preamble properties act as defaults that any matching section can override; under `EditorConfigCompatible`, only the well-known `root` key is honoured.

A **glob-anchored section** is everything else. Its header is a glob pattern (`[*.cs]`, `[src/**/*.cs]`) matched against the resolver's **target path** — the value passed to `Resolve(targetPath)`. Anchored patterns (those containing `/`) are tested against `PathRoot + targetPath`; unanchored patterns are tested against the filename only.

## Layered resolution

The resolver walks the document once, in source order: preamble first (when enabled), then each section whose pattern matches the target path, copying properties into a working dictionary with **last-wins** overwrite. There is no precedence rule beyond "later wins for a given key" — the same single-pass semantics EditorConfig defines. Nearer, more specific sections therefore appear later in well-organized files so they win over broad defaults.

## `ConfigurationView` and typed accessors

A **view** (<xref:Bodu.Text.Configuration.ConfigurationView>) is the resolved, flattened snapshot for one target path — a one-shot dictionary of colon-delimited keys to effective string values:

```
source text ──► ConfigurationDocument.Parse ──► ConfigurationDocument
ConfigurationDocument ──► .Resolve(targetPath) ──► ConfigurationView
ConfigurationView ──► .GetXxx(key) ──────────► typed value
```

Subsequent mutation of the originating document does not retroactively update the view — take a fresh view whenever the document or the target path changes. On top of the raw strings it layers typed getters: `GetString`, `GetInt32`, `GetBoolean`, `GetEnum<T>`, and `GetValue<T>` for any `ISpanParsable<T>`, each with throwing, fallback, and `Try` shapes. Lookups accept either the dotted or the colon-delimited key form; parsing uses the invariant culture so behaviour is deterministic across locales.

## Diagnostics

A **diagnostic** (<xref:Bodu.Text.Configuration.ConfigurationDiagnostic>) is a structured record — severity, code, message, source location — describing a recoverable parse issue. The **diagnostic mode** routes them: `Throw` (default) raises <xref:Bodu.Text.Configuration.ConfigurationParseException> on the first issue, `Collect` runs the parser to completion and attaches every issue to a <xref:Bodu.Text.Configuration.ConfigurationParseResult>, and `Ignore` discards them.

## Source and provider (Microsoft.Extensions side)

The bridge follows the three-part contract every `Microsoft.Extensions.Configuration` provider uses. A **source** (<xref:Bodu.Extensions.Configuration.Text.TextConfigurationSource>, or the stream sibling <xref:Bodu.Extensions.Configuration.Text.TextStreamConfigurationSource>) holds the description of *what to load and how* — path, target path, parse and resolve options, reload behaviour. A **provider** (<xref:Bodu.Extensions.Configuration.Text.TextConfigurationProvider>) performs the actual load when the builder calls `Build`, parsing and resolving via `Bodu.Text.Configuration` and populating the standard `Data` dictionary.

## Flattened colon-delimited keys

`IConfiguration` keys are colon-delimited by convention — `service:name`, `logging:level:default`. The Bodu reader projects raw dotted keys to the same shape under the default `DotToColon` mapping, so the provider copies the resolved view into `IConfiguration` verbatim. `configuration.GetSection("logging")` and POCO binding then work exactly as they do for the JSON provider.

A configuration key has three concurrent forms, all held by <xref:Bodu.Text.Configuration.ConfigurationKey>: the **raw key** as authored in the file (`logging.level.default`), the **segments** split on the configured separators, and the canonical **path** (`logging:level:default`) stored in the view. Lookups accept the raw and the canonical forms interchangeably.

## Precedence among providers

Within a single Bodu document, precedence is the layered resolution described above. Across providers in one `IConfigurationBuilder`, the standard `Microsoft.Extensions.Configuration` rule applies instead: **later-added sources override earlier ones** for any key both define. A Bodu file added after `appsettings.json` overrides matching JSON keys; added before, it supplies defaults the JSON file can override.

## Reload token

When a file source sets `ReloadOnChange`, the provider attaches a file watcher through its `IFileProvider`; a change triggers a reparse and re-resolve, and the standard `IConfiguration` **reload tokens** fire so `IOptionsMonitor<T>` consumers re-bind automatically. Stream sources are one-shot and do not reload — rebuild the configuration for dynamic stream-backed inputs.

## Going deeper

| Concept area | Where it is covered in full |
|---|---|
| Document vs view, key mapping, glob grammar, unset sentinel, diagnostic catalogue, round-trip save | [Bodu.Text.Configuration — Core concepts](../text-configuration/concepts.md) |
| Source vs provider vs loader, target path, option propagation, conventional file probe, reload-on-change, options binding | [Bodu.Extensions.Configuration.Text — Core concepts](../extensions-configuration-text/concepts.md) |

## See also

- **[Configuration overview](configuration.md)** — the topic landing page.
- **[Configuration guides](../../guides/topics/configuration.md)** — recipe-style walk-throughs across both packages.
