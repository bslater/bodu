---
title: Reading workbooks
---

# Reading workbooks

<xref:Bodu.Formats.Excel.ExcelBinaryWorkbook> opens an Excel 97–2003 binary workbook (`.xls`) and exposes its sheets and cell values. This guide covers the open path: open from a path or stream, list the sheets, govern ownership and optional work, and read the authored document properties.

The mental model is a disposable session over the container. Opening parses the workbook globals — the date system, the shared string table, the number-format table, and the sheet directory — once, then reads each sheet on demand.

## Pattern 1 — open from a path

<!-- compile -->
```csharp
using Bodu.Formats.Excel;

using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead("rates.xls");

Console.WriteLine($"{workbook.Worksheets.Count} sheet(s), {workbook.DateSystem} date system");
```

`OpenRead` opens the file, verifies it is a BIFF8 compound file, and parses the globals. The returned workbook is <xref:System.IDisposable>; the `using` declaration disposes it and closes the underlying file. A <xref:Bodu.Formats.Excel.ExcelBinaryWorkbook.OpenRead(System.IO.FileInfo)> overload accepts a <xref:System.IO.FileInfo> when you already have one in hand and want the same path-owning behaviour.

> [!IMPORTANT]
> The workbook owns the open compound-file container and seeks back into it every time you open a sheet. Keep the workbook alive for as long as you read its sheets, and do not dispose it until every <xref:Bodu.Formats.Excel.ExcelWorksheetReader> over it is finished — a reader returned by `OpenWorksheet` decodes from a buffer it captured at open, but `OpenWorksheet` / `ReadWorksheet` themselves seek into the live container and throw <xref:System.ObjectDisposedException> after the workbook is disposed.

## Pattern 2 — open from a stream you own

```csharp
using Bodu.Formats.Excel;

using Stream source = await httpClient.GetStreamAsync(uri);
using MemoryStream seekable = new();
await source.CopyToAsync(seekable);
seekable.Position = 0;

using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead(seekable, leaveOpen: true);
```

The stream overload reads from the stream's current position. Pass `leaveOpen: true` to keep a caller-owned stream open after the workbook is disposed; the default disposes it with the workbook. The source must be seekable, because the reader seeks to each sheet's recorded offset.

## Pattern 3 — list the sheets and their used ranges

```csharp
using Bodu.Formats.Excel;

using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead("rates.xls");

foreach (ExcelWorksheetInfo sheet in workbook.Worksheets)
{
    ExcelWorksheetDimensions used = sheet.Dimensions;
    Console.WriteLine(
        $"{sheet.Index}: {sheet.Name} " +
        $"[{sheet.Type}, {sheet.Visibility}] " +
        $"rows {used.FirstRowIndex}..{used.FirstRowIndex + used.RowCount}");
}
```

<xref:Bodu.Formats.Excel.ExcelWorksheetInfo> describes each sheet without reading its cells: its `Name`, zero-based `Index`, <xref:Bodu.Formats.Excel.ExcelSheetVisibility>, <xref:Bodu.Formats.Excel.ExcelSheetType>, and the <xref:Bodu.Formats.Excel.ExcelWorksheetDimensions> declared by its `DIMENSIONS` record. The used range is an upper bound on the populated region, not a tight fit. `IsVisible` is a shortcut for `Visibility == ExcelSheetVisibility.Visible`.

Only a <xref:Bodu.Formats.Excel.ExcelSheetType.Worksheet> carries the tabular cell records this reader surfaces. A workbook may also list `Chart`, `MacroSheet`, `VbaModule`, or `Unknown` entries; opening one of those yields no cells, and its `Dimensions` is the zero-count default. Filter by type before reading when you only want data sheets, and skip hidden sheets by visibility if your workflow should ignore them:

```csharp
foreach (ExcelWorksheetInfo sheet in workbook.Worksheets)
{
    if (sheet.Type != ExcelSheetType.Worksheet || sheet.Visibility == ExcelSheetVisibility.VeryHidden)
        continue;

    using ExcelWorksheetReader reader = workbook.OpenWorksheet(sheet.Index);
    // ... read the data sheet ...
}
```

The workbook's declared date system is available up front through <xref:Bodu.Formats.Excel.ExcelBinaryWorkbook.DateSystem> (read from the `DATEMODE` record); pass it to <xref:Bodu.Formats.Excel.ExcelSerialDate> when converting date-formatted cells rather than assuming the 1900 default — see [Cell values and dates](cell-values-and-dates.md).

## Pattern 4 — skip optional work for throughput

<!-- compile -->
```csharp
using Bodu.Formats.Excel;

var options = new ExcelBinaryReaderOptions
{
    ReadDocumentProperties = false,   // skip the OLE summary-information parse
    DetectDateFormats = false,        // skip date-format classification
    LeaveOpen = true,                 // keep the caller's stream open
};

using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.Open(File.OpenRead("rates.xls"), options);
```

<xref:Bodu.Formats.Excel.ExcelBinaryReaderOptions> trades optional metadata work for speed. For a pure numeric, time-series read — where only row, column, and numeric value matter — clearing both flags skips the property-set parse and the number-format interpretation. Use the `Open(stream, options)` overload to supply them; `OpenRead` uses the defaults (read everything, own the stream).

## Pattern 5 — read authored document properties

```csharp
using Bodu.Formats.Excel;

using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead("report.xls");

ExcelWorkbookProperties props = workbook.Properties;
Console.WriteLine(props.Title);
Console.WriteLine(props.Author);
Console.WriteLine(props.Company);
Console.WriteLine(props.LastSaved);
```

<xref:Bodu.Formats.Excel.ExcelWorkbookProperties> flattens the OLE `SummaryInformation` and `DocumentSummaryInformation` streams into nullable document fields. Every member is `null` when the corresponding property set is absent or was not read, so it is safe to read without guarding. When `ReadDocumentProperties` is `false`, the view is the empty instance and every field is `null`.

## Error handling

| Exception | Cause |
|---|---|
| <xref:System.ArgumentNullException> | The path or stream passed to `OpenRead` / `Open` is `null`. |
| <xref:Bodu.IO.Compound.CompoundFileFormatException> | The content is not a well-formed compound file. |
| <xref:Bodu.Formats.Excel.ExcelBinaryWorkbookStreamNotFoundException> | A valid compound file with no `Workbook` (or legacy `Book`) stream — not a spreadsheet. |
| <xref:Bodu.Formats.Excel.ExcelBinaryUnsupportedException> | The workbook declares a non-BIFF8 version. |
| <xref:Bodu.Formats.Excel.ExcelBinaryEncryptedWorkbookException> | The workbook is password-protected (a `FILEPASS` record is present). |
| <xref:Bodu.Formats.Excel.ExcelBinaryFormatException> | A BIFF record is malformed — raised while reading, not at open. |

## Where to go next

- [Cell values and dates](cell-values-and-dates.md) — interpret cell kinds and convert serial dates.
- [Streaming vs materialized](worksheets-and-rows.md) — choose the cell surface that fits your access pattern.
- [Bodu.Formats.Excel API reference](xref:Bodu.Formats.Excel).
