// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentPolicyTests.IsTriggered.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class AdjustmentPolicyTests
{
    /// <summary>
    /// Verifies that the nth-occurrence trigger fires for each Monday of June 2026 exactly when the configured ordinal
    /// names its seven-day block, or names the last occurrence and the Monday is the last of the month.
    /// </summary>
    /// <param name="day">The day of June 2026, a Monday.</param>
    /// <param name="ordinal">The configured week ordinal.</param>
    /// <param name="expected">Whether the trigger is expected to fire.</param>
    [TestMethod]
    [DataRow(1, WeekOrdinal.First, true)]
    [DataRow(8, WeekOrdinal.First, false)]
    [DataRow(8, WeekOrdinal.Second, true)]
    [DataRow(15, WeekOrdinal.Third, true)]
    [DataRow(22, WeekOrdinal.Fourth, true)]
    [DataRow(29, WeekOrdinal.Fifth, true)]
    [DataRow(22, WeekOrdinal.Fifth, false)]
    [DataRow(29, WeekOrdinal.Last, true)]
    [DataRow(22, WeekOrdinal.Last, false)]
    public void IsTriggered_WhenNthOccurrenceInMonth_ShouldFireOnTheConfiguredOccurrence(int day, WeekOrdinal ordinal, bool expected)
    {
        AdjustmentPolicy sut = NthOccurrencePolicy(ordinal, DayOfWeek.Monday);

        Assert.AreEqual(expected, sut.IsTriggered(new DateOnly(2026, 6, day), DayOfWeekSet.MondayToFriday));
    }

    /// <summary>
    /// Verifies that the nth-occurrence trigger never fires, and does not throw, when the policy carries a week ordinal
    /// that <see cref="WeekOrdinal" /> does not define, even on a date whose block such a value would otherwise name.
    /// </summary>
    /// <param name="ordinal">The undefined week ordinal.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(7)]
    [DataRow(int.MaxValue)]
    public void IsTriggered_WhenNthOccurrenceInMonthHasAnUndefinedOrdinal_ShouldNotFire(int ordinal)
    {
        AdjustmentPolicy sut = NthOccurrencePolicy((WeekOrdinal)ordinal, DayOfWeek.Monday);

        for (int day = 1; day <= 29; day += 7)
            Assert.IsFalse(sut.IsTriggered(new DateOnly(2026, 6, day), DayOfWeekSet.MondayToFriday), $"day {day}");
    }

    /// <summary>
    /// Verifies that the nth-occurrence trigger does not fire when the policy has no ordinal or no weekday, or when the
    /// date falls on another weekday than the first configured one.
    /// </summary>
    [TestMethod]
    public void IsTriggered_WhenNthOccurrenceInMonthIsIncompleteOrTheWeekdayDiffers_ShouldNotFire()
    {
        var firstMonday = new DateOnly(2026, 6, 1);

        Assert.IsFalse(NthOccurrencePolicy(null, DayOfWeek.Monday).IsTriggered(firstMonday, DayOfWeekSet.MondayToFriday));
        Assert.IsFalse(NthOccurrencePolicy(WeekOrdinal.First).IsTriggered(firstMonday, DayOfWeekSet.MondayToFriday));
        Assert.IsFalse(NthOccurrencePolicy(WeekOrdinal.First, DayOfWeek.Sunday, DayOfWeek.Monday).IsTriggered(firstMonday, DayOfWeekSet.MondayToFriday));
        Assert.IsFalse(NthOccurrencePolicy(WeekOrdinal.First, (DayOfWeek)9).IsTriggered(firstMonday, DayOfWeekSet.MondayToFriday));
    }
}
