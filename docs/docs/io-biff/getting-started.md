---
title: Bodu.IO.Biff — Getting started
---

# Bodu.IO.Biff — Getting started

Unfamiliar with terms like *record*, *substream*, *CONTINUE*, *shared string table*, or *RK*? Read [Core concepts](concepts.md) first.

## Install

```bash
dotnet add package Bodu.IO.Biff
```

Targets `net8.0`. Depends on `Bodu.Core` (shared throw-helpers) and `System.Text.Encoding.CodePages` (BIFF5 byte strings). For the spreadsheet view — sheets, cells by position, number formats, dates — install `Bodu.Formats.Excel.Binary` instead (it references this package) and see [its introduction](../excel/index.md).

## Walk a record stream

The reader takes the bytes of a BIFF stream. Where they come from is up to you — for an `.xls` file, the `Workbook` stream of the compound file, read with `Bodu.IO.Compound`:

```csharp
using Bodu.IO.Biff;
using Bodu.IO.Compound;

using CompoundFile container = CompoundFile.OpenRead("report.xls");
using CompoundStream workbook = container.RootStorage.OpenStream("Workbook");
byte[] bytes = new byte[workbook.Length];
workbook.ReadExactly(bytes);

var reader = new BiffReader(bytes);
while (reader.Read())
    Console.WriteLine($"{reader.RecordType,-14} id=0x{reader.RecordId:X4} length={reader.RecordLength}");
```

`Read()` advances to the next physical record and returns `false` at the clean end of the buffer. Nothing is allocated by advancing; `ValueSpan` is a slice of the buffer you supplied.

## Decode the records you care about

Typed accessors interpret the current record only. Calling one on a record of another type throws `InvalidOperationException`; a record too short for its layout throws <xref:Bodu.IO.Biff.BiffFormatException>.

```csharp
using Bodu.IO.Biff;

var reader = new BiffReader(bytes);
while (reader.Read())
{
    switch (reader.RecordType)
    {
        case BiffRecordType.Bof:
            BiffBofRecord bof = reader.GetBof();
            Console.WriteLine($"{bof.Version} {bof.SubstreamType} substream");
            break;

        case BiffRecordType.BoundSheet:
            BiffBoundSheetRecord sheet = reader.GetBoundSheet();
            Console.WriteLine($"sheet '{sheet.Name.GetString()}' at offset {sheet.StreamOffset}");
            break;

        case BiffRecordType.Number:
            BiffNumberRecord number = reader.GetNumber();
            Console.WriteLine($"R{number.Row}C{number.Column} = {number.Value}");
            break;

        case BiffRecordType.Label:
            BiffLabelRecord label = reader.GetLabel();
            Console.WriteLine($"R{label.Row}C{label.Column} = \"{label.Text.GetString()}\"");
            break;

        case BiffRecordType.Formula:
            BiffFormulaRecord formula = reader.GetFormula();
            Console.WriteLine($"R{formula.Row}C{formula.Column} cached {formula.CachedResultKind}, {formula.Tokens.Length} token bytes");
            break;
    }
}
```

The version-dependent accessors (`GetBoundSheet`, `GetDimensions`, `GetLabel`, `GetString`, `GetFormat`, `GetFont`, `GetFilePass`) need the version to be known: read the `BOF` first, or supply it through the options.

## Read the shared string table

BIFF8 pools cell text in the workbook globals. <xref:Bodu.IO.Biff.BiffSstReader> is created while the reader is on the `SST` record and pulls the `CONTINUE` records itself, advancing the parent as it goes:

```csharp
using Bodu.IO.Biff;

var reader = new BiffReader(bytes);
var sharedStrings = new List<string>();

while (reader.Read())
{
    if (reader.RecordType != BiffRecordType.Sst)
        continue;

    var strings = new BiffSstReader(ref reader);
    Console.WriteLine($"{strings.Header.UniqueCount} unique strings, {strings.Header.TotalCount} references");

    while (strings.Read(ref reader))
        sharedStrings.Add(strings.GetString());
}
```

`GetString()` works for every string. When a string lies within one record, `strings.Current` is a span-backed <xref:Bodu.IO.Biff.BiffString> you can inspect without allocating; when it straddles a `CONTINUE` boundary, `IsFragmented` is `true` and `Current` is unavailable.

## Work with text without allocating

<xref:Bodu.IO.Biff.BiffString> is a view over the record. It knows whether it is a BIFF8 Unicode string (16-bit or compressed) or a BIFF5 byte string, and which code page the latter is in:

```csharp
using Bodu.IO.Biff;

BiffLabelRecord label = reader.GetLabel();
BiffString text = label.Text;

Span<char> buffer = stackalloc char[text.GetCharCount()];
int written = text.CopyTo(buffer);
ReadOnlySpan<char> value = buffer.Slice(0, written);

Console.WriteLine($"{value.Length} chars, unicode={text.IsUnicode}, wide={text.IsHighByte}, code page={text.CodePage}");
```

