---
uid: Bodu.Text.Formats
---

![Bodu.Text.Formats](~/images/hero-formats.svg)

## Purpose

**Bodu.Text.Formats** decodes and encodes **self-framing binary serialization formats** — formats that describe their own structure inline rather than relying on an external schema. The package ships **Bencode** (the BitTorrent serialization grammar specified by [BEP 3](https://www.bittorrent.org/beps/bep_0003.html)) and the underlying **INI** primitives used by both <xref:Bodu.Text.Configuration> and direct INI consumers.

Each format is exposed through the same shape: a strongly-typed value tree, a static codec with `Encode` / `Decode` / `TryEncode` / `TryDecode` / `GetEncodedLength` over `ReadOnlySpan<byte>` / `byte[]` / `Stream`, and per-format invariants that the encoder always honours and the parser always enforces. No reflection, no `dynamic`, no schema, no allocations beyond the immutable result graph.

For binary-to-text encodings (Base16 / Base32 / Base64 / Base58 / Base85) that operate on flat byte sequences without a structural grammar, see the companion <xref:Bodu.Text.Encoding> package. For EditorConfig / INI configuration layering on top of <xref:Bodu.Text.Formats.IniDocument>, see <xref:Bodu.Text.Configuration>.

## Static documentation

- **[Bodu.Text.Formats introduction](~/docs/formats/index.md)** — namespaces, headline types, scenarios.
- **[Bodu.Text.Formats core concepts](~/docs/formats/concepts.md)** — vocabulary: format vs codec, value vs document, framing tokens, canonical encoding, byte string vs text, format exception.
- **[Bodu.Text.Formats getting started](~/docs/formats/getting-started.md)** — install and minimal samples for `Decode`, `Encode`, `Try*`, and the stream overloads.
- **[Bodu.Text.Formats guides](~/guides/formats/index.md)** — Using Bencode, the `BencodedValue` model, streams and async I/O.

## Key types

**Bencode codec (`Bodu.Text.Formats`)**

- <xref:Bodu.Text.Formats.Bencode> — static codec. Exposes `Encode`, `Decode`, `TryEncode`, `TryDecode`, `GetEncodedLength`, plus synchronous and asynchronous `Stream` overloads. The recursive writer emits framing tokens and payload bytes into a destination span; the forward-only parser peeks the leading byte, dispatches on the matching value kind, and rebuilds the value tree under BEP 3 invariants.
- <xref:Bodu.Text.Formats.BencodedValue> — abstract base for every decoded value. Exposes `Kind` for switch-style dispatch.
- <xref:Bodu.Text.Formats.BencodedValueKind> — `Integer`, `String`, `List`, `Dictionary`.
- <xref:Bodu.Text.Formats.BencodedInteger> — signed 64-bit integer; rejects leading zeros, negative zero, and out-of-range overflow.
- <xref:Bodu.Text.Formats.BencodedString> — length-prefixed raw byte payload. `Bytes`, `Length`, `FromUtf8(string)`, `GetUtf8String()`.
- <xref:Bodu.Text.Formats.BencodedList> — ordered, possibly-nested list. Constructor rejects `null` elements.
- <xref:Bodu.Text.Formats.BencodedDictionary> — byte-string-keyed mapping. Keys are stored sorted by raw byte ordinal using <xref:Bodu.Text.Formats.BencodedStringComparer.Ordinal>; the constructor rejects `null` keys / values and duplicates. Indexer and `TryGetValue(BencodedString)` / `TryGetValue(string)` (UTF-8) look up by byte order.
- <xref:Bodu.Text.Formats.BencodedStringComparer> — singleton `Ordinal` comparer; implements both `IComparer<BencodedString>` and `IEqualityComparer<BencodedString>`.
- <xref:Bodu.Text.Formats.BencodeFormatException> — derives from <xref:System.FormatException>; thrown on any BEP 3 violation. The message identifies the exact failure mode.

**INI primitives (`Bodu.Text.Formats`)**

- <xref:Bodu.Text.Formats.IniDocument> — root model: a preamble (global section) and zero or more named sections in source order. Mutable; supports round-trip parse + save.
- <xref:Bodu.Text.Formats.IniSection> — a named section with an ordered list of <xref:Bodu.Text.Formats.IniEntry> values, plus comment lines preserved verbatim.
- <xref:Bodu.Text.Formats.IniEntry> — a single `key = value` line with optional trailing comment.
- <xref:Bodu.Text.Formats.Ini> — static codec for INI files: `Parse(text, options?)`, `Load(path | Stream)`, `Save(document, path | Stream | TextWriter, options?)`.
- <xref:Bodu.Text.Formats.IniParseOptions> — duplicate-key, duplicate-section, comment-preservation, case-sensitivity options for the INI parser.
- <xref:Bodu.Text.Formats.IniDuplicateKeyBehavior> — `LastWins`, `FirstWins`, `Disallowed`, `Merge`.
- <xref:Bodu.Text.Formats.IniDuplicateSectionBehavior> — `Preserve`, `Merge`, `Disallowed`.

## Example

```csharp
using Bodu.Text.Formats;

// --- Bencode round-trip ----------------------------------------------------
byte[] payload = File.ReadAllBytes("ubuntu.iso.torrent");

BencodedValue root = Bencode.Decode(payload);
BencodedDictionary doc = (BencodedDictionary)root;

string tracker = ((BencodedString)doc["announce"]).GetUtf8String();
BencodedDictionary info = (BencodedDictionary)doc["info"];
string name = ((BencodedString)info["name"]).GetUtf8String();
long pieceLength = ((BencodedInteger)info["piece length"]).Value;

// Pre-size and re-emit canonically.
int size = Bencode.GetEncodedLength(doc);
byte[] reEncoded = new byte[size];
Bencode.TryEncode(doc, reEncoded, out int written);
Debug.Assert(written == size);

// --- INI round-trip --------------------------------------------------------
IniDocument iniDoc = Ini.Parse("""
[server]
host = localhost
port = 8080
""");

string host = iniDoc.Sections[0]["host"]!;     // "localhost"
int port = iniDoc.Sections[0].GetValue<int>("port");

using StringWriter sw = new();
Ini.Save(iniDoc, sw);   // re-emits the canonical form
```

## Notes

- **Canonical encoding.** Bencode has exactly one canonical encoding for any value — integers use the shortest decimal representation with no padding and no `+` sign; byte-string lengths use the shortest decimal representation; dictionary keys are sorted by raw byte order; no whitespace is permitted anywhere. The encoder always produces canonical output; the parser rejects every non-canonical input. This rejection is intentional — equivalence between two encodings would break content-addressed identifiers (the SHA-1 of a torrent's `info` dictionary, the *infohash*).
- **BEP 3 strictness.** Leading zeros, negative zero, unsorted or duplicate dictionary keys, missing terminators, and trailing data all surface as <xref:Bodu.Text.Formats.BencodeFormatException>. The exception message identifies the exact failure mode (e.g. *Bencoded dictionary keys must be unique and sorted by raw byte order*). `TryDecode` and `TryEncode` swap these exceptions for `bool` results.
- **Byte string vs text.** Bencoded strings are raw bytes, not characters. Treating them as text is a per-field decision driven by the consuming format — `info.name` in a torrent is UTF-8, `info.pieces` is a concatenation of 20-byte SHA-1 hashes. <xref:Bodu.Text.Formats.BencodedString.GetUtf8String> projects when the field is known to be text; <xref:Bodu.Text.Formats.BencodedString.FromUtf8(System.String)> goes the other way.
- **Stream buffering.** `Bencode.Decode(Stream)` and `DecodeAsync(Stream)` buffer the entire stream into a pooled `MemoryStream` before parsing — Bencode is not a streaming format because framing tokens can be arbitrarily far apart and the parser must consume them in order. The pooled buffer is sized to the stream length where available and released back to <xref:System.Buffers.ArrayPool`1> before the parser runs. Stream encoding writes in a single `Write` / `WriteAsync` call sized to `GetEncodedLength`.
- **Immutable value tree.** Every <xref:Bodu.Text.Formats.BencodedValue> subclass is immutable from the consumer's perspective — `BencodedDictionary` keys arrive sorted, `BencodedList` items are stored in their construction order, `BencodedString.Bytes` is exposed as a read-only span. Deep equality between two trees is something the consumer composes — `BencodedString.Equals` and `BencodedInteger.Equals` give per-leaf equality, the container types do not.
- **Ordering and equality.** Dictionary keys are ordered and compared by raw byte ordinal — never by Unicode collation, locale, or codepoint. <xref:Bodu.Text.Formats.BencodedStringComparer.Ordinal> is the singleton comparer used internally; surface it directly when building your own `SortedDictionary` keyed by bencoded values.
- **Determinism.** All encode and decode operations produce identical output across platforms and architectures for the same input. There is no random seed, no thread-local state, and no culture-sensitive code path.
- **See also:** the [introduction](~/docs/formats/index.md), [core concepts](~/docs/formats/concepts.md), and [getting-started](~/docs/formats/getting-started.md); the [Bencode](~/guides/formats/bencode.md), [value-model](~/guides/formats/value-model.md), and [streaming](~/guides/formats/streaming.md) guides; the EditorConfig-style layering on top of `IniDocument` in [Bodu.Text.Configuration](~/docs/text-configuration/index.md).
