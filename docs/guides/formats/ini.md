---
title: Using INI
---

# Using INI

`Ini` is the static codec for the INI configuration-file dialect — section-based `[name]` headers with key/value entries, optional global preamble, `#` and `;` comments, and EditorConfig-compatible variants. The codec round-trips comments as trivia on entries and sections, so an `Ini.Parse` → mutate → `Ini.Format` cycle preserves authored structure.

It exposes the package's common codec shape: `Parse` / `TryParse` / `Format` over spans and the document model. Unlike Delimited, there is no streaming reader / writer — INI files are configuration, not data, and don't warrant one.

For the vocabulary used below (document, section, entry, comment, parse options) see [Core concepts](../../docs/formats/concepts.md).

## Pattern 1 — parse a configuration file

<!-- compile -->
```csharp
using Bodu.Text.Ini;

string ini = """
# Application root
appName=Bodu.Sample

[database]
host=localhost
port=5432

[cache]
ttl=300
maxItems=1024
""";

IniDocument document = Ini.Parse(ini);

string? appName = document.GlobalSection["appName"];   // "Bodu.Sample"

IniSection? db = document.GetSection("database");
string? host   = db?["host"];                          // "localhost"
int port       = db?.GetValue<int>("port") ?? 0;       // 5432
```

`Parse(ReadOnlySpan<char>)` produces an `IniDocument` with two faces: a `GlobalSection` for entries preceding the first `[section]` header (the *preamble*) and a `Sections` list for every named section, in source order. Both `IniSection` and `IniEntry` expose typed `GetValue<T>` and `TryGetValue<T>` accessors that parse via `ISpanParsable<T>` under `CultureInfo.InvariantCulture`.

## Pattern 2 — non-throwing parse

```csharp
using Bodu.Text.Ini;

if (Ini.TryParse(source, out IniDocument? document))
{
    Configure(document);
}
else
{
    log.Warn("Malformed INI input");
}
```

`TryParse` returns `false` and sets `document` to `null` on the first parse error rather than raising `IniFormatException`. Use this pattern for untrusted input.

## Pattern 3 — preserved comments

```csharp
using Bodu.Text.Ini;

foreach (IniSection section in document.Sections)
{
    foreach (IniComment c in section.LeadingComments)
        Console.WriteLine($"; {c.Text}");

    Console.WriteLine($"[{section.Name}]");

    foreach (IniEntry entry in section.Entries)
    {
        foreach (IniComment c in entry.LeadingComments)
            Console.WriteLine($"; {c.Text}");

        Console.WriteLine($"{entry.Key} = {entry.Value}" +
                          (entry.InlineComment is { } ic ? $"  ; {ic.Text}" : ""));
    }
}
```

By default (`PreserveComments: true`), full-line comments before a section header attach to the section's `LeadingComments`, and full-line comments before an entry attach to the entry's `LeadingComments`. Both `#` and `;` are recognised and the original prefix character is preserved.

> [!IMPORTANT]
> The parser does **not** split a trailing inline comment out of a value. Everything after the first `=` / `:` separator (trimmed) becomes the entry's `Value`, so `host = localhost ; primary` parses to the value `localhost ; primary`. The `IniEntry.InlineComment` property is write-side only: set it yourself to have `Ini.Format` emit a trailing comment on the entry's line. The example below reads `InlineComment`, which is therefore populated only when you assigned it.

## Pattern 4 — programmatic mutation

```csharp
using Bodu.Text.Ini;

IniDocument document = Ini.Parse(input);

IniSection database = document.GetOrAddSection("database");
database.SetEntry("host", "10.0.0.1");
database.SetEntry("port", "5433");

document.RemoveSection("cache");

string output = Ini.Format(document);
File.WriteAllText("app.ini", output);
```

Unlike `DotEnvDocument` and `DelimitedDocument`, `IniDocument` is **mutable**: `AddSection`, `RemoveSection`, `GetOrAddSection`, and the per-section `SetEntry` / `RemoveEntry` / `ClearEntries` methods let you compose a configuration programmatically and then write it out. Comments can be set via `AddLeadingComment` / `SetLeadingComments` / `ClearLeadingComments` on either entry or section.

## Pattern 5 — duplicate-section policies

Many real-world INI files have the same section name in multiple places. The behaviour is controlled by `DuplicateSectionBehavior`:

| Member | Effect |
|---|---|
| `Merge` *(default)* | Every later occurrence merges into the **first** section of that name, under the active `DuplicateKeyBehavior`. |
| `Disallowed` | Duplicate section name raises `IniFormatException`. |
| `Preserve` | Duplicates are retained as separate `IniSection` objects in source order. |
| `MergeAdjacent` | Merge only when the duplicate immediately follows the same section; a non-adjacent repeat starts a new section. |

Combine with `DuplicateKeyBehavior` to control the within-section semantics:

| Member | Effect |
|---|---|
| `LastWins` *(default)* | Last occurrence replaces earlier ones. |
| `FirstWins` | First occurrence wins. |
| `Disallowed` | Duplicate key raises `IniFormatException`. |

