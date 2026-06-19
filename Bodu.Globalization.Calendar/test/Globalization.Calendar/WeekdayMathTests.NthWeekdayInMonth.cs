// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekdayMathTests.NthWeekdayInMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Algorithms;

namespace Bodu.Globalization.Calendar;

public partial class WeekdayMathTests
{
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
