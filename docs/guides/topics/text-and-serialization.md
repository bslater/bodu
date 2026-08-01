---
title: Text & Serialization — Guides
---

# Text & Serialization — Guides

Recipe-style walk-throughs for the text family — the `Bodu.Text.Encoding` binary-to-text codecs, the `Bodu.Text.Filtering` include/exclude filtering engine, the `Bodu.Text.Formats` document formats, and the `Bodu.Text.Bencode` / `Bodu.Text.Toml` object serializers. Four different jobs, four guide sections; this page is the topic-level map.

If you are unsure which package does the job you have, start with the [topic overview](../../docs/topics/text-and-serialization.md) — it leads with the codec / format / serializer disambiguation — and the [topic concepts](../../docs/topics/text-and-serialization-concepts.md) for the shared vocabulary.

## Bodu.Text.Encoding — binary-to-text codecs

Bytes ⇄ printable text. Base16 through Base85 with every common variant, plus Base45, Base62, and Bech32.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="../text-encoding/index.md">Overview</a></h3>
  <p>The encoding family at a glance — payload expansion, variants, the choose-an-encoding table, and the shared API shape.</p>
</div>

<div class="bodu-card">
  <h3><a href="../text-encoding/base64.md">Using Base64</a></h3>
  <p>Standard / URL-safe / MIME variants, JWT decoding, and 76-character line wrapping.</p>
</div>

<div class="bodu-card">
  <h3><a href="../text-encoding/base32.md">Using Base32</a></h3>
  <p>Standard / HexExtended / Crockford / Z-Base-32 variants, TOTP secrets, and padding control.</p>
</div>

<div class="bodu-card">
  <h3><a href="../text-encoding/bech32.md">Using Bech32</a></h3>
  <p>Bech32 / Bech32m (BIP 173 / 350) — human-readable part, separator, 5-bit data, and the six-symbol checksum.</p>
</div>

<div class="bodu-card">
  <h3><a href="../text-encoding/binary-encodings-interface.md">The IBinaryEncoding interface</a></h3>
  <p>Runtime-selected encoding choice via <code>BinaryEncodings.Get(name)</code> and the unified <code>IBinaryEncoding</code> contract.</p>
</div>

</div>

## Bodu.Text.Filtering — include/exclude filtering

Selecting values by pattern. Glob and regex patterns compiled into one cost-tiered matcher, with set or ordered-rule semantics and built-in telemetry.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="../text-filtering/index.md">Overview</a></h3>
  <p>How the engine works — compile-once filters, cost-tier classification, and the guide map.</p>
</div>

<div class="bodu-card">
  <h3><a href="../text-filtering/patterns-and-globs.md">Patterns and globs</a></h3>
  <p>The full glob grammar — classes, <code>{a,b}</code> alternation, escapes — and when to reach for regex.</p>
</div>

<div class="bodu-card">
  <h3><a href="../text-filtering/evaluation-modes.md">Evaluation modes</a></h3>
  <p><code>AnyMatch</code> sets vs <code>LastMatchWins</code> ordered rules, allowlists, and gitignore-convention parsing.</p>
</div>

<div class="bodu-card">
  <h3><a href="../text-filtering/telemetry-and-tuning.md">Telemetry and tuning</a></h3>
  <p>Statistics, per-pattern hit counts, the observer hook, cost tiers, and fail-safe regex timeouts.</p>
</div>

</div>

## Bodu.Text.Formats — document formats

Parse, edit, and write structured documents — typed value models with streaming readers and writers.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="../formats/index.md">Overview</a></h3>
  <p>The three format families, the namespace map, and where each guide fits.</p>
</div>

<div class="bodu-card">
  <h3><a href="../formats/delimited.md">Using delimited (CSV / TSV)</a></h3>
  <p>RFC 4180 quoting, delimiter selection, header handling, and the streaming <code>DelimitedReader</code> / <code>DelimitedWriter</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="../formats/dotenv.md">Using DotEnv</a></h3>
  <p><code>KEY=VALUE</code> parsing, quoting and escape rules, comment preservation, and duplicate-key policies.</p>
</div>

<div class="bodu-card">
  <h3><a href="../formats/ini.md">Using INI</a></h3>
  <p>Section / entry model, comment trivia, duplicate policies, and mutating the round-trippable <code>IniDocument</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="../formats/streaming.md">Streams and async I/O</a></h3>
  <p>The forward-only <code>Utf8*Reader</code> / <code>Utf8*Writer</code> token surfaces and the typed record-streaming serializer overloads.</p>
</div>

<div class="bodu-card">
  <h3><a href="../formats/choosing-a-format.md">Choosing a text format</a></h3>
  <p>Delimited vs. DotEnv vs. INI — the shape of data each format suits and how to pick between them.</p>
</div>

</div>

## Bodu.Text.Bencode, Bodu.Text.Toml, and Bodu.Text.Yaml — object serializers

POCO ⇄ wire format, `System.Text.Json`-shaped. The three libraries share an architecture and vocabulary — everything learned for one transfers to the next by swapping the prefix. Each has its own guide set.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="../serialization/index.md">Overview</a></h3>
  <p>The three libraries, the shared tiers (serializer, DOMs, reader / writer), how to choose a format, and the per-library guide hubs.</p>
</div>

<div class="bodu-card">
  <h3><a href="../serialization/toml/index.md">TOML guides</a></h3>
  <p><code>TomlSerializer</code> — type mapping, spec-version selection, both DOMs, the full attribute family, converters, callbacks, and the built-in catalog.</p>
</div>

<div class="bodu-card">
  <h3><a href="../serialization/bencode/index.md">Bencode guides</a></h3>
  <p><code>BencodeSerializer</code> — byte strings, canonical key ordering, the kinds Bencode cannot represent, attributes, converters, callbacks, and the catalog.</p>
</div>

<div class="bodu-card">
  <h3><a href="../serialization/yaml/index.md">YAML guides</a></h3>
  <p><code>YamlSerializer</code> — type mapping, the 1.2 core schema, both DOMs, multi-document streams, <code>[Yaml…]</code> attributes, custom converters, and the catalog.</p>
</div>

</div>

## Suggested reading path

1. **[Topic overview](../../docs/topics/text-and-serialization.md)** — settle which of the three jobs you have.
2. The matching **overview guide** — [encodings](../text-encoding/index.md), [formats](../formats/index.md), or [serializers](../serialization/index.md).
3. The **per-type walk-through** for your format or encoding — e.g. [Base64](../text-encoding/base64.md), [INI](../formats/ini.md), or [Using TOML](../serialization/toml/using.md).
4. For the serializers, the customization guides — e.g. TOML's **[attributes](../serialization/toml/attributes.md)**, then **[converters](../serialization/toml/converters.md)** (Bencode and YAML carry the same guides under their own hubs).

## Where to go next

- [Text & Serialization topic overview](../../docs/topics/text-and-serialization.md) — the disambiguation triangle, package table, and decision table.
- [Topic concepts](../../docs/topics/text-and-serialization-concepts.md) — codec vs. format vs. serializer, the tier model, framing, canonical output, strictness.
- Package introductions — [Bodu.Text.Encoding](../../docs/text-encoding/index.md), [Bodu.Text.Filtering](../../docs/text-filtering/index.md), [Bodu.Text.Formats](../../docs/formats/index.md), [Bodu serializers](../../docs/serialization/index.md).
