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
| `Bodu.Text.Formats` | Codec entry point (`Bencode`), value model (`BencodedValue` and the four kinds), supporting types (`BencodedStringComparer`, `BencodedValueKind`), and `BencodeFormatException`. | [Using Bencode](bencode.md) · [The BencodedValue model](value-model.md) · [Streams and async I/O](streaming.md) |

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
