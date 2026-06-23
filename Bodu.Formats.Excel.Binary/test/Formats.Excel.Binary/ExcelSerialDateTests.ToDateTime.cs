// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelSerialDateTests.ToDateTime.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Binary;

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
}
