---
title: Views and resolution
---

# Views and resolution

A parsed `IniDocument` is the codec's faithful representation — sections, entries, comments, line numbers. The *view* layer takes that document and projects it into the shape application code wants to consume: dotted-or-colon-delimited paths flattened across sections, EditorConfig glob sections resolved against a target path, typed value getters, and missing-key fallbacks.

This guide covers `ConfigurationView`, `ConfigurationResolveOptions`, `ConfigurationKey`, `ConfigurationKeyOptions`, and `ConfigurationPattern`. For the parse side — `ConfigurationDocument.Parse`, profiles, parse options — see [Parsing and profiles](parsing-and-profiles.md). For the diagnostic catalogue — every `ConfigurationDiagnosticCode` value — see [Diagnostics](diagnostics.md).

## Pattern 1 — resolve a document to a view

```csharp
using Bodu.Text.Configuration;
using Bodu.Text.Ini;

string source = """
appName = Bodu.Sample

[logging.console]
level = Information
verbose = true
""";

IniDocument document = ConfigurationDocument.Parse(source);

ConfigurationView view = document.Resolve();

string? name = view["appName"];                       // "Bodu.Sample"
string? lvl  = view["logging:console:level"];         // "Information"
string? lvl2 = view["logging.console.level"];         // "Information" — dot form accepted too
```

`Resolve(targetPath?)` is an extension method on `IniDocument` from `ConfigurationExtensions`. It walks the document, projects every entry into a colon-delimited path (`section:key`), applies the `KeyMapping` (dotted-to-colon by default), and produces a `ConfigurationView` that consumers query with either dotted or colon-delimited keys.

## Pattern 2 — anchored EditorConfig globs

The `targetPath` argument changes everything. When supplied, sections are treated as EditorConfig glob patterns and only the sections whose patterns match the target path contribute to the view:

```csharp
using Bodu.Text.Configuration;

string editorConfig = """
root = true

[*]
indent_style = space

[*.cs]
indent_size = 4

[*.{md,json}]
indent_size = 2
""";

IniDocument doc = ConfigurationDocument.Parse(editorConfig);

ConfigurationView csharp = doc.Resolve("src/Foo.cs");
csharp["indent_size"];   // "4"   — from [*.cs]
csharp["indent_style"];  // "space" — from [*]

ConfigurationView json = doc.Resolve("appsettings.json");
json["indent_size"];     // "2"   — from [*.{md,json}]
```

When two matching sections set the same key, the *later* section wins — same merge order as the EditorConfig specification.

When `targetPath` is `null`, every section contributes; this is the right mode for typical INI configuration where sections are namespaces, not globs.

## Pattern 3 — typed value getters

```csharp
using Bodu.Text.Configuration;

ConfigurationView view = document.Resolve();

string  name    = view.GetString("appName");
int     port    = view.GetInt32("server:port");
bool    verbose = view.GetBoolean("logging:verbose");
TimeSpan ttl    = view.GetValue<TimeSpan>("cache:ttl");
LogLevel level  = view.GetEnum<LogLevel>("logging:level");
```

`GetValue<T>(key)` parses via `ISpanParsable<T>` under `CultureInfo.InvariantCulture`. The specialised getters — `GetInt32`, `GetInt64`, `GetBoolean`, `GetEnum<T>` — are sugar with predictable error behaviour:

- All of them throw `KeyNotFoundException` when the key is missing.
- `GetBoolean` accepts only `true` / `false` case-insensitively — `"1"` is **not** a boolean (EditorConfig semantics).
- `GetEnum<T>` parses case-insensitively by name and rejects undefined values via `Enum.IsDefined`.

The `Try…` variants and fallback overloads return `false` / the fallback instead of throwing:

```csharp
int port = view.GetInt32("server:port", fallback: 8080);

if (view.TryGetString("logging:level", out string? level))
    Configure(level!);
```

## Pattern 4 — enumerate the resolved view

```csharp
using Bodu.Text.Configuration;

ConfigurationView view = document.Resolve();

Console.WriteLine($"{view.Count} key(s) resolved");

foreach (KeyValuePair<string, string?> kv in view)
    Console.WriteLine($"  {kv.Key} = {kv.Value}");

foreach (ConfigurationResolvedEntry entry in view.Entries)
    Console.WriteLine($"  {entry.Path} = {entry.Value}  (from {entry.SourceSection})");
```

`Values` returns the resolved dictionary; `Keys`, `Count`, and `GetEnumerator()` mirror the standard read-only collection contract. `Entries` exposes the richer `ConfigurationResolvedEntry` view — same keys, same values, but with provenance metadata so consumers can trace a resolved key back to its originating section.

## Pattern 5 — resolve options

```csharp
using Bodu.Text.Configuration;

ConfigurationView view = document.Resolve(
    targetPath: "src/Foo.cs",
    options: ConfigurationResolveOptions.EditorConfigCompatible);
```

