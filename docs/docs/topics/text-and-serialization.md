---
title: Text & Serialization — Overview
---

# Text & Serialization

The **Text & Serialization** topic groups the packages that move data into and out of textual representations: binary-to-text codecs, structured document formats, and object serializers. The packages are siblings under the `Bodu.Text.*` prefix but do three deliberately different jobs — knowing which job you have is the entire selection problem, so this page leads with the distinction.

## Three jobs that all sound like "text"

| Job | What it preserves | Package(s) |
|---|---|---|
| **Binary-to-text codec** — bytes ⇄ printable text | The exact byte sequence. No structure is added or interpreted; `Encode` then `Decode` returns the identical bytes. | `Bodu.Text.Encoding` — Base16, Base32, Base58, Base64, Base85, plus Base45, Base62, and Bech32 / Bech32m. |
| **Document format** — parse, edit, and write structured documents | The document's structure and (where the format supports it) its trivia — comments, ordering, whitespace — for faithful round-trips. | `Bodu.Text.Formats` — Delimited (RFC 4180 CSV / TSV), DotEnv, INI; each with a typed value model and streaming readers / writers. |
| **Object serializer** — POCO ⇄ wire format | Your object graph. Types, members, and collections are mapped to the format and bound back, `System.Text.Json`-style. | `Bodu.Text.Bencode` (BEP 3, binary), `Bodu.Text.Toml` (TOML v1.0.0 / v1.1.0, text), and `Bodu.Text.Yaml` (YAML 1.2 core schema, text) — a shared architecture and member shape. |

The boundaries are sharp. Base64 carries *any* bytes but knows nothing about what they mean; an `IniDocument` models sections and entries but does not map them onto your types; `TomlSerializer` maps your types but is not a general-purpose document editor (the DOMs cover that middle ground). When two of these jobs occur together — say, a Base64-encoded blob stored inside a TOML config — you simply compose two packages.

### Codecs: Bodu.Text.Encoding

![Encoding families — payload expansion at a glance](../../images/diagrams/encoding-families.svg)

The codec package fills the gaps `System.Convert` and `System.Buffers.Text.Base64` leave open: the variants the BCL does not cover (base32hex, Crockford, z-base-32, Bitcoin / Flickr / Ripple Base58, Ascii85, Z85, Base45, Base62, Bech32 / Bech32m) and the practical surfaces around them — lenient parsing (`0x` prefix tolerance, whitespace stripping, missing-padding acceptance), formatting decoration (case, prefix, byte spacing, line breaks), sizing and validation helpers, and a unified `IBinaryEncoding` interface so the encoding can be selected at runtime from configuration.

### Document formats: Bodu.Text.Formats

The formats package decodes and encodes **self-framing** documents — formats whose structure is described inline by the bytes themselves. Each of the three families (Delimited, DotEnv, INI) pairs a strongly-typed value model with a static codec (`Parse` / `Format`, `Try*` variants, and `CreateReader` / `CreateWriter` streaming factories), and each raises a typed `*FormatException` deriving from a common `TextFormatException` so parse failures can be caught uniformly.

### Serializers: Bodu.Text.Bencode and Bodu.Text.Toml

The serializers are standalone, self-contained twins shaped after `System.Text.Json`: the same member-for-member surface with only the `Bencode` / `Toml` prefix changing. Each layers four tiers over its format — the `…Serializer` for object mapping, a mutable `…Node` DOM for editing without a model, a read-only `…Document` DOM for low-allocation inspection, and the `Utf8…Reader` / `Utf8…Writer` ref-struct pair for forward-only token processing. Converters, the attribute family, naming policies, and serialization callbacks customize the mapping.

One nearby surface is easy to confuse with all three: the **`Bodu.Text` namespace in `Bodu.Core`** provides *character*-encoding helpers over `System.Text.Encoding` — BOM detection, preamble handling, span-friendly transcoding and validation. It converts bytes to *characters*, not bytes to a printable *alphabet*, and it ships in `Bodu.Core`, not in any package on this page. See the [Bodu.Text introduction](../text/index.md).

> [!NOTE]
> The packages compose but do not depend on one another — each depends only on `Bodu.Core`. Adopting one job never pulls in the machinery of the others.

## Packages in this topic

