---
title: Bodu.Text.Formats guides
---

# Bodu.Text.Formats guides

Recipe-style walk-throughs for **Bodu.Text.Formats**, organized by namespace and concern.

If you are new to the library, start with the [introduction](../../docs/formats/index.md), the [Core concepts](../../docs/formats/concepts.md) glossary, and the [getting-started page](../../docs/formats/getting-started.md). The guides below assume you know the vocabulary (self-framing format, value model, codec, round-trip rules).

## How the library works

Each format pairs a typed value model with a codec: parse turns a span of text into the model, format writes the model back out. Every format — Delimited, DotEnv, and INI — also exposes forward-only readers and writers for processing one logical unit at a time.

> **TOML** and **Bencode** object-mapping serializers (POCO ↔ format) are documented in the standalone <xref:Bodu.Text.Toml> and <xref:Bodu.Text.Bencode> packages — see the [Bodu serializer guides](../serialization/index.md).

## Namespace map

| Namespace | What lives here | Guides |
|---|---|---|
| <xref:Bodu.Text.Delimited> | The `Delimited` codec, `DelimitedDocument` / `DelimitedRow`, the streaming reader / writer, and `DelimitedParseOptions`. | [Using delimited](delimited.md) |
| <xref:Bodu.Text.DotEnv> | The `DotEnv` codec, `DotEnvDocument` / `DotEnvEntry`, and `DotEnvParseOptions`. | [Using DotEnv](dotenv.md) |
| <xref:Bodu.Text.Ini> | The `Ini` codec, `IniDocument` / `IniSection` / `IniEntry`, and `IniParseOptions`. | [Using INI](ini.md) |

## Guides

### `Bodu.Text.Formats` — Codec

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="delimited.md">Using delimited (CSV / TSV)</a></h3>
  <p>The <code>Delimited</code> codec — RFC 4180 quoting, delimiter selection, header handling, the streaming <code>DelimitedReader</code> / <code>DelimitedWriter</code>, and the strictness policies on <code>DelimitedParseOptions</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="dotenv.md">Using DotEnv</a></h3>
  <p>The <code>DotEnv</code> codec — <code>KEY=VALUE</code> parsing, quoting and escape rules, comment preservation, and duplicate-key policies on <code>DotEnvParseOptions</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="ini.md">Using INI</a></h3>
  <p>The <code>Ini</code> codec — section / entry model, comment trivia, duplicate-section and duplicate-key policies, and programmatic mutation of the round-trippable <code>IniDocument</code>.</p>
</div>

</div>

### `Bodu.Text.Formats` — I/O and policies

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="streaming.md">Streams and async I/O</a></h3>
  <p>The forward-only <code>CreateReader</code> / <code>CreateWriter</code> streaming surface — sync and async reads and writes, cancellation, lifetime contracts, mid-stream errors, and input-size limits.</p>
</div>

<div class="bodu-card">
  <h3><a href="../../docs/formats/parser-policies.md">Parser policies</a></h3>
  <p>The strictness and conflict-resolution options across all three formats — duplicate headers and keys, malformed records, lenient opt-ins, and the shared <code>TextFormatException</code> diagnostic surface.</p>
</div>

</div>

## Suggested reading path

1. **[Introduction](../../docs/formats/index.md)** and **[core concepts](../../docs/formats/concepts.md)** — the codec mental model and vocabulary.
2. The walk-through for your format — **[Delimited](delimited.md)**, **[DotEnv](dotenv.md)**, or **[INI](ini.md)**.
3. **[Parser policies](../../docs/formats/parser-policies.md)** — when the defaults are too strict or too lenient for your input.
4. **[Streams and async I/O](streaming.md)** — when the document no longer fits in memory.

## Where to go next

- [Bodu.Text.Formats introduction](../../docs/formats/index.md) — mental model, headline types, scenarios.
- [Core concepts](../../docs/formats/concepts.md) — vocabulary used throughout these guides.
- [Bodu.Text.Formats getting started](../../docs/formats/getting-started.md) — install and minimal samples.
- [Text & Serialization guides](../topics/text-and-serialization.md) — how the formats sit alongside the encodings and the object serializers.
