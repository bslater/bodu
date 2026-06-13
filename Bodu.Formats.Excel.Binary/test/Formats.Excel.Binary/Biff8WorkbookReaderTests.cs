// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8WorkbookReaderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using Bodu.Test;

namespace Bodu.Formats.Excel.Binary;

/// <summary>
/// Verifies the behavior of <see cref="Biff8WorkbookReader" /> against a real-world BIFF8 workbook.
/// </summary>
[TestClass]
public partial class Biff8WorkbookReaderTests
{
    /// <summary>
    /// Opens the embedded sample workbook.
    /// </summary>
    /// <returns>An open <see cref="Biff8WorkbookReader" /> over the sample fixture.</returns>
    private static Biff8WorkbookReader OpenSample() =>
        Biff8WorkbookReader.Open(ExcelBinaryFixtures.OpenStream(ExcelBinaryFixtures.SampleBiff8));

    /// <summary>
    /// Reads every populated cell of the named sheet into a position-keyed dictionary.
    /// </summary>
    /// <param name="reader">The workbook reader.</param>
    /// <param name="sheetName">The sheet to read.</param>
    /// <returns>A dictionary mapping each populated cell's (row, column) to its value.</returns>
    private static Dictionary<(int Row, int Column), ExcelCell> ReadCellGrid(Biff8WorkbookReader reader, string sheetName)
    {
        Dictionary<(int Row, int Column), ExcelCell> grid = new();
        foreach (ExcelCell cell in reader.ReadSheetCells(sheetName))
            grid[(cell.RowIndex, cell.ColumnIndex)] = cell;

        return grid;
    }

    /// <summary>
    /// Verifies that opening the sample workbook exposes its two worksheets in order.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Open_WhenSampleWorkbook_ShouldExposeDataAndNotesSheets()
    {
        Biff8WorkbookReader reader = OpenSample();

        var names = reader.Sheets.Select(s => s.Name).ToList();

        CollectionAssert.AreEqual(new[] { "Data", "Notes" }, names);
        Assert.IsTrue(reader.Sheets.All(s => s.IsVisible));
    }

    /// <summary>
    /// Verifies that opening a <see langword="null" /> stream throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Open_WhenStreamIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Biff8WorkbookReader.Open(null!);
        });

        Assert.AreEqual("stream", ex.ParamName);
    }

    /// <summary>
    /// Verifies that opening data that is not a compound file throws <see cref="IO.Compound.CompoundFileFormatException" />.
    /// </summary>
    [TestMethod]
    public void Open_WhenStreamIsNotCompoundFile_ShouldThrowCompoundFileFormatException()
    {
        using MemoryStream stream = new(new byte[600]);

        _ = Assert.ThrowsExactly<IO.Compound.CompoundFileFormatException>(() =>
        {
            _ = Biff8WorkbookReader.Open(stream);
        });
    }

    /// <summary>
    /// Verifies that requesting a sheet index outside the range throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void ReadSheetCells_WhenSheetIndexOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        Biff8WorkbookReader reader = OpenSample();

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = reader.ReadSheetCells(99).ToList();
        });
    }

    /// <summary>
    /// Verifies that requesting an unknown sheet name throws <see cref="KeyNotFoundException" />.
    /// </summary>
    [TestMethod]
    public void ReadSheetCells_WhenSheetNameMissing_ShouldThrowKeyNotFoundException()
    {
        Biff8WorkbookReader reader = OpenSample();

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = reader.ReadSheetCells("NoSuchSheet").ToList();
        });
    }
}