| Package | Status | What it provides | Docs |
|---|---|---|---|
| `Bodu.Text.Encoding` | Stable | Binary-to-text encodings with span / UTF-8 surfaces, `OperationStatus` streaming, formatting decorations, lenient parsing, and the runtime-pluggable `IBinaryEncoding` contract. | [Introduction](../text-encoding/index.md) |
| `Bodu.Text.Formats` | Stable | Self-framing document formats — Delimited (CSV / TSV), DotEnv, INI — each with a typed value model, `Parse` / `Format` / `Try*` codecs, and forward-only streaming I/O. | [Introduction](../formats/index.md) |
| `Bodu.Text.Bencode` | Stable | Self-contained Bencode (BEP 3) serializer shaped after `System.Text.Json`: `BencodeSerializer`, mutable and read-only DOMs, and the `Utf8BencodeReader` / `Utf8BencodeWriter` ref-struct pair. | [Serializers introduction](../serialization/index.md) · [Bencode](../serialization/bencode/index.md) |
| `Bodu.Text.Toml` | Stable | Self-contained TOML (v1.0.0 / v1.1.0) serializer with the same member-for-member shape: `TomlSerializer`, both DOMs, and `Utf8TomlReader` / `Utf8TomlWriter`. | [Serializers introduction](../serialization/index.md) · [TOML](../serialization/toml/index.md) |
| `Bodu.Text.Yaml` | Preview | Self-contained YAML (1.2 core schema) serializer sharing the family architecture with a YAML-tuned surface: `YamlSerializer`, both DOMs, the `Utf8YamlReader` / `Utf8YamlWriter` pair, block and flow collections, anchors and aliases, and multi-document streams. | [Serializers introduction](../serialization/index.md) · [YAML](../serialization/yaml/index.md) |

The authoritative dependency and status rows live in the [package matrix](../package-matrix.md). All five packages depend only on `Bodu.Core`.

### Namespace orientation

The package names and root namespaces line up one-to-one, with the formats and serializers subdividing by concern:

| Package | Namespaces |
|---|---|
| `Bodu.Text.Encoding` | `Bodu.Text.Encoding` — the per-encoding static classes, the option types, and the `IBinaryEncoding` registry. |
| `Bodu.Text.Formats` | `Bodu.Text.Delimited`, `Bodu.Text.DotEnv`, `Bodu.Text.Ini` — one sibling namespace per format family, plus the shared `TextFormatException` in `Bodu.Text`. |
| `Bodu.Text.Bencode` | `Bodu.Text.Bencode` plus `.Reader`, `.Writer`, `.Document`, `.Nodes`, and `.Serialization` — mirroring the `System.Text.Json` source layout. |
| `Bodu.Text.Toml` | `Bodu.Text.Toml` with the same `.Reader` / `.Writer` / `.Document` / `.Nodes` / `.Serialization` subdivision. |

## Which package do I need?

| Scenario | Reach for | Notes |
|---|---|---|
| "I have bytes and need printable text" — hashes as hex, TOTP secrets, JWT segments, Bitcoin addresses, QR payloads | `Bodu.Text.Encoding` | Pick the family by expansion and alphabet — see the [choose-an-encoding table](../text-encoding/index.md). |
| "I have a CSV / `.env` / INI file" — parse it, walk a typed model, edit, round-trip | `Bodu.Text.Formats` | INI and DotEnv preserve comments and ordering on round-trip; Delimited streams row by row. |
| "I map typed objects to a wire format" — config records, torrent-style payloads | `Bodu.Text.Toml` / `Bodu.Text.Bencode` / `Bodu.Text.Yaml` | `Serialize` / `Deserialize<T>` with converters, attributes, and naming policies; the three libraries share one architecture. |
| "I want to inspect or patch a TOML / Bencode document without a model" | The serializers' DOMs | Mutable `…Node` tree to edit, read-only `…Document` to inspect with minimal allocation. |
| "I need canonical, byte-identical output" — infohash-style hashing over the serialized form | `Bodu.Text.Bencode` | The spec mandates ascending bytewise dictionary-key order, and the serializer always emits it. |
| "Malformed input is expected; I don't want exceptions on the hot path" | Any of the above | `Try*` overloads on the codecs and formats; `IsValid` predicates on the codecs. |
| "I need BOM detection or `System.Text.Encoding` helpers" | The `Bodu.Text` namespace in `Bodu.Core` | Character encodings, not binary-to-text codecs — see [Bodu.Text](../text/index.md) and the [Core Foundations topic](core-foundations.md). |
| "I need EditorConfig-style configuration layering over INI" | `Bodu.Text.Configuration` | Builds on the INI model from `Bodu.Text.Formats` — see the [Configuration topic](configuration.md). |

