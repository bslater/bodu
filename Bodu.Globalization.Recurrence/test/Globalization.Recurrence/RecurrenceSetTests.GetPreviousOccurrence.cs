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
    /// Verifies that a run of exception dates over a sub-daily rule's occurrences is skipped, answering the occurrence
    /// before the run.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenSubDailyOccurrencesAreExcluded_ShouldSkipToTheOneBeforeThem()
    {
        var set = new RecurrenceSet(
            new DateTime(1990, 1, 1, 0, 0, 0),
            [RecurrenceRule.Parse("FREQ=MINUTELY;INTERVAL=15")],
            exceptionDates: [new DateTime(2026, 10, 4, 11, 30, 0), new DateTime(2026, 10, 4, 11, 45, 0), new DateTime(2026, 10, 4, 12, 0, 0)]);

        DateTime? previous = set.GetPreviousOccurrence(new DateTime(2026, 10, 4, 12, 0, 0), inclusive: true);

        Assert.AreEqual(new DateTime(2026, 10, 4, 11, 15, 0), previous);
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

    /// <summary>
    /// Verifies that the previous occurrence of a set that started decades earlier skips back over a run of exception
    /// dates.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenStartIsDecadesEarlier_ShouldSkipTheExcludedRun()
    {
        var set = new RecurrenceSet(
            new DateTime(1990, 3, 15, 9, 30, 0),
            [RecurrenceRule.Parse("FREQ=DAILY")],
            exceptionDates: Enumerable.Range(0, 17).Select(i => new DateTime(2025, 12, 20, 9, 30, 0).AddDays(i)));

        DateTime? previous = set.GetPreviousOccurrence(new DateTime(2026, 1, 5, 12, 0, 0));

        Assert.AreEqual(new DateTime(2025, 12, 19, 9, 30, 0), previous);
    }

    /// <summary>
    /// Verifies that when a whole year of a rule's occurrences is excluded, the previous occurrence is the last one
    /// before that year.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenAYearOfOccurrencesIsExcluded_ShouldReturnTheOccurrenceBeforeIt()
    {
        var set = new RecurrenceSet(
            new DateTime(2010, 1, 1, 7, 0, 0),
            [RecurrenceRule.Parse("FREQ=DAILY")],
            exceptionDates: Enumerable.Range(0, 366).Select(i => new DateTime(2024, 1, 1, 7, 0, 0).AddDays(i)));

        DateTime? previous = set.GetPreviousOccurrence(new DateTime(2024, 12, 31, 12, 0, 0));

        Assert.AreEqual(new DateTime(2023, 12, 31, 7, 0, 0), previous);
    }

    /// <summary>
    /// Verifies that an explicit date earlier than the set's start is the previous occurrence of an instant between the
    /// two.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenExplicitDatePrecedesTheStart_ShouldReturnTheExplicitDate()
    {
        var set = new RecurrenceSet(
            new DateTime(1991, 5, 31, 9, 0, 0),
            [RecurrenceRule.Parse("FREQ=MONTHLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-1")],
            dates: [new DateTime(1970, 1, 1)]);

        DateTime? previous = set.GetPreviousOccurrence(new DateTime(1990, 6, 1));

        Assert.AreEqual(new DateTime(1970, 1, 1), previous);
    }

    /// <summary>
    /// Verifies that a query exactly on an explicit date given twice answers the occurrence before it, and the date
    /// itself with the inclusive flag.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenQueryIsOnADuplicatedExplicitDate_ShouldHonorInclusiveFlag()
    {
        var date = new DateTime(2026, 3, 15, 10, 0, 0);
        var set = new RecurrenceSet(
            new DateTime(1991, 5, 31, 9, 0, 0),
            [RecurrenceRule.Parse("FREQ=MONTHLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-1")],
            dates: [date, date, new DateTime(2026, 6, 1)]);

        Assert.AreEqual(new DateTime(2026, 2, 27, 9, 0, 0), set.GetPreviousOccurrence(date));
        Assert.AreEqual(date, set.GetPreviousOccurrence(date, inclusive: true));
    }

    /// <summary>
    /// Verifies that the previous occurrence is the latest of the set's rules when one of them has a count.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenOneRuleHasACount_ShouldReturnTheLatestOfEitherRule()
    {
        var set = new RecurrenceSet(
            new DateTime(2000, 1, 1, 8, 0, 0),
            [RecurrenceRule.Parse("FREQ=DAILY;COUNT=500"), RecurrenceRule.Parse("FREQ=YEARLY;BYMONTH=11;BYDAY=4TH")]);

        Assert.AreEqual(new DateTime(2001, 5, 14, 8, 0, 0), set.GetPreviousOccurrence(new DateTime(2001, 6, 1)));
        Assert.AreEqual(new DateTime(2025, 11, 27, 8, 0, 0), set.GetPreviousOccurrence(new DateTime(2025, 12, 1)));
    }
}
