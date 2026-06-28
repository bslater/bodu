---
title: Streaming vs materialized
---

# Streaming vs materialized

A worksheet can be read two ways, and the choice is the main performance decision in the library. This guide contrasts the forward-only <xref:Bodu.Formats.Excel.ExcelWorksheetReader> with the materialized <xref:Bodu.Formats.Excel.ExcelWorksheet>, and shows when to reach for each.

Both surfaces yield the same sparse <xref:Bodu.Formats.Excel.ExcelCell> values — only the access pattern and memory profile differ.

## The two surfaces at a glance

| | <xref:Bodu.Formats.Excel.ExcelWorksheetReader> | <xref:Bodu.Formats.Excel.ExcelWorksheet> |
|---|---|---|
| Obtained from | `workbook.OpenWorksheet(...)` | `workbook.ReadWorksheet(...)` |
| Access | Forward-only, record order | Random, by `(row, column)` |
| Memory | One cell at a time | Whole sheet buffered |
| Surface | `TryReadCell`, `ReadCells`, `ReadRows` | `TryGetCell`, `Cells`, `Rows` |
| Reach for | A single pass over a large sheet | Lookups and revisiting cells |

## Pattern 1 — stream cells (lowest allocation)

```csharp
using Bodu.Formats.Excel;

using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead("rates.xls");
using ExcelWorksheetReader reader = workbook.OpenWorksheet("Data");

while (reader.TryReadCell(out ExcelCell cell))
    Accumulate(cell);
```

<xref:Bodu.Formats.Excel.ExcelWorksheetReader.TryReadCell(Bodu.Formats.Excel.ExcelCell@)> decodes one cell at a time in record order, without building an intermediate map. The reader is <xref:System.IDisposable> — dispose it (the `using` declaration does) before opening the next sheet. <xref:Bodu.Formats.Excel.ExcelWorksheetReader.ReadCells> wraps the same loop as a lazy `IEnumerable<ExcelCell>` for LINQ.

> [!NOTE]
> The reader is forward-only and single-pass: `TryReadCell`, `ReadCells`, and `ReadRows` all draw from one shared cursor that advances and never rewinds. Drive a given reader through exactly one of them — once the cursor reaches the worksheet's end it yields nothing further, and there is no reset. To scan a sheet a second time, open a fresh reader with `OpenWorksheet`, or materialise it once with `ReadWorksheet` and revisit the buffered cells.

## Pattern 2 — group cells into rows

```csharp
using Bodu.Formats.Excel;

using ExcelWorksheetReader reader = workbook.OpenWorksheet("Data");

foreach (ExcelRow row in reader.ReadRows())
{
    Console.WriteLine($"row {row.RowIndex}: {row.Cells.Count} cell(s)");
    foreach (ExcelCell cell in row.Cells)
        Console.Write($"  c{cell.ColumnIndex}");
    Console.WriteLine();
}
```

<xref:Bodu.Formats.Excel.ExcelWorksheetReader.ReadRows> groups consecutive cells into an <xref:Bodu.Formats.Excel.ExcelRow> as their row index advances. Because BIFF8 writes cells in row-major order, this groups a producer's rows without buffering the whole sheet. Rows with no populated cell are not materialized, so the sequence is sparse.

## Pattern 3 — random access by position

```csharp
using Bodu.Formats.Excel;

using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead("rates.xls");

ExcelWorksheet sheet = workbook.ReadWorksheet("Data");

if (sheet.TryGetCell(10, 1, out ExcelCell seriesId))
    Console.WriteLine(seriesId.StringValue);

Console.WriteLine($"{sheet.Cells.Count} populated cells across {sheet.Rows.Count} rows");
```

<xref:Bodu.Formats.Excel.ExcelBinaryWorkbook.ReadWorksheet(System.String)> reads the whole sheet once and returns an <xref:Bodu.Formats.Excel.ExcelWorksheet> that exposes cells by position through `TryGetCell`, all populated cells in row-major order through `Cells`, and grouped `Rows`. `TryGetCell` returns `false` for an absent (blank) cell. Reach for this surface when you need to look cells up, cross-reference columns, or revisit the sheet — at the cost of holding it in memory.

