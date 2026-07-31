---
title: Configuration sources
---

# Configuration sources

`Bodu.Extensions.Configuration.Text` bridges [`Bodu.Text.Configuration`](../text-configuration/index.md) into `Microsoft.Extensions.Configuration`. Add a Bodu-formatted INI file or stream to an `IConfigurationBuilder`, and the parsed and resolved view becomes available through the standard `IConfiguration` surface that ASP.NET Core, Generic Host, and the rest of the BCL configuration pipeline already consume.

## Pattern 1 — file-backed source with reload-on-change

<!-- compile -->
```csharp
using Bodu.Extensions.Configuration.Text;
using Microsoft.Extensions.Configuration;

IConfiguration configuration = new ConfigurationBuilder()
    .AddTextConfigurationFile("appsettings.ini", optional: false, reloadOnChange: true)
    .Build();

string? appName = configuration["appName"];
string? level   = configuration["logging:level"];
```

`AddTextConfigurationFile(path, …)` registers a `TextConfigurationSource`. The provider uses the host's default file provider, watches for changes when `reloadOnChange: true`, and re-resolves the view when the file is rewritten. The dotted keys in the INI source flatten through `ConfigurationKeyOptions.Default` so `logging.level.default = …` is reachable as `"logging:level:default"` — the canonical colon-delimited form `IConfiguration` consumers expect.

## Pattern 2 — explicit file provider

```csharp
using Bodu.Extensions.Configuration.Text;
using Microsoft.Extensions.FileProviders;

IConfiguration configuration = new ConfigurationBuilder()
    .AddTextConfigurationFile(
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
    .AddTextConfiguration(optional: true, reloadOnChange: false)
    .Build();
```

The parameterless overload probes for `.boduconfig` first and then `bodu.config` in the builder's base path. With `optional: true` (the default) the call is a no-op when neither file is present; with `optional: false` it throws `FileNotFoundException`.

## Pattern 4 — stream-backed source

```csharp
using Bodu.Extensions.Configuration.Text;
using Bodu.Text.Configuration;

using var ms = new MemoryStream(Encoding.UTF8.GetBytes(iniText));

IConfiguration configuration = new ConfigurationBuilder()
    .AddTextConfigurationStream(
        stream: ms,
        targetPath: null,
        parseOptions: ConfigurationParseOptions.Relaxed,
        resolveOptions: ConfigurationResolveOptions.EditorConfigCompatible)
    .Build();
```

`AddTextConfigurationStream(Stream, …)` registers a `TextStreamConfigurationSource`. Unlike file-backed sources, stream sources do not support reload-on-change — the stream is consumed once during `Build()`. Use the stream overload when the configuration comes from a network resource, an embedded resource, or anywhere else that cannot be expressed as a file path.

## Pattern 5 — pre-parsed document

```csharp
using Bodu.Extensions.Configuration.Text;
using Bodu.Text.Configuration;

// Already-parsed document, shared across builders.
ConfigurationDocument doc = ConfigurationDocument.Parse(iniText);

IConfiguration configuration = new ConfigurationBuilder()
    .AddTextConfigurationDocument(doc, targetPath: null)
    .Build();
```

`AddTextConfigurationDocument` accepts any <xref:Bodu.Text.Configuration.IniDocumentBase> — including a
<xref:Bodu.Text.Configuration.ConfigurationDocument>. The bridge resolves the document once and flattens the view into an
in-memory collection before handing it to the configuration root, so the source is captured **by value**: later mutations to the document do not flow into the configuration, and there is no reload-on-change.

