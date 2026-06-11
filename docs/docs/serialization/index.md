---
title: Bodu serializers — Introduction
---

# Bodu serializers (Bencode and TOML)

**Bodu.Text.Bencode** and **Bodu.Text.Toml** are two self-contained, [`System.Text.Json`](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializer)-shaped libraries that map your own types (POCOs, records, collections) to and from a document format. Each is a standalone package with no shared engine — every type a serializer needs lives inside its own assembly:

| Package | Namespace | Format | Entry point |
|---|---|---|---|
| **Bodu.Text.Bencode** | <xref:Bodu.Text.Bencode> | [Bencode (BEP 3)](https://www.bittorrent.org/beps/bep_0003.html) (binary) | <xref:Bodu.Text.Bencode.BencodeSerializer> |
| **Bodu.Text.Toml** | <xref:Bodu.Text.Toml> | [TOML](https://toml.io/) v1.0.0 / v1.1.0 (text) | <xref:Bodu.Text.Toml.TomlSerializer> |

The two libraries are deliberate twins: they expose the same shape, member for member, so anything you learn for one transfers directly to the other. Both mirror `System.Text.Json` so closely that the only adjustment from BCL JSON is the `Json` → `Bencode` / `Toml` prefix.

## Core mental model

Each library layers three surfaces over one format, exactly the way `System.Text.Json` layers `JsonSerializer`, `JsonNode`, and `Utf8JsonReader`:

| Tier | `System.Text.Json` | Bodu.Text.Bencode | Bodu.Text.Toml |
|---|---|---|---|
| **Serializer** (POCO ↔ format) | `JsonSerializer` | <xref:Bodu.Text.Bencode.BencodeSerializer> | <xref:Bodu.Text.Toml.TomlSerializer> |
| **Mutable DOM** | `JsonNode` / `JsonObject` / `JsonArray` / `JsonValue` | <xref:Bodu.Text.Bencode.Nodes.BencodeNode> | <xref:Bodu.Text.Toml.Nodes.TomlNode> |
| **Read-only DOM** | `JsonDocument` / `JsonElement` | <xref:Bodu.Text.Bencode.Document.BencodeDocument> | <xref:Bodu.Text.Toml.Document.TomlDocument> |
| **Low-level reader / writer** | `Utf8JsonReader` / `Utf8JsonWriter` | <xref:Bodu.Text.Bencode.Reader.Utf8BencodeReader> / <xref:Bodu.Text.Bencode.Writer.Utf8BencodeWriter> | <xref:Bodu.Text.Toml.Reader.Utf8TomlReader> / <xref:Bodu.Text.Toml.Writer.Utf8TomlWriter> |

Reach for the **serializer** for object mapping, a **DOM** to inspect or edit a document without a model, and the **`Utf8…Reader` / `Utf8…Writer`** ref-struct pair for allocation-free, forward-only token processing. The serializer is built on the reader/writer pair; a custom converter receives them directly.

## Headline types

### Serializers

| Type | Purpose |
|---|---|
| <xref:Bodu.Text.Bencode.BencodeSerializer> | `Serialize` / `Deserialize<T>` for Bencode (`byte[]`, `ReadOnlySpan<byte>`, `IBufferWriter<byte>`, `Stream`). |
| <xref:Bodu.Text.Toml.TomlSerializer> | `Serialize` / `Deserialize<T>` for TOML (`string`, `ReadOnlySpan<byte>`, `IBufferWriter<byte>`, `Stream`). |
| <xref:Bodu.Text.Bencode.BencodeSerializerOptions> · <xref:Bodu.Text.Toml.TomlSerializerOptions> | Converters, naming policy, ignore conditions, depth — the `JsonSerializerOptions` analogue. |
| <xref:Bodu.Text.Bencode.Serialization.BencodeConverter`1> · <xref:Bodu.Text.Toml.Serialization.TomlConverter`1> | Base for a custom converter — the `JsonConverter<T>` analogue. |
| <xref:Bodu.Text.Bencode.BencodeNamingPolicy> · <xref:Bodu.Text.Toml.TomlNamingPolicy> | Camel, snake, and kebab casing policies. |

### Document object models

| Type | Purpose |
|---|---|
| <xref:Bodu.Text.Bencode.Nodes.BencodeNode> · <xref:Bodu.Text.Toml.Nodes.TomlNode> | Mutable, editable tree — `Parse`, index, mutate, write back. |
| <xref:Bodu.Text.Bencode.Document.BencodeDocument> · <xref:Bodu.Text.Toml.Document.TomlDocument> | Read-only, low-allocation tree over a parsed buffer; walked through `RootElement`. |

## Common scenarios

| You want to… | Use |
|---|---|
| Map a config record to and from TOML | `TomlSerializer.Serialize` / `Deserialize<T>` |
| Encode a torrent-style object to Bencode bytes | `BencodeSerializer.Serialize` / `Deserialize<T>` |
| Rename members on the wire | a `[…PropertyName]` attribute or a naming policy |
| Control how a tricky type is written | a `…Converter<T>` |
| Edit a document in place without a model | the mutable `…Node` DOM |
| Inspect a document with minimal allocation | the read-only `…Document` / `…Element` DOM |
| Process tokens by hand | the `Utf8…Reader` / `Utf8…Writer` pair |

## Where to go next

- **[Core concepts](concepts.md)** — the serializer, the converter model, the two DOMs, and the reader/writer seam.
- **[Getting started](getting-started.md)** — install and the first round trip in each format.
- **Guides** — [Using TOML](../../guides/serialization/toml.md), [Using Bencode](../../guides/serialization/bencode.md), and [writing converters](../../guides/serialization/converters.md).
