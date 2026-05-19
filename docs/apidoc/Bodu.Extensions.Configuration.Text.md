---
uid: Bodu.Extensions.Configuration.Text
---

## Purpose

**Bodu.Extensions.Configuration.Text** bridges <xref:Bodu.Text.Configuration> to <xref:Microsoft.Extensions.Configuration>. It exposes the conventional `AddConfiguration` entry point on <xref:Microsoft.Extensions.Configuration.IConfigurationBuilder> — the overload set mirrors `Microsoft.Extensions.Configuration.Json`'s `AddJsonFile` / `AddJsonStream` — so a Bodu Text Configuration file can be layered alongside JSON, INI, XML, and environment-variable sources with no learning curve.

Once added, the source loads the file (or stream, or pre-parsed document), parses it with <xref:Bodu.Text.Configuration.ConfigurationDocument>, resolves it for the configured `TargetPath`, and copies the flattened view into the standard <xref:Microsoft.Extensions.Configuration.IConfiguration> dictionary as colon-delimited keys. A companion <xref:Bodu.Extensions.Configuration.Text.ConfigurationOptionsExtensions> helper binds a section to an <xref:Microsoft.Extensions.Options.IOptions`1> instance through the standard DI container.

## Static documentation

- **[Bodu.Extensions.Configuration.Text introduction](~/docs/extensions-configuration-text/index.md)** — shape of the library, headline types, scenarios.
- **[Bodu.Extensions.Configuration.Text core concepts](~/docs/extensions-configuration-text/concepts.md)** — vocabulary: source vs provider, target path, parse/resolve option propagation, reload-on-change, options binding.
- **[Bodu.Extensions.Configuration.Text getting started](~/docs/extensions-configuration-text/getting-started.md)** — install and minimal samples for the file overload, stream overload, conventional probe, options binding.
- **[Bodu.Text.Configuration](~/docs/text-configuration/index.md)** — the underlying parser, resolver, and view model.

## Key types

**Builder extensions**

- <xref:Bodu.Extensions.Configuration.Text.TextConfigurationExtensions> — the primary entry point. Six `AddConfiguration` overloads:
    - `AddConfiguration(builder, string path, string? targetPath, bool optional, bool reloadOnChange)` — file path.
    - `AddConfiguration(builder, IFileProvider?, string path, string? targetPath, bool optional, bool reloadOnChange)` — file path through a specific file provider.
    - `AddConfiguration(builder, Action<TextConfigurationSource> configureSource)` — configure callback.
    - `AddConfiguration(builder, bool optional, bool reloadOnChange)` — conventional file probe (`.boduconfig` → `bodu.config`).
    - `AddConfiguration(builder, Stream)` — stream source (no reload-on-change).
    - `AddConfiguration(builder, IniDocument, string? targetPath)` — pre-parsed document source.

**Sources and providers**

- <xref:Bodu.Extensions.Configuration.Text.TextConfigurationSource> — file-backed configuration source. Subclasses <xref:Microsoft.Extensions.Configuration.FileConfigurationSource>; inherits `Path`, `Optional`, `ReloadOnChange`, `FileProvider`; adds `TargetPath`, `ParseOptions`, `ResolveOptions`.
- <xref:Bodu.Extensions.Configuration.Text.TextConfigurationProvider> — the matching `FileConfigurationProvider`. Reads the file via the standard MEC pipeline and projects the resolved view into the inherited `Data` dictionary.
- <xref:Bodu.Extensions.Configuration.Text.TextStreamConfigurationSource> — stream-backed configuration source. Subclasses <xref:Microsoft.Extensions.Configuration.StreamConfigurationSource>; adds the same `TargetPath` / `ParseOptions` / `ResolveOptions` triple. One-shot — no reload-on-change.
- <xref:Bodu.Extensions.Configuration.Text.TextStreamConfigurationProvider> — the matching `StreamConfigurationProvider`.
- <xref:Bodu.Extensions.Configuration.Text.TextConfigurationLoader> — internal helper shared by both providers that parses a stream into an `IniDocument`, resolves it for `TargetPath`, and flattens the resolved view into `Dictionary<string, string?>`.

**Options binding**

- <xref:Bodu.Extensions.Configuration.Text.ConfigurationOptionsExtensions> — DI helpers:
    - `AddConfigurationOptions<TOptions>(services, IConfiguration configuration, string sectionName)` — section by name.
    - `AddConfigurationOptions<TOptions>(services, IConfigurationSection section)` — already-projected section.
  Both are thin shims over `services.Configure<TOptions>(...)` — provided for IntelliSense discoverability.

## Example

```csharp
using Bodu.Extensions.Configuration.Text;
using Bodu.Text.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

