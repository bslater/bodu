---
title: Bodu.Text.Formats — Introduction
---

# Bodu.Text.Formats

**Bodu.Text.Formats** decodes and encodes self-framing document formats — formats whose structure is described inline by the bytes themselves, rather than by an external schema. The library ships four format families, each with a strongly-typed value model and a span- and stream-friendly codec:

| Format | Namespace | Source | Use when |
|---|---|---|---|
| **Delimited** | <xref:Bodu.Text.Delimited> | [RFC 4180](https://www.rfc-editor.org/rfc/rfc4180) CSV and TSV variants | Row-oriented data interchange, spreadsheet import/export, log lines. |
| **DotEnv** | <xref:Bodu.Text.DotEnv> | `.env` key/value convention | Process environment configuration, deployment-time secrets, twelve-factor app config. |
| **INI** | <xref:Bodu.Text.Ini> | Classic INI / EditorConfig | Section/comment-preserving round-trippable configuration documents. Underpins [`Bodu.Text.Configuration`](../text-configuration/index.md). |
| **TOML** | <xref:Bodu.Text.Toml> | [TOML](https://toml.io/) v1.0.0 / v1.1.0 | Strongly-typed application configuration with tables, arrays, and first-class date-time values. |

Each format exposes a modern, span- and stream-friendly shape: a strongly-typed value model, a `Parse` / `Format` (or `Decode` / `Encode`) entry point, `Try*` variants that swap exceptions for `bool` results, and synchronous + asynchronous `Stream` overloads. No reflection, no `dynamic`, and minimal allocation beyond the result model.

> **Bencode and POCO serialization.** Object-mapping serializers — POCO ↔ format, shaped after `System.Text.Json` — live in the dedicated standalone packages **Bodu.Text.Bencode** (<xref:Bodu.Text.Bencode>) and **Bodu.Text.Toml** (<xref:Bodu.Text.Toml>). Bencode (BEP 3) is documented there, not in this package. See the [Bodu serializers introduction](../serialization/index.md).

## Key concepts

| Concept | Plain-language meaning |
|---|---|
| **Self-framing format** | The bytes describe their own structure inline. Delimited / DotEnv / INI / TOML use line breaks, separators, section headers, and table syntax. |
| **Value model** | The decoded document is a typed model — an `IReadOnlyList<DelimitedRow>`, an ordered `DotEnvDocument`, an `IniDocument`, or a `TomlTable` tree. |
| **Codec** | The static façade per format (`Delimited`, `DotEnv`, `Ini`, `Toml`) exposing `Parse` / `Format` (or `Decode` / `Encode`), `Try…`, and stream overloads. |
| **Canonical / round-trip rules** | INI and DotEnv preserve comments and ordering on round-trip; Delimited is canonical per quoting policy; TOML round-trips to an equal model and canonical text. |
| **Format exception** | Each format raises a typed `*FormatException` (deriving from <xref:Bodu.Text.TextFormatException>, a `FormatException`) with a precise message keyed to the failure mode. |

For the full glossary, see [Core concepts](concepts.md).

### Self-framing vs. binary text encoding

These formats are *not* binary-to-text encodings like Base64. They are **serialization grammars**: structure and framing live in the bytes themselves. Use [`Bodu.Text.Encoding`](../text-encoding/index.md) when you need to convert raw bytes into a printable alphabet; use **Bodu.Text.Formats** when you need to (de)serialise a structured document.

### INI primitives vs. configuration layering

The INI namespace ships <xref:Bodu.Text.Ini.IniDocument>, <xref:Bodu.Text.Ini.IniSection>, <xref:Bodu.Text.Ini.IniEntry>, and the static <xref:Bodu.Text.Ini.Ini> codec. [`Bodu.Text.Configuration`](../text-configuration/index.md) layers EditorConfig-style profile presets, glob-anchored sections, and a flat colon-delimited resolved view on top of this same model. When you need INI parsing without configuration layering, work with <xref:Bodu.Text.Ini.Ini> directly; when you need target-path resolution, reach for the Configuration package.

## Worked example — a small TOML document

A single document traces the typed-tree pipeline end-to-end:

1. Parse text into a tree: `TomlTable config = Toml.Parse(source)`.
2. Walk the tree by kind: each value derives from <xref:Bodu.Text.Toml.TomlValue> and exposes a `Kind` for switch-style dispatch.
3. Project a leaf to its expected type: `((TomlString)config["title"]).Value`.
4. Round-trip back to canonical text: `Toml.Format(config)` re-emits the model in document order.
5. Malformed input — an unterminated string, a duplicate key — surfaces as <xref:Bodu.Text.Toml.TomlFormatException> with the line, column, and offset of the failure.

## Common scenarios

| Scenario | Reach for |
|---|---|
| Parse a TOML config from disk | `Toml.Parse(File.ReadAllText(path))` |
| Read a CSV file row by row | `Delimited.CreateReader(...)` |
| Read a `.env` file into an ordered model | `DotEnv.Parse(source)` |
| Round-trip an INI file preserving comments | `Ini.Parse(source)` → mutate → `Ini.Format(doc)` |
| Parse without throwing on malformed input | the `Try…` overload for the format |
| Stream a large delimited file | `DelimitedReader` / `DelimitedWriter` |
| Catch any format's parse failure uniformly | `catch (TextFormatException)` |

## Main types per format

Every format follows the same shape — a static codec (or reader/writer pair), a typed value model, a format-specific exception type. The table below is the at-a-glance index; deeper coverage lives in the per-format guides.

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

### TOML — <xref:Bodu.Text.Toml>

TOML follows a reader/writer shape rather than a single static codec: <xref:Bodu.Text.Toml.TomlReader> and <xref:Bodu.Text.Toml.TomlWriter> own deserialization and serialization, with the static <xref:Bodu.Text.Toml.Toml> class as a convenience façade.

| Type | Purpose |
|---|---|
| <xref:Bodu.Text.Toml.Toml> | Static façade — `Parse` / `TryParse` / `Format` over spans, streams, and async, each with a `TomlReaderOptions` overload. |
| <xref:Bodu.Text.Toml.TomlReader>, <xref:Bodu.Text.Toml.TomlWriter> | The deserialize / serialize pair; the primary surface for configurable or reusable reads and writes. |
| <xref:Bodu.Text.Toml.TomlTable>, <xref:Bodu.Text.Toml.TomlArray>, <xref:Bodu.Text.Toml.TomlValue> | Ordered, mutable value model. `TomlTable` is the document root; the scalar and date-time subtypes derive from `TomlValue`. |
| <xref:Bodu.Text.Toml.TomlReaderOptions>, <xref:Bodu.Text.Toml.TomlSpecVersion> | Selects strict TOML v1.0.0 (default) or v1.1.0 grammar. |
| <xref:Bodu.Text.Toml.TomlFormatException> | Thrown for malformed TOML; carries line, column, and offset. |

## Where to go next

- **[Core concepts](concepts.md)** — full vocabulary: value vs. document, round-trip rules, framing, format exception.
- **[Getting started](getting-started.md)** — install + minimal samples for `Parse`, `Format`, `Try*`, and the stream overloads.
- **[Bodu.Text.Formats guides](../../guides/formats/index.md)** — using each codec, the value models, and stream support.
- **Per-format guides** — [Delimited](../../guides/formats/delimited.md), [DotEnv](../../guides/formats/dotenv.md), [INI](../../guides/formats/ini.md), [TOML](../../guides/formats/toml.md), [Streaming](../../guides/formats/streaming.md).
- **API reference** — per-namespace pages: [Delimited](xref:Bodu.Text.Delimited), [DotEnv](xref:Bodu.Text.DotEnv), [Ini](xref:Bodu.Text.Ini), [Toml](xref:Bodu.Text.Toml).
- **For EditorConfig-style configuration layering on `IniDocument`**, see [Bodu.Text.Configuration](../text-configuration/index.md).
