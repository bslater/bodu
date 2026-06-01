---
title: Bodu.Text.Configuration — Getting started
---

# Bodu.Text.Configuration — Getting started

Unfamiliar with terms like *document*, *view*, *profile*, *preamble*, *target path*, *unset*, or *diagnostic mode*?
Read [Core concepts](concepts.md) first.

## Install

```bash
dotnet add package Bodu.Text.Configuration
```

Targets `net8.0`. Depends on `Bodu.Core` (throw helpers) and `Bodu.Text.Formats` (the underlying `IniDocument`). No
external NuGet references.

For `Microsoft.Extensions.Configuration` integration — `AddConfiguration`, options binding, file-provider
support — install the sibling [`Bodu.Extensions.Configuration.Text`](../extensions-configuration-text/getting-started.md)
package on top.

## Minimal samples

### Parse, resolve, read

```csharp
using Bodu.Text.Configuration;
using Bodu.Text.Formats;

const string source = """
# Bodu configuration sample
root = true

[*.cs]
format.indent.style = space
format.indent.size  = 4
logging.level.default = Information

[src/**/*.{cs,csproj}]
format.indent.size = 2
logging.level.default = Warning
""";

IniDocument doc = ConfigurationDocument.Parse(source);

ConfigurationView view = doc.Resolve("src/Bodu.Text.Configuration/src/Foo.cs");

string indentStyle = view.GetString("format:indent:style");           // "space"
int    indentSize  = view.GetInt32("format:indent:size");             // 2 (last-wins from the [src/**] section)
string logLevel    = view.GetString("logging:level:default");         // "Warning"
```

Both the dotted form (`format.indent.style`) and the colon-delimited form (`format:indent:style`) work as lookup keys
on the view — the library projects raw keys to canonical colon-delimited form during resolve, but the dotted form
remains a valid alias.

### Read with a fallback

```csharp
int indent = view.GetInt32("format:indent:size", fallback: 4);        // 4 if the key is missing — but FormatException still fires on malformed values
string logTo = view.GetString("logging:writeTo", fallback: "console");
```

`TryGetXxx` never throws — including on malformed values. Use it when you cannot trust the source text and want
diagnostics on a per-key basis.

```csharp
if (view.TryGetInt32("format:indent:size", out int size))
{
    // size is set
}
```

### Generic typed accessor — any `ISpanParsable<T>`

```csharp
double threshold = view.GetValue<double>("limits:cpu:threshold");
TimeSpan timeout = view.GetValue<TimeSpan>("network:read:timeout");
Guid     correlation = view.GetValue<Guid>("trace:correlation:id");
```

All parsing uses `CultureInfo.InvariantCulture` so behaviour is deterministic across locales.

### Profile presets

```csharp
// Strict — duplicate keys are rejected, key-only properties forbidden, inline comments off.
IniDocument generated = ConfigurationDocument.Parse(
    text,
    ConfigurationParseOptions.Strict);

// EditorConfig-compatible — inline comments disabled, identity key mapping, only `root` from preamble.
IniDocument editorConfig = ConfigurationDocument.Parse(
    text,
    ConfigurationParseOptions.EditorConfigCompatible);

// Pick a profile at runtime.
ConfigurationProfile profile = userProfile;
ConfigurationParseOptions options = ConfigurationParseOptions.For(profile);
```

### Collect diagnostics instead of throwing

```csharp
ConfigurationParseOptions options = ConfigurationParseOptions.Relaxed; // DiagnosticMode = Collect

ConfigurationParseResult result = ConfigurationDocument.ParseWithDiagnostics(text, options);

foreach (ConfigurationDiagnostic d in result.Diagnostics)
{
    Console.WriteLine($"{d.Severity} {d.Code} at line {d.Location.Line}: {d.Message}");
}

if (result.Diagnostics.Length == 0)
{
    // Clean parse — document is fully usable.
    ConfigurationView view = result.Document.Resolve("src/Foo.cs");
}
```

`ParseWithDiagnostics` returns successfully even when the document contains recoverable issues; valid sections remain
usable in `result.Document`. Under the default `Throw` diagnostic mode, the same method raises
<xref:Bodu.Text.Configuration.ConfigurationParseException> on the first error.