To author or mutate a document in code rather than parsing one, build an <xref:Bodu.Text.Configuration.IniDocument> (the configuration library's own INI model) through its public surface (`GetOrAddSection`, `AddEntry`) and pass it the same way:

```csharp
using Bodu.Extensions.Configuration.Text;
using Bodu.Text.Configuration;

var doc = new IniDocument();
doc.GetOrAddSection("logging").AddEntry(new IniEntry("level", "Debug"));

IConfiguration configuration = new ConfigurationBuilder()
    .AddTextConfigurationDocument(doc)
    .Build();
```

## Pattern 6 — fluent source configuration

```csharp
using Bodu.Extensions.Configuration.Text;

IConfiguration configuration = new ConfigurationBuilder()
    .AddTextConfigurationFile(source =>
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

The delegate overload mirrors the `AddJsonFile(source => …)` pattern from `Microsoft.Extensions.Configuration.Json`. Set every property up front rather than choosing the right `AddTextConfiguration*` overload for the combination you need.

## Pattern 7 — TOML file or stream source

Alongside the Bodu INI bridge, the package ships a read-only TOML bridge that surfaces [`Bodu.Text.Toml`](../serialization/toml/index.md) through the same `IConfiguration` pipeline, mirroring the `AddJsonFile` / `AddJsonStream` shape:

```csharp
using Bodu.Extensions.Configuration.Text;
using Microsoft.Extensions.Configuration;

IConfiguration configuration = new ConfigurationBuilder()
    .AddTomlFile("appsettings.toml", optional: true)
    .Build();

string? level = configuration["logging:level"];
```

`AddTomlFile(path, optional)` registers a `TomlConfigurationSource`. The source is **read once** when the configuration is built — the TOML bridge is read-only and attaches no reload-on-change watcher, so there is no `reloadOnChange` parameter (unlike the file-backed INI source in Pattern 1). With `optional: true` a missing file yields an empty source; with `optional: false` it throws `FileNotFoundException`.

For configuration that arrives as a stream rather than a file path, use the stream overload — the UTF-8 TOML text is consumed once during `Build()`:

```csharp
using Bodu.Extensions.Configuration.Text;
using System.Text;

using var ms = new MemoryStream(Encoding.UTF8.GetBytes("[logging]\nlevel = \"Debug\""));

IConfiguration configuration = new ConfigurationBuilder()
    .AddTomlStream(ms)
    .Build();
```

### How TOML keys map to configuration keys

The provider flattens the TOML document's table hierarchy into the canonical colon-delimited form `IConfiguration` consumers expect. A TOML table dotted-path becomes a colon-delimited key:

```toml
[logging]
level = "Debug"

[logging.console]
includeScopes = true
```

flattens to:

```
logging:level            = Debug
logging:console:includeScopes = true
```

Keys are matched case-insensitively, exactly as the BCL JSON and INI providers behave, so `configuration["Logging:Level"]` and `configuration["logging:level"]` resolve to the same value. Because the TOML bridge surfaces the same colon-delimited key space, it layers and binds to `IOptions<T>` exactly like the INI sources described below; the only difference is that, being read-only, it never participates in reload-on-change.

## Pattern 8 — Bencode file or stream source

The package also ships a read-only Bencode bridge that surfaces [`Bodu.Text.Bencode`](../serialization/bencode/index.md) through the same `IConfiguration` pipeline, with the same shape as the TOML bridge:

<!-- compile -->
```csharp
using Bodu.Extensions.Configuration.Text;
using Microsoft.Extensions.Configuration;

IConfiguration configuration = new ConfigurationBuilder()
    .AddBencodeFile("appsettings.bencode", optional: true)
    .Build();

string? level = configuration["logging:level"];
```

`AddBencodeFile(path, optional)` registers a `BencodeConfigurationSource`. Like the TOML bridge, the source is **read once** when the configuration is built — it is read-only and attaches no reload-on-change watcher. With `optional: true` a missing file yields an empty source; with `optional: false` it throws `FileNotFoundException`.

For configuration that arrives as a stream, use the stream overload — the Bencode document is consumed once during `Build()`:

<!-- compile -->
```csharp
using Bodu.Extensions.Configuration.Text;
using Microsoft.Extensions.Configuration;

using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("d7:loggingd5:level5:Debugee"));

IConfiguration configuration = new ConfigurationBuilder()
    .AddBencodeStream(ms)
    .Build();
```

### How Bencode values map to configuration keys

The document root must be a Bencode **dictionary** — an integer, byte-string, or list root is rejected with `FormatException`, because it cannot contribute named configuration keys. Nested dictionaries contribute one colon-delimited segment per level, and list elements contribute their zero-based index as a segment, mirroring the framework JSON provider:

```
d7:loggingd6:levelsl4:info4:warneee
```

flattens to:

```
logging:levels:0 = info
logging:levels:1 = warn
```

The document is parsed with the library's strict canonical defaults, so unsorted or duplicate dictionary keys are rejected. Integers render invariant across the full unsigned 64-bit range the format supports; byte strings decode as UTF-8, with content that is not valid UTF-8 decoded via U+FFFD replacement rather than rejected. Keys are matched case-insensitively, so two Bencode keys that differ only in case collide and are rejected as duplicates.

## Pattern 9 — binding to `IOptions<T>`

Because the bridge surfaces standard colon-delimited keys, the values bind to typed options classes through the ordinary `Microsoft.Extensions.Options` pipeline. `ConfigurationOptionsExtensions.AddConfigurationOptions<TOptions>` is a discoverable shim over `services.Configure<TOptions>(section)` — either call produces the same registration:

```csharp
using Bodu.Extensions.Configuration.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public sealed class LoggingOptions
{
    public string Level { get; init; } = "Information";
    public bool   IncludeScopes { get; init; }
}

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddTextConfigurationFile("appsettings.boduconfig");

// Bind by section name against the configuration root…
builder.Services.AddConfigurationOptions<LoggingOptions>(
    builder.Configuration, sectionName: "logging");

