---
title: Configuration — Overview
---

# Configuration

The **Configuration** topic covers two packages that together provide layered, EditorConfig-style configuration for .NET applications. [`Bodu.Text.Configuration`](../text-configuration/index.md) reads a single text file in the familiar INI / EditorConfig shape — preamble, glob-anchored sections, `key = value` properties — and projects it into a flattened, target-aware view of colon-delimited configuration keys. [`Bodu.Extensions.Configuration.Text`](../extensions-configuration-text/index.md) bridges that view into the `Microsoft.Extensions.Configuration` world: an <xref:Microsoft.Extensions.Configuration.IConfigurationBuilder> source alongside JSON and environment variables, `IOptions<T>` binding, and reload-on-change.

The split is deliberate. The parser, resolver, and typed view carry no dependency on `Microsoft.Extensions.Configuration`, so console tools, analyzers, and build tasks can consume configuration documents directly. The bridge is a thin host that any `Microsoft.Extensions`-based application adds on top — its overload set mirrors `AddJsonFile` / `AddJsonStream`, so call sites stay familiar.

A configuration file in this model is *not* a snapshot of a single object graph — it is a layered description of how properties change as a target path moves through a directory tree. The libraries' job is to collapse those layers down to the right answer for a specific target.

## The pipeline

Configuration flows through five stages; every stage past the parse is opt-in.

| Stage | Performed by | Produces |
|---|---|---|
| **Document model** | The INI codec in [`Bodu.Text.Formats`](../formats/index.md) | An immutable <xref:Bodu.Text.Ini.IniDocument> — sections, entries, comments, ordering preserved. |
| **Profile-validated parse** | <xref:Bodu.Text.Configuration.ConfigurationDocument> with <xref:Bodu.Text.Configuration.ConfigurationParseOptions> | A `ConfigurationDocument`, optionally paired with diagnostics via `ParseWithDiagnostics`. |
| **Layered resolution** | `Resolve(targetPath)` with <xref:Bodu.Text.Configuration.ConfigurationResolveOptions> | A <xref:Bodu.Text.Configuration.ConfigurationView> — preamble plus matching glob-anchored sections, layered last-wins. |
| **Typed access** | The view's getter family | `GetString`, `GetInt32`, `GetBoolean`, `GetEnum<T>`, and `GetValue<T>` for any `ISpanParsable<T>`. |
| **Microsoft.Extensions bridge** *(optional)* | <xref:Bodu.Extensions.Configuration.Text.TextConfigurationSource> / <xref:Bodu.Extensions.Configuration.Text.TextConfigurationProvider> | Colon-delimited keys in `IConfiguration`, section binding to `IOptions<T>`, reload tokens. |

Parse without resolving when you only want the document; resolve without typed accessors when you only need raw strings; skip the bridge entirely when you do not host in `Microsoft.Extensions`.

Behaviour at each stage is governed by a **profile** — a named, validated combination of parse, resolve, and write options. Four ship in the box: `Bodu` (the permissive default), `EditorConfigCompatible` (strict alignment with EditorConfig 0.17.2), `Strict` (deterministic parsing for generated files), and `Relaxed` (collect diagnostics from user-authored files instead of throwing). See [Configuration concepts](configuration-concepts.md) for one-line definitions of each.

## One file, end to end

The same source text serves both packages. A file in the EditorConfig shape:

```ini
# Preamble — properties that apply before any section opens.
root = true
service.name = Bodu.Sample

# A section header is a glob pattern matched against the target path.
[*.cs]
format.indent.style = space
format.indent.size  = 4

# Later sections override earlier sections for any path both match.
[src/**/*.cs]
format.indent.size = 2
logging.level.default = Warning
```

Reading it directly with `Bodu.Text.Configuration`:

```csharp
using Bodu.Text.Configuration;

ConfigurationDocument document = ConfigurationDocument.Load(".boduconfig");
ConfigurationView view = document.Resolve("src/MyApp/Program.cs");

int indent = view.GetInt32("format:indent:size");          // 2 — the src/** section won
string level = view.GetString("logging:level:default");    // "Warning"
```

Or surfacing it through `Microsoft.Extensions.Configuration` with the bridge:

```csharp
using Bodu.Extensions.Configuration.Text;
using Microsoft.Extensions.Configuration;

IConfiguration configuration = new ConfigurationBuilder()
    .AddTextConfigurationFile(".boduconfig", targetPath: "src/MyApp/Program.cs")
    .Build();

string? level = configuration["logging:level:default"];    // "Warning"
```

Dotted keys (`logging.level.default`) project to the canonical colon-delimited form (`logging:level:default`) under the default `DotToColon` key mapping, which is exactly the shape `IConfiguration` consumes — the bridge adds no translation layer of its own.

## The packages

