---
title: Configuration sources
---

# Configuration sources

`Bodu.Extensions.Configuration.Text` bridges [`Bodu.Text.Configuration`](../text-configuration/index.md) into `Microsoft.Extensions.Configuration`. Add a Bodu-formatted INI file or stream to an `IConfigurationBuilder`, and the parsed and resolved view becomes available through the standard `IConfiguration` surface that ASP.NET Core, Generic Host, and the rest of the BCL configuration pipeline already consume.

## Pattern 1 — file-backed source with reload-on-change

```csharp
using Bodu.Extensions.Configuration.Text;
using Microsoft.Extensions.Configuration;

IConfiguration configuration = new ConfigurationBuilder()
    .AddConfiguration("appsettings.ini", optional: false, reloadOnChange: true)
    .Build();

string? appName = configuration["appName"];
string? level   = configuration["logging:level"];
```

`AddConfiguration(path, …)` registers a `TextConfigurationSource`. The provider uses the host's default file provider, watches for changes when `reloadOnChange: true`, and re-resolves the view when the file is rewritten. The dotted keys in the INI source flatten through `ConfigurationKeyOptions.Default` so `logging.level.default = …` is reachable as `"logging:level:default"` — the canonical colon-delimited form `IConfiguration` consumers expect.

## Pattern 2 — explicit file provider

```csharp
using Bodu.Extensions.Configuration.Text;
using Microsoft.Extensions.FileProviders;

IConfiguration configuration = new ConfigurationBuilder()
    .AddConfiguration(
        provider: new PhysicalFileProvider("/etc/myapp"),
        path: "config.ini",
        targetPath: null,
        optional: false,
        reloadOnChange: true)
    .Build();
```

Pass an `IFileProvider` to read from a specific physical or embedded location rather than the builder's default file provider. The `targetPath` argument, when supplied, enables EditorConfig glob anchoring — sections become globs that the resolver matches against the target path. See [Views and resolution](../text-configuration/views-and-resolution.md#pattern-2--anchored-editorconfig-globs).

## Pattern 3 — convention-based discovery

```csharp
using Bodu.Extensions.Configuration.Text;

IConfiguration configuration = new ConfigurationBuilder()
    .AddConfiguration(optional: true, reloadOnChange: false)
    .Build();
```

The parameterless overload probes for `.boduconfig` first and then `bodu.config` in the builder's base path. With `optional: true` (the default) the call is a no-op when neither file is present; with `optional: false` it throws `FileNotFoundException`.

## Pattern 4 — stream-backed source

```csharp
using Bodu.Extensions.Configuration.Text;
using Bodu.Text.Configuration;

using var ms = new MemoryStream(Encoding.UTF8.GetBytes(iniText));

IConfiguration configuration = new ConfigurationBuilder()
    .AddConfiguration(
        stream: ms,
        targetPath: null,
        parseOptions: ConfigurationParseOptions.Relaxed,
        resolveOptions: ConfigurationResolveOptions.EditorConfigCompatible)
    .Build();
```

`AddConfiguration(Stream, …)` registers a `TextStreamConfigurationSource`. Unlike file-backed sources, stream sources do not support reload-on-change — the stream is consumed once during `Build()`. Use the stream overload when the configuration comes from a network resource, an embedded resource, or anywhere else that cannot be expressed as a file path.

## Pattern 5 — pre-parsed document

```csharp
using Bodu.Extensions.Configuration.Text;
using Bodu.Text.Configuration;
using Bodu.Text.Ini;

IniDocument doc = ConfigurationDocument.Parse(iniText);

// Mutate the document programmatically.
doc.GetOrAddSection("logging").SetEntry("level", "Debug");

IConfiguration configuration = new ConfigurationBuilder()
    .AddConfiguration(doc, targetPath: null)
    .Build();
```

When you already have an `IniDocument` in hand — built programmatically, mutated at runtime, or shared across multiple builders — pass it directly. The bridge flattens the resolved view into an in-memory collection before handing it to the configuration root, so the source is captured by value; later mutations to the document do not flow into the configuration.

## Pattern 6 — fluent source configuration

