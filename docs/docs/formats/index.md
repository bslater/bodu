---
title: Bodu.Text.Formats — Introduction
---

# Bodu.Text.Formats

**Bodu.Text.Formats** decodes and encodes self-framing document formats — formats whose structure is described inline by the bytes themselves, rather than by an external schema. The library ships three format families, each with a strongly-typed value model and a span- and stream-friendly codec:

| Format | Namespace | Source | Use when |
|---|---|---|---|
| **Delimited** | <xref:Bodu.Text.Delimited> | [RFC 4180](https://www.rfc-editor.org/rfc/rfc4180) CSV and TSV variants | Row-oriented data interchange, spreadsheet import/export, log lines. |
| **DotEnv** | <xref:Bodu.Text.DotEnv> | `.env` key/value convention | Process environment configuration, deployment-time secrets, twelve-factor app config. |
| **INI** | <xref:Bodu.Text.Ini> | Classic INI / EditorConfig | Section/comment-preserving round-trippable configuration documents. Underpins [`Bodu.Text.Configuration`](../text-configuration/index.md). |

Each format exposes a modern, span-friendly shape: a strongly-typed value model, a `Parse` / `Format` entry point, `Try*` variants that swap exceptions for `bool` results, and forward-only streaming readers and writers with synchronous and asynchronous APIs. No reflection, no `dynamic`, and minimal allocation beyond the result model.

> **TOML, Bencode, and POCO serialization.** TOML and Bencode (BEP 3) live in the dedicated standalone packages **Bodu.Text.Toml** (<xref:Bodu.Text.Toml>) and **Bodu.Text.Bencode** (<xref:Bodu.Text.Bencode>) — object-mapping serializers (POCO ↔ format) — and are documented there, not in this package. See the [Bodu serializers introduction](../serialization/index.md).

## Key concepts

| Concept | Plain-language meaning |
|---|---|
| **Self-framing format** | The bytes describe their own structure inline. Delimited / DotEnv / INI use line breaks, separators, and section headers. |
| **Value model** | The decoded document is a typed model — an `IReadOnlyList<DelimitedRow>`, an ordered `DotEnvDocument`, or an `IniDocument`. |
| **Codec** | The static façade per format (`Delimited`, `DotEnv`, `Ini`) exposing `Parse` / `Format`, `Try…`, and the `CreateReader` / `CreateWriter` streaming factories. |
| **Canonical / round-trip rules** | INI and DotEnv preserve comments and ordering on round-trip; Delimited is canonical per quoting policy. |
| **Format exception** | Each format raises a typed `*FormatException` (deriving from <xref:Bodu.Text.TextFormatException>, a `FormatException`) with a precise message keyed to the failure mode. |

For the full glossary, see [Core concepts](concepts.md).

### Self-framing vs. binary text encoding

These formats are *not* binary-to-text encodings like Base64. They are **serialization grammars**: structure and framing live in the bytes themselves. Use [`Bodu.Text.Encoding`](../text-encoding/index.md) when you need to convert raw bytes into a printable alphabet; use **Bodu.Text.Formats** when you need to (de)serialise a structured document.

### INI primitives vs. configuration layering

The INI namespace ships <xref:Bodu.Text.Ini.IniDocument>, <xref:Bodu.Text.Ini.IniSection>, <xref:Bodu.Text.Ini.IniEntry>, and the static <xref:Bodu.Text.Ini.Ini> codec. [`Bodu.Text.Configuration`](../text-configuration/index.md) layers EditorConfig-style profile presets, glob-anchored sections, and a flat colon-delimited resolved view on top of this same model. When you need INI parsing without configuration layering, work with <xref:Bodu.Text.Ini.Ini> directly; when you need target-path resolution, reach for the Configuration package.

## Worked example — a small INI document

A single document traces the typed-model pipeline end-to-end:

1. Parse text into a model: `IniDocument config = Ini.Parse(source)`.
2. Walk the model: each <xref:Bodu.Text.Ini.IniSection> exposes its <xref:Bodu.Text.Ini.IniEntry> items in source order, with comments and whitespace retained as trivia.
3. Read or mutate an entry: `config.GetOrAddSection("database").SetEntry("port", "5433")`.
4. Round-trip back to text: `Ini.Format(config)` re-emits the model with comments, ordering, and whitespace preserved.
5. Malformed input — a broken section header, an invalid escape sequence — surfaces as <xref:Bodu.Text.Ini.IniFormatException> with the line of the failure.

## Common scenarios

| Scenario | Reach for |
|---|---|
| Read a CSV file row by row | `Delimited.CreateReader(...)` |
| Read a `.env` file into an ordered model | `DotEnv.Parse(source)` |
| Round-trip an INI file preserving comments | `Ini.Parse(source)` → mutate → `Ini.Format(doc)` |
| Parse without throwing on malformed input | the `Try…` overload for the format |
| Stream a large delimited file | `DelimitedReader` / `DelimitedWriter` |
| Catch any format's parse failure uniformly | `catch (TextFormatException)` |

## Main types per format

Every format follows the same shape — a static codec, a typed value model, a format-specific exception type. The table below is the at-a-glance index; deeper coverage lives in the per-format guides.

### Delimited (CSV / TSV) — <xref:Bodu.Text.Delimited>

| Type | Purpose |
|---|---|
| <xref:Bodu.Text.Delimited.Delimited> | Static codec — read and write delimited records over span / byte / `Stream` / `TextReader` / `TextWriter`. |
| <xref:Bodu.Text.Delimited.DelimitedDocument>, <xref:Bodu.Text.Delimited.DelimitedRow> | Immutable document model. Each row exposes its `Fields` as `IReadOnlyList<string>`. |
| <xref:Bodu.Text.Delimited.DelimitedParseOptions> | Quoting policy, delimiter selection (comma / tab / custom), header handling. Shared between read and write paths. |
| <xref:Bodu.Text.Delimited.DelimitedFormatException> | Thrown for unterminated quotes, ragged rows under strict mode, and other structural violations. |

### DotEnv — <xref:Bodu.Text.DotEnv>

| Type | Purpose |
|---|---|
| <xref:Bodu.Text.DotEnv.DotEnv> | Static codec — read and write `.env` documents. |
| <xref:Bodu.Text.DotEnv.DotEnvDocument>, <xref:Bodu.Text.DotEnv.DotEnvEntry> | Ordered key/value model that preserves entry order. |
| <xref:Bodu.Text.DotEnv.DotEnvParseOptions> | Quote handling, comment preservation, escape policy. Shared between read and write paths. |
| <xref:Bodu.Text.DotEnv.DotEnvFormatException> | Thrown for malformed key/value lines. |

### INI — <xref:Bodu.Text.Ini>

| Type | Purpose |
|---|---|
| <xref:Bodu.Text.Ini.Ini> | Static codec — read and write INI documents. |
| <xref:Bodu.Text.Ini.IniDocument>, <xref:Bodu.Text.Ini.IniSection>, <xref:Bodu.Text.Ini.IniEntry> | Section/entry model that preserves comments, whitespace, and order. |
| <xref:Bodu.Text.Ini.IniParseOptions> | Profile presets, escape rules, duplicate-section policy. Shared between read and write paths. |
| <xref:Bodu.Text.Ini.IniFormatException> | Thrown for malformed headers, entries, or escape sequences. |

## Where to go next

- **[Core concepts](concepts.md)** — full vocabulary: value vs. document, round-trip rules, framing, format exception.
- **[Getting started](getting-started.md)** — install + minimal samples for `Parse`, `Format`, `Try*`, and the stream overloads.
- **[Bodu.Text.Formats guides](../../guides/formats/index.md)** — using each codec, the value models, and stream support.
- **Per-format guides** — [Delimited](../../guides/formats/delimited.md), [DotEnv](../../guides/formats/dotenv.md), [INI](../../guides/formats/ini.md), [Streaming](../../guides/formats/streaming.md).
- **API reference** — per-namespace pages: [Delimited](xref:Bodu.Text.Delimited), [DotEnv](xref:Bodu.Text.DotEnv), [Ini](xref:Bodu.Text.Ini).
- **For EditorConfig-style configuration layering on `IniDocument`**, see [Bodu.Text.Configuration](../text-configuration/index.md).
