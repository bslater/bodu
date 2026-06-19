// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentTests.Action.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class AdjustmentTests
{
    /// <summary>
    /// Verifies that an always trigger with an add-days action shifts the occurrence forward by the configured day,
    /// emitting an observed 2 January 2026 that carries the 1 January actual date.
    /// </summary>
    [TestMethod]
    public void Action_AddDays_WithAlwaysTrigger_WhenShifted_ShouldEmitObservedWithActualDate()
    {
        NotableDate observed = Single(CreateResolver().Resolve(new DateOnly(2026, 1, 2), Territory), "always-shift");

        Assert.AreEqual(
            (true, (DateOnly?)new DateOnly(2026, 1, 1)),
            (observed.IsObserved, observed.ActualDate));
    }

    /// <summary>
    /// Verifies that the add-days shift, emitted observed-only, suppresses the unshifted 1 January actual date.
    /// </summary>
    [TestMethod]
    public void Action_AddDays_WithAlwaysTrigger_WhenActualDate_ShouldSuppressActual()
    {
        Assert.AreEqual(0, Count(CreateResolver().Resolve(new DateOnly(2026, 1, 1), Territory), "always-shift"));
    }

    /// <summary>
    /// Verifies that the move-to-previous-weekday action moves a Sunday occurrence back to the preceding Friday, across
    /// a year boundary.
    /// </summary>
    [TestMethod]
    public void Action_MoveToPreviousWeekday_MovesSundayBackToFriday()
    {
        NotableDate observed = Single(CreateResolver().Resolve(new DateOnly(2022, 12, 30), Territory), "prev-friday");

        Assert.AreEqual(
            (true, new DateOnly(2022, 12, 30), (DateOnly?)new DateOnly(2023, 1, 1)),
            (observed.IsObserved, observed.Date, observed.ActualDate));
    }

    /// <summary>
    /// Verifies that the move-to-next-working-day action moves a weekend 1 January occurrence forward to the following
    /// Monday. 1 January 2022 (a Saturday) is observed on Monday 3 January; 1 January 2023 (a Sunday) on Monday 2 January.
    /// </summary>
    /// <param name="year">The observed Monday year.</param>
    /// <param name="month">The observed Monday month.</param>
    /// <param name="day">The observed Monday day.</param>
    [TestMethod]
    [DataRow(2022, 1, 3)]  // 1 Jan 2022 Saturday -> Monday 3 Jan
    [DataRow(2023, 1, 2)]  // 1 Jan 2023 Sunday -> Monday 2 Jan
    public void Action_MoveToNextWorkingDay_WhenWeekendOccurrence_ShouldMoveToMonday(int year, int month, int day)
    {
        DateOnly monday = new(year, month, day);

        Assert.AreEqual(monday, Single(CreateResolver().Resolve(monday, Territory), "next-working-day").Date);
    }

    /// <summary>
    /// Verifies that the move-to-previous-working-day action moves a Saturday occurrence back to the preceding Friday.
    /// </summary>
    [TestMethod]
    public void Action_MoveToPreviousWorkingDay_MovesSaturdayBackToFriday()
    {
        NotableDate observed = Single(CreateResolver().Resolve(new DateOnly(2021, 12, 31), Territory), "prev-working-day");

        Assert.AreEqual(
            (new DateOnly(2021, 12, 31), (DateOnly?)new DateOnly(2022, 1, 1)),
            (observed.Date, observed.ActualDate));
    }
}
