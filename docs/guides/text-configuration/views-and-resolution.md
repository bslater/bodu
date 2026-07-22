---
title: Views and resolution
---

# Views and resolution

A parsed `ConfigurationDocument` is the codec's faithful representation — sections, entries, comments, line numbers. The *view* layer takes that document and projects it into the shape application code wants to consume: dotted-or-colon-delimited paths flattened across sections, EditorConfig glob sections resolved against a target path, typed value getters, and missing-key fallbacks.

This guide covers `ConfigurationView`, `ConfigurationResolveOptions`, `ConfigurationKey`, `ConfigurationKeyOptions`, and `ConfigurationPattern`. For the parse side — `ConfigurationDocument.Parse`, profiles, parse options — see [Parsing and profiles](parsing-and-profiles.md). For the diagnostic catalogue — every `ConfigurationDiagnosticCode` value — see [Diagnostics](diagnostics.md).

## Pattern 1 — resolve a document to a view

A section header is a **glob pattern matched against a target path**, not a namespace. To pull a section's keys into the view you must supply the path you are resolving for; the section's glob is then matched against it:

```csharp
using Bodu.Text.Configuration;

string source = """
appName = Bodu.Sample

[*.log]
level = Information
verbose = true
""";

ConfigurationDocument document = ConfigurationDocument.Parse(source);

ConfigurationView view = document.Resolve("app/server.log");

string? name = view["appName"];          // "Bodu.Sample" — from the preamble
string? lvl  = view["level"];            // "Information" — from [*.log], which matches "app/server.log"
string? same = view["LEVEL"];            // "Information" — lookups are case-insensitive by default
```

`Resolve(targetPath?)` is an extension method on `IniDocumentBase` (so it works on both `ConfigurationDocument` and `IniDocument`) from <xref:Bodu.Text.Configuration.ConfigurationExtensions>. It layers the preamble first, then every section whose glob matches the target path in source order, projecting each raw key into a colon-delimited path via <xref:Bodu.Text.Configuration.ConfigurationKey> (dotted-to-colon by default), and produces a <xref:Bodu.Text.Configuration.ConfigurationView> queryable with either notation.

> [!IMPORTANT]
> With **no** target path — `document.Resolve()` or `Resolve(null)` — every named section is skipped and the view contains only the preamble. This is the single most common surprise: an INI file whose sections are intended as namespaces will resolve to an empty-but-for-preamble view unless you treat the section names as globs and pass a matching target path, or read the sections directly off the document (`document.Sections[i]["key"]`) instead of resolving.

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

ConfigurationDocument doc = ConfigurationDocument.Parse(editorConfig);

ConfigurationView csharp = doc.Resolve("src/Foo.cs");
csharp["indent_size"];   // "4"   — from [*.cs]
csharp["indent_style"];  // "space" — from [*]

ConfigurationView json = doc.Resolve("appsettings.json");
json["indent_size"];     // "2"   — from [*.{md,json}]
```

When two matching sections set the same key, the *later* section wins — same merge order as the EditorConfig specification. Here both `[*]` and `[*.cs]` match `src/Foo.cs`, so `indent_style` comes from `[*]` and `indent_size` from the later, more specific `[*.cs]`.

When `targetPath` is `null`, **no** section contributes — only the preamble does (see the admonition in Pattern 1). For typical INI configuration where sections are namespaces rather than globs, read the sections directly off the parsed `ConfigurationDocument` (it inherits the read-only `IniDocumentBase` surface: `document.Sections`, `section.Entries`, `section["key"]`) instead of resolving against a target path.

## Pattern 3 — typed value getters

```csharp
using Bodu.Text.Configuration;

ConfigurationView view = document.Resolve("src/Foo.cs");

string   name    = view.GetString("appName");
int      port    = view.GetInt32("server:port");
long     maxSize = view.GetInt64("limits:max:bytes");
bool     verbose = view.GetBoolean("logging:verbose");
TimeSpan ttl     = view.GetValue<TimeSpan>("cache:ttl");
Guid     tenant  = view.GetValue<Guid>("tenant:id");
LogLevel level   = view.GetEnum<LogLevel>("logging:level");
```

`GetValue<T>(key)` parses any `ISpanParsable<T>` under `CultureInfo.InvariantCulture`. The specialised getters — `GetInt32`, `GetInt64`, `GetBoolean`, `GetEnum<T>` — are sugar with predictable error behaviour:

- All of them throw `KeyNotFoundException` when the key is missing and `FormatException` when the value is present but unparseable.
- `GetInt32` / `GetInt64` parse with `NumberStyles.Integer` (a leading sign and surrounding whitespace, no thousands separators or decimal point).
- `GetBoolean` accepts only `true` / `false` case-insensitively — `"yes"`, `"on"`, and `"1"` are **not** booleans (EditorConfig semantics).
- `GetEnum<T>` parses case-insensitively by name and rejects undefined integers and unlisted combined-flag values via `Enum.IsDefined`.

The `Try…` variants and fallback overloads soften the missing-key case — but they differ on malformed values:

```csharp
int port = view.GetInt32("server:port", fallback: 8080);

if (view.TryGetString("logging:level", out string? level))
    Configure(level!);
