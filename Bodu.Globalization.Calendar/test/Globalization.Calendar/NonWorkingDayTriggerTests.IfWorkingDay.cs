// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonWorkingDayTriggerTests.IfWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NonWorkingDayTriggerTests
{
    /// <summary>
    /// Verifies that the working-day trigger suppresses the actual working-day date of a holiday emitted observed-only.
    /// The Thursday 15 January 2026 occurrence does not appear on its actual date.
    /// </summary>
    [TestMethod]
    public void IfWorkingDay_WhenHolidayOnWorkingDay_ShouldSuppressActual()
    {
        Assert.DoesNotContain(
            n => n.NotableDateId == "working-shift", Build().Resolve(new DateOnly(2026, 1, 15), Territory),
            "actual Thursday suppressed by observed-only");
    }

    /// <summary>
    /// Verifies that the working-day trigger shifts a lone holiday on a working day by the configured number of days,
    /// carrying its actual date. Thursday 15 January 2026 is observed two days later on 17 January.
    /// </summary>
    [TestMethod]
    public void IfWorkingDay_WhenHolidayOnWorkingDay_ShouldShiftOccurrence()
    {
        NotableDate observed = Build().Resolve(new DateOnly(2026, 1, 17), Territory).Single(n => n.NotableDateId == "working-shift");

        Assert.AreEqual(
            (true, (DateOnly?)new DateOnly(2026, 1, 15)),
            (observed.IsObserved, observed.ActualDate));
    }

    /// <summary>
    /// Verifies that the working-day trigger does not fire for a holiday on a weekend, so the holiday is emitted on its
    /// actual date.
    /// </summary>
    [TestMethod]
    public void IfWorkingDay_WhenHolidayOnWeekend_ShouldEmitActualDate()
    {
        NotableDate actual = Build().Resolve(new DateOnly(2026, 1, 10), Territory).Single(n => n.NotableDateId == "weekend-no-shift");

        Assert.AreEqual(
            (false, new DateOnly(2026, 1, 10)),
            (actual.IsObserved, actual.Date));
    }
}