### Load from a file or stream

```csharp
IniDocument fromPath = ConfigurationDocument.Load(".boduconfig");

await using FileStream fs = File.OpenRead("bodu.config");
IniDocument fromStream = ConfigurationDocument.Load(fs);
```

`Load(path)` records the originating directory so anchored glob patterns (e.g. `[src/**]`) can resolve against the
correct root without an explicit `PathRoot` setting. `Load(Stream)` and `Parse(string)` produce documents with no
path context, so anchored globs require `ConfigurationResolveOptions.PathRoot` to be set explicitly — or
`MissingPathRootMode` to opt into the empty-root or ignore behaviour.

### Resolve options — anchor a path root

```csharp
IniDocument doc = ConfigurationDocument.Parse(source);

ConfigurationResolveOptions options = new()
{
    PathRoot = "/home/user/projects/my-app",
    ApplyPreambleProperties = true,
    UnsetValueMode = ConfigurationUnsetValueMode.RemoveEffectiveValue, // EditorConfig sentinel
};

ConfigurationView view = doc.Resolve("src/Bodu/Foo.cs", options);
```

### Save (round-trip)

```csharp
IniDocument doc = ConfigurationDocument.Parse(source);

// Modify a value via the underlying IniDocument API.
doc.Sections[0].SetEntry("format.indent.size", "8");

ConfigurationDocument.Save(doc, "/tmp/output.boduconfig");
```

The writer emits canonical Bodu formatting by default. Use
<xref:Bodu.Text.Configuration.ConfigurationWriteOptions.EditorConfigCompatible> when round-tripping into an
EditorConfig-strict toolchain.

### Key parsing

```csharp
ConfigurationKey key = ConfigurationKey.Parse("logging.level.default");

string raw      = key.RawKey;             // "logging.level.default"
string canonical = key.Path;              // "logging:level:default"
ImmutableArray<string> segments = key.Segments; // ["logging", "level", "default"]

// Non-throwing variant.
if (ConfigurationKey.TryParse(userInput, out ConfigurationKey parsed))
{
    // parsed.Path is ready for Microsoft.Extensions.Configuration interop
}
```

### Iterate the resolved view

```csharp
ConfigurationView view = doc.Resolve("src/Foo.cs");

foreach (KeyValuePair<string, string?> kvp in view)
{
    Console.WriteLine($"{kvp.Key} = {kvp.Value}");
}

// Or directly via the underlying read-only dictionary.
IReadOnlyDictionary<string, string?> raw = view.Values;
```

The view implements `IEnumerable<KeyValuePair<string, string?>>` and exposes the underlying dictionary through
`Values`, `Keys`, and `Count`.

## End-to-end round-trip example

```csharp
using Bodu.Text.Configuration;
using Bodu.Text.Formats;

const string source = """
root = true

[*.cs]
format.indent.style = space
format.indent.size  = 4
""";

// Parse → Resolve → Read.
IniDocument doc = ConfigurationDocument.Parse(source);
ConfigurationView view = doc.Resolve("Bodu/Foo.cs");

string style = view.GetString("format:indent:style");                 // "space"
int size = view.GetInt32("format:indent:size");                       // 4

// Save → Parse again → Compare.
using StringWriter sw = new();
ConfigurationDocument.Save(doc, sw);

IniDocument reparsed = ConfigurationDocument.Parse(sw.ToString());
Debug.Assert(reparsed.Sections.Count == doc.Sections.Count);
Debug.Assert(reparsed.GlobalSection["root"] == doc.GlobalSection["root"]);
```

## Where to go next

- **[Core concepts](concepts.md)** — vocabulary refresher.
- **[Introduction](index.md)** — type map, scenario index.
- **[Bodu.Extensions.Configuration.Text](../extensions-configuration-text/index.md)** — plug into `IConfigurationBuilder`, bind to `IOptions<T>`.
- **[Bodu.Text.Configuration API reference](xref:Bodu.Text.Configuration)** — full type-by-type docs.
- **[Bodu.Text.Formats](../formats/index.md)** — the underlying `IniDocument` model.
