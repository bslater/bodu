---
title: Bodu.Extensions.Configuration.Text — Getting started
---

# Bodu.Extensions.Configuration.Text — Getting started

Unfamiliar with terms like *source*, *provider*, *target path*, *conventional probe*, *reload-on-change*, or *options
binding*? Read [Core concepts](concepts.md) first.

## Install

```bash
dotnet add package Bodu.Extensions.Configuration.Text
```

Targets `net8.0`. Depends on:

- `Bodu.Core` and `Bodu.Text.Configuration` (the parser, resolver, and view).
- `Microsoft.Extensions.Configuration`, `Microsoft.Extensions.Configuration.FileExtensions`,
  `Microsoft.Extensions.Configuration.Binder`, `Microsoft.Extensions.FileProviders.Physical`,
  `Microsoft.Extensions.Options.ConfigurationExtensions`, and the associated abstractions packages — all at the .NET
  8.0 LTS line.

## Minimal samples

### Add a configuration file

```csharp
using Bodu.Extensions.Configuration.Text;
using Microsoft.Extensions.Configuration;

IConfiguration configuration = new ConfigurationBuilder()
    .AddTextConfigurationFile("appsettings.bodu")
    .Build();

string? logLevel = configuration["logging:level:default"];
```

The overload is named to match the JSON provider's `AddJsonFile` — same arguments, same defaults, same shape. The
file is required by default; pass `optional: true` to tolerate a missing file.

### Conventional file probe

```csharp
IConfiguration configuration = new ConfigurationBuilder()
    .AddTextConfiguration(optional: true, reloadOnChange: true)
    .Build();
```

The no-argument overload probes the builder's base path for `.boduconfig`, then `bodu.config`. The first file found
is loaded; if neither exists and `optional` is `true`, the call is a no-op. Use this when your application has a
single conventional configuration file in its working directory.

### Anchor glob resolution to a source path

```csharp
IConfiguration configuration = new ConfigurationBuilder()
    .AddTextConfigurationFile(
        path: "team.boduconfig",
        targetPath: "src/MyApp/Program.cs",
        optional: false,
        reloadOnChange: true)
    .Build();
```

The `targetPath` is handed to `ConfigurationDocument.Resolve(targetPath)` — sections whose header globs match
contribute to the resolved view, in source order, last-wins.

### Configure via callback

```csharp
using Bodu.Text.Configuration;
using Microsoft.Extensions.FileProviders;

IConfiguration configuration = new ConfigurationBuilder()
    .AddTextConfigurationFile(source =>
    {
        source.FileProvider     = new PhysicalFileProvider("/etc/myapp");
        source.Path             = "settings.bodu";
        source.TargetPath       = "src/Foo.cs";
        source.Optional         = true;
        source.ReloadOnChange   = true;
        source.ParseOptions     = ConfigurationParseOptions.EditorConfigCompatible;
        source.ResolveOptions   = new ConfigurationResolveOptions
        {
            UnsetValueMode = ConfigurationUnsetValueMode.RemoveEffectiveValue,
        };
    })
    .Build();
```

The callback receives the <xref:Bodu.Extensions.Configuration.Text.TextConfigurationSource> directly, so every
property is reachable. Use this overload when several knobs need to be set together — a one-liner overload exists for
the common "just give me the file" case.

### Load from a stream

```csharp
using MemoryStream stream = new(Encoding.UTF8.GetBytes("""
service.name = Bodu
service.port = 8080
"""));

IConfiguration configuration = new ConfigurationBuilder()
    .AddTextConfigurationStream(stream)
    .Build();

string? name = configuration["service:name"];   // "Bodu"
int port = configuration.GetValue<int>("service:port");
```

Stream sources are one-shot — no reload-on-change. Useful for tests, embedded resources, and HTTP-fetched
configuration.

### Add a TOML file or stream

