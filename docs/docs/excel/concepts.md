---
title: Bodu.Formats.Excel.Binary — Core concepts
---

# Bodu.Formats.Excel.Binary — Core concepts

This page is the vocabulary the rest of the documentation assumes. Read it once before the [getting-started samples](getting-started.md) or the [guides](../../guides/excel/index.md), and refer back whenever a term feels imprecise.

`Bodu.Formats.Excel.Binary` is part of the **[Binary Formats & I/O](../topics/binary-formats.md)** topic — the format tier above the container reader. For the high-level shape of the library, start with the [introduction](index.md).

## BIFF8 and the Workbook stream

**BIFF8** (Binary Interchange File Format, version 8) is the on-disk format of an Excel 97–2003 `.xls` workbook. The workbook is a flat sequence of **records** — each a two-byte type, a two-byte length, and a typed body (a cell value, a shared-string entry, a sheet boundary) — stored inside the `Workbook` stream of an OLE2 compound file. <xref:Bodu.IO.Compound.CompoundFile> supplies that stream's bytes; <xref:Bodu.Formats.Excel.ExcelBinaryWorkbook> reads the records within. A compatibility save may carry both a legacy `Book` stream and a BIFF8 `Workbook` stream; the reader prefers `Workbook` and falls back to `Book`. A compound file that holds neither (a Word document, say) raises <xref:Bodu.Formats.Excel.ExcelBinaryWorkbookStreamNotFoundException>.

> [!NOTE]
> The two byte-length fields are unsigned 16-bit values, so a single record's body is capped at 65,535 bytes. The reader does not surface raw records; it decodes the value-bearing ones into <xref:Bodu.Formats.Excel.ExcelCell> values and skips the rest. The record layer itself is internal — consumers work in terms of cells, sheets, and the workbook globals, never record types.

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

A populated cell is surfaced as an immutable <xref:Bodu.Formats.Excel.ExcelCell> carrying its zero-based row and column and a <xref:Bodu.Formats.Excel.ExcelCellKind> — `String`, `Number`, `Boolean`, or `Error`. The matching value is read from the projection that fits the kind (`StringValue`, `NumberValue`, `BooleanValue`, `ErrorValue`); the others are `null`. **Blank cells are not returned at all**, so both reader surfaces are sparse. `ExcelCellKind` also defines `Blank` for completeness, but a blank cell is never emitted — the value is informational rather than something a reader returns.

`ExcelCellKind` is the classification a reader consumer switches on. The mapping from the on-disk record types is an internal detail, but understanding it explains why some surfaces behave as they do:

| Cell kind | Originating BIFF8 records | Notes |
|---|---|---|
| `String` | `LABELSST`, `LABEL` | `LABELSST` references the shared string table by index; `LABEL` carries an inline string. |
| `Number` | `NUMBER`, `RK`, `MULRK` | `RK` is a compact encoding for integers and short decimals; `MULRK` packs a run of adjacent numeric cells into one record, which the reader expands to one cell per value. |
| `Boolean` / `Error` | `BOOLERR` | A single record type carries either a boolean or an error, distinguished by a flag. |
| `Number` / `String` / `Boolean` / `Error` | `FORMULA` (+ trailing `STRING`) | A formula cell is surfaced as whichever kind its cached result holds; see below. |

