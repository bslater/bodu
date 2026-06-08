---
title: Streams and async I/O
---

# Streams and async I/O

Bencode is a **buffered**, not a *streaming*, format: a single value can declare an arbitrarily long byte string, dictionary key validation requires the full key set, and the parser is forward-only. The library reflects that by reading and writing the entire payload through pooled buffers — the stream overloads are convenience wrappers around the span path, not incremental codecs.

For the synchronous span / array surface see [Using Bencode](bencode.md); this guide focuses specifically on the `Stream` overloads.

## At a glance

| Direction | Sync | Async |
|---|---|---|
| Decode | `BencodedValue Bencode.Parse(Stream)` | `ValueTask<BencodedValue> Bencode.ParseAsync(Stream, CancellationToken)` |
| Encode | `void Bencode.Format(BencodedValue, Stream)` | `ValueTask Bencode.FormatAsync(BencodedValue, Stream, CancellationToken)` |

All four overloads:

- Throw `ArgumentNullException` for a `null` stream or value.
- Throw `ArgumentException` if the stream does not support the required direction (the parameter name in the exception is the offending stream).
- Throw `BencodeFormatException` for any structural violation in the payload (decode path).
- Throw `OverflowException` if the encoded length would exceed `int.MaxValue` (encode path).
- Throw `OperationCanceledException` if `cancellationToken` is signalled during the async copy (async path).
- **Do not close the stream** — lifetime is the caller's responsibility.

## Pattern 1 — decode synchronously from a file

```csharp
using Bodu.Text.Formats;

using FileStream fs = File.OpenRead("doc.bencode");
BencodedValue root = Bencode.Parse(fs);
```

`Parse(Stream)` copies the stream to a pooled `MemoryStream`, then dispatches to the span parser. The pooled buffer is disposed before this method returns. The file stream is left open — the `using` block is what closes it.

For seekable streams of known length this approach is fine in absolute cost (the copy is one pass through the bytes). For network streams it is the right shape: a half-arrived bencode payload cannot be partially parsed anyway.

## Pattern 2 — decode asynchronously with cancellation

```csharp
using Bodu.Text.Formats;

await using FileStream fs = File.OpenRead("doc.bencode");
BencodedValue root = await Bencode.ParseAsync(fs, cancellationToken);
```

`ParseAsync` does the same buffer-then-parse, using `Stream.CopyToAsync` to absorb the source. Cancellation is checked by the underlying copy — if the token fires mid-copy, `OperationCanceledException` propagates and the parser never runs.

The `BencodedValue` result is built from a temporary byte buffer that is disposed before the method returns; the returned value owns its own copies of every byte string and is safe to keep beyond the call.

## Pattern 3 — encode synchronously to a file

```csharp
using Bodu.Text.Formats;

using FileStream fs = File.Create("doc.bencode");
Bencode.Format(root, fs);
```

The synchronous `Format(value, Stream)`:

1. Validates `value` and `destination` for `null` and writability.
2. Calls `GetEncodedLength(value)` to obtain the exact byte count.
3. Rents a `byte[]` of that size from `ArrayPool<byte>.Shared`.
4. Writes the encoded bytes into the rented buffer with the span path.
5. Calls `destination.Write(buffer, 0, length)` once.
6. Returns the buffer to the pool in a `finally` block.

`GetFormattedLength` uses `checked` arithmetic — if the encoded length would not fit in `int`, it throws `OverflowException` and the rented buffer is never allocated.

## Pattern 4 — encode asynchronously with cancellation

```csharp
using Bodu.Text.Formats;

await using FileStream fs = File.Create("doc.bencode");
await Bencode.FormatAsync(root, fs, cancellationToken);
```

`FormatAsync` is structurally identical to the sync path but uses `WriteAsync(ReadOnlyMemory<byte>, CancellationToken)` for the single output call. Cancellation is checked by the underlying write — if the token fires before the write completes, `OperationCanceledException` propagates and the pooled buffer is still returned via the `finally` clause.

The token is **not** checked between encoding and writing — encoding is synchronous and (typically) fast; the cancellation surface is the I/O call itself.

