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
