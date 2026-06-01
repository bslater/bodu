---
title: Using INI
---

# Using INI

`Ini` is the static codec for the INI configuration-file dialect — section-based `[name]` headers with key/value entries, optional global preamble, `#` and `;` comments, and EditorConfig-compatible variants. The codec round-trips comments as trivia on entries and sections, so an `Ini.Parse` → mutate → `Ini.Format` cycle preserves authored structure.

The surface mirrors the [Bencode codec](bencode.md): `Parse` / `TryParse` / `Format` over spans and the document model. Unlike Delimited, there is no streaming reader / writer — INI files are configuration, not data, and don't warrant one.

For the vocabulary used below (document, section, entry, comment, parse options) see [Core concepts](../../docs/formats/concepts.md).

## Pattern 1 — parse a configuration file

```csharp
using Bodu.Text.Formats;

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
using Bodu.Text.Formats;

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
using Bodu.Text.Formats;

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

By default (`PreserveComments: true`), full-line comments before a section header attach to the section's `LeadingComments`; comments before an entry attach to the entry's `LeadingComments`; a `#` or `;` on the same line as an entry attaches as the entry's `InlineComment`. Both prefix characters are recognised and the original prefix is preserved.

## Pattern 4 — programmatic mutation

```csharp
using Bodu.Text.Formats;

IniDocument document = Ini.Parse(input);

IniSection database = document.GetOrAddSection("database");
database.SetEntry("host", "10.0.0.1");
database.SetEntry("port", "5433");

document.RemoveSection("cache");

string output = Ini.Format(document);
File.WriteAllText("app.ini", output);
```

Unlike `BencodedValue`, `DotEnvDocument`, and `DelimitedDocument`, `IniDocument` is **mutable**: `AddSection`, `RemoveSection`, `GetOrAddSection`, and the per-section `SetEntry` / `RemoveEntry` / `ClearEntries` methods let you compose a configuration programmatically and then write it out. Comments can be set via `AddLeadingComment` / `SetLeadingComments` / `ClearLeadingComments` on either entry or section.

## Pattern 5 — duplicate-section policies

Many real-world INI files have the same section name in multiple places. The behaviour is controlled by `DuplicateSectionBehavior`:

| Member | Effect |
|---|---|
| `Merge` / `MergeAll` *(default)* | Later occurrences merge into the first under the active `DuplicateKeyBehavior`. |
| `Disallowed` | Duplicate section name raises `IniFormatException`. |
| `Preserve` | Duplicates are retained as separate `IniSection` objects in source order. |
| `MergeAdjacent` | Merge only consecutive duplicates; otherwise preserve as separate sections. |

Combine with `DuplicateKeyBehavior` to control the within-section semantics:

| Member | Effect |
|---|---|
| `LastWins` *(default)* | Last occurrence replaces earlier ones. |
| `FirstWins` | First occurrence wins. |
| `Disallowed` | Duplicate key raises `IniFormatException`. |

## Pattern 6 — round-trip through `Format`

```csharp
using Bodu.Text.Formats;

IniDocument document = Ini.Parse(input);
string roundTrip = Ini.Format(document);
```

`Format` writes the document back to text: global entries first (when present), then a blank line, then each section with a blank line separator. Leading comments precede their owning entry or section; inline comments are appended on the same line as their entry. Keys and values are trimmed of surrounding whitespace on parse, so the formatted output does not preserve original padding — pin to the original bytes if byte-stable round-tripping matters.

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

The parser accepts both `=` and `:` as the key / value separator. Comment lines may start with either `#` or `;` — both are recognised, and the prefix is preserved on the `IniComment` value struct so a round trip retains the original convention.

## Exceptions

`IniFormatException` derives from `TextFormatException` and carries a `LineNumber` property (1-based; 0 when the source line is unknown). Thrown for unterminated section headers, keys outside a valid context when the global section is disallowed, duplicate keys / sections under their respective `Disallowed` policies, and empty section names.

## Extension methods

The `IniExtensions` helpers add fluent overloads:

```csharp
using Bodu.Text.Formats;

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

- [Bencode](bencode.md), [Delimited](delimited.md), [DotEnv](dotenv.md) — the other formats in the package.
- [`Bodu.Text.Ini` API reference](xref:Bodu.Text.Ini)
- [`Bodu.Text.Configuration` overview](../text-configuration/index.md) — for the resolved / view-projecting / `IConfiguration`-bridging surface built on this codec.
