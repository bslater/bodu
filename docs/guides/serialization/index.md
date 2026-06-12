---
title: Bodu serializers — Guides
---

# Bodu serializer guides

Practical, task-focused walkthroughs for the two Bodu serializers, **Bodu.Text.Bencode** and **Bodu.Text.Toml**. Start with the [introduction](../../docs/serialization/index.md) and [core concepts](../../docs/serialization/concepts.md) for the mental model.

- **[Using TOML](toml.md)** — `TomlSerializer`, the type mapping, spec-version selection, the DOMs, and streams.
- **[Using Bencode](bencode.md)** — `BencodeSerializer`, byte strings, canonical key ordering, and the kinds Bencode cannot represent.
- **[Mapping attributes](attributes.md)** — rename, ignore, order, require, and capture members with the `[Toml…]` / `[Bencode…]` attribute family.
- **[Writing converters](converters.md)** — customise a type's shape with `BencodeConverter<T>` / `TomlConverter<T>`, and understand resolution order.
- **[Serialization callbacks](callbacks.md)** — hook the lifecycle with the `I…OnSerializing` / `I…OnSerialized` / `I…OnDeserializing` / `I…OnDeserialized` interfaces.
- **[Built-in converter catalog](builtin-converters.md)** — every provisioned converter in both libraries, and how each type is represented in TOML or Bencode.