| Package | Status | What it provides | Docs |
|---|---|---|---|
| `Bodu.Text.Configuration` | Stable | The parser, profiles, layered resolver, typed `ConfigurationView`, key model, diagnostics, and round-trip `Save`. Built on the INI document model from `Bodu.Text.Formats`; no `Microsoft.Extensions` dependency. | [Intro](../text-configuration/index.md) · [Concepts](../text-configuration/concepts.md) · [Get started](../text-configuration/getting-started.md) |
| `Bodu.Extensions.Configuration.Text` | Stable | The `Microsoft.Extensions.Configuration` bridge: `AddTextConfigurationFile` / `AddTextConfigurationStream`, the conventional `.boduconfig` → `bodu.config` probe, reload-on-change, and `AddConfigurationOptions<T>` binding. | [Intro](../extensions-configuration-text/index.md) · [Concepts](../extensions-configuration-text/concepts.md) · [Get started](../extensions-configuration-text/getting-started.md) |

### Boundaries

- **Use raw `Ini` from [`Bodu.Text.Formats`](../formats/index.md) instead** when you just need to read or edit an INI file with no layering, no profiles, and no glob resolution. `ConfigurationDocument` inherits the same <xref:Bodu.Text.Ini.IniDocumentBase> model, so promoting an `IniDocument` workflow to the configuration layer later is incremental, not a rewrite.
- **Skip the bridge when you don't host in `Microsoft.Extensions`.** `Bodu.Text.Configuration` is self-sufficient — `Parse`, `Resolve`, and the typed getters cover the full read path without an `IConfigurationBuilder` in sight.
- **The bridge is not a general INI provider.** It exists specifically to surface profile-parsed, target-resolved Bodu configuration documents; for plain key-value INI in `IConfiguration`, the stock `Microsoft.Extensions.Configuration.Ini` provider may be all you need.

## Choosing an entry point

| Scenario | Reach for | Notes |
|---|---|---|
| Parse a configuration file and read typed values | `ConfigurationDocument.Parse(text)` → `doc.Resolve(targetPath)` → `view.GetInt32(...)` | The minimal happy path; defaults to the `Bodu` profile. |
| Resolve per-file settings the EditorConfig way | `doc.Resolve("src/Foo.cs")` with `ConfigurationParseOptions.EditorConfigCompatible` | Section headers are glob patterns matched against the target path; later matches win. |
| Surface every problem in a user-authored file at once | `ConfigurationDocument.ParseWithDiagnostics(text, ConfigurationParseOptions.Relaxed)` | Diagnostics collect instead of throwing; the valid portions of the document remain usable. |
| Reject any input the parser cannot prove canonical | `ConfigurationParseOptions.Strict` | Duplicate keys are disallowed; suited to generated files. |
| Round-trip a document through save | `ConfigurationDocument.Save(doc, path)` | Comments, section ordering, and property ordering are preserved. |
| Feed the file into ASP.NET Core / Generic Host configuration | `builder.AddTextConfigurationFile(".boduconfig")` | Keys surface in the colon-delimited form `IConfiguration` consumes. |
| Bind a section to a POCO with `IOptions<T>` | `services.AddConfigurationOptions<MyOptions>(configuration, "section")` | A discoverability shim over the standard `Configure<T>` shape. |
| Hot-reload settings when the file changes | `AddTextConfigurationFile(..., reloadOnChange: true)` | File watcher + standard reload tokens; `IOptionsMonitor<T>` re-binds automatically. |
| Test fixtures or embedded resources | `builder.AddTextConfigurationStream(stream)` | One-shot; no reload-on-change. |
| Plain INI editing with no layering | [`Bodu.Text.Formats`](../formats/index.md) `IniDocument` | The configuration layer is unnecessary overhead for that case. |

## Install

```bash
dotnet add package Bodu.Text.Configuration
dotnet add package Bodu.Extensions.Configuration.Text
```

`Bodu.Extensions.Configuration.Text` depends on `Bodu.Text.Configuration`, so applications that host in `Microsoft.Extensions` need only the second command.

## Where to go next

- **[Configuration concepts](configuration-concepts.md)** — the cross-package vocabulary: profiles, preamble, glob-anchored sections, layered resolution, views, sources, providers, reload tokens.
- **[Bodu.Text.Configuration introduction](../text-configuration/index.md)** — the parser, resolver, and view model in detail.
- **[Bodu.Text.Configuration getting started](../text-configuration/getting-started.md)** — install + minimal samples for parse-resolve-read, profile presets, diagnostics, round-trip save.
- **[Bodu.Extensions.Configuration.Text introduction](../extensions-configuration-text/index.md)** — the `IConfigurationBuilder` integration.
- **[Bodu.Extensions.Configuration.Text getting started](../extensions-configuration-text/getting-started.md)** — install + minimal samples for the file overload, the stream overload, the conventional probe, and options binding.
- **[Configuration guides](../../guides/topics/configuration.md)** — recipe-style walk-throughs across both packages.
- **API reference:** [Bodu.Text.Configuration](xref:Bodu.Text.Configuration) · [Bodu.Extensions.Configuration.Text](xref:Bodu.Extensions.Configuration.Text)
