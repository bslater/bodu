---
title: Bodu.Text.Serialization — Guides
---

# Bodu.Text.Serialization guides

Practical, task-focused walkthroughs for the object-mapping serializers. Start with the [introduction](../../docs/serialization/index.md) and [core concepts](../../docs/serialization/concepts.md) for the mental model.

- **[Using TOML](toml.md)** — `TomlSerializer`, the type mapping, spec-version selection, and streams.
- **[Using Bencode](bencode.md)** — `BencodeSerializer`, byte strings, canonical key ordering, and the kinds Bencode cannot represent.
- **[Writing converters](converters.md)** — customise a type's shape with `FormatConverter<T>`, and understand resolution order.