Every cell also carries a <xref:Bodu.Formats.Excel.ExcelCell.FormatIndex> — the number-format index referenced by its record (`0`, the General format, when none is recorded) — and an <xref:Bodu.Formats.Excel.ExcelCell.IsDateFormatted> flag described under [Number format and serial dates](#number-format-and-serial-dates).

## Formula cached result

The reader does **not** evaluate formulas. A `FORMULA` record carries the **cached result** Excel last computed and stored — a number, boolean, error, or string — and that cached value is what the matching <xref:Bodu.Formats.Excel.ExcelCell> exposes. There is no separate "formula" cell kind: a formula cell is indistinguishable from a literal cell of the same kind, by design. A string-valued formula result is encoded as a `FORMULA` record immediately followed by a `STRING` record holding the text; the reader consumes both transparently and emits a single `String` cell.

> [!IMPORTANT]
> The cached result is the value Excel stored at its last save, not a recomputation. If a workbook was edited by a tool that did not refresh cached results, the value read here can be stale relative to the formula. The reader has no formula engine and cannot detect or correct this.

## Number format and serial dates

Excel stores dates and times as floating-point **serial numbers**, so a date cell is just a `Number` cell whose *format* renders it as a date. The integer part is the day count from the date system's epoch; the fractional part is the time of day. The reader never assumes a number is a date, but it inspects each numeric cell's number format and sets <xref:Bodu.Formats.Excel.ExcelCell.IsDateFormatted> when the format is a date or time format (unless `DetectDateFormats` is cleared, which leaves the flag always `false`). <xref:Bodu.Formats.Excel.ExcelSerialDate> converts a serial number to a <xref:System.DateOnly> or <xref:System.DateTime> using a <xref:Bodu.Formats.Excel.ExcelDateSystem>.

A <xref:Bodu.Formats.Excel.ExcelDateSystem> names the epoch a serial number is measured from:

| Date system | Epoch | Origin |
|---|---|---|
| <xref:Bodu.Formats.Excel.ExcelDateSystem.Excel1900> | `1899-12-30` | The default for workbooks authored on Windows; the value when a workbook declares none. |
| <xref:Bodu.Formats.Excel.ExcelDateSystem.Excel1904> | `1904-01-01` | Historically the default for workbooks authored on the Macintosh. |

The two systems differ by 1,462 days, so a serial number paired with the wrong system lands four-plus years off. The workbook's own system is exposed through <xref:Bodu.Formats.Excel.ExcelBinaryWorkbook.DateSystem> (read from the `DATEMODE` record) — pass it to the converter rather than assuming the 1900 default.

> [!IMPORTANT]
> The 1900 epoch is `1899-12-30`, not `1900-01-01`, because Excel deliberately preserves a historical bug: it treats 1900 as a leap year and counts a non-existent `1900-02-29`. <xref:Bodu.Formats.Excel.ExcelSerialDate> reproduces Excel's own arithmetic (it delegates to `DateTime.FromOADate`), so serial numbers round-trip with Excel for all dates from `1900-03-01` onward. Serial numbers before that boundary are outside the range this converter handles faithfully.

## Sheet descriptor, type, and visibility

Each entry in the sheet directory is surfaced as an <xref:Bodu.Formats.Excel.ExcelWorksheetInfo> — the sheet's `Name`, zero-based `Index`, <xref:Bodu.Formats.Excel.ExcelSheetVisibility>, <xref:Bodu.Formats.Excel.ExcelSheetType>, and declared `Dimensions` — built without reading any cells.

A sheet's **type** records what kind of substream its directory entry points at. Only a <xref:Bodu.Formats.Excel.ExcelSheetType.Worksheet> carries the tabular cell records this reader surfaces; `MacroSheet`, `Chart`, `VbaModule`, and `Unknown` sheets are listed for completeness but yield no cells. The workbook reads each sheet's `DIMENSIONS` only for worksheet-typed sheets; the others report the zero-count default range.

A sheet's **visibility** distinguishes three states rather than a single hidden flag: `Visible`, `Hidden` (concealable and revealable through the Excel UI), and `VeryHidden` (revealable only through the object model). <xref:Bodu.Formats.Excel.ExcelWorksheetInfo.IsVisible> is a shortcut for `Visibility == ExcelSheetVisibility.Visible`.

## Used range (dimensions)

A sheet's `DIMENSIONS` record declares its **used range** — the half-open span of rows `[FirstRowIndex, FirstRowIndex + RowCount)` and columns `[FirstColumnIndex, FirstColumnIndex + ColumnCount)` that bounds its populated cells, surfaced as <xref:Bodu.Formats.Excel.ExcelWorksheetDimensions>. Excel records the extent it allocated, which may be larger than the region that actually holds values, so the range is an **upper bound**, not a tight fit. A sheet with no `DIMENSIONS` record yields the default value, whose counts are zero. The used range is a *declared* hint for sizing buffers and reporting; the authoritative populated cells are still the sparse sequence the reader yields.

## A1 references

Cells are addressed throughout the API by zero-based `(row, column)` coordinates. <xref:Bodu.Formats.Excel.ExcelCellReference> converts between those coordinates and the spreadsheet **A1 notation** — a bijective base-26 column name (`A`..`Z`, `AA`..`AZ`, …) followed by a one-based row number. `ToA1` and `ColumnName` build the label; `TryParseA1` parses one back (accepting upper- or lower-case letters) and returns `false` for a malformed reference rather than throwing. The conversions are culture-independent.

## Streaming vs materialized

A sheet can be read two ways:

- **Streaming** (<xref:Bodu.Formats.Excel.ExcelWorksheetReader>) is the forward-only, low-allocation surface: cells are decoded on demand through `TryReadCell` in record order, without building an intermediate map. `ReadCells` and `ReadRows` are lazy wrappers over it.
- **Materialized** (<xref:Bodu.Formats.Excel.ExcelWorksheet>) buffers every populated cell once and exposes them by position through `TryGetCell` and grouped into `Rows`, for random access at the cost of holding the sheet in memory.

Both surfaces are sparse and yield the same <xref:Bodu.Formats.Excel.ExcelCell> values; only the access pattern and memory profile differ.

## Document properties

The workbook's authored metadata is flattened into <xref:Bodu.Formats.Excel.ExcelWorkbookProperties>, read from the compound file's `SummaryInformation` and `DocumentSummaryInformation` property-set streams. The surface exposes only the document fields (`Title`, `Subject`, `Author`, `Keywords`, `Comments`, `LastSavedBy`, `ApplicationName`, `Created`, `LastSaved`, `LastPrinted`, `Company`, `Manager`, `Category`); the lower-level property-set model is intentionally not surfaced. Every member is nullable, and a workbook that omits a stream — or whose property set is corrupt — yields `null` for the affected members rather than failing the open. When `ReadDocumentProperties` is cleared, the workbook exposes an empty view in which every member is `null`.

## Reader options

<xref:Bodu.Formats.Excel.ExcelBinaryReaderOptions> trades optional work for throughput and governs ownership:

| Option | Default | Effect when cleared |
|---|---|---|
| `LeaveOpen` | `false` | Leaves a caller-supplied stream open after the workbook is disposed (default disposes it with the workbook). |
| `ReadDocumentProperties` | `true` | Skips the summary-information parse and surfaces an empty <xref:Bodu.Formats.Excel.ExcelWorkbookProperties>. |
| `DetectDateFormats` | `true` | Skips per-cell number-format classification, leaving <xref:Bodu.Formats.Excel.ExcelCell.IsDateFormatted> always `false`. |

`LeaveOpen` applies only to the `Open(stream, options)` factory; the `OpenRead(stream, leaveOpen)` overload carries the same toggle as an explicit parameter, and the file-path and `FileInfo` overloads always own the stream they open. The defaults read the full metadata surface and own the stream.

## Errors

| Exception | Meaning |
|---|---|
| <xref:Bodu.Formats.Excel.ExcelBinaryFormatException> | A BIFF record is malformed — a truncated record, an inconsistent length, trailing bytes, or an out-of-range shared-string index. |
| <xref:Bodu.Formats.Excel.ExcelBinaryWorkbookStreamNotFoundException> | The compound file has no `Workbook` (or legacy `Book`) stream — it is a valid container but not a spreadsheet. |
| <xref:Bodu.Formats.Excel.ExcelBinaryUnsupportedException> | The workbook declares a BIFF version this reader does not support (it targets BIFF8). |
| <xref:Bodu.Formats.Excel.ExcelBinaryEncryptedWorkbookException> | The workbook is password-protected (a `FILEPASS` record is present); this reader does not decrypt. |

The first three open-time failures all derive from familiar base types, so a caller can catch them by base where convenient: `ExcelBinaryFormatException` is a <xref:System.FormatException>, `ExcelBinaryWorkbookStreamNotFoundException` a <xref:System.Collections.Generic.KeyNotFoundException>, and both `ExcelBinaryUnsupportedException` and `ExcelBinaryEncryptedWorkbookException` are <xref:System.NotSupportedException>. Addressing a sheet by an unknown name throws a plain <xref:System.Collections.Generic.KeyNotFoundException>; an out-of-range index throws <xref:System.ArgumentOutOfRangeException>. Using a workbook or reader after disposal throws <xref:System.ObjectDisposedException>.

> [!NOTE]
> `ExcelBinaryFormatException` is the only one of these raised *during reading* rather than at open — a record can be found malformed only when the reader reaches it. The other three are detected while parsing the globals at open time.

## Where to go next

- **[Getting started](getting-started.md)** — install + minimal samples.
- **[Introduction](index.md)** — the headline types and scenarios.
- **[Bodu.Formats.Excel.Binary guides](../../guides/excel/index.md)** — the recipe-style walk-throughs.
- **[Bodu.IO.Compound concepts](../io-compound/concepts.md)** — the container vocabulary beneath this format reader.
