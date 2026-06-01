// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.MonthBoundary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.GetFirstDateOfMonth(int, int)" /> returns the first day of the supplied month and year.
    /// </summary>
    [TestMethod]
    [DataRow(2024, 1, 2024, 1, 1)]
    [DataRow(2024, 12, 2024, 12, 1)]
    [DataRow(2000, 2, 2000, 2, 1)]
    public void GetFirstDateOfMonth_FromYearMonth_ShouldReturnFirstDayOfMonth(int year, int month, int expY, int expM, int expD)
    {
        DateOnly actual = DateOnlyExtensions.GetFirstDateOfMonth(year, month);
        Assert.AreEqual(new DateOnly(expY, expM, expD), actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.GetFirstDateOfMonth(int, int)" /> throws when the month is outside [1, 12].
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(13)]
    public void GetFirstDateOfMonth_FromYearMonth_WhenMonthIsOutOfRange_ShouldThrowExactly(int month)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnlyExtensions.GetFirstDateOfMonth(2024, month);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.GetLastDateOfMonth(int, int)" /> returns the last day of the supplied month and year,
    /// handling leap-year February correctly.
    /// </summary>
    [TestMethod]
    [DataRow(2024, 1, 2024, 1, 31)]
    [DataRow(2024, 2, 2024, 2, 29)] // leap year
    [DataRow(2023, 2, 2023, 2, 28)] // non-leap year
    [DataRow(2024, 4, 2024, 4, 30)]
    [DataRow(2024, 12, 2024, 12, 31)]
    public void GetLastDateOfMonth_FromYearMonth_ShouldReturnLastDayOfMonth(int year, int month, int expY, int expM, int expD)
    {
        DateOnly actual = DateOnlyExtensions.GetLastDateOfMonth(year, month);
        Assert.AreEqual(new DateOnly(expY, expM, expD), actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.GetLastDateOfMonth(int, int)" /> throws when the month is outside [1, 12].
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(13)]
    public void GetLastDateOfMonth_FromYearMonth_WhenMonthIsOutOfRange_ShouldThrowExactly(int month)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnlyExtensions.GetLastDateOfMonth(2024, month);
        });
    }

}
