# Bodu.Formats.Excel.Binary

> **API stability — Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

A narrow, read-only reader for the **Excel 97-2003 binary workbook format** (BIFF8 /
`.xls`). It exposes the raw cell values of each worksheet — strings, numbers, booleans,
and errors — and nothing more. There is no formula evaluation, styling, charting, or
date inference; turning a numeric cell into a date or a column into a record is the
caller's job.

Built on [`Bodu.IO.Compound`](../Bodu.IO.Compound) for the underlying container.

A workbook is opened as a disposable, read-only session that keeps the container open and
reads each sheet on demand by seeking to the stream offset its bound-sheet record records,
so a single sheet can be read without parsing the others and the whole workbook is never
materialized. The primary surface is the forward-only `ExcelWorksheetReader`; a
materialized, randomly addressable `ExcelWorksheet` is offered as a convenience.

```csharp
using Bodu.Formats.Excel;

using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead(path);

foreach (ExcelWorksheetInfo sheet in workbook.Worksheets)
    Console.WriteLine($"{sheet.Index}: {sheet.Name} ({sheet.Visibility}, {sheet.Type})");

// Primary, low-allocation forward scan.
using ExcelWorksheetReader reader = workbook.OpenWorksheet("Data");
while (reader.TryReadCell(out ExcelCell cell))
{
    switch (cell.Kind)
    {
        case ExcelCellKind.String: /* cell.StringValue */ break;
        case ExcelCellKind.Number: /* cell.NumberValue */ break;
        case ExcelCellKind.Boolean: /* cell.BooleanValue */ break;
    }
}

// Convenience, materialized random access.
ExcelWorksheet data = workbook.ReadWorksheet("Data");
if (data.TryGetCell(11, 0, out ExcelCell rate)) { /* ... */ }

// A caller that knows a column holds dates can convert a numeric cell:
DateOnly date = ExcelSerialDate.FromSerialDate(rate.NumberValue!.Value);
```

For a numeric, time-series workload, `ExcelBinaryReaderOptions` can skip optional metadata
work:

```csharp
using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.Open(stream, new ExcelBinaryReaderOptions
{
    ReadDocumentProperties = false,
    DetectDateFormats = false,
});
```

## Records handled

`BOF` / `EOF` (substream boundaries), `BOUNDSHEET8` (sheet directory, with `lbPlyPos`
offset, sheet type, and hidden state), `SST` (+ `CONTINUE` for split strings), `LABELSST`,
`LABEL`, `NUMBER`, `RK`, `MULRK`, `BOOLERR`, `FORMULA` (cached result, with a trailing
`STRING` record for a string result), `XF` / `FORMAT` (number formats), `DATEMODE`, and
`DIMENSIONS`. `BLANK` / `MULBLANK` and unrecognized records are skipped. Malformed records
fail with `ExcelBinaryFormatException`.

## Runnable samples

The repository ships an offline, `dotnet run`-able sample for this package — the workbook
session and sheet directory, forward-only streaming over ~18,000 cells, the materialized
worksheet surface, and format-classified serial-date decoding against a real committed
fixture — under
[`samples/Formats.Excel/`](https://github.com/bslater/bodu/tree/master/samples/Formats.Excel).

## Out of scope

Formulas, styles, charts, pivot tables, macros, named ranges, and writing `.xls` files.

Part of the [Bodu](https://github.com/bodu/bodu) utility library.
