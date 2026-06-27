---
title: Text & Serialization — Concepts
---

# Text & Serialization — Concepts

The four packages in this topic — `Bodu.Text.Encoding`, `Bodu.Text.Formats`, `Bodu.Text.Bencode`, and `Bodu.Text.Toml` — share a small set of cross-cutting ideas. This page defines them once at the topic level; each package's own concepts page (linked at the bottom) carries the full per-package vocabulary.

## Codec, format, serializer

The three jobs preserve different things, and that difference drives every API decision in the topic:

- A **binary-to-text codec** (Base16 / Base32 / Base64 / Base58 / Base85 and friends) preserves the **byte sequence**. It maps raw bytes onto a printable alphabet and back; it attaches no meaning to the bytes, adds no structure, and is exactly invertible — `Decode(Encode(bytes))` is the identical input. Its vocabulary is alphabets, variants, padding, and decoration.
- A **document format** (Delimited, DotEnv, INI) preserves the **document**. Parsing produces a typed value model — rows and fields, ordered entries, sections — and formatting writes the model back. INI and DotEnv additionally preserve trivia (comments, ordering, whitespace) so a parse–edit–format cycle round-trips faithfully. Its vocabulary is value models, codecs, and round-trip rules.
- An **object serializer** (Bencode, TOML) preserves the **object graph**. `Serialize<T>` maps your types, members, and collections onto the format; `Deserialize<T>` binds the format back. The document is a means, not the subject — converters, attributes, and naming policies control the mapping. Its vocabulary is converters, options, DOMs, and readers / writers.

A practical test: if you would be satisfied getting `byte[]` back, you want a codec; if you would be satisfied getting a generic document tree back, you want a format or a DOM; if you want *your* type back, you want a serializer.

## The tier model

Both serializers layer the same four surfaces over their format, and choosing a tier is choosing how much machinery you want between you and the bytes:

| Tier | Bencode / TOML types | Reach for it when |
|---|---|---|
| **Serializer** | `BencodeSerializer` / `TomlSerializer` | You have a model type. The default tier. |
| **Mutable DOM** | `BencodeNode` / `TomlNode` trees | You need to parse, index, edit, and write back without a model. |
| **Read-only DOM** | `BencodeDocument` / `TomlDocument` | You need to inspect a parsed buffer with minimal allocation. |
| **Utf8 reader / writer** | `Utf8BencodeReader` / `Utf8BencodeWriter`, `Utf8TomlReader` / `Utf8TomlWriter` | You process tokens by hand — forward-only, allocation-free `ref struct` machines. |

The tiers nest: the serializer is built on the reader / writer pair, and every custom converter receives that pair directly. `Bodu.Text.Formats` follows a compatible two-tier instinct — a typed value model over a streaming reader / writer — without the serializer tier, because mapping onto user types is not its job.

## Framing and self-describing documents

A **self-framing** (self-describing) format carries its structure inline in the bytes: Bencode's length-prefixed strings and `d…e` / `l…e` containers, TOML's tables and key syntax, INI's section headers, CSV's delimiters and quotes. A reader can walk the document without an external schema. Binary-to-text encodings are the opposite — a Base64 string is structureless payload, and any framing must come from the surrounding context. This is why the codecs expose `OperationStatus`-style streaming over raw spans, while the formats and serializers expose token readers and typed models.

## Canonical output

Where a format admits several spellings of the same data, the libraries pick one and emit it consistently. Bencode is the strict case: the specification requires dictionary keys in ascending bytewise order, and `BencodeSerializer` always writes them that way, so equal inputs produce byte-identical output — a property torrent-style infohashing depends on. The document formats state their round-trip rules instead: INI and DotEnv re-emit comments, ordering, and whitespace as parsed, while Delimited output is canonical per the configured quoting policy.

## Text wire vs. binary wire

Not everything under the `Bodu.Text.*` prefix is text on the wire. TOML is a text format — its natural serialized form is a `string`, and `TomlSerializer` offers `string` overloads alongside the UTF-8 ones. Bencode (BEP 3) is a **binary** format — length-prefixed byte strings with no character-escaping layer — so `BencodeSerializer` works in `byte[]`, `ReadOnlySpan<byte>`, `IBufferWriter<byte>`, and `Stream`, never `string`. The distinction also shapes the value models: a Bencode "string" is a byte string that happens to often hold UTF-8, which is why `byte[]` maps to it natively, whereas TOML represents `byte[]` as an integer array or a Base64 string depending on its `ByteArrayHandling` setting — the latter being a small in-document use of exactly the codec job `Bodu.Text.Encoding` does standalone.

## Round-trip fidelity

Each job comes with its own round-trip promise, and knowing which one you are owed prevents most surprises:

- **Codecs** promise *byte fidelity*: decode-of-encode is the identical byte sequence, always.
- **Formats** promise *document fidelity* to the extent the format defines it: INI and DotEnv preserve comments, ordering, and whitespace through a parse–edit–format cycle; Delimited re-emits canonically per its quoting policy rather than byte-for-byte.
- **Serializers** promise *graph fidelity for representable values*: what the format can express round-trips through `Serialize` / `Deserialize<T>`, and what it cannot express fails loudly (or is delegated to a registered converter) rather than degrading silently — Bencode, for example, has no native boolean, floating-point, or date-time form.

## Strict vs. lenient parsing

Every package in the topic treats strictness as an explicit, opt-in policy rather than a global mood:

- **Codecs** parse strictly by default and loosen per call via `BaseFormatStyles` flags — `IgnoreWhitespace`, `AllowPrefix`, `AllowMissingPadding`.
- **Formats** centralize policy on their options types (`DelimitedParseOptions`, `DotEnvParseOptions`, `IniParseOptions`) — quoting strictness, duplicate-key and duplicate-section handling, escape rules — and offer `Try*` overloads that swap exceptions for `bool` results.
- **Serializers** reject malformed documents with a format-specific parse exception (`BencodeFormatException`, `TomlFormatException`) and binding failures with a serialization exception; tolerance for unmapped members, missing values, and type shapes is configured on `…SerializerOptions` and the attribute family, never silently assumed.

The shared rule across the family: the default path validates, and every relaxation is visible at the call site.

## Options as the unit of configuration

The serializers concentrate configuration on a single reusable object: `BencodeSerializerOptions` / `TomlSerializerOptions` hold the converter list, the property naming policy, ignore conditions, unmapped-member policy, and maximum depth. An options instance becomes read-only the first time it is used and then caches its resolved converters and type metadata — so the intended pattern is one configured instance reused across many operations, not a fresh options object per call. The formats apply the same instinct one tier down: each `*ParseOptions` type is shared between the read and write paths, so a document parsed under a policy is formatted back under the same one.

## Per-package concept pages

| Package | Full vocabulary |
|---|---|
| `Bodu.Text.Encoding` | [Core concepts](../text-encoding/concepts.md) — alphabet, variant, terminal quantum, padding, shortcut, decoration. |
| `Bodu.Text.Formats` | [Core concepts](../formats/concepts.md) — self-framing format, value model, codec, round-trip rules, format exception. |
| `Bodu.Text.Bencode` | [Core concepts](../serialization/bencode/concepts.md) — the serializer, options, converters and resolution, the DOMs, value mapping, errors. |
| `Bodu.Text.Toml` | [Core concepts](../serialization/toml/concepts.md) — the serializer, options, converters and resolution, the DOMs, value mapping, errors. |
| `Bodu.Text.Yaml` | [Core concepts](../serialization/yaml/concepts.md) — the serializer, options, converters and resolution, the DOMs, value mapping, multi-document streams, errors. |
