// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Excel.Binary.Samples.ExcelReading.Scenarios;

namespace Bodu.Formats.Excel.Binary.Samples.ExcelReading;

/// <summary>
/// Entry point for the Excel-reading sample: the read-only BIFF8 (<c>.xls</c>) workbook reader
/// in <c>Bodu.Formats.Excel.Binary</c> - opening a workbook and listing its sheets, the
/// forward-only cell reader (the primary surface), the materialized convenience surface, and
/// decoding cell kinds including date-formatted serial numbers. Everything runs offline against
/// a workbook the sample writes first (<see cref="SampleWorkbook" />), whose values are synthetic.
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

        // The scenarios read an .xls file from disk, as an application would, so the sample writes its workbook to a
        // temporary file first and deletes it at the end.
        string path = Path.Combine(Path.GetTempPath(), $"bodu-excel-sample-{Guid.NewGuid():N}.xls");
        SampleWorkbook.Write(path);
        try
        {
            WorkbookAndSheets.Run(path);
            ForwardOnlyReader.Run(path);
            MaterializedWorksheet.Run(path);
            CellKindsAndDates.Run(path);
        }
        finally
        {
            File.Delete(path);
        }

        Console.WriteLine("Done.");
    }
}
