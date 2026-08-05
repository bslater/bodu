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
    /// Verifies that a weekly rule whose <c>BYSETPOS</c> selects a position the candidate set never reaches
    /// enumerates empty, rather than overflowing the calendar while scanning to the representable bound.
    /// </summary>
    /// <remarks>
    /// The equivalent rules crashed or hung in ical.net (#856), ical4j (#851), and lib-recur (#137): the search runs
    /// to the end of the representable calendar and the final week expansion must not step past
    /// <see cref="DateTime.MaxValue" />.
    /// </remarks>
    [TestMethod]
    public void GetOccurrences_WhenSetPosNeverSelects_ShouldEnumerateEmptyWithoutOverflowing()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=MO,TU,WE;BYSETPOS=4");

        DateTime[] actual = rule.GetOccurrences(new DateTime(2026, 1, 1)).ToArray();

        Assert.AreEqual(0, actual.Length);
    }

    /// <summary>
    /// Verifies that a yearly rule whose per-period candidate set is always empty enumerates empty rather than
    /// scanning without bound.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenYearlySetPosNeverSelects_ShouldEnumerateEmpty()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=YEARLY;BYMONTH=11;BYDAY=4TH;BYSETPOS=4");

        DateTime[] actual = rule.GetOccurrences(new DateTime(2024, 11, 28, 8, 0, 0)).ToArray();

        Assert.AreEqual(0, actual.Length);
    }

    /// <summary>
    /// Verifies that a monthly rule whose day rules can never intersect enumerates empty; the second Sunday always
    /// falls between the 8th and the 14th, so it is never the 1st.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenDayRulesCannotIntersect_ShouldEnumerateEmpty()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=MONTHLY;BYMONTHDAY=1;BYDAY=2SU");

        DateTime[] actual = rule.GetOccurrences(new DateTime(2022, 1, 1)).ToArray();

        Assert.AreEqual(0, actual.Length);
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