```csharp
using Bodu.Extensions.Configuration.Text;
using Microsoft.Extensions.Configuration;

IConfiguration configuration = new ConfigurationBuilder()
    .AddTomlFile("appsettings.toml", optional: true)
    .Build();

string? level = configuration["logging:level"];   // from [logging] level = "..."
```

The TOML bridge flattens the table hierarchy into the same colon-delimited key space — `[logging.console]` with
`includeScopes = true` surfaces as `configuration["logging:console:includeScopes"]`. It is read-once and read-only, so
there is no `reloadOnChange` parameter; for stream input use `AddTomlStream(stream)`.

### Add a pre-parsed document

```csharp
using Bodu.Extensions.Configuration.Text;
using Bodu.Text.Configuration;

ConfigurationDocument document = ConfigurationDocument.Parse("""
    service.name = Bodu
    service.port = 8080
    """);

IConfiguration configuration = new ConfigurationBuilder()
    .AddTextConfigurationDocument(document)
    .Build();
```

`AddTextConfigurationDocument` resolves the document once and adds the flattened pairs in-memory. It takes a one-shot
snapshot — later edits to the document do not flow into the built configuration, and there is no reload-on-change.

### Bind to a POCO via DI

```csharp
using Bodu.Extensions.Configuration.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

ServiceCollection services = new();
services.AddOptions();
services.AddConfigurationOptions<ServiceOptions>(configuration, "service");

using ServiceProvider provider = services.BuildServiceProvider();
ServiceOptions options = provider.GetRequiredService<IOptions<ServiceOptions>>().Value;

Console.WriteLine($"{options.Name} on port {options.Port}");

sealed class ServiceOptions
{
    public string? Name { get; set; }
    public int Port { get; set; }
}
```

The section-overload is equivalent and useful when the section is already projected:

```csharp
services.AddConfigurationOptions<ServiceOptions>(configuration.GetSection("service"));
```

### Combine with other providers

The Bodu source layers with anything else in the builder. Standard MEC precedence applies — later sources override
earlier ones for the same key:

```csharp
IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddTextConfigurationFile("appsettings.bodu", optional: true)
    .AddEnvironmentVariables()
    .Build();
```

Defaults come from `appsettings.json`, project-specific overrides from `appsettings.bodu`, deployment overrides from
environment variables.

### Watch for changes

```csharp
IConfiguration configuration = new ConfigurationBuilder()
    .AddTextConfigurationFile("appsettings.bodu", reloadOnChange: true)
    .Build();

ChangeToken.OnChange(
    () => configuration.GetReloadToken(),
    () => Console.WriteLine("Configuration reloaded"));
```

The file watcher is attached through the configured `IFileProvider`. Edits, atomic replaces, and `touch` all trigger
a reload.

## Pattern — Bodu + IOptionsMonitor for hot-swappable settings

```csharp
using Bodu.Extensions.Configuration.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

IConfiguration configuration = new ConfigurationBuilder()
    .AddTextConfigurationFile("settings.bodu", optional: false, reloadOnChange: true)
    .Build();

ServiceCollection services = new();
services.AddSingleton(configuration);
services.AddOptions();
services.AddConfigurationOptions<FeatureOptions>(configuration, "features");

using ServiceProvider provider = services.BuildServiceProvider();
IOptionsMonitor<FeatureOptions> monitor = provider.GetRequiredService<IOptionsMonitor<FeatureOptions>>();

monitor.OnChange(updated => Console.WriteLine($"Reload — beta = {updated.BetaEnabled}"));
```

`IOptionsMonitor<T>` re-binds automatically whenever the underlying file changes — there is no manual reload step.

## Where to go next

- **[Core concepts](concepts.md)** — vocabulary refresher.
- **[Introduction](index.md)** — type map and scenario index.
- **[Bodu.Text.Configuration](../text-configuration/index.md)** — the underlying parser, resolver, and view.
- **[Bodu.Extensions.Configuration.Text API reference](xref:Bodu.Extensions.Configuration.Text)** — full type-by-type docs.
