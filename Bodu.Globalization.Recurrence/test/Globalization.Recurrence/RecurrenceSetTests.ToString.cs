// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceSetTests.ToString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceSetTests
{
    /// <summary>
    /// Verifies that a set with rules, explicit dates, and exception dates renders as the exact canonical property
    /// block.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFullyPopulated_ShouldRenderCanonicalPropertyBlock()
    {
        var set = new RecurrenceSet(
            new DateTime(2026, 1, 1, 9, 0, 0),
            [RecurrenceRule.Parse("FREQ=DAILY"), RecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=MO")],
            dates: [new DateTime(2026, 1, 10, 15, 0, 0), new DateTime(2026, 1, 3, 15, 0, 0)],
            exceptionDates: [new DateTime(2026, 1, 5, 9, 0, 0), new DateTime(2026, 1, 2, 9, 0, 0)]);

        string text = set.ToString();

        Assert.AreEqual(
            "DTSTART:20260101T090000\r\n"
            + "RRULE:FREQ=DAILY\r\n"
            + "RRULE:FREQ=WEEKLY;BYDAY=MO\r\n"
            + "RDATE:20260103T150000,20260110T150000\r\n"
            + "EXDATE:20260102T090000,20260105T090000",
            text);
    }

    /// <summary>
    /// Verifies that a rules-only set renders without <c>RDATE</c> or <c>EXDATE</c> lines.
    /// </summary>
    [TestMethod]
    public void ToString_WhenRulesOnly_ShouldOmitDateLines()
    {
        var set = new RecurrenceSet(
            new DateTime(2026, 1, 1, 9, 0, 0),
            [RecurrenceRule.Parse("FREQ=DAILY")]);

        Assert.AreEqual("DTSTART:20260101T090000\r\nRRULE:FREQ=DAILY", set.ToString());
    }

    /// <summary>
    /// Verifies that the canonical text re-parses to an equal set.
    /// </summary>
    [TestMethod]
    public void ToString_WhenReparsed_ShouldRoundTrip()
    {
        var set = new RecurrenceSet(
            new DateTime(2026, 1, 1, 9, 0, 0),
            [RecurrenceRule.Parse("FREQ=DAILY;COUNT=10")],
            dates: [new DateTime(2026, 1, 3, 15, 0, 0)],
            exceptionDates: [new DateTime(2026, 1, 2, 9, 0, 0)]);

        RecurrenceSet reparsed = RecurrenceSet.Parse(set.ToString());

        Assert.AreEqual(set, reparsed);
    }

    /// <summary>
    /// Verifies that the general format specifier and the parameterless overload produce the same text.
    /// </summary>
    [TestMethod]
    public void ToString_WhenGeneralSpecifier_ShouldMatchDefault()
    {
        var set = new RecurrenceSet(
            new DateTime(2026, 1, 1, 9, 0, 0),
            [RecurrenceRule.Parse("FREQ=DAILY")]);

        Assert.AreEqual(set.ToString(), set.ToString("G", null));
        Assert.AreEqual(set.ToString(), set.ToString(null, null));
    }

    /// <summary>
    /// Verifies that an unsupported format specifier throws <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFormatSpecifierUnsupported_ShouldThrowFormatException()
    {
        var set = new RecurrenceSet(
            new DateTime(2026, 1, 1, 9, 0, 0),
            [RecurrenceRule.Parse("FREQ=DAILY")]);

        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = set.ToString("X", null);
        });

        Assert.IsTrue(ex.Message.Contains("'X'", StringComparison.Ordinal));
    }
}
