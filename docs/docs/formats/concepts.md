---
title: Bodu.Text.Formats — Core concepts
---

# Bodu.Text.Formats — Core concepts

This page is the vocabulary the rest of the documentation assumes. Read it once before the [getting-started samples](getting-started.md) or the [guides](../../guides/formats/index.md), and refer back to it whenever a term feels imprecise.

`Bodu.Text.Formats` is part of the **[Text & Serialization](../topics/text-and-serialization.md)** topic — the document-codec tier between the binary-to-text codecs and the POCO serializers. For the high-level shape of the library, start with the [introduction](index.md).

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

## Typed value access

The decoded values are strings on the wire, but every model exposes the same **typed accessor** pair: `GetValue<T>` (throwing) and `TryGetValue<T>` (non-throwing) — on a <xref:Bodu.Text.Delimited.DelimitedRow> by ordinal or column name, on a <xref:Bodu.Text.DotEnv.DotEnvDocument> by key (and on a <xref:Bodu.Text.DotEnv.DotEnvEntry> directly), and on an <xref:Bodu.Text.Ini.IniSection> by key (and on an <xref:Bodu.Text.Ini.IniEntry> directly). The accessor is generically constrained to `where T : ISpanParsable<T>` and dispatches to `T.Parse` / `T.TryParse` under `CultureInfo.InvariantCulture`, so `int`, `decimal`, `TimeSpan`, `DateTime`, `Guid`, and any consumer-defined `ISpanParsable<T>` type work uniformly across all three formats.

> [!NOTE]
> The accessor is `ISpanParsable<T>`-only — there are no per-type convenience methods (`GetInt32`, `GetDouble`, …); call `GetValue<int>` / `GetValue<double>` instead. `GetValue<T>` raises the standard `T.Parse` `FormatException` on an unparseable value; `TryGetValue<T>` returns `false`. A missing key or column raises `KeyNotFoundException` (`GetValue`) or returns `false` (`TryGetValue`). The invariant culture is deliberate: a value authored as `3.14` parses identically regardless of the host machine's locale.

## Framing and grammar

Each format announces structure inline. Line-oriented formats (DotEnv, INI) split on line breaks and section headers; Delimited splits on a single-character delimiter with RFC 4180 quoting. All three accept any of the three common line terminators — `\n`, `\r`, or `\r\n` — interchangeably as a record/line break, with `\r\n` treated as a single break. The grammars are single-character throughout: the Delimited delimiter and quote, the INI section brackets and `=` / `:` separators, and the DotEnv `=` are all one character — multi-character delimiters and separators are not supported.

## Parse options and strictness policies

Every codec takes a per-format **options type** — <xref:Bodu.Text.Delimited.DelimitedParseOptions>, <xref:Bodu.Text.DotEnv.DotEnvParseOptions>, <xref:Bodu.Text.Ini.IniParseOptions> — shared between the read and write paths. Two kinds of knob live there:

- **Dialect options** describe the wire shape: the Delimited `Delimiter` / `Quote` / `HasHeader`, the DotEnv `AllowExportPrefix`, the INI section / key case sensitivity.
- **Policy options** decide what happens when the input is ambiguous or conflicting. The recurring policy axes are:
  - **Duplicate keys** — DotEnv and INI resolve duplicate keys through a `FirstWins` / `LastWins` / `Disallowed` policy (<xref:Bodu.Text.DuplicateKeyPolicy>); INI adds <xref:Bodu.Text.Ini.IniDuplicateSectionBehavior> for repeated `[section]` headers (merge, preserve, disallow). Delimited has the analogous `DuplicateHeaderBehavior` for repeated column names.
  - **Comments** — `PreserveComments` (DotEnv, INI) controls whether comment lines are retained as trivia or discarded; Delimited's `AllowComments` controls whether comment lines are recognized at all.
  - **Quoting and malformed records** — Delimited's `MalformedRecordBehavior` and `FieldCountBehavior` decide whether a structurally suspect row throws or is tolerated.

The parsers default to **strict**: anything the grammar cannot unambiguously interpret raises an exception rather than silently dropping data, and lenient behavior is always an explicit opt-in on the options. The full default-by-default matrix, with migration notes, is in [Parser policies](parser-policies.md).

## Streaming reader and writer lifecycle

Beyond the in-memory `Parse` / `Format` surface, each codec's `CreateReader` / `CreateWriter` factories return **forward-only** streaming types that process one logical unit at a time so a large source never materializes as a single document:

| Format | Reader (unit per `Read` / `ReadAsync`) | Writer |
|---|---|---|
| Delimited | `DelimitedReader` — one row | `DelimitedWriter.WriteHeader` / `WriteRow` (+ `…Async`) |
| DotEnv | `DotEnvReader` — one entry | `DotEnvWriter.WriteEntry` / `WriteComment` (+ `…Async`) |
| INI | `IniReader` — one entry, exposing `Section` / `Key` / `Value` | `IniWriter.WriteSection` / `WriteEntry` / `WriteComment` (+ `…Async`) |

The lifecycle rules:

- **Create, drain, dispose.** A reader or writer takes ownership of the supplied `TextReader` / `TextWriter` and disposes it; wrap the instance in a `using` block.
- **Forward-only.** Each successful `Read()` advances to the next unit; there is no seeking, buffering of prior units, or rewind.
- **Sync or async, not both interleaved.** Every member has a synchronous and an `…Async` form. Pick one mode per instance — in particular, do not interleave `Read` and `ReadAsync` on a `DelimitedReader`.
- **Document-level policies do not apply.** Streaming readers surface raw units in source order; policies that need the whole document — duplicate-key resolution, INI section merging — are applied only by the in-memory `Parse` entry points.

