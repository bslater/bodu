// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceSetTests.Conformance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceSetTests
{
    /// <summary>
    /// Parses the property block and returns the first ten occurrences of the resulting set.
    /// </summary>
    /// <param name="block">The iCalendar property block.</param>
    /// <returns>The leading occurrences.</returns>
    private static DateTime[] SetOccurrences(string block) =>
        RecurrenceSet.Parse(block).GetOccurrences().Take(10).ToArray();

    /// <summary>
    /// Verifies the canonical dateutil example: an <c>EXDATE</c> removes a rule occurrence, an off-pattern <c>RDATE</c>
    /// sorts into the middle, and <c>COUNT</c> is applied before exclusion (four generated, one removed → three rule
    /// dates survive).
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenCanonicalDateutilExample_ShouldMatchReference()
    {
        DateTime[] actual = SetOccurrences(
            "DTSTART:19970902T090000\n" +
            "RRULE:FREQ=WEEKLY;COUNT=4\n" +
            "RDATE:19970907T090000\n" +
            "EXDATE:19970916T090000");

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(1997, 9, 2, 9, 0, 0),
                new DateTime(1997, 9, 7, 9, 0, 0),
                new DateTime(1997, 9, 9, 9, 0, 0),
                new DateTime(1997, 9, 23, 9, 0, 0),
            },
            actual);
    }

    /// <summary>
    /// Verifies that an <c>RDATE</c> with a different time-of-day is a distinct instant and sorts into the sequence.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenRDateHasDifferentTime_ShouldNotDeduplicate()
    {
        DateTime[] actual = SetOccurrences(
            "DTSTART:19970902T090000\n" +
            "RRULE:FREQ=DAILY;COUNT=3\n" +
            "RDATE:19970903T120000");

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(1997, 9, 2, 9, 0, 0),
                new DateTime(1997, 9, 3, 9, 0, 0),
                new DateTime(1997, 9, 3, 12, 0, 0),
                new DateTime(1997, 9, 4, 9, 0, 0),
            },
            actual);
    }

    /// <summary>
    /// Verifies that an <c>EXDATE</c> matching an <c>RDATE</c> removes it (exclusion wins over inclusion).
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenExDateMatchesRDate_ShouldExclude()
    {
        DateTime[] actual = SetOccurrences(
            "DTSTART:19970902T090000\n" +
            "RRULE:FREQ=DAILY;COUNT=2\n" +
            "RDATE:19970910T090000\n" +
            "EXDATE:19970910T090000");

        CollectionAssert.AreEqual(
            new[] { new DateTime(1997, 9, 2, 9, 0, 0), new DateTime(1997, 9, 3, 9, 0, 0) },
            actual);
    }

    /// <summary>
    /// Verifies that duplicate instants produced by two rules are collapsed to a single occurrence.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenTwoRulesOverlap_ShouldDeduplicate()
    {
        DateTime[] actual = SetOccurrences(
            "DTSTART:19970902T090000\n" +
            "RRULE:FREQ=DAILY;COUNT=3\n" +
            "RRULE:FREQ=WEEKLY;BYDAY=TU;COUNT=2");

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(1997, 9, 2, 9, 0, 0),
                new DateTime(1997, 9, 3, 9, 0, 0),
                new DateTime(1997, 9, 4, 9, 0, 0),
                new DateTime(1997, 9, 9, 9, 0, 0),
            },
            actual);
    }

    /// <summary>
    /// Verifies that an excluded rule occurrence is not back-filled: <c>COUNT</c> counts before exclusion.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenExDateRemovesCountedOccurrence_ShouldNotBackfill()
    {
        DateTime[] actual = SetOccurrences(
            "DTSTART:19970902T090000\n" +
            "RRULE:FREQ=DAILY;COUNT=5\n" +
            "EXDATE:19970904T090000");

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(1997, 9, 2, 9, 0, 0),
                new DateTime(1997, 9, 3, 9, 0, 0),
                new DateTime(1997, 9, 5, 9, 0, 0),
                new DateTime(1997, 9, 6, 9, 0, 0),
            },
            actual);
    }

    /// <summary>
    /// Verifies that an <c>EXDATE</c> matching no generated instant (same day, different time) is a no-op.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenExDateMatchesNothing_ShouldBeNoOp()
    {
        DateTime[] actual = SetOccurrences(
            "DTSTART:19970902T090000\n" +
            "RRULE:FREQ=DAILY;COUNT=3\n" +
            "EXDATE:19970902T120000");

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(1997, 9, 2, 9, 0, 0),
                new DateTime(1997, 9, 3, 9, 0, 0),
                new DateTime(1997, 9, 4, 9, 0, 0),
            },
            actual);
    }

    /// <summary>
    /// Verifies that a non-matching <c>DTSTART</c> is not emitted (rruleset semantics).
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenDtStartDoesNotMatchRule_ShouldNotEmitStart()
    {
        DateTime[] actual = SetOccurrences(
            "DTSTART:19970902T090000\n" +
            "RRULE:FREQ=WEEKLY;BYDAY=TH;COUNT=2");

        CollectionAssert.AreEqual(
            new[] { new DateTime(1997, 9, 4, 9, 0, 0), new DateTime(1997, 9, 11, 9, 0, 0) },
            actual);
    }

    /// <summary>
    /// Verifies comma-separated <c>RDATE</c>/<c>EXDATE</c> lists and tolerance of surrounding <c>VEVENT</c> lines.
    /// </summary>
    [TestMethod]
    public void Parse_WhenVeventBlockWithDateLists_ShouldMergeAndExclude()
    {
        DateTime[] actual = SetOccurrences(
            "BEGIN:VEVENT\n" +
            "DTSTART:19970902T090000\n" +
            "RRULE:FREQ=DAILY;COUNT=3\n" +
            "RDATE:19970906T090000,19970908T090000\n" +
            "EXDATE:19970903T090000,19970908T090000\n" +
            "END:VEVENT");

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(1997, 9, 2, 9, 0, 0),
                new DateTime(1997, 9, 4, 9, 0, 0),
                new DateTime(1997, 9, 6, 9, 0, 0),
            },
            actual);
    }
}
