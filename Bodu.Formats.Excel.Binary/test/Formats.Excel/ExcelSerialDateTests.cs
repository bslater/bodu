// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelSerialDateTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Formats.Excel;

/// <summary>
/// Verifies that <see cref="ExcelSerialDate" /> converts Excel serial numbers to calendar dates.
/// </summary>
[TestClass]
public partial class ExcelSerialDateTests
{
    /// <summary>
    /// Verifies that a broad table of serial numbers converts to the expected calendar date in the 1900 date system,
    /// spanning the epoch boundary, the Unix epoch, and recent dates.
    /// </summary>
    /// <param name="serial">The Excel serial number.</param>
    /// <param name="year">The expected year.</param>
    /// <param name="month">The expected month.</param>
    /// <param name="day">The expected day.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DataRow(1.0, 1899, 12, 31)]
    [DataRow(2.0, 1900, 1, 1)]
    [DataRow(367.0, 1901, 1, 1)]
    [DataRow(25569.0, 1970, 1, 1)]
    [DataRow(44929.0, 2023, 1, 3)]
    [DataRow(46185.0, 2026, 6, 12)]
    public void FromSerialDate_WhenSerialInTable_ShouldReturnExpectedDate(double serial, int year, int month, int day)
    {
        DateOnly result = ExcelSerialDate.FromSerialDate(serial);

        Assert.AreEqual(new DateOnly(year, month, day), result);
    }
}
