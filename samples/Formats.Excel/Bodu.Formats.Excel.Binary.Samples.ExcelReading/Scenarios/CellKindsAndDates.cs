// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CellKindsAndDates.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Excel;

namespace Bodu.Formats.Excel.Binary.Samples.ExcelReading.Scenarios;

/// <summary>
/// Demonstrates cell-value decoding: the five <see cref="ExcelCellKind" />s, and the trap BIFF8
/// sets for every importer — dates are not a cell kind, they are numbers whose <em>format</em>
/// is a date format. <see cref="ExcelCell.IsDateFormatted" /> surfaces that classification, and
/// <see cref="ExcelSerialDate" /> plus the workbook's <see cref="ExcelDateSystem" /> turn the
/// serial number into a real <see cref="DateTime" />.
/// </summary>
public static class CellKindsAndDates
{
    /// <summary>
    /// Surveys cell kinds across all sheets and decodes the date-formatted numbers.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Cell kinds, spreadsheet errors, and serial dates",
            what: "Reads cells of each kind - text, number, boolean, error, a formula's cached result - and "
                + "converts a numeric cell to a date under both of the workbook's possible epochs.",
            why: "Two things here are easy to get wrong and both are silent. A formula cell stores its last "
                + "computed result, and since this reader does not evaluate formulas, reporting that cached value "
                + "as the cell's value is the honest answer - it is what Excel last wrote, which may be stale but "
                + "is not invented. The other is dates: Excel has no date type, only a number plus a workbook-"
                + "level epoch flag, and the 1900 and 1904 systems are four years apart. Reading the flag rather "
                + "than assuming one is the difference between a correct date and a plausible wrong one.",
            expect: "Each kind is distinguished rather than flattened to text, including the spreadsheet error "
                + "code, which is a value and not a failure. The same serial number yields dates four years "
                + "apart under the two epochs - which is why the workbook's own flag has to be consulted.");

        using var workbook = ExcelBinaryWorkbook.OpenRead(Path.Combine(AppContext.BaseDirectory, "Data", "sample-biff8.xls"));

        // Survey every sheet for the kinds it contains.
        // There is no "formula" kind: a formula cell surfaces as the kind of its cached result
        // (no evaluation is performed).
        foreach (var info in workbook.Worksheets)
        {
            var sheet = workbook.ReadWorksheet(info.Index);
            var kinds = sheet.Cells.GroupBy(c => c.Kind).OrderBy(g => g.Key)
                .Select(g => $"{g.Key}={g.Count()}");
            var dateFormatted = sheet.Cells.Count(c => c.IsDateFormatted);

            Console.WriteLine($"  '{info.Name}': {string.Join(", ", kinds)}{(dateFormatted > 0 ? $", date-formatted={dateFormatted}" : string.Empty)}");
        }

        // Decode date-formatted numbers: serial -> DateTime via the workbook's date system.
        var firstSheet = workbook.ReadWorksheet(0);
        var dates = firstSheet.Cells.Where(c => c.IsDateFormatted && c.NumberValue is not null).Take(3).ToList();

        if (dates.Count > 0)
        {
            Console.WriteLine($"  date decoding ({workbook.DateSystem}):");
            foreach (var cell in dates)
            {
                // ExcelSerialDate.ToDateTime is the manual path; workbook.GetDateTime(cell) is the
                // convenience that applies the workbook's declared date system for you.
                var decoded = ExcelSerialDate.ToDateTime(cell.NumberValue!.Value, workbook.DateSystem);
                Console.WriteLine($"  {ExcelCellReference.ToA1(cell.RowIndex, cell.ColumnIndex)}: serial {cell.NumberValue} -> {decoded:yyyy-MM-dd HH:mm} (via workbook.GetDateTime: {workbook.GetDateTime(cell):yyyy-MM-dd})");
            }
        }
        else
        {
            Console.WriteLine("  no date-formatted cells in the first sheet.");
        }

        // Error cells carry the spreadsheet error code, not an exception.
        var errors = workbook.Worksheets
            .SelectMany(info => workbook.ReadWorksheet(info.Index).Cells)
            .Where(c => c.Kind == ExcelCellKind.Error)
            .Take(3)
            .ToList();
        Console.WriteLine(errors.Count > 0
            ? $"error cells  : {string.Join(", ", errors.Select(c => $"{ExcelCellReference.ToA1(c.RowIndex, c.ColumnIndex)}=#{c.ErrorValue}"))}"
            : "error cells  : none in this workbook");

        Console.WriteLine();
    }
}
