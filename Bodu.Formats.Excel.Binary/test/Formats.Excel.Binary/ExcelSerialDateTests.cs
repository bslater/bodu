// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelSerialDateTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Binary;

/// <summary>
/// Verifies that <see cref="ExcelSerialDate" /> converts Excel serial numbers to calendar dates.
/// </summary>
[TestClass]
public class ExcelSerialDateTests
{
    /// <summary>
    /// Verifies that a serial number converts to the expected calendar date.
    /// </summary>
    /// <param name="serial">The Excel serial number.</param>
    /// <param name="year">The expected year.</param>
    /// <param name="month">The expected month.</param>
    /// <param name="day">The expected day.</param>
    [DataRow(1.0, 1899, 12, 31)]
    [DataRow(44929.0, 2023, 1, 3)]
    [DataRow(46185.0, 2026, 6, 12)]
    [TestMethod]
    public void FromSerialDate_WhenGivenSerial_ShouldReturnExpectedDate(double serial, int year, int month, int day)
    {
        DateOnly result = ExcelSerialDate.FromSerialDate(serial);

        Assert.AreEqual(new DateOnly(year, month, day), result);
    }

    /// <summary>
    /// Verifies that the date and time conversion preserves a fractional time-of-day component.
    /// </summary>
    [TestMethod]
    public void ToDateTime_WhenSerialHasFraction_ShouldPreserveTimeOfDay()
    {
        var result = ExcelSerialDate.ToDateTime(44929.5);

        Assert.AreEqual(new DateTime(2023, 1, 3, 12, 0, 0), result);
    }
}
