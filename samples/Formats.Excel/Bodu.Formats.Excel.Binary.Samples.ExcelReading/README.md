# Bodu.Formats.Excel.Binary.Samples.ExcelReading

The read-only BIFF8 workbook reader in `Bodu.Formats.Excel.Binary`: opening an `.xls` and
listing its sheets, the forward-only cell reader (the primary surface), the materialized
convenience surface, and decoding cell kinds - including BIFF8's classic importer trap, dates
stored as format-classified numbers. All scenarios run offline against a workbook the sample
writes first, to a temporary file it deletes at the end: `SampleWorkbook` builds a two-sheet
BIFF8 workbook of synthetic daily readings (about 2,400 cells) with the codec's `BiffWriter`
and the container's `CompoundStorageBuilder`, so the sample carries no data file.

> Scope notes: this package reads the Excel **97-2003 binary** format (`.xls`) only - not
> `.xlsx` - and is read-only. Formula cells surface their *cached results*; encrypted
> workbooks throw `ExcelBinaryEncryptedWorkbookException`.

```bash
dotnet run --project samples/Formats.Excel/Bodu.Formats.Excel.Binary.Samples.ExcelReading
```

## Scenario 1 - WorkbookAndSheets

**Intent.** Show the session model: `ExcelBinaryWorkbook` opens the container once and
exposes everything needed to decide *what* to read before reading any cells - the sheet
directory (name, type, visibility, declared used range), the flattened document properties,
and the workbook's declared date system.

**What it does.** Opens the workbook and prints the date system, the summary properties, and
each sheet's `ExcelWorksheetInfo` with its used range rendered in A1 notation via
`ExcelCellReference`.

**What to expect.** Two visible worksheets. The used range is the one each sheet *declares*,
read without loading a cell; a real workbook's declaration can reach past its last populated
cell, which is why it is the sheet's claim rather than a promise:

```text
--- Opening a workbook - sheets, visibility, and used ranges ---

  date system : Excel1900
  properties  : title='Daily readings', author='Bodu samples', app='Bodu sample generator'
  worksheets  : 2
  [0] 'Data' (Worksheet, Visible) used range A1:F404 (404 rows x 6 cols)
  [1] 'Notes' (Worksheet, Visible) used range A1:B5 (5 rows x 2 cols)
```

**APIs demonstrated.** `ExcelBinaryWorkbook.OpenRead(path)` (+ `IDisposable`), `.Worksheets`
/ `ExcelWorksheetInfo`, `.Properties` (`ExcelWorkbookProperties`), `.DateSystem`,
`ExcelWorksheetDimensions`, `ExcelCellReference.ToA1`.

## Scenario 2 - ForwardOnlyReader

**Intent.** Show the primary surface: `ExcelWorksheetReader` streams cells forward-only in
file order from the sheet's records, buffered once, without materializing a cell map - the
right shape for import pipelines that transform data as it arrives rather than loading every
cell to look at each once.

**What it does.** Drives the lowest-level `TryReadCell` loop over the `Data` sheet, counting
and classifying every cell without buffering any; then reopens the sheet and uses `ReadRows`
(the same stream, grouped into rows) to preview the first three rows in A1 notation.

**What to expect.**

```text
--- The forward-only worksheet reader ---

  streaming 'Data'...
  cells read  : 2417 across rows 0..403
  kinds       : String=417, Number=2000
  row 0: A1='Daily readings, four stations'
  row 1: A2='Station' | B2='North' | C2='South' | D2='East'
  row 2: A3='Units' | B3='mm' | C3='mm' | D3='mm'
```

**APIs demonstrated.** `ExcelBinaryWorkbook.OpenWorksheet(index)`,
`ExcelWorksheetReader.TryReadCell` / `.ReadRows` / `.Worksheet`, `ExcelCell.Kind` /
`.RowIndex` / `.ColumnIndex`, `ExcelRow`.

## Scenario 3 - MaterializedWorksheet

**Intent.** Show the convenience surface for when the sheet fits in memory and you need
*random* access: `ReadWorksheet` materializes an `ExcelWorksheet` with indexed rows,
coordinate lookup, and LINQ-friendly collections.

**What it does.** Materializes the `Data` sheet, looks up `A1` by coordinates with
`TryGetCell`, aggregates every non-date numeric cell with LINQ, and finds the widest row -
noting that rows are sparse (only populated rows appear, each holding only its populated
cells).

**What to expect.**

```text
--- The materialized worksheet ---

  'Data': 404 rows, 2417 cells materialized
  A1 = 'Daily readings, four stations' (String)
  numeric cells: 1600, sum = 41592, max = 41.99
  widest row   : row 3 with 6 cells
```

**APIs demonstrated.** `ExcelBinaryWorkbook.ReadWorksheet(index)`, `ExcelWorksheet.Rows` /
`.Cells` / `.TryGetCell`, sparse-row semantics.

## Scenario 4 - CellKindsAndDates

**Intent.** Decode cell values correctly. BIFF8 has five cell kinds (`Blank`, `String`,
`Number`, `Boolean`, `Error`) - and **dates are not one of them**: a date is a `Number`
whose *format* is a date format. `ExcelCell.IsDateFormatted` surfaces the classification,
and `ExcelSerialDate` + the workbook's `ExcelDateSystem` turn the serial into a real
`DateTime` - get the system wrong and every date shifts by 1,462 days.

**What it does.** Surveys both sheets' kind distributions (the `Data` sheet has 400
date-formatted numbers), decodes three of them via `ExcelSerialDate.ToDateTime(serial,
workbook.DateSystem)` cross-checked against the `workbook.GetDateTime(cell)` convenience,
and scans for error cells (the `Notes` sheet's `#DIV/0!` surfaces as `ExcelCellKind.Error` +
`ExcelErrorCode`, not an exception; its formula surfaces as the number it cached).

**What to expect.**

```text
--- Cell kinds, spreadsheet errors, and serial dates ---

  'Data': String=417, Number=2000, date-formatted=400
  'Notes': String=5, Number=1, Boolean=1, Error=1
  date decoding (Excel1900):
  A5: serial 45292 -> 2024-01-01 00:00 (via workbook.GetDateTime: 2024-01-01)
  A6: serial 45293 -> 2024-01-02 00:00 (via workbook.GetDateTime: 2024-01-02)
  A7: serial 45294 -> 2024-01-03 00:00 (via workbook.GetDateTime: 2024-01-03)
error cells  : B4=#DivideByZero
```

**APIs demonstrated.** `ExcelCellKind`, `ExcelCell.IsDateFormatted` / `.NumberValue` /
`.ErrorValue`, `ExcelSerialDate.ToDateTime`, `ExcelBinaryWorkbook.GetDateTime`,
`ExcelDateSystem`.

## Layout

```text
Bodu.Formats.Excel.Binary.Samples.ExcelReading/
  Program.cs                        # writes the workbook, then runs the scenarios in order
  SampleConsole.cs                  # the What / Why / Expect scenario banner
  SampleWorkbook.cs                 # builds the BIFF8 workbook the scenarios read
  Scenarios/WorkbookAndSheets.cs
  Scenarios/ForwardOnlyReader.cs
  Scenarios/MaterializedWorksheet.cs
  Scenarios/CellKindsAndDates.cs
```

## Related

- `Bodu.IO.Compound` samples (`samples/IO.Compound/`) - the OLE2 container format a `.xls`
  file lives inside.
- Guides: `docs/guides/excel/`.
