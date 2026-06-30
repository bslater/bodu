// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelWorksheetInfoTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

/// <summary>
/// Verifies the behavior of <see cref="ExcelWorksheetInfo" />, the descriptor of a sheet within a workbook.
/// </summary>
[TestClass]
public class ExcelWorksheetInfoTests
{
    /// <summary>
    /// Verifies that the descriptor exposes the name, index, visibility, type, and used range it was constructed with.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBuilt_ShouldExposeDeclaredMetadata()
    {
        ExcelWorksheetInfo info = new("Notes", 1, ExcelSheetVisibility.Hidden, ExcelSheetType.Chart, new ExcelWorksheetDimensions(0, 28, 0, 12));

        Assert.AreEqual("Notes", info.Name);
        Assert.AreEqual(1, info.Index);
        Assert.AreEqual(ExcelSheetVisibility.Hidden, info.Visibility);
        Assert.AreEqual(ExcelSheetType.Chart, info.Type);
        Assert.AreEqual(28, info.Dimensions.RowCount);
    }

    /// <summary>
    /// Verifies that <see cref="ExcelWorksheetInfo.IsVisible" /> is <see langword="true" /> only for a visible sheet.
    /// </summary>
    /// <param name="visibility">The declared visibility.</param>
    /// <param name="expected">The expected <see cref="ExcelWorksheetInfo.IsVisible" /> result.</param>
    [TestMethod]
    [DataRow(ExcelSheetVisibility.Visible, true)]
    [DataRow(ExcelSheetVisibility.Hidden, false)]
    [DataRow(ExcelSheetVisibility.VeryHidden, false)]
    public void IsVisible_WhenVisibilityVaries_ShouldReflectVisibleState(ExcelSheetVisibility visibility, bool expected)
    {
        ExcelWorksheetInfo info = new("Sheet", 0, visibility, ExcelSheetType.Worksheet, default);

        Assert.AreEqual(expected, info.IsVisible);
    }

    /// <summary>
    /// Verifies that the data sheet of the sample workbook reports the used range declared by its <c>DIMENSIONS</c>
    /// record.
    /// </summary>
    [TestMethod]
    public void Dimensions_WhenSampleDataSheet_ShouldReportDeclaredUsedRange()
    {
        using var workbook = ExcelBinaryWorkbook.OpenRead(ExcelBinaryFixtures.OpenStream(ExcelBinaryFixtures.SampleBiff8));

        ExcelWorksheetDimensions dimensions = workbook.Worksheets[0].Dimensions;

        Assert.AreEqual(0, dimensions.FirstRowIndex);
        Assert.AreEqual(2186, dimensions.RowCount);
        Assert.AreEqual(0, dimensions.FirstColumnIndex);
        Assert.AreEqual(68, dimensions.ColumnCount);
    }

    /// <summary>
    /// Verifies that the notes sheet of the sample workbook reports its own, smaller used range.
    /// </summary>
    [TestMethod]
    public void Dimensions_WhenSampleNotesSheet_ShouldReportDeclaredUsedRange()
    {
        using var workbook = ExcelBinaryWorkbook.OpenRead(ExcelBinaryFixtures.OpenStream(ExcelBinaryFixtures.SampleBiff8));

        ExcelWorksheetDimensions dimensions = workbook.Worksheets[1].Dimensions;

        Assert.AreEqual(0, dimensions.FirstRowIndex);
        Assert.AreEqual(28, dimensions.RowCount);
        Assert.AreEqual(0, dimensions.FirstColumnIndex);
        Assert.AreEqual(12, dimensions.ColumnCount);
    }
}
