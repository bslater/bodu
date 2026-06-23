---
title: Bodu.Formats.Excel.Binary — Core concepts
---

# Bodu.Formats.Excel.Binary — Core concepts

This page is the vocabulary the rest of the documentation assumes. Read it once before the [getting-started samples](getting-started.md) or the [guides](../../guides/excel/index.md), and refer back whenever a term feels imprecise.

`Bodu.Formats.Excel.Binary` is part of the **[Binary Formats & I/O](../topics/binary-formats.md)** topic — the format tier above the container reader. For the high-level shape of the library, start with the [introduction](index.md).

## BIFF8 and the Workbook stream

**BIFF8** (Binary Interchange File Format, version 8) is the on-disk format of an Excel 97–2003 `.xls` workbook. The workbook is a flat sequence of **records** — each a two-byte type, a two-byte length, and a typed body (a cell value, a shared-string entry, a sheet boundary) — stored inside the `Workbook` stream of an OLE2 compound file. <xref:Bodu.IO.Compound.CompoundFile> supplies that stream's bytes; <xref:Bodu.Formats.Excel.ExcelBinaryWorkbook> reads the records within. A compatibility save may carry both a legacy `Book` stream and a BIFF8 `Workbook` stream; the reader prefers `Workbook`.

## Workbook globals

The records before the first sheet form the **workbook globals**, parsed once when the workbook is opened:

- the **date system** (1900 or 1904), from the `DATEMODE` record;
- the **shared string table** (SST), the deduplicated pool of strings that text cells reference by index;
- the **number-format table** (`FORMAT` / `XF`), used to resolve each cell's format and to classify date formatting;
- the **sheet directory** (`BOUNDSHEET`), naming each sheet, recording its type and visibility, and pointing at the byte offset of its substream.

Because the globals are parsed up front, a single sheet can then be read on demand by seeking to its recorded offset, without parsing the others.

## Worksheet substream

Each sheet is its own **substream** — a `BOF` record, an optional `DIMENSIONS` record, the cell records, and an `EOF`. <xref:Bodu.Formats.Excel.ExcelBinaryWorkbook.OpenWorksheet(System.Int32)> seeks to the substream and returns a reader over it; the whole workbook is never materialized at once.

## Cell and cell kind

A populated cell is surfaced as an immutable <xref:Bodu.Formats.Excel.ExcelCell> carrying its zero-based row and column and a <xref:Bodu.Formats.Excel.ExcelCellKind> — `String`, `Number`, `Boolean`, or `Error`. The matching value is read from the projection that fits the kind (`StringValue`, `NumberValue`, `BooleanValue`, `ErrorValue`); the others are `null`. **Blank cells are not returned at all**, so both reader surfaces are sparse. Several BIFF record types map onto these kinds: `LABELSST` and `LABEL` (text), `NUMBER` and `RK` (numbers), `MULRK` (a run of numbers expanded one cell per value), `BOOLERR` (boolean or error), and `FORMULA` (whichever kind its cached result holds).

## Formula cached result

The reader does **not** evaluate formulas. A `FORMULA` record carries the **cached result** Excel last computed and stored — a number, boolean, error, or string — and that cached value is what the matching <xref:Bodu.Formats.Excel.ExcelCell> exposes. A string-valued formula result is carried in a trailing `STRING` record, which the reader consumes transparently.

## Number format and serial dates

Excel stores dates and times as floating-point **serial numbers** measured from an epoch (`1899-12-30` for the 1900 system), so a date cell is just a `Number` cell whose *format* renders it as a date. The reader never assumes a number is a date, but it inspects each numeric cell's number format and sets <xref:Bodu.Formats.Excel.ExcelCell.IsDateFormatted> when the format is a date or time format. <xref:Bodu.Formats.Excel.ExcelSerialDate> converts a serial number to a <xref:System.DateOnly> or <xref:System.DateTime> using a <xref:Bodu.Formats.Excel.ExcelDateSystem>; the two systems differ by 1,462 days.

## Used range (dimensions)

A sheet's `DIMENSIONS` record declares its **used range** — the half-open span of rows `[FirstRowIndex, FirstRowIndex + RowCount)` and columns `[FirstColumnIndex, FirstColumnIndex + ColumnCount)` that bounds its populated cells, surfaced as <xref:Bodu.Formats.Excel.ExcelWorksheetDimensions>. Excel records the extent it allocated, which may be larger than the region that actually holds values, so the range is an **upper bound**, not a tight fit. A sheet with no `DIMENSIONS` record yields the default value, whose counts are zero.

## Streaming vs materialized

A sheet can be read two ways:

- **Streaming** (<xref:Bodu.Formats.Excel.ExcelWorksheetReader>) is the forward-only, low-allocation surface: cells are decoded on demand through `TryReadCell` in record order, without building an intermediate map. `ReadCells` and `ReadRows` are lazy wrappers over it.
- **Materialized** (<xref:Bodu.Formats.Excel.ExcelWorksheet>) buffers every populated cell once and exposes them by position through `TryGetCell` and grouped into `Rows`, for random access at the cost of holding the sheet in memory.

Both surfaces are sparse and yield the same <xref:Bodu.Formats.Excel.ExcelCell> values; only the access pattern and memory profile differ.

## Reader options

<xref:Bodu.Formats.Excel.ExcelBinaryReaderOptions> trades optional work for throughput and governs ownership: `LeaveOpen` keeps a caller-supplied stream open after the workbook is disposed, `ReadDocumentProperties` controls whether the OLE summary-information streams are read into <xref:Bodu.Formats.Excel.ExcelWorkbookProperties>, and `DetectDateFormats` controls whether numeric cells are classified as date-formatted. The defaults read the full metadata surface and own the stream.

## Errors

| Exception | Meaning |
|---|---|
| <xref:Bodu.Formats.Excel.ExcelBinaryFormatException> | A BIFF record is malformed — a truncated record, an inconsistent length, trailing bytes, or an out-of-range shared-string index. |
| <xref:Bodu.Formats.Excel.ExcelBinaryWorkbookStreamNotFoundException> | The compound file has no `Workbook` (or legacy `Book`) stream — it is a valid container but not a spreadsheet. |
| <xref:Bodu.Formats.Excel.ExcelBinaryUnsupportedException> | The workbook declares a BIFF version this reader does not support (it targets BIFF8). |
| <xref:Bodu.Formats.Excel.ExcelBinaryEncryptedWorkbookException> | The workbook is password-protected (a `FILEPASS` record is present); this reader does not decrypt. |

## Where to go next

- **[Getting started](getting-started.md)** — install + minimal samples.
- **[Introduction](index.md)** — the headline types and scenarios.
- **[Bodu.Formats.Excel.Binary guides](../../guides/excel/index.md)** — the recipe-style walk-throughs.
- **[Bodu.IO.Compound concepts](../io-compound/concepts.md)** — the container vocabulary beneath this format reader.
