---
title: Choosing a text format
---

# Choosing a text format

The three line formats cover different jobs. This guide helps you pick, and explains what round-trips faithfully and how each parser recovers from dirty input.

## Decision matrix

| You have / need | Reach for |
|---|---|
| Tabular data exchanged with spreadsheets, databases, other systems | **Delimited** (`Bodu.Text.Delimited`) |
| Per-environment process settings, secrets injected at deploy time | **DotEnv** (`Bodu.Text.DotEnv`) |
| Human-owned configuration with sections and comments | **INI** (`Bodu.Text.Ini`) |
| Layered configuration with glob targets and typed views (EditorConfig-style) | `Bodu.Text.Configuration` |
| Nested structures, arrays, non-string scalars | TOML / YAML / Bencode (the structured-text quartet) |

The line formats are **flat**: Delimited is an array of records, DotEnv a single object, INI a two-level object-of-objects. If your data nests deeper, use a structured format instead of encoding structure into key names.

## Round-trip fidelity

- **Delimited** round-trips values exactly; layout is canonicalized (quotes are emitted only where required, rows end in the writer's line ending). Use the mutable `DelimitedNode` DOM to parse → edit → rewrite.
- **DotEnv** round-trips keys, values, and each entry's `export` flag through the mutable DOM; quoting is canonicalized on write.
- **INI** is the fidelity flagship: the mutable `IniNode` DOM preserves every leading comment line and the end-of-scope comment block, so parse → edit → write keeps a human's annotations. Whitespace around `=` is canonicalized (`key=value`), and global entries are always emitted before the first section header. Inline comments are not modeled — the dialect treats them as value content.

The read-only `*Document` DOMs are for querying, not round-tripping: they drop comment trivia.

## Mutation

Author or edit through the mutable node DOMs — insertion-ordered maps (`DotEnvObject`, `IniObject`) or record lists (`DelimitedArray`), written back with `ToUtf8Bytes()` / `ToString()` or `WriteTo(ref Utf8*Writer)` for custom writer options (e.g. a tab delimiter). For typed data, skip the DOM entirely and round-trip POCOs through the `*Serializer`.

## Parser error recovery

All three readers throw their `*FormatException` with a line/offset by default. Leniency is opt-in per dialect:

- **Delimited** — `FieldCountBehavior.Ragged` accepts records whose field count differs from the header; `MalformedRecordBehavior.SkipRecord` truncates a structurally broken record instead of throwing; `DuplicateHeaderBehavior` picks a winner for repeated column names.
- **DotEnv** — the reader is line-incremental, so one malformed line fails fast with its position; quoting errors (an unterminated quote) always throw.
- **INI** — malformed lines (no `=`, unterminated `[section`) always throw; duplicate sections and keys are resolved by the document-model policies (`Merge` / `Disallowed`, `LastWins` / `FirstWins` / `Disallowed`).

See [Parser policies](../../docs/formats/parser-policies.md) for the full table.

## See also

- [Using delimited](delimited.md) · [Using DotEnv](dotenv.md) · [Using INI](ini.md)
- [Streams and token-level I/O](streaming.md)
