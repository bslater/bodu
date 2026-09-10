---
uid: Bodu.IO.Biff
---

![Bodu.IO.Biff](~/images/hero-io-biff.svg)

## Purpose

**Bodu.IO.Biff** is a low-level codec for the Excel Binary Interchange File Format — the BIFF5 (Excel 5.0/95) and BIFF8 (Excel 97–2003) record streams stored inside legacy `.xls` workbooks. It is the substrate beneath `Bodu.Formats.Excel.Binary`, in the same relation <xref:Bodu.IO.Pst> has to the Outlook mail-store reader: it understands how BIFF is encoded, not what a workbook means. There is no compound-file dependency, no workbook or cell model, and no formula evaluation.

<xref:Bodu.IO.Biff.BiffReader> walks a record stream forward over a span, one physical record at a time, establishing the version from the first `BOF` record and the code page from `CODEPAGE`, exposing every record's identifier and raw payload, and decoding the common structural and cell records through typed accessors. <xref:Bodu.IO.Biff.BiffSstReader> walks the BIFF8 shared string table across its `CONTINUE` records. <xref:Bodu.IO.Biff.BiffWriter> emits records under an explicit version, from typed values or raw payloads, splitting oversized structures into `CONTINUE` records by the format's rules.

## Static documentation

- **[Introduction](~/docs/io-biff/index.md)** — the headline types, the layering between the container and the Excel reader, and the scenarios the codec covers.
- **[Core concepts](~/docs/io-biff/concepts.md)** — records, versions, substreams, continuation, strings and code pages, the shared string table, RK numbers, resumption.
- **[Record reference](~/docs/io-biff/records.md)** — every named record with its identifier, BIFF5 and BIFF8 layouts, decoded type, reader accessor, and writer method.
- **[Getting started](~/docs/io-biff/getting-started.md)** — install and minimal samples for reading, decoding, resuming, and writing.
- **[Binary Formats & I/O topic overview](~/docs/topics/binary-formats.md)** — where the codec sits beneath the format readers.

## Key types

**Reader**

- <xref:Bodu.IO.Biff.BiffReader> — the forward-only `ref struct` reader. `Read` advances to the next physical record; `RecordId` / `RecordType` / `RecordLength` / `ValueSpan` / `Header` describe it (unknown identifiers are never an error); `Version` and `CodePage` are the state the stream established; `TryReadContinuation` consumes a following `CONTINUE`; `CurrentState` / `BytesConsumed` with the `(data, isFinalBlock, state)` constructor resume across buffers. Typed accessors: `GetBof`, `GetBoundSheet`, `GetDimensions`, `GetRow`, `GetNumber`, `GetRk`, `GetMulRk`, `GetLabel`, `GetLabelSst`, `GetRString`, `GetBoolErr`, `GetBlank`, `GetMulBlank`, `GetFormula`, `GetString`, `GetXf`, `GetFormat`, `GetFont`, `GetCodePage`, `GetDateMode`, `GetFilePass`, `GetSstHeader`.
- <xref:Bodu.IO.Biff.BiffReaderOptions> / <xref:Bodu.IO.Biff.BiffReaderState> — seed a known version and code page; carry the established state between readers.
- <xref:Bodu.IO.Biff.BiffSstReader> — the shared-string-table walker: `Header`, `Read(ref reader)`, `Index`, `Length`, `IsFragmented`, `Current` (span-backed view when contiguous), `GetString` / `CopyTo` for every string.

**Records**

