# Bodu.Extensions.Configuration.Text

The EditorConfig-compatible `Microsoft.Extensions.Configuration` provider. This package bridges
[`Bodu.Text.Configuration`](../Bodu.Text.Configuration) into the standard
[`Microsoft.Extensions.Configuration`](https://learn.microsoft.com/dotnet/core/extensions/configuration)
pipeline so consumers can register a `.boduconfig` or `bodu.config` file with the same shape they already
use for JSON, INI, or XML providers.

## API matrix vs `Microsoft.Extensions.Configuration.Json`

| Feature | `Microsoft.Extensions.Configuration.Json` | `Bodu.Extensions.Configuration.Text` |
|---|---|---|
| `AddXxxFile(builder, path)` | `AddJsonFile(builder, path)` | `AddBoduConfiguration(builder, path)` |
| `AddXxxFile(builder, path, optional, reloadOnChange)` | yes | yes (extra `targetPath`) |
| `AddXxxFile(builder, provider, path, optional, reloadOnChange)` | yes | yes |
| `AddXxxFile(builder, Action<XxxSource>)` | yes | yes |
| `AddXxxStream(builder, Stream)` | `AddJsonStream(builder, stream)` | `AddBoduConfiguration(builder, stream)` |
| `IFileProvider`-backed reload-on-change | yes | yes (inherited) |
| `GetReloadToken` / change tokens | yes | yes (inherited) |
| `GetSection`, `GetChildren`, `Bind` | yes | yes (via colon-delimited keys) |
| Default-filename convention | none | `.boduconfig` then `bodu.config` |
| Programmatic-document entry point | none | `AddBoduConfiguration(builder, IniDocument)` |
| `IOptions<T>` helper | provided by `Microsoft.Extensions.Options.ConfigurationExtensions` | `AddBoduConfigurationOptions<TOptions>` |

## Worked examples

### Load a file by path

```csharp
using Microsoft.Extensions.Configuration;
using Bodu.Extensions.Configuration.Text;

IConfiguration config = new ConfigurationBuilder()
    .AddBoduConfiguration("app.boduconfig", optional: false, reloadOnChange: true)
    .Build();

string? indentSize = config["format:indent:size"];
```

The source uses dotted keys (`format.indent.size`), which the bridge translates into colon-delimited keys
(`format:indent:size`) for `Microsoft.Extensions.Configuration`.

### Load from a stream

```csharp
using MemoryStream stream = new(Encoding.UTF8.GetBytes("""
service.name = Bodu
service.port = 8080
"""));

IConfiguration config = new ConfigurationBuilder()
    .AddBoduConfiguration(stream)
    .Build();
```

Stream sources are one-shot — the stream is read once when the builder is built and no file watcher is
attached.

### Load with an explicit `IFileProvider`

```csharp
using Microsoft.Extensions.FileProviders;

var fileProvider = new PhysicalFileProvider(repoRoot);

IConfiguration config = new ConfigurationBuilder()
    .AddBoduConfiguration(fileProvider, "app.boduconfig")
    .Build();
```

## Target paths and section resolution

Unlike JSON, the source format supports EditorConfig-style glob-anchored sections. Set `TargetPath` to choose
which section a build resolves against:

```csharp
new ConfigurationBuilder()
    .AddBoduConfiguration(source =>
    {
        source.Path = "app.boduconfig";
        source.TargetPath = "src/Foo.cs";  // selects [src/**/*.cs] when present
    })
    .Build();
```

When `TargetPath` is `null`, only preamble (top-of-file) keys flow into the configuration view — anchored
sections are skipped.

## Default-filename convention

`AddBoduConfiguration()` (no arguments) probes the builder's file provider for `.boduconfig` first, then
falls back to `bodu.config`. To resolve the dot-prefixed name through `PhysicalFileProvider`, construct the
provider with `ExclusionFilters.None`:

```csharp
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

var builder = new ConfigurationBuilder();
builder.SetFileProvider(new PhysicalFileProvider(repoRoot, ExclusionFilters.None));
IConfiguration config = builder.AddBoduConfiguration().Build();
```

`bodu.config` resolves through the default `Sensitive` exclusion filters without any further configuration.

## `IOptions<T>` binding

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

services.AddOptions();
services.AddBoduConfigurationOptions<ServiceOptions>(config, sectionName: "service");

var options = provider.GetRequiredService<IOptions<ServiceOptions>>().Value;
```

The helper is a thin shim over `services.Configure<TOptions>(config.GetSection(name))` — it exists for
discoverability alongside the `AddBoduConfiguration` API surface.