## Pattern 5 — pipe one bencode payload through a stream

```csharp
using Bodu.Text.Formats;

await using FileStream input = File.OpenRead("source.bencode");
await using FileStream output = File.Create("normalized.bencode");

BencodedValue tree = await Bencode.ParseAsync(input, cancellationToken);
await Bencode.FormatAsync(tree, output, cancellationToken);
```

The output is canonical: every dictionary in the source is re-emitted in raw byte order, every integer in shortest form, every string with the minimal length prefix. Use this pattern to normalize a corpus of bencoded files that may have been produced by encoders with different ordering conventions.

For two-way piping through a network handshake, build the tree once and pass it to both `Format` calls — the encoder is deterministic, so the same tree produces the same bytes every time.

## Pattern 6 — limit input size

Stream `Parse` reads to end of stream. For untrusted streams, wrap the source in a length-limited adapter or apply a server-level cap before calling:

```csharp
using Bodu.Text.Formats;

const long maxBytes = 16L * 1024 * 1024;

if (stream.CanSeek && stream.Length > maxBytes)
    throw new InvalidOperationException("Bencode payload too large.");

BencodedValue root = await Bencode.ParseAsync(stream, cancellationToken);
```

For non-seekable streams (network sockets, decompressors), wrap the stream in a `StreamReader`-style throttle — copy at most `maxBytes` into a `MemoryStream` yourself, then pass that to `Parse`. The bencode parser cannot help here because the size limit is a transport-layer concern.

## Why there is no incremental decoder

A streaming `OperationStatus`-style decoder is a non-goal for bencode for two reasons:

- **Byte-string framing.** A single `5000000:` length prefix declares 5 MB of opaque payload that the parser must read contiguously. Suspending and resuming the parser at byte 3 of that payload would require a stateful machine for a single byte-copy operation.
- **Key ordering.** Dictionary keys must be globally sorted. The parser cannot validate the ordering invariant on the *N*th key without having read every key before it. Suspending mid-dictionary trades complexity for no real benefit.

Streaming-friendly formats (CBOR, MessagePack, length-prefixed protobufs) are better choices when incremental decode actually matters. For bencode, the buffer-then-parse approach is both simpler and identical in total work.

## Streaming the text formats

Unlike bencode, the line- and record-oriented text formats expose forward-only readers and writers that process one logical unit at a time without materialising the whole document. Each format's facade offers `CreateReader` / `CreateWriter` factory methods:

| Format | Reader | Writer |
|---|---|---|
| Delimited (CSV/TSV) | `DelimitedReader.Read` / `ReadAsync` (one row) | `DelimitedWriter.WriteHeader` / `WriteRow` (+ `…Async`) |
| INI | `IniReader.Read` / `ReadAsync` (one entry, exposing `Section`/`Key`/`Value`) | `IniWriter.WriteSection` / `WriteEntry` / `WriteComment` (+ `…Async`) |
| DotEnv | `DotEnvReader.Read` / `ReadAsync` (one entry) | `DotEnvWriter.WriteEntry` / `WriteComment` (+ `…Async`) |

```csharp
using var reader = Ini.CreateReader(File.OpenText("config.ini"));
while (reader.Read())
    Console.WriteLine($"[{reader.Section}] {reader.Key} = {reader.Value}");
```

These readers surface raw units in source order: document-level policies that require the whole document — duplicate-key resolution and INI section merging — are applied by the in-memory `Parse` entry points, not by the streaming readers. `DotEnvReader` parses incrementally even across the embedded newlines and `\`-continuations a double-quoted value may contain; `DelimitedReader.ReadAsync` drains its source asynchronously before parsing (do not interleave `Read` and `ReadAsync` on one instance). All readers and writers take ownership of the supplied `TextReader` / `TextWriter` and dispose it.

## Where to go next

- **[Using Bencode](bencode.md)** — the codec surface and the BEP 3 invariants it enforces.
- **[The BencodedValue model](value-model.md)** — the value types you parse into and encode from.
- **[Core concepts](../../docs/formats/concepts.md)** — vocabulary refresher.
