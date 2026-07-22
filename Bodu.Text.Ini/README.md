# Bodu.Text.Ini

> **API stability — Preview.** The public API surface is largely settled but is still being finalized ahead of the 1.0 release and may change; breaking changes can land in a minor version until then.

An INI library for .NET 8, shaped after `System.Text.Json`: a typed serializer over a two-reader token surface, with a comment-preserving mutable document object model and a read-only one. The value model is a two-level object-of-objects — global keys hoist onto the root, each `[section]` is a nested object of string values.

## Installation

```shell
dotnet add package Bodu.Text.Ini
```

Targets `net8.0`. Also available through the `Bodu.Text.Formats` umbrella package.

## API shape

| Type(s) | Namespace | Role |
|---|---|---|
| `IniSerializer` / `IniSerializerOptions` / `IniSerializerDefaults` | `Bodu.Text.Ini` | Static serializer entry point, configuration (`GlobalSectionName`, duplicate policies), and presets (`Strict` = configparser strict mode). |
| `IniDocumentOptions`, `IniDuplicateSectionBehavior` / `IniDuplicateKeyBehavior` | `Bodu.Text.Ini` | The document-model duplicate policies (merge / disallow sections; last-wins / first-wins / disallow keys). |
| `IniFormatException` / `IniSerializationException` | `Bodu.Text.Ini` | Failures split by cause: malformed input or policy violations vs. values that cannot be mapped. |
| `Utf8IniReader` (+ `IniReaderOptions`) | `Bodu.Text.Ini.Reader` | Forward-only `ref struct` reader over the file **as authored** (source order, comments included). |
| `IniDocumentReader` | `Bodu.Text.Ini.Reader` | Normalized cursor over the logical object-of-objects shape (globals hoisted, duplicate sections merged). |
| `Utf8IniWriter` (+ `IniWriterOptions`) | `Bodu.Text.Ini.Writer` | Forward-only `ref struct` writer (section headers, entries, comment lines). |
| `IniDocument` / `IniElement` / `IniProperty` | `Bodu.Text.Ini.Document` | Read-only, trivia-free document object model. |
| `IniNode` / `IniObject` / `IniValue` | `Bodu.Text.Ini.Nodes` | Mutable, **comment-preserving** document object model: parse, edit, write back with the human's annotations intact. |

```csharp
using Bodu.Text.Ini.Nodes;

IniObject root = IniNode.Parse(File.ReadAllBytes("app.ini"));
root["server"].AsObject()["port"].AsValue().Value = "9090";
File.WriteAllBytes("app.ini", root.ToUtf8Bytes());   // comments survive
```

The dialect is deliberately conservative: `=` only, values literal to end of line (inline `;`/`#` is content), `;` and `#` full-line comments, BOM and mixed line endings handled.
