// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Excel.Binary.Samples.ExcelReading.Scenarios;

namespace Bodu.Formats.Excel.Binary.Samples.ExcelReading;

/// <summary>
/// Entry point for the Excel-reading sample: the read-only BIFF8 (<c>.xls</c>) workbook reader
/// in <c>Bodu.Formats.Excel.Binary</c> — opening a workbook and listing its sheets, the
/// forward-only cell reader (the primary surface), the materialized convenience surface, and
/// decoding cell kinds including date-formatted serial numbers. Everything runs offline against
/// the committed <c>Data/sample-biff8.xls</c> fixture.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Formats.Excel.Binary.Samples.ExcelReading");
        Console.WriteLine("==============================================");
        Console.WriteLine();

        WorkbookAndSheets.Run();
        ForwardOnlyReader.Run();
        MaterializedWorksheet.Run();
        CellKindsAndDates.Run();

        Console.WriteLine("Done.");
    }
}
