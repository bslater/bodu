---
title: Bodu.Text.Formats — parser policies
---

# Parser policies

The `Bodu.Text.Formats` parsers default to **strict** behaviour: anything the grammar cannot unambiguously interpret raises a [`TextFormatException`](xref:Bodu.Text.TextFormatException) rather than silently dropping data. This page summarises the options that control the strictness, the defaults each parser ships with, and how to opt into the historical lenient behaviour where it exists.

## Common diagnostic surface

Every format-specific exception derives from `TextFormatException`, so a single `catch` block can handle parse failures across Delimited, DotEnv, and INI sources:

```csharp
try
{
    IniDocument document = Ini.Parse(source);
}
catch (TextFormatException ex)
{
    Console.Error.WriteLine(
        $"Parse failed at line {ex.LineNumber}, column {ex.ColumnNumber}, offset {ex.Offset}: {ex.Message}");
}
```

| Property | Meaning |
|---|---|
| `LineNumber` | 1-based source line; `0` when the parser cannot identify a line. |
| `ColumnNumber` | 1-based column within the line; `0` when the column is unknown or the format does not track it. |
| `Offset` | 0-based byte/character offset from the start of the source; `null` when not tracked. |

The location properties are advisory: each parser populates the fields it can identify. The line-oriented formats report a line (and, where tracked, a column).

## Delimited

`DelimitedParseOptions` carries three policies that govern strictness. All three default to the strictest setting; relax them only when the source genuinely requires it.

| Option | Default | Strict behaviour | Opt-in alternatives |
|---|---|---|---|
| `DuplicateHeaderBehavior` | `Throw` | Reject a header row that contains duplicate column names. | `FirstWins`, `LastWins`, `AllowDuplicates` |
| `MalformedRecordBehavior` | `Throw` | Reject any character that follows a closing quote before the next delimiter or line break. | `SkipRecord` (historical lenient mode — discards the rest of the row). |
| `FieldCountBehavior` | `Strict` | When the document has a header, every data row must have the same field count. | `Ragged` |

```csharp
DelimitedParseOptions lenient = new()
{
    DuplicateHeaderBehavior = DelimitedDuplicateHeaderBehavior.LastWins,
    MalformedRecordBehavior = DelimitedMalformedRecordBehavior.SkipRecord,
    FieldCountBehavior      = DelimitedFieldCountBehavior.Ragged,
};

DelimitedDocument doc = Delimited.Parse(source.AsSpan(), lenient);
```

`FieldCountBehavior` is not enforced when `HasHeader` is `false` because there is no reference field count to compare against.

## DotEnv

| Option | Default | Behaviour |
|---|---|---|
| `PreserveComments` | `true` | Retains `#` comment lines that precede an entry on `DotEnvEntry.LeadingComments`, and emits them ahead of the corresponding `KEY=VALUE` line on `Format`. |
| `DuplicateKeyBehavior` | `LastWins` | Existing option — `FirstWins`, `LastWins`, or `Disallowed`. |

Round-tripping a comment-annotated `.env` file is now lossless for leading comments:

```text
# API key for production
API_KEY=secret
```

Both `Parse` and `Format` preserve the comment in the resulting document. Set `PreserveComments = false` when the parsed model is purely data and comments should be discarded.

## INI

The INI parser has the broadest existing policy surface. See [`DuplicateKeyPolicy`](xref:Bodu.Text.DuplicateKeyPolicy) and [`IniDuplicateSectionBehavior`](xref:Bodu.Text.Ini.IniDuplicateSectionBehavior) for the duplicate-resolution modes, and `IniParseOptions.PreserveComments` for trivia retention.

## Migration notes

These defaults changed compared with earlier pre-1.0 builds:

- A Delimited source with duplicate column names now throws; previously the first occurrence was silently overwritten in the name-based lookup.
- A Delimited row that has a character between a closing quote and the next delimiter or newline now throws; previously the rest of the row was silently discarded.
- A Delimited document with a header row whose data rows differ in field count now throws; previously the mismatch produced rows whose `Count` did not match the header.

Each of these is opt-in to the legacy behaviour through the option flag listed above.
