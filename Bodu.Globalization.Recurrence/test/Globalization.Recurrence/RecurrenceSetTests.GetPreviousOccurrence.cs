// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceSetTests.GetPreviousOccurrence.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceSetTests
{
    /// <summary>
    /// Verifies that the previous occurrence of a composed set is the most recent contributing instant.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenDailyRule_ShouldReturnMostRecentOccurrence()
    {
        var set = new RecurrenceSet(
            new DateTime(2026, 1, 1, 9, 0, 0),
            [RecurrenceRule.Parse("FREQ=DAILY")]);

        DateTime? previous = set.GetPreviousOccurrence(new DateTime(2026, 1, 5, 0, 0, 0));

        Assert.AreEqual(new DateTime(2026, 1, 4, 9, 0, 0), previous);
    }

    /// <summary>
    /// Verifies that an exception date is skipped, answering the occurrence before it.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenMostRecentIsExceptionDate_ShouldSkipToPriorOccurrence()
    {
        var set = new RecurrenceSet(
            new DateTime(2026, 1, 1, 9, 0, 0),
            [RecurrenceRule.Parse("FREQ=DAILY")],
            exceptionDates: [new DateTime(2026, 1, 3, 9, 0, 0)]);

        DateTime? previous = set.GetPreviousOccurrence(new DateTime(2026, 1, 3, 12, 0, 0));

        Assert.AreEqual(new DateTime(2026, 1, 2, 9, 0, 0), previous);
    }

    /// <summary>
    /// Verifies that an explicit recurrence date contributes to the previous-occurrence answer.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenExplicitDateIsMostRecent_ShouldReturnExplicitDate()
    {
        var set = new RecurrenceSet(
            new DateTime(2026, 1, 1, 9, 0, 0),
            [RecurrenceRule.Parse("FREQ=WEEKLY")],
            dates: [new DateTime(2026, 1, 3, 15, 0, 0)]);

        DateTime? previous = set.GetPreviousOccurrence(new DateTime(2026, 1, 4, 0, 0, 0));

        Assert.AreEqual(new DateTime(2026, 1, 3, 15, 0, 0), previous);
    }

    /// <summary>
    /// Verifies that a query exactly on an occurrence is excluded by default and included with the inclusive flag.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenBeforeEqualsOccurrence_ShouldHonorInclusiveFlag()
    {
        var set = new RecurrenceSet(
            new DateTime(2026, 1, 1, 9, 0, 0),
            [RecurrenceRule.Parse("FREQ=DAILY")]);
        var occurrence = new DateTime(2026, 1, 3, 9, 0, 0);

        Assert.AreEqual(new DateTime(2026, 1, 2, 9, 0, 0), set.GetPreviousOccurrence(occurrence));
        Assert.AreEqual(occurrence, set.GetPreviousOccurrence(occurrence, inclusive: true));
    }

    /// <summary>
    /// Verifies that no occurrence precedes the set's start.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenBeforeSetStart_ShouldReturnNull()
    {
        var set = new RecurrenceSet(
            new DateTime(2026, 1, 1, 9, 0, 0),
            [RecurrenceRule.Parse("FREQ=DAILY")]);

        Assert.IsNull(set.GetPreviousOccurrence(new DateTime(2025, 12, 31, 0, 0, 0)));
    }

    /// <summary>
    /// Verifies that the offset overload interprets the set's wall-clock values in the query's offset and carries
    /// that offset on the answer.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenQueryHasOffset_ShouldCarryQueryOffset()
    {
        var set = new RecurrenceSet(
            new DateTime(2026, 1, 1, 9, 0, 0),
            [RecurrenceRule.Parse("FREQ=DAILY")]);

        DateTimeOffset? previous = set.GetPreviousOccurrence(new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.FromHours(10)));

        Assert.AreEqual(new DateTimeOffset(2026, 1, 4, 9, 0, 0, TimeSpan.FromHours(10)), previous);
        Assert.AreEqual(TimeSpan.FromHours(10), previous!.Value.Offset);
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

    /// <summary>
    /// Verifies the due-ness recipe coalesces missed occurrences on a composed set: the due boolean is identical
    /// whether the evaluation is one occurrence late or five.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenOccurrencesWereMissed_ShouldCoalesceToSingleDueAnswer()
    {
        var set = new RecurrenceSet(
            new DateTime(2026, 1, 1, 2, 0, 0),
            [RecurrenceRule.Parse("FREQ=DAILY")]);
        var lastCompleted = new DateTime(2026, 1, 2, 2, 0, 0);

        bool dueShortlyAfter = lastCompleted < set.GetPreviousOccurrence(new DateTime(2026, 1, 3, 2, 1, 0), inclusive: true);
        bool dueMuchLater = lastCompleted < set.GetPreviousOccurrence(new DateTime(2026, 1, 8, 2, 0, 0), inclusive: true);

        Assert.IsTrue(dueShortlyAfter);
        Assert.AreEqual(dueShortlyAfter, dueMuchLater);
    }
}
