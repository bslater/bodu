// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentTriggerMatrixTests.IfWeekend.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class AdjustmentTriggerMatrixTests
{
    /// <summary>
    /// Verifies that the <see cref="AdjustmentTrigger.IfWeekend" /> trigger fires only when the anchor falls on a
    /// Saturday or Sunday.
    /// </summary>
    /// <param name="year">The Gregorian year whose 1 January anchor is resolved.</param>
    /// <param name="expectedFire">Whether the trigger is expected to fire.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2021, false)] // Friday
    [DataRow(2022, true)]  // Saturday
    [DataRow(2023, true)]  // Sunday
    [DataRow(2024, false)] // Monday
    [DataRow(2025, false)] // Wednesday
    [DataRow(2026, false)] // Thursday
    public void IfWeekend_WhenAnchorWeekday_ShouldFireOnlyOnSaturdayOrSunday(int year, bool expectedFire) =>
        AssertActivation(TriggerService, "weekend-h", year, expectedFire ? "if-weekend" : null);
}
