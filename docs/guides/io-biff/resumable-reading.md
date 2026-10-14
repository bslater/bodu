---
title: Reading a stream a block at a time
---

# Reading a stream a block at a time

<xref:Bodu.IO.Biff.BiffReader> reads a span, and the simplest use hands it the whole record stream. A large workbook stream need not be in memory at once: the reader can take one block, report how far it got, and hand its state to a reader over the next block. This guide builds that loop over any `Stream` - a `CompoundStream` opened with the `Streaming` strategy, or a stream that cannot seek at all - including the one structure that needs care, the shared string table.

## The three pieces

| Piece | Role |
|---|---|
| `new BiffReader(block, isFinalBlock, state)` | A reader over one block, starting at a record header, continuing from the state of the reader before it. |
| `BytesConsumed` | How far the reader got: the offset in the block of the first record it has not read. The bytes from there on are the start of the next block. |
| `CurrentState` | The <xref:Bodu.IO.Biff.BiffReaderState> the next reader needs: the version from the first `BOF` record and the code page from the last `CODEPAGE` record. The reader keeps no other state. |

With `isFinalBlock: false`, a record cut off at the end of the block makes `Read()` return `false` without consuming it, so you can move it to the front, read more bytes after it, and try again. With `isFinalBlock: true`, the same record is malformed, and `Read()` throws <xref:Bodu.IO.Biff.BiffFormatException>.

## Pattern 1 - read any stream in blocks

The loop keeps one buffer. It fills the buffer from the stream, lets a reader take every whole record in it, moves the unread tail to the front, and repeats; when the stream ends, the last block is read as the final one.

<!-- compile -->
```csharp
using Bodu.IO.Biff;
using Bodu.IO.Compound;

using CompoundFile container = CompoundFile.OpenRead(
    "report.xls", new CompoundFileOptions { ReadStrategy = CompoundReadStrategy.Streaming });
using CompoundStream workbook = container.RootStorage.OpenStream("Workbook");

var sharedStrings = new List<string>();
int records = ReadInBlocks(workbook, sharedStrings);
Console.WriteLine($"{records} records, {sharedStrings.Count} shared strings");

// Reads a BIFF record stream from any stream a block at a time, carrying only the unread tail of each block into
// the next.
static int ReadInBlocks(Stream source, List<string> sharedStrings)
{
    byte[] buffer = new byte[64 * 1024];
    int buffered = 0;
    bool endOfStream = false;
    BiffReaderState state = default;
    int records = 0;

    while (true)
    {
        // Top the buffer up; a read that returns nothing means the stream has ended.
        while (!endOfStream && buffered < buffer.Length)
        {
            int read = source.Read(buffer, buffered, buffer.Length - buffered);
            if (read == 0)
                endOfStream = true;
            else
                buffered += read;
        }

        int consumed = ReadBlock(buffer.AsSpan(0, buffered), endOfStream, ref state, sharedStrings, ref records);
        if (endOfStream)
            return records;

        // A full buffer that yields nothing holds one record, or one string table, larger than the buffer.
        if (consumed == 0 && buffered == buffer.Length)
            Array.Resize(ref buffer, buffer.Length * 2);

        // The unread tail starts at a record header; it becomes the start of the next block.
        buffer.AsSpan(consumed, buffered - consumed).CopyTo(buffer);
        buffered -= consumed;
    }
}

// Reads the whole records in one block and returns the number of bytes consumed.
static int ReadBlock(ReadOnlySpan<byte> block, bool isFinalBlock, ref BiffReaderState state, List<string> sharedStrings, ref int records)
{
    var reader = new BiffReader(block, isFinalBlock, state);
    int consumed = 0;

    while (reader.Read())
    {
        if (reader.RecordType == BiffRecordType.Sst)
        {
            // Decode the table only once every CONTINUE record it spans is in this block.
            if (!isFinalBlock && !HoldsWholeRun(block, reader.RecordStartIndex))
                break;

            var strings = new BiffSstReader(ref reader);
            while (strings.Read(ref reader))
                sharedStrings.Add(strings.GetString());
        }

        records++;
        consumed = reader.BytesConsumed;
        state = reader.CurrentState;
    }

    return consumed;
}

// Whether the record at start, and every CONTINUE record after it, lie wholly in the block with the next record's
// header in view, so that nothing the record continues into is still to arrive.
static bool HoldsWholeRun(ReadOnlySpan<byte> block, int start)
{
    int position = start;
    while (true)
    {
        if (!BiffRecordHeader.TryParse(block.Slice(position), out BiffRecordHeader header))
            return false;

        if (position != start && header.Id != (ushort)BiffRecordType.Continue)
            return true;

        position += BiffLimits.RecordHeaderSize + header.Length;
        if (position > block.Length)
            return false;
    }
}
```

