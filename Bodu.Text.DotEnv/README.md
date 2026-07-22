# Bodu.Text.DotEnv

> **API stability — Preview.** The public API surface is largely settled but is still being finalized ahead of the 1.0 release and may change; breaking changes can land in a minor version until then.

A DotEnv (`.env`) library for .NET 8, shaped after `System.Text.Json`: a typed settings serializer over a low-level forward-only token reader and writer, with both a mutable and a read-only document object model. The dialect follows the mainstream `dotenv` implementations — `export` prefixes, double/single quoting with escapes, inline comments — and values are deliberately literal: no `${VAR}` interpolation happens at parse time.

## Installation

```shell
dotnet add package Bodu.Text.DotEnv
```

Targets `net8.0`. Also available through the `Bodu.Text.Formats` umbrella package.

## API shape

| Type(s) | Namespace | Role |
|---|---|---|
| `DotEnvSerializer` / `DotEnvSerializerOptions` / `DotEnvSerializerDefaults` | `Bodu.Text.DotEnv` | Static serializer entry point, configuration, and presets (`Web` = SCREAMING_SNAKE_CASE, case-insensitive). |
| `DotEnvFormatException` / `DotEnvSerializationException` | `Bodu.Text.DotEnv` | Failures split by cause: malformed input vs. values that cannot be mapped. |
| `Utf8DotEnvReader` (+ `DotEnvReaderOptions`) | `Bodu.Text.DotEnv.Reader` | Forward-only `ref struct` token reader (export detection, quoting, inline comments). |
| `Utf8DotEnvWriter` (+ `DotEnvWriterOptions`) | `Bodu.Text.DotEnv.Writer` | Forward-only `ref struct` token writer. |
| `DotEnvDocument` / `DotEnvElement` / `DotEnvProperty` | `Bodu.Text.DotEnv.Document` | Read-only document object model over the flat key/value object. |
| `DotEnvNode` / `DotEnvObject` / `DotEnvValue` | `Bodu.Text.DotEnv.Nodes` | Mutable document object model; preserves each entry's `export` flag. |

```csharp
using Bodu.Text.DotEnv;

sealed class Settings
{
    public string? AppEnv { get; set; }   // binds APP_ENV
    public int AppPort { get; set; }      // binds APP_PORT
}

Settings settings = DotEnvSerializer.Deserialize<Settings>(
    envText, new DotEnvSerializerOptions(DotEnvSerializerDefaults.Web));
```
