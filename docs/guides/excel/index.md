---
title: Bodu.Formats.Excel.Binary guides
---

# Bodu.Formats.Excel.Binary guides

Recipe-style walk-throughs for **Bodu.Formats.Excel.Binary**, the narrow, read-only reader for the Excel 97–2003 binary workbook format (BIFF8 / `.xls`). It surfaces the raw cell values of each worksheet — strings, numbers, booleans, and errors, including a formula cell's cached result — without formula evaluation, styling, or higher-level interpretation.

An `.xls` file is a BIFF8 record stream stored inside the `Workbook` stream of an OLE2 compound file. This package reads the records; the container around them is read by <xref:Bodu.IO.Compound.CompoundFile>, on which it is built.

If you are new to the library, start with the [introduction](../../docs/excel/index.md), the [Core concepts](../../docs/excel/concepts.md) glossary, and the [getting-started page](../../docs/excel/getting-started.md). The guides below assume you know the vocabulary (BIFF8 record, workbook globals, cell kind, serial date, used range).

## How the library works

<xref:Bodu.Formats.Excel.ExcelBinaryWorkbook> opens the `.xls` container, parses the workbook globals once — the date system, the shared string table, the number-format table, and the sheet directory — and lists the sheets. A sheet is read on demand by seeking to the byte offset its directory entry records, so a single sheet can be read without parsing the others and the whole workbook is never materialized.

![An Excel 97-2003 binary workbook is a BIFF8 record stream stored inside the Workbook stream of an OLE2 compound file. Bodu.IO.Compound supplies the Workbook stream's bytes; ExcelBinaryWorkbook parses the workbook globals once, then reads each sheet on demand and surfaces ExcelCell values through a forward-only reader or a materialized worksheet.](../../images/diagrams/excel-binary-structure.svg)

A sheet is surfaced through one of two cell surfaces: the forward-only, low-allocation <xref:Bodu.Formats.Excel.ExcelWorksheetReader>, or the materialized, randomly addressable <xref:Bodu.Formats.Excel.ExcelWorksheet>. Both yield the same sparse <xref:Bodu.Formats.Excel.ExcelCell> values; only the access pattern and memory profile differ.

> These guides cover the read path only — the reader never writes, evaluates formulas, or applies styling.

## Namespace map

| Namespace | What lives here | Guides |
|---|---|---|
| <xref:Bodu.Formats.Excel> | The `ExcelBinaryWorkbook` session, the `ExcelWorksheetReader` / `ExcelWorksheet` surfaces, the `ExcelCell` value model, `ExcelSerialDate` / `ExcelCellReference` helpers, and the workbook exceptions. | [Reading workbooks](reading-workbooks.md) · [Cell values and dates](cell-values-and-dates.md) · [Streaming vs materialized](worksheets-and-rows.md) |

## Guides

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="reading-workbooks.md">Reading workbooks</a></h3>
  <p>Open an <code>.xls</code> from a path or stream, list the sheets and their used ranges, control stream ownership and optional metadata work, and read authored document properties — the end-to-end open recipe.</p>
</div>

<div class="bodu-card">
  <h3><a href="cell-values-and-dates.md">Cell values and dates</a></h3>
  <p>The <code>ExcelCell</code> kinds and value projections, a formula cell's cached result, date-format detection, serial-date conversion across the 1900 and 1904 systems, and A1 reference conversion.</p>
</div>

<div class="bodu-card">
  <h3><a href="worksheets-and-rows.md">Streaming vs materialized</a></h3>
  <p>The forward-only <code>ExcelWorksheetReader</code> (<code>TryReadCell</code>, <code>ReadCells</code>, <code>ReadRows</code>) versus the materialized <code>ExcelWorksheet</code> (<code>TryGetCell</code>, <code>Rows</code>) — when to reach for each, and how to bound allocation.</p>
</div>

</div>

## Suggested reading path

1. **[Reading workbooks](reading-workbooks.md)** — the core open → list → read recipe that every other use builds on.
2. **[Cell values and dates](cell-values-and-dates.md)** — interpret the cell kinds and turn date-formatted numbers into calendar dates.
3. **[Streaming vs materialized](worksheets-and-rows.md)** — choose the access pattern that fits a one-pass scan or random access.

## Where to go next

- [Bodu.Formats.Excel API reference](xref:Bodu.Formats.Excel) — every type and member.
- [Bodu.IO.Compound](../io-compound/index.md) — the container reader beneath this package.
- [Binary Formats & I/O topic guides](../topics/binary-formats.md) — recipe-style walk-throughs across the topic.
- [Package matrix](../../docs/package-matrix.md) — where Bodu.Formats.Excel.Binary sits in the suite and its dependency stack.
