---
title: Bodu.Extensions.Configuration.Text — Core concepts
---

# Bodu.Extensions.Configuration.Text — Core concepts

This page is the vocabulary the rest of the documentation assumes. Read it once before the
[getting-started samples](getting-started.md), and refer back whenever a term feels imprecise.

For the high-level shape of the library, start with the [introduction](index.md).

## Source vs provider vs loader

The library follows the three-part contract every `Microsoft.Extensions.Configuration` provider uses:

| Role | Type | Responsibility |
|---|---|---|
| **Source** | <xref:Bodu.Extensions.Configuration.Text.TextConfigurationSource>, <xref:Bodu.Extensions.Configuration.Text.TextStreamConfigurationSource> | Holds the configuration of "what to load and how" — the path, target path, parse and resolve options, reload behaviour. Implements `Build(IConfigurationBuilder)`. |
| **Provider** | <xref:Bodu.Extensions.Configuration.Text.TextConfigurationProvider>, <xref:Bodu.Extensions.Configuration.Text.TextStreamConfigurationProvider> | Performs the actual load. Subclasses `FileConfigurationProvider` / `StreamConfigurationProvider`; inherits change-token plumbing. Populates the inherited `Data` dictionary. |
| **Loader** | <xref:Bodu.Extensions.Configuration.Text.TextConfigurationLoader> | Internal helper that parses a stream into an <xref:Bodu.Text.Ini.IniDocument>, resolves it, and flattens the view into `Dictionary<string, string?>` for the provider. |

Both providers share the same loader, so behaviour stays in lockstep across file and stream backings.

## File vs stream source

The two source types correspond to the two file shapes Microsoft ships:

| Source | Backing | Reload-on-change |
|---|---|---|
| <xref:Bodu.Extensions.Configuration.Text.TextConfigurationSource> | Path resolved through an `IFileProvider` | Yes |
| <xref:Bodu.Extensions.Configuration.Text.TextStreamConfigurationSource> | Arbitrary `System.IO.Stream` | No |

File sources are the common case — they layer naturally with `appsettings.json`, support hot reload, and accept the
same path-based options every provider does. Stream sources are useful for tests, embedded resources, or in-memory
fixtures where the configuration data does not live on disk.

## Target path

`TargetPath` is the value the source hands to `ConfigurationDocument.Resolve(targetPath)`. It anchors glob
matching for sections whose headers contain path separators.

```csharp
builder.AddConfiguration(
    "team.boduconfig",
    targetPath: "src/MyApp/Program.cs");
```

When the file says

```ini
[src/**/*.cs]
logging.level.default = Warning
```

the `[src/**]` pattern matches `src/MyApp/Program.cs`, so the resolved view picks up that section's properties on top
of any earlier matches. When `TargetPath` is `null`, only unanchored patterns and preamble values contribute.

`TargetPath` is **per source** — different sources in the same builder can have different anchors. If your application
needs configuration evaluated for several paths in parallel, add several sources with the same `Path` but different
`TargetPath` values.

## Parse and resolve option propagation

A source carries two optional bags:

| Source property | Drives |
|---|---|
| `ParseOptions` | The <xref:Bodu.Text.Configuration.ConfigurationParseOptions> passed to the reader. Controls inline-comment mode, duplicate handling, diagnostic mode, length limits. |
| `ResolveOptions` | The <xref:Bodu.Text.Configuration.ConfigurationResolveOptions> passed to the resolver. Controls `PathRoot`, `MissingPathRootMode`, `UnsetValueMode`, `PathComparison`. |

Both default to `null`, in which case the library uses
<xref:Bodu.Text.Configuration.ConfigurationParseOptions.Bodu> and
<xref:Bodu.Text.Configuration.ConfigurationResolveOptions.Bodu>. Set them per-source when one file in a builder
needs different semantics — for example, an EditorConfig-strict file alongside a Bodu-permissive one.

```csharp
builder.AddConfiguration(src =>
{
    src.Path = ".editorconfig";
    src.ParseOptions   = ConfigurationParseOptions.EditorConfigCompatible;
    src.ResolveOptions = ConfigurationResolveOptions.EditorConfigCompatible;
    src.TargetPath     = "src/Foo.cs";
});
```

