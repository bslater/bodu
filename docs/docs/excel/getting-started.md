---
title: Bodu.Formats.Excel.Binary — Getting started
---

# Bodu.Formats.Excel.Binary — Getting started

Unfamiliar with terms like *BIFF8*, *workbook globals*, *cell kind*, *serial date*, or *used range*? Read [Core concepts](concepts.md) first.

## Install

```bash
dotnet add package Bodu.Formats.Excel.Binary
```

Targets `net8.0`. Depends on `Bodu.IO.Compound` (the container reader), which depends only on `Bodu.Core` — no other NuGet references.

## Open a workbook and list its sheets

```csharp
using Bodu.Formats.Excel;

using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead("rates.xls");

foreach (ExcelWorksheetInfo sheet in workbook.Worksheets)
    Console.WriteLine($"{sheet.Index}: {sheet.Name} — {sheet.Dimensions.RowCount} × {sheet.Dimensions.ColumnCount}");
```

`OpenRead` parses the workbook globals — the date system, shared strings, number formats, and sheet directory — and lists the sheets without reading any of their cells. The returned workbook is <xref:System.IDisposable>; the `using` declaration disposes it and closes the source unless `leaveOpen: true` was passed.

## Stream a sheet's cells

```csharp
using Bodu.Formats.Excel;

using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead("rates.xls");
using ExcelWorksheetReader reader = workbook.OpenWorksheet("Data");

while (reader.TryReadCell(out ExcelCell cell))
{
    string a1 = ExcelCellReference.ToA1(cell.RowIndex, cell.ColumnIndex);
    object? value = cell.Kind switch
    {
        ExcelCellKind.String  => cell.StringValue,
        ExcelCellKind.Number  => cell.NumberValue,
        ExcelCellKind.Boolean => cell.BooleanValue,
        ExcelCellKind.Error   => cell.ErrorValue,
        _ => null,
    };

    Console.WriteLine($"{a1} [{cell.Kind}] = {value}");
}
```

<xref:Bodu.Formats.Excel.ExcelWorksheetReader> is the forward-only, low-allocation surface. Only populated cells are returned, in record order, so the sequence is sparse. Each <xref:Bodu.Formats.Excel.ExcelCell> carries the value matching its <xref:Bodu.Formats.Excel.ExcelCellKind>; the other projections are `null`.

## Group cells into rows

```csharp
using Bodu.Formats.Excel;

using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead("rates.xls");
using ExcelWorksheetReader reader = workbook.OpenWorksheet(0);

foreach (ExcelRow row in reader.ReadRows())
    Console.WriteLine($"row {row.RowIndex}: {row.Cells.Count} cell(s)");
```

`ReadRows` groups consecutive cells by row as they are read, without buffering the whole sheet.

## Read a sheet by position

```csharp
using Bodu.Formats.Excel;

using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead("rates.xls");

ExcelWorksheet sheet = workbook.ReadWorksheet("Data");
if (sheet.TryGetCell(10, 1, out ExcelCell cell))
    Console.WriteLine(cell.StringValue);
```

<xref:Bodu.Formats.Excel.ExcelWorksheet> materializes every populated cell once and exposes them by position through `TryGetCell` and grouped into `Rows`. Prefer it for random access; prefer the streaming reader when you scan a sheet once and want to bound allocation.

## Convert serial-date cells

```csharp
using Bodu.Formats.Excel;

using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead("rates.xls");
using ExcelWorksheetReader reader = workbook.OpenWorksheet("Data");

while (reader.TryReadCell(out ExcelCell cell))
{
    if (cell.Kind == ExcelCellKind.Number && cell.IsDateFormatted)
    {
        DateOnly date = ExcelSerialDate.FromSerialDate(cell.NumberValue!.Value, workbook.DateSystem);
        Console.WriteLine(date);
    }
}
```

The reader never assumes a number is a date, but it flags numeric cells whose format is a date or time format via <xref:Bodu.Formats.Excel.ExcelCell.IsDateFormatted>. <xref:Bodu.Formats.Excel.ExcelSerialDate> converts the serial number using the workbook's <xref:Bodu.Formats.Excel.ExcelDateSystem>. The workbook also offers `GetDateTime(cell)` as a shortcut that returns `null` for non-date cells.

## Read document metadata, or skip it for throughput

```csharp
using Bodu.Formats.Excel;

// Read the OLE summary-information streams (the default).
using (ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead("report.xls"))
{
    Console.WriteLine(workbook.Properties.Title);
    Console.WriteLine(workbook.Properties.Author);
}

// Or skip the optional metadata work for a pure numeric, time-series read.
var options = new ExcelBinaryReaderOptions { ReadDocumentProperties = false, DetectDateFormats = false };
using (ExcelBinaryWorkbook fast = ExcelBinaryWorkbook.Open(File.OpenRead("rates.xls"), options))
{
    // ... read cells ...
}
```

<xref:Bodu.Formats.Excel.ExcelBinaryReaderOptions> governs ownership and optional work: clearing `ReadDocumentProperties` skips the property-set parse (and yields an empty <xref:Bodu.Formats.Excel.ExcelWorkbookProperties>), and clearing `DetectDateFormats` skips the date-format classification (leaving `IsDateFormatted` always `false`).

## Handle the failure cases

```csharp
using Bodu.Formats.Excel;

try
{
    using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead(path);
    // ...
}
catch (ExcelBinaryWorkbookStreamNotFoundException)
{
    // A valid compound file, but not a spreadsheet (no Workbook/Book stream).
}
catch (ExcelBinaryEncryptedWorkbookException)
{
    // Password-protected; this reader does not decrypt.
}
catch (ExcelBinaryUnsupportedException)
{
    // An earlier BIFF version this reader does not target.
}
```

## Where to go next

- **[Core concepts](concepts.md)** — the vocabulary behind these samples.
- **[Introduction](index.md)** — headline types and common scenarios.
- **[Bodu.Formats.Excel.Binary guides](../../guides/excel/index.md)** — reading workbooks, cell values and dates, and the streaming-vs-materialized surfaces.
- **API reference** — [Bodu.Formats.Excel](xref:Bodu.Formats.Excel).
