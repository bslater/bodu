---
title: Bodu.Text.Formats — Introduction
---

# Bodu.Text.Formats

**Bodu.Text.Formats** decodes and encodes self-framing binary serialization formats — formats that describe their own structure inline rather than relying on an external schema. The first format the library ships is **Bencode** (the BitTorrent serialization grammar specified by [BEP 3](https://www.bittorrent.org/beps/bep_0003.html)), exposed through a small, immutable object model and a span- and stream-friendly codec.

The library is intentionally narrow: each format gets a strongly-typed value tree, a static `Encode` / `Decode` entry point, `Try*` variants that swap exceptions for `bool` results, an explicit `GetEncodedLength` for pre-sizing destinations, and synchronous + asynchronous `Stream` overloads. No reflection, no `dynamic`, no allocations beyond the immutable result graph.

## Core mental model

![Bencode encode/decode pipeline — object tree to canonical bytes and back](../../images/diagrams/formats-bencode-pipeline.svg)

A bencoded document is a single tree of typed values. Encoding walks the tree with a recursive writer that emits framing tokens (`i…e`, `l…e`, `d…e`, or a length-prefixed byte string) into a destination span; decoding runs a forward-only parser that peeks the leading byte, dispatches on it, validates the BEP 3 invariants, and rebuilds the tree.

A **`BencodedValue`** is the abstract base. **`Bencode`** is the codec — static `Encode` / `Decode` / `TryEncode` / `TryDecode` / `GetEncodedLength` over `ReadOnlySpan<byte>`, `byte[]`, and `Stream`. The pipeline never silently repairs malformed input: every BEP 3 invariant (no leading zeros, no negative zero, sorted dictionary keys, no trailing bytes) is enforced.

## Key concepts

| Concept | Plain-language meaning |
|---|---|
| **Framed format** | Each value carries its own framing — a leading prefix and trailing delimiter (or a length prefix) — so the parser never needs a schema to know where a value starts or ends. |
| **Value tree** | The decoded document is a `BencodedValue` (Integer · String · List · Dictionary) that may nest other values to any depth. |
| **Byte string** | Bencoded strings are raw bytes with a known length, not necessarily UTF-8. The library preserves the exact payload and offers an opt-in `GetUtf8String()` when the consuming format guarantees text. |
| **Canonical encoding** | A single byte sequence is the canonical form of any given value. Encoding always produces it; decoding rejects non-canonical input (leading zeros, unsorted keys, …). |
| **Codec** | The static façade (`Bencode`) that exposes `Encode` / `Decode` / `Try…` / `GetEncodedLength` / stream overloads. |
| **Format exception** | A `BencodeFormatException` (a `FormatException`) is thrown for any structural violation, with a precise message keyed to the failure mode. |

For the full glossary, see [Core concepts](concepts.md).

### Self-framing vs. binary text encoding

Bencode is not a binary-to-text encoding like Base64. It is a **binary serialization grammar**: integers and structure tokens are emitted as ASCII for human readability, but the string payload is raw bytes — a torrent file's `pieces` field is a SHA-1 hash table, not text. Use [`Bodu.Text.Encoding`](../text-encoding/index.md) when you need to convert raw bytes into a printable alphabet; use **Bodu.Text.Formats** when you need to (de)serialize a structured value tree.

### INI as a sibling format

Alongside Bencode, the package ships the **INI primitives** — <xref:Bodu.Text.Formats.IniDocument>, <xref:Bodu.Text.Formats.IniSection>, <xref:Bodu.Text.Formats.IniEntry>, and the static <xref:Bodu.Text.Formats.Ini> codec — used directly by [`Bodu.Text.Configuration`](../text-configuration/index.md) to layer EditorConfig-style configuration on top of the same model. When you need INI parsing without configuration layering, work with <xref:Bodu.Text.Formats.Ini> directly; when you need glob-anchored sections, profile presets, and a flat colon-delimited view, reach for the Configuration package.

### Canonicality

A bencoded value has exactly one canonical encoding: `i42e`, not `i+42e` or `i042e`; dictionary keys are sorted by raw byte order, not insertion order. The codec produces the canonical form and the parser rejects every non-canonical input. This is a load-bearing property for content-addressed identifiers — the SHA-1 of a torrent's `info` dictionary (the *infohash*) only works because every parser agrees on the canonical bytes.

## Worked example — a one-key torrent dictionary

A single document traces the pipeline end-to-end:

1. Author a value: `new BencodedDictionary([new("length"_utf8, new BencodedInteger(1024))])`.
2. `Bencode.GetEncodedLength(root)` walks the tree once and returns the exact destination size — `d6:lengthi1024ee` is 16 bytes.
3. `Bencode.Encode(root)` rents a buffer of that exact size, recursively emits framing tokens and payload bytes, and returns the byte array.
4. The same bytes round-trip back through `Bencode.Decode(bytes)`. The parser reads `d`, expects key/value pairs, parses `6:length` as the key, parses `i1024e` as the value, reads the trailing `e`, then checks that no input remains.
5. If the source bytes were `d6:lengthi1024e` — missing the trailing `e` — the parser throws `BencodeFormatException` with the message *Unterminated bencoded dictionary*.

Encoding is allocation-conscious: stream variants stage to a pooled buffer of the exact size, the span path writes straight into the caller's destination, and `TryEncode` reports overflow with `false` instead of an exception.

## Common scenarios

| Scenario | Reach for |
|---|---|
| Decode a torrent file from disk | `Bencode.Decode(File.ReadAllBytes(path))` |
| Decode a bencoded payload from a network stream | `await Bencode.DecodeAsync(stream, cancellationToken)` |
| Walk the value tree by kind | `value.Kind` + a switch over `BencodedValueKind` |
| Look up a UTF-8 key in a dictionary | `dict.TryGetValue("announce", out var v)` |
| Re-encode a tree canonically | `Bencode.Encode(tree)` |
| Pre-size a destination span | `int size = Bencode.GetEncodedLength(tree);` |
| Parse without throwing on malformed input | `Bencode.TryDecode(source, out var value, out var consumed)` |
| Write canonically to a `Stream` | `Bencode.Encode(tree, stream)` / `EncodeAsync(tree, stream)` |
| Compare byte-string keys ordinally | <xref:Bodu.Text.Formats.BencodedStringComparer.Ordinal> |
| Treat a key payload as text | <xref:Bodu.Text.Formats.BencodedString.GetUtf8String> |

## Main types

The same surface, grouped by what role you're playing rather than by namespace.

### Types most consumers use

| Type | Purpose |
|---|---|
| <xref:Bodu.Text.Formats.Bencode> | Static codec — `Encode`, `Decode`, `TryEncode`, `TryDecode`, `GetEncodedLength` over spans, byte arrays, and `Stream`. |
| <xref:Bodu.Text.Formats.BencodedValue> | Abstract base for every decoded value; exposes `Kind` for switch-style dispatch. |
| <xref:Bodu.Text.Formats.BencodedValueKind> | `Integer` · `String` · `List` · `Dictionary`. |
| <xref:Bodu.Text.Formats.BencodeFormatException> | Thrown for any BEP 3 violation; the message identifies the exact failure mode. |

### Value types

| Type | Purpose |
|---|---|
| <xref:Bodu.Text.Formats.BencodedInteger> | Signed 64-bit integer; rejects leading zeros, negative zero, and values outside `Int64` range. |
| <xref:Bodu.Text.Formats.BencodedString> | Raw byte string with a `Bytes` payload and helpers (`FromUtf8`, `GetUtf8String`) for the text case. |
| <xref:Bodu.Text.Formats.BencodedList> | Ordered list of values; constructor rejects `null` elements. |
| <xref:Bodu.Text.Formats.BencodedDictionary> | Byte-string-keyed dictionary; keys are stored sorted by raw byte order. Indexer, `TryGetValue(BencodedString)`, and `TryGetValue(string)` (UTF-8) all work. |

### Supporting types

| Type | Purpose |
|---|---|
| <xref:Bodu.Text.Formats.BencodedStringComparer> | Singleton `Ordinal` comparer for raw byte-string ordering; implements both `IComparer<BencodedString>` and `IEqualityComparer<BencodedString>`. |

## Where to go next

- **[Core concepts](concepts.md)** — full vocabulary: value vs. document, canonical encoding, framing tokens, byte string vs. text, format exception.
- **[Getting started](getting-started.md)** — install + minimal samples for `Decode`, `Encode`, `Try*`, and the stream overloads.
- **[Bodu.Text.Formats guides](../../guides/formats/index.md)** — using the Bencode codec, the value model, and stream support.
- **[Bodu.Text.Formats API reference](../../apidoc/Bodu.Text.Formats.md)** — full type-by-type docs.
- **For EditorConfig-style configuration layering on `IniDocument`**, see [Bodu.Text.Configuration](../text-configuration/index.md).
