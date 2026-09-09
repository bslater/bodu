// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelWorksheetReaderTests.ReadCells.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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

        Assert.IsEmpty(reader.ReadCells());
    }

    /// <summary>
    /// Verifies that enumeration resumes after cells were read directly and includes the remainder of a MULRK run.
    /// </summary>
    [TestMethod]
    public void ReadCells_WhenCalledMidRun_ShouldContinueFromPendingCells()
    {
        using ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReader(
            Biff8TestWorkbook.MulRk(0, 0, (0, 0x06), (0, 0x0A), (0, 0x0E)),
            Biff8TestWorkbook.Number(1, 0, 4.0));
        Assert.IsTrue(reader.TryReadCell(out ExcelCell first));
        Assert.AreEqual(1.0, first.NumberValue);

        List<double?> rest = reader.ReadCells().Select(c => c.NumberValue).ToList();

        CollectionAssert.AreEqual(new double?[] { 2.0, 3.0, 4.0 }, rest);
    }

    /// <summary>
    /// Verifies that the sequence is lazy: nothing is decoded until it is enumerated, and a malformed record fails
    /// only when reached.
    /// </summary>
    [TestMethod]
    public void ReadCells_WhenEnumeratedLazily_ShouldFailOnlyWhenBadRecordIsReached()
    {
        using ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReader(
            Biff8TestWorkbook.Number(0, 0, 1.0),
            Biff8TestWorkbook.Record(0x0203, new byte[8]));

        IEnumerable<ExcelCell> cells = reader.ReadCells();
        using IEnumerator<ExcelCell> enumerator = cells.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(1.0, enumerator.Current.NumberValue);
        _ = Assert.ThrowsExactly<ExcelBinaryFormatException>(() =>
        {
            _ = enumerator.MoveNext();
        });
    }

    /// <summary>
    /// Verifies that a second enumeration after the first exhausted the reader yields nothing.
    /// </summary>
    [TestMethod]
    public void ReadCells_WhenEnumeratedTwice_ShouldBeEmptySecondTime()
    {
        using ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReader(Biff8TestWorkbook.Number(0, 0, 1.0));

        Assert.HasCount(1, reader.ReadCells().ToList());
        Assert.IsEmpty(reader.ReadCells());
    }
}