```

> [!IMPORTANT]
> A `fallback` overload (`GetInt32(key, 8080)`, `GetValue<T>(key, fallback)`, `GetEnum(key, fallback)`) only substitutes the fallback when the **key is absent**. A key that is present but holds a malformed value still throws `FormatException` — fallbacks never silently swallow bad data. Only the `TryGetXxx` family is fully non-throwing on a parse failure: it returns `false` for both a missing key *and* an unparseable value. Use `TryGetXxx` when the source text is untrusted and you want per-key error handling.

## Pattern 4 — enumerate the resolved view

```csharp
using Bodu.Text.Configuration;

ConfigurationView view = document.Resolve("src/Foo.cs");

Console.WriteLine($"{view.Count} key(s) resolved");

foreach (KeyValuePair<string, string?> kv in view)
    Console.WriteLine($"  {kv.Key} = {kv.Value}");

foreach (ConfigurationResolvedEntry entry in view.Entries)
    Console.WriteLine($"  {entry.Key} = {entry.Value}  (from {entry.SectionPattern ?? "<preamble>"})");
```

`Values` returns the resolved dictionary; `Keys`, `Count`, and `GetEnumerator()` mirror the standard read-only collection contract, and enumeration yields keys in canonical colon-delimited form. `Entries` exposes the richer <xref:Bodu.Text.Configuration.ConfigurationResolvedEntry> view — same keys, same values, but with provenance: `Key`, `Value`, `SectionPattern` (the winning section's glob, or `null` for the preamble), and `SourceLocation`. Fetch a single key's provenance with `view.GetEntry("format:indent:size")`.

> [!NOTE]
> `ConfigurationView` also implements `IReadOnlyDictionary<string, string?>`, but its indexer deviates from the dictionary contract: `view["absent:key"]` returns `null` rather than throwing `KeyNotFoundException`, matching `Microsoft.Extensions.Configuration`'s null-on-absent convention. Use `view.ContainsKey(key)` to distinguish an absent key from one whose value is `null`.

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
| `PathRoot` | `null` | Optional anchor that anchored globs are rebased against; `null` defers to the document's load path. |
| `MissingPathRootMode` | `UseEmptyRoot` | Behaviour for a **path-less** resolve when no root is available — `UseEmptyRoot` returns a preamble-only view; `Throw` raises `InvalidOperationException`. No effect once a target path is supplied. |
| `ApplyPreambleProperties` | `true` (Bodu/Strict/Relaxed) / `false` (EditorConfig) | Whether the global section contributes to the view. |
| `PathComparison` | `Ordinal` | `StringComparison` used to match the target path against globs (case-insensitive variants compile the regex with `IgnoreCase`). |
| `UnsetValueMode` | `TreatAsLiteral` (Bodu/Relaxed) / `RemoveEffectiveValue` (EditorConfig/Strict) | How a value equal to `"unset"` (case-insensitive) is treated. |
| `KeyOptions` | `Default` | Segment-separator and mapping config (see below). Should match `ConfigurationParseOptions.KeyOptions`. |

Static presets are thinner here than on the parse side: only `ConfigurationResolveOptions.Bodu` and `ConfigurationResolveOptions.EditorConfigCompatible` exist as named properties. For `Strict` or `Relaxed` resolve defaults, call `ConfigurationResolveOptions.For(ConfigurationProfile.Strict)`.

### How `PathRoot` rebases the target

Before matching, the resolver normalises the target path to forward slashes and makes it relative to `PathRoot`: a `PathRoot` prefix (plus its trailing `/`) is stripped, an exact match collapses to the filename, and anything else is matched as-authored. This lets an anchored glob like `[src/**/*.cs]` match an absolute file path:

```csharp
var options = new ConfigurationResolveOptions { PathRoot = "/repo/my-app" };

// "/repo/my-app/src/svc/Foo.cs" → relative "src/svc/Foo.cs" → matches [src/**/*.cs]
ConfigurationView view = document.Resolve("/repo/my-app/src/svc/Foo.cs", options);
```

When a document is loaded with `ConfigurationDocument.Load(path)`, its originating directory is recorded and used as the implicit `PathRoot`, so anchored globs resolve against the right base without setting `PathRoot` explicitly. Documents parsed from a string or stream carry no path context — set `PathRoot` yourself when your globs are anchored.

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

- **You only need to read or write the INI bytes.** Reach for [`Bodu.Text.Ini`](../formats/ini.md) directly — `ConfigurationView` is the projection layer; you do not need it for codec-only use.
- **You need `IConfiguration` integration.** Reach for [`Bodu.Extensions.Configuration.Text`](../extensions-configuration-text/index.md), which bridges `ConfigurationView` into `IConfigurationBuilder` so the rest of the standard configuration pipeline works unchanged.
- **You need diagnostics at parse time.** That work lives in [`ConfigurationDocument.ParseWithDiagnostics`](parsing-and-profiles.md#pattern-4--collect-diagnostics-instead-of-throwing) — `Resolve` runs against a successfully parsed document.

## See also

- [Parsing and profiles](parsing-and-profiles.md) — the parse half of the surface.
- [Diagnostics](diagnostics.md) — the diagnostic-code catalogue.
- [`Bodu.Text.Configuration` API reference](xref:Bodu.Text.Configuration).
- [`Bodu.Extensions.Configuration.Text`](../extensions-configuration-text/index.md) — `IConfiguration` bridge.
- **[Configuration guides](../topics/configuration.md)** — every guide in this topic, across Bodu.Text.Configuration and Bodu.Extensions.Configuration.Text.