## Read a sheet substream with a known version

A sheet's substream carries its own `BOF`, so a reader started at the sheet's offset establishes the version itself. When you want the version-dependent accessors to work before that — or you are resuming from a saved state — seed the reader:

```csharp
using Bodu.IO.Biff;

var options = new BiffReaderOptions { Version = BiffVersion.Biff8, CodePage = 1252 };
var sheet = new BiffReader(bytes.AsSpan(sheetOffset), options);
```

## Resume across buffers

<xref:Bodu.IO.Biff.BiffReader> is a `ref struct` and cannot live in a field. A class that reads one record per call, or a loop feeding chunks of a stream, carries <xref:Bodu.IO.Biff.BiffReaderState> instead:

```csharp
using Bodu.IO.Biff;

BiffReaderState state = default;
int position = 0;

// Each pass constructs a reader over the unread remainder with the state from the previous pass.
var reader = new BiffReader(bytes.AsSpan(position), isFinalBlock: true, state);
if (reader.Read())
{
    // ... use reader ...
    position += reader.BytesConsumed;
    state = reader.CurrentState;
}
```

With `isFinalBlock: false`, an incomplete trailing record makes `Read()` return `false` without consuming it, so a chunked source can append more bytes and try again.

## Write records

<xref:Bodu.IO.Biff.BiffWriter> emits to any `IBufferWriter<byte>` under an explicit version. Typed writers produce the version's layout; `WriteRecord` writes anything from an identifier and payload; `WriteSst` splits the table into `CONTINUE` records by the format's rules.

```csharp
using System.Buffers;
using Bodu.IO.Biff;

var output = new ArrayBufferWriter<byte>();
var writer = new BiffWriter(output, BiffVersion.Biff8);

writer.WriteBof(BiffSubstreamType.WorkbookGlobals);
writer.WriteCodePage(1200);
writer.WriteSst(["alpha", "beta"]);
long boundSheetOffset = writer.BytesCommitted;            // patch the sheet offset here once it is known
writer.WriteBoundSheet(0, BiffSheetState.Visible, BiffSheetType.Worksheet, "Sheet1");
writer.WriteEof();

long sheetOffset = writer.BytesCommitted;
writer.WriteBof(BiffSubstreamType.Worksheet);
writer.WriteDimensions(new BiffDimensionsRecord(0, 1, 0, 3));
writer.WriteLabelSst(0, 0, 0, 1);
writer.WriteNumber(0, 1, 0, 42.5);
if (BiffRk.TryEncode(0.25, out uint rk))
    writer.WriteRk(0, 2, 0, rk);
writer.WriteEof();

byte[] stream = output.WrittenSpan.ToArray();
```

The writer serializes records; assembling a workbook — ordering, the `BOUNDSHEET` offsets, the compound-file container — is the caller's job, which is why `BytesCommitted` is exposed.

## Copy a stream, preserving everything

Because unknown records are readable and writable raw, a stream can be transformed without understanding all of it:

```csharp
using System.Buffers;
using Bodu.IO.Biff;

var output = new ArrayBufferWriter<byte>();
var writer = new BiffWriter(output, BiffVersion.Biff8);
var reader = new BiffReader(bytes);

while (reader.Read())
{
    if (reader.RecordType == BiffRecordType.Number && reader.GetNumber().Value < 0)
        continue;                                          // drop negative numbers, keep everything else verbatim

    writer.WriteRecord(reader.RecordId, reader.ValueSpan);
}
```

## Handle malformed input

```csharp
using Bodu.IO.Biff;

try
{
    var reader = new BiffReader(bytes);
    while (reader.Read())
    {
        // ...
    }
}
catch (BiffUnsupportedVersionException ex)
{
    Console.WriteLine($"Well-formed but too old: version marker 0x{ex.RawVersion:X4}.");
}
catch (BiffFormatException ex)
{
    Console.WriteLine($"Malformed at offset {ex.Offset}: {ex.Message}");
}
```

## Read a workbook instead

For cells with number formats and date detection over a whole `.xls`, use `Bodu.Formats.Excel.Binary`, which opens the container, parses the globals with this codec, and streams each sheet's cells:

```csharp
using Bodu.Formats.Excel;

using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead("report.xls");
Console.WriteLine($"{workbook.BiffVersion}: {workbook.Worksheets.Count} sheets");
```

## Where to go next

- **[Core concepts](concepts.md)** — the full vocabulary.
- **[Runnable sample](../../samples/io-biff.md)** — the `BiffBasics` console project.
- **API reference** — [Bodu.IO.Biff](xref:Bodu.IO.Biff).
