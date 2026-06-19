// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentActionMatrixTests.AddDays.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class AdjustmentActionMatrixTests
{
    /// <summary>
    /// Verifies that <see cref="AdjustmentAction.AddDays" /> shifts the 1 July 2026 anchor by the configured signed
    /// offset, including negative offsets and offsets that cross month and year boundaries.
    /// </summary>
    /// <param name="days">The signed day delta.</param>
    /// <param name="expectedYear">The expected emitted year.</param>
    /// <param name="expectedMonth">The expected emitted month.</param>
    /// <param name="expectedDay">The expected emitted day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(1, 2026, 7, 2)]
    [DataRow(7, 2026, 7, 8)]
    [DataRow(31, 2026, 8, 1)]      // Cross month forward
    [DataRow(180, 2026, 12, 28)]   // Half-year forward
    [DataRow(-1, 2026, 6, 30)]     // Cross month backward
    [DataRow(-7, 2026, 6, 24)]
    [DataRow(-180, 2026, 1, 2)]    // Half-year backward
    [DataRow(365, 2027, 7, 1)]     // Forward into the next year
    [DataRow(-365, 2025, 7, 1)]    // Backward into the prior year
    public void AddDays_WhenAlwaysTrigger_ShouldShiftAnchorBySignedOffset(int days, int expectedYear, int expectedMonth, int expectedDay)
    {
        INotableDateService service = AddDaysService(days);
        DateOnly expected = new(expectedYear, expectedMonth, expectedDay);

        // Scan the full span between the anchor and the shifted date so the emitted occurrence is always visible.
        DateOnly anchor = new(2026, 7, 1);
        DateOnly start = expected < anchor ? expected : anchor;
        DateOnly end = expected > anchor ? expected : anchor;

        NotableDate match = Single(service, "probe", start, end);

        Assert.AreEqual(expected, match.Date);
        Assert.AreEqual(new DateOnly(2026, 7, 1), match.ActualDate);
        Assert.IsTrue(match.IsObserved);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentAction.AddDays" /> with a zero delta leaves the date unchanged while the
    /// observed-only emission still suppresses no occurrence and reports the policy id on the single emitted date.
    /// </summary>
    [TestMethod]
    public void AddDays_WhenZeroOffset_ShouldLeaveDateButReportPolicy()
    {
        NotableDate match = Single(AddDaysService(0), "probe", new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 1));

        Assert.AreEqual(
            (new DateOnly(2026, 7, 1), (string?)"shift"),
            (match.Date, match.AdjustmentPolicyId));
    }
}
