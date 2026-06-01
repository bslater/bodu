---
title: Bodu.Text.Formats — Core concepts
---

# Bodu.Text.Formats — Core concepts

This page is the vocabulary the rest of the documentation assumes. Read it once before the [getting-started samples](getting-started.md) or the [guides](../../guides/formats/index.md), and refer back to it whenever a term feels imprecise.

For the high-level shape of the library and the encode/decode pipeline diagram, start with the [introduction](index.md).

## Format and codec

A **format** is the wire grammar — for now, **Bencode** as specified by [BEP 3](https://www.bittorrent.org/beps/bep_0003.html). The grammar defines four kinds of value (integer, byte string, list, dictionary), the framing tokens that delimit each kind, and the invariants every encoder must preserve.

A **codec** is the static façade that exposes encode and decode operations for a format. For Bencode it is <xref:Bodu.Text.Bencode.Bencode> — a single `static partial class` with the recursive writer, the forward-only parser, and the span / array / stream overloads layered around them.

## Value and document

A **value** is one node in the decoded tree — a `BencodedInteger`, `BencodedString`, `BencodedList`, or `BencodedDictionary`. Every value derives from <xref:Bodu.Text.Bencode.BencodedValue> and exposes a `Kind` property that returns the matching <xref:Bodu.Text.Bencode.BencodedValueKind> member.

A **document** is exactly one top-level value (in practice, almost always a `BencodedDictionary`). `Bencode.Decode(source)` returns that root value; the parser also checks that the entire input was consumed, so a payload with trailing bytes after a complete value is rejected.

## Value kinds

![Bencode value grammar — four kinds, four framings](../../images/diagrams/formats-bencode-grammar.svg)

Bencode defines exactly four value kinds. Each kind has its own framing and its own invariants.

### Integer (`BencodedInteger`)

A signed 64-bit integer framed between `i` and `e` — `i-1024e`, `i0e`, `i9223372036854775807e`. BEP 3 invariants enforced by the parser:

- No leading zeros — `i01e` is rejected.
- No negative zero — `i-0e` is rejected.
- Must fit in `Int64` — values that overflow are rejected with *outside the supported Int64 range*.
- Must be terminated by `e` before end of input.

The encoder always emits the shortest canonical form.

### Byte string (`BencodedString`)

A length-prefixed raw byte payload — `5:hello`, `0:`, `20:` + 20 bytes of binary. The length is an ASCII decimal integer, the separator is `:`, and the payload is exactly that many bytes of opaque binary.

BEP 3 invariants enforced by the parser:

- The length must be a non-negative integer with no leading zeros.
- The colon separator must follow the length.
- The declared payload must fit in the remaining input.
- The length itself must fit in `Int32`.

A bencoded string is **not** required to be text. Consumers that know a field is UTF-8 can call <xref:Bodu.Text.Bencode.BencodedString.GetUtf8String> to project it, or build one from a `string` via <xref:Bodu.Text.Bencode.BencodedString.FromUtf8>. For arbitrary binary, hold onto the raw `Bytes` directly.

### List (`BencodedList`)

An ordered sequence of values, framed between `l` and `e` — `l4:spami42ee` is a two-element list containing the string `spam` and the integer `42`. Elements may be any kind, and lists may nest to any depth.

The constructor rejects a `null` element with an `ArgumentException`. List ordering is preserved exactly.

### Dictionary (`BencodedDictionary`)

A keyed mapping framed between `d` and `e` — `d3:cow3:moo4:spam4:eggse` is `{cow → moo, spam → eggs}`. Keys are **byte strings** (BEP 3 forbids non-string keys); values may be any kind.

BEP 3 invariants enforced by the parser:

- Every key is parsed as a `BencodedString` — `dii42ee` (with an integer key) is rejected.
- Keys must appear in **strict ascending raw byte order**. Out-of-order or duplicate keys cause the parser to reject the input.

The constructor likewise rejects `null` keys, `null` values, and duplicates. Internally the dictionary stores its items in a `SortedDictionary` keyed by <xref:Bodu.Text.Bencode.BencodedStringComparer.Ordinal>, so iteration order is always canonical regardless of insertion order.

## Framing tokens

A **framing token** is a single ASCII byte that announces the kind of the next value to the parser:

| Token | Role |
|---|---|
| `i` | Start of an integer; matching `e` terminates the body. |
| `0`–`9` | Start of a byte-string length; ends at the `:` separator. |
| `l` | Start of a list; matching `e` terminates it. |
| `d` | Start of a dictionary; matching `e` terminates it. |
| `e` | End of an integer, list, or dictionary. |
| `:` | Separator between a string length and its payload. |
| `-` | Sign marker inside an integer body (only as the first character after `i`). |

The parser is forward-only and dispatches solely on the first byte of each value. There is no look-ahead beyond the leading token.

## Canonical encoding

A bencoded value has exactly one **canonical encoding**:

- Integers use the shortest decimal representation with no padding and no `+` sign.
- Byte-string lengths use the shortest decimal representation.
- Dictionary keys are sorted by raw byte order before encoding.
- No whitespace is permitted anywhere in the stream.

The encoder always produces canonical output. The parser rejects every form of non-canonical input — leading zeros, negative zero, unsorted keys, trailing bytes, and so on. This rejection is intentional: any equivalence between two encodings would break content-addressed identifiers (the SHA-1 of a torrent's `info` dictionary, in the BitTorrent case).

`Bencode.GetEncodedLength(value)` walks the tree once and returns the exact canonical-encoded size in bytes, suitable for sizing the destination span or buffer before encoding.

## Encode and decode

**Encode** takes a `BencodedValue` and produces canonical bytes:

| API | Result |
|---|---|
| `byte[] Bencode.Encode(BencodedValue)` | Allocate a `byte[]` sized to `GetEncodedLength` and write into it. |
| `bool Bencode.TryEncode(value, Span<byte>, out int written)` | Write into a caller-provided span; `false` if the span is too small. |
| `void Bencode.Encode(value, Stream)` | Stage to a pooled buffer of exact size and `Write` it in one call. |
| `ValueTask Bencode.EncodeAsync(value, Stream, CancellationToken)` | Async variant using `WriteAsync`. |

**Decode** takes bytes and produces a `BencodedValue`:

| API | Result |
|---|---|
| `BencodedValue Bencode.Decode(ReadOnlySpan<byte>)` | Parse a complete document; reject trailing bytes. |
| `BencodedValue Bencode.Decode(byte[])` | Same, with a `null` guard. |
| `bool Bencode.TryDecode(span, out value, out consumed)` | Parse a single value; `false` on `BencodeFormatException`. |
| `BencodedValue Bencode.Decode(Stream)` | Read the stream to its end into a pooled buffer, then parse. |
| `ValueTask<BencodedValue> Bencode.DecodeAsync(Stream, CancellationToken)` | Async variant of the above. |

The synchronous `Decode(Stream)` and asynchronous `DecodeAsync(Stream)` overloads buffer the entire stream before parsing — bencode is not a streaming format because a value's framing tokens can be arbitrarily far apart and the parser must consume them in order. The pooled `MemoryStream` is disposed before the parser runs.

## Format exception

A <xref:Bodu.Text.Bencode.BencodeFormatException> derives from <xref:System.FormatException> and is thrown by the throwing `Decode` overloads on any structural violation. The message identifies the exact failure mode:

| Message | Trigger |
|---|---|
| *Unexpected end of bencoded data.* | Source ends before a complete value is read. |
| *Unexpected bencode token '{0}' at offset {1}.* | A leading byte does not match any known framing token. |
| *Unterminated bencoded integer.* | `i` opened but `e` was not seen before end of input. |
| *Invalid bencoded integer.* | The integer body is missing or malformed. |
| *Bencoded integers cannot contain leading zeros.* | e.g. `i01e`. |
| *Negative zero is not a valid bencoded integer.* | `i-0e`. |
| *The bencoded integer is outside the supported Int64 range.* | Value would overflow `long`. |
| *Expected a bencoded string length.* | A string slot did not start with a digit. |
| *Bencoded string lengths cannot contain leading zeros.* | e.g. `02:ab`. |
| *Expected ':' after bencoded string length.* | Length digits not followed by `:`. |
| *The bencoded string length exceeds Int32.MaxValue.* | Length numeric overflow. |
| *The bencoded string length exceeds the available input.* | Declared length runs past end of input. |
| *Unterminated bencoded list.* | `l` opened but `e` was not seen before end of input. |
| *Unterminated bencoded dictionary.* | `d` opened but `e` was not seen before end of input. |
| *Bencoded dictionary keys must be byte strings.* | A non-string token appeared in a key slot. |
| *Bencoded dictionary keys must be unique and sorted by raw byte order.* | Duplicate or out-of-order key. |
| *The bencoded value contains trailing data.* | A complete value was followed by extra bytes. |

`TryDecode` and `TryEncode` swallow these exceptions and report failure as a `bool` instead.

## Byte string vs. text

Bencoded strings are **bytes**, not characters. Treating them as text is a per-field decision driven by the consuming format:

- **Always text.** `info.name` in a torrent file is UTF-8 (the BEP says so) — call `GetUtf8String()`.
- **Never text.** `info.pieces` is a concatenation of 20-byte SHA-1 hashes — keep the raw `Bytes`.
- **Sometimes text.** Custom extensions vary; consult the extension spec before projecting.

When you build a value, use `BencodedString.FromUtf8(string)` for the text case and `new BencodedString(ReadOnlySpan<byte>)` / `new BencodedString(byte[])` for raw bytes.

## Ordering and equality

Dictionary keys are ordered and compared by raw byte ordinal — *not* by Unicode collation, locale, or codepoint. The library exposes this as <xref:Bodu.Text.Bencode.BencodedStringComparer.Ordinal>, a singleton that implements both `IComparer<BencodedString>` and `IEqualityComparer<BencodedString>`. Use it when you build a `SortedDictionary` of bencoded keys yourself.

`BencodedString.Equals` compares the underlying byte sequences. `BencodedInteger.Equals` compares `long` values. The container types do not override `Equals`; deep equality of two trees is something the consumer composes.

## Where to go next

- **[Getting started](getting-started.md)** — install + runnable minimal samples.
- **[Bodu.Text.Formats guides](../../guides/formats/index.md)** — deep-dive walk-throughs for every concept above.
- **API reference** — per-namespace pages: [Bencode](xref:Bodu.Text.Bencode), [Delimited](xref:Bodu.Text.Delimited), [DotEnv](xref:Bodu.Text.DotEnv), [Ini](xref:Bodu.Text.Ini).
- **[Introduction](index.md)** — the high-level shape of the library.
