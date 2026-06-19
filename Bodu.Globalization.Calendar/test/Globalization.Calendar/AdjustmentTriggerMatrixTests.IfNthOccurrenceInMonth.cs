// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentTriggerMatrixTests.IfNthOccurrenceInMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class AdjustmentTriggerMatrixTests
{
    /// <summary>
    /// Verifies that <see cref="AdjustmentTrigger.IfNthOccurrenceInMonth" /> fires only when the occurrence falls in the
    /// seven-day block the ordinal selects (days 1-7 = First, 8-14 = Second, …) and shares the configured weekday. Each
    /// row resolves a Monday in June 2026 (days 1, 8, 15, 22, 29 are Mondays) against a single weekday set to Monday.
    /// </summary>
    /// <param name="ordinal">The configured week ordinal.</param>
    /// <param name="strategyDay">The Monday day-of-June the holiday resolves to.</param>
    /// <param name="expectedFire">Whether the trigger is expected to fire.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("First", 1, true)]    // 1 Jun → First block
    [DataRow("First", 8, false)]   // 8 Jun → Second block
    [DataRow("Second", 8, true)]   // 8 Jun → Second block
    [DataRow("Second", 1, false)]  // 1 Jun → First block
    [DataRow("Third", 15, true)]   // 15 Jun → Third block
    [DataRow("Fourth", 22, true)]  // 22 Jun → Fourth block
    [DataRow("Fifth", 29, true)]   // 29 Jun → Fifth block
    [DataRow("Fifth", 1, false)]   // 1 Jun → First block
    [DataRow("Last", 29, true)]    // 29 Jun is the last Monday of June 2026
    [DataRow("Last", 22, false)]   // 22 Jun is not the last Monday
    public void IfNthOccurrenceInMonth_WhenOrdinalBlockMatches_ShouldFireOnlyForMatchingBlock(string ordinal, int strategyDay, bool expectedFire)
    {
        NotableDate match = NthOccurrenceService("Monday", ordinal, strategyDay)
            .Resolve(new DateRange(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)), Territory)
            .Single(r => r.NotableDateId == "probe");

        Assert.AreEqual(expectedFire, match.IsObserved, $"{ordinal} block, day {strategyDay}");
        Assert.AreEqual(expectedFire ? "nth" : null, match.AdjustmentPolicyId);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentTrigger.IfNthOccurrenceInMonth" /> does not fire when the occurrence falls in
    /// the correct ordinal block but on a different weekday than the one configured. 1 June 2026 is a Monday, so a
    /// trigger configured for the first Sunday does not fire even though the day is in the first-week block.
    /// </summary>
    [TestMethod]
    public void IfNthOccurrenceInMonth_WhenWeekdayDiffers_ShouldNotFire()
    {
        NotableDate match = NthOccurrenceService("Sunday", "First", 1)
            .Resolve(new DateRange(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)), Territory)
            .Single(r => r.NotableDateId == "probe");

        Assert.AreEqual(
            (false, (string?)null),
            (match.IsObserved, match.AdjustmentPolicyId));
    }
}