// …or bind a pre-resolved section directly.
builder.Services.AddConfigurationOptions<LoggingOptions>(
    builder.Configuration.GetSection("logging"));
```

Consume the bound options through constructor injection as usual:

```csharp
public sealed class RequestLogger
{
    private readonly LoggingOptions _options;

    public RequestLogger(IOptions<LoggingOptions> options) =>
        _options = options.Value;   // e.g. _options.Level == "Debug"
}
```

So an INI source containing `logging.level = Debug` flattens to the key `"logging:level"`, the `"logging"` section binds onto `LoggingOptions`, and `options.Value.Level` reads `"Debug"`. Callers comfortable with the BCL surface can keep calling `services.Configure<LoggingOptions>(section)` directly — the helper exists purely to keep the call site short.

## How the bridge surfaces values

```
ConfigurationDocument.Load(stream, ParseOptions)
    ↓ → IniDocumentBase
document.Resolve(TargetPath, ResolveOptions)
    ↓ → ConfigurationView
Flatten to Dictionary<string, string?> (OrdinalIgnoreCase)
    ↓
provider.Data → IConfiguration["key:subkey"]
```

The file and stream providers assign the flattened map straight to the inherited `Data` dictionary; only the pre-parsed-document overload (Pattern 5) routes through `AddInMemoryCollection`. Either way, every key is reachable in the same colon-delimited form `IConfiguration` consumers already use. The values come from the resolved view, so the EditorConfig glob behaviour, preamble handling, key mapping, and unset-value treatment from [Views and resolution](../text-configuration/views-and-resolution.md) all apply.

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

Both build provider classes — <xref:Bodu.Extensions.Configuration.Text.TextConfigurationProvider> (a `FileConfigurationProvider`) and <xref:Bodu.Extensions.Configuration.Text.TextStreamConfigurationProvider> (a `StreamConfigurationProvider`) — flatten the resolved view into the inherited `Data` dictionary. They are public but rarely constructed directly; you configure the source and the provider handles the rest. Each exposes a typed `TextSource` accessor for diagnostic introspection — locate the provider on `IConfigurationRoot.Providers` and read back the originating source:

```csharp
IConfigurationRoot root = builder.Build();
TextConfigurationProvider? bodu = root.Providers
    .OfType<TextConfigurationProvider>()
    .FirstOrDefault();

string? loadedFrom = bodu?.TextSource.Path;
```

## Reload-on-change behaviour

When `reloadOnChange: true` on a file-backed source, the `TextConfigurationProvider` registers a watcher with the file provider. On change:

1. The file is re-read under its original `ParseOptions`.
2. The document is re-resolved through the original `ResolveOptions` with the same `TargetPath`.
3. The flattened key / value map replaces the previous one.
4. The standard `IConfiguration.Reload()` change-token fires.

Subscribers to `ChangeToken.OnChange(...)` see the new values without re-instantiating the configuration root. The reload is atomic from the consumer's perspective — there is no window in which the configuration is half-loaded.

Reload composes with options binding through the standard monitor surface — `IOptionsMonitor<T>` re-binds on every reload, where `IOptions<T>` is a one-shot snapshot taken at first resolution:

```csharp
public sealed class RequestLogger
{
    public RequestLogger(IOptionsMonitor<LoggingOptions> monitor)
    {
        // CurrentValue tracks the file: edit appsettings.ini and the next
        // read reflects the re-parsed, re-resolved values.
        string level = monitor.CurrentValue.Level;

        monitor.OnChange(updated => Console.WriteLine($"level → {updated.Level}"));
    }
}
```

Stream and pre-parsed-document sources have no file to watch, so they never reload — re-add the source and rebuild the root to pick up new data.

## Layering with other providers

A Bodu source participates in the standard `IConfiguration` precedence rules: providers are consulted in registration order, and for any key supplied by more than one source, **the last-registered source wins**. A common shape is a JSON baseline with a Bodu-formatted override file on top:

```csharp
IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)               // baseline
    .AddTextConfigurationFile("overrides.ini", optional: true)      // wins on conflict
    .AddEnvironmentVariables()                                      // wins over both
    .Build();

// appsettings.json:  { "logging": { "level": "Information" } }
// overrides.ini:     logging.level = Debug

configuration["logging:level"];   // "Debug" — the Bodu source is later
```

Reverse the order to make the JSON file the override layer. Because both sources flatten to the same colon-delimited key space, no key translation is needed — a `logging.level` INI entry and a nested `"logging": { "level": … }` JSON property occupy the same key, `"logging:level"`.

## ASP.NET Core and the Generic Host

Because a Bodu source is an ordinary `IConfigurationSource`, it registers on the
same `IConfigurationBuilder` that `WebApplicationBuilder` and `HostBuilder`
already expose — there is no host-specific entry point to learn. The patterns
above (file, stream, layering, `IOptions<T>` binding) all apply unchanged inside
a hosted app; this section shows the wiring that is specific to the host.

**Register on the host's configuration builder.** `WebApplicationBuilder.Configuration`
and `IHostBuilder.ConfigureAppConfiguration` both hand you the builder the host
will build into:

```csharp
using Bodu.Extensions.Configuration.Text;

