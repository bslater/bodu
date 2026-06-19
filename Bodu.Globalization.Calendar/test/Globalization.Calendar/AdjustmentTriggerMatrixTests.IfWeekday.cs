// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentTriggerMatrixTests.IfWeekday.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class AdjustmentTriggerMatrixTests
{
    /// <summary>
    /// Verifies that the <see cref="AdjustmentTrigger.IfWeekday" /> trigger fires only on Monday through Friday.
    /// </summary>
    /// <param name="year">The Gregorian year whose 1 January anchor is resolved.</param>
    /// <param name="expectedFire">Whether the trigger is expected to fire.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2021, true)]  // Friday
    [DataRow(2022, false)] // Saturday
    [DataRow(2023, false)] // Sunday
    [DataRow(2024, true)]  // Monday
    [DataRow(2025, true)]  // Wednesday
    [DataRow(2026, true)]  // Thursday
    public void IfWeekday_WhenAnchorWeekday_ShouldFireOnlyOnMondayThroughFriday(int year, bool expectedFire) =>
        AssertActivation(TriggerService, "weekday-h", year, expectedFire ? "if-weekday" : null);
}
