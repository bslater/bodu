---
title: Using INI
---

# Using INI

`Bodu.Text.Ini` reads and writes sectioned `[name]` / `key=value` configuration files. The value model is a two-level object-of-objects: global keys (before the first section header) hoist onto the root, and each section is a nested object of string values.

## Pattern 1 — query a document

```csharp
using Bodu.Text.Ini;
using Bodu.Text.Ini.Document;

using IniDocument document = IniDocument.Parse(File.ReadAllBytes("app.ini"));
IniElement root = document.RootElement;

string environment = root.GetProperty("environment").GetString();      // global key
IniElement server = root.GetProperty("server");                        // [server] section
string host = server.GetProperty("host").GetString();

foreach (IniProperty property in root.EnumerateObject())
{
    // Globals surface first (String kind), then sections (Object kind).
    Console.WriteLine($"{property.Name}: {property.Value.ValueKind}");
}
```

## Pattern 2 — typed binding via the serializer

Scalar members bind global keys; object-shaped members (section POCOs or `Dictionary<string, string>`) bind sections:

```csharp
using Bodu.Text.Serialization;

sealed class AppConfig
{
    public string? Environment { get; set; }                  // global key
    public ServerSection? Server { get; set; }                // [server]
    public Dictionary<string, string>? Logging { get; set; }  // [logging]
}

sealed class ServerSection
{
    public string? Host { get; set; }
    public int Port { get; set; }
}

AppConfig config = IniSerializer.Deserialize<AppConfig>(
    iniText, new IniSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower });
```

`Dictionary<string, Dictionary<string, string>>` works as an all-sections root. A member nested beyond INI's two levels throws `IniSerializationException`.

### The global section name

Hoisting global keys onto the root is ergonomic but ambiguous when a global key and a section share a name (always rejected), and impossible for a nested-dictionary root. `IniSerializerOptions.GlobalSectionName` routes the global entries to and from a reserved root key instead:

```csharp
var options = new IniSerializerOptions { GlobalSectionName = "global" };
var all = IniSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(iniText, options);
string env = all["global"]["environment"];
```

## Pattern 3 — comment-preserving edits with the mutable DOM

```csharp
using Bodu.Text.Ini.Nodes;

IniObject root = IniNode.Parse(File.ReadAllBytes("app.ini"));

root["server"].AsObject()["port"].AsValue().Value = "9090";   // edit in place, trivia kept

var metrics = new IniObject();
var enabled = new IniValue("true");
enabled.LeadingComments.Add(" scrape target");                 // authored comment
metrics["enabled"] = enabled;
root["metrics"] = metrics;

File.WriteAllBytes("app.ini", root.ToUtf8Bytes());
```

Every comment line from the source survives (`LeadingComments` on sections and values, `TrailingComments` per object). Layout is canonicalized: `key=value` without padding, and global entries always precede the first section header. Inline comments are not modeled — the dialect keeps everything after `=` as value content.

## Pattern 4 — duplicate policies

Duplicates are resolved when the document is materialized, controlled by `IniDocumentOptions` (also on `IniSerializerOptions`):

```csharp
using Bodu.Text.Ini.Reader;

var strict = new IniDocumentOptions
{
    DuplicateSectionBehavior = IniDuplicateSectionBehavior.Disallowed,
    DuplicateKeyBehavior = IniDuplicateKeyBehavior.Disallowed,
};
using IniDocument document = IniDocument.Parse(bytes, IniReaderOptions.Default, strict);
```

The defaults merge repeated sections and keep the last duplicate key — the permissive Windows-profile reading. `IniSerializerDefaults.Strict` selects `Disallowed` for both — Python `configparser` strict mode.

## The two readers

`Utf8IniReader` streams the file **as authored** (section headers, keys, values, comments, in source order). The normalized `IniDocumentReader` pre-parses the whole document — duplicate-section merge declares structure out of source order — and emits the logical object-of-objects token stream that the serializer and read-only DOM consume.

## Dialect

`=` only (no `:`), values literal to end of line (quotes preserved, inline `;`/`#` kept as content), `;` and `#` full-line comments, BOM skipped, LF/CRLF/CR equivalent. See [Parser policies](../../docs/formats/parser-policies.md).

## Exceptions

`IniFormatException` for malformed input and duplicate-policy violations (line/offset attached); `IniSerializationException` for binding failures (non-object root, depth beyond two levels, missing `[Required]` member, non-convertible value).

## When to reach for `Bodu.Text.Configuration` instead

When you need EditorConfig-style behaviour — glob-targeted sections, layered resolution, typed views with diagnostics — use `Bodu.Text.Configuration`. It carries its own INI document model and does not depend on this package.

## See also

- [Streams and token-level I/O](streaming.md)
- [Choosing a text format](choosing-a-format.md)
