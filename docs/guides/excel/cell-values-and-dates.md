---
title: Cell values and dates
---

# Cell values and dates

Every populated cell is surfaced as an immutable <xref:Bodu.Formats.Excel.ExcelCell>. This guide covers reading its value by kind, handling a formula cell's cached result, detecting and converting date-formatted numbers, and converting between coordinates and A1 references.

A cell carries its zero-based `RowIndex` and `ColumnIndex`, a <xref:Bodu.Formats.Excel.ExcelCellKind>, and the value projection that matches the kind. Blank cells are never returned, so a worksheet is a sparse sequence of populated cells.

## Pattern 1 — read a cell by kind

```csharp
using Bodu.Formats.Excel;

static object? ValueOf(ExcelCell cell) => cell.Kind switch
{
    ExcelCellKind.String  => cell.StringValue,
    ExcelCellKind.Number  => cell.NumberValue,
    ExcelCellKind.Boolean => cell.BooleanValue,
    ExcelCellKind.Error   => cell.ErrorValue,
    _ => null,
};
```

The projection matching the <xref:Bodu.Formats.Excel.ExcelCellKind> holds the value; the others are `null`. Read `StringValue` only on a `String` cell, `NumberValue` only on a `Number` cell, and so on. Numbers are raw `double` values with no date interpretation applied.

## Pattern 2 — handle error cells

```csharp
using Bodu.Formats.Excel;

if (cell.Kind == ExcelCellKind.Error)
{
    string display = cell.ErrorValue switch
    {
        ExcelErrorCode.DivideByZero => "#DIV/0!",
        ExcelErrorCode.NotAvailable => "#N/A",
        ExcelErrorCode.Reference    => "#REF!",
        ExcelErrorCode.Value        => "#VALUE!",
        _ => $"#ERR({(byte)cell.ErrorValue!.Value:X2})",
    };
}
```

<xref:Bodu.Formats.Excel.ExcelErrorCode> names the documented BIFF8 spreadsheet error codes — there are seven, each a `byte` whose value matches the on-disk error code:

| Member | Display | Meaning |
|---|---|---|
| <xref:Bodu.Formats.Excel.ExcelErrorCode.Null> | `#NULL!` | The intersection of two ranges that do not intersect. |
| <xref:Bodu.Formats.Excel.ExcelErrorCode.DivideByZero> | `#DIV/0!` | A division by zero. |
| <xref:Bodu.Formats.Excel.ExcelErrorCode.Value> | `#VALUE!` | A value of the wrong type for an operation or function. |
| <xref:Bodu.Formats.Excel.ExcelErrorCode.Reference> | `#REF!` | A reference to a cell that is not valid. |
| <xref:Bodu.Formats.Excel.ExcelErrorCode.Name> | `#NAME?` | An unrecognised name in a formula. |
| <xref:Bodu.Formats.Excel.ExcelErrorCode.Number> | `#NUM!` | An invalid numeric value for a function or formula. |
| <xref:Bodu.Formats.Excel.ExcelErrorCode.NotAvailable> | `#N/A` | A value that is not available to a function or formula. |

An undocumented code is surfaced as the raw byte cast to the enumeration, so compare against the named members before relying on the symbol — the fall-through arm above formats any unrecognised value as a hex byte rather than assuming it maps to a known error.

## Pattern 3 — a formula cell's cached result

The reader does not evaluate formulas. A formula cell is surfaced as whichever kind its **cached result** holds — the value Excel last computed and stored:

```csharp
using Bodu.Formats.Excel;

// A formula whose cached result is a number arrives as an ExcelCellKind.Number cell;
// one whose cached result is text arrives as an ExcelCellKind.String cell; and so on.
while (reader.TryReadCell(out ExcelCell cell))
    Console.WriteLine($"{cell.Kind}: {ValueOf(cell)}");
```

There is no separate "formula" cell kind — a formula cell is indistinguishable from a literal cell of the same kind, by design. The cached value is exactly what Excel stored.

## Pattern 4 — detect and convert date-formatted numbers

Excel stores dates as floating-point serial numbers, so a date cell is a `Number` cell whose *format* renders it as a date. The reader flags those cells but never reinterprets the number itself:

