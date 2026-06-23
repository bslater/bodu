// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelWorksheetDimensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

/// <summary>
/// Verifies the behavior of <see cref="ExcelWorksheetDimensions" />, the value-type used-range descriptor.
/// </summary>
[TestClass]
public class ExcelWorksheetDimensionsTests
{
    /// <summary>
    /// Verifies that the struct exposes the first-row, row-count, first-column, and column-count it was built with.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBuilt_ShouldExposeRangeComponents()
    {
        ExcelWorksheetDimensions dimensions = new(3, 10, 2, 5);

        Assert.AreEqual(3, dimensions.FirstRowIndex);
        Assert.AreEqual(10, dimensions.RowCount);
        Assert.AreEqual(2, dimensions.FirstColumnIndex);
        Assert.AreEqual(5, dimensions.ColumnCount);
    }

    /// <summary>
    /// Verifies that a default value reports zero counts, the value used when a sheet declares no <c>DIMENSIONS</c>
    /// record.
    /// </summary>
    [TestMethod]
    public void Default_WhenUninitialized_ShouldReportZeroCounts()
    {
        ExcelWorksheetDimensions dimensions = default;

        Assert.AreEqual(0, dimensions.FirstRowIndex);
        Assert.AreEqual(0, dimensions.RowCount);
        Assert.AreEqual(0, dimensions.FirstColumnIndex);
        Assert.AreEqual(0, dimensions.ColumnCount);
    }

    /// <summary>
    /// Verifies that two dimensions with the same components compare equal by value.
    /// </summary>
    [TestMethod]
    public void Equality_WhenSameComponents_ShouldBeEqual()
    {
        ExcelWorksheetDimensions first = new(0, 2186, 0, 68);
        ExcelWorksheetDimensions second = new(0, 2186, 0, 68);

        Assert.AreEqual(first, second);
        Assert.IsTrue(first == second);
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
    }
}
