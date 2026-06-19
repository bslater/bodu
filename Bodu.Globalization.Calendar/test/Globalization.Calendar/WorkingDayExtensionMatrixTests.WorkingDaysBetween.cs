// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingDayExtensionMatrixTests.WorkingDaysBetween.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class WorkingDayExtensionMatrixTests
{
    /// <summary>
    /// Verifies the inclusive working-day count across single-day, full-week, multi-week and reversed-boundary ranges.
    /// </summary>
    /// <param name="startYear">The start year.</param>
    /// <param name="startMonth">The start month.</param>
    /// <param name="startDay">The start day.</param>
    /// <param name="endYear">The end year.</param>
    /// <param name="endMonth">The end month.</param>
    /// <param name="endDay">The end day.</param>
    /// <param name="expected">The expected inclusive working-day count.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(WorkingDaysBetweenRows))]
    public void WorkingDaysBetween_WhenScannedAcrossRanges_ShouldReturnExpectedCount(int startYear, int startMonth, int startDay, int endYear, int endMonth, int endDay, int expected)
    {
        int actual = new DateOnly(startYear, startMonth, startDay)
            .WorkingDaysBetween(new DateOnly(endYear, endMonth, endDay), Service, "XX");

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that swapped boundaries produce a count equal to the in-order range, confirming the count is symmetric.
    /// </summary>
    [TestMethod]
    public void WorkingDaysBetween_WhenBoundariesReversed_ShouldReturnSymmetricCount()
    {
        INotableDateService service = Service;

        int forward = new DateOnly(2026, 1, 5).WorkingDaysBetween(new DateOnly(2026, 1, 11), service, "XX");
        int reversed = new DateOnly(2026, 1, 11).WorkingDaysBetween(new DateOnly(2026, 1, 5), service, "XX");

        Assert.AreEqual(forward, reversed);
    }

    /// <summary>
    /// Verifies that a non-working holiday inside the range is excluded from the working-day count. The fixture holiday
    /// on 1 January 2026 falls inside 31 December 2025 to 7 January 2026 (4 weekday working days remain).
    /// </summary>
    [TestMethod]
    public void WorkingDaysBetween_WhenHolidayInRange_ShouldExcludeIt()
    {
        // 2025-12-31 Wed, 2026-01-01 Thu (holiday), 01-02 Fri, 01-03 Sat, 01-04 Sun, 01-05 Mon, 01-06 Tue, 01-07 Wed.
        // Working days: Wed 31, Fri 2, Mon 5, Tue 6, Wed 7 = 5; the holiday on Thursday is excluded.
        int count = new DateOnly(2025, 12, 31).WorkingDaysBetween(new DateOnly(2026, 1, 7), Service, "XX");

        Assert.AreEqual(5, count);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> service throws <see cref="ArgumentNullException" /> from
    /// <see cref="NotableDateOnlyExtensions.WorkingDaysBetween" />.
    /// </summary>
    [TestMethod]
    public void WorkingDaysBetween_WhenServiceIsNull_ShouldThrow()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2026, 1, 5).WorkingDaysBetween(new DateOnly(2026, 1, 11), null!, "XX");
        });
    }
}
