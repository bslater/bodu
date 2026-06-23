---
uid: Bodu.Formats.Excel
---

![Bodu.Formats.Excel.Binary](~/images/hero-excel.svg)

## Purpose

**Bodu.Formats.Excel.Binary** is a narrow, read-only reader for the Excel 97–2003 binary workbook format (BIFF8 / `.xls`). It surfaces the raw cell values of each worksheet — strings, numbers, booleans, and errors, including a formula cell's cached result — along with each cell's number format and date-format detection, the workbook date system, each sheet's declared used range, and the workbook document properties. It performs no formula evaluation, styling, or higher-level interpretation.

An `.xls` file is a BIFF8 record stream stored inside the `Workbook` stream of an OLE2 compound file. This package reads that record stream; the container around it is read by <xref:Bodu.IO.Compound.CompoundFile>, on which the package is built. The assembly and package keep the `.Binary` suffix, but the public namespace is flattened to `Bodu.Formats.Excel` so a future Excel-format package can share the value model.

## Static documentation

- **[Introduction](~/docs/excel/index.md)** — the headline types, the layered shape, and the scenarios the reader covers.
- **[Core concepts](~/docs/excel/concepts.md)** — BIFF8 record, workbook globals, shared string table, cell kind, serial date, and used range.
- **[Getting started](~/docs/excel/getting-started.md)** — install and minimal samples for opening, listing, and reading cells.
- **[Reading workbooks](~/guides/excel/reading-workbooks.md)** — the open path, sheet listing, reader options, and document properties.
- **[Cell values and dates](~/guides/excel/cell-values-and-dates.md)** — cell kinds, cached formula results, date detection, serial-date and A1 conversion.
- **[Streaming vs materialized](~/guides/excel/worksheets-and-rows.md)** — the forward-only reader versus the randomly addressable worksheet.

## Key types

**Workbook session**

- <xref:Bodu.Formats.Excel.ExcelBinaryWorkbook> — the disposable read-only session. `OpenRead` (path / `FileInfo` / `Stream`) and `Open(Stream, options)` factories, `Worksheets`, `Properties`, `DateSystem`, the `OpenWorksheet` / `ReadWorksheet` surfaces, and the `GetDateTime` / `GetNumberFormatCode` helpers.
- <xref:Bodu.Formats.Excel.ExcelBinaryReaderOptions> — `LeaveOpen`, `ReadDocumentProperties`, `DetectDateFormats`; trades optional metadata work for throughput and governs stream ownership.
- <xref:Bodu.Formats.Excel.ExcelWorkbookProperties> — the flattened document fields read from the OLE summary-information streams.

**Cell surfaces**

- <xref:Bodu.Formats.Excel.ExcelWorksheetReader> — the forward-only, low-allocation reader; `TryReadCell`, `ReadCells`, `ReadRows`.
- <xref:Bodu.Formats.Excel.ExcelWorksheet> / <xref:Bodu.Formats.Excel.ExcelRow> — the materialized, randomly addressable worksheet (`TryGetCell`, `Cells`, `Rows`) and its grouped rows.

**Value model**

- <xref:Bodu.Formats.Excel.ExcelCell> — the immutable cell value: `RowIndex`, `ColumnIndex`, `Kind`, the typed value projection, `FormatIndex`, and `IsDateFormatted`.
- <xref:Bodu.Formats.Excel.ExcelCellKind>, <xref:Bodu.Formats.Excel.ExcelErrorCode> — the cell classification and the BIFF8 spreadsheet error codes.
- <xref:Bodu.Formats.Excel.ExcelWorksheetInfo>, <xref:Bodu.Formats.Excel.ExcelWorksheetDimensions>, <xref:Bodu.Formats.Excel.ExcelSheetVisibility>, <xref:Bodu.Formats.Excel.ExcelSheetType> — the sheet descriptor, its declared used range, visibility, and type.

**Helpers**

- <xref:Bodu.Formats.Excel.ExcelSerialDate>, <xref:Bodu.Formats.Excel.ExcelDateSystem> — serial-date conversion and the 1900 / 1904 date systems.
- <xref:Bodu.Formats.Excel.ExcelCellReference> — A1 reference conversion (`ColumnName`, `ToA1`, `TryParseA1`).

**Errors**

- <xref:Bodu.Formats.Excel.ExcelBinaryFormatException> (malformed record), <xref:Bodu.Formats.Excel.ExcelBinaryWorkbookStreamNotFoundException> (no workbook stream), <xref:Bodu.Formats.Excel.ExcelBinaryUnsupportedException> (non-BIFF8 version), <xref:Bodu.Formats.Excel.ExcelBinaryEncryptedWorkbookException> (encrypted).

## Example

```csharp
using Bodu.Formats.Excel;

using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead("rates.xls");

foreach (ExcelWorksheetInfo sheet in workbook.Worksheets)
    Console.WriteLine($"{sheet.Index}: {sheet.Name} ({sheet.Dimensions.RowCount} rows)");

using ExcelWorksheetReader reader = workbook.OpenWorksheet("Data");
while (reader.TryReadCell(out ExcelCell cell))
{
    string a1 = ExcelCellReference.ToA1(cell.RowIndex, cell.ColumnIndex);
    if (cell.Kind == ExcelCellKind.Number && cell.IsDateFormatted)
        Console.WriteLine($"{a1} = {ExcelSerialDate.FromSerialDate(cell.NumberValue!.Value, workbook.DateSystem)}");
    else if (cell.Kind == ExcelCellKind.String)
        Console.WriteLine($"{a1} = {cell.StringValue}");
}
```

## Notes

- **Read-only.** The reader surfaces values; it never writes, evaluates formulas, applies styles, or interprets charts and macros. A formula cell yields its *cached* result — the value Excel last stored — surfaced as whichever kind that value holds.
- **Layered stack.** The package reads only the BIFF8 records; the OLE2 container around them is read by <xref:Bodu.IO.Compound>. A consumer that needs only the container depends on `Bodu.IO.Compound` alone. The Reserve Bank of Australia exchange-rate provider (`Bodu.Financial.ExchangeRates.Rba`) parses the same `.xls` shape on top of this reader.
- **Sparse surfaces.** Blank cells are never returned, so both the streaming reader and the materialized worksheet are sparse. The streaming <xref:Bodu.Formats.Excel.ExcelWorksheetReader> bounds allocation to one cell; the materialized <xref:Bodu.Formats.Excel.ExcelWorksheet> buffers the whole sheet for random access.
- **Dates are numbers.** Excel stores dates as floating-point serial numbers. The reader never reinterprets a number, but flags date-formatted cells via <xref:Bodu.Formats.Excel.ExcelCell.IsDateFormatted> and offers <xref:Bodu.Formats.Excel.ExcelSerialDate> for conversion against the workbook's 1900 or 1904 date system.
- **Errors.** Malformed records surface through <xref:Bodu.Formats.Excel.ExcelBinaryFormatException>; a missing workbook stream, a non-BIFF8 version, and an encrypted workbook through their dedicated exceptions.
- **See also:** the [introduction](~/docs/excel/index.md), [core concepts](~/docs/excel/concepts.md), and [getting-started](~/docs/excel/getting-started.md); the [Binary Formats & I/O topic](~/docs/topics/binary-formats.md); and the container reader [Bodu.IO.Compound](xref:Bodu.IO.Compound) beneath it.
```
