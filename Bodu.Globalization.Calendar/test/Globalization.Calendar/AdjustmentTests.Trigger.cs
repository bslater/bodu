// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentTests.Trigger.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class AdjustmentTests
{
    /// <summary>
    /// Verifies that the if-weekday trigger fires on a weekday occurrence, emitting an observed 3 January 2026 that carries
    /// the 1 January actual date.
    /// </summary>
    [TestMethod]
    public void Trigger_IfWeekday_WhenWeekday_ShouldFireAndShift()
    {
        NotableDate weekday = Single(CreateResolver().Resolve(new DateOnly(2026, 1, 3), Territory), "weekday-shift");

        Assert.AreEqual(
            (true, (DateOnly?)new DateOnly(2026, 1, 1)),
            (weekday.IsObserved, weekday.ActualDate));
    }

    /// <summary>
    /// Verifies that the if-weekday trigger does not fire on a weekend occurrence, leaving 1 January 2022 (a Saturday)
    /// unobserved.
    /// </summary>
    [TestMethod]
    public void Trigger_IfWeekday_WhenWeekend_ShouldNotFire()
    {
        NotableDate weekend = Single(CreateResolver().Resolve(new DateOnly(2022, 1, 1), Territory), "weekday-shift");

        Assert.IsFalse(weekend.IsObserved);
    }
}