The default options (`ConfigurationResolveOptions.Bodu`) treat the global section's properties as participating preamble and the literal value `"unset"` as a normal string. The `EditorConfigCompatible` preset switches both — preamble properties are dropped from the resolved view, and `"unset"` removes the effective value the way EditorConfig requires.

Every field of `ConfigurationResolveOptions`:

| Field | Default | Effect |
|---|---|---|
| `Profile` | `Bodu` | Selects the cohort of resolve defaults. |
| `PathRoot` | `null` | Optional path root for anchored globs; null defers to the document's load path. |
| `MissingPathRootMode` | `UseEmptyRoot` | Behaviour when no root is available — `UseEmptyRoot` (patterns without `/` match at any depth) or `Throw`. |
| `ApplyPreambleProperties` | `true` (Bodu) / `false` (EditorConfig) | Whether the global section contributes. |
| `PathComparison` | `Ordinal` | Comparison used to match the target path against globs. |
| `UnsetValueMode` | `TreatAsLiteral` (Bodu) / `RemoveEffectiveValue` (EditorConfig) | How the literal value `"unset"` is treated. |
| `KeyOptions` | `Default` | Segment-separator and mapping config (see below). |

## `ConfigurationKey` and `ConfigurationKeyOptions`

`ConfigurationKey` is the parsed form of a key — both the raw authored shape (`"logging.console.level"`) and the canonical colon-delimited path (`"logging:console:level"`). It is a readonly struct used internally by the view and exposed for code that needs to manipulate keys explicitly.

```csharp
using Bodu.Text.Configuration;

ConfigurationKey key = ConfigurationKey.Parse("logging.console.level");

key.RawKey;       // "logging.console.level"
key.Path;         // "logging:console:level"
key.Segments;     // ["logging", "console", "level"]
key.CaseSensitive;// false
```

The behaviour is governed by `ConfigurationKeyOptions`:

| Field | Default | Effect |
|---|---|---|
| `SegmentSeparators` | `{ '.', ':' }` | Characters recognised as path separators. |
| `Mapping` | `DotToColon` | Raw-to-canonical mapping — `DotToColon`, `Colon` (assume already colon-delimited), or `Identity` (no transformation). |
| `CaseSensitive` | `false` | Case-sensitive comparison (the default `false` matches `Microsoft.Extensions.Configuration`). |
| `AllowEmptySegments` | `false` | Permit empty segments like `a..b`. |

The static `ConfigurationKeyOptions.Default` is the cached default.

## `ConfigurationPattern` — EditorConfig globs

When the view is resolved with a `targetPath`, section names are compiled to `ConfigurationPattern` and matched against the path:

```csharp
using Bodu.Text.Configuration;

ConfigurationPattern p = ConfigurationPattern.Compile("**/*.{md,json}");

p.IsMatch("docs/getting-started.md");      // True
p.IsMatch("src/appsettings.json");         // True
p.IsMatch("src/Foo.cs");                   // False
```

The glob grammar:

| Token | Meaning |
|---|---|
| `*` | Any character except `/`. |
| `**` | Any sequence including `/`. |
| `?` | A single character except `/`. |
| `{a,b,c}` | Alternation (nesting allowed). |
| `{n1..n2}` | Inclusive integer range. |
| `[seq]` / `[!seq]` | Character set or its complement. |
| `\` | Escape the next character. |

Patterns without `/` match at any depth. Patterns with `/` anchor to the start of the path. The grammar is the EditorConfig 0.17.2 specification verbatim, plus a bounded process-wide pattern cache so the same pattern compiled twice does not recompile.

`Compile(pattern, StringComparison)` accepts an explicit comparison; the default `Ordinal` matches the EditorConfig requirement that paths be case-sensitive on case-sensitive file systems.

## When *not* to use `ConfigurationView`

- **You only need to read or write the INI bytes.** Reach for [`Bodu.Text.Formats.Ini`](../formats/ini.md) directly — `ConfigurationView` is the projection layer; you do not need it for codec-only use.
- **You need `IConfiguration` integration.** Reach for [`Bodu.Extensions.Configuration.Text`](../extensions-configuration-text/index.md), which bridges `ConfigurationView` into `IConfigurationBuilder` so the rest of the standard configuration pipeline works unchanged.
- **You need diagnostics at parse time.** That work lives in [`ConfigurationDocument.ParseWithDiagnostics`](parsing-and-profiles.md#pattern-4--collect-diagnostics-instead-of-throwing) — `Resolve` runs against a successfully parsed document.

## See also

- [Parsing and profiles](parsing-and-profiles.md) — the parse half of the surface.
- [Diagnostics](diagnostics.md) — the diagnostic-code catalogue.
- [`Bodu.Text.Configuration` API reference](~/apidoc/Bodu.Text.Configuration.md).
- [`Bodu.Extensions.Configuration.Text`](../extensions-configuration-text/index.md) — `IConfiguration` bridge.
