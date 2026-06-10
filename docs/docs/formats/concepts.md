---
title: Bodu.Text.Formats — Core concepts
---

# Bodu.Text.Formats — Core concepts

This page is the vocabulary the rest of the documentation assumes. Read it once before the [getting-started samples](getting-started.md) or the [guides](../../guides/formats/index.md), and refer back to it whenever a term feels imprecise.

For the high-level shape of the library, start with the [introduction](index.md).

> Object-mapping serializers (POCO ↔ format) — including **Bencode** — live in the standalone <xref:Bodu.Text.Bencode> and <xref:Bodu.Text.Toml> packages and follow a different, `System.Text.Json`-shaped model. This page is about the **document codecs** in `Bodu.Text.Formats`.

## Format and codec

A **format** is the wire grammar. The package ships four — Delimited, DotEnv, INI, and TOML. Each defines the kinds of value it carries, the syntax that delimits them, and the invariants every parser enforces.

A **codec** is the static façade that exposes parse and format operations for a format — <xref:Bodu.Text.Delimited.Delimited>, <xref:Bodu.Text.DotEnv.DotEnv>, <xref:Bodu.Text.Ini.Ini>, and the <xref:Bodu.Text.Toml.Toml> façade over the <xref:Bodu.Text.Toml.TomlReader> / <xref:Bodu.Text.Toml.TomlWriter> pair. Each exposes the span / `Stream` overloads and `Try…` variants around a single parser and writer.

## Value and document

A **value** is one node in a decoded model. The shape varies by format:

- **Delimited** — a <xref:Bodu.Text.Delimited.DelimitedRow> whose `Fields` are `IReadOnlyList<string>`.
- **DotEnv** — a <xref:Bodu.Text.DotEnv.DotEnvEntry> key/value pair.
- **INI** — an <xref:Bodu.Text.Ini.IniEntry> within an <xref:Bodu.Text.Ini.IniSection>.
- **TOML** — a node deriving from <xref:Bodu.Text.Toml.TomlValue>, exposing a `Kind` that returns the matching <xref:Bodu.Text.Toml.TomlValueKind> member.

A **document** is the whole parsed model — a <xref:Bodu.Text.Delimited.DelimitedDocument>, a <xref:Bodu.Text.DotEnv.DotEnvDocument>, an <xref:Bodu.Text.Ini.IniDocument>, or the root <xref:Bodu.Text.Toml.TomlTable>.

## The TOML value model

TOML is the package's one richly-typed tree. Its value kinds map onto first-class BCL types:

| TOML kind | <xref:Bodu.Text.Toml.TomlValue> subtype | CLR projection |
|---|---|---|
| String (all four forms) | <xref:Bodu.Text.Toml.TomlString> | `string` |
| Integer (four radices) | <xref:Bodu.Text.Toml.TomlInteger> | `long` |
| Float (incl. `inf` / `nan`) | <xref:Bodu.Text.Toml.TomlFloat> | `double` |
| Boolean | <xref:Bodu.Text.Toml.TomlBoolean> | `bool` |
| Offset date-time | <xref:Bodu.Text.Toml.TomlOffsetDateTime> | `DateTimeOffset` |
| Local date-time | <xref:Bodu.Text.Toml.TomlLocalDateTime> | `DateTime` (`Unspecified`) |
| Local date / local time | <xref:Bodu.Text.Toml.TomlLocalDate> / <xref:Bodu.Text.Toml.TomlLocalTime> | `DateOnly` / `TimeOnly` |
| Array | <xref:Bodu.Text.Toml.TomlArray> | ordered, mutable list (mixed-type allowed) |
| Table | <xref:Bodu.Text.Toml.TomlTable> | ordered, case-sensitive map (the document root) |

`Toml.Parse(source)` returns the root table; `Toml.Format(table)` re-emits it as canonical, block-style TOML in insertion order.

## Framing and grammar

Each format announces structure inline. Line-oriented formats (DotEnv, INI) split on line breaks and section headers. Delimited splits on a single-character delimiter with RFC 4180 quoting. TOML carries an explicit grammar of `key = value` pairs, `[table]` and `[[array-of-tables]]` headers, inline `{ }` tables, and `[ ]` arrays.

## Round-trip and canonicality

- **INI / DotEnv** preserve comments, whitespace, and ordering on round-trip, so a `Parse` → `Format` cycle reproduces the source's layout.
- **Delimited** is canonical per quoting policy — the writer emits a deterministic quoting of each field under the configured <xref:Bodu.Text.Delimited.DelimitedParseOptions>.
- **TOML** records *meaning*, not syntax. A standard `[table]`, an inline `{ }` table, dotted keys, and an array of tables all materialize as <xref:Bodu.Text.Toml.TomlTable> / <xref:Bodu.Text.Toml.TomlArray>. A `Parse` → `Format` round trip yields an equal model and canonical text, but does not reproduce the original layout or comments. When comment-preserving round-trips matter, prefer <xref:Bodu.Text.Ini.Ini>.

## Spec-version selection (TOML)

The TOML reader is strict and defaults to **TOML v1.0.0**. Setting <xref:Bodu.Text.Toml.TomlReaderOptions.SpecVersion> to `V1_1` accepts the v1.1.0 additions: `\e` and `\xHH` escapes, seconds-less time values, and multi-line / trailing-comma inline tables. The version affects parsing only — <xref:Bodu.Text.Toml.TomlWriter> always emits output valid under both versions. See [parser policies](parser-policies.md) for the full strictness matrix.

## Format exception

Every format raises a typed `*FormatException` that derives from <xref:Bodu.Text.TextFormatException> (itself a <xref:System.FormatException>), so a single `catch (TextFormatException)` handles parse failures across all four formats. The exception carries the location it can identify — a 1-based `LineNumber` and `ColumnNumber` and a 0-based `Offset` (each `0` / `null` when not tracked for that format). TOML reports all three; the line-oriented formats report a line and, where tracked, a column.

| Exception | Format |
|---|---|
| <xref:Bodu.Text.Delimited.DelimitedFormatException> | Delimited |
| <xref:Bodu.Text.DotEnv.DotEnvFormatException> | DotEnv |
| <xref:Bodu.Text.Ini.IniFormatException> | INI |
| <xref:Bodu.Text.Toml.TomlFormatException> | TOML |

## Ordering and equality (TOML tables)

<xref:Bodu.Text.Toml.TomlTable> compares keys with ordinal semantics, matching the TOML specification, and preserves insertion order so enumeration and writer output are stable. The scalar subtypes implement `IEquatable<T>` over their projected CLR value.

## Where to go next

- **[Getting started](getting-started.md)** — install + runnable minimal samples.
- **[Bodu.Text.Formats guides](../../guides/formats/index.md)** — deep-dive walk-throughs for every concept above.
- **[Parser policies](parser-policies.md)** — the strictness matrix and the diagnostics each parser carries.
- **API reference** — per-namespace pages: [Delimited](xref:Bodu.Text.Delimited), [DotEnv](xref:Bodu.Text.DotEnv), [Ini](xref:Bodu.Text.Ini), [Toml](xref:Bodu.Text.Toml).
- **[Introduction](index.md)** — the high-level shape of the library.
