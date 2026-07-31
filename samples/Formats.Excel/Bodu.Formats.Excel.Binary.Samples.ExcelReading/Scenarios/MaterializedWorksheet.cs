// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MaterializedWorksheet.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Excel;

namespace Bodu.Formats.Excel.Binary.Samples.ExcelReading.Scenarios;

/// <summary>
/// Demonstrates the convenience surface: <see cref="ExcelBinaryWorkbook.ReadWorksheet(int)" />
/// materializes a whole sheet into an <see cref="ExcelWorksheet" /> with indexed rows, cell
/// lookup by coordinates, and LINQ-friendly collections — the right shape when the sheet fits
/// in memory and you need random access rather than a single pass.
/// </summary>
public static class MaterializedWorksheet
{
    /// <summary>
    /// Materializes the first worksheet and accesses it randomly.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- ExcelWorksheet: the materialized convenience surface ---");

        using var workbook = ExcelBinaryWorkbook.OpenRead(Path.Combine(AppContext.BaseDirectory, "Data", "sample-biff8.xls"));
        var sheet = workbook.ReadWorksheet(0);

        Console.WriteLine($"'{sheet.Name}': {sheet.Rows.Count} rows, {sheet.Cells.Count} cells materialized");

        // Random access by coordinates - no streaming position to manage.
        if (sheet.TryGetCell(0, 0, out var a1))
        {
            Console.WriteLine($"A1 = {(a1.Kind == ExcelCellKind.String ? $"'{a1.StringValue}'" : a1.NumberValue?.ToString())} ({a1.Kind})");
        }

        // LINQ over the cell collection: aggregate every numeric cell.
        // Date cells are Number cells too (their value is a date serial), so exclude the
        // date-formatted ones to keep the aggregate meaningful.
        var numbers = sheet.Cells.Where(c => c.Kind == ExcelCellKind.Number && !c.IsDateFormatted).ToList();
        if (numbers.Count > 0)
        {
            Console.WriteLine($"numeric cells: {numbers.Count}, sum = {numbers.Sum(c => c.NumberValue ?? 0):G6}, max = {numbers.Max(c => c.NumberValue ?? 0):G6}");
        }

        // Rows are sparse: only rows with content appear, each holding only its populated cells.
        var widest = sheet.Rows.MaxBy(r => r.Cells.Count);
        Console.WriteLine($"widest row   : row {widest?.RowIndex} with {widest?.Cells.Count} cells");

        Console.WriteLine();
    }
}
