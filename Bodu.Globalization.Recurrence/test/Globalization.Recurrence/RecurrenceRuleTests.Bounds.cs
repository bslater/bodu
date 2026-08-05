// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRuleTests.Bounds.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceRuleTests
{
    /// <summary>
    /// Verifies that a rule that can never match — 30 February yearly — enumerates empty rather than looping, with
    /// the end of the representable calendar as the search bound.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenRuleCanNeverMatch_ShouldEnumerateEmpty()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=YEARLY;BYMONTH=2;BYMONTHDAY=30");

        DateTime[] actual = rule.GetOccurrences(new DateTime(2026, 1, 1)).ToArray();

        Assert.AreEqual(0, actual.Length);
    }

    /// <summary>
    /// Verifies that the point queries answer <see langword="null" /> for a rule that can never match, in both
    /// directions, instead of scanning unboundedly.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenRuleCanNeverMatch_ShouldReturnNullBothDirections()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=YEARLY;BYMONTH=2;BYMONTHDAY=30");
        var start = new DateTime(2026, 1, 1);

        Assert.IsNull(rule.GetNextOccurrence(start, new DateTime(2026, 6, 1)));
        Assert.IsNull(rule.GetPreviousOccurrence(start, new DateTime(2030, 6, 1)));
    }

    /// <summary>
    /// Verifies that a rule whose occurrences all precede the window enumerates the window empty, and that a window
    /// preceding the series start is empty.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenWindowMissesSeries_ShouldEnumerateEmpty()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY;COUNT=3");
        var start = new DateTime(2026, 1, 1, 9, 0, 0);

        Assert.AreEqual(0, rule.GetOccurrences(start, new DateTime(2026, 2, 1), new DateTime(2026, 3, 1)).Count());
        Assert.AreEqual(0, rule.GetOccurrences(start, new DateTime(2025, 1, 1), new DateTime(2025, 12, 31)).Count());
    }
}
