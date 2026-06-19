// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonWorkingDayTriggerTests.IfNonWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NonWorkingDayTriggerTests
{
    /// <summary>
    /// Verifies that the unadjusted holiday of a colliding pair keeps the contested working day, emitting on its actual
    /// date. Collide A has no adjustment, so it remains on Thursday 1 January 2026.
    /// </summary>
    [TestMethod]
    public void IfNonWorkingDay_WhenCollidingWithAnotherHoliday_ShouldKeepFirstHolidayOnContestedDay()
    {
        IReadOnlyList<NotableDate> thursday = Build().Resolve(new DateOnly(2026, 1, 1), Territory);

        Assert.Contains(n => n.NotableDateId == "collide-a" && !n.IsObserved, thursday, "Collide A on its actual day");
    }

    /// <summary>
    /// Verifies that the adjusted holiday of a colliding pair leaves the contested working day, because the non-working-day
    /// trigger fires when the day is already claimed. Collide B is absent from Thursday 1 January 2026.
    /// </summary>
    [TestMethod]
    public void IfNonWorkingDay_WhenCollidingWithAnotherHoliday_ShouldMoveSecondHolidayOffContestedDay()
    {
        IReadOnlyList<NotableDate> thursday = Build().Resolve(new DateOnly(2026, 1, 1), Territory);

        Assert.DoesNotContain(n => n.NotableDateId == "collide-b", thursday, "Collide B moved off the contested day");
    }

    /// <summary>
    /// Verifies that the adjusted holiday of a colliding pair is observed on the next free working day, carrying its actual
    /// date. Collide B (Thursday 1 January 2026) is observed on Friday 2 January.
    /// </summary>
    [TestMethod]
    public void IfNonWorkingDay_WhenCollidingWithAnotherHoliday_ShouldObserveOnNextWorkingDay()
    {
        NotableDate observed = Build().Resolve(new DateOnly(2026, 1, 2), Territory).Single(n => n.NotableDateId == "collide-b");

        Assert.AreEqual(
            (true, (DateOnly?)new DateOnly(2026, 1, 1)),
            (observed.IsObserved, observed.ActualDate));
    }

    /// <summary>
    /// Verifies that the non-working-day trigger suppresses the weekend actual date of a holiday emitted observed-only.
    /// The Saturday 3 January 2026 occurrence does not appear on its actual date.
    /// </summary>
    [TestMethod]
    public void IfNonWorkingDay_WhenHolidayFallsOnWeekend_ShouldSuppressWeekendActual()
    {
        Assert.DoesNotContain(
            n => n.NotableDateId == "weekend-holiday", Build().Resolve(new DateOnly(2026, 1, 3), Territory),
            "Saturday actual suppressed");
    }

    /// <summary>
    /// Verifies that the non-working-day trigger moves a weekend holiday to the next working day, skipping the weekend,
    /// carrying its actual date. Saturday 3 January 2026 is observed on Monday 5 January.
    /// </summary>
    [TestMethod]
    public void IfNonWorkingDay_WhenHolidayFallsOnWeekend_ShouldObserveOnNextWorkingDay()
    {
        NotableDate observed = Build().Resolve(new DateOnly(2026, 1, 5), Territory).Single(n => n.NotableDateId == "weekend-holiday");

        Assert.AreEqual(
            (true, (DateOnly?)new DateOnly(2026, 1, 3)),
            (observed.IsObserved, observed.ActualDate));
    }

    /// <summary>
    /// Verifies that the non-working-day trigger does not fire for a lone holiday on a working day, so the holiday is
    /// emitted on its actual date.
    /// </summary>
    [TestMethod]
    public void IfNonWorkingDay_WhenLoneHolidayOnWorkingDay_ShouldEmitActualDate()
    {
        NotableDate actual = Build().Resolve(new DateOnly(2026, 1, 8), Territory).Single(n => n.NotableDateId == "lone-working");

        Assert.AreEqual(
            (false, new DateOnly(2026, 1, 8)),
            (actual.IsObserved, actual.Date));
    }
}
