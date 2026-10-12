// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelSerialDateTests.ToDateTime.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

public partial class ExcelSerialDateTests
{
    /// <summary>
    /// Verifies that the date and time conversion preserves a fractional time-of-day component.
    /// </summary>
    [TestMethod]
    public void ToDateTime_WhenSerialHasFraction_ShouldPreserveTimeOfDay()
    {
        var result = ExcelSerialDate.ToDateTime(44929.5);

        Assert.AreEqual(new DateTime(2023, 1, 3, 12, 0, 0), result);
    }

    /// <summary>
    /// Verifies that a serial number is interpreted against the 1904 epoch when the 1904 date system is specified.
    /// </summary>
    [TestMethod]
    public void ToDateTime_WhenDateSystemIs1904_ShouldOffsetFrom1904Epoch()
    {
        var result = ExcelSerialDate.ToDateTime(0.0, ExcelDateSystem.Excel1904);

        Assert.AreEqual(new DateTime(1904, 1, 1), result);
    }

    /// <summary>
    /// Verifies that the same serial number resolves 1,462 days apart between the 1900 and 1904 date systems.
    /// </summary>
    [TestMethod]
    public void ToDateTime_WhenSerialIsSame_ShouldDiffer1462DaysBetweenSystems()
    {
        var system1900 = ExcelSerialDate.ToDateTime(44929.0, ExcelDateSystem.Excel1900);
        var system1904 = ExcelSerialDate.ToDateTime(44929.0, ExcelDateSystem.Excel1904);

        Assert.AreEqual(1462.0, (system1904 - system1900).TotalDays, 0.0);
    }

    /// <summary>
    /// Verifies that converting <see cref="double.NaN" />, <see cref="double.PositiveInfinity" /> or
    /// <see cref="double.NegativeInfinity" /> throws <see cref="ArgumentException" />, as the method documents for a
    /// serial outside the range of OLE Automation dates.
    /// </summary>
    [TestMethod]
    public void ToDateTime_WhenSerialIsNotFinite_ShouldThrowArgumentException()
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = ExcelSerialDate.ToDateTime(double.NaN);
        });
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = ExcelSerialDate.ToDateTime(double.PositiveInfinity);
        });
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = ExcelSerialDate.ToDateTime(double.NegativeInfinity);
        });
    }

    /// <summary>
    /// Verifies that a time of day whose millisecond count rounds up to a whole second carries into the seconds, so the
    /// serial 0.99998842592 is 1899-12-30 23:59:59.000 and never a 1,000-millisecond part.
    /// </summary>
    [TestMethod]
    public void ToDateTime_WhenMillisecondsRoundToAWholeSecond_ShouldCarryIntoTheSecond()
    {
        var result = ExcelSerialDate.ToDateTime(0.99998842592, ExcelDateSystem.Excel1900);

        Assert.AreEqual(new DateTime(1899, 12, 30, 23, 59, 59, 0), result);
    }

    /// <summary>
    /// Verifies that the serial 44484.7916666667 keeps its time of day, 2021-10-15 19:00:00.000.
    /// </summary>
    [TestMethod]
    public void ToDateTime_WhenSerialHasAFraction_ShouldKeepTheTimeOfDay()
    {
        var result = ExcelSerialDate.ToDateTime(44484.7916666667, ExcelDateSystem.Excel1900);

        Assert.AreEqual(new DateTime(2021, 10, 15, 19, 0, 0, 0), result);
    }

    /// <summary>
    /// Verifies that a time of day that rounds up to a whole day carries into the next day, never a 24:00 time: the
    /// serials 41640.999999999 and 41639.9999999999 are midnight of 2014-01-02 and of 2014-01-01.
    /// </summary>
    [TestMethod]
    public void ToDateTime_WhenTimeRoundsToMidnight_ShouldCarryIntoTheNextDay()
    {
        Assert.AreEqual(new DateTime(2014, 1, 2, 0, 0, 0, 0), ExcelSerialDate.ToDateTime(41640.999999999));
        Assert.AreEqual(new DateTime(2014, 1, 1, 0, 0, 0, 0), ExcelSerialDate.ToDateTime(41639.9999999999));
    }

    /// <summary>
    /// Verifies that the serials -700000 and 2958466 (the day after 9999-12-31) throw <see cref="ArgumentException" />,
    /// while 2958465.99999 is 9999-12-31 23:59:59.136.
    /// </summary>
    [TestMethod]
    public void ToDateTime_WhenSerialIsOutsideTheDateRange_ShouldThrowArgumentException()
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = ExcelSerialDate.ToDateTime(-700000);
        });
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = ExcelSerialDate.ToDateTime(2958466);
        });

        Assert.AreEqual(new DateTime(9999, 12, 31, 23, 59, 59, 136), ExcelSerialDate.ToDateTime(2958465.99999));
    }

    /// <summary>
    /// Verifies that the serials 41640, 36526 and 61 convert to dates on their weekdays: Wednesday 2014-01-01, Saturday
    /// 2000-01-01 and Thursday 1900-03-01.
    /// </summary>
    [TestMethod]
    public void ToDateTime_WhenSerialIsADate_ShouldGiveItsWeekday()
    {
        Assert.AreEqual(DayOfWeek.Wednesday, ExcelSerialDate.ToDateTime(41640).DayOfWeek);
        Assert.AreEqual(DayOfWeek.Saturday, ExcelSerialDate.ToDateTime(36526).DayOfWeek);
        Assert.AreEqual(DayOfWeek.Thursday, ExcelSerialDate.ToDateTime(61).DayOfWeek);
    }
}
