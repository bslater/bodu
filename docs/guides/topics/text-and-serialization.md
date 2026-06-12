---
title: Text & Serialization — Guides
---

# Text & Serialization — Guides

Recipe-style walk-throughs for the text family — the `Bodu.Text.Encoding` binary-to-text codecs, the `Bodu.Text.Formats` document formats, and the `Bodu.Text.Bencode` / `Bodu.Text.Toml` object serializers. Three different jobs, three guide sections; this page is the topic-level map.

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
  <p>The forward-only <code>CreateReader</code> / <code>CreateWriter</code> surface — sync and async, cancellation, and input-size limits.</p>
</div>

</div>

## Bodu.Text.Bencode and Bodu.Text.Toml — object serializers

POCO ⇄ wire format, `System.Text.Json`-shaped. Deliberate twins — everything learned for one transfers to the other.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="../serialization/index.md">Overview</a></h3>
  <p>The two libraries, the three tiers (serializer, DOMs, reader / writer), and the full guide list.</p>
</div>

<div class="bodu-card">
  <h3><a href="../serialization/toml.md">Using TOML</a></h3>
  <p><code>TomlSerializer</code>, the type mapping, spec-version selection, the mutable and read-only DOMs, and streams.</p>
</div>

<div class="bodu-card">
  <h3><a href="../serialization/bencode.md">Using Bencode</a></h3>
  <p><code>BencodeSerializer</code>, byte strings, canonical key ordering, and the kinds Bencode cannot represent.</p>
</div>

<div class="bodu-card">
  <h3><a href="../serialization/converters.md">Writing converters</a></h3>
  <p>Custom shapes with <code>BencodeConverter&lt;T&gt;</code> / <code>TomlConverter&lt;T&gt;</code>, factories, and converter resolution order.</p>
</div>

<div class="bodu-card">
  <h3><a href="../serialization/attributes.md">Mapping attributes</a></h3>
  <p>Rename, ignore, order, require, and capture members with the <code>[Toml…]</code> / <code>[Bencode…]</code> attribute family.</p>
</div>

</div>

## Suggested reading path

1. **[Topic overview](../../docs/topics/text-and-serialization.md)** — settle which of the three jobs you have.
2. The matching **overview guide** — [encodings](../text-encoding/index.md), [formats](../formats/index.md), or [serializers](../serialization/index.md).
3. The **per-type walk-through** for your format or encoding — e.g. [Base64](../text-encoding/base64.md), [INI](../formats/ini.md), or [TOML](../serialization/toml.md).
4. For the serializers, the customization guides — **[attributes](../serialization/attributes.md)**, then **[converters](../serialization/converters.md)**.

## Where to go next

- [Text & Serialization topic overview](../../docs/topics/text-and-serialization.md) — the disambiguation triangle, package table, and decision table.
- [Topic concepts](../../docs/topics/text-and-serialization-concepts.md) — codec vs. format vs. serializer, the tier model, framing, canonical output, strictness.
- Package introductions — [Bodu.Text.Encoding](../../docs/text-encoding/index.md), [Bodu.Text.Formats](../../docs/formats/index.md), [Bodu serializers](../../docs/serialization/index.md).