IConfiguration configuration = new ConfigurationBuilder()
    .AddConfiguration(src =>
    {
        src.FileProvider     = new PhysicalFileProvider("/etc/myapp");
        src.Path             = "settings.boduconfig";
        src.TargetPath       = "src/Foo.cs";
        src.Optional         = true;
        src.ReloadOnChange   = true;
        src.ParseOptions     = ConfigurationParseOptions.EditorConfigCompatible;
        src.ResolveOptions   = new ConfigurationResolveOptions
        {
            UnsetValueMode = ConfigurationUnsetValueMode.RemoveEffectiveValue,
        };
    })
    .Build();

string? logLevel = configuration["logging:level:default"];

// Bind a section to a POCO through the DI container.
ServiceCollection services = new();
services.AddOptions();
services.AddConfigurationOptions<ServiceOptions>(configuration, "service");

using ServiceProvider provider = services.BuildServiceProvider();
ServiceOptions options = provider.GetRequiredService<IOptions<ServiceOptions>>().Value;

sealed class ServiceOptions
{
    public string? Name { get; set; }
    public int Port { get; set; }
}
```

## Notes

- **Conventional file probe.** The no-argument overload probes the builder's default file provider for `.boduconfig`, then `bodu.config`. The first file present is loaded; if neither exists and `optional` is `true`, the call is a no-op. This matches the dotfile / plain-file convention common in version-controlled repos.
- **Reload-on-change.** <xref:Bodu.Extensions.Configuration.Text.TextConfigurationSource> inherits `ReloadOnChange` from `FileConfigurationSource`. When set, the provider attaches a file watcher via the configured `IFileProvider`, reparses on change, and triggers the standard `IConfiguration` reload tokens — so consumers using `Microsoft.Extensions.Options.IOptionsMonitor<T>` rebind automatically. <xref:Bodu.Extensions.Configuration.Text.TextStreamConfigurationSource> does **not** support reload; the stream is parsed once when `Build` is called.
- **File-provider precedence.** When `AddConfiguration` is given an explicit `IFileProvider`, that wins; otherwise the source's `FileProvider` wins; otherwise the builder's default applies. Standard MEC precedence is preserved.
- **Parse / resolve option propagation.** Both `ParseOptions` and `ResolveOptions` default to `null`, in which case `ConfigurationParseOptions.Bodu` and `ConfigurationResolveOptions.Bodu` are used. Set them per-source to mix profiles within a single builder (an EditorConfig-strict file alongside a Bodu-permissive one).
- **Target path.** `TargetPath` is per-source. Each source resolves the document for its own target; multiple sources with the same path but different targets are a supported pattern when the application needs configuration evaluated for several paths in parallel.
- **DI integration.** <xref:Bodu.Extensions.Configuration.Text.ConfigurationOptionsExtensions> is a discoverability shim. Calls to `services.Configure<TOptions>(configuration.GetSection(name))` produce equivalent bindings. Callers comfortable with the MEC pattern may use either form interchangeably.
- **Validation.** All public entry points validate inputs via `ThrowHelper`. `ArgumentNullException` covers null builders / configurations / streams / sections; `ArgumentException` covers null, empty, or whitespace path / section-name strings.
- **See also:** the [introduction](~/docs/extensions-configuration-text/index.md), [core concepts](~/docs/extensions-configuration-text/concepts.md), and [getting-started](~/docs/extensions-configuration-text/getting-started.md); the underlying parser, resolver, and view in [Bodu.Text.Configuration](~/docs/text-configuration/index.md).