## Conventional file probe

The no-argument overload of `AddConfiguration` is a convenience helper for the common "drop a file in the project
root" pattern:

```csharp
builder.AddConfiguration(optional: true, reloadOnChange: true);
```

The probe runs against the builder's default file provider and looks for two file names in order:

1. **`.boduconfig`** — the dotfile form, common in version-control-friendly repos.
2. **`bodu.config`** — the plain form, common on Windows where dotfiles need explicit attribute toggles.

The first file found is added; if neither is present and `optional` is `true`, the helper returns the builder
unchanged. The probe is cheap (it consults `IFileProvider.GetFileInfo(name).Exists`) and runs once per `Build` call.

## Reload-on-change

`FileConfigurationSource.ReloadOnChange` is inherited as-is. When `true`, the provider:

1. Attaches a file watcher via the source's `IFileProvider`.
2. On a change notification, reparses the file and rebuilds the resolved view.
3. Triggers the standard `IConfiguration` reload tokens, so callers using `IOptionsMonitor<TOptions>` re-bind to the
   new values automatically.

Reload is **not** atomic with respect to multiple providers — if your builder has three file sources and two change
at once, the providers reload independently, in the order their watchers fire. This matches the MEC contract and is
not specific to Bodu.

The stream source does not support reload. The stream is parsed once when `Build` is called; the stream's lifetime
ends with that parse. If you need dynamic stream-backed inputs, rebuild the configuration.

## File provider precedence

`AddConfiguration` resolves the `IFileProvider` in the standard MEC order:

1. If a provider is supplied directly to the overload, use it.
2. Otherwise, if the source's `FileProvider` is set, use that.
3. Otherwise, defer to the builder's default file provider — typically a `PhysicalFileProvider` rooted at
   `Directory.GetCurrentDirectory()`.

Tests typically supply a `PhysicalFileProvider` rooted at a temp directory; production code typically relies on the
builder default.

## Options binding

<xref:Bodu.Extensions.Configuration.Text.ConfigurationOptionsExtensions> wraps the standard
`services.Configure<TOptions>(...)` shape:

```csharp
services.AddConfigurationOptions<ServiceOptions>(configuration, "service");
// equivalent to:
services.Configure<ServiceOptions>(configuration.GetSection("service"));
```

The wrapper exists for discoverability — call sites that reach for an `AddConfiguration*` API by IntelliSense
find an options helper with the same prefix. The shape is identical to the MEC version; callers who already use
`Configure<T>` are not penalised, and callers who switch to `AddConfigurationOptions` are not locked in.

The two overloads:

| Overload | Use when |
|---|---|
| `AddConfigurationOptions<T>(services, configuration, sectionName)` | Section is identified by colon-delimited name in an `IConfiguration` root. |
| `AddConfigurationOptions<T>(services, section)` | Section is already projected via `configuration.GetSection(...)`. |

Both throw `ArgumentNullException` for null `services` / `configuration` / `section`; the name-based overload throws
`ArgumentException` for an empty or whitespace section name.

## Colon-delimited key model

`IConfiguration` keys are colon-delimited by convention — `service:name`, `logging:level:default`. Bodu's reader
projects raw keys to the same shape under the default `DotToColon` mapping, so a file written as

```ini
service.name = Bodu
service.port = 8080
logging.level.default = Information
```

surfaces as `configuration["service:name"]`, `configuration["service:port"]`,
`configuration["logging:level:default"]`. Switching to `ConfigurationKeyMapping.Identity` preserves the original
delimiter — useful when the file is also consumed by tools that interpret dots as path separators (e.g. AppSettings
patches).

## Where to go next

- **[Getting started](getting-started.md)** — install + runnable minimal samples.
- **[Bodu.Text.Configuration](../text-configuration/index.md)** — the underlying parser, resolver, and view.
- **[Bodu.Extensions.Configuration.Text API reference](../../apidoc/Bodu.Extensions.Configuration.Text.md)** — full type-by-type docs.
- **[Introduction](index.md)** — the high-level shape of the library.
