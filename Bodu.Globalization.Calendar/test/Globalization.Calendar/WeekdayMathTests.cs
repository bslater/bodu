// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekdayMathTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Algorithms;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the <see cref="WeekdayMath" /> seek and nth-weekday helpers, including the strictly-before proximity, the
/// undefined-proximity fallback, and the last-occurrence ordinal.
/// </summary>
[TestClass]
public class WeekdayMathTests
{
    /// <summary>
    /// Verifies that a strictly-before seek lands on the previous occurrence of the target weekday.
    /// </summary>
    [TestMethod]
    public void Seek_WhenBefore_ShouldReturnPreviousWeekday()
    {
        // 2026-01-07 is a Wednesday; the strictly-previous Wednesday is 2025-12-31.
        DateOnly result = WeekdayMath.Seek(new DateOnly(2026, 1, 7), DayOfWeek.Wednesday, WeekdayProximity.Before);

        Assert.AreEqual(new DateOnly(2025, 12, 31), result);
    }

    /// <summary>
    /// Verifies that an undefined proximity returns the anchor unchanged.
    /// </summary>
    [TestMethod]
    public void Seek_WhenProximityUndefined_ShouldReturnAnchor()
    {
        var anchor = new DateOnly(2026, 1, 7);

        Assert.AreEqual(anchor, WeekdayMath.Seek(anchor, DayOfWeek.Monday, (WeekdayProximity)999));
    }

    /// <summary>
    /// Verifies that the last-occurrence ordinal returns the final occurrence of the weekday in the month.
    /// </summary>
    [TestMethod]
    public void NthWeekdayInMonth_WhenLast_ShouldReturnFinalOccurrence()
    {
        // The last Monday of January 2025 is 2025-01-27.
        DateOnly? result = WeekdayMath.NthWeekdayInMonth(2025, 1, DayOfWeek.Monday, WeekOrdinal.Last);

        Assert.AreEqual(new DateOnly(2025, 1, 27), result);
    }
}
