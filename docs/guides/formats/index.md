---
title: Line-format guides
---

# Line-format guides

Recipe-style walk-throughs for the Bodu line formats — **`Bodu.Text.Delimited`**, **`Bodu.Text.DotEnv`**, and **`Bodu.Text.Ini`** (available together through the `Bodu.Text.Formats` umbrella package).

If you are new to the family, start with the [introduction](../../docs/formats/index.md), the [Core concepts](../../docs/formats/concepts.md) glossary, and the [getting-started page](../../docs/formats/getting-started.md). The guides below assume you know the quartet vocabulary (token reader/writer, serializer, mutable node DOM, read-only document DOM).

## How the libraries work

Each format is a self-contained `System.Text.Json`-shaped library: a forward-only `Utf8*Reader` / `Utf8*Writer` pair over UTF-8 bytes, a `*Serializer` that binds POCOs and dictionaries with the shared attribute/naming-policy/callback layer, a mutable `*Node` DOM for authoring, and a disposable read-only `*Document` DOM for querying.

> The serializer infrastructure (attributes, naming policies, callbacks) is shared with the **TOML**, **Bencode**, and **YAML** libraries — see the [Bodu serializer guides](../serialization/index.md).

## Namespace map

| Namespace | What lives here | Guides |
|---|---|---|
| <xref:Bodu.Text.Delimited> | `DelimitedSerializer`, the dialect-policy enums, and the exceptions; readers/writers and DOMs in the `.Reader` / `.Writer` / `.Nodes` / `.Document` child namespaces. | [Using delimited](delimited.md) |
| <xref:Bodu.Text.DotEnv> | `DotEnvSerializer` and companions, with the same child-namespace layout. | [Using DotEnv](dotenv.md) |
| <xref:Bodu.Text.Ini> | `IniSerializer`, the duplicate-policy enums, and companions; the two readers live in `Bodu.Text.Ini.Reader`. | [Using INI](ini.md) |

## Guides

- [Choosing a text format](choosing-a-format.md) — which format fits which job, round-trip fidelity, error recovery.
- [Using delimited (CSV / TSV)](delimited.md) — documents, typed records, dialect policies, CSV↔TSV conversion.
- [Using DotEnv](dotenv.md) — literal values, quoting, export prefixes, typed settings.
- [Using INI](ini.md) — global keys and sections, comment-preserving edits, duplicate policies.
- [Streams and token-level I/O](streaming.md) — the forward-only readers/writers and the record-streaming serializer surface.

## Suggested reading path

1. [Choosing a text format](choosing-a-format.md)
2. The guide for your format
3. [Streams and token-level I/O](streaming.md) for large inputs

## Where to go next

- The [serializer guides](../serialization/index.md) for the shared attribute family, naming policies, and callbacks.
- `Bodu.Text.Configuration` when you need EditorConfig-style layered configuration rather than raw INI.
