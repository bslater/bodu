// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingDayExhaustionTests.NextNonWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public partial class WorkingDayExhaustionTests
{
    /// <summary>
    /// Verifies that the next-non-working-day search throws when every day is a working day and no holiday applies.
    /// </summary>
    [TestMethod]
    public void NextNonWorkingDay_WhenEveryDayIsWorking_ShouldThrowInvalidOperationException()
    {
        var allWorking = new WeekPattern(
            DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new DateOnly(2025, 6, 2).NextNonWorkingDay(Service, "XX", allWorking);
        });
    }
}
