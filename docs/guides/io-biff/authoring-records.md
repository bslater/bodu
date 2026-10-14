---
title: Authoring a workbook stream
---

# Authoring a workbook stream

<xref:Bodu.IO.Biff.BiffWriter> serializes records; it does not decide which records a workbook needs, in what order, or where each sheet starts. This guide assembles a complete BIFF8 workbook stream from those records - the workbook globals, one substream per sheet, the sheet directory with its offsets, and a shared string table - and stores it in a compound file that `Bodu.Formats.Excel.Binary` reads back. It ends with the BIFF5 differences.

## The shape of a workbook stream

A workbook stream is a sequence of **substreams**, each opened by a `BOF` record and closed by an `EOF` record:

1. **The workbook globals**, first: the code page, the date system, the shared string table, and one `BOUNDSHEET` record per sheet, which names the sheet and gives the stream offset of its `BOF` record.
2. **One substream per sheet**, in any order the `BOUNDSHEET` offsets describe: a `DIMENSIONS` record with the used range, then the cell records.

The writer enforces only that each `EOF` closes a `BOF`. Everything else - the order, the offsets, the counts - is yours, and <xref:Bodu.IO.Biff.BiffWriter.BytesCommitted> is how you learn where each record starts.

## Pattern 1 - write a two-sheet workbook

A `BOUNDSHEET` record precedes the sheet it points to, so the offset it carries is not known when it is written. Write a placeholder, remember where the record starts, and patch the offset once the sheet's `BOF` has been written: the offset is the first four bytes of the record's payload, which begins after the four-byte record header.

<!-- compile -->
```csharp
using System.Buffers;
using System.Buffers.Binary;
using Bodu.IO.Biff;
using Bodu.IO.Compound.Builders;

string[] sharedStrings = ["Region", "Revenue", "North", "South", "Figures are in thousands."];

var output = new ArrayBufferWriter<byte>();
var writer = new BiffWriter(output, new BiffWriterOptions { Version = BiffVersion.Biff8 });

// The workbook globals: the encoding, the date system, the shared strings, and the sheet directory.
writer.WriteBof(BiffSubstreamType.WorkbookGlobals);
writer.WriteCodePage(1200);                        // 1200 declares UTF-16LE text, as BIFF8 writes it
writer.WriteDateMode(is1904: false);
writer.WriteSst(sharedStrings, totalReferenceCount: 5);

long salesEntry = writer.BytesCommitted;           // where each BOUNDSHEET record starts
writer.WriteBoundSheet(0, BiffSheetState.Visible, BiffSheetType.Worksheet, "Sales");
long notesEntry = writer.BytesCommitted;
writer.WriteBoundSheet(0, BiffSheetState.Visible, BiffSheetType.Worksheet, "Notes");
writer.WriteEof();

// The first sheet: a header row and two data rows.
long salesOffset = writer.BytesCommitted;
writer.WriteBof(BiffSubstreamType.Worksheet);
writer.WriteDimensions(new BiffDimensionsRecord(0, 3, 0, 2));   // rows 0-2, columns 0-1, each end exclusive
writer.WriteLabelSst(0, 0, 0, 0);                  // row, column, XF index, shared-string index
writer.WriteLabelSst(0, 1, 0, 1);
writer.WriteLabelSst(1, 0, 0, 2);
writer.WriteNumber(1, 1, 0, 1250.75);
writer.WriteLabelSst(2, 0, 0, 3);
if (BiffRk.TryEncode(980, out uint rk))
    writer.WriteRk(2, 1, 0, rk);                   // the compact form, when the value has one
else
    writer.WriteNumber(2, 1, 0, 980);
writer.WriteEof();

// The second sheet: one note.
long notesOffset = writer.BytesCommitted;
writer.WriteBof(BiffSubstreamType.Worksheet);
writer.WriteDimensions(new BiffDimensionsRecord(0, 1, 0, 1));
writer.WriteLabelSst(0, 0, 0, 4);
writer.WriteEof();

// Patch each BOUNDSHEET record's offset now that the sheets' positions are known.
byte[] workbook = output.WrittenSpan.ToArray();
BinaryPrimitives.WriteUInt32LittleEndian(workbook.AsSpan((int)salesEntry + BiffLimits.RecordHeaderSize), (uint)salesOffset);
BinaryPrimitives.WriteUInt32LittleEndian(workbook.AsSpan((int)notesEntry + BiffLimits.RecordHeaderSize), (uint)notesOffset);

// Store the stream in a compound file, under the name a BIFF8 workbook uses.
CompoundStorageBuilder root = CompoundStorageBuilder.CreateRoot();
root.AddStream("Workbook", workbook);
root.Save("report.xls");
```

Three things in it are easy to get wrong:

