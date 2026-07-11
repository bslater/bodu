---
title: Bencode guides
---

# Bencode guides

Recipe-style walk-throughs for **Bodu.Text.Bencode** (<xref:Bodu.Text.Bencode.BencodeSerializer>). If you are new to the library, start with the [introduction](../../../docs/serialization/bencode/index.md) for the format specifics — byte strings, canonical ordering, the kinds Bencode cannot represent — and the [core concepts](../../../docs/serialization/bencode/concepts.md) for the three-tier mental model. The sibling [TOML](../toml/index.md) and [YAML](../yaml/index.md) serializers share the same shape; see the [family guide hub](../index.md).

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="using.md">Using Bencode</a></h3>
  <p>The walk-through: round-tripping objects, the type mapping, a torrent-style worked example, canonical ordering, both DOMs, streams, and raw tokens.</p>
</div>

<div class="bodu-card">
  <h3><a href="attributes.md">Mapping attributes</a></h3>
  <p>Shaping the wire form declaratively — renaming, ignoring, ordering, required keys, constructors, extension data, and the precedence ladder.</p>
</div>

<div class="bodu-card">
  <h3><a href="converters.md">Writing converters</a></h3>
  <p><code>BencodeConverter&lt;T&gt;</code> — convert a type the defaults do not, register and resolve it, the enum converters, and failing clearly on malformed data.</p>
</div>

<div class="bodu-card">
  <h3><a href="polymorphic-converters.md">Polymorphic converters</a></h3>
  <p><code>BencodeConverterFactory</code> — open-generic families and tagged (discriminated) hierarchies selected by a <code>"kind"</code> field.</p>
</div>

<div class="bodu-card">
  <h3><a href="callbacks.md">Serialization callbacks</a></h3>
  <p>The four <code>IBencodeOn…</code> hooks — apply defaults, validate, derive state, and observe completed writes across the serialization lifecycle.</p>
</div>

<div class="bodu-card">
  <h3><a href="builtin-converters.md">Built-in converter catalog</a></h3>
  <p>Every type Bencode handles without a user converter — its wire form, what the read path accepts, and the types that need a converter.</p>
</div>

</div>

## Where to go next

- [Runnable samples](../../../samples/bencode.md) — the offline torrent-file sample under `samples/Text.Bencode/`: DOM inspection, canonical round trips, the raw-slice info-hash, and POCO mapping.
- [Bodu.Text.Bencode introduction](../../../docs/serialization/bencode/index.md) — the format specifics behind these guides.
- [Bodu serializer guides](../index.md) — the family hub, with the TOML and YAML guide sets.
- [Text & Serialization guides](../../topics/text-and-serialization.md) — how these guides sit alongside the encoding and format guides.
- API reference — <xref:Bodu.Text.Bencode>.
