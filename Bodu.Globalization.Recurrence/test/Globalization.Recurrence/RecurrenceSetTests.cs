// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceSetTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Contains unit tests for the <see cref="RecurrenceSet" /> type.
/// </summary>
[TestClass]
public sealed partial class RecurrenceSetTests
{
    /// <summary>
    /// Verifies that parsing a property block merges the rule, adds the recurrence date, and removes the exception date.
    /// </summary>
    [TestMethod]
    public void Parse_WhenBlockHasRuleRDateAndExDate_ShouldMergeAndExclude()
    {
        const string block =
            "DTSTART:20260101T090000\n" +
            "RRULE:FREQ=WEEKLY;BYDAY=MO;COUNT=3\n" +
            "RDATE:20260102T090000\n" +
            "EXDATE:20260105T090000";

        RecurrenceSet set = RecurrenceSet.Parse(block);
        DateTime[] actual = set.GetOccurrences().Take(10).ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(2026, 1, 2, 9, 0, 0),
                new DateTime(2026, 1, 12, 9, 0, 0),
                new DateTime(2026, 1, 19, 9, 0, 0),
            },
            actual);
    }

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
    /// Verifies that constructing a set with neither a rule nor an explicit date throws
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenNoRuleOrDate_ShouldThrowArgumentException()
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new RecurrenceSet(new DateTime(2026, 1, 1), []);
        });
    }

    /// <summary>
    /// Verifies that a property block missing a <c>DTSTART</c> line fails to parse.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenNoDtStart_ShouldReturnFalse()
    {
        bool parsed = RecurrenceSet.TryParse("RRULE:FREQ=DAILY;COUNT=3", out RecurrenceSet? result);

        Assert.IsFalse(parsed);
        Assert.IsNull(result);
    }
}