```csharp
using Bodu.Extensions.Configuration.Text;

IConfiguration configuration = new ConfigurationBuilder()
    .AddConfiguration(source =>
    {
        source.Path           = "appsettings.ini";
        source.TargetPath     = "src/Foo.cs";          // enable EditorConfig glob mode
        source.Optional       = true;
        source.ReloadOnChange = true;
        source.ParseOptions   = ConfigurationParseOptions.Strict;
        source.ResolveOptions = ConfigurationResolveOptions.EditorConfigCompatible;
    })
    .Build();
```

The delegate overload mirrors the `AddJsonFile(source => …)` pattern from `Microsoft.Extensions.Configuration.Json`. Set every property up front rather than choosing the right `AddConfiguration` overload for the combination you need.

## How the bridge surfaces values

```
ConfigurationDocument.Parse
    ↓ → IniDocument
ConfigurationExtensions.Resolve(targetPath?, resolveOptions?)
    ↓ → ConfigurationView
Flatten to IDictionary<string, string?>
    ↓
IConfigurationProvider → AddInMemoryCollection → IConfiguration
```

Every key is reachable in the same colon-delimited form `IConfiguration` consumers already use. The values come from the resolved view, so the EditorConfig glob behaviour, preamble handling, key mapping, and unset-value treatment from [Views and resolution](../text-configuration/views-and-resolution.md) all apply.

## Source types

The bridge exposes two source types directly:

- **`TextConfigurationSource`** — extends `FileConfigurationSource`. Properties:
  - `Path`, `Optional`, `ReloadOnChange`, `FileProvider` — inherited from the BCL base.
  - `TargetPath` — optional target path for EditorConfig glob anchoring.
  - `ParseOptions` — `ConfigurationParseOptions?` (null defers to defaults).
  - `ResolveOptions` — `ConfigurationResolveOptions?` (null defers to defaults).

- **`TextStreamConfigurationSource`** — extends `StreamConfigurationSource`. Properties:
  - `Stream` — inherited from the BCL base; consumed during `Build()`.
  - `TargetPath`, `ParseOptions`, `ResolveOptions` — as above.

Both build provider classes (`TextConfigurationProvider`, `TextStreamConfigurationProvider`) are internal — they implement `IConfigurationProvider` and flatten the resolved view into the in-memory backing store. Consumers do not interact with them directly; they configure the source and the provider handles the rest.

## Reload-on-change behaviour

When `reloadOnChange: true` on a file-backed source, the `TextConfigurationProvider` registers a watcher with the file provider. On change:

1. The file is re-read under its original `ParseOptions`.
2. The document is re-resolved through the original `ResolveOptions` with the same `TargetPath`.
3. The flattened key / value map replaces the previous one.
4. The standard `IConfiguration.Reload()` change-token fires.

Subscribers to `ChangeToken.OnChange(...)` see the new values without re-instantiating the configuration root. The reload is atomic from the consumer's perspective — there is no window in which the configuration is half-loaded.

## When *not* to use the bridge

- **You only need the codec.** Reach for [`Bodu.Text.Formats.Ini`](../formats/ini.md) for codec-only access without the bridge or the resolve layer.
- **You only need the resolved view.** Reach for [`Bodu.Text.Configuration`](../text-configuration/index.md) directly — call `Resolve()` and consume `ConfigurationView` without the `IConfiguration` surface.
- **You need JSON, environment-variable, or command-line configuration.** Use the standard Microsoft sources — `AddJsonFile`, `AddEnvironmentVariables`, `AddCommandLine`. The Bodu bridge composes with them; sources earlier in the builder chain are overridden by later sources, per the standard `IConfiguration` rules.

## See also

- [`Bodu.Text.Configuration` overview](../text-configuration/index.md) — the underlying parse / view layer.
- [Parsing and profiles](../text-configuration/parsing-and-profiles.md) — the parse-time options surfaced via `ParseOptions`.
- [Views and resolution](../text-configuration/views-and-resolution.md) — the resolve-time options surfaced via `ResolveOptions` and `TargetPath`.
- [`Bodu.Extensions.Configuration.Text` API reference](~/apidoc/Bodu.Extensions.Configuration.Text.md).