```csharp
using Bodu.Formats.Excel;

using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead("rates.xls");
using ExcelWorksheetReader reader = workbook.OpenWorksheet("Data");

while (reader.TryReadCell(out ExcelCell cell))
{
    if (cell.Kind != ExcelCellKind.Number)
        continue;

    if (cell.IsDateFormatted)
    {
        DateOnly date = ExcelSerialDate.FromSerialDate(cell.NumberValue!.Value, workbook.DateSystem);
        Console.WriteLine($"date: {date:yyyy-MM-dd}");
    }
    else
    {
        Console.WriteLine($"number: {cell.NumberValue}");
    }
}
```

<xref:Bodu.Formats.Excel.ExcelCell.IsDateFormatted> is set when the cell's number format is a date or time format (and date detection is enabled). <xref:Bodu.Formats.Excel.ExcelSerialDate.FromSerialDate(System.Double,Bodu.Formats.Excel.ExcelDateSystem)> converts the serial number to a <xref:System.DateOnly>, discarding any fractional time-of-day; use <xref:Bodu.Formats.Excel.ExcelSerialDate.ToDateTime(System.Double,Bodu.Formats.Excel.ExcelDateSystem)> when you need the time of day, which it preserves from the fractional part. Both helpers also offer single-argument overloads that assume the 1900 system, but prefer the date-system overload and always pass the workbook's <xref:Bodu.Formats.Excel.ExcelBinaryWorkbook.DateSystem>: the 1900 (`1899-12-30`) and 1904 (`1904-01-01`) epochs differ by 1,462 days, so the wrong system lands a serial number four-plus years off.

> [!IMPORTANT]
> The 1900 epoch is `1899-12-30`, not `1900-01-01`. Excel deliberately keeps a historical bug that treats 1900 as a leap year, and <xref:Bodu.Formats.Excel.ExcelSerialDate> reproduces that arithmetic (it delegates to <xref:System.DateTime.FromOADate(System.Double)>) so values round-trip with Excel for dates from `1900-03-01` onward. A serial number outside the representable OLE Automation date range raises <xref:System.ArgumentException>, so guard or catch when reading untrusted workbooks whose "date" column might hold an out-of-range number.

## Pattern 5 — convert with the workbook shortcut

```csharp
using Bodu.Formats.Excel;

DateTime? when = workbook.GetDateTime(cell);   // null for non-numeric cells
string? formatCode = workbook.GetNumberFormatCode(cell.FormatIndex);
```

<xref:Bodu.Formats.Excel.ExcelBinaryWorkbook.GetDateTime(Bodu.Formats.Excel.ExcelCell)> converts a numeric cell using the workbook's date system and returns `null` for any non-numeric cell, so it is safe to call on any cell. It applies the conversion to *every* numeric cell, not only date-formatted ones — inspect <xref:Bodu.Formats.Excel.ExcelCell.IsDateFormatted> first when only date-formatted cells should be treated as dates, otherwise a plain count or amount comes back as a meaningless calendar value.

`GetNumberFormatCode` resolves a cell's <xref:Bodu.Formats.Excel.ExcelCell.FormatIndex> to its format-code string (built-in or custom), or `null` when the index is unknown. A cell whose record carries no explicit format reports `FormatIndex` `0`, the General format. The format code is the same raw string Excel stores (for example `"0.00"` or `"yyyy-mm-dd"`); this reader does not render values through it.

## Pattern 6 — convert coordinates to and from A1

```csharp
using Bodu.Formats.Excel;

string a1 = ExcelCellReference.ToA1(9, 27);        // "AB10"
string col = ExcelCellReference.ColumnName(27);    // "AB"

if (ExcelCellReference.TryParseA1("AB10", out int row, out int column))
{
    // row == 9, column == 27
}
```

<xref:Bodu.Formats.Excel.ExcelCellReference> converts between zero-based coordinates and A1 references. `ToA1` and `ColumnName` produce the spreadsheet-style label; `TryParseA1` parses one back to coordinates (accepting lower-case column letters) and returns `false` for a malformed reference rather than throwing.

## Where to go next

- [Streaming vs materialized](worksheets-and-rows.md) — the two cell surfaces and when to use each.
- [Reading workbooks](reading-workbooks.md) — the open path and reader options.
- [Bodu.Formats.Excel API reference](xref:Bodu.Formats.Excel).
