// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentTriggerMatrixTests.IfLeapYear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class AdjustmentTriggerMatrixTests
{
    /// <summary>
    /// Verifies that the <see cref="AdjustmentTrigger.IfLeapYear" /> trigger fires only in Gregorian leap years,
    /// including the century rule (divisible by 100 is common unless also divisible by 400).
    /// </summary>
    /// <param name="year">The Gregorian year whose 1 January anchor is resolved.</param>
    /// <param name="expectedFire">Whether the trigger is expected to fire.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2024, true)]  // Leap
    [DataRow(2025, false)] // Common
    [DataRow(2026, false)] // Common
    [DataRow(2027, false)] // Common
    [DataRow(2028, true)]  // Leap
    [DataRow(2000, true)]  // Divisible by 400 → leap
    [DataRow(2100, false)] // Divisible by 100 but not 400 → common
    [DataRow(2400, true)]  // Divisible by 400 → leap
    public void IfLeapYear_WhenAnchorYear_ShouldFireOnlyInLeapYears(int year, bool expectedFire) =>
        AssertActivation(TriggerService, "leap-h", year, expectedFire ? "if-leap-year" : null);
}