// Minimal hosting (ASP.NET Core / WebApplication)
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddTextConfigurationFile(
    "appsettings.boduconfig", optional: true, reloadOnChange: true);

// Generic Host
Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
        config.AddTextConfigurationFile(
            "appsettings.boduconfig", optional: true, reloadOnChange: true));
```

**Environment-based file selection** mirrors the host's own
`appsettings.{Environment}.json` convention — layer a base file with an
environment overlay, the later source winning on conflict:

```csharp
var builder = WebApplication.CreateBuilder(args);
string env = builder.Environment.EnvironmentName;   // "Development", "Production", …

builder.Configuration
    .AddTextConfigurationFile("appsettings.boduconfig", optional: true, reloadOnChange: true)
    .AddTextConfigurationFile($"appsettings.{env}.boduconfig", optional: true, reloadOnChange: true);
```

**Layering precedence is the host's, not the bridge's.** The host registers its
own sources (JSON, environment variables, command line) before `Program.cs`
runs, so a source you add lands *after* them and wins on conflicting keys; a
later `AddEnvironmentVariables()` would in turn win over the Bodu source. The
order is the registration order on the builder, exactly as in the
[Layering with other providers](#layering-with-other-providers) section above.

**`IOptions<T>` and `IOptionsMonitor<T>` binding in a hosted app.** Bind the
flattened colon-delimited keys onto a typed options class through the host's DI
container, then inject the bound options where you need them:

```csharp
builder.Services.AddConfigurationOptions<LoggingOptions>(
    builder.Configuration, sectionName: "logging");

WebApplication app = builder.Build();
```

```csharp
public sealed class RequestLogger
{
    private readonly IOptionsMonitor<LoggingOptions> _monitor;

    public RequestLogger(IOptionsMonitor<LoggingOptions> monitor) =>
        _monitor = monitor;   // CurrentValue tracks the file across reloads

    public void Log(string message) =>
        Console.WriteLine($"[{_monitor.CurrentValue.Level}] {message}");
}
```

Reload-on-change composes with the host the same way it does outside it: with
`reloadOnChange: true` on a file source, the provider re-reads and re-resolves on
a file edit, the configuration root's change token fires, and
`IOptionsMonitor<T>.CurrentValue` re-binds — so a long-lived singleton sees the
new values without a restart, while `IOptions<T>` stays the one-shot snapshot
taken at first resolution. Stream and pre-parsed-document sources have no file to
watch and never reload, so reserve them for configuration that is fixed for the
process lifetime.

## When *not* to use the bridge

- **You only need the codec.** Reach for [`Bodu.Text.Ini`](../formats/ini.md) for codec-only access without the bridge or the resolve layer.
- **You only need the resolved view.** Reach for [`Bodu.Text.Configuration`](../text-configuration/index.md) directly — call `Resolve()` and consume `ConfigurationView` without the `IConfiguration` surface.
- **You need JSON, environment-variable, or command-line configuration.** Use the standard Microsoft sources — `AddJsonFile`, `AddEnvironmentVariables`, `AddCommandLine`. The Bodu bridge composes with them; sources earlier in the builder chain are overridden by later sources, per the standard `IConfiguration` rules.

## See also

- [`Bodu.Extensions.Configuration.Text` guides](index.md) — the member overview for this package.
- [`Bodu.Text.Configuration` overview](../text-configuration/index.md) — the underlying parse / view layer.
- [Parsing and profiles](../text-configuration/parsing-and-profiles.md) — the parse-time options surfaced via `ParseOptions`.
- [Views and resolution](../text-configuration/views-and-resolution.md) — the resolve-time options surfaced via `ResolveOptions` and `TargetPath`.
- [Configuration topic guides](../topics/configuration.md) — every guide in the Configuration topic.
- [Configuration topic overview](../../docs/topics/configuration.md) — the pipeline and package boundaries.
- [`TextConfigurationSource`](xref:Bodu.Extensions.Configuration.Text.TextConfigurationSource) · [`TextStreamConfigurationSource`](xref:Bodu.Extensions.Configuration.Text.TextStreamConfigurationSource) · [`TomlConfigurationExtensions`](xref:Bodu.Extensions.Configuration.Text.TomlConfigurationExtensions) · [`ConfigurationOptionsExtensions`](xref:Bodu.Extensions.Configuration.Text.ConfigurationOptionsExtensions)
- [`Bodu.Extensions.Configuration.Text` API reference](xref:Bodu.Extensions.Configuration.Text).
