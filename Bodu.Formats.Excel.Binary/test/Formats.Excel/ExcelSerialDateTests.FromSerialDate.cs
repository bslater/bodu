// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelSerialDateTests.FromSerialDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

public partial class ExcelSerialDateTests
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
    /// Verifies that serials before March 1900 follow the OLE Automation dates rather than the dates Excel shows: 1 is
    /// 1899-12-31, 60 is 1900-02-28 and 61 is 1900-03-01, the first date on which the two agree.
    /// </summary>
    [TestMethod]
    public void FromSerialDate_WhenSerialIsBeforeMarch1900_ShouldFollowOleAutomationDates()
    {
        Assert.AreEqual(new DateOnly(1899, 12, 31), ExcelSerialDate.FromSerialDate(1.0, ExcelDateSystem.Excel1900));
        Assert.AreEqual(new DateOnly(1900, 2, 28), ExcelSerialDate.FromSerialDate(60.0, ExcelDateSystem.Excel1900));
        Assert.AreEqual(new DateOnly(1900, 3, 1), ExcelSerialDate.FromSerialDate(61.0, ExcelDateSystem.Excel1900));
    }
}