## Pattern 6 — round-trip through `Format`

```csharp
using Bodu.Text.Ini;

IniDocument document = Ini.Parse(input);
string roundTrip = Ini.Format(document);
```

`Format` writes the document back to text: global entries first (when present), then each named section, separated by a blank line. Entries are written as `key = value` (one space either side of `=`). Leading comments precede their owning entry or section; a programmatically set inline comment is appended after the value on the same line, with its prefix. Keys and values are trimmed of surrounding whitespace on parse, so the formatted output does not preserve original padding or the original separator character (a `:` separator re-emits as `=`) — pin to the original bytes if byte-stable round-tripping matters. Each line is terminated with the platform `Environment.NewLine`.

## Behaviour options

The `IniParseOptions` fields control the format dialect:

| Field | Default | Controls |
|---|---|---|
| `AllowGlobalSection` | `true` | Permit entries before the first `[section]` header. |
| `CaseSensitiveSections` | `false` | Section-name comparison (case-insensitive by default). |
| `CaseSensitiveKeys` | `false` | Within-section key comparison (case-insensitive by default). |
| `DuplicateKeyBehavior` | `LastWins` | See the table above. |
| `DuplicateSectionBehavior` | `Merge` | See the table above. |
| `PreserveComments` | `true` | Retain comments as trivia on entries / sections. |

## Separators and comment markers

The parser accepts both `=` and `:` as the key / value separator — whichever appears **first** on the line wins, so `path = C:\tmp` splits on the `=` and keeps `C:\tmp` as the value. Key and value are each trimmed of surrounding whitespace. A non-comment line with no separator is treated as a key with an empty value (`flag` ⇒ key `flag`, value `""`); a line whose key trims to empty raises `IniFormatException`.

Comment lines may start with either `#` or `;` — both are recognised, and the prefix is preserved on the `IniComment` value struct so a round trip retains the original convention. Section and key name comparisons are ordinal-ignore-case by default; set `CaseSensitiveSections` / `CaseSensitiveKeys` for ordinal matching.

## Duplicate-section merge in practice

```csharp
using Bodu.Text.Ini;

string source = """
    [server]
    host = a

    [other]
    x = 1

    [server]
    port = 9000
    """;

// Default Merge: the second [server] folds into the first.
IniDocument merged = Ini.Parse(source);
// merged.Sections has 2 sections; [server] carries both host and port.

// MergeAdjacent: the [server] blocks are not adjacent, so they stay separate.
IniDocument split = Ini.Parse(source,
    new IniParseOptions { DuplicateSectionBehavior = IniDuplicateSectionBehavior.MergeAdjacent });
// split.Sections has 3 sections: [server], [other], [server].
```

## Exceptions

`IniFormatException` derives from `TextFormatException` and carries a `LineNumber` property (1-based; 0 when the source line is unknown). Thrown for unterminated section headers, keys outside a valid context when the global section is disallowed, duplicate keys / sections under their respective `Disallowed` policies, and empty section names.

## Extension methods

The `IniExtensions` helpers add fluent overloads:

```csharp
using Bodu.Text.Ini;

IniDocument doc = ini.ParseIni();
string output    = doc.FormatIni();
```

`ParseIni`, `TryParseIni`, and `FormatIni` mirror the static-class entry points.

## When to reach for `Bodu.Text.Configuration` instead

`Bodu.Text.Formats.Ini` is the **codec** — `Parse` and `Format` over a faithful document model. It does not split dotted keys into hierarchical paths, evaluate EditorConfig-style globs, resolve overrides, or bridge to `Microsoft.Extensions.Configuration`. For those, reach for [`Bodu.Text.Configuration`](../text-configuration/index.md) — which uses this codec under the hood but adds the parsing-profile / view-resolution / typed-getter surface that consumers of `IConfiguration` are used to.

A rough decision tree:

- **You need to read or write `.ini` faithfully and you don't care about path resolution.** Use `Bodu.Text.Formats.Ini`.
- **You need typed configuration accessors, dotted-path lookup, EditorConfig globs, or `IConfiguration` integration.** Use [`Bodu.Text.Configuration`](../text-configuration/index.md) (and, for `IConfiguration`, [`Bodu.Extensions.Configuration.Text`](../extensions-configuration-text/index.md)).

## When *not* to use `Ini`

- **Tabular data.** Reach for [Delimited](delimited.md).
- **Flat environment-style configuration.** Reach for [DotEnv](dotenv.md).
- **Strict round-trip fidelity at the byte level.** Parse normalises whitespace and discards blank lines from sections.

## See also

- [Delimited](delimited.md), [DotEnv](dotenv.md) — the other formats in the package.
- [`Bodu.Text.Ini` API reference](xref:Bodu.Text.Ini)
- [`Bodu.Text.Configuration` overview](../text-configuration/index.md) — for the resolved / view-projecting / `IConfiguration`-bridging surface built on this codec.
- **[Text & Serialization guides](../topics/text-and-serialization.md)** — every guide in this topic, across Bodu.Text.Encoding, Bodu.Text.Formats, and the Bencode / TOML serializers.