The full streaming surface is covered in the [streams and async I/O guide](../../guides/formats/streaming.md).

## Trivia and round-trip preservation

**Trivia** is the content that carries no data but carries meaning for humans: comments, blank lines, ordering, and incidental whitespace. The codecs preserve trivia where the format's consumers expect it:

- **INI** attaches full-line `#` / `;` comments as `LeadingComments` on the next section or entry, preserving the original prefix character. Sections and entries round-trip in source order.
- **DotEnv** attaches full-line comments as `LeadingComments` on the next entry and preserves entry order.
- **Delimited** retains field values, field order, and header order, but comment lines and blank lines are not part of the document model.

> [!IMPORTANT]
> INI's `InlineComment` property on <xref:Bodu.Text.Ini.IniEntry> is a **write-side** feature only. The parser does not split a same-line trailing `;` / `#` out of a value — everything after the first `=` / `:` separator (trimmed) becomes the entry's `Value`, so `host = localhost ; primary` parses to the value `localhost ; primary`. `InlineComment` is settable so you can attach a trailing comment programmatically; <xref:Bodu.Text.Ini.Ini>'s `Format` then emits it on the entry's line. DotEnv is different — there, an inline `#` (preceded by whitespace, in an *unquoted* value) genuinely truncates the value on parse when `AllowInlineComments` is on.

Preservation is structural, not byte-for-byte: INI trims whitespace around keys and values and drops blank lines, DotEnv does not retain bare blank lines or single-quoted form on output, and Delimited discards blank lines and unquoted padding. A `Parse` → `Format` cycle reproduces the *authored structure* — keys, values, comments, ordering — not the exact source bytes. When byte-stable round-tripping matters, keep the original bytes.

## Canonicality

Writing a document back always produces **canonical** output for its format:

- **Delimited** emits deterministic RFC 4180 quoting under the configured <xref:Bodu.Text.Delimited.DelimitedParseOptions> — a field is quoted when it is empty or contains the delimiter, the quote character, `\n`, or `\r`; embedded quote characters are doubled. (Empty fields are quoted as `""` so that a leading/trailing empty field survives a round trip through the parser.)
- **DotEnv** applies a conservative quoting rule — values matching `[A-Za-z0-9_.,:/@+-]` render unquoted, everything else is double-quoted with `"`, `\`, `$`, `\n`, `\t`, and `\r` escaped; single-quoted form is never emitted.
- **INI** writes global entries first (when present), then each named section separated by a blank line, with leading comments ahead of their owner and any programmatically set inline comment appended on the entry's line. Entries are written as `key = value` (a single space either side of `=`).

Canonical output means two semantically equal documents format to the same text, which makes diffs and snapshot tests stable.

> [!NOTE]
> Line terminators differ between the in-memory `Format` and the streaming writers. `Delimited.Format` and **all three streaming writers** emit a bare line feed (`\n`). `Ini.Format` and `DotEnv.Format` use the platform `Environment.NewLine` instead. If a downstream tool requires a specific terminator, normalise the formatted string yourself.

## Defensive parsing and input size

Strict-by-default policies are the first line of defense; input size is the second. The span `Parse` entry points operate on an in-memory source, so the parser cannot bound how much you hand it — **cap the size of an untrusted stream before reading it into memory** (and for non-seekable streams, copy at most your limit into a buffer first). For sources that are legitimately large, prefer the streaming readers, which hold only the current unit in memory. The size-limiting pattern is shown in the [streaming guide](../../guides/formats/streaming.md).

## Format exception

Every format raises a typed `*FormatException` that derives from <xref:Bodu.Text.TextFormatException> (itself a `System.FormatException`), so a single `catch (TextFormatException)` handles parse failures across all three formats. The base carries the location it can identify: a 1-based `LineNumber`, a 1-based `ColumnNumber`, and a 0-based `Offset` — each `0` / `null` when not tracked for that failure. In practice these parsers track the line; the column and offset are reserved and commonly `0` / `null`, so treat the location as advisory and key user-facing diagnostics on the line number plus the message.

| Exception | Format |
|---|---|
| <xref:Bodu.Text.Delimited.DelimitedFormatException> | Delimited |
| <xref:Bodu.Text.DotEnv.DotEnvFormatException> | DotEnv |
| <xref:Bodu.Text.Ini.IniFormatException> | INI |

Because `*FormatException` is a `FormatException`, code that already filters on the BCL base type keeps working; the Bodu hierarchy only narrows it. The `Try*` overloads (`Delimited.TryParse`, `DotEnv.TryParse`, `Ini.TryParse`) catch the format-specific exception internally and return `false` instead — they do **not** swallow unrelated exceptions such as `ArgumentNullException` from a `null` source.

## Where to go next

- **[Getting started](getting-started.md)** — install + runnable minimal samples.
- **[Bodu.Text.Formats guides](../../guides/formats/index.md)** — deep-dive walk-throughs for every concept above.
- **[Parser policies](parser-policies.md)** — the strictness matrix and the diagnostics each parser carries.
- **[Text & Serialization concepts](../topics/text-and-serialization-concepts.md)** — the topic-level vocabulary these terms specialize: codec vs. serializer, the tier model, canonical output, round-trip fidelity.
- **API reference** — per-namespace pages: [Delimited](xref:Bodu.Text.Delimited), [DotEnv](xref:Bodu.Text.DotEnv), [Ini](xref:Bodu.Text.Ini).
- **[Introduction](index.md)** — the high-level shape of the library.
