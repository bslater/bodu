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
        Console.WriteLine("--- Cell kinds and serial dates ---");

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
            Console.WriteLine($"date decoding ({workbook.DateSystem}):");
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
            Console.WriteLine("no date-formatted cells in the first sheet.");
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