## Pattern 4 — read several sheets independently

```csharp
using Bodu.Formats.Excel;

using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead("rates.xls");

using ExcelWorksheetReader data = workbook.OpenWorksheet("Data");
using ExcelWorksheetReader notes = workbook.OpenWorksheet("Notes");

// Each reader seeks to its own sheet's substream and advances independently.
data.TryReadCell(out _);
notes.TryReadCell(out _);
```

Opening a sheet seeks to its recorded byte offset, so readers from the same workbook are independent. Address a sheet by name or by zero-based index — both `OpenWorksheet` and `ReadWorksheet` accept either; an unknown name throws <xref:System.Collections.Generic.KeyNotFoundException> and an out-of-range index throws <xref:System.ArgumentOutOfRangeException>.

## Choosing a surface

- **Reach for the streaming reader** when you scan a sheet once — aggregations, exports, row-by-row transforms over a large worksheet — and want to bound allocation to a single cell.
- **Reach for the materialized worksheet** when you need random access — looking up a cell by position, cross-referencing columns, or reading the same sheet more than once.

## Tuning large-workbook reads

On a small `.xls` the choice of surface barely matters; on a workbook with
hundreds of thousands of cells it is the difference between a bounded read and
holding the whole sheet in memory. The levers below all bound the working set
without changing the values you read.

**Prefer the streaming reader.** <xref:Bodu.Formats.Excel.ExcelWorksheetReader>
decodes one <xref:Bodu.Formats.Excel.ExcelCell> at a time in record order, so its
allocation is a single cell regardless of sheet size. The materialized
<xref:Bodu.Formats.Excel.ExcelWorksheet> buffers every populated cell, so it
scales with the sheet — use it only when you genuinely need random access, and
let it fall out of scope as soon as the lookups are done.

**Read one sheet at a time, and dispose between sheets.** Each reader seeks to its
own substream and holds decode state until disposed. Open, drain, and dispose one
reader before opening the next rather than holding several open across a workbook:

```csharp
using Bodu.Formats.Excel;

using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead("big.xls");

foreach (ExcelWorksheetInfo sheet in workbook.Worksheets)
{
    using ExcelWorksheetReader reader = workbook.OpenWorksheet(sheet.Index);
    while (reader.TryReadCell(out ExcelCell cell))
        Accumulate(cell);
}   // each reader's decode state is released before the next opens
```

**Skip the metadata you do not use.** Pass an
<xref:Bodu.Formats.Excel.ExcelBinaryReaderOptions> that clears the work you do
not need — `ReadDocumentProperties = false` skips the summary-information
property sets at open time, and `DetectDateFormats = false` skips the
number-format classification on every numeric cell (leaving
`ExcelCell.IsDateFormatted` always `false`):

```csharp
var options = new ExcelBinaryReaderOptions
{
    ReadDocumentProperties = false,   // no summary-information parse
    DetectDateFormats      = false,   // no per-cell number-format lookup
};

using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead("big.xls", options);
```

**Avoid full materialization for one-pass work.** `ReadCells` and `ReadRows`
stream lazily; `ReadRows` groups consecutive cells into an
<xref:Bodu.Formats.Excel.ExcelRow> as the row index advances without buffering the
whole sheet, because BIFF8 writes cells in row-major order. Reach for
`ReadWorksheet` (which buffers) only when you must revisit cells.

| Goal | Do | Avoid |
|---|---|---|
| Bound memory to one cell | `OpenWorksheet` + `TryReadCell` / `ReadCells` | `ReadWorksheet` |
| Process row by row | `ReadRows` | materializing then iterating `Rows` |
| Numeric / time-series only | `ReadDocumentProperties = false`, `DetectDateFormats = false` | default options |
| Several sheets | one reader at a time, disposed between | holding many readers open |
| Random lookups (small sheet) | `ReadWorksheet` + `TryGetCell` | streaming and re-scanning |

## Where to go next

- [Cell values and dates](cell-values-and-dates.md) — interpret the `ExcelCell` values both surfaces yield.
- [Reading workbooks](reading-workbooks.md) — the open path and reader options.
- [Bodu.Formats.Excel API reference](xref:Bodu.Formats.Excel).
