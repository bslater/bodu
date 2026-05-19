---
title: Bodu.Text.Configuration — Introduction
---

# Bodu.Text.Configuration

**Bodu.Text.Configuration** is the configuration-layering package of the Bodu suite. It reads a single text file in the
familiar **INI / EditorConfig** shape — preamble, named sections, `key = value` properties — and projects it into a
flattened, target-aware **view** keyed by colon-delimited configuration keys. The result drops directly into the same
shape `Microsoft.Extensions.Configuration` expects, without taking a dependency on that package: the bridge lives in
the sibling [`Bodu.Extensions.Configuration.Text`](../extensions-configuration-text/index.md) library.

The package is intentionally narrow: parse a document, optionally collect diagnostics, resolve it for a target path,
and read typed values back out. No reflection, no `dynamic`, no schema, no global state.

## Core mental model

![Configuration pipeline — source text to resolved view](../../images/diagrams/text-configuration-pipeline.svg)

Configuration runs as a four-stage pipeline: the **reader** (`BoduConfigurationReader`) tokenises the source text and
produces an immutable `IniDocument` from `Bodu.Text.Formats`; the **resolver** layers the document's preamble and
matching glob-anchored sections in source order to produce a `BoduConfigurationView`; the **getter API** on the view
returns typed values (`GetString`, `GetInt32`, `GetBoolean`, `GetEnum<T>`, `GetValue<T>` for any `ISpanParsable<T>`).
Every stage is opt-in: parse without resolving when you just want the document, resolve without typed accessors when
you only need raw strings.

A configuration file is *not* a snapshot of a single object graph — it is a layered description of how
properties change as a target path moves through a directory tree. The library's job is to collapse those layers down
to the right answer for a specific target.

## The shape of the library

The package contains five concept groups, all in the `Bodu.Text.Configuration` namespace.

### Document and view

*The minimal happy-path: `Parse` → `Resolve` → `GetXxx`.*

| Type | Purpose |
|---|---|
| <xref:Bodu.Text.Configuration.BoduConfigurationDocument> | Static façade — `Parse`, `ParseWithDiagnostics`, `Load`, `Save` over strings, streams, paths, and text readers. Backed by <xref:Bodu.Text.Formats.IniDocument>. |
| <xref:Bodu.Text.Configuration.BoduConfigurationView> | Resolved, flattened snapshot for one target path; implements `IEnumerable<KeyValuePair<string, string?>>`. |
| <xref:Bodu.Text.Configuration.BoduConfigurationExtensions> | Extension methods on `IniDocument`, `IniSection`, `IniEntry` — including the `Resolve(targetPath)` projection. |
| <xref:Bodu.Text.Configuration.BoduConfigurationParseResult> | The output of `ParseWithDiagnostics` — carries both the document and any diagnostics collected during the parse. |
| <xref:Bodu.Text.Configuration.BoduConfigurationParseException> | Thrown by the throwing parse / load overloads when `DiagnosticMode` is `Throw`. |

### Profiles and options

*Four named profiles cover the common use cases; everything else is composed from the per-stage option types.*

| Type | Purpose |
|---|---|
| <xref:Bodu.Text.Configuration.BoduConfigurationProfile> | Enum: `Bodu` (default), `EditorConfigCompatible`, `Strict`, `Relaxed`. |
| <xref:Bodu.Text.Configuration.BoduConfigurationParseOptions> | Reader behaviour: inline-comment mode, duplicate-key / -section handling, diagnostic mode, length limits, key options. Static `Bodu` / `EditorConfigCompatible` / `Strict` / `Relaxed` presets. |
| <xref:Bodu.Text.Configuration.BoduConfigurationResolveOptions> | Resolver behaviour: `PathRoot`, `MissingPathRootMode`, `ApplyPreambleProperties`, `UnsetValueMode`, `PathComparison`, `KeyOptions`. |
| <xref:Bodu.Text.Configuration.BoduConfigurationWriteOptions> | Save behaviour: encoding, newline style, blank-line policy, property formatting. |
| <xref:Bodu.Text.Configuration.BoduConfigurationKeyOptions> | Key behaviour: segment separators (default `.` and `:`), mapping (`DotToColon` / `Colon` / `Identity`), case sensitivity. |

### Keys

*The library distinguishes the raw key as authored in the file from the canonical colon-delimited form.*

| Type | Purpose |
|---|---|
| <xref:Bodu.Text.Configuration.BoduConfigurationKey> | Read-only struct with `RawKey`, `ConfigurationKey`, `Segments`, and `CaseSensitive`. Static `Parse` / `TryParse` factories. |
| <xref:Bodu.Text.Configuration.BoduConfigurationKeyMapping> | Enum: `DotToColon` (default), `Colon`, `Identity`. |

### Diagnostics

*When `DiagnosticMode` is `Collect`, the parser runs to completion and lists every recoverable issue.*

