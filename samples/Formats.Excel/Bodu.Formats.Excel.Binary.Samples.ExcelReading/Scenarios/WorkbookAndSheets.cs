// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkbookAndSheets.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Excel;

namespace Bodu.Formats.Excel.Binary.Samples.ExcelReading.Scenarios;

/// <summary>
/// Demonstrates the workbook session: <see cref="ExcelBinaryWorkbook" /> opens the container
/// once, exposes the sheet directory (<see cref="ExcelWorksheetInfo" /> — name, visibility,
/// type, declared used range), the flattened document properties, and the workbook's declared
/// date system — everything you need to decide what to read before reading any cells.
/// </summary>
public static class WorkbookAndSheets
{
    /// <summary>
    /// Opens <c>Data/sample-biff8.xls</c> and reports its sheets and metadata.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Opening a workbook - sheets, visibility, and used ranges",
            what: "Opens a committed .xls file, lists its sheets with their visibility, type and declared "
                + "dimensions, and reads the flattened document properties.",
            why: "The Excel 97-2003 binary format is a record stream inside a compound-file container, and this "
                + "reader deliberately stops at raw values - no formula evaluation, no styling, no "
                + "interpretation. That narrowness is the point: a reader that tried to be a spreadsheet engine "
                + "would be enormous and would still disagree with Excel, whereas one that reports what the file "
                + "literally says is small enough to trust. The used range is worth reading rather than inferring, "
                + "because a sheet declares its own extent and scanning for it means loading cells you did not "
                + "want.",
            expect: "Sheet metadata comes from the workbook globals without any sheet being loaded, so listing a "
                + "workbook is cheap regardless of how much data it holds. Hidden sheets are reported rather than "
                + "skipped - a consumer decides what to do with them, since 'hidden' is a presentation flag, not "
                + "a privacy one.");

        using var workbook = ExcelBinaryWorkbook.OpenRead(Path.Combine(AppContext.BaseDirectory, "Data", "sample-biff8.xls"));

        // The workbook declares its serial-date epoch (1900 vs the legacy Mac 1904 system); every
        // date-formatted number in the file must be decoded against this system.
        Console.WriteLine($"  date system : {workbook.DateSystem}");
        Console.WriteLine($"  properties  : title='{workbook.Properties.Title}', author='{workbook.Properties.Author}', app='{workbook.Properties.ApplicationName}'");
        Console.WriteLine($"  worksheets  : {workbook.Worksheets.Count}");

        // Dimensions come from each sheet's DIMENSIONS record - the used range as declared by the
        // writing application, available without reading any cells.
        foreach (var sheet in workbook.Worksheets)
        {
            var d = sheet.Dimensions;
            Console.WriteLine(
                $"  [{sheet.Index}] '{sheet.Name}' ({sheet.Type}, {sheet.Visibility}) " +
                $"used range {ExcelCellReference.ToA1(d.FirstRowIndex, d.FirstColumnIndex)}:" +
                $"{ExcelCellReference.ToA1(Math.Max(d.FirstRowIndex, d.FirstRowIndex + d.RowCount - 1), Math.Max(d.FirstColumnIndex, d.FirstColumnIndex + d.ColumnCount - 1))} " +
                $"({d.RowCount} rows x {d.ColumnCount} cols)");
        }

        Console.WriteLine();
    }
}
