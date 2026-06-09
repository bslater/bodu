---
title: Bodu.Text.Formats guides
---

# Bodu.Text.Formats guides

Recipe-style walk-throughs for **Bodu.Text.Formats**, organized by namespace and concern.

If you are new to the library, start with the [introduction](../../docs/formats/index.md), the [Core concepts](../../docs/formats/concepts.md) glossary, and the [getting-started page](../../docs/formats/getting-started.md). The guides below assume you know the vocabulary (framed format, value tree, byte string, canonical encoding, framing token).

## How the library works

![Bencode encode/decode pipeline — object tree to canonical bytes and back](../../images/diagrams/formats-bencode-pipeline.svg)

A bencoded document is a single tree of typed values. **`Bencode`** is the static codec — encode walks the tree with a recursive writer, decode runs a forward-only parser that dispatches on the leading framing token. The library enforces BEP 3 canonicality on both sides: encoders always emit the canonical form, parsers reject every non-canonical input.

## Namespace map

| Namespace | What lives here | Guides |
|---|---|---|
| <xref:Bodu.Text.Bencode> | The `Bencode` codec, the `BencodedValue` model and its four kinds, `BencodedStringComparer`, and `BencodeFormatException`. | [Using Bencode](bencode.md) · [The BencodedValue model](value-model.md) |
| <xref:Bodu.Text.Delimited> | The `Delimited` codec, `DelimitedDocument` / `DelimitedRow`, the streaming reader / writer, and `DelimitedParseOptions`. | [Using delimited](delimited.md) |
| <xref:Bodu.Text.DotEnv> | The `DotEnv` codec, `DotEnvDocument` / `DotEnvEntry`, and `DotEnvParseOptions`. | [Using DotEnv](dotenv.md) |
| <xref:Bodu.Text.Ini> | The `Ini` codec, `IniDocument` / `IniSection` / `IniEntry`, and `IniParseOptions`. | [Using INI](ini.md) |
| <xref:Bodu.Text.Toml> | The `Toml` façade, the `TomlReader` / `TomlWriter` pair, the `TomlValue` model, `TomlReaderOptions`, and `TomlFormatException`. | [Using TOML](toml.md) |

## Guides

### `Bodu.Text.Formats` — Codec

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="bencode.md">Using Bencode</a></h3>
  <p>The static <code>Bencode</code> codec — <code>Encode</code>, <code>Decode</code>, <code>TryEncode</code>, <code>TryDecode</code>, <code>GetEncodedLength</code>, and the BEP 3 invariants enforced on both sides of the pipeline.</p>
</div>

<div class="bodu-card">
  <h3><a href="value-model.md">The BencodedValue model</a></h3>
  <p>Walk-through of <code>BencodedInteger</code>, <code>BencodedString</code>, <code>BencodedList</code>, and <code>BencodedDictionary</code> — their construction rules, dispatch via <code>BencodedValueKind</code>, and the ordinal <code>BencodedStringComparer</code> that drives dictionary ordering.</p>
</div>

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

<div class="bodu-card">
  <h3><a href="toml.md">Using TOML</a></h3>
  <p>The <code>TomlReader</code> / <code>TomlWriter</code> pair and the <code>Toml</code> façade — the typed <code>TomlValue</code> model, tables and arrays, first-class date-time kinds, TOML v1.0.0 / v1.1.0 selection, and stream support.</p>
</div>

</div>

### `Bodu.Text.Formats` — I/O

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="streaming.md">Streams and async I/O</a></h3>
  <p>Sync and async stream overloads — buffer staging via <code>ArrayPool&lt;byte&gt;</code>, cancellation, lifetime contracts, and when to prefer the span path over <code>Stream</code>.</p>
</div>

</div>

## Where to go next

- [Bodu.Text.Formats introduction](../../docs/formats/index.md) — mental model, headline types, scenarios.
- [Core concepts](../../docs/formats/concepts.md) — vocabulary used throughout these guides.
- [Bodu.Text.Formats getting started](../../docs/formats/getting-started.md) — install and minimal samples.
