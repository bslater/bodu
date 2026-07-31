// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ForwardOnlyReader.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Excel;

namespace Bodu.Formats.Excel.Binary.Samples.ExcelReading.Scenarios;

/// <summary>
/// Demonstrates the primary surface: <see cref="ExcelWorksheetReader" /> streams cells
/// forward-only in file order, one at a time — constant memory regardless of sheet size, the
/// right shape for import pipelines that transform rows as they arrive.
/// </summary>
public static class ForwardOnlyReader
{
    /// <summary>
    /// Streams the first worksheet's cells and aggregates without materializing anything.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- ExcelWorksheetReader: forward-only streaming ---");

        using var workbook = ExcelBinaryWorkbook.OpenRead(Path.Combine(AppContext.BaseDirectory, "Data", "sample-biff8.xls"));
        using var reader = workbook.OpenWorksheet(0);

        Console.WriteLine($"streaming '{reader.Worksheet.Name}'...");

        // TryReadCell: the lowest-level loop - count and classify without buffering.
        var counts = new Dictionary<ExcelCellKind, int>();
        var cells = 0;
        var maxRow = -1;

        while (reader.TryReadCell(out var cell))
        {
            cells++;
            counts[cell.Kind] = counts.GetValueOrDefault(cell.Kind) + 1;
            maxRow = Math.Max(maxRow, cell.RowIndex);
        }

        Console.WriteLine($"cells read  : {cells} across rows 0..{maxRow}");
        Console.WriteLine($"kinds       : {string.Join(", ", counts.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}"))}");

        // ReadRows groups the same forward-only stream into rows - here the first three.
        // A fresh reader is required: the first one is exhausted, and forward-only readers cannot rewind.
        using var rowReader = workbook.OpenWorksheet(0);
        foreach (var row in rowReader.ReadRows().Take(3))
        {
            var preview = string.Join(" | ", row.Cells.Take(4).Select(Describe));
            Console.WriteLine($"  row {row.RowIndex}: {preview}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Renders a cell as "A1='value'" for display.
    /// </summary>
    private static string Describe(ExcelCell cell)
    {
        var value = cell.Kind switch
        {
            ExcelCellKind.String => $"'{cell.StringValue}'",
            ExcelCellKind.Number => cell.NumberValue?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "?",
            ExcelCellKind.Boolean => cell.BooleanValue?.ToString() ?? "?",
            ExcelCellKind.Error => $"#{cell.ErrorValue}",
            _ => "(blank)",
        };

        return $"{ExcelCellReference.ToA1(cell.RowIndex, cell.ColumnIndex)}={value}";
    }
}
