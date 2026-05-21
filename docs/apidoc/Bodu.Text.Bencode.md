---
uid: Bodu.Text.Bencode
---

![Bodu.Text.Bencode](~/images/hero-bencode.svg)

## Purpose

**Bodu.Text.Bencode** decodes and encodes the **Bencode** serialization grammar specified by [BEP 3](https://www.bittorrent.org/beps/bep_0003.html), the BitTorrent metadata format. It is one of four format namespaces shipped by the **Bodu.Text.Formats** package; see also <xref:Bodu.Text.Delimited>, <xref:Bodu.Text.DotEnv>, and <xref:Bodu.Text.Ini>.

Bencode is exposed through the same shape used across the format family: a strongly-typed value tree, a static codec with `Encode` / `Decode` / `TryEncode` / `TryDecode` / `GetEncodedLength` over `ReadOnlySpan<byte>` / `byte[]` / `Stream`, and BEP 3 invariants that the encoder always honours and the parser always enforces. No reflection, no `dynamic`, no schema, no allocations beyond the immutable result graph.

For binary-to-text encodings (Base16 / Base32 / Base64 / Base58 / Base85) that operate on flat byte sequences without a structural grammar, see the companion <xref:Bodu.Text.Encoding> package.

## Static documentation

- **[Bodu.Text.Formats introduction](~/docs/formats/index.md)** — namespaces, headline types, scenarios.
- **[Bodu.Text.Formats core concepts](~/docs/formats/concepts.md)** — vocabulary: format vs codec, value vs document, framing tokens, canonical encoding, byte string vs text, format exception.
- **[Bodu.Text.Formats getting started](~/docs/formats/getting-started.md)** — install and minimal samples for `Decode`, `Encode`, `Try*`, and the stream overloads.
- **[Bodu.Text.Formats guides](~/guides/formats/index.md)** — Using Bencode, the `BencodedValue` model, streams and async I/O.

## Key types

- <xref:Bodu.Text.Bencode.Bencode> — static codec. Exposes `Encode`, `Decode`, `TryEncode`, `TryDecode`, `GetEncodedLength`, plus synchronous and asynchronous `Stream` overloads. The recursive writer emits framing tokens and payload bytes into a destination span; the forward-only parser peeks the leading byte, dispatches on the matching value kind, and rebuilds the value tree under BEP 3 invariants.
- <xref:Bodu.Text.Bencode.BencodedValue> — abstract base for every decoded value. Exposes `Kind` for switch-style dispatch.
- <xref:Bodu.Text.Bencode.BencodedValueKind> — `Integer`, `String`, `List`, `Dictionary`.
- <xref:Bodu.Text.Bencode.BencodedInteger> — signed 64-bit integer; rejects leading zeros, negative zero, and out-of-range overflow.
- <xref:Bodu.Text.Bencode.BencodedString> — length-prefixed raw byte payload. `Bytes`, `Length`, `FromUtf8(string)`, `GetUtf8String()`.
- <xref:Bodu.Text.Bencode.BencodedList> — ordered, possibly-nested list. Constructor rejects `null` elements.
- <xref:Bodu.Text.Bencode.BencodedDictionary> — byte-string-keyed mapping. Keys are stored sorted by raw byte ordinal using <xref:Bodu.Text.Bencode.BencodedStringComparer.Ordinal>; the constructor rejects `null` keys / values and duplicates. Indexer and `TryGetValue(BencodedString)` / `TryGetValue(string)` (UTF-8) look up by byte order.
- <xref:Bodu.Text.Bencode.BencodedStringComparer> — singleton `Ordinal` comparer; implements both `IComparer<BencodedString>` and `IEqualityComparer<BencodedString>`.
- <xref:Bodu.Text.Bencode.BencodeFormatException> — derives from <xref:System.FormatException>; thrown on any BEP 3 violation. The message identifies the exact failure mode.

## Example

```csharp
using Bodu.Text.Bencode;

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
```

## Notes

- **Canonical encoding.** Bencode has exactly one canonical encoding for any value — integers use the shortest decimal representation with no padding and no `+` sign; byte-string lengths use the shortest decimal representation; dictionary keys are sorted by raw byte order; no whitespace is permitted anywhere. The encoder always produces canonical output; the parser rejects every non-canonical input. This rejection is intentional — equivalence between two encodings would break content-addressed identifiers (the SHA-1 of a torrent's `info` dictionary, the *infohash*).
- **BEP 3 strictness.** Leading zeros, negative zero, unsorted or duplicate dictionary keys, missing terminators, and trailing data all surface as <xref:Bodu.Text.Bencode.BencodeFormatException>. The exception message identifies the exact failure mode (e.g. *Bencoded dictionary keys must be unique and sorted by raw byte order*). `TryDecode` and `TryEncode` swap these exceptions for `bool` results.
- **Byte string vs text.** Bencoded strings are raw bytes, not characters. Treating them as text is a per-field decision driven by the consuming format — `info.name` in a torrent is UTF-8, `info.pieces` is a concatenation of 20-byte SHA-1 hashes. <xref:Bodu.Text.Bencode.BencodedString.GetUtf8String> projects when the field is known to be text; <xref:Bodu.Text.Bencode.BencodedString.FromUtf8(System.String)> goes the other way.
- **Stream buffering.** `Bencode.Decode(Stream)` and `DecodeAsync(Stream)` buffer the entire stream into a pooled `MemoryStream` before parsing — Bencode is not a streaming format because framing tokens can be arbitrarily far apart and the parser must consume them in order. The pooled buffer is sized to the stream length where available and released back to <xref:System.Buffers.ArrayPool`1> before the parser runs. Stream encoding writes in a single `Write` / `WriteAsync` call sized to `GetEncodedLength`.
- **Immutable value tree.** Every <xref:Bodu.Text.Bencode.BencodedValue> subclass is immutable from the consumer's perspective — `BencodedDictionary` keys arrive sorted, `BencodedList` items are stored in their construction order, `BencodedString.Bytes` is exposed as a read-only span. Deep equality between two trees is something the consumer composes — `BencodedString.Equals` and `BencodedInteger.Equals` give per-leaf equality, the container types do not.
- **Ordering and equality.** Dictionary keys are ordered and compared by raw byte ordinal — never by Unicode collation, locale, or codepoint. <xref:Bodu.Text.Bencode.BencodedStringComparer.Ordinal> is the singleton comparer used internally; surface it directly when building your own `SortedDictionary` keyed by bencoded values.
- **Determinism.** All encode and decode operations produce identical output across platforms and architectures for the same input. There is no random seed, no thread-local state, and no culture-sensitive code path.
- **See also:** the [introduction](~/docs/formats/index.md), [core concepts](~/docs/formats/concepts.md), and [getting-started](~/docs/formats/getting-started.md); the [Bencode](~/guides/formats/bencode.md), [value-model](~/guides/formats/value-model.md), and [streaming](~/guides/formats/streaming.md) guides.
