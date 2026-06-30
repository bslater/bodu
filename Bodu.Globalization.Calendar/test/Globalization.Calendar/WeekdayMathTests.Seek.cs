// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekdayMathTests.Seek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public partial class WeekdayMathTests
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
}
