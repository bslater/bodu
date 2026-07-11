---
title: Runnable samples
---

# Runnable samples

The repository ships a runnable, self-contained sample project for
`Bodu.Formats.Excel.Binary` under
[`samples/Formats.Excel/`](https://github.com/bslater/bodu/tree/master/samples/Formats.Excel).
It is **offline and deterministic** — every scenario reads the committed
`Data/sample-biff8.xls` fixture (464 KB, a genuine two-sheet exchange-rates workbook with
~18,000 cells) — and is a member of `bodu.slnx`, built and executed by CI. The README
documents every scenario individually: its intent, what the code does, the output to expect,
and the APIs demonstrated.

Run it from the repository root:

```bash
dotnet run --project samples/Formats.Excel/Bodu.Formats.Excel.Binary.Samples.ExcelReading
```

## The sample

### Bodu.Formats.Excel.Binary.Samples.ExcelReading

The read-only BIFF8 (`.xls`) workbook reader, layer by layer:

- **WorkbookAndSheets** — the <xref:Bodu.Formats.Excel.ExcelBinaryWorkbook> session: the
  sheet directory (<xref:Bodu.Formats.Excel.ExcelWorksheetInfo> — name, type, visibility,
  declared used range in A1 notation), flattened document properties, and the workbook's
  declared <xref:Bodu.Formats.Excel.ExcelDateSystem>.
- **ForwardOnlyReader** — the primary surface:
  <xref:Bodu.Formats.Excel.ExcelWorksheetReader> streams cells forward-only with constant
  memory (17,937 cells counted and classified without buffering), plus `ReadRows` grouping
  for row-shaped import pipelines.
- **MaterializedWorksheet** — the convenience surface:
  <xref:Bodu.Formats.Excel.ExcelWorksheet> with coordinate lookup (`TryGetCell`), LINQ
  aggregation over the cell collection, and sparse-row semantics.
- **CellKindsAndDates** — the five <xref:Bodu.Formats.Excel.ExcelCellKind>s and BIFF8's
  classic importer trap: dates are `Number` cells classified by *format*.
  `ExcelCell.IsDateFormatted` plus <xref:Bodu.Formats.Excel.ExcelSerialDate> and the
  workbook's date system decode 887 serial dates; error cells surface as
  <xref:Bodu.Formats.Excel.ExcelErrorCode> values rather than exceptions.

Scope: Excel **97-2003 binary** (`.xls`) only, read-only; formula cells expose their cached
results; encrypted workbooks throw. *Package: `Bodu.Formats.Excel.Binary`.*

## Guarded documentation

The guides under [`docs/guides/excel/`](../guides/excel/index.md) carry compile-guarded
snippets: examples marked with a `<!-- compile -->` sentinel are compiled against the
current public API by `DocumentationSnippetCompileTests` in the library's test project
(Regression tier).

## Related

- [Excel guides](../guides/excel/index.md) — reading workbooks, worksheets and rows, cell
  values and dates.
- [IO.Compound samples](io-compound.md) — the OLE2 container a `.xls` lives inside.
