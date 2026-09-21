---
title: YAML guides
---

# YAML guides

Recipe-style walk-throughs for **Bodu.Text.Yaml** (<xref:Bodu.Text.Yaml.YamlSerializer>) — the indentation-structured format member of the [Bodu serializer family](../index.md). YAML keeps the family architecture and the shared attribute family: member shaping is naming policies, the `[PropertyName]` / `[Ignore]` attributes (and the wider `Bodu.Text.Serialization` set), options flags, and custom `YamlConverter<T>` converters. Each guide below is written against the real surface.

If you are new to the family, start with the [introduction](../../../docs/serialization/yaml/index.md) for the format specifics and the [core concepts](../../../docs/serialization/yaml/concepts.md) for the three-tier mental model, then work through the guides in order.

## Guides

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="using.md">Using YAML</a></h3>
  <p><code>YamlSerializer</code> — round-trip an object, configure <code>YamlSerializerOptions</code>, both DOMs, multi-document streams via <code>ParseAllDocuments</code>, spec-version selection, anchors and merge keys on read, and error handling.</p>
</div>

<div class="bodu-card">
  <h3><a href="attributes.md">Mapping attributes</a></h3>
  <p>Shaping members with <code>[PropertyName]</code>, <code>[Ignore]</code>, and the wider shared attribute family, the naming policies, and the options flags.</p>
</div>

<div class="bodu-card">
  <h3><a href="converters.md">Writing converters</a></h3>
  <p>Derive <code>YamlConverter&lt;T&gt;</code>, implement <code>Read(ref Utf8YamlReader, Type, YamlSerializerOptions)</code> and <code>Write(Utf8YamlWriter, T, YamlSerializerOptions)</code>, and register on <code>options.Converters</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="polymorphic-converters.md">Polymorphic converters</a></h3>
  <p><code>YamlConverterFactory</code> for open-generic families and tagged (discriminated) hierarchies — what the built-ins do for an abstract member without one, reading and writing a discriminator, and the two registration placements.</p>
</div>

<div class="bodu-card">
  <h3><a href="callbacks.md">Serialization callbacks</a></h3>
  <p>The four shared <code>IOnSerializing</code> / <code>IOnSerialized</code> / <code>IOnDeserializing</code> / <code>IOnDeserialized</code> lifecycle hooks — defaults that survive omitted keys, post-deserialization validation, derived state on write, and the YAML-specific invocation-order notes.</p>
</div>

<div class="bodu-card">
  <h3><a href="builtin-converters.md">Built-in converter catalog</a></h3>
  <p>Which .NET types map without a user converter, and exactly how each appears in YAML — scalars, enums, collections, dictionaries, objects, and fields.</p>
</div>

</div>

## Suggested reading path

1. **[Using YAML](using.md)** — the end-to-end walk-through of the serializer and both DOMs.
2. **[Mapping attributes](attributes.md)** — declarative shaping covers most customization needs.
3. **[Writing converters](converters.md)** — when a type needs a wire form the defaults do not provide; check the **[built-in catalog](builtin-converters.md)** first so you do not rewrite a provisioned one.
4. **[Polymorphic converters](polymorphic-converters.md)** — when a member is declared as a base type or an open generic.
5. **[Serialization callbacks](callbacks.md)** — lifecycle hooks for defaults, validation, and derived state.

## Where to go next

- [Serializer options: freezing, caching, and thread safety](../options-and-lifetime.md) and [Migrating from System.Text.Json](../migrating-from-system-text-json.md) — the cross-format guides.
- [Bodu serializers guides](../index.md) — the family parent guide hub. The sibling [TOML](../toml/index.md) and [Bencode](../bencode/index.md) hubs cover the twin libraries.
- [Bodu.Text.Yaml introduction](../../../docs/serialization/yaml/index.md) and [core concepts](../../../docs/serialization/yaml/concepts.md) — the format specifics and family vocabulary.
- [Text & Serialization guides](../../topics/text-and-serialization.md) — how these guides sit alongside the encoding and format guides.
- API reference — <xref:Bodu.Text.Yaml>.