`consumed` and `state` are taken after each record is handled, not when the reader stops, so a record the loop sets aside - here, a string table whose continuation has not arrived - is read again, whole, from the next block.

## Why the buffer may grow

A conformant BIFF8 record is at most 8,228 bytes with its header, but the reader accepts any payload its 16-bit length field can declare, so one record can need 65,539 bytes. A block that holds only part of such a record yields nothing, and the loop doubles the buffer until the record fits. The 64 KiB starting size holds nearly eight of the largest conformant records; growth is for the exceptions.

## The shared string table

`Read()` frames one physical record at a time, so a block boundary between two records is never a problem for it. The shared string table is different: <xref:Bodu.IO.Biff.BiffSstReader> pulls the table's `CONTINUE` records from the parent reader itself, and when the next record is not in the block yet, it has no way to say "wait for more". It takes the table as ended, or, when a string is cut off, throws `BiffFormatException`.

`HoldsWholeRun` closes that gap. It walks the record headers from the `SST` record on, and reports success only when every `CONTINUE` record is complete and the header of the record after them is in the block, which proves the table has ended. Until then the loop leaves the table unread, keeps it at the front of the buffer, and reads more after it. A table larger than the buffer makes the buffer grow, so the table, unlike every other record, is held whole: plan for the largest string table you expect, which is the price of decoding it in one pass.

The same applies to any structure you read with `TryReadContinuation`: check that its run is whole before you start on it.

## Errors and offsets

Everything the reader reports is relative to the block it was created over. `RecordStartIndex` and `BytesConsumed` are offsets in the block, and so is <xref:Bodu.IO.Biff.BiffFormatException.Offset>. To report a position in the stream, keep a running count of the bytes the loop has discarded from the front of the buffer, and add it.

A stream that ends inside a record is malformed, and the final block reports it: `Read()` throws `BiffFormatException` for a trailing fragment shorter than a header, or a payload that runs past the end. A stream that opens with a BIFF2, BIFF3, or BIFF4 `BOF` record throws <xref:Bodu.IO.Biff.BiffUnsupportedVersionException> from the first block.

## Reading one sheet

A sheet substream can be read on its own, from the offset its `BOUNDSHEET` record gives. It opens with its own `BOF` record, which establishes the version, but the code page comes from the globals: a reader started at the sheet decodes BIFF5 text in the default code page unless it is given the state the globals established. Carry that state across, as the block loop does:

<!-- compile -->
```csharp
using Bodu.IO.Biff;
using Bodu.IO.Compound;

using CompoundFile container = CompoundFile.OpenRead("report.xls");
using CompoundStream stream = container.RootStorage.OpenStream("Workbook");
byte[] bytes = new byte[stream.Length];
stream.ReadExactly(bytes);

// Read the globals, noting the last sheet's offset; the reader's state then holds their version and code page.
var globals = new BiffReader(bytes);
long sheetOffset = 0;
while (globals.Read() && globals.RecordType != BiffRecordType.Eof)
{
    if (globals.RecordType == BiffRecordType.BoundSheet)
        sheetOffset = globals.GetBoundSheet().StreamOffset;
}

// Read that sheet alone, from its BOF to its EOF, continuing from the globals' state.
var sheet = new BiffReader(bytes.AsSpan((int)sheetOffset), isFinalBlock: true, globals.CurrentState);
while (sheet.Read() && sheet.RecordType != BiffRecordType.Eof)
    Console.WriteLine($"{sheet.RecordType} ({sheet.RecordLength} bytes)");
```

When you know the version and code page some other way, `new BiffReaderState(new BiffReaderOptions { Version = ..., CodePage = ... })` seeds the same state.

## Where to go next

- **[Authoring a workbook stream](authoring-records.md)** - the writing side, and the stream this guide reads.
- **[Getting started](../../docs/io-biff/getting-started.md)** - reading a whole stream, decoding records, and the shared string table.
- **[Buffered vs streaming access](../io-compound/streaming-and-buffering.md)** - how the container reads a stream's sectors on demand.
- **[Reading untrusted input](../../docs/topics/untrusted-input.md)** - the limits the readers apply to a crafted file.
