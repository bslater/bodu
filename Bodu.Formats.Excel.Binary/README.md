# Bodu.Formats.Excel.Binary

A narrow, read-only reader for the **Excel 97-2003 binary workbook format** (BIFF8 /
`.xls`). It exposes the raw cell values of each worksheet — strings, numbers, booleans,
and errors — and nothing more. There is no formula evaluation, styling, charting, or
date inference; turning a numeric cell into a date or a column into a record is the
caller's job.

Built on [`Bodu.IO.Compound`](../Bodu.IO.Compound) for the underlying container.

```csharp
using Bodu.Formats.Excel.Binary;

Biff8WorkbookReader workbook = Biff8WorkbookReader.Open(stream);

foreach (Biff8SheetInfo sheet in workbook.Sheets)
    Console.WriteLine($"{sheet.Index}: {sheet.Name} (visible: {sheet.IsVisible})");

foreach (ExcelCell cell in workbook.ReadSheetCells("Data"))
{
    switch (cell.Kind)
    {
        case ExcelCellKind.String: /* cell.StringValue */ break;
        case ExcelCellKind.Number: /* cell.NumberValue */ break;
        case ExcelCellKind.Boolean: /* cell.BooleanValue */ break;
    }
}

// A caller that knows a column holds dates can convert a numeric cell:
DateOnly date = ExcelSerialDate.FromSerialDate(cell.NumberValue!.Value);
```

## Records handled

`BOF` / `EOF` (substream boundaries), `BOUNDSHEET` (sheet directory), `SST` (+ `CONTINUE`
for split strings), `LABELSST`, `NUMBER`, `RK`, `MULRK`, `BOOLERR`, and `BLANK` /
`MULBLANK` (skipped). Unrecognized records are skipped.

## Out of scope

Formulas, styles, charts, pivot tables, macros, named ranges, and writing `.xls` files.

Part of the [Bodu](https://github.com/bodu/bodu) utility library.
