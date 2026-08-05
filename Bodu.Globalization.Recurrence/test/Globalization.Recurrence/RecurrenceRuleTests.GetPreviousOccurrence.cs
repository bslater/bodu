// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRuleTests.GetPreviousOccurrence.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceRuleTests
{
    /// <summary>
    /// Verifies that the previous occurrence of a daily rule is the most recent expansion before the query instant.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenDaily_ShouldReturnMostRecentOccurrence()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY");
        var start = new DateTime(2026, 1, 1, 9, 0, 0);

        DateTime? previous = rule.GetPreviousOccurrence(start, new DateTime(2026, 1, 5, 0, 0, 0));

        Assert.AreEqual(new DateTime(2026, 1, 4, 9, 0, 0), previous);
    }

    /// <summary>
    /// Verifies that a query exactly on an occurrence is excluded by default and included with the inclusive flag.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenBeforeEqualsOccurrence_ShouldHonorInclusiveFlag()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY");
        var start = new DateTime(2026, 1, 1, 9, 0, 0);
        var occurrence = new DateTime(2026, 1, 3, 9, 0, 0);

        Assert.AreEqual(new DateTime(2026, 1, 2, 9, 0, 0), rule.GetPreviousOccurrence(start, occurrence));
        Assert.AreEqual(occurrence, rule.GetPreviousOccurrence(start, occurrence, inclusive: true));
    }

    /// <summary>
    /// Verifies that no occurrence precedes the series start, and that the start itself is answered only inclusively.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenBeforeSeriesStart_ShouldReturnNull()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY");
        var start = new DateTime(2026, 1, 1, 9, 0, 0);

        Assert.IsNull(rule.GetPreviousOccurrence(start, start.AddDays(-1)));
        Assert.IsNull(rule.GetPreviousOccurrence(start, start));
        Assert.AreEqual(start, rule.GetPreviousOccurrence(start, start, inclusive: true));
    }

    /// <summary>
    /// Verifies that a count-bounded rule answers its final occurrence for any later query instant.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenAfterCountBoundedSeriesEnds_ShouldReturnFinalOccurrence()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY;COUNT=3");
        var start = new DateTime(2026, 1, 1, 9, 0, 0);

        DateTime? previous = rule.GetPreviousOccurrence(start, new DateTime(2026, 6, 1, 0, 0, 0));

        Assert.AreEqual(new DateTime(2026, 1, 3, 9, 0, 0), previous);
    }

    /// <summary>
    /// Verifies that a weekly <c>BYDAY</c> rule answers the most recent matching weekday.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenWeeklyByDay_ShouldReturnMostRecentMatchingWeekday()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=MO,FR");
        var start = new DateTime(2026, 1, 5, 9, 0, 0);

        DateTime? previous = rule.GetPreviousOccurrence(start, new DateTime(2026, 1, 14, 0, 0, 0));

        Assert.AreEqual(new DateTime(2026, 1, 12, 9, 0, 0), previous);
    }

    /// <summary>
    /// Verifies that the offset overload preserves the start's offset on the answered occurrence.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenStartHasOffset_ShouldPreserveStartOffset()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY");
        var start = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.FromHours(10));

        DateTimeOffset? previous = rule.GetPreviousOccurrence(start, new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.FromHours(10)));

        Assert.AreEqual(new DateTimeOffset(2026, 1, 4, 9, 0, 0, TimeSpan.FromHours(10)), previous);
        Assert.AreEqual(TimeSpan.FromHours(10), previous!.Value.Offset);
    }

    /// <summary>
    /// Verifies the due-ness recipe coalesces missed occurrences: a daily schedule evaluated five days late answers
    /// the same due boolean as one evaluated a minute late.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenOccurrencesWereMissed_ShouldCoalesceToSingleDueAnswer()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY");
        var start = new DateTime(2026, 1, 1, 2, 0, 0);
        var lastCompleted = new DateTime(2026, 1, 2, 2, 0, 0);

        bool dueShortlyAfter = lastCompleted < rule.GetPreviousOccurrence(start, new DateTime(2026, 1, 3, 2, 1, 0), inclusive: true);
        bool dueMuchLater = lastCompleted < rule.GetPreviousOccurrence(start, new DateTime(2026, 1, 8, 2, 0, 0), inclusive: true);

        Assert.IsTrue(dueShortlyAfter);
        Assert.AreEqual(dueShortlyAfter, dueMuchLater);
    }
}