- The decoded record types — <xref:Bodu.IO.Biff.BiffBofRecord>, <xref:Bodu.IO.Biff.BiffBoundSheetRecord>, <xref:Bodu.IO.Biff.BiffDimensionsRecord>, <xref:Bodu.IO.Biff.BiffRowRecord>, <xref:Bodu.IO.Biff.BiffNumberRecord>, <xref:Bodu.IO.Biff.BiffRkRecord>, <xref:Bodu.IO.Biff.BiffMulRkRecord> (with <xref:Bodu.IO.Biff.BiffRkCell>), <xref:Bodu.IO.Biff.BiffLabelRecord>, <xref:Bodu.IO.Biff.BiffLabelSstRecord>, <xref:Bodu.IO.Biff.BiffRStringRecord>, <xref:Bodu.IO.Biff.BiffBoolErrRecord>, <xref:Bodu.IO.Biff.BiffBlankRecord>, <xref:Bodu.IO.Biff.BiffMulBlankRecord>, <xref:Bodu.IO.Biff.BiffFormulaRecord> (with <xref:Bodu.IO.Biff.BiffCachedResultKind>), <xref:Bodu.IO.Biff.BiffStringRecord>, <xref:Bodu.IO.Biff.BiffXfRecord>, <xref:Bodu.IO.Biff.BiffFormatRecord>, <xref:Bodu.IO.Biff.BiffFontRecord>, <xref:Bodu.IO.Biff.BiffCodePageRecord>, <xref:Bodu.IO.Biff.BiffDateModeRecord>, <xref:Bodu.IO.Biff.BiffFilePassRecord> (with <xref:Bodu.IO.Biff.BiffEncryptionType>), <xref:Bodu.IO.Biff.BiffSstHeader>. Records carrying text or spans are `ref struct`s over the payload; the rest are `record struct`s.
- <xref:Bodu.IO.Biff.BiffString> — the span-backed text view: `Length`, `IsUnicode` / `IsHighByte`, `RawCharacters`, `RichRuns` / `ExtendedData`, `CodePage`, `EncodedLength`, `GetCharCount` / `GetString` / `CopyTo`.
- <xref:Bodu.IO.Biff.BiffRk> — `Decode` and `TryEncode` for the RK number representation.

**Writer**

- <xref:Bodu.IO.Biff.BiffWriter> — the `ref struct` writer over `IBufferWriter<byte>`: `WriteRecord` (raw, per-version maximum enforced), `WriteContinuedRecord`, `WriteBof` / `WriteEof`, `WriteCodePage` / `WriteDateMode`, `WriteBoundSheet` / `WriteDimensions` / `WriteRow`, the cell writers (`WriteNumber`, `WriteRk`, `WriteMulRk`, `WriteBlank`, `WriteMulBlank`, `WriteBoolean` / `WriteError` / `WriteBoolErr`, `WriteLabel`, `WriteLabelSst`, `WriteFormula`, `WriteString`), `WriteXf` / `WriteFormat` / `WriteFont`, and `WriteSst` with `CONTINUE` splitting; `BytesCommitted` and `OpenSubstreamDepth`.
- <xref:Bodu.IO.Biff.BiffWriterOptions> — the version and the BIFF5 code page.

**Vocabulary and limits**

- <xref:Bodu.IO.Biff.BiffRecordType> / <xref:Bodu.IO.Biff.BiffVersion> / <xref:Bodu.IO.Biff.BiffSubstreamType> / <xref:Bodu.IO.Biff.BiffSheetState> / <xref:Bodu.IO.Biff.BiffSheetType> — the named identifiers, the two versions, the substream kinds, and the bound-sheet enumerations.
- <xref:Bodu.IO.Biff.BiffRecordHeader> — the four-byte header with `TryParse` / `WriteTo`; <xref:Bodu.IO.Biff.BiffLimits> — header size, per-version maximum payload, default code page.

**Errors**

- <xref:Bodu.IO.Biff.BiffFormatException> (invalid data, with the record `Offset`), <xref:Bodu.IO.Biff.BiffUnsupportedVersionException> (a well-formed stream in a version the codec does not process, with the `RawVersion` marker).

## Example

```csharp
using Bodu.IO.Biff;

var reader = new BiffReader(workbookStreamBytes);
string[] sharedStrings = [];

while (reader.Read())
{
    switch (reader.RecordType)
    {
        case BiffRecordType.Sst:
            var strings = new BiffSstReader(ref reader);
            var list = new List<string>();
            while (strings.Read(ref reader))
                list.Add(strings.GetString());
            sharedStrings = [.. list];
            break;

        case BiffRecordType.LabelSst:
            BiffLabelSstRecord label = reader.GetLabelSst();
            Console.WriteLine($"R{label.Row}C{label.Column}: {sharedStrings[label.SstIndex]}");
            break;

        case BiffRecordType.Number:
            BiffNumberRecord number = reader.GetNumber();
            Console.WriteLine($"R{number.Row}C{number.Column}: {number.Value}");
            break;
    }
}
```
