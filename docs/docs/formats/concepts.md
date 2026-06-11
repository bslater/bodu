---
title: Bodu.Text.Formats — Core concepts
---

# Bodu.Text.Formats — Core concepts

This page is the vocabulary the rest of the documentation assumes. Read it once before the [getting-started samples](getting-started.md) or the [guides](../../guides/formats/index.md), and refer back to it whenever a term feels imprecise.

For the high-level shape of the library, start with the [introduction](index.md).

> **TOML** and **Bencode** live in the standalone <xref:Bodu.Text.Toml> and <xref:Bodu.Text.Bencode> packages as object-mapping serializers (POCO ↔ format) that follow a different model. This page is about the **document codecs** in `Bodu.Text.Formats`.

## Format and codec

A **format** is the wire grammar. The package ships three — Delimited, DotEnv, and INI. Each defines the kinds of value it carries, the syntax that delimits them, and the invariants every parser enforces.

A **codec** is the static façade that exposes parse and format operations for a format — <xref:Bodu.Text.Delimited.Delimited>, <xref:Bodu.Text.DotEnv.DotEnv>, and <xref:Bodu.Text.Ini.Ini>. Each exposes span overloads, `Try…` variants, and the streaming `CreateReader` / `CreateWriter` factories around a single parser and writer.

## Value and document

A **value** is one node in a decoded model. The shape varies by format:

- **Delimited** — a <xref:Bodu.Text.Delimited.DelimitedRow> whose `Fields` are `IReadOnlyList<string>`.
- **DotEnv** — a <xref:Bodu.Text.DotEnv.DotEnvEntry> key/value pair.
- **INI** — an <xref:Bodu.Text.Ini.IniEntry> within an <xref:Bodu.Text.Ini.IniSection>.

A **document** is the whole parsed model — a <xref:Bodu.Text.Delimited.DelimitedDocument>, a <xref:Bodu.Text.DotEnv.DotEnvDocument>, or an <xref:Bodu.Text.Ini.IniDocument>.

## Framing and grammar

Each format announces structure inline. Line-oriented formats (DotEnv, INI) split on line breaks and section headers. Delimited splits on a single-character delimiter with RFC 4180 quoting.

## Round-trip and canonicality

- **INI / DotEnv** preserve comments, whitespace, and ordering on round-trip, so a `Parse` → `Format` cycle reproduces the source's layout.
- **Delimited** is canonical per quoting policy — the writer emits a deterministic quoting of each field under the configured <xref:Bodu.Text.Delimited.DelimitedParseOptions>.

## Format exception

Every format raises a typed `*FormatException` that derives from <xref:Bodu.Text.TextFormatException> (itself a <xref:System.FormatException>), so a single `catch (TextFormatException)` handles parse failures across all three formats. The exception carries the location it can identify — a 1-based `LineNumber` and `ColumnNumber` and a 0-based `Offset` (each `0` / `null` when not tracked for that format). The line-oriented formats report a line and, where tracked, a column.

| Exception | Format |
|---|---|
| <xref:Bodu.Text.Delimited.DelimitedFormatException> | Delimited |
| <xref:Bodu.Text.DotEnv.DotEnvFormatException> | DotEnv |
| <xref:Bodu.Text.Ini.IniFormatException> | INI |

## Where to go next

- **[Getting started](getting-started.md)** — install + runnable minimal samples.
- **[Bodu.Text.Formats guides](../../guides/formats/index.md)** — deep-dive walk-throughs for every concept above.
- **[Parser policies](parser-policies.md)** — the strictness matrix and the diagnostics each parser carries.
- **API reference** — per-namespace pages: [Delimited](xref:Bodu.Text.Delimited), [DotEnv](xref:Bodu.Text.DotEnv), [Ini](xref:Bodu.Text.Ini).
- **[Introduction](index.md)** — the high-level shape of the library.
