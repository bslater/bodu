---
title: Bodu serializers — Introduction
---

# Bodu serializers (Bencode and TOML)

**Bodu.Text.Bencode** and **Bodu.Text.Toml** are two self-contained libraries that map your own types (POCOs, records, collections) to and from a document format. Part of the **[Text & Serialization](../topics/text-and-serialization.md)** topic, each is a standalone package with no shared engine — every type a serializer needs lives inside its own assembly:

| Package | Namespace | Format | Entry point |
|---|---|---|---|
| **Bodu.Text.Bencode** | <xref:Bodu.Text.Bencode> | [Bencode (BEP 3)](https://www.bittorrent.org/beps/bep_0003.html) (binary) | <xref:Bodu.Text.Bencode.BencodeSerializer> |
| **Bodu.Text.Toml** | <xref:Bodu.Text.Toml> | [TOML](https://toml.io/) v1.0.0 / v1.1.0 (text) | <xref:Bodu.Text.Toml.TomlSerializer> |

The two libraries are deliberate twins: they expose the same shape, member for member, so anything you learn for one transfers directly to the other — the only adjustment is the `Bencode` / `Toml` prefix.

## The two members

This page is the family parent: it describes everything the twins share. What is *specific* to each format lives on its own introduction:

| Library | Introduction | In one line |
|---|---|---|
| **Bodu.Text.Bencode** | [Bodu.Text.Bencode](bencode.md) | The binary BEP 3 format — byte strings as first-class values, canonical dictionary ordering, and the converter bridge for the kinds Bencode cannot represent. |
| **Bodu.Text.Toml** | [Bodu.Text.Toml](toml.md) | The human-readable configuration format — a rich native value model (floats, Booleans, RFC 3339 date-times), spec-version selection (v1.0.0 / v1.1.0), and positional parse diagnostics. |

## Core mental model

Each library layers three surfaces over one format:

| Tier | Bodu.Text.Bencode | Bodu.Text.Toml |
|---|---|---|
| **Serializer** (POCO ↔ format) | <xref:Bodu.Text.Bencode.BencodeSerializer> | <xref:Bodu.Text.Toml.TomlSerializer> |
| **Mutable DOM** | <xref:Bodu.Text.Bencode.Nodes.BencodeNode> | <xref:Bodu.Text.Toml.Nodes.TomlNode> |
| **Read-only DOM** | <xref:Bodu.Text.Bencode.Document.BencodeDocument> | <xref:Bodu.Text.Toml.Document.TomlDocument> |
| **Low-level reader / writer** | <xref:Bodu.Text.Bencode.Reader.Utf8BencodeReader> / <xref:Bodu.Text.Bencode.Writer.Utf8BencodeWriter> | <xref:Bodu.Text.Toml.Reader.Utf8TomlReader> / <xref:Bodu.Text.Toml.Writer.Utf8TomlWriter> |

Reach for the **serializer** for object mapping, a **DOM** to inspect or edit a document without a model, and the **`Utf8…Reader` / `Utf8…Writer`** ref-struct pair for allocation-free, forward-only token processing. The serializer is built on the reader/writer pair; a custom converter receives them directly.

## Headline types

### Serializers

| Type | Purpose |
|---|---|
| <xref:Bodu.Text.Bencode.BencodeSerializer> | `Serialize` / `Deserialize<T>` for Bencode (`byte[]`, `ReadOnlySpan<byte>`, `IBufferWriter<byte>`, `Stream`). |
| <xref:Bodu.Text.Toml.TomlSerializer> | `Serialize` / `Deserialize<T>` for TOML (`string`, `ReadOnlySpan<byte>`, `IBufferWriter<byte>`, `Stream`). |
| <xref:Bodu.Text.Bencode.BencodeSerializerOptions> · <xref:Bodu.Text.Toml.TomlSerializerOptions> | Serializer configuration: converters, naming policy, ignore conditions, depth. |
| <xref:Bodu.Text.Bencode.Serialization.BencodeConverter`1> · <xref:Bodu.Text.Toml.Serialization.TomlConverter`1> | Base for a custom converter that controls how one type is read and written. |
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

- **Member introductions** — [Bodu.Text.Bencode](bencode.md) and [Bodu.Text.Toml](toml.md) for what is specific to each format.
- **[Core concepts](concepts.md)** — the serializer, the converter model, the two DOMs, and the reader/writer seam.
- **[Getting started](getting-started.md)** — install and the first round trip in each format.
- **Guides** — [Using TOML](../../guides/serialization/toml.md), [Using Bencode](../../guides/serialization/bencode.md), and [writing converters](../../guides/serialization/converters.md).
- **[Text & Serialization topic](../topics/text-and-serialization.md)** — how the serializers sit alongside `Bodu.Text.Encoding` and `Bodu.Text.Formats`.
