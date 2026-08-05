// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AnchoredIntervalTests.Conformance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class AnchoredIntervalTests
{
    /// <summary>
    /// Verifies the anchored-interval acceptance contract: a four-hour interval anchored at 08:00 is due at 12:00
    /// exactly, via the due-ness comparison over the previous occurrence.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenEvaluatedAtNoon_ShouldMakeScheduleDueExactly()
    {
        var interval = new AnchoredInterval(TimeSpan.FromHours(4));
        var lastCompleted = new DateTime(2026, 1, 1, 8, 0, 0);

        DateTime? justBefore = interval.GetPreviousOccurrence(lastCompleted, new DateTime(2026, 1, 1, 11, 59, 59), inclusive: true);
        DateTime? atNoon = interval.GetPreviousOccurrence(lastCompleted, new DateTime(2026, 1, 1, 12, 0, 0), inclusive: true);

        Assert.IsNull(justBefore);
        Assert.IsTrue(lastCompleted < atNoon);
    }

    /// <summary>
    /// Verifies the coalescing contract: an evaluation two missed occurrences late answers due-ness identically to an
    /// evaluation one minute after the first occurrence — the count of missed occurrences is never part of the
    /// answer.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenOccurrencesWereMissed_ShouldCoalesceToSingleDueAnswer()
    {
        var interval = new AnchoredInterval(TimeSpan.FromHours(4));
        var lastCompleted = new DateTime(2026, 1, 1, 8, 0, 0);

        bool dueShortlyAfter = lastCompleted < interval.GetPreviousOccurrence(
            lastCompleted, new DateTime(2026, 1, 1, 12, 1, 0), inclusive: true);
        bool dueMuchLater = lastCompleted < interval.GetPreviousOccurrence(
            lastCompleted, new DateTime(2026, 1, 1, 20, 0, 0), inclusive: true);

        Assert.IsTrue(dueShortlyAfter);
        Assert.AreEqual(dueShortlyAfter, dueMuchLater);
    }

    /// <summary>
    /// Verifies that a run completed at the evaluation instant is not immediately due again, because the anchor is
    /// not an occurrence.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenRunJustCompleted_ShouldNotBeDue()
    {
        var interval = new AnchoredInterval(TimeSpan.FromHours(4));
        var now = new DateTime(2026, 1, 1, 12, 0, 0);

        DateTime? previous = interval.GetPreviousOccurrence(anchor: now, before: now, inclusive: true);

        Assert.IsNull(previous);
    }

    /// <summary>
    /// Verifies that the due-ness answer is identical whether the evaluation instant is supplied in UTC or in a local
    /// offset describing the same instant, so a UTC-only test matrix can never mask an offset regression.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenSameInstantInDifferentOffsets_ShouldAnswerIdentically()
    {
        var interval = new AnchoredInterval(TimeSpan.FromHours(4));
        var anchor = new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero);
        var nowUtc = new DateTimeOffset(2026, 1, 1, 13, 0, 0, TimeSpan.Zero);
        DateTimeOffset nowLocal = nowUtc.ToOffset(TimeSpan.FromHours(10));

        DateTimeOffset? fromUtc = interval.GetPreviousOccurrence(anchor, nowUtc, inclusive: true);
        DateTimeOffset? fromLocal = interval.GetPreviousOccurrence(anchor, nowLocal, inclusive: true);

        Assert.IsNotNull(fromUtc);
        Assert.IsNotNull(fromLocal);
        Assert.AreEqual(fromUtc.Value.UtcDateTime, fromLocal.Value.UtcDateTime);
    }
}
