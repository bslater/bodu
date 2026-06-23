// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8WorkbookReaderTests.Dimensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Binary;

public partial class Biff8WorkbookReaderTests
{
    /// <summary>
    /// Verifies that the data sheet exposes the used range declared by its <c>DIMENSIONS</c> record.
    /// </summary>
    [TestMethod]
    public void Dimensions_WhenDataSheet_ShouldReportDeclaredUsedRange()
    {
        Biff8WorkbookReader reader = OpenSample();

        Biff8SheetDimensions dimensions = reader.Sheets[0].Dimensions;

        Assert.AreEqual(0, dimensions.FirstRowIndex);
        Assert.AreEqual(2186, dimensions.RowCount);
        Assert.AreEqual(0, dimensions.FirstColumnIndex);
        Assert.AreEqual(68, dimensions.ColumnCount);
    }

    /// <summary>
    /// Verifies that the notes sheet exposes its own, smaller used range.
    /// </summary>
    [TestMethod]
    public void Dimensions_WhenNotesSheet_ShouldReportDeclaredUsedRange()
    {
        Biff8WorkbookReader reader = OpenSample();

        Biff8SheetDimensions dimensions = reader.Sheets[1].Dimensions;

        Assert.AreEqual(0, dimensions.FirstRowIndex);
        Assert.AreEqual(28, dimensions.RowCount);
        Assert.AreEqual(0, dimensions.FirstColumnIndex);
        Assert.AreEqual(12, dimensions.ColumnCount);
    }
}
