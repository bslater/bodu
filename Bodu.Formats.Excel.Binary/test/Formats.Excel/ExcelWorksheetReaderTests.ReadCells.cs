// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelWorksheetReaderTests.ReadCells.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Formats.Excel;

public partial class ExcelWorksheetReaderTests
{
    /// <summary>
    /// Verifies that <see cref="ExcelWorksheetReader.ReadCells" /> enumerates every populated cell in record order.
    /// </summary>
    [TestMethod]
    public void ReadCells_WhenCellsPresent_ShouldEnumerateInRecordOrder()
    {
        using ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReader(
            Biff8TestWorkbook.Number(0, 0, 1.0),
            Biff8TestWorkbook.Number(0, 1, 2.0),
            Biff8TestWorkbook.Number(1, 0, 3.0));

        List<(int Row, int Column)> positions = reader.ReadCells().Select(c => (c.RowIndex, c.ColumnIndex)).ToList();

        CollectionAssert.AreEqual(new[] { (0, 0), (0, 1), (1, 0) }, positions);
    }

    /// <summary>
    /// Verifies that <see cref="ExcelWorksheetReader.ReadCells" /> over a worksheet with no value records is empty.
    /// </summary>
    [TestMethod]
    public void ReadCells_WhenWorksheetHasNoCells_ShouldBeEmpty()
    {
        using ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReader();

        Assert.AreEqual(0, reader.ReadCells().Count());
    }
}