- **The patch position.** `BytesCommitted` before `WriteBoundSheet` is the offset of the record's header; the stream offset field starts `BiffLimits.RecordHeaderSize` (4) bytes later. Writing the placeholder costs nothing, because the record's length does not depend on the value.
- **The `DIMENSIONS` ends.** <xref:Bodu.IO.Biff.BiffDimensionsRecord> takes the first row and column and one past the last, as the format stores them.
- **The XF index.** Every cell record names an extended format (`XF`) record by index. This workbook writes none, so the cells name index 0; see [What this workbook leaves out](#what-this-workbook-leaves-out).

If you would rather not patch, size the globals first: every record in them except the `BOUNDSHEET` offsets is known before the sheets are written, so writing the globals once to a scratch `ArrayBufferWriter<byte>` gives their length, and the first sheet starts there.

## Pattern 2 - read it back

`Bodu.Formats.Excel.Binary` opens the result, follows the `BOUNDSHEET` offsets, and resolves the shared strings:

<!-- compile -->
```csharp
using Bodu.Formats.Excel;

using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead("report.xls");
foreach (ExcelWorksheetInfo info in workbook.Worksheets)
{
    ExcelWorksheet sheet = workbook.ReadWorksheet(info.Index);
    foreach (ExcelCell cell in sheet.Cells)
        Console.WriteLine($"{info.Name}!R{cell.RowIndex}C{cell.ColumnIndex}: {cell.StringValue ?? cell.NumberValue?.ToString()}");
}
```

It prints the five text cells and the two numbers, the RK cell among them as `980`.

## The shared string table and `CONTINUE` records

A BIFF8 record holds at most 8,224 bytes of payload (`BiffLimits.Biff8MaxPayloadLength`), and a shared string table often holds more. The format continues it in `CONTINUE` records with rules of its own: a string's header (its length and flags) is never split, its characters may be split only between characters, and each continued run of characters starts with a fresh flags byte, so one part of a string can be stored one byte per character and the next two. `WriteSst` applies those rules; you pass the strings, and it writes as many `CONTINUE` records as the table needs.

<!-- compile -->
```csharp
using System.Buffers;
using Bodu.IO.Biff;

string[] strings = Enumerable.Range(0, 1000).Select(i => $"Customer reference {i:D5}").ToArray();

var output = new ArrayBufferWriter<byte>();
var writer = new BiffWriter(output, BiffVersion.Biff8);
writer.WriteSst(strings);

int continuations = 0;
var reader = new BiffReader(output.WrittenSpan, new BiffReaderOptions { Version = BiffVersion.Biff8 });
while (reader.Read())
{
    if (reader.RecordType == BiffRecordType.Continue)
        continuations++;
}

Console.WriteLine($"{output.WrittenCount} bytes: one SST record and {continuations} CONTINUE records");
```

Do not write a string table with `WriteContinuedRecord`. It splits a payload at the maximum record length whatever lies there, which is right for structures the format lets continue at any byte, but would cut a string's header in two or leave a continued run without its flags byte. The `WriteSst` overload without a reference count records the number of strings as the number of references; pass `totalReferenceCount` when you know how many `LABELSST` cells use the table, as Pattern 1 does.

## Writing BIFF5

<xref:Bodu.IO.Biff.BiffWriterOptions> selects the version and, for BIFF5, the code page text is encoded in:

<!-- compile -->
```csharp
using System.Buffers;
using Bodu.IO.Biff;

var output = new ArrayBufferWriter<byte>();
var writer = new BiffWriter(output, new BiffWriterOptions { Version = BiffVersion.Biff5, CodePage = 1252 });

writer.WriteBof(BiffSubstreamType.Worksheet);
writer.WriteDimensions(new BiffDimensionsRecord(0, 1, 0, 2));
writer.WriteLabel(0, 0, 0, "Café");                // inline text, encoded in Windows-1252
writer.WriteNumber(0, 1, 0, 3.5);
writer.WriteEof();

Console.WriteLine($"{writer.Version}, code page {writer.CodePage}, records of at most {writer.MaxPayloadLength} bytes");
```

The differences from BIFF8 follow from the format:

- **No shared string table.** `WriteSst` and `WriteLabelSst` throw `InvalidOperationException` under BIFF5; cells carry their text inline, through `WriteLabel`.
- **Text in a code page.** Text is written in the options' code page (Windows-1252 when it is zero), and the globals should declare the same page in a `CODEPAGE` record so a reader decodes it the same way.
- **Shorter records.** A BIFF5 record holds at most 2,080 bytes of payload, and every writer checks its record against the limit of the version it writes.
- **A different stream name.** A BIFF5 workbook is stored in a stream named `Book`, not `Workbook`.

## What this workbook leaves out

The workbook above is the minimum `Bodu.Formats.Excel.Binary` reads. Applications that display or edit workbooks expect a fuller globals substream - fonts, cell formats (`XF` records), number formats, and window settings among them - and a stream meant for them needs those records too. The writer emits `FONT`, `FORMAT`, and `XF` records through `WriteFont`, `WriteFormat`, and `WriteXf`, and any other record from its identifier and payload through `WriteRecord`; [MS-XLS] specifies which records each substream requires and in what order.

## Where to go next

- **[Reading a stream a block at a time](resumable-reading.md)** - the reading side, for streams you do not want in memory whole.
- **[Record reference](../../docs/io-biff/records.md)** - every record the writer emits, with its layout in each version.
- **[Authoring compound files](../io-compound/authoring-compound-files.md)** - the container side: the builder, deferred streams, and in-place editing.
- **[Reading workbooks](../excel/reading-workbooks.md)** - the reader that opened the result.
