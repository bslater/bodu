---
title: Using Bencode
---

# Using Bencode

`Bencode` is the static codec for the [BEP 3 Bencode grammar](https://www.bittorrent.org/beps/bep_0003.html). It exposes a small, predictable surface — `Encode` / `Decode` over spans, byte arrays, and streams; `TryEncode` / `TryDecode` for non-throwing callers; `GetEncodedLength` for pre-sizing destinations.

For the vocabulary used below (value vs. document, canonical encoding, framing tokens, byte string vs. text) see [Core concepts](../../docs/formats/concepts.md).

## Pattern 1 — decode bytes you already have

```csharp
using Bodu.Text.Formats;

byte[] payload = File.ReadAllBytes("torrent.torrent");
BencodedValue root = Bencode.Decode(payload);
```

`Decode(byte[])` is a `null`-guarded shim over the span overload `Decode(ReadOnlySpan<byte>)`. Both parse exactly one complete value and reject any trailing bytes — a payload that holds two concatenated values throws `BencodeFormatException` with the message *The bencoded value contains trailing data*.

If you genuinely need to read one value out of a larger buffer (for example a Mainline DHT message that carries a bencode header followed by other data), use `TryDecode`, which returns the byte count it consumed.

## Pattern 2 — non-throwing decode

```csharp
using Bodu.Text.Formats;

if (Bencode.TryDecode(buffer, out BencodedValue? value, out int consumed))
{
    Process(value);
    buffer = buffer[consumed..];   // continue reading what follows
}
else
{
    log.Warn("Malformed bencode at byte offset {Offset}", offset);
}
```

`TryDecode` succeeds when the parser reads exactly one well-formed value. On failure it returns `false`, sets `value` to `null`, and sets `consumed` to zero. Unlike `Decode(ReadOnlySpan<byte>)` it does **not** reject trailing bytes — the parser stops after the first complete value and leaves the rest of the buffer to the caller.

## Pattern 3 — pre-size and encode into a caller buffer

```csharp
using Bodu.Text.Formats;

int size = Bencode.GetEncodedLength(value);
Span<byte> destination = size <= 256 ? stackalloc byte[size] : new byte[size];

if (!Bencode.TryEncode(value, destination, out int written))
    throw new InvalidOperationException("Buffer too small.");

return destination[..written];
```

`GetEncodedLength` walks the tree once with `checked` arithmetic and returns the exact destination size — never an upper bound. `TryEncode` writes into a caller-provided span and returns `false` if the destination is shorter than the encoded length; the allocating `Encode(BencodedValue)` overload calls `GetEncodedLength` internally and sizes a fresh `byte[]` to fit.

For values that may exceed `int.MaxValue` bytes, `GetEncodedLength` throws `OverflowException` — wrap the call in a `try` if you intend to accept arbitrarily large inputs.

## Pattern 4 — encode straight to a stream

```csharp
using Bodu.Text.Formats;

using FileStream fs = File.Create("doc.bencode");
Bencode.Encode(value, fs);
```

The synchronous `Encode(value, Stream)` stages to an `ArrayPool<byte>` buffer sized exactly to `GetEncodedLength`, writes it in a single `Write` call, returns the buffer to the pool, and leaves the stream open. The async variant follows the same pattern using `WriteAsync`:

```csharp
await using FileStream fs = File.Create("doc.bencode");
await Bencode.EncodeAsync(value, fs, cancellationToken);
```

Both stream overloads validate that the stream supports writing before allocating the buffer and throw `ArgumentException` with the offending parameter name if not. See [Streams and async I/O](streaming.md) for the buffer-lifecycle details.

## Pattern 5 — decode from a stream

```csharp
using Bodu.Text.Formats;

await using FileStream fs = File.OpenRead("doc.bencode");
BencodedValue root = await Bencode.DecodeAsync(fs, cancellationToken);
```

`Decode(Stream)` and `DecodeAsync(Stream)` copy the entire stream into a pooled `MemoryStream`, then dispatch to the existing span parser. Bencode is **not** a streaming format — a value's framing tokens can be arbitrarily far apart, and dictionary key ordering can only be validated once all keys have been seen, so the parser must have the complete buffer in front of it.

For seekable streams of known length, the cost is one extra copy versus reading into a span yourself. For network streams you usually want the async overload because of the cancellation support.

## Pattern 6 — round-trip equality

```csharp
using Bodu.Text.Formats;

byte[] originalEncoded = File.ReadAllBytes("input.torrent");

BencodedValue decoded = Bencode.Decode(originalEncoded);
byte[] reEncoded = Bencode.Encode(decoded);

Debug.Assert(originalEncoded.SequenceEqual(reEncoded));
```

A canonical bencode payload round-trips bit-for-bit: the parser rejects every non-canonical input, the encoder always emits the canonical form, so any input the parser accepts will re-encode to the same bytes. This is the foundation of the BitTorrent *infohash* — `SHA-1(re-encode(decode(info)))` equals `SHA-1(info)` exactly because the decoder-encoder pair is the identity on canonical input.

If the round-trip assertion ever fires, you have either accepted non-canonical input (a parser bug) or emitted non-canonical output (an encoder bug) — both are contract violations the library is designed to make impossible.

## Pattern 7 — validate without retaining the result

```csharp
using Bodu.Text.Formats;

public static bool IsWellFormedBencode(ReadOnlySpan<byte> source) =>
    Bencode.TryDecode(source, out _, out int consumed) && consumed == source.Length;
```

`TryDecode` exits early on the first structural error; the resulting `BencodedValue` is immediately discarded by `_`. This is useful for input filters at the edge of an application — accept only payloads that parse cleanly, log the rest, drop the rest before any other code touches them.

For the strict *complete-document* semantics of `Decode(ReadOnlySpan<byte>)` (no trailing bytes), check `consumed == source.Length` as shown above.

## Pattern 8 — guard against malicious input

Bencode itself has no built-in size limit on byte strings or recursion depth. For untrusted input — incoming network bytes, user-supplied torrent files — apply an outer length cap before calling the parser:

```csharp
using Bodu.Text.Formats;

const int maxBytes = 16 * 1024 * 1024;   // 16 MB cap

if (payload.Length > maxBytes)
{
    log.Warn("Bencode payload exceeds {Max} bytes; dropping.", maxBytes);
    return;
}

BencodedValue root = Bencode.Decode(payload);
```

The parser already rejects unbounded length prefixes — `BencodeFormatException_StringLengthTooLarge` fires when a single string declares a length larger than `int.MaxValue`. The outer cap above adds protection against an attacker who sends a series of valid-but-massive nested structures.

## Pattern 9 — what happens on malformed input

```csharp
using Bodu.Text.Formats;

byte[] invalid = "d3:cow"u8.ToArray();    // dictionary missing value and 'e'

try
{
    BencodedValue root = Bencode.Decode(invalid);
}
catch (BencodeFormatException ex)
{
    // ex.Message → "Unexpected end of bencoded data." (the next value never started)
}
```

See [Core concepts — Format exception](../../docs/formats/concepts.md#format-exception) for the full message catalogue. Every BEP 3 invariant has a dedicated message; the parser never reports a generic *invalid input* error.

## When to use which overload

| Source | Recommended overload | Notes |
|---|---|---|
| `byte[]` from disk / memory | `Bencode.Decode(byte[])` | Null-guarded; throws on malformed input. |
| `ReadOnlySpan<byte>` from a larger buffer | `Bencode.Decode(span)` or `Bencode.TryDecode(span, …)` | Choose based on whether trailing data is an error or expected. |
| File / network `Stream` | `Bencode.Decode(stream)` / `DecodeAsync(stream)` | Buffers to end before parsing. |
| Speculative parse (might fail) | `Bencode.TryDecode(...)` | Returns `false` instead of throwing. |

| Destination | Recommended overload | Notes |
|---|---|---|
| Fresh `byte[]` | `Bencode.Encode(value)` | Allocates `GetEncodedLength(value)` bytes. |
| Caller-owned `Span<byte>` | `Bencode.TryEncode(value, dest, out written)` | `false` if `dest` is shorter than the encoded length. |
| File / network `Stream` | `Bencode.Encode(value, stream)` / `EncodeAsync(value, stream, ct)` | Pooled buffer, single `Write`. |

## Where to go next

- **[The BencodedValue model](value-model.md)** — the value types and their construction rules.
- **[Streams and async I/O](streaming.md)** — buffer lifecycle, cancellation, and stream contracts.
- **[Core concepts](../../docs/formats/concepts.md)** — vocabulary refresher.
