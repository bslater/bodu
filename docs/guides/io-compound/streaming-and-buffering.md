---
title: Buffered vs streaming access
---

# Buffered vs streaming access

<xref:Bodu.IO.Compound.CompoundFile> can hold a file two ways, chosen by the `buffered` flag on `CompoundFile.Open`. The choice affects memory, the source stream's lifetime, and the behaviour of the <xref:Bodu.IO.Compound.CompoundStream> cursor you open over a stream's bytes. This guide explains both modes and when to pick each.

![CompoundStorage.OpenStream returns a read-only seekable CompoundStream. Under a buffered file the payload is materialized into an in-memory byte array at open time; under a streaming file the cursor walks the stream's sector chain on demand, reading only the bytes within the current sector and advancing.](../../images/diagrams/io-compound-stream-access.svg)

## The two modes

```csharp
using Bodu.IO.Compound;

// Buffered (default): the whole file is read into memory at open time.
using CompoundFile buffered = CompoundFile.Open(File.OpenRead("book.xls"));

// Streaming: sectors are read on demand from the seekable source.
using FileStream source = File.OpenRead("large.msg");
using CompoundFile streaming = CompoundFile.Open(source, buffered: false);
```

| | Buffered (default) | Streaming (`buffered: false`) |
|---|---|---|
| Source after open | Can be closed immediately. | Must stay open and unmodified for the file's lifetime. |
| Memory | Whole file resident. | Bounded — one sector at a time for large streams. |
| Source requirement | Any readable stream. | Must be seekable (`ArgumentException` otherwise). |
| Concurrency | Read-only and safe to share across threads. | Reads are serialized against the shared source position — concurrent, but not parallel. |

The default suits the common case — a few-kilobyte `.xls` or `.msg` — where reading it all up front is simplest and lets you drop the file handle right away. Reach for streaming when the file is large enough that holding it whole is undesirable and you can keep the source open.

## The CompoundStream cursor

`CompoundStorage.OpenStream` returns a `CompoundStream`, a standard read-only, seekable <xref:System.IO.Stream> — `CanRead` and `CanSeek` are `true`, `CanWrite` is `false`. Because it is a `Stream`, it composes with the BCL surfaces that consume one:

```csharp
using CompoundStream stream = file.RootStorage.OpenStream("Workbook");

// Hand it to any Stream consumer.
using var reader = new StreamReader(stream, Encoding.Unicode);
string text = reader.ReadToEnd();

// Or seek and read primitives directly.
stream.Seek(0, SeekOrigin.Begin);
Span<byte> header = stackalloc byte[8];
stream.ReadExactly(header);
```

Under a buffered file the cursor's payload was materialized into a `byte[]` at open time, and `Read` / `Seek` work over that array. Under a streaming file the same cursor instead walks the stream's sector chain in the source: each `Read` locates the sector for the current `Position`, copies only the bytes within that sector, and advances — so the full payload is never resident. The two behave identically from the caller's side; only the memory profile differs.

> A single cursor is not safe for concurrent use — its `Position` advances as you read. Open one cursor per reader.

## AsMemory vs chunked Read

```csharp
using CompoundStream stream = entry.Open();

// Whole-payload view — convenient for small streams.
ReadOnlyMemory<byte> all = stream.AsMemory();
ushort magic = BinaryPrimitives.ReadUInt16LittleEndian(all.Span);
```

`AsMemory` returns a read-only view over the entire payload. For a buffered stream this is a view over the already-materialized bytes with no copy; for a streaming stream it reads the whole chain into memory on request. It does not depend on or advance `Position`.

Prefer chunked `Read` over `AsMemory` for large streaming payloads — `AsMemory` defeats the point of streaming by pulling the whole thing into memory:

```csharp
using CompoundStream stream = entry.Open();

byte[] buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
try
{
    int read;
    while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        sink.Write(buffer.AsSpan(0, read));
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}
```

## Lifetime checklist

- Dispose the `CompoundFile` when finished; it releases the source according to the `leaveOpen` contract.
- Under streaming, keep the source stream open and do not seek or write it yourself until the `CompoundFile` is disposed.
- Dispose each `CompoundStream` cursor when done with it.
- Reading from a `CompoundStream` after its owning `CompoundFile` is disposed throws <xref:System.ObjectDisposedException>.

## Where to go next

- [Reading compound files](reading-compound-files.md) — opening, navigating, and the `Open` vs `ReadAllBytes` choice.
- [Reading property sets](property-sets.md) — the metadata streams.
- [Bodu.IO.Compound API reference](xref:Bodu.IO.Compound).
