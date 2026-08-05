// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRuleTests.GetNextOccurrence.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceRuleTests
{
    /// <summary>
    /// Verifies that the next occurrence of a daily rule is the earliest expansion after the query instant.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenDaily_ShouldReturnEarliestLaterOccurrence()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY");
        var start = new DateTime(2026, 1, 1, 9, 0, 0);

        DateTime? next = rule.GetNextOccurrence(start, new DateTime(2026, 1, 4, 12, 0, 0));

        Assert.AreEqual(new DateTime(2026, 1, 5, 9, 0, 0), next);
    }

    /// <summary>
    /// Verifies that a query exactly on an occurrence is excluded by default and returned with the inclusive flag.
    /// </summary>
    /// <remarks>
    /// This is the flag the documented due-ness recipe turns on, so it is asserted in both directions rather than
    /// only in its default position.
    /// </remarks>
    [TestMethod]
    public void GetNextOccurrence_WhenAfterEqualsOccurrence_ShouldHonorInclusiveFlag()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY");
        var start = new DateTime(2026, 1, 1, 9, 0, 0);
        var occurrence = new DateTime(2026, 1, 3, 9, 0, 0);

        Assert.AreEqual(new DateTime(2026, 1, 4, 9, 0, 0), rule.GetNextOccurrence(start, occurrence));
        Assert.AreEqual(occurrence, rule.GetNextOccurrence(start, occurrence, inclusive: true));
    }

    /// <summary>
    /// Verifies that the series start is answered for a query before it, and is itself subject to the inclusive flag.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenAfterPrecedesSeriesStart_ShouldReturnSeriesStart()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY");
        var start = new DateTime(2026, 1, 1, 9, 0, 0);

        Assert.AreEqual(start, rule.GetNextOccurrence(start, start.AddDays(-1)));
        Assert.AreEqual(start.AddDays(1), rule.GetNextOccurrence(start, start));
        Assert.AreEqual(start, rule.GetNextOccurrence(start, start, inclusive: true));
    }

    /// <summary>
    /// Verifies that a weekly rule skips to the next selected weekday rather than the next calendar week.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenWeeklyWithByDay_ShouldReturnNextSelectedWeekday()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=MO,WE,FR");
        var start = new DateTime(2026, 1, 5, 9, 0, 0);   // a Monday

        // Queried on the Monday, the next selection is that week's Wednesday.
        Assert.AreEqual(new DateTime(2026, 1, 7, 9, 0, 0), rule.GetNextOccurrence(start, start));

        // Queried after the Friday, the next selection is the following Monday.
        Assert.AreEqual(
            new DateTime(2026, 1, 12, 9, 0, 0),
            rule.GetNextOccurrence(start, new DateTime(2026, 1, 9, 12, 0, 0)));
    }

    /// <summary>
    /// Verifies that a monthly rule anchored on a day that some months lack skips those months rather than clamping
    /// the date back into them.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenMonthlyOnThirtyFirst_ShouldSkipShorterMonths()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=MONTHLY;BYMONTHDAY=31");
        var start = new DateTime(2026, 1, 1, 0, 0, 0);

        // February, April, June, September and November have no 31st, so January is followed by March.
        Assert.AreEqual(
            new DateTime(2026, 3, 31, 0, 0, 0),
            rule.GetNextOccurrence(start, new DateTime(2026, 1, 31, 0, 0, 0)));
    }

    /// <summary>
    /// Verifies that a count-bounded rule reports no next occurrence once its final occurrence has passed.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenAfterCountBoundedSeriesEnds_ShouldReturnNull()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY;COUNT=3");
        var start = new DateTime(2026, 1, 1, 9, 0, 0);

        Assert.AreEqual(new DateTime(2026, 1, 3, 9, 0, 0), rule.GetNextOccurrence(start, new DateTime(2026, 1, 2, 12, 0, 0)));
        Assert.IsNull(rule.GetNextOccurrence(start, new DateTime(2026, 1, 3, 12, 0, 0)));
    }

    /// <summary>
    /// Verifies that an <c>UNTIL</c>-bounded rule reports no next occurrence past its bound.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenAfterUntilBound_ShouldReturnNull()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY;UNTIL=20260103T090000");
        var start = new DateTime(2026, 1, 1, 9, 0, 0);

        Assert.AreEqual(new DateTime(2026, 1, 3, 9, 0, 0), rule.GetNextOccurrence(start, new DateTime(2026, 1, 2, 12, 0, 0)));
        Assert.IsNull(rule.GetNextOccurrence(start, new DateTime(2026, 1, 3, 12, 0, 0)));
    }

    /// <summary>
    /// Verifies that a rule selecting a date no year contains reports no next occurrence rather than searching without
    /// end.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenRuleCanNeverMatch_ShouldReturnNull()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=YEARLY;BYMONTH=2;BYMONTHDAY=30");

        Assert.IsNull(rule.GetNextOccurrence(new DateTime(2026, 1, 1), new DateTime(2026, 1, 1)));
    }

    /// <summary>
    /// Verifies that the <see cref="DateTimeOffset" /> overload answers the same wall clock as its
    /// <see cref="DateTime" /> counterpart and carries the query's offset onto the result.
    /// </summary>
    /// <remarks>
    /// The library resolves no time zone, so the offset must ride along unchanged rather than being converted.
    /// </remarks>
    [TestMethod]
    public void GetNextOccurrence_ForDateTimeOffset_ShouldPreserveOffset()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY");
        var offset = TimeSpan.FromHours(10);
        var start = new DateTimeOffset(2026, 1, 1, 9, 0, 0, offset);

        DateTimeOffset? next = rule.GetNextOccurrence(start, new DateTimeOffset(2026, 1, 4, 12, 0, 0, offset));

        Assert.AreEqual(new DateTimeOffset(2026, 1, 5, 9, 0, 0, offset), next);
        Assert.AreEqual(offset, next!.Value.Offset);
    }

    /// <summary>
    /// Verifies that the <see cref="DateTimeOffset" /> overload honors the inclusive flag identically to the
    /// <see cref="DateTime" /> overload.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_ForDateTimeOffset_ShouldHonorInclusiveFlag()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY");
        var offset = TimeSpan.FromHours(-5);
        var start = new DateTimeOffset(2026, 1, 1, 9, 0, 0, offset);
        var occurrence = new DateTimeOffset(2026, 1, 3, 9, 0, 0, offset);

        Assert.AreEqual(new DateTimeOffset(2026, 1, 4, 9, 0, 0, offset), rule.GetNextOccurrence(start, occurrence));
        Assert.AreEqual(occurrence, rule.GetNextOccurrence(start, occurrence, inclusive: true));
    }

    /// <summary>
    /// Verifies that the same wall-clock series is produced whatever offset the arguments carry, so the offset
    /// selects no different occurrences.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_ForDateTimeOffset_WhenOffsetsDiffer_ShouldSelectTheSameWallClock()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=MO");

        foreach (TimeSpan offset in new[] { TimeSpan.Zero, TimeSpan.FromHours(10), TimeSpan.FromHours(-8) })
        {
            var start = new DateTimeOffset(2026, 1, 5, 9, 0, 0, offset);
            DateTimeOffset? next = rule.GetNextOccurrence(start, new DateTimeOffset(2026, 1, 7, 0, 0, 0, offset));

            Assert.AreEqual(new DateTimeOffset(2026, 1, 12, 9, 0, 0, offset), next, $"offset {offset}");
        }
    }
}
