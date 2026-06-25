---
title: Bodu serializers — Guides
---

# Bodu serializer guides

Recipe-style walk-throughs for the Bodu serializers — **Bodu.Text.Bencode** (<xref:Bodu.Text.Bencode.BencodeSerializer>), **Bodu.Text.Toml** (<xref:Bodu.Text.Toml.TomlSerializer>), and **Bodu.Text.Yaml** (<xref:Bodu.Text.Yaml.YamlSerializer>). The libraries share the same `System.Text.Json`-aligned shape, member for member, so most patterns transfer by swapping the `Bencode` / `Toml` / `Yaml` prefix.

If you are new to the family, start with the [introduction](../../docs/serialization/index.md) for the four-tier mental model (serializer, DOMs, reader/writer), the [core concepts](../../docs/serialization/concepts.md) for the shared vocabulary, and the per-format member introductions — [Bodu.Text.Bencode](../../docs/serialization/bencode.md), [Bodu.Text.Toml](../../docs/serialization/toml.md), and [Bodu.Text.Yaml](../../docs/serialization/yaml.md) — for what is specific to each wire format.

## Guides

### Using the serializers

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="toml.md">Using TOML</a></h3>
  <p><code>TomlSerializer</code> — round-tripping configuration types, the full type mapping, spec-version selection (v1.0.0 / v1.1.0), both DOMs, streams, and error handling.</p>
</div>

<div class="bodu-card">
  <h3><a href="bencode.md">Using Bencode</a></h3>
  <p><code>BencodeSerializer</code> — byte strings as first-class values, canonical key ordering, a torrent-style worked example, the kinds Bencode cannot represent, and error handling.</p>
</div>

<div class="bodu-card">
  <h3><a href="yaml.md">Using YAML</a></h3>
  <p><code>YamlSerializer</code> — round-tripping configuration types, implicit typing (1.1 / 1.2), anchors and merge keys, multi-document streams, both DOMs, and error handling.</p>
</div>

</div>

### Customizing the mapping

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="attributes.md">Mapping attributes</a></h3>
  <p>Rename, ignore, order, require, and capture members with the <code>[Toml…]</code> / <code>[Bencode…]</code> attribute family, and the precedence ladder that resolves conflicts.</p>
</div>

<div class="bodu-card">
  <h3><a href="converters.md">Writing converters</a></h3>
  <p>Customize a type's shape with <code>BencodeConverter&lt;T&gt;</code> / <code>TomlConverter&lt;T&gt;</code>, converter factories for type families, registration, and resolution order.</p>
</div>

<div class="bodu-card">
  <h3><a href="polymorphic-converters.md">Polymorphic converters</a></h3>
  <p>Serialize and round-trip type hierarchies — emitting a type discriminator and dispatching to the right derived type on the read path.</p>
</div>

<div class="bodu-card">
  <h3><a href="callbacks.md">Serialization callbacks</a></h3>
  <p>Hook the lifecycle with the <code>I…OnSerializing</code> / <code>I…OnSerialized</code> / <code>I…OnDeserializing</code> / <code>I…OnDeserialized</code> interfaces — defaults, validation, derived state.</p>
</div>

<div class="bodu-card">
  <h3><a href="builtin-converters.md">Built-in converter catalog</a></h3>
  <p>Every provisioned converter in both libraries — how each .NET type is represented in TOML or Bencode, and what the read path accepts.</p>
</div>

</div>

## Suggested reading path

1. **[Introduction](../../docs/serialization/index.md)** and **[core concepts](../../docs/serialization/concepts.md)** — the shared shape and vocabulary.
2. The walk-through for your format — **[Using TOML](toml.md)** or **[Using Bencode](bencode.md)**.
3. **[Mapping attributes](attributes.md)** — declarative shaping covers most customization needs.
4. **[Writing converters](converters.md)** — when a type needs a wire form the defaults do not provide; check the **[built-in catalog](builtin-converters.md)** first so you do not rewrite a provisioned one.
5. **[Serialization callbacks](callbacks.md)** — lifecycle hooks for defaults, validation, and derived state.

## Where to go next

- [Bodu serializers introduction](../../docs/serialization/index.md) — the family parent: tiers, headline types, scenarios.
- Member introductions — [Bodu.Text.Bencode](../../docs/serialization/bencode.md) and [Bodu.Text.Toml](../../docs/serialization/toml.md).
- [Core concepts](../../docs/serialization/concepts.md) and [getting started](../../docs/serialization/getting-started.md).
- [Text & Serialization guides](../topics/text-and-serialization.md) — how these guides sit alongside the encoding and format guides.
- API reference — <xref:Bodu.Text.Bencode> and <xref:Bodu.Text.Toml>.
