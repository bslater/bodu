// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AnchoredIntervalTests.GetPreviousOccurrence.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class AnchoredIntervalTests
{
    /// <summary>
    /// Verifies that the previous occurrence is the most recent multiple of the interval after the anchor.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenAfterFirstOccurrence_ShouldReturnMostRecentOccurrence()
    {
        var interval = new AnchoredInterval(TimeSpan.FromHours(4));
        var anchor = new DateTime(2026, 1, 1, 8, 0, 0);

        DateTime? previous = interval.GetPreviousOccurrence(anchor, new DateTime(2026, 1, 1, 13, 0, 0));

        Assert.AreEqual(new DateTime(2026, 1, 1, 12, 0, 0), previous);
    }

    /// <summary>
    /// Verifies that a query exactly on an occurrence is excluded by default and included with the inclusive flag.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenBeforeEqualsOccurrence_ShouldHonorInclusiveFlag()
    {
        var interval = new AnchoredInterval(TimeSpan.FromHours(4));
        var anchor = new DateTime(2026, 1, 1, 8, 0, 0);
        var occurrence = new DateTime(2026, 1, 1, 12, 0, 0);

        Assert.IsNull(interval.GetPreviousOccurrence(anchor, occurrence));
        Assert.AreEqual(occurrence, interval.GetPreviousOccurrence(anchor, occurrence, inclusive: true));
    }

    /// <summary>
    /// Verifies that no occurrence precedes the first one: the anchor itself is never answered.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenBeforeFirstOccurrence_ShouldReturnNull()
    {
        var interval = new AnchoredInterval(TimeSpan.FromHours(4));
        var anchor = new DateTime(2026, 1, 1, 8, 0, 0);

        Assert.IsNull(interval.GetPreviousOccurrence(anchor, new DateTime(2026, 1, 1, 11, 59, 0)));
        Assert.IsNull(interval.GetPreviousOccurrence(anchor, anchor, inclusive: true));
        Assert.IsNull(interval.GetPreviousOccurrence(anchor, anchor.AddDays(-1)));
    }

    /// <summary>
    /// Verifies that the result preserves the <see cref="DateTime.Kind" /> of the anchor.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenAnchorIsUtc_ShouldPreserveAnchorKind()
    {
        var interval = new AnchoredInterval(TimeSpan.FromHours(4));
        var anchor = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

        DateTime? previous = interval.GetPreviousOccurrence(anchor, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));

        Assert.AreEqual(DateTimeKind.Utc, previous!.Value.Kind);
    }

    /// <summary>
    /// Verifies that anchor and query instants supplied in different offsets are compared as absolute instants, with
    /// the result carrying the query's offset.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenOffsetsDiffer_ShouldCompareInstantsAndCarryQueryOffset()
    {
        var interval = new AnchoredInterval(TimeSpan.FromHours(4));
        var anchor = new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero);
        var before = new DateTimeOffset(2026, 1, 1, 23, 30, 0, TimeSpan.FromHours(10));

        DateTimeOffset? previous = interval.GetPreviousOccurrence(anchor, before);

        Assert.AreEqual(new DateTimeOffset(2026, 1, 1, 22, 0, 0, TimeSpan.FromHours(10)), previous);
        Assert.AreEqual(TimeSpan.FromHours(10), previous!.Value.Offset);
    }
}
