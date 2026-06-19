// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingDayExhaustionTests.NextWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public partial class WorkingDayExhaustionTests
{
    /// <summary>
    /// Verifies that the next-working-day search throws when no day of the week is a working day.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_WhenNoDayIsWorking_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new DateOnly(2025, 6, 2).NextWorkingDay(Service, "XX", WeekPattern.Empty);
        });
    }
}
