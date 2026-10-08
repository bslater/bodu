---
title: Line formats - parser policies
---

# Parser policies

Real-world CSV, `.env`, and INI files break their specs constantly. Each reader therefore couples a strict default with explicit, opt-in leniency knobs on its options struct.

## Common diagnostic surface

Every reader throws its format's `*FormatException` (`DelimitedFormatException`, `DotEnvFormatException`, `IniFormatException`) carrying the 1-based line number and byte offset at which the problem was detected. Serializer binding failures throw the format's `*SerializationException` instead.

## Delimited

`DelimitedReaderOptions` carries the RFC 4180 dialect knobs:

| Knob | Values | Default |
|---|---|---|
| `FieldCountBehavior` | `Strict` (every record matches the header's field count) / `Ragged` | `Strict` |
| `MalformedRecordBehavior` | `Throw` / `SkipRecord` (truncate the record at the structural error) | `Throw` |
| `DuplicateHeaderBehavior` | `Throw` / `TakeFirst` / `TakeLast` | `Throw` |
| `Delimiter`, `Quote` | any character | `,` / `"` |
| `NoHeader` | treat the first record as data (records become positional arrays) | header mode |
| `TrimFields` | trim unquoted fields | off |
| `AllowComments`, `CommentChar` | skip comment lines | off / `#` |

Strict field counts are measured against the header row, so they apply in header mode; positional mode accepts any shape unless you enforce one yourself.

Where the reader is always strict: an unterminated quoted field throws, and characters after a closing quote are a structural error (subject to `MalformedRecordBehavior`).

## DotEnv

The DotEnv dialect follows the mainstream `dotenv` implementations:

- `export KEY=value` binds as `KEY`; the prefix is shell syntax (preserved by the mutable DOM, surfaced via `Utf8DotEnvReader.CurrentIsExport`).
- Double-quoted values resolve escape sequences and may span lines; single-quoted values are literal; unquoted values end at an inline `#` comment.
- An empty value is a real value, distinct from an absent key.
- `DotEnvReaderOptions` can disable the `export` prefix, inline comments, or comment tokens (`DisallowExportPrefix`, `DisallowInlineComments`, `SkipComments`).

## INI

The `Utf8IniReader` dialect is deliberately conservative (the `configparser`-compatible reading):

- `=` is the only key/value delimiter; a `:` line is malformed.
- The spaces and tabs around a key and around a value are trimmed; everything else in a value is literal to the end of the line - quotes are preserved and an inline `;` or `#` is **content**, not a comment. Because the reader trims it, `Utf8IniWriter` refuses a value that begins or ends with a space or tab rather than write one that reads back without it.
- Both `;` and `#` start full-line comments (`IniReaderOptions.DisallowHashComments` restricts to `;`).
- A section name runs to the first `]` that only whitespace or a comment follows, so `[foo]bar]` names `foo]bar`. A comment after a header is skipped rather than reported as a comment token; any other text after the header is malformed.
- A leading UTF-8 BOM is skipped; LF, CRLF, and lone-CR line endings are equivalent.

Duplicate handling is a **document-model** policy (`IniDocumentOptions`, also surfaced on `IniSerializerOptions`), because the source-order reader reports the file verbatim:

| Knob | Values | Default |
|---|---|---|
| `DuplicateSectionBehavior` | `Merge` (later `[section]` blocks append to the first) / `Disallowed` | `Merge` |
| `DuplicateKeyBehavior` | `LastWins` / `FirstWins` / `Disallowed` | `LastWins` |

A global key that collides with a section of the same name is always rejected (`IniFormatException`), because both would claim the same root property; `IniSerializerOptions.GlobalSectionName` maps the global entries to a reserved root key instead of hoisting them, which removes the ambiguity. `IniSerializerDefaults.Strict` selects `Disallowed` for both duplicate policies - Python `configparser` strict mode.

## Where to go next

- The per-format guides: [Delimited](../../guides/formats/delimited.md), [DotEnv](../../guides/formats/dotenv.md), [INI](../../guides/formats/ini.md).
