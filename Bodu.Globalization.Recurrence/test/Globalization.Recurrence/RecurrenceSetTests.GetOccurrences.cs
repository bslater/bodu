// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceSetTests.GetOccurrences.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceSetTests
{
    /// <summary>
    /// Verifies that a single-rule set enumerates the rule's occurrences from the start.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void GetOccurrences_WhenSingleRule_ShouldYieldRuleOccurrences()
    {
        var set = new RecurrenceSet(new DateTime(2026, 1, 1), [RecurrenceRule.Parse("FREQ=DAILY;COUNT=3")]);

        DateTime[] actual = set.GetOccurrences().Take(10).ToArray();

        CollectionAssert.AreEqual(
            new[] { new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), new DateTime(2026, 1, 3) },
            actual);
    }

    /// <summary>
    /// Verifies that overlapping rule streams are merged into an ascending, duplicate-free sequence.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenRulesOverlap_ShouldDeduplicate()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0);
        var set = new RecurrenceSet(
            start,
            new[]
            {
                RecurrenceRule.Parse("FREQ=DAILY;COUNT=3"),
                RecurrenceRule.Parse("FREQ=DAILY;INTERVAL=2;COUNT=3"),
            });

        DateTime[] actual = set.GetOccurrences().Take(10).ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(2026, 1, 1),
                new DateTime(2026, 1, 2),
                new DateTime(2026, 1, 3),
                new DateTime(2026, 1, 5),
            },
            actual);
    }

    /// <summary>
    /// Verifies that explicit recurrence dates are merged in ascending order with the rule occurrences.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenExplicitDatesSupplied_ShouldMergeInOrder()
    {
        var start = new DateTime(2026, 3, 10);
        var set = new RecurrenceSet(
            start,
            [RecurrenceRule.Parse("FREQ=YEARLY;COUNT=1")],
            dates: [new DateTime(2026, 1, 1), new DateTime(2026, 12, 25)]);

        DateTime[] actual = set.GetOccurrences().Take(10).ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(2026, 1, 1),
                new DateTime(2026, 3, 10),
                new DateTime(2026, 12, 25),
            },
            actual);
    }

    /// <summary>
    /// Verifies that a window decades after the set's start holds the occurrences within it, less the exception dates.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenWindowIsDecadesAfterTheStart_ShouldYieldTheOccurrencesWithinIt()
    {
        var set = new RecurrenceSet(
            new DateTime(1990, 3, 15, 9, 30, 0),
            [RecurrenceRule.Parse("FREQ=DAILY")],
            exceptionDates: Enumerable.Range(0, 17).Select(i => new DateTime(2025, 12, 20, 9, 30, 0).AddDays(i)));

        DateTime[] actual = set.GetOccurrences(new DateTime(2026, 1, 3), new DateTime(2026, 1, 8, 23, 59, 59)).ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(2026, 1, 6, 9, 30, 0),
                new DateTime(2026, 1, 7, 9, 30, 0),
                new DateTime(2026, 1, 8, 9, 30, 0),
            },
            actual);
    }

    /// <summary>
    /// Verifies that a window merges the rule occurrences and the explicit dates within it, and yields an explicit
    /// date given twice once.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenWindowHoldsRuleOccurrencesAndExplicitDates_ShouldMergeThem()
    {
        var date = new DateTime(2026, 3, 15, 10, 0, 0);
        var set = new RecurrenceSet(
            new DateTime(1991, 5, 31, 9, 0, 0),
            [RecurrenceRule.Parse("FREQ=MONTHLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-1")],
            dates: [date, date]);

        DateTime[] actual = set.GetOccurrences(new DateTime(2026, 2, 1), new DateTime(2026, 4, 30, 23, 59, 59)).ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(2026, 2, 27, 9, 0, 0),
                date,
                new DateTime(2026, 3, 31, 9, 0, 0),
                new DateTime(2026, 4, 30, 9, 0, 0),
            },
            actual);
    }
}