## Install

```bash
dotnet add package Bodu.Text.Encoding
dotnet add package Bodu.Text.Formats
dotnet add package Bodu.Text.Toml
dotnet add package Bodu.Text.Bencode
```

## Shared design traits

However different the three jobs are, the packages share the suite's design grain, so moving between them costs little:

- **Span- and UTF-8-first.** Every package exposes `ReadOnlySpan<byte>` / `ReadOnlySpan<char>` overloads alongside `string` and `byte[]`; the codecs add `OperationStatus`-returning streaming methods, and the serializers' readers and writers operate on UTF-8 directly.
- **`Try*` alongside throwing entry points.** Codecs (`TryDecode`, `TryGetDecodedLength`), formats (`Try*` parse overloads), and validation predicates let hot paths trade exceptions for `bool` results.
- **Options objects, not parameter sprawl.** Behavior is configured on dedicated types — `BaseFormattingOptions` / `BaseFormatStyles` for the codecs, the per-format `*ParseOptions` for the formats, and `BencodeSerializerOptions` / `TomlSerializerOptions` for the serializers.
- **Typed failures.** Malformed input surfaces as a precise, format-specific exception type rather than a bare `FormatException`, with the failure position where the format can supply one (TOML carries line, column, and offset).
- **Streaming where the format allows it.** Delimited rows, format readers and writers, and the serializers' stream overloads (sync and async) all process input incrementally instead of demanding the whole document in memory.

## A taste of each surface

```csharp
using Bodu.Text.Encoding;
using Bodu.Text.Ini;
using Bodu.Text.Toml;

// 1. Codec — bytes to printable text and back:
string hex   = Base16.Encode(hash);
byte[] token = Base64.Decode(segment, Base64Variant.UrlSafe, BaseFormatStyles.AllowMissingPadding);

// 2. Document format — parse, edit, round-trip with comments preserved:
IniDocument config = Ini.Parse(source);
config.GetOrAddSection("database").SetEntry("port", "5433");
string updated = Ini.Format(config);

// 3. Serializer — POCO to wire format and back:
string toml = TomlSerializer.Serialize(new AppSettings { Name = "demo", Retries = 3 });
AppSettings roundTripped = TomlSerializer.Deserialize<AppSettings>(toml);
```

## Boundaries with neighboring packages

Three nearby surfaces sit just outside this topic, and each boundary is deliberate:

- **`Bodu.Text` (in `Bodu.Core`)** — character-encoding helpers over <xref:System.Text.Encoding?displayProperty=nameWithType>: BOM detection, preamble handling, span-friendly transcoding and validation. Bytes to *characters*, not bytes to a printable alphabet. See the [Bodu.Text introduction](../text/index.md).
- **`Bodu.Text.Configuration`** — EditorConfig-style profile presets, glob-anchored sections, and target-path resolution layered on the INI model that `Bodu.Text.Formats` ships. When you need INI parsing alone, use `Ini` directly; when you need configuration layering, move up a package. See the [Configuration topic](configuration.md).
- **Checksums and digests** — the codecs print hashes; they do not compute them. Hashing lives in the [Hashing & Cryptography topic](hashing-and-cryptography.md).

## Where to go next

- **[Topic concepts](text-and-serialization-concepts.md)** — codec vs. format vs. serializer, the tier model the serializers share, framing, canonical output, and strict vs. lenient parsing.
- **[Bodu.Text.Encoding introduction](../text-encoding/index.md)** — the encoding families, payload expansion, and the shared API shape; [getting started](../text-encoding/getting-started.md) for minimal samples.
- **[Bodu.Text.Formats introduction](../formats/index.md)** — the three format families and their typed value models; [getting started](../formats/getting-started.md) for minimal samples.
- **[Bodu serializers introduction](../serialization/index.md)** — the three libraries and the shared tier model, then the per-format intros for [Bencode](../serialization/bencode/index.md), [TOML](../serialization/toml/index.md), and [YAML](../serialization/yaml/index.md), each with its own getting-started.
- **[Text & Serialization guides](../../guides/topics/text-and-serialization.md)** — the topic's guide landing page.
- **[Package matrix](../package-matrix.md)** — status, dependencies, and install commands for every package.
