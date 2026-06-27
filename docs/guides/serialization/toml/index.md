---
title: TOML guides
---

# TOML guides

Recipe-style walk-throughs for **Bodu.Text.Toml** (<xref:Bodu.Text.Toml.TomlSerializer>). Each guide is written against the real TOML surface. The sibling libraries [Bodu.Text.Bencode](../bencode/index.md) and [Bodu.Text.Yaml](../yaml/index.md) share the same architecture, so a pattern learned here transfers by swapping the prefix — see the [serializer guides hub](../index.md).

New to the library? Start with the [introduction](../../../docs/serialization/toml/index.md) for what is specific to TOML, then work through the guides below.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="using.md">Using TOML</a></h3>
  <p>Round-trip configuration types, the full type-mapping table, spec-version selection (v1.0.0 / v1.1.0), nested tables and arrays of tables, both DOMs, streams, and error handling.</p>
</div>

<div class="bodu-card">
  <h3><a href="attributes.md">Mapping attributes</a></h3>
  <p>The full <code>[Toml…]</code> attribute family — rename, ignore, include, order, require, extension data, constructor selection, and the precedence ladder.</p>
</div>

<div class="bodu-card">
  <h3><a href="converters.md">Writing converters</a></h3>
  <p>Custom <code>TomlConverter&lt;T&gt;</code> shapes, registration, resolution order, the enum converters, and clear failure with the serialization exception.</p>
</div>

<div class="bodu-card">
  <h3><a href="polymorphic-converters.md">Polymorphic converters</a></h3>
  <p><code>TomlConverterFactory</code> for open-generic families and tagged (discriminated) hierarchies — reading and writing a discriminator.</p>
</div>

<div class="bodu-card">
  <h3><a href="callbacks.md">Serialization callbacks</a></h3>
  <p>The four <code>ITomlOn…</code> lifecycle hooks — defaults that survive omitted keys, post-deserialization validation, and derived state on write.</p>
</div>

<div class="bodu-card">
  <h3><a href="builtin-converters.md">Built-in converter catalog</a></h3>
  <p>Every type the serializer handles without a user converter — its wire form and what the read path accepts.</p>
</div>

</div>

## Where to go next

- [Bodu serializer guides](../index.md) — the family guide hub across all three libraries.
- [Bodu.Text.Toml introduction](../../../docs/serialization/toml/index.md) and [core concepts](../../../docs/serialization/toml/concepts.md) — the format-specific shape and vocabulary.
- [Text & Serialization guides](../../topics/text-and-serialization.md) — how these guides sit alongside the encoding and format guides.
- API reference — <xref:Bodu.Text.Toml>.
