---
title: Bodu serializers — Guides
---

# Bodu serializer guides

Recipe-style walk-throughs for the three Bodu serializers — **Bodu.Text.Bencode** (<xref:Bodu.Text.Bencode.BencodeSerializer>), **Bodu.Text.Toml** (<xref:Bodu.Text.Toml.TomlSerializer>), and **Bodu.Text.Yaml** (<xref:Bodu.Text.Yaml.YamlSerializer>). The libraries share an architecture and a `System.Text.Json`-aligned shape, so a pattern learned in one transfers to the next by swapping the `Bencode` / `Toml` / `Yaml` prefix. Each library has its own guide set below, written against its real surface.

If you are new to the family, start with the [introduction](../../docs/serialization/index.md) for the three-tier mental model (serializer, DOMs, reader/writer) and how to choose a format, then open the guide hub for the library you need.

## Per-library guides

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="toml/index.md">TOML guides</a></h3>
  <p><code>TomlSerializer</code> — type mapping, spec-version selection (v1.0.0 / v1.1.0), both DOMs, the full attribute family, converters and factories, callbacks, and the built-in catalog.</p>
</div>

<div class="bodu-card">
  <h3><a href="bencode/index.md">Bencode guides</a></h3>
  <p><code>BencodeSerializer</code> — byte strings as first-class values, canonical key ordering, a torrent-style worked example, the full attribute family, converters and factories, callbacks, and the built-in catalog.</p>
</div>

<div class="bodu-card">
  <h3><a href="yaml/index.md">YAML guides</a></h3>
  <p><code>YamlSerializer</code> — type mapping, the 1.2 core schema (opt-in 1.1 typing), both DOMs, multi-document streams, member shaping with <code>[Yaml…]</code> attributes and naming policies, custom converters, and the built-in catalog.</p>
</div>

</div>

## Suggested reading path

1. **[Introduction](../../docs/serialization/index.md)** — the shared shape and how to choose a format.
2. The **guide hub** for your library — [TOML](toml/index.md), [Bencode](bencode/index.md), or [YAML](yaml/index.md) — starting with its *Using…* walk-through.
3. **Mapping attributes** and **naming policies** — declarative shaping covers most customization needs.
4. **Writing converters** — when a type needs a wire form the defaults do not provide; check the **built-in catalog** first so you do not rewrite a provisioned one.

## Where to go next

- [Bodu serializers introduction](../../docs/serialization/index.md) — the family parent: tiers, headline types, format selection.
- Member introductions — [Bodu.Text.Bencode](../../docs/serialization/bencode/index.md), [Bodu.Text.Toml](../../docs/serialization/toml/index.md), and [Bodu.Text.Yaml](../../docs/serialization/yaml/index.md).
- [Text & Serialization guides](../topics/text-and-serialization.md) — how these guides sit alongside the encoding and format guides.
- API reference — <xref:Bodu.Text.Bencode>, <xref:Bodu.Text.Toml>, and <xref:Bodu.Text.Yaml>.
