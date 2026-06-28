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

### Where the Delimited parser is strict vs. lenient

The quoting engine implements RFC 4180 with a handful of deliberate, documented relaxations:

| Construct | RFC 4180 | Bodu parser |
|---|---|---|
| Quote that opens a field | A field is quoted only when its **first** character is the quote. | Same — a quote that appears mid-field in an otherwise unquoted field is a literal character, never the start of a quoted region. |
| Doubled quote inside a quoted field | Two consecutive quotes encode one literal quote. | Same. |
| Embedded line break in a quoted field | Permitted; the field spans lines. | Same — literal `\n` / `\r` are preserved verbatim inside the field. |
| Unterminated quoted field (EOF before the closing quote) | Undefined. | Always throws `DelimitedFormatException`, regardless of `MalformedRecordBehavior`. |
| Trailing text after a closing quote | Undefined. | Governed by `MalformedRecordBehavior` — `Throw` (default) or `SkipRecord`. |
| Blank line | Skipped. | Skipped (zero characters before the line break). |
| Comment line | Not part of RFC 4180. | Skipped only when `AllowComments` is `true` and the line's first character is `CommentChar`. |

`TrimFields` trims **unquoted** fields only — a value's surrounding whitespace inside a quoted field is always preserved, because quoting is the explicit signal that the whitespace is significant. An unterminated quote is a hard error: `SkipRecord` relaxes *trailing junk after a close*, not a *missing close*.

## DotEnv

| Option | Default | Behaviour |
|---|---|---|
| `PreserveComments` | `true` | Retains full-line `#` comment lines that precede an entry on `DotEnvEntry.LeadingComments`, and emits them ahead of the corresponding `KEY=VALUE` line on `Format`. |
| `DuplicateKeyBehavior` | `LastWins` | Resolves a repeated key via the shared `DuplicateKeyPolicy` — `FirstWins`, `LastWins`, or `Disallowed`. |
| `AllowExportPrefix` | `true` | Strips a leading `export ` (the word `export` plus one or more spaces/tabs) before the key. |
| `AllowInlineComments` | `true` | In an **unquoted** value, a `#` preceded by whitespace terminates the value; the remainder is discarded (not retained as trivia). |

Round-tripping a comment-annotated `.env` file is lossless for leading comments:

```text
# API key for production
API_KEY=secret
```

Both `Parse` and `Format` preserve the comment in the resulting document. Set `PreserveComments = false` when the parsed model is purely data and comments should be discarded.

### DotEnv quoting and escaping

The parser recognises three value forms; the choice is made by the first non-whitespace character after `=`:

| Form | Syntax | Escapes | Multi-line |
|---|---|---|---|
| Unquoted | `KEY=value` | None — value runs to an inline-comment boundary, end-of-line, or end-of-input; leading/trailing whitespace trimmed. | No |
| Single-quoted | `KEY='value'` | None — content is literal. | No (a line break before the closing `'` throws) |
| Double-quoted | `KEY="value"` | `\"`, `\\`, `\n`, `\t`, `\r`, `\$`, plus `\<newline>` line continuation (the backslash and newline are discarded). | Yes — literal embedded newlines are preserved |

> [!NOTE]
> The double-quote escape set is exactly the six listed sequences plus the line continuation. An unrecognised escape (for example `\q`) is preserved verbatim as `\q` — the backslash is **not** dropped. There is no `\xHH` / `\uHHHH` numeric escape. An `=`-less line, an empty or invalid key, or a quote left open at end-of-line / end-of-input throws `DotEnvFormatException`.

## INI

The INI parser has the broadest policy surface. Sections are introduced by a `[name]` header; entries before the first header live in the **global** section (the preamble). The key/value separator is the first `=` **or** `:` on the line, with key and value trimmed; a separator-less non-comment line is treated as a key with an empty value. Full-line `#` / `;` comments attach as `LeadingComments` to the next section or entry.

| Option | Default | Behaviour |
|---|---|---|
| `AllowGlobalSection` | `true` | When `false`, a key before the first `[section]` header throws. |
| `CaseSensitiveSections` | `false` | Section-name comparison; ordinal when `true`, ordinal-ignore-case when `false`. |
| `CaseSensitiveKeys` | `false` | Within-section key comparison. |
| `DuplicateKeyBehavior` | `LastWins` | Shared `DuplicateKeyPolicy` (`FirstWins` / `LastWins` / `Disallowed`), applied within each resolved section. |
| `DuplicateSectionBehavior` | `Merge` | <xref:Bodu.Text.Ini.IniDuplicateSectionBehavior> — see below. |
| `PreserveComments` | `true` | Retain comment lines as trivia; when `false` the comment list is never populated. |

The duplicate-section modes (<xref:Bodu.Text.Ini.IniDuplicateSectionBehavior>):

| Member | Effect |
|---|---|
| `Merge` *(default)* | Every later occurrence of a section name folds into the **first** builder of that name; keys then resolve under `DuplicateKeyBehavior`. |
| `MergeAdjacent` | Merge only when the duplicate is the *immediately preceding* section; a non-adjacent repeat starts a new section. |
| `Preserve` | Keep every occurrence as a separate `IniSection` in source order. |
| `Disallowed` | A repeated section name throws `IniFormatException`. |

> [!IMPORTANT]
> A malformed header — a `[` with no matching `]`, or an empty name `[]` — throws `IniFormatException`. A line whose key trims to empty (e.g. `= value`) throws as a missing key. See [`DuplicateKeyPolicy`](xref:Bodu.Text.DuplicateKeyPolicy) for the shared key-resolution modes.

## Migration notes

These defaults changed compared with earlier pre-1.0 builds:

- A Delimited source with duplicate column names now throws; previously the first occurrence was silently overwritten in the name-based lookup.
- A Delimited row that has a character between a closing quote and the next delimiter or newline now throws; previously the rest of the row was silently discarded.
- A Delimited document with a header row whose data rows differ in field count now throws; previously the mismatch produced rows whose `Count` did not match the header.

Each of these is opt-in to the legacy behaviour through the option flag listed above.
