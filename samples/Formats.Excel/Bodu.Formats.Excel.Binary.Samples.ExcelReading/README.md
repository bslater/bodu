# Bodu.Formats.Excel.Binary.Samples.ExcelReading

The read-only BIFF8 workbook reader in `Bodu.Formats.Excel.Binary`: opening a real `.xls`
and listing its sheets, the forward-only cell reader (the primary surface), the materialized
convenience surface, and decoding cell kinds — including BIFF8's classic importer trap, dates
stored as format-classified numbers. All scenarios run offline against the committed
`Data/sample-biff8.xls` (464 KB, copied from the library's test fixtures — a genuine
exchange-rates workbook with two sheets and ~18,000 cells).

> Scope notes: this package reads the Excel **97-2003 binary** format (`.xls`) only — not
> `.xlsx` — and is read-only. Formula cells surface their *cached results*; encrypted
> workbooks throw `ExcelBinaryEncryptedWorkbookException`.

```bash
dotnet run --project samples/Formats.Excel/Bodu.Formats.Excel.Binary.Samples.ExcelReading
```

## Scenario 1 — WorkbookAndSheets

**Intent.** Show the session model: `ExcelBinaryWorkbook` opens the container once and
exposes everything needed to decide *what* to read before reading any cells — the sheet
directory (name, type, visibility, declared used range), the flattened document properties,
and the workbook's declared date system.

**What it does.** Opens the fixture and prints the date system, the summary properties, and
each sheet's `ExcelWorksheetInfo` with its used range rendered in A1 notation via
`ExcelCellReference`.

**What to expect.** Two visible worksheets; note the *declared* used range (`A1:BP2186`) is
the sheet's claim, not a promise of populated cells — Scenario 2 shows actual content stops
at row 874:

```text
date system : Excel1900
properties  : title='', author='', app='Microsoft Excel'
worksheets  : 2
  [0] 'Data' (Worksheet, Visible) used range A1:BP2186 (2186 rows x 68 cols)
  [1] 'Notes' (Worksheet, Visible) used range A1:L28 (28 rows x 12 cols)
```

**APIs demonstrated.** `ExcelBinaryWorkbook.OpenRead(path)` (+ `IDisposable`), `.Worksheets`
/ `ExcelWorksheetInfo`, `.Properties` (`ExcelWorkbookProperties`), `.DateSystem`,
`ExcelWorksheetDimensions`, `ExcelCellReference.ToA1`.

## Scenario 2 — ForwardOnlyReader

**Intent.** Show the primary surface: `ExcelWorksheetReader` streams cells forward-only in
file order — constant memory regardless of sheet size, the right shape for import pipelines
that transform data as it arrives rather than loading 18,000 cells to look at each once.

**What it does.** Drives the lowest-level `TryReadCell` loop over the `Data` sheet, counting
and classifying every cell without buffering any; then reopens the sheet and uses `ReadRows`
(the same stream, grouped into rows) to preview the first three rows in A1 notation.

**What to expect.**

```text
cells read  : 17937 across rows 0..874
kinds       : String=170, Number=17767
  row 0: A1='F11.1  EXCHANGE RATES '
  row 1: A2='Title' | B2='A$1=USD' | ...
```

**APIs demonstrated.** `ExcelBinaryWorkbook.OpenWorksheet(index)`,
`ExcelWorksheetReader.TryReadCell` / `.ReadRows` / `.Worksheet`, `ExcelCell.Kind` /
`.RowIndex` / `.ColumnIndex`, `ExcelRow`.

## Scenario 3 — MaterializedWorksheet

**Intent.** Show the convenience surface for when the sheet fits in memory and you need
*random* access: `ReadWorksheet` materializes an `ExcelWorksheet` with indexed rows,
coordinate lookup, and LINQ-friendly collections.

**What it does.** Materializes the `Data` sheet, looks up `A1` by coordinates with
`TryGetCell`, aggregates every non-date numeric cell with LINQ, and finds the widest row —
noting that rows are sparse (only populated rows appear, each holding only its populated
cells).

**What to expect.**

```text
'Data': 873 rows, 17937 cells materialized
A1 = 'F11.1  EXCHANGE RATES ' (String)
numeric cells: 16880, sum = 2.46213E+07, max = 19126
widest row   : row 1 with 24 cells
```

**APIs demonstrated.** `ExcelBinaryWorkbook.ReadWorksheet(index)`, `ExcelWorksheet.Rows` /
`.Cells` / `.TryGetCell`, sparse-row semantics.

## Scenario 4 — CellKindsAndDates

**Intent.** Decode cell values correctly. BIFF8 has five cell kinds (`Blank`, `String`,
`Number`, `Boolean`, `Error`) — and **dates are not one of them**: a date is a `Number`
whose *format* is a date format. `ExcelCell.IsDateFormatted` surfaces the classification,
and `ExcelSerialDate` + the workbook's `ExcelDateSystem` turn the serial into a real
`DateTime` — get the system wrong and every date shifts by 1,462 days.

**What it does.** Surveys both sheets' kind distributions (the `Data` sheet has 887
date-formatted numbers), decodes three of them via `ExcelSerialDate.ToDateTime(serial,
workbook.DateSystem)` cross-checked against the `workbook.GetDateTime(cell)` convenience,
and scans for error cells (this workbook has none; a `#DIV/0!` would surface as
`ExcelCellKind.Error` + `ExcelErrorCode`, not an exception).

**What to expect.**

```text
  'Data': String=170, Number=17767, date-formatted=887
  'Notes': String=45, Number=18
date decoding (Excel1900):
  B10: serial 46185 -> 2026-06-12 00:00 (via workbook.GetDateTime: 2026-06-12)
error cells  : none in this workbook
```

**APIs demonstrated.** `ExcelCellKind`, `ExcelCell.IsDateFormatted` / `.NumberValue` /
`.ErrorValue`, `ExcelSerialDate.ToDateTime`, `ExcelBinaryWorkbook.GetDateTime`,
`ExcelDateSystem`.

## Layout

```text
Bodu.Formats.Excel.Binary.Samples.ExcelReading/
  Program.cs                        # runs the scenarios in order
  Data/sample-biff8.xls             # committed 464 KB BIFF8 fixture (2 sheets, ~18k cells)
  Scenarios/WorkbookAndSheets.cs
  Scenarios/ForwardOnlyReader.cs
  Scenarios/MaterializedWorksheet.cs
  Scenarios/CellKindsAndDates.cs
```

## Related

- `Bodu.IO.Compound` samples (`samples/IO.Compound/`) — the OLE2 container format a `.xls`
  file lives inside.
- Guides: `docs/guides/excel/`.
