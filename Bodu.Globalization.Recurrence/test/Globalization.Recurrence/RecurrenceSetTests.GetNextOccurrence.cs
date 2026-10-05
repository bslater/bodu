// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceSetTests.GetNextOccurrence.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceSetTests
{
    /// <summary>
    /// Verifies that the next occurrence of a set that started decades earlier skips a run of exception dates.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenStartIsDecadesEarlier_ShouldSkipTheExcludedRun()
    {
        var set = new RecurrenceSet(
            new DateTime(1990, 3, 15, 9, 30, 0),
            [RecurrenceRule.Parse("FREQ=DAILY")],
            exceptionDates: Enumerable.Range(0, 17).Select(i => new DateTime(2025, 12, 20, 9, 30, 0).AddDays(i)));

        DateTime? next = set.GetNextOccurrence(new DateTime(2025, 12, 24, 12, 0, 0));

        Assert.AreEqual(new DateTime(2026, 1, 6, 9, 30, 0), next);
    }

    /// <summary>
    /// Verifies that when two rules produce the same instant, the next occurrence after it is the next distinct one,
    /// and the instant itself with the inclusive flag.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenRulesShareAnInstant_ShouldReturnTheNextDistinctOccurrence()
    {
        var set = new RecurrenceSet(
            new DateTime(1993, 2, 10, 14, 0, 0),
            [RecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=MO,WE,FR"), RecurrenceRule.Parse("FREQ=WEEKLY;INTERVAL=2;BYDAY=WE,SA")]);
        var shared = new DateTime(2026, 3, 11, 14, 0, 0);

        Assert.AreEqual(shared, set.GetNextOccurrence(shared, inclusive: true));
        Assert.AreEqual(new DateTime(2026, 3, 13, 14, 0, 0), set.GetNextOccurrence(shared));
    }

    /// <summary>
    /// Verifies that an explicit date after the last occurrence of a bounded rule is the next occurrence.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenExplicitDateFollowsTheRuleEnd_ShouldReturnTheExplicitDate()
    {
        var set = new RecurrenceSet(
            new DateTime(2000, 1, 1, 9, 0, 0),
            [RecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=TU;UNTIL=20100630T000000")],
            dates: [new DateTime(2012, 1, 1, 9, 0, 0)]);

        DateTime? next = set.GetNextOccurrence(new DateTime(2011, 1, 1));

        Assert.AreEqual(new DateTime(2012, 1, 1, 9, 0, 0), next);
    }

    /// <summary>
    /// Verifies that once a rule with a count is exhausted, the next occurrence comes from the set's other rule.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenCountRuleIsExhausted_ShouldReturnTheOtherRulesOccurrence()
    {
        var set = new RecurrenceSet(
            new DateTime(2000, 1, 1, 8, 0, 0),
            [RecurrenceRule.Parse("FREQ=DAILY;COUNT=500"), RecurrenceRule.Parse("FREQ=YEARLY;BYMONTH=11;BYDAY=4TH")]);

        DateTime? next = set.GetNextOccurrence(new DateTime(2025, 1, 1));

        Assert.AreEqual(new DateTime(2025, 11, 27, 8, 0, 0), next);
    }

    /// <summary>
    /// Verifies that the offset overload of the next-occurrence query interprets the set's wall-clock values in the
    /// query's offset and carries that offset on the answer.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenQueryHasOffset_ShouldCarryQueryOffset()
    {
        var set = new RecurrenceSet(
            new DateTime(2026, 1, 1, 9, 0, 0),
            [RecurrenceRule.Parse("FREQ=DAILY")]);

        DateTimeOffset? next = set.GetNextOccurrence(new DateTimeOffset(2026, 1, 4, 12, 0, 0, TimeSpan.FromHours(10)));

        Assert.AreEqual(new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.FromHours(10)), next);
        Assert.AreEqual(TimeSpan.FromHours(10), next!.Value.Offset);
    }
}