| Type | Purpose |
|---|---|
| <xref:Bodu.Text.Configuration.BoduConfigurationDiagnostic> | Immutable diagnostic record: severity, code, message, source location. |
| <xref:Bodu.Text.Configuration.BoduConfigurationDiagnosticSeverity> | Enum: `Warning`, `Error`. |
| <xref:Bodu.Text.Configuration.BoduConfigurationDiagnosticCode> | Enum identifying the diagnostic category (duplicate key, invalid section, unset literal, …). |
| <xref:Bodu.Text.Configuration.BoduConfigurationDiagnosticMode> | Enum: `Throw` (default), `Collect`, `Ignore`. |
| <xref:Bodu.Text.Configuration.BoduConfigurationSourceLocation> | Line / column metadata pointing into the source text. |

### Resolution modes

| Type | Purpose |
|---|---|
| <xref:Bodu.Text.Configuration.BoduConfigurationInlineCommentMode> | Enum: `Disabled` (EditorConfig), `WhitespaceIntroduced` (default), `Always`. |
| <xref:Bodu.Text.Configuration.BoduConfigurationUnsetValueMode> | Enum: `TreatAsLiteral` (default), `RemoveEffectiveValue` (EditorConfig). |
| <xref:Bodu.Text.Configuration.BoduConfigurationMissingPathRootMode> | Enum: `UseEmptyRoot` (default), `Throw`, `IgnoreAnchoredPatterns`. |

## Profile presets at a glance

| Profile | Inline comments | Duplicate keys | Diagnostics | Preamble in resolve | Unset semantics |
|---|---|---|---|---|---|
| `Bodu` (default) | WhitespaceIntroduced | LastWins | Throw | Applied | Literal |
| `EditorConfigCompatible` | Disabled | LastWins | Throw | Only `root` | Removes value |
| `Strict` | Disabled | Disallowed | Throw | Applied | Literal |
| `Relaxed` | WhitespaceIntroduced | LastWins | Collect | Applied | Literal |

Profiles are not exclusive — every option type is fully mutable through `init` properties, so the presets are
starting points, not contracts.

## Common scenarios

| Scenario | Reach for |
|---|---|
| Parse a file with default behaviour | `BoduConfigurationDocument.Parse(text)` or `Load(path)` |
| Parse a user-authored file without halting on errors | `BoduConfigurationDocument.ParseWithDiagnostics(text, BoduConfigurationParseOptions.Relaxed)` |
| Resolve effective settings for one source file | `doc.Resolve("src/Foo.cs").GetString("format:indent:style")` |
| Read a typed value with a fallback | `view.GetInt32("format:indent:size", fallback: 4)` |
| Read any `ISpanParsable<T>` | `view.GetValue<double>("limits:cpu:threshold")` |
| EditorConfig-strict parsing | `BoduConfigurationDocument.Parse(text, BoduConfigurationParseOptions.EditorConfigCompatible)` |
| Reject any input the parser cannot prove canonical | `BoduConfigurationParseOptions.Strict` |
| Use dots in keys but project to colon-delimited form | (default — `BoduConfigurationKeyMapping.DotToColon`) |
| Round-trip a document through save | `BoduConfigurationDocument.Save(doc, path)` |
| Pick the profile from configuration at runtime | `BoduConfigurationParseOptions.For(profile)` |
| Plug into <xref:Microsoft.Extensions.Configuration.IConfigurationBuilder> | See [Bodu.Extensions.Configuration.Text](../extensions-configuration-text/index.md). |

## File grammar at a glance

```ini
# A preamble property — applies before any section opens.
root = true

# A section: the header is a glob pattern matched against the resolver's target path.
[*.cs]
format.indent.style = space
format.indent.size  = 4

# Later sections override earlier sections for any path they both match (last-wins).
[src/**/*.{cs,csproj}]
format.indent.size = 2
```

The grammar matches **EditorConfig** verbatim with two Bodu-specific extensions:

1. **Inline comments** are recognised when introduced by whitespace (`key = value  # comment`) under the default `Bodu`
   profile. Set the profile to `EditorConfigCompatible` to disable them.
2. **Key mapping** projects dotted keys (`logging.level.default`) to colon-delimited configuration keys
   (`logging:level:default`) for direct interoperability with `Microsoft.Extensions.Configuration`. Both forms work as
   lookup inputs on the view; the canonical stored form is the colon-delimited one.

## Where to go next

- **[Core concepts](concepts.md)** — vocabulary: document vs view, profile, parse/resolve/write options, key mapping, glob pattern, preamble, target path, diagnostic mode, unset.
- **[Getting started](getting-started.md)** — install + minimal samples for parse-resolve-read, profile presets, diagnostics, round-trip save.
- **[Bodu.Extensions.Configuration.Text](../extensions-configuration-text/index.md)** — `IConfigurationBuilder` integration, options binding, file probing.
- **[Bodu.Text.Configuration API reference](../../apidoc/Bodu.Text.Configuration.md)** — full type-by-type docs.
- **[Bodu.Text.Formats](../formats/index.md)** — the underlying `IniDocument` model that `BoduConfigurationDocument.Parse` returns.
