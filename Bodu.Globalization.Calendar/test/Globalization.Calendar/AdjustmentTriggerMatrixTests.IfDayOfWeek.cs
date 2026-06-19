// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentTriggerMatrixTests.IfDayOfWeek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class AdjustmentTriggerMatrixTests
{
    /// <summary>
    /// Verifies that the <see cref="AdjustmentTrigger.IfDayOfWeek" /> trigger configured with a single Sunday weekday
    /// fires only when the anchor is a Sunday.
    /// </summary>
    /// <param name="year">The Gregorian year whose 1 January anchor is resolved.</param>
    /// <param name="expectedFire">Whether the trigger is expected to fire.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2021, false)] // Friday
    [DataRow(2022, false)] // Saturday
    [DataRow(2023, true)]  // Sunday
    [DataRow(2024, false)] // Monday
    [DataRow(2026, false)] // Thursday
    public void IfDayOfWeek_WhenConfiguredSunday_ShouldFireOnlyOnSundays(int year, bool expectedFire) =>
        AssertActivation(TriggerService, "sunday-h", year, expectedFire ? "if-sunday" : null);

    /// <summary>
    /// Verifies that the <see cref="AdjustmentTrigger.IfDayOfWeek" /> trigger configured with both weekend weekdays
    /// fires on either Saturday or Sunday, matching the canonical mondayisation weekday set.
    /// </summary>
    /// <param name="year">The Gregorian year whose 1 January anchor is resolved.</param>
    /// <param name="expectedFire">Whether the trigger is expected to fire.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2021, false)] // Friday
    [DataRow(2022, true)]  // Saturday
    [DataRow(2023, true)]  // Sunday
    [DataRow(2024, false)] // Monday
    public void IfDayOfWeek_WhenConfiguredSaturdayAndSunday_ShouldFireOnEitherWeekendDay(int year, bool expectedFire) =>
        AssertActivation(TriggerService, "sat-sun-h", year, expectedFire ? "if-sat-or-sun" : null);
}
