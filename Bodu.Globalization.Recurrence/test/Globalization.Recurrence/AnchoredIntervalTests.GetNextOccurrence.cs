// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AnchoredIntervalTests.GetNextOccurrence.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class AnchoredIntervalTests
{
    /// <summary>
    /// Verifies that a four-hour interval anchored at eight o'clock answers noon as the next occurrence.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void GetNextOccurrence_WhenAfterAnchor_ShouldReturnAnchorPlusInterval()
    {
        var interval = new AnchoredInterval(TimeSpan.FromHours(4));
        var anchor = new DateTime(2026, 1, 1, 8, 0, 0);

        DateTime? next = interval.GetNextOccurrence(anchor, new DateTime(2026, 1, 1, 9, 0, 0));

        Assert.AreEqual(new DateTime(2026, 1, 1, 12, 0, 0), next);
    }

    /// <summary>
    /// Verifies that a query instant before the anchor still answers the first occurrence, one interval after the
    /// anchor.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenAfterPrecedesAnchor_ShouldReturnFirstOccurrence()
    {
        var interval = new AnchoredInterval(TimeSpan.FromHours(4));
        var anchor = new DateTime(2026, 1, 1, 8, 0, 0);

        DateTime? next = interval.GetNextOccurrence(anchor, new DateTime(2025, 12, 31, 0, 0, 0));

        Assert.AreEqual(new DateTime(2026, 1, 1, 12, 0, 0), next);
    }

    /// <summary>
    /// Verifies that a query exactly on an occurrence is excluded by default and included with the inclusive flag.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenAfterEqualsOccurrence_ShouldHonorInclusiveFlag()
    {
        var interval = new AnchoredInterval(TimeSpan.FromHours(4));
        var anchor = new DateTime(2026, 1, 1, 8, 0, 0);
        var occurrence = new DateTime(2026, 1, 1, 12, 0, 0);

        Assert.AreEqual(new DateTime(2026, 1, 1, 16, 0, 0), interval.GetNextOccurrence(anchor, occurrence));
        Assert.AreEqual(occurrence, interval.GetNextOccurrence(anchor, occurrence, inclusive: true));
    }

    /// <summary>
    /// Verifies that the anchor itself is not an occurrence: a query just before the anchor answers the first
    /// occurrence, never the anchor.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenAfterJustBeforeAnchor_ShouldSkipAnchor()
    {
        var interval = new AnchoredInterval(TimeSpan.FromHours(4));
        var anchor = new DateTime(2026, 1, 1, 8, 0, 0);

        DateTime? next = interval.GetNextOccurrence(anchor, anchor.AddSeconds(-1), inclusive: true);

        Assert.AreEqual(new DateTime(2026, 1, 1, 12, 0, 0), next);
    }

    /// <summary>
    /// Verifies that the result preserves the <see cref="DateTime.Kind" /> of the anchor.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenAnchorIsUtc_ShouldPreserveAnchorKind()
    {
        var interval = new AnchoredInterval(TimeSpan.FromHours(4));
        var anchor = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

        DateTime? next = interval.GetNextOccurrence(anchor, new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc));

        Assert.AreEqual(DateTimeKind.Utc, next!.Value.Kind);
    }

    /// <summary>
    /// Verifies that no occurrence is answered when the next one would fall past the end of the representable
    /// calendar.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenPastRepresentableRange_ShouldReturnNull()
    {
        var interval = new AnchoredInterval(TimeSpan.FromHours(4));
        DateTime anchor = DateTime.MaxValue.AddHours(-1);

        DateTime? next = interval.GetNextOccurrence(anchor, anchor);

        Assert.IsNull(next);
    }

    /// <summary>
    /// Verifies that anchor and query instants supplied in different offsets are compared as absolute instants, with
    /// the result carrying the query's offset.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenOffsetsDiffer_ShouldCompareInstantsAndCarryQueryOffset()
    {
        var interval = new AnchoredInterval(TimeSpan.FromHours(4));
        var anchor = new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero);
        var after = new DateTimeOffset(2026, 1, 1, 19, 0, 0, TimeSpan.FromHours(10));

        DateTimeOffset? next = interval.GetNextOccurrence(anchor, after);

        Assert.AreEqual(new DateTimeOffset(2026, 1, 1, 22, 0, 0, TimeSpan.FromHours(10)), next);
        Assert.AreEqual(TimeSpan.FromHours(10), next!.Value.Offset);
    }
}
