---
title: Using INI
---

# Using INI

`Bodu.Text.Ini` reads and writes sectioned `[name]` / `key=value` configuration files. The value model is a two-level object-of-objects: global keys (before the first section header) hoist onto the root, and each section is a nested object of string values.

## Pattern 1 - query a document

<!-- compile -->
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

## Pattern 2 - typed binding via the serializer

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

### Reflection-free binding

Annotate a partial section type with `[IniSection]` and reference the `Bodu.Text.Formats.Generators` source generator, and a static `IniFactory` property (`IIniSectionFactory<ServerSection>`) is emitted at compile time. The section overloads - `IniSerializer.SerializeSection("server", section, ServerSection.IniFactory)` / `DeserializeSection(iniText, "server", ServerSection.IniFactory)` - bind one section through the factory instead of the reflection binder, making the path trimming- and AOT-safe. An empty section name addresses the document's global keys; duplicate-section merge still applies before binding.

## Pattern 3 - comment-preserving edits with the mutable DOM

<!-- compile -->
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

Every comment line from the source survives (`LeadingComments` on sections and values, `TrailingComments` per object). Layout is canonicalized: `key=value` without padding, and global entries always precede the first section header. Inline comments are not modeled - the dialect keeps an inline `;` or `#` and the text after it as value content.

## Pattern 4 - duplicate policies

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

The defaults merge repeated sections and keep the last duplicate key - the permissive Windows-profile reading. `IniSerializerDefaults.Strict` selects `Disallowed` for both - Python `configparser` strict mode.

## The two readers

`Utf8IniReader` streams the file **as authored** (section headers, keys, values, comments, in source order). The normalized `IniDocumentReader` pre-parses the whole document - duplicate-section merge declares structure out of source order - and emits the logical object-of-objects token stream that the serializer and read-only DOM consume.

## Dialect

`=` only (no `:`), keys and values trimmed of the spaces and tabs around them, values otherwise literal to end of line (quotes preserved, inline `;`/`#` kept as content), `;` and `#` full-line comments, BOM skipped, LF/CRLF/CR equivalent. See [Parser policies](../../docs/formats/parser-policies.md).

A section name runs to the first `]` that only whitespace or a comment follows, so a name may contain `]`: `[foo]bar]` names the section `foo]bar`, and `[server] ; primary` names `server`. A comment after a header is skipped, not reported as a comment token, so the mutable DOM does not keep it; any other text after the header, as in `[server] primary`, throws `IniFormatException`.

## Writing

`Utf8IniWriter` writes only text that `Utf8IniReader` reads back unchanged, and throws `ArgumentException` for the text below, so the mutable DOM's `WriteTo` and `ToUtf8Bytes` refuse it too, and the serializer's `Serialize`, `SerializeAsync` and `SerializeSection` overloads report it as an `IniSerializationException` that names the section and key, with the writer's `ArgumentException` as its inner exception:

- a **value** containing a line break, which a `key=value` line cannot hold, or beginning or ending with a space or tab, which the reader trims (an empty value is written as `key=`);
- a **key** that is empty, begins or ends with a space or tab, contains `=` or a line break, or begins with `[`, `;` or `#`, which would read back as another key, a section header or a comment (`#` is refused even where `#` comments are disallowed, because the writer cannot know how its output will be read);
- a **section name** that is empty, begins or ends with a space or tab, contains a line break, or holds a `]` followed, after optional whitespace, by `;` or `#`, where the reader would end the name (any other `]` is fine: `WriteSectionHeader("foo]bar")` writes `[foo]bar]`, which reads back as `foo]bar`);
- a **key or section name** that begins with U+FEFF, which the reader skips as a byte order mark at the start of a document (refused wherever the name is written, so that the rule does not depend on position).

A comment containing line breaks is written as one comment line per line, each with the comment prefix, so `WriteComment("first\nsecond")` writes `;first` and `;second`.

## Exceptions

`IniFormatException` for malformed input and duplicate-policy violations (line/offset attached; a duplicate is reported at the offending section header or key); `IniSerializationException` for binding failures (non-object root, depth beyond two levels, missing `[Required]` member, non-convertible value, a key, section name or value INI cannot represent); `ArgumentException` from the writer and the mutable DOM for text they refuse to write (see [Writing](#writing)).

## When to reach for `Bodu.Text.Configuration` instead

When you need EditorConfig-style behaviour - glob-targeted sections, layered resolution, typed views with diagnostics - use `Bodu.Text.Configuration`. It carries its own INI document model and does not depend on this package.

## See also

- [Streams and token-level I/O](streaming.md)
- [Choosing a text format](choosing-a-format.md)
